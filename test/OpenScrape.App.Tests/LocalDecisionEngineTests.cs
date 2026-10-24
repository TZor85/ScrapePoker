using Microsoft.Extensions.Options;
using OpenScrape.App.Tests.Fixtures;
using OpenScrape.Contracts;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.DecisionMaker.Services;
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

        var profile = StrategyProfileFixture.LoadOrCreate();
        var options = Options.Create(profile);

        var preflop = PreflopTestCaseBuilder.Create(
            new FixedRangeStore(table),
            new FixedRandomProvider(30));

        var engine = new LocalDecisionEngine(
            new PreflopRequestFactory(),
            new PostflopRequestFactory(
                GameCoordinatorFixtureBuilder.CreateCalculator(profile, monteCarloSeed: 1),
                new BoardTextureAnalyzer(),
                new PreflopAnalyzer(),
                options),
            preflop,
            GameCoordinatorFixtureBuilder.CreateEngine(profile));

        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            HeroPosition = TablePosition.Button,
            PotSize = 3m,
            BetToCall = 3m,
            HeroStack = 100m,
            VillainStack = 100m,
            IsInPosition = true,
            HandSituation = HandSituation.None
        };

        // Act
        var result = await engine.DecideAsync(request);

        // Assert
        Assert.That(result.Action, Is.EqualTo("Raise"));
    }
}
