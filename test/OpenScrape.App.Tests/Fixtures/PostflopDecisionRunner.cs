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
        var calculator = GameCoordinatorFixtureBuilder.CreateCalculator(profile, monteCarloSeed, calculationLogger);
        var community = ToCommunityCards(state);

        PokerCalculationResult Calculate(int take) => calculator.Calculate(
            ToCards(state.HoleCard1Suit, state.HoleCard1Rank, state.HoleCard2Suit, state.HoleCard2Rank),
            community.Take(take).ToList(),
            state.PotSize,
            maxBet,
            isInPosition: state.IsInPosition,
            heroStack: state.HeroStack,
            villainStack: coordinator.GetVillainStack(state),
            handSituation: state.HandSituation.ToString(),
            opponentProfile: null);

        var flopResult = Calculate(3);
        coordinator.SetFlopResult(flopResult);
        var flopDecision = await coordinator.DetermineFlopAction(state);
        if (street == BoardPosition.Flop)
            return ToSnapshot(flopDecision, flopResult);

        var turnResult = Calculate(4);
        coordinator.SetTurnResult(turnResult);
        coordinator.TurnBoardTexture = coordinator.AnalyzeTurnBoardTexture(state.BoardCards);
        var turnDecision = await coordinator.DetermineTurnAction(state);
        if (street == BoardPosition.Turn)
            return ToSnapshot(turnDecision, turnResult);

        var riverResult = Calculate(5);
        coordinator.SetRiverResult(riverResult);
        coordinator.RiverBoardTexture = coordinator.AnalyzeRiverBoardTexture(state.BoardCards);
        var riverDecision = await coordinator.DetermineRiverAction(state);

        return ToSnapshot(riverDecision, riverResult);
    }

    private static PostflopDecisionSnapshot ToSnapshot(
        GameDecisionResult decision,
        PokerCalculationResult result) => new(
            decision.Action,
            result.EquityPercentage,
            decision.Decision?.Reason,
            decision.BoardTexture,
            decision.Decision?.IsBluff ?? false,
            decision.Decision?.IsBarrel ?? false,
            decision.Decision?.IsCheckRaise ?? false);

    private static List<CardDataOuts> ToCards(int suit1, int rank1, int suit2, int rank2) =>
    [
        new((Suit)suit1, (Rank)rank1),
        new((Suit)suit2, (Rank)rank2)
    ];

    private static List<CardDataOuts> ToCommunityCards(PlayerGameState state) =>
        state.BoardCards
            .Where(b => b.Position != BoardPosition.Hand)
            .OrderBy(b => b.Location)
            .Select(b => new CardDataOuts((Suit)b.Suit, (Rank)b.Force))
            .ToList();
}
