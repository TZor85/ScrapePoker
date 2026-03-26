using Marten;
using Microsoft.Extensions.Logging;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

public class GameLoggerService
{
    // Máximo de manos que se mantienen en la lista en memoria por sesión.
    // Las manos anteriores ya están persistidas como documentos HandRecord individuales.
    private const int MaxHandsInMemory = 20;

    private readonly IDocumentStore _store;
    private readonly ILogger<GameLoggerService> _logger;
    private GameSession? _currentSession;
    private HandRecord? _currentHand;

    // Acumuladores de sesión: necesarios porque la lista Hands en memoria está truncada
    private int _sessionTotalHands;
    private decimal _sessionTotalProfit;

    /// <summary>Big blind de la sesión activa (para calcular ciegas pagadas).</summary>
    public decimal CurrentBigBlind => _currentSession?.BigBlind ?? 0.50m;

    public GameLoggerService(IDocumentStore store, ILogger<GameLoggerService> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Inicia o reanuda una sesión para la mesa dada.
    /// Si ya hay una sesión activa para la misma mesa, la reutiliza.
    /// Si hay una sesión anterior distinta, la guarda antes de cambiar.
    /// </summary>
    public async Task StartSessionAsync(string sessionId, string tableName, decimal bigBlind = 0.50m)
    {
        if (_currentSession != null && _currentSession.SessionId == sessionId)
            return; // Misma sesión, no reiniciar

        // Guardar sesión anterior con await — evita pérdida silenciosa de datos
        if (_currentSession != null)
        {
            try { await SaveSessionAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Error guardando sesión anterior al cambiar de mesa"); }
        }

        _currentSession = new GameSession
        {
            SessionId = sessionId,
            TableName = tableName,
            BigBlind = bigBlind
        };

        // Resetear acumuladores para la nueva sesión
        _sessionTotalHands = 0;
        _sessionTotalProfit = 0;

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

        // Finalizar y persistir mano anterior si existe
        if (_currentHand != null)
            await FinalizeAndPersistHandAsync();

        _currentHand = new HandRecord
        {
            HandNumber = handNumber,
            HeroCard1 = heroCard1,
            HeroCard2 = heroCard2,
            HeroPosition = heroPosition,
            HeroStackStart = heroStack,
            NumOpponents = numOpponents,
            BlindPosted = blindPosted
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
    /// Agrega la mano actual a la sesión en memoria y la persiste como documento individual.
    /// Mantiene solo las últimas MaxHandsInMemory manos en la lista para controlar el consumo de RAM.
    /// </summary>
    private async Task FinalizeAndPersistHandAsync()
    {
        if (_currentHand == null || _currentSession == null) return;

        _currentHand.GameSessionId = _currentSession.Id;
        _currentSession.EndTime = DateTime.UtcNow;

        // Actualizar acumuladores antes de truncar
        _sessionTotalHands++;
        if (_currentHand.Result != HandResult.Unknown)
            _sessionTotalProfit += _currentHand.HeroStackEnd - _currentHand.HeroStackStart;

        // Agregar a la lista en memoria y mantener solo las últimas N manos
        _currentSession.Hands.Add(_currentHand);
        if (_currentSession.Hands.Count > MaxHandsInMemory)
            _currentSession.Hands.RemoveAt(0);

        // Persistir la mano como documento Marten independiente
        try
        {
            await using var session = _store.LightweightSession();
            session.Store(_currentHand);
            await session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al persistir HandRecord #{HandNumber}", _currentHand.HandNumber);
        }

        _currentHand = null;
    }

    /// <summary>
    /// Guarda la sesión actual en la base de datos.
    /// Si hay una mano en progreso, la persiste primero como documento HandRecord individual.
    /// Se llama después de cada mano finalizada para actualizar EndTime y metadata.
    /// </summary>
    public async Task SaveSessionAsync()
    {
        if (_currentSession == null)
            return;

        // Finalizar y persistir mano en progreso si existe
        if (_currentHand != null)
            await FinalizeAndPersistHandAsync();

        try
        {
            await using var session = _store.LightweightSession();
            session.Store(_currentSession);
            await session.SaveChangesAsync();

            _logger.LogInformation(
                "Sesión guardada: {SessionId}, {HandCount} manos acumuladas",
                _currentSession.SessionId, _sessionTotalHands);
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
    /// Extrae las manos más recientes consultando directamente la colección HandRecord.
    /// </summary>
    public async Task<List<HandRecord>> GetRecentHandsAsync(int maxHands = 500)
    {
        await using var session = _store.QuerySession();
        var results = await session.Query<HandRecord>()
            .OrderByDescending(h => h.Timestamp)
            .Take(maxHands)
            .ToListAsync();
        return results.ToList();
    }

    /// <summary>
    /// Obtiene todas las manos de una sesión específica.
    /// </summary>
    public async Task<List<HandRecord>> GetHandsForSessionAsync(string gameSessionId)
    {
        await using var session = _store.QuerySession();
        var results = await session.Query<HandRecord>()
            .Where(h => h.GameSessionId == gameSessionId)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();
        return results.ToList();
    }

    /// <summary>
    /// Obtiene las sesiones recientes con estadísticas agregadas calculadas desde HandRecord.
    /// </summary>
    public async Task<List<SessionStatsDto>> GetRecentSessionsWithStatsAsync(int count = 50)
    {
        await using var session = _store.QuerySession();
        var sessions = await session.Query<GameSession>()
            .OrderByDescending(s => s.EndTime)
            .Take(count)
            .ToListAsync();

        var result = new List<SessionStatsDto>();
        foreach (var gs in sessions)
        {
            var hands = await session.Query<HandRecord>()
                .Where(h => h.GameSessionId == gs.Id)
                .ToListAsync();

            int totalHands = hands.Count;
            decimal totalProfit = hands
                .Where(h => h.Result != HandResult.Unknown)
                .Sum(h => h.HeroStackEnd - h.HeroStackStart);
            double bbPer100 = totalHands > 0 && gs.BigBlind > 0
                ? (double)(totalProfit / gs.BigBlind) / totalHands * 100
                : 0;

            result.Add(new SessionStatsDto(
                gs.Id, gs.SessionId, gs.TableName,
                gs.StartTime, gs.EndTime, gs.BigBlind,
                totalHands, totalProfit, bbPer100));
        }
        return result;
    }

    public bool HasActiveSession => _currentSession != null;
    public bool HasActiveHand => _currentHand != null;

    /// <summary>Total acumulado de manos en la sesión (incluye las ya desalojadas de memoria).</summary>
    public int CurrentSessionHandCount => _sessionTotalHands;

    /// <summary>Profit acumulado de la sesión actual (incluye manos ya desalojadas de memoria).</summary>
    public decimal CurrentSessionProfit => _sessionTotalProfit;
}
