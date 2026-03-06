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
    private GameRound? _currentRound;

    public GameLoggerService(IDocumentStore store, ILogger<GameLoggerService> logger)
    {
        _store = store;
        _logger = logger;
    }

    public void StartNewRound(
        long handNumber,
        string tableName,
        string heroCard1,
        string heroCard2,
        TablePosition heroPosition,
        decimal heroStack,
        int numOpponents)
    {
        // Guardar la ronda anterior si existe
        if (_currentRound != null)
        {
            _ = SaveRoundAsync();
        }

        _currentRound = new GameRound
        {
            HandNumber = handNumber,
            TableName = tableName,
            HeroCard1 = heroCard1,
            HeroCard2 = heroCard2,
            HeroPosition = heroPosition,
            HeroStackStart = heroStack,
            NumOpponents = numOpponents
        };

        _logger.LogInformation(
            "Nueva ronda iniciada: Hand #{HandNumber} en {TableName}",
            handNumber, tableName);
    }

    public void LogStreetDecision(StreetDecision decision)
    {
        if (_currentRound == null)
        {
            _logger.LogWarning("Se intentó registrar decisión sin ronda activa");
            return;
        }

        _currentRound.Decisions.Add(decision);
        _currentRound.LastStreetPlayed = decision.Street;

        _logger.LogInformation(
            "Decisión registrada: {Street} - Equity: {Equity:F1}% - Acción: {Action}",
            decision.Street, decision.EquityPercent, decision.ActionTaken);
    }

    public void UpdateBoard(List<string> flopCards, string? turnCard = null, string? riverCard = null)
    {
        if (_currentRound == null)
            return;

        if (flopCards.Count > 0)
            _currentRound.FlopCards = flopCards;
        if (turnCard != null)
            _currentRound.TurnCard = turnCard;
        if (riverCard != null)
            _currentRound.RiverCard = riverCard;
    }

    public void UpdatePotSize(decimal potSize)
    {
        if (_currentRound != null)
            _currentRound.PotSizeFinal = potSize;
    }

    public async Task SaveRoundAsync()
    {
        if (_currentRound == null)
            return;

        try
        {
            await using var session = _store.LightweightSession();
            session.Store(_currentRound);
            await session.SaveChangesAsync();

            _logger.LogInformation(
                "Ronda guardada: Hand #{HandNumber}, {DecisionCount} decisiones",
                _currentRound.HandNumber, _currentRound.Decisions.Count);

            _currentRound = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar ronda: Hand #{HandNumber}",
                _currentRound?.HandNumber);
        }
    }

    public async Task<List<GameRound>> GetRecentRoundsAsync(int count = 50)
    {
        await using var session = _store.QuerySession();
        var results = await session.Query<GameRound>()
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync();
        return results.ToList();
    }

    public bool HasActiveRound => _currentRound != null;
}
