using OpenScrape.App.Tests.Fixtures;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// La puerta única del motor: despacha por calle y devuelve un resultado uniforme.
/// </summary>
[TestFixture]
public class LocalDecisionEngineTests
{
    [Test]
    public async Task DecideAsync_PreflopDosTramos_RespetaElNumeroAleatorio()
    {
        // Arrange
        var table = new TableDTO
        {
            Name = "fx-local-engine",
            Positions =
            [
                new PlayerActionSequence(
                    Name: "fx-local-engine",
                    HeroPosition: "Button",
                    OpenRaiser: null,
                    ThreeBetPosition: null,
                    Limper: null,
                    Caller: null,
                    Squeezer: null,
                    BetSize: null,
                    IsGreater: null,
                    RaiserFolds: null,
                    Hands:
                    [
                        new Hand("AK", true, "Raise", 60),
                        new Hand("AK", true, "Call", 40)
                    ])
            ]
        };

        var preflop = PreflopTestCaseBuilder.Create(
            new FixedRangeStore(table),
            new FixedRandomProvider(30));

        var engine = new LocalDecisionEngine(
            preflop,
            GameCoordinatorFixtureBuilder.CreateEngine(StrategyProfileFixture.LoadOrCreate()));

        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            Preflop = CreateOpenRaiseRequest()
        };

        // Act
        var result = await engine.DecideAsync(request);

        // Assert
        Assert.That(result.Action, Is.EqualTo("Raise"));
    }

    private static SetPreflopActionUseCaseRequest CreateOpenRaiseRequest()
    {
        var state = new PlayerGameState
        {
            Position = TablePosition.Button,
            PotSize = 3m,
            HeroStack = 100m,
            IsInPosition = true,
            HandSituation = HandSituation.None,
            HoleCard1Face = "A",
            HoleCard2Face = "K",
            HoleCard1Rank = (int)Rank.Ace,
            HoleCard2Rank = (int)Rank.King,
            HoleCard1Suit = (int)Suit.Spades,
            HoleCard2Suit = (int)Suit.Spades
        };

        return new SetPreflopActionUseCaseRequest
        {
            ResponseAction = new ResponseAction(),
            PlayerState = state,
            PreflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>()
        };
    }
}
