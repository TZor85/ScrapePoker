using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Genera los fixtures postflop desde el comportamiento actual. No corre en CI:
/// se ejecuta a mano cuando cambia el motor a propósito y hay que re-congelar el contrato.
/// </summary>
[TestFixture]
[Explicit("Genera fixtures: ejecutar a mano cuando se acepte un cambio de comportamiento")]
public class PostflopFixtureGeneratorTests
{
    private const int Seed = 20261023;

    [Test]
    public void Generate_Escenarios_EscribeUnJsonPorEscenario()
    {
        // Arrange
        var profile = StrategyProfileFixture.LoadOrCreate();
        StrategyProfileFixture.Save(profile);

        // Act
        foreach (var fixture in BuildFixtures(profile))
            PostflopFixtureStore.Save(fixture);

        // Assert
        Assert.That(PostflopFixtureStore.LoadAll(FixturePaths.SourcePostflop), Has.Count.EqualTo(7));
    }

    private static IEnumerable<PostflopFixture> BuildFixtures(StrategyProfile profile)
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
            BoardPosition.Flop,
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
            BoardPosition.Flop,
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
            BoardPosition.Flop,
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
            BoardPosition.Flop,
            profile);

        yield return CreateFixture(
            "turn-top-pair-out-of-position",
            State(
                hole: [(Rank.Queen, Suit.Clubs), (Rank.Ten, Suit.Spades)],
                board:
                [
                    (Rank.King, Suit.Spades), (Rank.Queen, Suit.Diamonds),
                    (Rank.Seven, Suit.Clubs), (Rank.Two, Suit.Hearts)
                ],
                situation: HandSituation.Call,
                inPosition: false,
                pot: 16m,
                heroStack: 84m),
            maxBet: 8m,
            BoardPosition.Turn,
            profile);

        yield return CreateFixture(
            "river-bluff-catcher",
            State(
                hole: [(Rank.Ace, Suit.Hearts), (Rank.Five, Suit.Hearts)],
                board:
                [
                    (Rank.King, Suit.Hearts), (Rank.Nine, Suit.Hearts),
                    (Rank.Four, Suit.Hearts), (Rank.Two, Suit.Clubs), (Rank.Eight, Suit.Spades)
                ],
                situation: HandSituation.Call,
                inPosition: false,
                pot: 24m,
                heroStack: 76m),
            maxBet: 20m,
            BoardPosition.River,
            profile);

        yield return CreateFixture(
            "river-second-pair",
            State(
                hole: [(Rank.Queen, Suit.Clubs), (Rank.Jack, Suit.Diamonds)],
                board:
                [
                    (Rank.Queen, Suit.Spades), (Rank.Eight, Suit.Hearts),
                    (Rank.Three, Suit.Clubs), (Rank.Five, Suit.Diamonds), (Rank.King, Suit.Spades)
                ],
                situation: HandSituation.Call,
                inPosition: true,
                pot: 30m,
                heroStack: 70m),
            maxBet: 15m,
            BoardPosition.River,
            profile);
    }

    private static PostflopFixture CreateFixture(
        string id,
        PlayerGameState state,
        decimal maxBet,
        BoardPosition street,
        StrategyProfile profile)
    {
        var snapshot = PostflopDecisionRunner.RunStreet(
            profile, state, maxBet, Seed, street,
            calculationLogger: new TestLogger<OpenScrape.App.Aplication.UseCases.UnifiedPokerCalculator>());

        return new PostflopFixture(
            id,
            street.ToString(),
            state,
            maxBet,
            snapshot.Action,
            snapshot.Equity,
            Seed,
            snapshot.Reason,
            snapshot.BoardTexture,
            snapshot.IsBluff,
            snapshot.IsBarrel,
            snapshot.IsCheckRaise);
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
                    Position = index < 3
                        ? BoardPosition.Flop
                        : index == 3 ? BoardPosition.Turn : BoardPosition.River,
                    Location = index + 1
                })
                .ToList()
        };
    }
}


