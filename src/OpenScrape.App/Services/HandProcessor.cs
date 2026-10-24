using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Configuration;
using OpenScrape.App.Entities;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Enums;
using OpenScrape.DecisionMaker;

namespace OpenScrape.App.Services;

/// <summary>
/// Procesador de mano que implementa <see cref="IGameLoopTickProcessor"/>.
/// Ejecuta el pipeline de captura → OCR → decisión preflop en cada tick
/// del <see cref="GameLoopCoordinator"/> y retorna un <see cref="GameLoopResult"/>
/// con los datos estructurados para la UI.
///
/// En esta fase (preflop-first scope) solo se procesa el pipeline preflop;
/// los estados postflop retornan <see cref="GameLoopResult.Empty"/> = true
/// para que FrmMain los maneje por el path legacy.
/// </summary>
public sealed class HandProcessor : IGameLoopTickProcessor
{
    private readonly ICaptureService _captureService;
    private readonly ITableReaderService _tableReaderService;
    private readonly GameLoopStateMachine _stateMachine;
    private readonly IDecisionEngine _decisionEngine;
    private readonly IGameCoordinator _gameCoordinator;
    private readonly IPostflopContextHolder _contextHolder;
    private readonly ITableLayoutService _tableLayout;
    private readonly GameLoggerService _gameLogger;
    private readonly IOpponentTracker _opponentTracker;
    private readonly IPokerCalculator _pokerCalculator;
    private readonly IBetSizingService _betSizingService;
    private readonly IOptions<GameLoopOptions> _options;
    private readonly ILogger<HandProcessor> _logger;

    /// <summary>
    /// Inicializa el procesador con todas las dependencias del pipeline.
    /// Las dependencias marcadas como opcionales solo se usan en el pipeline
    /// postflop (futura fase).
    /// </summary>
    public HandProcessor(
        ICaptureService captureService,
        ITableReaderService tableReaderService,
        GameLoopStateMachine stateMachine,
        IDecisionEngine decisionEngine,
        IGameCoordinator? gameCoordinator = null,
        IPostflopContextHolder? contextHolder = null,
        ITableLayoutService? tableLayout = null,
        GameLoggerService? gameLogger = null,
        IOpponentTracker? opponentTracker = null,
        IPokerCalculator? pokerCalculator = null,
        IBetSizingService? betSizingService = null,
        IOptions<GameLoopOptions>? options = null,
        ILogger<HandProcessor>? logger = null)
    {
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _tableReaderService = tableReaderService ?? throw new ArgumentNullException(nameof(tableReaderService));
        _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
        _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
        _gameCoordinator = gameCoordinator!;
        _contextHolder = contextHolder!;
        _tableLayout = tableLayout!;
        _gameLogger = gameLogger!;
        _opponentTracker = opponentTracker!;
        _pokerCalculator = pokerCalculator!;
        _betSizingService = betSizingService!;
        _options = options!;
        _logger = logger!;
    }

    /// <inheritdoc />
    public async Task<GameLoopResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Capturar screenshot
        using var screenshot = _captureService.CaptureWindow();
        if (screenshot is null)
        {
            _logger.LogDebug("HandProcessor: captura nula, retornando Empty");
            return new GameLoopResult { Empty = true };
        }

        // 2. Leer datos de la mesa
        var state = new PlayerGameState();
        ReadTableData(screenshot, state);

        // 3. Determinar si es preflop o postflop
        bool isPreflop = _stateMachine.IsPreflop;

        if (isPreflop)
        {
            return await ProcessPreflopPipelineAsync(screenshot, state, cancellationToken);
        }

        // 4. Postflop: retornar Empty (FrmMain maneja por path legacy)
        _logger.LogDebug("HandProcessor: estado {State} no es preflop, retornando Empty",
            _stateMachine.CurrentState);
        return new GameLoopResult { Empty = true };
    }

    #region Preflop Pipeline

    /// <summary>
    /// Ejecuta el pipeline completo de preflop: lee bote, hole cards,
    /// determina posición, llama al use case de decisión y construye el resultado.
    /// </summary>
    private async Task<GameLoopResult> ProcessPreflopPipelineAsync(
        Bitmap screenshot, PlayerGameState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Leer hole cards con reintentos
        bool hasHoleCards = await TryReadHoleCardsAsync(screenshot, state);
        if (!hasHoleCards)
        {
            _logger.LogError("HandProcessor: hole cards no detectadas tras reintentos");
            return new GameLoopResult { Empty = true };
        }

        // Construir diccionario de posición preflop
        var preflopHeroPosition = GetPreflopHeroPosition(state);

        // Validar posición
        if (state.Position == TablePosition.None)
        {
            _logger.LogError("HandProcessor: posición del jugador no detectada");
            return new GameLoopResult { Empty = true };
        }

        if (!preflopHeroPosition.ContainsKey(state.Position))
        {
            _logger.LogError("HandProcessor: PreflopHeroPosition sin datos para {Position}",
                state.Position);
            return new GameLoopResult { Empty = true };
        }

        // Ejecutar decision preflop por la puerta unica del motor
        var responseAction = new ResponseAction();
        var decision = await _decisionEngine.DecideAsync(
            PlayerStateMapper.MapPreflop(state, responseAction.IsSecondAction, villain: null));

        responseAction.Action = decision.Action;
        if (Enum.TryParse<HandSituation>(decision.Situation, out var nextSituation))
            responseAction.HandSituation = nextSituation;

        var playerState = state.Clone();
        if (decision.Action.Contains("Call"))
            playerState.HandSituation = HandSituation.Call;

        // Marcar aggressors preflop
        SetPreflopAggressors(playerState);

        // Construir GameLoopResult
        return BuildGameLoopResult(playerState, responseAction);
    }

    /// <summary>
    /// Lee las hole cards con reintentos.
    /// </summary>
    private async Task<bool> TryReadHoleCardsAsync(Bitmap screenshot, PlayerGameState state)
    {
        bool hasHoleCards = !string.IsNullOrEmpty(state.HoleCard1Face) &&
                            !string.IsNullOrEmpty(state.HoleCard2Face);

        int maxRetries = 2;
        int retryDelayMs = 200;

        for (int retry = 0; retry < maxRetries && !hasHoleCards; retry++)
        {
            if (retry > 0)
                await Task.Delay(retryDelayMs);

            await _tableReaderService.ObtainCardsPlayerAsync(screenshot, state);

            hasHoleCards = !string.IsNullOrEmpty(state.HoleCard1Face) &&
                           !string.IsNullOrEmpty(state.HoleCard2Face);
        }

        return hasHoleCards;
    }

    /// <summary>
    /// Lee los datos de la mesa: bote, stacks, apuestas de jugadores.
    /// </summary>
    private void ReadTableData(Bitmap screenshot, PlayerGameState state)
    {
        _tableReaderService.SetPotValue(screenshot, state);
        _tableReaderService.SetHeroStack(screenshot, state);
        _tableReaderService.SetBetPlayer(screenshot, state);
    }

    /// <summary>
    /// Marca los jugadores que hicieron raise como aggressors preflop.
    /// </summary>
    private static void SetPreflopAggressors(PlayerGameState state)
    {
        var maxBet = state.Players.Max(p => p.Bet);
        if (maxBet <= 1)
            return;

        var bigBlind = 1m;
        var playersWhoRaised = state.Players
            .Where(p => p.Active && p.Bet > bigBlind)
            .ToList();

        foreach (var player in playersWhoRaised)
        {
            player.WasPreflopAggressor = true;
        }
    }

    /// <summary>
    /// Construye el diccionario de posiciones preflop desde el estado del jugador.
    /// </summary>
    private static Dictionary<TablePosition, Dictionary<TablePosition, decimal>> GetPreflopHeroPosition(
        PlayerGameState state)
    {
        var players = new Dictionary<TablePosition, decimal>();

        foreach (var item in state.Players.Where(p => !p.Empty || !p.SitOut))
        {
            if (item.Position != TablePosition.None)
                players[item.Position] = item.Bet;
        }

        var position = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();

        foreach (var heroPosition in Enum.GetValues(typeof(TablePosition))
                                         .Cast<TablePosition>()
                                         .Where(p => p != TablePosition.None))
        {
            position[heroPosition] = new Dictionary<TablePosition, decimal>(players);
        }

        return position;
    }

    /// <summary>
    /// Construye el <see cref="GameLoopResult"/> a partir del estado y la acción decidida.
    /// </summary>
    private static GameLoopResult BuildGameLoopResult(
        PlayerGameState state, ResponseAction responseAction)
    {
        return new GameLoopResult
        {
            Empty = false,
            PotOdds = null,  // Postflop: proviene de PokerCalculationResult (futura fase)
            Equity = null,   // Postflop: proviene de PokerCalculationResult (futura fase)
            HoleCards = !string.IsNullOrEmpty(state.HoleCard1Face) &&
                        !string.IsNullOrEmpty(state.HoleCard2Face)
                ? (state.HoleCard1Face, state.HoleCard2Face)
                : null,
            BoardCards = state.BoardCards is { Count: > 0 }
                ? state.BoardCards
                    .Where(b => b.Position != BoardPosition.Hand && !string.IsNullOrEmpty(b.Name))
                    .Select(b => b.Name!)
                    .ToList()
                : Array.Empty<string>(),
            PlayerBets = state.Players
                ?.Where(p => !string.IsNullOrEmpty(p.Name))
                .ToDictionary(p => p.Name!, p => p.Bet)
                ?? new Dictionary<string, decimal>(),
            HeroPosition = state.Position,
            HandSituation = responseAction.HandSituation,
            ResponseAction = responseAction.Action,
            TableName = null,
            SummaryText = null
        };
    }

    #endregion
}
