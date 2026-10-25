using Microsoft.Extensions.Options;
using OpenScrape.App.Tests.Fixtures;
using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Contract;

/// <summary>
/// Si la equity no se pudo calcular, el motor no puede devolver una decisión:
/// una equity en cero es una mentira, no un empate.
/// </summary>
[TestFixture]
public class PostflopRequestFactoryTests
{
    [Test]
    public void Create_CalculadoraQueFalla_LanzaCalculationFailed()
    {
        var factory = new PostflopRequestFactory(
            new FailingCalculator(),
            new BoardTextureAnalyzer(),
            new PreflopAnalyzer(),
            Options.Create(StrategyProfileFixture.LoadOrCreate()));

        var request = new DecisionRequest
        {
            Street = DecisionStreet.Flop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            Board =
            [
                new CardRef((int)Rank.King, (int)Suit.Hearts),
                new CardRef((int)Rank.Seven, (int)Suit.Clubs),
                new CardRef((int)Rank.Two, (int)Suit.Diamonds)
            ],
            PotSize = 10m,
            BetToCall = 5m,
            HeroStack = 90m,
            VillainStack = 95m,
            HeroPosition = TablePosition.Button,
            VillainPosition = TablePosition.BigBlind,
            IsInPosition = true,
            HandSituation = HandSituation.OpenRaise
        };

        Assert.Throws<CalculationFailedException>(() => factory.Create(request));
    }

    [Test]
    public void Create_ConPerfilDeVillano_LoPasaAlCalculador()
    {
        var calculator = new RecordingCalculator();
        var factory = new PostflopRequestFactory(
            calculator,
            new BoardTextureAnalyzer(),
            new PreflopAnalyzer(),
            Options.Create(StrategyProfileFixture.LoadOrCreate()));

        var request = new DecisionRequest
        {
            Street = DecisionStreet.Flop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            Board =
            [
                new CardRef((int)Rank.King, (int)Suit.Hearts),
                new CardRef((int)Rank.Seven, (int)Suit.Clubs),
                new CardRef((int)Rank.Two, (int)Suit.Diamonds)
            ],
            PotSize = 10m,
            BetToCall = 5m,
            HeroStack = 90m,
            VillainStack = 95m,
            HeroPosition = TablePosition.Button,
            VillainPosition = TablePosition.BigBlind,
            IsInPosition = true,
            HandSituation = HandSituation.OpenRaise,
            VillainProfile = new VillainProfile
            {
                HandsPlayed = 20,
                TimesVoluntarilyPutMoneyIn = 8,
                TimesPreflopRaised = 5,
                TimesThreeBet = 2
            }
        };

        factory.Create(request);

        Assert.That(calculator.Received, Is.Not.Null);
        Assert.That(calculator.Received!.HandsPlayed, Is.EqualTo(20));
        Assert.That(calculator.Received.TimesVoluntarilyPutMoneyIn, Is.EqualTo(8));
        Assert.That(calculator.Received.TimesThreeBet, Is.EqualTo(2));
    }

    [TestCase(OpponentType.LP, 1.25)]
    [TestCase(OpponentType.TP, 1.10)]
    [TestCase(OpponentType.TAG, 0.85)]
    [TestCase(OpponentType.LAG, 0.70)]
    public void Create_PerfilFiableSinAjusteDelCliente_AplicaElMultiplicadorPorTipo(
        OpponentType type,
        double factor)
    {
        var factory = CreateFactory(new RecordingCalculator { FoldEquity = 40 });

        var engineRequest = factory.Create(CreateFlopRequest(new VillainProfile
        {
            Type = type,
            AdjustedFoldEquity = -1,
            HasReliableAggressionData = true
        }));

        Assert.That(engineRequest.FoldEquity, Is.EqualTo(40 * factor).Within(0.001));
    }

    [Test]
    public void Create_PerfilSinMuestrasDeAgresion_UsaLaFoldEquityCalculada()
    {
        var factory = CreateFactory(new RecordingCalculator { FoldEquity = 40 });

        var engineRequest = factory.Create(CreateFlopRequest(new VillainProfile
        {
            Type = OpponentType.LP,
            AdjustedFoldEquity = -1,
            HasReliableAggressionData = false
        }));

        Assert.That(engineRequest.FoldEquity, Is.EqualTo(40).Within(0.001));
    }

    [Test]
    public void Create_AjusteExplicitoDelCliente_TienePrioridadSobreElMultiplicador()
    {
        var factory = CreateFactory(new RecordingCalculator { FoldEquity = 40 });

        var engineRequest = factory.Create(CreateFlopRequest(new VillainProfile
        {
            Type = OpponentType.LP,
            AdjustedFoldEquity = 33,
            HasReliableAggressionData = true
        }));

        Assert.That(engineRequest.FoldEquity, Is.EqualTo(33).Within(0.001));
    }

    private static PostflopRequestFactory CreateFactory(IPokerCalculator calculator) => new(
        calculator,
        new BoardTextureAnalyzer(),
        new PreflopAnalyzer(),
        Options.Create(StrategyProfileFixture.LoadOrCreate()));

    private static DecisionRequest CreateFlopRequest(VillainProfile profile) => new()
    {
        Street = DecisionStreet.Flop,
        HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
        HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
        Board =
        [
            new CardRef((int)Rank.King, (int)Suit.Hearts),
            new CardRef((int)Rank.Seven, (int)Suit.Clubs),
            new CardRef((int)Rank.Two, (int)Suit.Diamonds)
        ],
        PotSize = 10m,
        BetToCall = 5m,
        HeroStack = 90m,
        VillainStack = 95m,
        HeroPosition = TablePosition.Button,
        VillainPosition = TablePosition.BigBlind,
        IsInPosition = true,
        HandSituation = HandSituation.OpenRaise,
        VillainProfile = profile
    };

    private sealed class RecordingCalculator : IPokerCalculator
    {
        public OpponentProfile? Received { get; private set; }

        public double FoldEquity { get; init; }

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
            OpponentProfile? opponentProfile = null)
        {
            Received = opponentProfile;
            return new PokerCalculationResult { DrawTypes = [], FoldEquity = FoldEquity };
        }
    }

    private sealed class FailingCalculator : IPokerCalculator
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
            new() { Failed = true, FailureReason = "simulado" };
    }
}
