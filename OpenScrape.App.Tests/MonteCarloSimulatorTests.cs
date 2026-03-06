using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class MonteCarloSimulatorTests
{
    private MonteCarloSimulator _simulator;

    [SetUp]
    public void Setup()
    {
        _simulator = new MonteCarloSimulator();
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void CalculateEquity_PocketAces_Vs1Oponente_DeberiaSerAlrededor85Porciento()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 5000);

        Assert.That(result.Equity, Is.InRange(0.75, 0.95));
    }

    [Test]
    public void CalculateEquity_FlushDrawEnFlop_DeberiaSerRazonable()
    {
        // Hero: Ah 5h, Flop: 2h 7h Kc (flush draw + Ace high)
        // Equity real ~60-70% vs 1 oponente aleatorio (tiene flush draw + Ace)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 5000);

        Assert.That(result.Equity, Is.InRange(0.45, 0.85));
    }

    [Test]
    public void CalculateEquity_NutQuads_DeberiaSerCasi100Porciento()
    {
        // Hero: AA, Board: A A K (quads en flop)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.Ace, Suit.Clubs),
            C(Rank.King, Suit.Spades)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 3000);

        Assert.That(result.Equity, Is.GreaterThanOrEqualTo(0.95));
    }

    [Test]
    public void CalculateEquity_HandDistribution_DeberiaContenerManoCorrecta()
    {
        // Hero: AA con board completo de quads -> HandDistribution debe tener FourOfAKind > 0
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.Ace, Suit.Clubs),
            C(Rank.King, Suit.Spades)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 1000);

        Assert.That(result.HandDistribution.ContainsKey(HandRank.FourOfAKind), Is.True);
        Assert.That(result.HandDistribution[HandRank.FourOfAKind], Is.GreaterThan(0));
    }

    [Test]
    public void CalculateEquity_SimulationsCount_DeberiaIgualarIteracionesSolicitadas()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 500);

        Assert.That(result.Simulations, Is.EqualTo(500));
    }

    [Test]
    public void CalculateEquity_ProbabilidadesSumanUno()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Queen, Suit.Spades),
            C(Rank.Jack, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 2000);

        var total = result.WinProbability + result.TieProbability + result.LoseProbability;
        Assert.That(total, Is.InRange(0.99, 1.01));
    }
}
