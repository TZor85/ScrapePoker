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
        var engine = CreateEngine();

        var request = CreatePreflopRequest();

        // Act
        var result = await engine.DecideAsync(request);

        // Assert
        Assert.That(result.Action, Is.EqualTo("Raise"));
    }

    [Test]
    public async Task DecideAsync_Preflop_DevuelveLaEquityParaElResumen()
    {
        // Arrange
        var engine = CreateEngine();

        // Act
        var result = await engine.DecideAsync(CreatePreflopRequest());

        // Assert
        Assert.That(result.Equity, Is.InRange(20d, 95d));
    }

    [Test]
    public async Task DecideAsync_Preflop_UsaLaEquityDelCalculadorDelMotor()
    {
        // Arrange: la equity del resume tiene que salir del mismo calculador que
        // usa el motor (Monte Carlo contra el rango), no de una tabla paralela.
        var engine = CreateEngine(new FixedEquityCalculator(12.5));

        // Act
        var result = await engine.DecideAsync(CreatePreflopRequest());

        // Assert
        Assert.That(result.Equity, Is.EqualTo(12.5).Within(0.001));
    }

    private static DecisionRequest CreatePreflopRequest() => new()
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

    private static LocalDecisionEngine CreateEngine(IPokerCalculator? calculator = null)
    {
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

        return new LocalDecisionEngine(
            new PreflopRequestFactory(),
            new PostflopRequestFactory(
                GameCoordinatorFixtureBuilder.CreateCalculator(profile, monteCarloSeed: 1),
                new BoardTextureAnalyzer(),
                new PreflopAnalyzer(),
                options),
            preflop,
            GameCoordinatorFixtureBuilder.CreateEngine(profile),
            calculator ?? GameCoordinatorFixtureBuilder.CreateCalculator(profile, monteCarloSeed: 1));
    }
}

/// <summary>Calculador con equity fija, para aislar el mapeo del contrato.</summary>
internal sealed class FixedEquityCalculator(double equity) : IPokerCalculator
{
    public PokerCalculationResult Calculate(
        List<CardDataOuts> playerHand,
        List<CardDataOuts> communityCards,
        decimal currentPotSize,
        decimal betToCall,
        int numOpponents = 1,
        int? monteCarloIterations = null,
        bool isInPosition = false,
        decimal heroStack = 0,
        decimal villainStack = 0,
        string? handSituation = null,
        TablePosition villainPosition = TablePosition.None,
        OpponentProfile? opponentProfile = null) =>
        new() { EquityPercentage = equity, DrawTypes = [] };
}