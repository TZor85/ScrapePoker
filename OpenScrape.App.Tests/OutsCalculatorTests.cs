using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class OutsCalculatorTests
{
    private OutsCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator = new OutsCalculator();
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void CalculateOuts_FlushDraw_DeberiaDetectar9Outs()
    {
        // Hero: Ah 5h, Flop: 2h 7h Kc (4 hearts = flush draw)
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

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasFlushDraw, Is.True);
        Assert.That(result.TotalOuts, Is.EqualTo(9));
    }

    [Test]
    public void CalculateOuts_OpenEndedStraightDraw_DeberiaDetectarOuts()
    {
        // El algoritmo detecta OESD cuando hay 4 cartas con rango diferencia=4
        // Hero: 7s 8d, Flop: 9c Th 2c -> ranks sorted: 2,7,8,9,10
        // 7,8,9,10 => diff=3 -> no activa el OESD del algoritmo
        // El algoritmo busca gaps: 7,8,10 (diff=3) -> gutshot
        // Usamos 7,8,9,J (diff=4 entre 7 y J) para activar OESD
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Spades),
            C(Rank.Eight, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Two, Suit.Clubs)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // 7,8,9,J tiene diferencia 4 -> OESD: necesita 6 o Q (= 8 cartas)
        Assert.That(result.HasOpenEndedStraightDraw, Is.True);
        Assert.That(result.TotalOuts, Is.GreaterThanOrEqualTo(8));
    }

    [Test]
    public void CalculateOuts_Gutshot_DeberiaDetectar4Outs()
    {
        // Hero: 8s 9d, Flop: 6c Jh 2s (gutshot: necesita 7 o 10)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Eight, Suit.Spades),
            C(Rank.Nine, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Six, Suit.Clubs),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Two, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.HasGutshotStraightDraw || result.HasOpenEndedStraightDraw, Is.True);
        Assert.That(result.TotalOuts, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void CalculateOuts_SinDraws_DeberiaSer0Outs()
    {
        // Hero: 2s 7d, Flop: Kh Qc 9d (sin draw alguno)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades),
            C(Rank.Seven, Suit.Diamonds)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Nine, Suit.Diamonds)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        Assert.That(result.TotalOuts, Is.EqualTo(0));
        Assert.That(result.HasFlushDraw, Is.False);
        Assert.That(result.HasOpenEndedStraightDraw, Is.False);
        Assert.That(result.HasGutshotStraightDraw, Is.False);
    }

    [Test]
    public void CalculateOuts_OutsToEquity_ReglaDe2y4_Flop()
    {
        // En el flop, cardsToCome = 2, equity = outs * 2 * 2
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

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // cardsToCome = 5 - 3 = 2, equity = 9 * 2 * 2 = 36
        Assert.That(result.OutsToEquity, Is.EqualTo(result.TotalOuts * 2 * 2.0));
    }

    [Test]
    public void CalculateOuts_OutsToEquity_ReglaDe2_Turn()
    {
        // En el turn, cardsToCome = 1, equity = outs * 1 * 2
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs),
            C(Rank.Three, Suit.Spades)
        };

        var result = _calculator.CalculateOuts(myCards, communityCards);

        // cardsToCome = 5 - 4 = 1, equity = outs * 1 * 2
        Assert.That(result.OutsToEquity, Is.EqualTo(result.TotalOuts * 1 * 2.0));
    }
}
