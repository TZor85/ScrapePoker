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
}
