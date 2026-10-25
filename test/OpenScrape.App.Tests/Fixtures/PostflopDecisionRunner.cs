using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Foto de una decisión postflop: todo lo que el motor devuelve y que la migración
/// no puede cambiar sin evidencia.
/// </summary>
internal sealed record PostflopDecisionSnapshot(
    string Action,
    double Equity,
    string? Reason,
    string? BoardTexture,
    bool IsBluff,
    bool IsBarrel,
    bool IsCheckRaise);

/// <summary>
/// Ejecuta el pipeline postflop actual (equity + coordinador) para un estado de mesa.
/// Lo comparten el generador de fixtures y el replay: lo que se congela es exactamente
/// lo que después se verifica.
/// </summary>
internal static class PostflopDecisionRunner
{
    public static async Task<(string Action, double Equity)> Run(
        StrategyProfile profile,
        PlayerGameState state,
        decimal maxBet,
        int monteCarloSeed,
        IGameLoggerService? logger = null,
        ILogger<UnifiedPokerCalculator>? calculationLogger = null,
        IDecisionEngine? engine = null)
    {
        var snapshot = await RunStreet(
            profile, state, maxBet, monteCarloSeed, BoardPosition.Flop, logger, calculationLogger, engine);

        return (snapshot.Action, snapshot.Equity);
    }

    /// <summary>
    /// Corre la secuencia completa hasta la calle pedida. Las calles previas se ejecutan
    /// igual que en producción porque el coordinador arrastra contexto entre ellas.
    /// </summary>
    public static async Task<PostflopDecisionSnapshot> RunStreet(
        StrategyProfile profile,
        PlayerGameState state,
        decimal maxBet,
        int monteCarloSeed,
        BoardPosition street,
        IGameLoggerService? logger = null,
        ILogger<UnifiedPokerCalculator>? calculationLogger = null,
        IDecisionEngine? engine = null)
    {
        var coordinator = GameCoordinatorFixtureBuilder.Create(profile, logger ?? new NoOpGameLogger(), engine, monteCarloSeed);

        var flopDecision = await coordinator.DetermineFlopAction(state);
        if (street == BoardPosition.Flop)
            return ToSnapshot(flopDecision);

        var turnDecision = await coordinator.DetermineTurnAction(state);
        if (street == BoardPosition.Turn)
            return ToSnapshot(turnDecision);

        var riverDecision = await coordinator.DetermineRiverAction(state);

        return ToSnapshot(riverDecision);
    }

    private static PostflopDecisionSnapshot ToSnapshot(GameDecisionResult decision) => new(
        decision.Action,
        decision.Decision.RawEquity ?? decision.Decision.Equity,
        decision.Decision.Reason,
        decision.BoardTexture,
        decision.Decision.IsBluff,
        decision.Decision.IsBarrel,
        decision.Decision.IsCheckRaise);
}
