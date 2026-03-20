using Marten;
using Microsoft.Extensions.Logging;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

public class GameLoggerService
{
    private readonly IDocumentStore _store;
    private readonly ILogger<GameLoggerService> _logger;
    private GameSession? _currentSession;
    private HandRecord? _currentHand;

    public GameLoggerService(IDocumentStore store, ILogger<GameLoggerService> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Inicia o reanuda una sesión para la mesa dada.
    /// Si ya hay una sesión activa para la misma mesa, la reutiliza.
    /// </summary>
    public void StartSession(string sessionId, string tableName, decimal bigBlind = 0.50m)
    {
        if (_currentSession != null && _currentSession.SessionId == sessionId)
            return; // Misma sesión, no reiniciar

        // Guardar sesión anterior si existe (fire-and-forget con logging de errores)
        if (_currentSession != null)
            _ = Task.Run(async () =>
            {
                try { await SaveSessionAsync(); }
                catch (Exception ex) { _logger.LogError(ex, "Error crítico guardando sesión anterior"); }
            });

        _currentSession = new GameSession
        {
            SessionId = sessionId,
            TableName = tableName,
            BigBlind = bigBlind
        };

        _logger.LogInformation(
            "Sesión iniciada: {SessionId} en {TableName}",
            sessionId, tableName);
    }

    /// <summary>
    /// Inicia una nueva mano dentro de la sesión activa.
    /// </summary>
    public void StartNewHand(
        long handNumber,
        string heroCard1,
        string heroCard2,
        TablePosition heroPosition,
        decimal heroStack,
        int numOpponents)
    {
        if (_currentSession == null)
        {
            _logger.LogWarning("Se intentó iniciar mano sin sesión activa");
            return;
        }

        // Finalizar mano anterior si existe
        if (_currentHand != null)
            FinalizeCurrentHand();

        _currentHand = new HandRecord
        {
            HandNumber = handNumber,
            HeroCard1 = heroCard1,
            HeroCard2 = heroCard2,
            HeroPosition = heroPosition,
            HeroStackStart = heroStack,
            NumOpponents = numOpponents
        };

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
    /// Agrega la mano actual a la sesión y persiste.
    /// </summary>
    private void FinalizeCurrentHand()
    {
        if (_currentHand == null || _currentSession == null) return;

        _currentSession.Hands.Add(_currentHand);
        _currentSession.EndTime = DateTime.UtcNow;
        _currentHand = null;
    }

    /// <summary>
    /// Guarda la sesión actual (con todas sus manos) en la base de datos.
    /// Se llama después de cada mano finalizada para no perder datos.
    /// </summary>
    public async Task SaveSessionAsync()
    {
        if (_currentSession == null)
            return;

        // Finalizar mano en progreso si existe
        FinalizeCurrentHand();

        try
        {
            await using var session = _store.LightweightSession();
            session.Store(_currentSession);
            await session.SaveChangesAsync();

            _logger.LogInformation(
                "Sesión guardada: {SessionId}, {HandCount} manos",
                _currentSession.SessionId, _currentSession.Hands.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar sesión: {SessionId}",
                _currentSession?.SessionId);
        }
    }

    /// <summary>
    /// Obtiene las sesiones recientes.
    /// </summary>
    public async Task<List<GameSession>> GetRecentSessionsAsync(int count = 20)
    {
        await using var session = _store.QuerySession();
        var results = await session.Query<GameSession>()
            .OrderByDescending(s => s.EndTime)
            .Take(count)
            .ToListAsync();
        return results.ToList();
    }

    /// <summary>
    /// Extrae todas las manos de las sesiones para análisis.
    /// </summary>
    public async Task<List<HandRecord>> GetRecentHandsAsync(int maxHands = 500)
    {
        var sessions = await GetRecentSessionsAsync(50);
        return sessions
            .SelectMany(s => s.Hands)
            .OrderByDescending(h => h.Timestamp)
            .Take(maxHands)
            .ToList();
    }

    public bool HasActiveSession => _currentSession != null;
    public bool HasActiveHand => _currentHand != null;
    public int CurrentSessionHandCount => _currentSession?.Hands.Count ?? 0;
}
