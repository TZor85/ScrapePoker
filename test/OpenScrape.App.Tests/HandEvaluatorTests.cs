using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests parametrizados que verifican AMBAS implementaciones de IHandEvaluator:
/// - HandEvaluator (brute-force combinatorial, referencia)
/// - BitHandEvaluator (bit-manipulation, optimizado)
/// </summary>
[TestFixture]
public class HandEvaluatorTests
{
    private HandEvaluator _evaluator;

    [SetUp]
    public void Setup()
    {
        _evaluator = new HandEvaluator();
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void EvaluateBestHand_RoyalFlush_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ten, Suit.Spades),
            C(Rank.Jack, Suit.Spades),
            C(Rank.Queen, Suit.Spades),
            C(Rank.King, Suit.Spades),
            C(Rank.Ace, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.StraightFlush));
    }

    [Test]
    public void EvaluateBestHand_StraightFlush_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Five, Suit.Hearts),
            C(Rank.Six, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.StraightFlush));
    }

    [Test]
    public void EvaluateBestHand_FourOfAKind_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.Ace, Suit.Clubs),
            C(Rank.King, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.FourOfAKind));
    }

    [Test]
    public void EvaluateBestHand_FullHouse_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts),
            C(Rank.King, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Queen, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.FullHouse));
    }

    [Test]
    public void EvaluateBestHand_Flush_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Diamonds),
            C(Rank.Five, Suit.Diamonds),
            C(Rank.Eight, Suit.Diamonds),
            C(Rank.Jack, Suit.Diamonds),
            C(Rank.Ace, Suit.Diamonds)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.Flush));
    }

    [Test]
    public void EvaluateBestHand_Straight_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Six, Suit.Spades),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Eight, Suit.Diamonds),
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Ten, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.Straight));
    }

    [Test]
    public void EvaluateBestHand_WheelStraight_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Two, Suit.Hearts),
            C(Rank.Three, Suit.Diamonds),
            C(Rank.Four, Suit.Clubs),
            C(Rank.Five, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.Straight));
    }

    [Test]
    public void EvaluateBestHand_ThreeOfAKind_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Spades),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Jack, Suit.Diamonds),
            C(Rank.King, Suit.Clubs),
            C(Rank.Two, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.ThreeOfAKind));
    }

    [Test]
    public void EvaluateBestHand_TwoPair_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Two, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.TwoPair));
    }

    [Test]
    public void EvaluateBestHand_OnePair_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Jack, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.OnePair));
    }

    [Test]
    public void EvaluateBestHand_HighCard_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades),
            C(Rank.Five, Suit.Hearts),
            C(Rank.Eight, Suit.Diamonds),
            C(Rank.Jack, Suit.Clubs),
            C(Rank.Ace, Suit.Spades)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.HighCard));
    }

    [Test]
    public void EvaluateBestHand_7Cartas_DeberiaSeleccionarMejorCombinacion()
    {
        // Hero: AhKh, Board: Qh Jh Th 2c 3d -> Royal Flush
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts),
            C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.StraightFlush));
    }

    [Test]
    public void EvaluateBestHand_6Cartas_DeberiaSeleccionarMejorCombinacion()
    {
        // 6 cartas con full house posible
        var cards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts),
            C(Rank.King, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Queen, Suit.Spades),
            C(Rank.Two, Suit.Hearts)
        };

        var result = _evaluator.EvaluateBestHand(cards);

        Assert.That(result.Rank, Is.EqualTo(HandRank.FullHouse));
    }

    [Test]
    public void EvaluateBestHand_KickerComparacion_DosParesMismoRango()
    {
        // Dos pares KK QQ con kicker A
        var manoConKickerAlto = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs),
            C(Rank.Ace, Suit.Spades)
        };

        // Dos pares KK QQ con kicker 2
        var manoConKickerBajo = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Diamonds),
            C(Rank.King, Suit.Clubs),
            C(Rank.Queen, Suit.Spades),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Two, Suit.Spades)
        };

        var resultAlto = _evaluator.EvaluateBestHand(manoConKickerAlto);
        var resultBajo = _evaluator.EvaluateBestHand(manoConKickerBajo);

        Assert.That(resultAlto.Rank, Is.EqualTo(HandRank.TwoPair));
        Assert.That(resultBajo.Rank, Is.EqualTo(HandRank.TwoPair));
        // Ambos tienen mismo Score base (TWO_PAIR_MULTIPLIER * King)
        // pero el kicker del primero (Ace=14) es mayor que el del segundo (Two=2)
        Assert.That(resultAlto.Kickers[^1], Is.GreaterThan(resultBajo.Kickers[^1]));
    }

    [Test]
    public void EvaluateBestHand_MenosDe5Cartas_DeberiaLanzarExcepcion()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds),
            C(Rank.Jack, Suit.Clubs)
        };

        Assert.Throws<ArgumentException>(() => _evaluator.EvaluateBestHand(cards));
    }

    [Test]
    public void EvaluateBestHand_WheelStraight_DeberiaSerMenorQueEscalera6High()
    {
        // Wheel (A-2-3-4-5) debe puntuar menos que escalera 6-high (2-3-4-5-6)
        var wheel = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Two, Suit.Hearts),
            C(Rank.Three, Suit.Diamonds),
            C(Rank.Four, Suit.Clubs),
            C(Rank.Five, Suit.Spades)
        };

        var seisHigh = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Diamonds),
            C(Rank.Three, Suit.Clubs),
            C(Rank.Four, Suit.Spades),
            C(Rank.Five, Suit.Hearts),
            C(Rank.Six, Suit.Diamonds)
        };

        var wheelResult = _evaluator.EvaluateBestHand(wheel);
        var seisHighResult = _evaluator.EvaluateBestHand(seisHigh);

        Assert.That(wheelResult.Rank, Is.EqualTo(HandRank.Straight));
        Assert.That(seisHighResult.Rank, Is.EqualTo(HandRank.Straight));
        Assert.That(wheelResult.Score, Is.LessThan(seisHighResult.Score),
            "El wheel (A-2-3-4-5) debe puntuar menor que la escalera 6-high");
    }

    [Test]
    public void EvaluateBestHand_JerarquiaCorrecta_FlushRankMayorQueStraight()
    {
        var flush = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Five, Suit.Hearts),
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Jack, Suit.Hearts),
            C(Rank.King, Suit.Hearts)
        };

        var straight = new List<CardDataOuts>
        {
            C(Rank.Six, Suit.Spades),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Eight, Suit.Diamonds),
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Ten, Suit.Spades)
        };

        var flushResult = _evaluator.EvaluateBestHand(flush);
        var straightResult = _evaluator.EvaluateBestHand(straight);

        Assert.That(flushResult.Rank, Is.GreaterThan(straightResult.Rank));
    }
}

/// <summary>
/// Tests de regresión para BitHandEvaluator: verifica que produce resultados
/// idénticos al HandEvaluator original (referencia) en todos los casos.
/// </summary>
[TestFixture]
public class BitHandEvaluatorTests
{
    private BitHandEvaluator _evaluator;
    private HandEvaluator _referencia;

    [SetUp]
    public void Setup()
    {
        _evaluator = new BitHandEvaluator();
        _referencia = new HandEvaluator();
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void EvaluateBestHand_StraightFlush_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Five, Suit.Hearts),
            C(Rank.Six, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.StraightFlush));
    }

    [Test]
    public void EvaluateBestHand_FourOfAKind_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts),
            C(Rank.Ace, Suit.Diamonds), C(Rank.Ace, Suit.Clubs),
            C(Rank.King, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.FourOfAKind));
    }

    [Test]
    public void EvaluateBestHand_FullHouse_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts), C(Rank.King, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs), C(Rank.Queen, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.FullHouse));
    }

    [Test]
    public void EvaluateBestHand_Flush_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Diamonds), C(Rank.Five, Suit.Diamonds),
            C(Rank.Eight, Suit.Diamonds), C(Rank.Jack, Suit.Diamonds),
            C(Rank.Ace, Suit.Diamonds)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.Flush));
    }

    [Test]
    public void EvaluateBestHand_Straight_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Six, Suit.Spades), C(Rank.Seven, Suit.Hearts),
            C(Rank.Eight, Suit.Diamonds), C(Rank.Nine, Suit.Clubs),
            C(Rank.Ten, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.Straight));
    }

    [Test]
    public void EvaluateBestHand_WheelStraight_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.Two, Suit.Hearts),
            C(Rank.Three, Suit.Diamonds), C(Rank.Four, Suit.Clubs),
            C(Rank.Five, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.Straight));
    }

    [Test]
    public void EvaluateBestHand_ThreeOfAKind_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Spades), C(Rank.Jack, Suit.Hearts), C(Rank.Jack, Suit.Diamonds),
            C(Rank.King, Suit.Clubs), C(Rank.Two, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.ThreeOfAKind));
    }

    [Test]
    public void EvaluateBestHand_TwoPair_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Two, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.TwoPair));
    }

    [Test]
    public void EvaluateBestHand_OnePair_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Jack, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.OnePair));
    }

    [Test]
    public void EvaluateBestHand_HighCard_DeberiaDetectarCorrectamente()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades), C(Rank.Five, Suit.Hearts),
            C(Rank.Eight, Suit.Diamonds), C(Rank.Jack, Suit.Clubs),
            C(Rank.Ace, Suit.Spades)
        };
        var result = _evaluator.EvaluateBestHand(cards);
        Assert.That(result.Rank, Is.EqualTo(HandRank.HighCard));
    }

    [Test]
    public void EvaluateBestHand_7Cartas_MejorManoCoincidesConReferencia()
    {
        // Hero: AhKh, Board: Qh Jh Th 2c 3d -> Royal Flush
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts), C(Rank.Jack, Suit.Hearts),
            C(Rank.Ten, Suit.Hearts), C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };
        var bitResult = _evaluator.EvaluateBestHand(cards);
        var refResult = _referencia.EvaluateBestHand(cards);
        Assert.That(bitResult.Rank, Is.EqualTo(refResult.Rank));
        Assert.That(bitResult.Score, Is.EqualTo(refResult.Score));
    }

    [Test]
    public void EvaluateBestHand_7Cartas_FullHouse_MejorManoCoincidesConReferencia()
    {
        // Mano con full house disponible entre 7 cartas
        var cards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts), C(Rank.King, Suit.Diamonds),
            C(Rank.Queen, Suit.Clubs), C(Rank.Queen, Suit.Spades),
            C(Rank.Two, Suit.Hearts), C(Rank.Three, Suit.Clubs)
        };
        var bitResult = _evaluator.EvaluateBestHand(cards);
        var refResult = _referencia.EvaluateBestHand(cards);
        Assert.That(bitResult.Rank, Is.EqualTo(refResult.Rank),
            $"BitHandEvaluator devolvió {bitResult.Rank}, referencia devolvió {refResult.Rank}");
        Assert.That(bitResult.Score, Is.EqualTo(refResult.Score));
    }

    [Test]
    public void EvaluateBestHand_WheelStraight_ScoreMenorQueSeisHigh()
    {
        var wheel = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.Two, Suit.Hearts),
            C(Rank.Three, Suit.Diamonds), C(Rank.Four, Suit.Clubs),
            C(Rank.Five, Suit.Spades)
        };
        var seisHigh = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Diamonds), C(Rank.Three, Suit.Clubs),
            C(Rank.Four, Suit.Spades), C(Rank.Five, Suit.Hearts),
            C(Rank.Six, Suit.Diamonds)
        };
        var wheelResult = _evaluator.EvaluateBestHand(wheel);
        var seisHighResult = _evaluator.EvaluateBestHand(seisHigh);
        Assert.That(wheelResult.Rank, Is.EqualTo(HandRank.Straight));
        Assert.That(seisHighResult.Rank, Is.EqualTo(HandRank.Straight));
        Assert.That(wheelResult.Score, Is.LessThan(seisHighResult.Score),
            "El wheel (A-2-3-4-5) debe puntuar menor que la escalera 6-high");
    }

    [Test]
    public void EvaluateBestHand_MenosDe5Cartas_DeberiaLanzarExcepcion()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds), C(Rank.Jack, Suit.Clubs)
        };
        Assert.Throws<ArgumentException>(() => _evaluator.EvaluateBestHand(cards));
    }

    [Test]
    public void EvaluateBestHand_StraightFlush_ScoreCoincidesConReferencia()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Five, Suit.Hearts), C(Rank.Six, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts), C(Rank.Eight, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts)
        };
        var bitResult = _evaluator.EvaluateBestHand(cards);
        var refResult = _referencia.EvaluateBestHand(cards);
        Assert.That(bitResult.Score, Is.EqualTo(refResult.Score));
    }

    [Test]
    public void EvaluateBestHand_7Cartas_StraightFlush_VsFullHouse()
    {
        // Con 7 cartas: tiene straight flush (5-9 hearts) Y full house (KKK QQ)
        // Debe elegir el straight flush
        var cards = new List<CardDataOuts>
        {
            C(Rank.Five, Suit.Hearts), C(Rank.Six, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts), C(Rank.Eight, Suit.Hearts),
            C(Rank.Nine, Suit.Hearts),
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Clubs)
        };
        var bitResult = _evaluator.EvaluateBestHand(cards);
        var refResult = _referencia.EvaluateBestHand(cards);
        Assert.That(bitResult.Rank, Is.EqualTo(HandRank.StraightFlush));
        Assert.That(bitResult.Rank, Is.EqualTo(refResult.Rank));
    }

    [Test]
    public void EvaluateBestHand_Comparacion_BitVsReferencia_TodosLosRangos()
    {
        // Colección de manos que cubre todos los rangos — ambas implementaciones deben coincidir
        var manos = new List<(string desc, List<CardDataOuts> cards, HandRank expectedRank)>
        {
            ("StraightFlush", new List<CardDataOuts> { C(Rank.Five, Suit.Clubs), C(Rank.Six, Suit.Clubs), C(Rank.Seven, Suit.Clubs), C(Rank.Eight, Suit.Clubs), C(Rank.Nine, Suit.Clubs) }, HandRank.StraightFlush),
            ("FourOfAKind",   new List<CardDataOuts> { C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts), C(Rank.Ace, Suit.Diamonds), C(Rank.Ace, Suit.Clubs), C(Rank.King, Suit.Spades) }, HandRank.FourOfAKind),
            ("FullHouse",     new List<CardDataOuts> { C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts), C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs), C(Rank.Queen, Suit.Spades) }, HandRank.FullHouse),
            ("Flush",         new List<CardDataOuts> { C(Rank.Two, Suit.Diamonds), C(Rank.Five, Suit.Diamonds), C(Rank.Eight, Suit.Diamonds), C(Rank.Jack, Suit.Diamonds), C(Rank.Ace, Suit.Diamonds) }, HandRank.Flush),
            ("Straight",      new List<CardDataOuts> { C(Rank.Six, Suit.Spades), C(Rank.Seven, Suit.Hearts), C(Rank.Eight, Suit.Diamonds), C(Rank.Nine, Suit.Clubs), C(Rank.Ten, Suit.Spades) }, HandRank.Straight),
            ("ThreeOfAKind",  new List<CardDataOuts> { C(Rank.Jack, Suit.Spades), C(Rank.Jack, Suit.Hearts), C(Rank.Jack, Suit.Diamonds), C(Rank.King, Suit.Clubs), C(Rank.Two, Suit.Spades) }, HandRank.ThreeOfAKind),
            ("TwoPair",       new List<CardDataOuts> { C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts), C(Rank.Queen, Suit.Diamonds), C(Rank.Queen, Suit.Clubs), C(Rank.Two, Suit.Spades) }, HandRank.TwoPair),
            ("OnePair",       new List<CardDataOuts> { C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs), C(Rank.Jack, Suit.Spades) }, HandRank.OnePair),
            ("HighCard",      new List<CardDataOuts> { C(Rank.Two, Suit.Spades), C(Rank.Five, Suit.Hearts), C(Rank.Eight, Suit.Diamonds), C(Rank.Jack, Suit.Clubs), C(Rank.Ace, Suit.Spades) }, HandRank.HighCard),
        };

        foreach (var (desc, cards, expectedRank) in manos)
        {
            var bitResult = _evaluator.EvaluateBestHand(cards);
            var refResult = _referencia.EvaluateBestHand(cards);
            Assert.That(bitResult.Rank, Is.EqualTo(expectedRank), $"BitHandEvaluator: {desc} - Rank incorrecto");
            Assert.That(bitResult.Score, Is.EqualTo(refResult.Score), $"BitHandEvaluator: {desc} - Score difiere de referencia");
        }
    }
}
