using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Genera los fixtures postflop desde el comportamiento actual. No corre en CI:
/// se ejecuta a mano con <c>-- NUnit.ExplicitMode=Include</c> cuando cambia el motor
/// a propósito y hay que re-congelar el contrato.
/// </summary>
[TestFixture]
[Explicit("Genera fixtures: ejecutar a mano cuando se acepte un cambio de comportamiento")]
public class PostflopFixtureGeneratorTests
{
    private const int Seed = 20261023;

    [Test]
    public void Generate_EscenariosFlop_EscribeUnJsonPorEscenario()
    {
        // Arrange
        var profile = StrategyProfileFixture.LoadOrCreate();
        StrategyProfileFixture.Save(profile);

        // Act
        foreach (var fixture in BuildFixtures(profile))
            PostflopFixtureStore.Save(fixture);

        // Assert
        Assert.That(PostflopFixtureStore.LoadAll(), Has.Count.EqualTo(4));
    }

    private static IEnumerable<PostflopFixture> BuildFixtures(OpenScrape.Domain.Entities.StrategyProfile profile)
    {
        yield return CreateFixture(
            "flop-set-in-position",
            State(
                hole: [(Rank.King, Suit.Spades), (Rank.King, Suit.Diamonds)],
                board: [(Rank.King, Suit.Hearts), (Rank.Seven, Suit.Clubs), (Rank.Two, Suit.Diamonds)],
                situation: HandSituation.OpenRaise,
                inPosition: true,
                pot: 10m,
                heroStack: 90m),
            maxBet: 0m,
            profile);

        yield return CreateFixture(
            "flop-flush-draw-out-of-position",
            State(
                hole: [(Rank.Queen, Suit.Hearts), (Rank.Jack, Suit.Hearts)],
                board: [(Rank.Ace, Suit.Hearts), (Rank.Nine, Suit.Hearts), (Rank.Four, Suit.Clubs)],
                situation: HandSituation.Call,
                inPosition: false,
                pot: 12m,
                heroStack: 88m),
            maxBet: 6m,
            profile);

        yield return CreateFixture(
            "flop-air-vs-aggressor",
            State(
                hole: [(Rank.Five, Suit.Clubs), (Rank.Four, Suit.Clubs)],
                board: [(Rank.Queen, Suit.Spades), (Rank.Eight, Suit.Diamonds), (Rank.Three, Suit.Hearts)],
                situation: HandSituation.RaiseOverLimper,
                inPosition: false,
                pot: 15m,
                heroStack: 85m),
            maxBet: 10m,
            profile);

        yield return CreateFixture(
            "flop-top-pair-out-of-position",
            State(
                hole: [(Rank.Jack, Suit.Clubs), (Rank.Ten, Suit.Spades)],
                board: [(Rank.Jack, Suit.Diamonds), (Rank.Seven, Suit.Spades), (Rank.Two, Suit.Clubs)],
                situation: HandSituation.OpenRaise,
                inPosition: false,
                pot: 8m,
                heroStack: 92m),
            maxBet: 0m,
            profile);
    }

    private static PostflopFixture CreateFixture(
        string id,
        PlayerGameState state,
        decimal maxBet,
        OpenScrape.Domain.Entities.StrategyProfile profile)
    {
        var (action, equity) = PostflopDecisionRunner.Run(
            profile, state, maxBet, Seed,
            calculationLogger: new TestLogger<OpenScrape.App.Aplication.UseCases.UnifiedPokerCalculator>());
        return new PostflopFixture(id, "Flop", state, maxBet, action, equity, Seed);
    }

    private static PlayerGameState State(
        (Rank Rank, Suit Suit)[] hole,
        (Rank Rank, Suit Suit)[] board,
        HandSituation situation,
        bool inPosition,
        decimal pot,
        decimal heroStack)
    {
        return new PlayerGameState
        {
            PotSize = pot,
            HeroStack = heroStack,
            IsInPosition = inPosition,
            HandSituation = situation,
            HoleCard1Rank = (int)hole[0].Rank,
            HoleCard1Suit = (int)hole[0].Suit,
            HoleCard2Rank = (int)hole[1].Rank,
            HoleCard2Suit = (int)hole[1].Suit,
            Players =
            [
                new Player { Name = "P0", Active = true, Stack = heroStack, Bet = 0m, ValuePosition = 0 },
                new Player { Name = "P1", Active = true, Stack = 100m, Bet = 0m, ValuePosition = 1 },
                new Player { Name = "P2", Active = true, Stack = 100m, Bet = 0m, ValuePosition = 2 }
            ],
            BoardCards = board
                .Select((card, index) => new BoardData
                {
                    Name = card.Rank.ToString(),
                    Force = (int)card.Rank,
                    Suit = (int)card.Suit,
                    Position = BoardPosition.Flop,
                    Location = index + 1
                })
                .ToList()
        };
    }
}
