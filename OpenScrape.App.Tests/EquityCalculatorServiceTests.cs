using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class EquityCalculatorServiceTests
{
    private EquityCalculatorService _service;

    [SetUp]
    public void Setup()
    {
        var monteCarlo = new MonteCarloSimulator();
        var outsCalculator = new OutsCalculator();
        var preflopCalculator = new PreflopEquityCalculator();
        _service = new EquityCalculatorService(monteCarlo, outsCalculator, preflopCalculator);
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void CalculateFullEquity_Preflop_DeberiaUsarPreflopCalculator()
    {
        // AA preflop -> sin community cards
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 100, 50);

        Assert.That(result.OverallEquity, Is.InRange(0.5, 1.0));
        // Preflop no calcula outs
        Assert.That(result.Outs, Is.EqualTo(0));
        Assert.That(result.DrawTypes, Is.Empty);
    }

    [Test]
    public void CalculateFullEquity_Postflop_DeberiaUsarMonteCarlo()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Queen, Suit.Clubs)
        };

        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 100, 50, 2000);

        Assert.That(result.OverallEquity, Is.InRange(0.0, 1.0));
        Assert.That(result.HandDistribution, Is.Not.Empty);
    }

    [Test]
    public void CalculateFullEquity_EquityAlta_DeberiaRecomendarRaise()
    {
        // AA vs 1 oponente con board favorable
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.Two, Suit.Clubs),
            C(Rank.Seven, Suit.Spades)
        };

        // potSize=100, callAmount=10 -> pot odds bajas, equity alta
        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 100, 10, 3000);

        Assert.That(result.RecommendedAction, Is.EqualTo("RAISE/BET"));
    }

    [Test]
    public void CalculateFullEquity_EquityBaja_SinOuts_DeberiaRecomendarFold()
    {
        // Hero: 2s 7d vs board que no ayuda
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades),
            C(Rank.Seven, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Jack, Suit.Spades)
        };

        // callAmount grande relativo al pot -> EV negativo
        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 50, 100, 3000);

        Assert.That(result.RecommendedAction, Is.EqualTo("FOLD"));
    }

    [Test]
    public void CalculateFullEquity_PotOdds_CalculoCorrectoCallAmountVsPot()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        // potSize=100, callAmount=25 -> potOdds = 25/(100+25) = 0.20
        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 100, 25);

        Assert.That(result.PotOdds, Is.EqualTo(25.0 / 125.0).Within(0.001));
    }

    [Test]
    public void CalculateFullEquity_ExpectedValue_SignoCorrecto()
    {
        // AA preflop con pot odds favorables -> EV positivo
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 100, 10);

        // AA tiene equity alta (~85%), EV deberia ser positivo
        Assert.That(result.ExpectedValue, Is.GreaterThan(0));
    }

    [Test]
    public void CalculateFullEquity_ExpectedValue_ManoDebil_EVNegativo()
    {
        // 72o con call grande -> EV negativo
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades),
            C(Rank.Seven, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Jack, Suit.Spades)
        };

        var result = _service.CalculateFullEquity(myCards, communityCards, 1, 50, 100, 3000);

        Assert.That(result.ExpectedValue, Is.LessThan(0));
    }
}
