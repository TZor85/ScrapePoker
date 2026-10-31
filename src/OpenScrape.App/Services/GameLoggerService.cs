using Microsoft.Extensions.Logging;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Agregador en memoria de la sesión y la mano en curso. Ya no persiste nada: el
/// historial se retiró y lo único que importa conservar es el bucket de métricas, que
/// se cierra con <see cref="IMetricsCollector.EndHand"/> al finalizar cada mano.
/// </summary>
public class GameLoggerService : IGameLoggerService
{
    // Máximo de manos que se mantienen en la lista en memoria por sesión.
    private const int MaxHandsInMemory = 20;

    private readonly ILogger<GameLoggerService> _logger;
    private readonly IMetricsCollector _metrics;
    private GameSession? _currentSession;
    private HandRecord? _currentHand;
    private IDisposable? _sessionScope;
    private IDisposable? _handScope;

    // Acumulador de sesión: la lista Hands en memoria está truncada, así que no sirve
    // para saber si la mano actual es la primera de la sesión.
    private int _sessionTotalHands;

    /// <summary>Big blind de la sesión activa (para calcular ciegas pagadas).</summary>
    public decimal CurrentBigBlind => _currentSession?.BigBlind ?? 0.50m;

    public GameLoggerService(ILogger<GameLoggerService> logger, IMetricsCollector metrics)
    {
        _logger = logger;
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <summary>
    /// Inicia o reanuda una sesión para la mesa dada.
    /// Si ya hay una sesión activa para la misma mesa, la reutiliza.
    /// Si hay una sesión anterior distinta, la cierra antes de cambiar.
    /// </summary>
    public async Task StartSessionAsync(string sessionId, string tableName, decimal bigBlind = 0.50m)
    {
        if (_currentSession != null && _currentSession.SessionId == sessionId)
            return; // Misma sesión, no reiniciar

        if (_currentSession != null)
        {
            try { await SaveSessionAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error cerrando la sesión anterior al cambiar de mesa"); }
        }

        _currentSession = new GameSession
        {
            SessionId = sessionId,
            TableName = tableName,
            BigBlind = bigBlind
        };

        _sessionTotalHands = 0;

        // Correlation scope: todos los logs siguientes incluirán SessionId y TableName.
        _sessionScope?.Dispose();
        _sessionScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["SessionId"] = sessionId,
            ["TableName"] = tableName
        });

        _logger.LogInformation(
            "Sesión iniciada: {SessionId} en {TableName}",
            sessionId, tableName);
    }

    /// <summary>
    /// Inicia una nueva mano dentro de la sesión activa.
    /// </summary>
    public async Task StartNewHandAsync(
        long handNumber,
        string heroCard1,
        string heroCard2,
        TablePosition heroPosition,
        decimal heroStack,
        int numOpponents,
        decimal blindPosted = 0)
    {
        if (_currentSession == null)
        {
            _logger.LogWarning("Se intentó iniciar mano sin sesión activa");
            return;
        }

        // Cerrar la mano anterior si quedó abierta
        if (_currentHand != null)
            await SaveSessionAsync();

        _currentHand = new HandRecord
        {
            HandNumber = handNumber,
            HeroCard1 = heroCard1,
            HeroCard2 = heroCard2,
            HeroPosition = heroPosition,
            HeroStackStart = _sessionTotalHands == 0 && heroStack == 0 ? 100 : heroStack,
            NumOpponents = numOpponents,
            BlindPosted = blindPosted
        };

        _metrics.StartHand(handNumber.ToString());

        // Correlation scope: HandNumber se propaga hasta que la mano se cierra.
        _handScope?.Dispose();
        _handScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["HandNumber"] = handNumber
        });

        _logger.LogInformation(
            "Nueva mano iniciada: Hand #{HandNumber}",
            handNumber);
    }

    public void LogStreetDecision(StreetDecision decision)
    {
        if (_currentHand == null)
        {
            _logger.LogWarning("Se intentó registrar decisión sin mano activa");
            return;
        }

        _currentHand.Decisions.Add(decision);
        _currentHand.LastStreetPlayed = decision.Street;
    }

    public void UpdateBoard(List<string> flopCards, string? turnCard = null, string? riverCard = null)
    {
        if (_currentHand == null)
            return;

        if (flopCards.Count > 0)
            _currentHand.FlopCards = flopCards;
        if (turnCard != null)
            _currentHand.TurnCard = turnCard;
        if (riverCard != null)
            _currentHand.RiverCard = riverCard;
    }

    public void UpdatePotSize(decimal potSize)
    {
        if (_currentHand != null)
            _currentHand.PotSizeFinal = potSize;
    }

    public void UpdateSituation(HandSituation situation)
    {
        if (_currentHand != null)
            _currentHand.Situation = situation;
    }

    public void RegisterAutoRebuy(decimal amount)
    {
        if (_currentHand != null)
        {
            _currentHand.AutoRebuy = amount;
            _logger.LogInformation("Auto-rebuy registrado: {Amount}BB en mano {HandNumber}", amount, _currentHand.HandNumber);
        }
    }

    /// <summary>
    /// Asigna telemetría a la mano actual (se conserva en el modelo en memoria).
    /// </summary>
    public void SetTelemetry(TelemetryAggregate telemetry)
    {
        if (_currentHand != null)
        {
            _currentHand.Telemetry = telemetry;
        }
    }

    /// <summary>
    /// Finaliza la mano registrando el stack final y calculando el resultado.
    /// </summary>
    public void EndHand(decimal heroStackEnd)
    {
        if (_currentHand == null) return;

        _currentHand.HeroStackEnd = heroStackEnd;

        decimal diff = heroStackEnd - _currentHand.HeroStackStart;
        _currentHand.Result = diff switch
        {
            > 0 => HandResult.Won,
            < 0 => HandResult.Lost,
            _ => HandResult.Push,
        };

        _logger.LogInformation(
            "Mano finalizada: Hand #{HandNumber}, Result={Result}, Diff={Diff:+0.00;-0.00}",
            _currentHand.HandNumber, _currentHand.Result, diff);
    }

    /// <summary>
    /// Cierra la mano actual: vuelca su bucket de métricas, la agrega a la lista en
    /// memoria (truncada a <see cref="MaxHandsInMemory"/>) y libera el scope de log.
    /// </summary>
    private void FinalizeHand()
    {
        if (_currentHand == null || _currentSession == null) return;

        _currentHand.GameSessionId = _currentSession.Id;
        _currentSession.EndTime = DateTime.UtcNow;

        // Telemetry de la mano antes de cerrarla en el recolector
        _currentHand.Telemetry = _metrics.EndHand();

        _sessionTotalHands++;

        _currentSession.Hands.Add(_currentHand);
        if (_currentSession.Hands.Count > MaxHandsInMemory)
            _currentSession.Hands.RemoveAt(0);

        _currentHand = null;
        _handScope?.Dispose();
        _handScope = null;
    }

    /// <summary>
    /// Cierra la mano en curso, si hay, y deja la sesión lista para cerrarse.
    /// No escribe nada: el historial dejó de persistirse.
    /// </summary>
    public Task SaveSessionAsync()
    {
        if (_currentSession == null)
            return Task.CompletedTask;

        if (_currentHand != null)
            FinalizeHand();

        _logger.LogInformation(
            "Sesión cerrada en memoria: {SessionId}, {HandCount} manos acumuladas",
            _currentSession.SessionId, _sessionTotalHands);

        return Task.CompletedTask;
    }

    public bool HasActiveSession => _currentSession != null;
    public bool HasActiveHand => _currentHand != null;
}
