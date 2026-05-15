using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
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

    // ─── Tests de VillainRange ─────────────────────────────────────────

    [Test]
    public void CalculateEquity_ConRangoVillano_DeberiaRetornarResultadoValido()
    {
        // Hero: AKs en flop seco → vs rango de 3Bettor debería tener equity razonable
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Spades)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Nine, Suit.Clubs)
        };

        var range = VillainRange.GetForSituation(HandSituation.OpenRaiseVs3Bet);
        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 3000, range);

        Assert.That(result.Equity, Is.InRange(0.10, 0.90));
        Assert.That(result.Simulations, Is.EqualTo(3000));
        var total = result.WinProbability + result.TieProbability + result.LoseProbability;
        Assert.That(total, Is.InRange(0.99, 1.01));
    }

    [Test]
    public void CalculateEquity_RangoFuerte_MenosEquityQueAleatorio()
    {
        // Hero: mano marginal (JTo) en flop seco
        // Vs rango fuerte (4Bet pot) debería tener MENOS equity que vs aleatorio
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Spades),
            C(Rank.Ten, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Clubs),
            C(Rank.Five, Suit.Diamonds),
            C(Rank.Eight, Suit.Hearts)
        };

        var rangoFuerte = VillainRange.GetForSituation(HandSituation.FourBet);
        var resultVsRango = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, rangoFuerte);
        var resultVsAleatorio = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, null);

        // Vs rango fuerte (AA,KK,QQ,AK) debería tener menos equity
        Assert.That(resultVsRango.Equity, Is.LessThan(resultVsAleatorio.Equity));
    }

    [Test]
    public void CalculateEquity_RangoNulo_ComportamientoOriginal()
    {
        // Sin rango → mismo comportamiento que antes
        var myCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 2000, null);

        Assert.That(result.Equity, Is.InRange(0.75, 0.95));
    }

    [Test]
    public void CalculateEquity_RangoLimper_MasEquityQueVs3Bettor()
    {
        // Hero: AQs → vs limper (rango débil) debería tener MÁS equity que vs 3Bettor (rango fuerte)
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Three, Suit.Clubs),
            C(Rank.Six, Suit.Diamonds),
            C(Rank.Nine, Suit.Spades)
        };

        var rangoLimper = VillainRange.GetForSituation(HandSituation.RaiseOverLimper);
        var rango3Bettor = VillainRange.GetForSituation(HandSituation.OpenRaiseVs3Bet);

        var resultVsLimper = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, rangoLimper);
        var resultVs3Bettor = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, rango3Bettor);

        Assert.That(resultVsLimper.Equity, Is.GreaterThan(resultVs3Bettor.Equity));
    }

    /// <summary>
    /// Reproduce bug: AQo preflop vs _callerVs3Bet debería dar ~50-55%, no 19.7%.
    /// </summary>
    [Test]
    public void CalculateEquity_AQoPreflop_VsCallerVs3Bet_DeberiaSerRazonable()
    {
        var heroCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Queen, Suit.Clubs)
        };
        var communityCards = new List<CardDataOuts>();
        var villainRange = VillainRange.GetForSituation(HandSituation.ThreeBet);

        var result = _simulator.CalculateEquity(heroCards, communityCards, 1, 5000, villainRange);

        // AQo vs callerVs3Bet (10%) debería ser ~50-55% equity
        Assert.That(result.Equity, Is.InRange(0.40, 0.65),
            $"AQo vs callerVs3Bet: equity={result.Equity:P1}, esperado 45-60%. " +
            $"Win={result.WinProbability:P1}, Tie={result.TieProbability:P1}, Loss={result.LoseProbability:P1}, " +
            $"Sims={result.Simulations}");
    }

    // ─── Tests de enumeración exacta ─────────────────────────────

    [Test]
    public void CalculateEquity_River_EnumeracionExacta_QuadAces()
    {
        // River completo: AA en board A A K 7 2 → quads, equity ~99%+
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.Ace, Suit.Clubs),
            C(Rank.King, Suit.Spades),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Two, Suit.Clubs)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1);

        // Enumeración exacta → equity determinística, siempre el mismo valor
        Assert.That(result.Equity, Is.GreaterThanOrEqualTo(0.99));
        // Simulations debe ser C(45,2) = 990
        Assert.That(result.Simulations, Is.EqualTo(990));
    }

    [Test]
    public void CalculateEquity_River_EnumeracionExacta_EsDeterminista()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.Queen, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Three, Suit.Clubs),
            C(Rank.Nine, Suit.Spades),
            C(Rank.Two, Suit.Hearts)
        };

        var result1 = _simulator.CalculateEquity(myCards, communityCards, 1);
        var result2 = _simulator.CalculateEquity(myCards, communityCards, 1);

        // Enumeración exacta → resultados idénticos entre ejecuciones
        Assert.That(result1.Equity, Is.EqualTo(result2.Equity));
        Assert.That(result1.WinProbability, Is.EqualTo(result2.WinProbability));
    }

    [Test]
    public void CalculateEquity_Turn_EnumeracionExacta_TopPair()
    {
        // Turn: KQo en board K 7 3 9 → top pair, equity razonable
        var myCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades),
            C(Rank.Queen, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Three, Suit.Clubs),
            C(Rank.Nine, Suit.Spades)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1);

        Assert.That(result.Equity, Is.InRange(0.60, 0.90));
        // Debe ser enumeración exacta (no MC), así que determinístico
        var result2 = _simulator.CalculateEquity(myCards, communityCards, 1);
        Assert.That(result.Equity, Is.EqualTo(result2.Equity));
    }

    [Test]
    public void CalculateEquity_Turn_ConRango_EnumeracionExacta()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Spades)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Two, Suit.Clubs),
            C(Rank.Nine, Suit.Hearts)
        };

        var range = VillainRange.GetForSituation(HandSituation.ThreeBet);
        var result = _simulator.CalculateEquity(myCards, communityCards, 1, null, range);

        Assert.That(result.Equity, Is.InRange(0.40, 0.95));
        // Determinístico con rango
        var result2 = _simulator.CalculateEquity(myCards, communityCards, 1, null, range);
        Assert.That(result.Equity, Is.EqualTo(result2.Equity));
    }

    [Test]
    public void CalculateEquity_Flop_UsaIteracionesAdaptativas()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Jack, Suit.Spades),
            C(Rank.Ten, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Clubs),
            C(Rank.Five, Suit.Diamonds),
            C(Rank.Eight, Suit.Hearts)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1);

        // Flop adaptativo: 50K iteraciones (menos skipped)
        Assert.That(result.Simulations, Is.GreaterThanOrEqualTo(45_000));
    }

    [Test]
    public void CalculateEquity_Flop_MismaSemilla_EsDeterminista()
    {
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

        var result1 = _simulator.CalculateEquity(myCards, communityCards, 1, 2000, null, randomSeed: 12345);
        var result2 = _simulator.CalculateEquity(myCards, communityCards, 1, 2000, null, randomSeed: 12345);

        Assert.That(result1.RandomSeed, Is.EqualTo(12345));
        Assert.That(result2.RandomSeed, Is.EqualTo(12345));
        Assert.That(result1.Equity, Is.EqualTo(result2.Equity));
        Assert.That(result1.WinProbability, Is.EqualTo(result2.WinProbability));
        Assert.That(result1.TieProbability, Is.EqualTo(result2.TieProbability));
        Assert.That(result1.LoseProbability, Is.EqualTo(result2.LoseProbability));
        Assert.That(result1.HandDistribution, Is.EqualTo(result2.HandDistribution));
    }

    [Test]
    public void CalculateEquity_FlopConRango_MismaSemilla_EsDeterminista()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Spades)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Nine, Suit.Clubs)
        };
        var range = VillainRange.GetForSituation(HandSituation.OpenRaiseVs3Bet);

        var result1 = _simulator.CalculateEquity(myCards, communityCards, 1, 2000, range, randomSeed: 67890);
        var result2 = _simulator.CalculateEquity(myCards, communityCards, 1, 2000, range, randomSeed: 67890);

        Assert.That(result1.RandomSeed, Is.EqualTo(67890));
        Assert.That(result2.RandomSeed, Is.EqualTo(67890));
        Assert.That(result1.Equity, Is.EqualTo(result2.Equity));
        Assert.That(result1.WinProbability, Is.EqualTo(result2.WinProbability));
        Assert.That(result1.TieProbability, Is.EqualTo(result2.TieProbability));
        Assert.That(result1.LoseProbability, Is.EqualTo(result2.LoseProbability));
        Assert.That(result1.SkippedSimulations, Is.EqualTo(result2.SkippedSimulations));
        Assert.That(result1.HandDistribution, Is.EqualTo(result2.HandDistribution));
    }

    [Test]
    public void CalculateEquity_SemillaDeSesion_EsDeterminista()
    {
        var simulator1 = new MonteCarloSimulator(24680);
        var simulator2 = new MonteCarloSimulator(24680);
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Queen, Suit.Spades),
            C(Rank.Jack, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>();

        var result1 = simulator1.CalculateEquity(myCards, communityCards, 1, 2000);
        var result2 = simulator2.CalculateEquity(myCards, communityCards, 1, 2000);

        Assert.That(result1.RandomSeed, Is.EqualTo(24680));
        Assert.That(result2.RandomSeed, Is.EqualTo(24680));
        Assert.That(result1.Equity, Is.EqualTo(result2.Equity));
        Assert.That(result1.WinProbability, Is.EqualTo(result2.WinProbability));
        Assert.That(result1.TieProbability, Is.EqualTo(result2.TieProbability));
        Assert.That(result1.LoseProbability, Is.EqualTo(result2.LoseProbability));
        Assert.That(result1.HandDistribution, Is.EqualTo(result2.HandDistribution));
    }

    [Test]
    public void CalculateEquity_VillainRangeBloqueado_NoContaminaEquity()
    {
        // Rango muy reducido (4Bet: AA,KK,QQ,AK) con hero AK en board AAKK
        // Muchos combos bloqueados → iteraciones skipped, equity no contaminada
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.King, Suit.Spades)
        };

        var range = VillainRange.GetForSituation(HandSituation.FourBet);
        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, range);

        // Debe tener equity válida (no NaN o 0 por fallback corrupto)
        Assert.That(result.Equity, Is.InRange(0.01, 0.99));
        var total = result.WinProbability + result.TieProbability + result.LoseProbability;
        Assert.That(total, Is.InRange(0.99, 1.01));
    }

    [Test]
    public void CalculateEquity_RangoConAltoBloqueo_MarcaComoNoConfiable()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.King, Suit.Spades)
        };

        var range = VillainRange.GetForSituation(HandSituation.FourBet);
        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, range);

        Assert.That(result.BlockedComboPercentage, Is.GreaterThan(0));
        Assert.That(result.IsReliable, Is.False);
    }

    [Test]
    public void CalculateEquity_SinRango_EsConfiable()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Diamonds),
            C(Rank.Ten, Suit.Spades)
        };

        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 3000, null);

        Assert.That(result.IsReliable, Is.True);
        Assert.That(result.BlockedComboPercentage, Is.EqualTo(0));
    }

    [Test]
    public void CalculateEquity_RangoSinBloqueo_EsConfiable()
    {
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Spades),
            C(Rank.Three, Suit.Hearts)
        };
        var communityCards = new List<CardDataOuts>
        {
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Five, Suit.Diamonds),
            C(Rank.Two, Suit.Spades)
        };

        var range = VillainRange.GetForSituation(HandSituation.OpenRaise);
        var result = _simulator.CalculateEquity(myCards, communityCards, 1, 5000, range);

        Assert.That(result.BlockedComboPercentage, Is.LessThan(0.20));
        Assert.That(result.IsReliable, Is.True);
    }
}

// ─── Tests de HandScore struct ───────────────────────────────

[TestFixture]
public class HandScoreTests
{
    private BitHandEvaluator _evaluator;

    [SetUp]
    public void Setup()
    {
        _evaluator = new BitHandEvaluator();
    }

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    [Test]
    public void EvaluateHandScore_MismoRankQueEvaluateBestHand()
    {
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.Seven, Suit.Clubs),
            C(Rank.Three, Suit.Hearts), C(Rank.Nine, Suit.Spades),
            C(Rank.Two, Suit.Clubs)
        };

        var full = _evaluator.EvaluateBestHand(cards);
        var score = _evaluator.EvaluateHandScore(cards);

        Assert.That(score.Rank, Is.EqualTo(full.Rank));
    }

    [Test]
    public void HandScore_MejorManoGana_CompareTo()
    {
        // Flush > Straight
        var flush = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts), C(Rank.Ten, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts), C(Rank.Three, Suit.Hearts),
            C(Rank.Two, Suit.Hearts), C(Rank.King, Suit.Clubs),
            C(Rank.Nine, Suit.Diamonds)
        };
        var straight = new List<CardDataOuts>
        {
            C(Rank.Ten, Suit.Spades), C(Rank.Nine, Suit.Hearts),
            C(Rank.Eight, Suit.Diamonds), C(Rank.Seven, Suit.Clubs),
            C(Rank.Six, Suit.Hearts), C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };

        var flushScore = _evaluator.EvaluateHandScore(flush);
        var straightScore = _evaluator.EvaluateHandScore(straight);

        Assert.That(flushScore.CompareTo(straightScore), Is.GreaterThan(0));
    }

    [Test]
    public void HandScore_KickersDesempatan()
    {
        // OnePair de Aces con K kicker vs OnePair de Aces con Q kicker
        var aceK = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.Ace, Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.Seven, Suit.Clubs),
            C(Rank.Three, Suit.Hearts)
        };
        var aceQ = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Diamonds), C(Rank.Ace, Suit.Clubs),
            C(Rank.Queen, Suit.Spades), C(Rank.Seven, Suit.Hearts),
            C(Rank.Three, Suit.Clubs)
        };

        var scoreK = _evaluator.EvaluateHandScore(aceK);
        var scoreQ = _evaluator.EvaluateHandScore(aceQ);

        Assert.That(scoreK.CompareTo(scoreQ), Is.GreaterThan(0));
    }

    [Test]
    public void HandScore_DetectaTodosLosRanks()
    {
        // Straight Flush
        var sf = new List<CardDataOuts>
        {
            C(Rank.Nine, Suit.Hearts), C(Rank.Eight, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts), C(Rank.Six, Suit.Hearts),
            C(Rank.Five, Suit.Hearts), C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };
        Assert.That(_evaluator.EvaluateHandScore(sf).Rank, Is.EqualTo(HandRank.StraightFlush));

        // Four of a Kind
        var quads = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.King, Suit.Clubs),
            C(Rank.Ace, Suit.Spades), C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };
        Assert.That(_evaluator.EvaluateHandScore(quads).Rank, Is.EqualTo(HandRank.FourOfAKind));

        // Full House
        var fh = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts),
            C(Rank.King, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Queen, Suit.Spades), C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };
        Assert.That(_evaluator.EvaluateHandScore(fh).Rank, Is.EqualTo(HandRank.FullHouse));

        // Two Pair
        var tp = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Spades), C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Diamonds), C(Rank.Queen, Suit.Clubs),
            C(Rank.Ace, Suit.Spades), C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };
        Assert.That(_evaluator.EvaluateHandScore(tp).Rank, Is.EqualTo(HandRank.TwoPair));

        // High Card
        var hc = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades), C(Rank.King, Suit.Hearts),
            C(Rank.Nine, Suit.Diamonds), C(Rank.Seven, Suit.Clubs),
            C(Rank.Three, Suit.Spades), C(Rank.Two, Suit.Clubs),
            C(Rank.Four, Suit.Diamonds)
        };
        Assert.That(_evaluator.EvaluateHandScore(hc).Rank, Is.EqualTo(HandRank.HighCard));
    }
}

[TestFixture]
public class VillainRangeTests
{
    [Test]
    public void GetForSituation_None_DeberiaRetornarNull()
    {
        var range = VillainRange.GetForSituation(HandSituation.None);
        Assert.That(range, Is.Null);
    }

    [Test]
    public void GetForSituation_OpenRaise_DeberiaRetornarRango()
    {
        var range = VillainRange.GetForSituation(HandSituation.OpenRaise);
        Assert.That(range, Is.Not.Null);
        Assert.That(range!.Hands.Count, Is.GreaterThan(10));
        Assert.That(range.RangePercentage, Is.EqualTo(25.0));
    }

    [Test]
    public void GetForSituation_FourBet_DeberiaSerRangoPequeno()
    {
        var range = VillainRange.GetForSituation(HandSituation.FourBet);
        Assert.That(range, Is.Not.Null);
        Assert.That(range!.Hands.Count, Is.LessThan(10));
        Assert.That(range.RangePercentage, Is.EqualTo(5.0));
    }

    [Test]
    public void ExpandHandNotation_Par_Deberia6Combos()
    {
        var combos = VillainRange.ExpandHandNotation("AA");
        Assert.That(combos.Count, Is.EqualTo(6));
    }

    [Test]
    public void ExpandHandNotation_Suited_Deberia4Combos()
    {
        var combos = VillainRange.ExpandHandNotation("AKs");
        Assert.That(combos.Count, Is.EqualTo(4));
        // Todas deben tener mismo suit
        foreach (var (c1, c2) in combos)
        {
            Assert.That(c1.Suit, Is.EqualTo(c2.Suit));
        }
    }

    [Test]
    public void ExpandHandNotation_Offsuit_Deberia12Combos()
    {
        var combos = VillainRange.ExpandHandNotation("AKo");
        Assert.That(combos.Count, Is.EqualTo(12));
        // Todas deben tener suits diferentes
        foreach (var (c1, c2) in combos)
        {
            Assert.That(c1.Suit, Is.Not.EqualTo(c2.Suit));
        }
    }

    [Test]
    public void ExpandHandNotation_Invalida_DeberiaRetornarVacio()
    {
        var combos = VillainRange.ExpandHandNotation("X");
        Assert.That(combos.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetForSituation_TodasSituaciones_DeberianTenerFrecuenciasValidas()
    {
        foreach (HandSituation situation in Enum.GetValues(typeof(HandSituation)))
        {
            var range = VillainRange.GetForSituation(situation);
            if (range == null) continue; // None es válido

            foreach (var (hand, freq) in range.Hands)
            {
                Assert.That(freq, Is.InRange(0.0, 1.0),
                    $"Frecuencia inválida para {hand} en {situation}: {freq}");
            }
        }
    }

    // ─── H3 — Rangos adaptativos por OpponentProfile ─────────────

    [Test]
    public void VpipMultiplier_VPIPNormal_RetornaCercaDeUno()
    {
        // Rango base 25%, VPIP observado 25% → multiplier ≈ 1.0
        double mult = VillainRange.CalculateVpipMultiplier(25.0, 25.0);
        Assert.That(mult, Is.EqualTo(1.0).Within(0.01));
    }

    [Test]
    public void VpipMultiplier_VPIPAlto_RetornaMayorQueUno()
    {
        // Rango base 25%, VPIP observado 40% → multiplier = 40/25 = 1.6
        double mult = VillainRange.CalculateVpipMultiplier(25.0, 40.0);
        Assert.That(mult, Is.EqualTo(1.6).Within(0.01));
    }

    [Test]
    public void VpipMultiplier_VPIPBajo_RetornaMenorQueUno()
    {
        // Rango base 25%, VPIP observado 15% → multiplier = 15/25 = 0.6
        double mult = VillainRange.CalculateVpipMultiplier(25.0, 15.0);
        Assert.That(mult, Is.EqualTo(0.6).Within(0.01));
    }

    [Test]
    public void VpipMultiplier_ClampMaximo_NoExcede2()
    {
        // VPIP 80% con rango 10% → ratio 8.0 → clamped a 2.0
        double mult = VillainRange.CalculateVpipMultiplier(10.0, 80.0);
        Assert.That(mult, Is.EqualTo(2.0));
    }

    [Test]
    public void VpipMultiplier_ClampMinimo_NoMenorQue05()
    {
        // VPIP 5% con rango 25% → ratio 0.2 → clamped a 0.5
        double mult = VillainRange.CalculateVpipMultiplier(25.0, 5.0);
        Assert.That(mult, Is.EqualTo(0.5));
    }

    [Test]
    public void GetForSituation_ConProfile_AjustaRangoPorVPIP()
    {
        var profile = new OpponentProfile
        {
            HandsPlayed = 50,
            TimesVoluntarilyPutMoneyIn = 20 // VPIP = 40%
        };

        var baseRange = VillainRange.GetForSituation(HandSituation.OpenRaise, TablePosition.CutOff);
        var adaptiveRange = VillainRange.GetForSituation(HandSituation.OpenRaise, TablePosition.CutOff, profile);

        Assert.That(adaptiveRange, Is.Not.Null);
        Assert.That(adaptiveRange!.RangePercentage, Is.GreaterThan(baseRange!.RangePercentage),
            "VPIP 40% con rango base 25% → rango adaptativo más amplio");
    }

    [Test]
    public void GetForSituation_ConProfileSinDatos_RetornaRangoBase()
    {
        var profile = new OpponentProfile { HandsPlayed = 5 }; // < 10, no fiable

        var baseRange = VillainRange.GetForSituation(HandSituation.OpenRaise, TablePosition.CutOff);
        var adaptiveRange = VillainRange.GetForSituation(HandSituation.OpenRaise, TablePosition.CutOff, profile);

        // Sin datos fiables → retorna rango base sin cambios
        Assert.That(adaptiveRange!.RangePercentage, Is.EqualTo(baseRange!.RangePercentage));
    }

    [Test]
    public void GetForSituation_3Bet_AjustaPor3BetPct()
    {
        var profile = new OpponentProfile
        {
            HandsPlayed = 50,
            TimesVoluntarilyPutMoneyIn = 15, // VPIP 30%
            TimesThreeBet = 8                 // 3Bet% = 16% (alto, ~2.7x del promedio 6%)
        };

        var baseRange = VillainRange.GetForSituation(HandSituation.OpenRaiseVs3Bet, TablePosition.Button);
        var adaptiveRange = VillainRange.GetForSituation(HandSituation.OpenRaiseVs3Bet, TablePosition.Button, profile);

        Assert.That(adaptiveRange, Is.Not.Null);
        Assert.That(adaptiveRange!.RangePercentage, Is.GreaterThan(baseRange!.RangePercentage),
            "3Bet% 16% (alto) → rango de 3bettor más amplio");
    }

    [Test]
    public void GetForSituation_ProfileNull_RetornaRangoConPosicion()
    {
        var range = VillainRange.GetForSituation(HandSituation.OpenRaise, TablePosition.Button, null);
        var posRange = VillainRange.GetForSituation(HandSituation.OpenRaise, TablePosition.Button);

        Assert.That(range!.RangePercentage, Is.EqualTo(posRange!.RangePercentage));
    }
}
