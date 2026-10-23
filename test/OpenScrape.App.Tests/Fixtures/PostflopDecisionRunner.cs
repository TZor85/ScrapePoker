using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Ejecuta el pipeline postflop actual (equity + coordinador) para un estado de mesa.
/// Lo comparten el generador de fixtures y el replay: lo que se congela es exactamente
/// lo que después se verifica.
/// </summary>
internal static class PostflopDecisionRunner
{
    public static (string Action, double Equity) Run(
        StrategyProfile profile,
        PlayerGameState state,
        decimal maxBet,
        int monteCarloSeed,
        IGameLoggerService? logger = null,
        ILogger<UnifiedPokerCalculator>? calculationLogger = null)
    {
        var coordinator = GameCoordinatorFixtureBuilder.Create(profile, logger ?? new NoOpGameLogger());
        var calculator = GameCoordinatorFixtureBuilder.CreateCalculator(profile, monteCarloSeed, calculationLogger);

        var result = calculator.Calculate(
            ToCards(state.HoleCard1Suit, state.HoleCard1Rank, state.HoleCard2Suit, state.HoleCard2Rank),
            ToCommunityCards(state),
            state.PotSize,
            maxBet,
            isInPosition: state.IsInPosition,
            heroStack: state.HeroStack,
            villainStack: coordinator.GetVillainStack(state),
            handSituation: state.HandSituation.ToString(),
            opponentProfile: null);

        coordinator.SetFlopResult(result);
        var decision = coordinator.DetermineFlopAction(state);

        return (decision.Action, result.EquityPercentage);
    }

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
