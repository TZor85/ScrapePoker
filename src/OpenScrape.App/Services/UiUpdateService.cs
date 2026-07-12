using System.ComponentModel;
using System.Drawing;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación de <see cref="IUiUpdateService"/> para WinForms.
/// Actualiza los controles del formulario con los resultados del pipeline
/// de decisión. Siempre se invoca desde el thread de UI.
/// </summary>
public sealed class UiUpdateService : IUiUpdateService
{
    private readonly IPokerCalculator _pokerCalculator;
    private readonly GameLoggerService _gameLoggerService;
    private readonly IActionFormatter _actionFormatter;
    private readonly ImageCropperService _imageCropperService;
    private readonly IGameCoordinator _coordinator;
    private readonly GameLoopStateMachine _gameLoopStateMachine;
    private readonly ILogger<UiUpdateService> _logger;

    private ISynchronizeInvoke? _syncTarget;
    private IFrmOverlay? _overlay;
    private UiUpdateControls? _controls;
    private List<Card> _cards = new();
    private bool _disposed;

    public UiUpdateService(
        IPokerCalculator pokerCalculator,
        GameLoggerService gameLoggerService,
        IActionFormatter actionFormatter,
        ImageCropperService imageCropperService,
        IGameCoordinator coordinator,
        GameLoopStateMachine gameLoopStateMachine,
        ILogger<UiUpdateService> logger)
    {
        _pokerCalculator = pokerCalculator ?? throw new ArgumentNullException(nameof(pokerCalculator));
        _gameLoggerService = gameLoggerService ?? throw new ArgumentNullException(nameof(gameLoggerService));
        _actionFormatter = actionFormatter ?? throw new ArgumentNullException(nameof(actionFormatter));
        _imageCropperService = imageCropperService ?? throw new ArgumentNullException(nameof(imageCropperService));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _gameLoopStateMachine = gameLoopStateMachine ?? throw new ArgumentNullException(nameof(gameLoopStateMachine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Attach(ISynchronizeInvoke syncTarget, IFrmOverlay overlay, UiUpdateControls controls)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _syncTarget = syncTarget ?? throw new ArgumentNullException(nameof(syncTarget));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _controls = controls ?? throw new ArgumentNullException(nameof(controls));
    }

    public void Detach()
    {
        _syncTarget = null;
        _overlay = null;
        _controls = null;
    }

    public void SetCardList(List<Card> cards)
    {
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
    }

    public void UpdateResults(PlayerGameState state, PokerCalculationResult result, string? responseAction, string tableHand, string tableName)
    {
        var controls = _controls;
        if (controls is null) return;

        if (state != null)
        {
            if (_gameLoopStateMachine.IsPreflop)
            {
                UpdateResumeTextForPreflop(state, result, responseAction, tableHand);
            }

            if (_gameLoopStateMachine.IsFlop)
            {
                UpdateResumeTextForFlop(state, controls);
            }

            UpdatePlayerIndicators(state, controls, tableHand, tableName);
            UpdateCardImages(state, controls);
        }

        controls.LbAction.Text = responseAction ?? string.Empty;
        controls.LbPositionAction.Text = state?.HandSituation.ToString() ?? string.Empty;

        var overlay = _overlay;
        if (overlay != null)
        {
            overlay.UpdateSituacion(controls.LbPositionAction.Text);

            var actionText = responseAction ?? string.Empty;
            var villains = state?.Players.Where(p => p.Name != "P0").ToList() ?? new();
            overlay.UpdateAction(_actionFormatter.EnrichActionWithBBAmount(actionText, villains, _gameLoggerService.CurrentBigBlind));
        }
    }

    /// <summary>
    /// Actualiza la UI desde un <see cref="GameLoopResult"/> (ruta del coordinator).
    /// Convierte los campos del result a los objetos necesarios para UpdateResults.
    /// </summary>
    public void UpdateResults(GameLoopResult result)
    {
        if (result.Empty || result.Error is not null)
            return;

        // Construir synthetic PokerCalculationResult desde GameLoopResult
        var calcResult = new PokerCalculationResult
        {
            PotOddsPercentage = (double)(result.PotOdds ?? 0),
            EquityPercentage = result.Equity ?? 0,
            RecommendedAction = result.RecommendedAction ?? string.Empty,
            Street = result.Street?.ToString() ?? string.Empty,
        };

        // Construir synthetic PlayerGameState desde GameLoopResult
        var state = new PlayerGameState
        {
            HandSituation = result.HandSituation ?? HandSituation.None,
            Position = result.HeroPosition ?? TablePosition.None,
        };
        if (result.HoleCards is var (hc1, hc2))
        {
            state.HoleCard1Face = hc1;
            state.HoleCard2Face = hc2;
        }
        foreach (var bet in result.PlayerBets)
        {
            state.Players.Add(new Player { Name = bet.Key, Bet = bet.Value, Active = true, Empty = false });
        }

        UpdateResults(state, calcResult, result.ResponseAction, string.Empty, result.TableName ?? string.Empty);
    }

    public void UpdateOverlay(PokerCalculationResult result)
    {
        _overlay?.UpdateWithCalculationResult(result);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
    }

    // ===================================================================
    // Private methods — extraídos de FrmMain (misma lógica, sin cambios)
    // ===================================================================

    private void UpdateResumeTextForPreflop(PlayerGameState state, PokerCalculationResult potOddsResult, string? responseAction, string tableHand)
    {
        var controls = _controls;
        if (controls is null) return;

        var enMesa = state.Players.Count(e => !e.Empty) + 1;
        var sitout = state.Players.Count(s => s.SitOut);
        var playing = state.Players.Count(p => p.Active) + 1;

        // Calcular equity preflop si hay hole cards
        double preflopEquity = 0;
        if (state.HoleCard1Rank > 0 && state.HoleCard2Rank > 0)
        {
            try
            {
                var heroCards = new List<CardDataOuts>
                {
                    new((Suit)state.HoleCard1Suit, (Rank)state.HoleCard1Rank),
                    new((Suit)state.HoleCard2Suit, (Rank)state.HoleCard2Rank)
                };
                int voluntaryBettors = state.Players.Count(p => p.Active && p.Bet > 1m);
                var numOpp = Math.Max(1, voluntaryBettors);
                var preflopResult = _pokerCalculator.Calculate(
                    heroCards, new List<CardDataOuts>(),
                    state.PotSize,
                    state.Players.Max(m => m.Bet),
                    numOpponents: numOpp,
                    heroStack: state.HeroStack,
                    villainStack: _coordinator.GetVillainStack(state),
                    handSituation: state.HandSituation.ToString(),
                    opponentProfile: _coordinator.GetActiveVillainProfile(state));
                preflopEquity = preflopResult.EquityPercentage;

                // Ajuste posicional: posiciones tardías tienen ventaja de información
                double positionAdjust = state.Position switch
                {
                    TablePosition.Button => 3.0,
                    TablePosition.CutOff => 1.0,
                    TablePosition.Middle => 0.0,
                    TablePosition.Early => -2.0,
                    TablePosition.SmallBlind => -3.0,
                    TablePosition.BigBlind => -1.0,
                    _ => 0.0
                };
                preflopEquity = Math.Max(0, Math.Min(100, preflopEquity + positionAdjust));
            }
            catch { /* OCR puede dar valores inválidos */ }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Hand #{tableHand}: Hold'em No Limit");
        sb.AppendLine($"#{state.Players.FirstOrDefault(d => d.Dealer)?.Name ?? "Hero"} is the Dealer");
        sb.AppendLine($"{state.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "Hero"}: posts small blind");
        sb.AppendLine($"{state.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "Hero"}: posts big blind");
        sb.AppendLine($"Pot: {state.PotSize}");
        sb.AppendLine($"*** STATISTICS ***");
        sb.AppendLine($"PotOdds: {(decimal)potOddsResult.PotOddsPercentage}%");
        sb.AppendLine($"Equity: {preflopEquity:F1}%");
        sb.AppendLine($"Should Call: {potOddsResult.ShouldCall}");
        sb.AppendLine("*** HOLE CARDS ***");
        sb.AppendLine($"Dealt to Hero [{state.HoleCard1Face} {state.HoleCard2Face}]");

        foreach (TablePosition position in Enum.GetValues(typeof(TablePosition)))
        {
            if (position != TablePosition.None && position <= state.Position)
            {
                var player = state.Players.FirstOrDefault(f => f.Position == position);
                string name = player?.Name ?? "Hero";
                string action = string.Empty;

                if (name != "Hero")
                    action = player?.Bet == null ? "folds" : $"bets/calls {player.Bet}";
                else
                {
                    if (position == state.Position)
                    {
                        sb.AppendLine($"Hand Situation: {state.HandSituation}");
                        action = responseAction ?? string.Empty;
                    }
                }

                if (!string.IsNullOrWhiteSpace(action))
                    sb.AppendLine($"{name}: {action}");
            }
        }

        // Siempre mostrar la acción recomendada de Hero al final
        var heroAction = responseAction ?? "No action";
        var heroSituation = state.HandSituation;
        sb.AppendLine($"*** HERO ACTION ***");
        sb.AppendLine($"Position: {state.Position}  |  Situation: {heroSituation}");
        sb.AppendLine($"Equity preflop: {preflopEquity:F1}%");

        var villainsSnapshot = state.Players.Where(p => p.Name != "P0").ToList();
        var displayAction = _actionFormatter.EnrichActionWithBBAmount(heroAction, villainsSnapshot, _gameLoggerService.CurrentBigBlind);
        sb.AppendLine($"▶ DECISIÓN: {displayAction}");

        controls.TbResume.AppendText(sb.ToString() + Environment.NewLine);
    }

    private static void UpdateResumeTextForFlop(PlayerGameState state, UiUpdateControls controls)
    {
        controls.TbResume.Text += "*** FLOP *** [";
        var countFlop = 0;
        foreach (var carta in state.BoardCards.Where(w => w.Position == BoardPosition.Flop))
        {
            countFlop++;
            if (countFlop == 3)
                controls.TbResume.Text += $"{carta.Name}]";
            else
                controls.TbResume.Text += $"{carta.Name} ";
        }
    }

    private static void UpdatePlayerIndicators(PlayerGameState state, UiUpdateControls controls, string tableHand, string tableName)
    {
        // Resetear colores de botones
        controls.PbButtonPlayerOne.BackColor = Color.Transparent;
        controls.PbButtonPlayerTwo.BackColor = Color.Transparent;
        controls.PbButtonPlayerThree.BackColor = Color.Transparent;
        controls.PbButtonPlayerFour.BackColor = Color.Transparent;
        controls.PbButtonPlayerFive.BackColor = Color.Transparent;
        controls.PbButtonHero.BackColor = Color.Transparent;

        // Actualizar nombres y apuestas
        controls.LbNamePlayerOne.Text = state.Players.FirstOrDefault(f => f.Name == "P1")?.Alias;
        controls.LbNamePlayerTwo.Text = state.Players.FirstOrDefault(f => f.Name == "P2")?.Alias;
        controls.LbNamePlayerThree.Text = state.Players.FirstOrDefault(f => f.Name == "P3")?.Alias;
        controls.LbNamePlayerFour.Text = state.Players.FirstOrDefault(f => f.Name == "P4")?.Alias;
        controls.LbNamePlayerFive.Text = state.Players.FirstOrDefault(f => f.Name == "P5")?.Alias;

        controls.LbBetPlayerOne.Text = state.Players.FirstOrDefault(f => f.Name == "P1")?.Bet.ToString();
        controls.LbBetPlayerTwo.Text = state.Players.FirstOrDefault(f => f.Name == "P2")?.Bet.ToString();
        controls.LbBetPlayerThree.Text = state.Players.FirstOrDefault(f => f.Name == "P3")?.Bet.ToString();
        controls.LbBetPlayerFour.Text = state.Players.FirstOrDefault(f => f.Name == "P4")?.Bet.ToString();
        controls.LbBetPlayerFive.Text = state.Players.FirstOrDefault(f => f.Name == "P5")?.Bet.ToString();

        controls.LbBetHero.Text = state.CurrentBet.ToString();
        controls.LbPot.Text = state.PotSize.ToString();

        // Marcar el dealer
        if (state.Players.FirstOrDefault(f => f.Name == "P1")?.Dealer == true)
            controls.PbButtonPlayerOne.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P2")?.Dealer == true)
            controls.PbButtonPlayerTwo.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P3")?.Dealer == true)
            controls.PbButtonPlayerThree.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P4")?.Dealer == true)
            controls.PbButtonPlayerFour.BackColor = Color.Red;
        if (state.Players.FirstOrDefault(f => f.Name == "P5")?.Dealer == true)
            controls.PbButtonPlayerFive.BackColor = Color.Red;
        if (state.IsDealer == true)
            controls.PbButtonHero.BackColor = Color.Red;

        controls.LbTableHand.Text = tableHand;
        controls.LbTableName.Text = tableName;
    }

    private void UpdateCardImages(PlayerGameState state, UiUpdateControls controls)
    {
        // Cartas del héroe
        controls.PbHeroCard0.Image = GetCardImage(state.HoleCard1Face);
        controls.PbHeroCard1.Image = GetCardImage(state.HoleCard2Face);

        // Cartas del tablero
        controls.PbBoard1.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 1)?.Name);
        controls.PbBoard2.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 2)?.Name);
        controls.PbBoard3.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 3)?.Name);
        controls.PbBoard4.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 4)?.Name);
        controls.PbBoard5.Image = GetCardImage(state.BoardCards.FirstOrDefault(f => f.Location == 5)?.Name);
    }

    private Image? GetCardImage(string? cardName)
    {
        if (string.IsNullOrEmpty(cardName))
            return null;

        var card = _cards.FirstOrDefault(x => x.Id.Contains(cardName));
        return card != null ? _imageCropperService.Base64ToImage(card.ImageBase64) : null;
    }
}
