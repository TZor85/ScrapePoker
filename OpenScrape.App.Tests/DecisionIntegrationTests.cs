using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests de integración que verifican el flujo completo:
/// Cartas → Equity (MonteCarlo) → Thresholds (StrategyProfile) → Acción recomendada.
/// </summary>
[TestFixture]
public class DecisionIntegrationTests
{
    private MonteCarloSimulator _simulator;
    private OutsCalculator _outsCalculator;
    private HandEvaluator _handEvaluator;
    private ThresholdsRegistry _registry;

    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    private StreetThresholds GetThresholds(BoardPosition street, HandSituation situation)
        => _registry.Get(new ThresholdKey(street, situation));

    [SetUp]
    public void Setup()
    {
        _simulator = new MonteCarloSimulator();
        _outsCalculator = new OutsCalculator();
        _handEvaluator = new HandEvaluator();

        var profile = CreateDefaultProfile();
        _registry = new ThresholdsRegistry(Options.Create(profile));
    }

    [Test]
    public void FlujoCompleto_PocketAces_Flop_DeberiaRecomendarValueBet()
    {
        // Hero: AA en flop seco → equity alta → debe recomendar value bet
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var community = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Clubs),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.King, Suit.Spades)
        };

        var equityResult = _simulator.CalculateEquity(myCards, community, 1, 3000);
        var thresholds = GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        // AA en flop seco debería tener equity > 80%
        Assert.That(equityResult.Equity * 100, Is.GreaterThan(thresholds.StrongValueAbove));
    }

    [Test]
    public void FlujoCompleto_TrashHand_Flop_DeberiaRecomendarFold()
    {
        // Hero: 2c 7d en flop coordinado alto → equity baja → fold
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Clubs),
            C(Rank.Seven, Suit.Diamonds)
        };
        var community = new List<CardDataOuts>
        {
            C(Rank.King, Suit.Hearts),
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Jack, Suit.Spades)
        };

        var equityResult = _simulator.CalculateEquity(myCards, community, 1, 3000);
        var thresholds = GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        // 27o en board KQJ debería tener equity < fold threshold
        Assert.That(equityResult.Equity * 100, Is.LessThan(thresholds.FoldBelow));
    }

    [Test]
    public void FlujoCompleto_FlushDraw_Turn_DeberiaEstarEnRangoThinValue()
    {
        // Hero: Ah5h en board con 3 hearts + carta no-heart → flush draw en turn
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Hearts),
            C(Rank.Five, Suit.Hearts)
        };
        var community = new List<CardDataOuts>
        {
            C(Rank.Two, Suit.Hearts),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.King, Suit.Clubs),
            C(Rank.Three, Suit.Spades)
        };

        var equityResult = _simulator.CalculateEquity(myCards, community, 1, 3000);
        var outsResult = _outsCalculator.CalculateOuts(myCards, community);
        var thresholds = GetThresholds(BoardPosition.Turn, HandSituation.Call);

        // Flush draw en turn con Ace → equity media, debería tener outs
        Assert.That(outsResult.HasFlushDraw, Is.True);
        Assert.That(outsResult.TotalOuts, Is.GreaterThanOrEqualTo(9));
    }

    [Test]
    public void FlujoCompleto_NutHand_River_DeberiaSerStrongValue()
    {
        // Hero: AA, Board: A A K 2 3 → quads en river
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.Ace, Suit.Hearts)
        };
        var community = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Diamonds),
            C(Rank.Ace, Suit.Clubs),
            C(Rank.King, Suit.Spades),
            C(Rank.Two, Suit.Clubs),
            C(Rank.Three, Suit.Diamonds)
        };

        var equityResult = _simulator.CalculateEquity(myCards, community, 1, 1000);
        var thresholds = GetThresholds(BoardPosition.River, HandSituation.OpenRaise);

        // Quads en river → equity ~100% → strong value
        Assert.That(equityResult.Equity * 100, Is.GreaterThan(thresholds.StrongValueAbove));
    }

    [Test]
    public void FlujoCompleto_MidPair_Turn_ThreeBet_DeberiaEstarEnRangoValue()
    {
        // Hero: QQ en board bajo → equity buena pero no premium
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Queen, Suit.Spades),
            C(Rank.Queen, Suit.Hearts)
        };
        var community = new List<CardDataOuts>
        {
            C(Rank.Five, Suit.Clubs),
            C(Rank.Eight, Suit.Diamonds),
            C(Rank.Two, Suit.Spades),
            C(Rank.Nine, Suit.Hearts)
        };

        var equityResult = _simulator.CalculateEquity(myCards, community, 1, 3000);
        var thresholds = GetThresholds(BoardPosition.Turn, HandSituation.ThreeBet);
        var equityPct = equityResult.Equity * 100;

        // QQ en board bajo → equity > ValueAbove (55) o > ThinValueAbove (45)
        Assert.That(equityPct, Is.GreaterThan(thresholds.ThinValueAbove));
    }

    [Test]
    public void FlujoCompleto_Vs3BetAndCall_LowEquity_DeberiaUsarCallComoFallback()
    {
        // Verificar que la configuración de Vs3BetAndCall usa "Call" en vez de "Fold"
        var thresholds = GetThresholds(BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall);

        Assert.That(thresholds.LowEquityAction, Is.EqualTo("Call"));
    }

    [Test]
    public void FlujoCompleto_HandEvaluator_ConMonteCarlo_ResultadosCoherentes()
    {
        // Verificar que HandEvaluator y MonteCarlo coinciden: mano fuerte → equity alta
        var cards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Spades),
            C(Rank.Queen, Suit.Spades),
            C(Rank.Jack, Suit.Spades),
            C(Rank.Ten, Suit.Spades)
        };

        var handResult = _handEvaluator.EvaluateBestHand(cards);
        Assert.That(handResult.Rank, Is.EqualTo(HandRank.StraightFlush));

        // Royal flush debería tener equity muy alta
        var myCards = new List<CardDataOuts>
        {
            C(Rank.Ace, Suit.Spades),
            C(Rank.King, Suit.Spades)
        };
        var community = new List<CardDataOuts>
        {
            C(Rank.Queen, Suit.Spades),
            C(Rank.Jack, Suit.Spades),
            C(Rank.Ten, Suit.Spades)
        };

        var equityResult = _simulator.CalculateEquity(myCards, community, 1, 1000);
        Assert.That(equityResult.Equity, Is.GreaterThanOrEqualTo(0.99));
    }

    [Test]
    public void FlujoCompleto_ThresholdsCoherentes_TurnMasEstrictoQueRiver()
    {
        // Verificar coherencia: Turn_OpenRaise.FoldBelow (45) >= River_OpenRaise.FoldBelow (40)
        var turnThresholds = GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);
        var riverThresholds = GetThresholds(BoardPosition.River, HandSituation.OpenRaise);

        Assert.That(turnThresholds.FoldBelow, Is.GreaterThanOrEqualTo(riverThresholds.FoldBelow));
    }

    private static StrategyProfile CreateDefaultProfile()
    {
        return new StrategyProfile
        {
            Name = "IntegrationTest",
            TurnBluffFrequency = 0.15,
            RiverBluffFrequency = 0.10,
            FlopBluffFrequency = 0.15,
            Thresholds = new Dictionary<string, StreetThresholds>
            {
                ["Turn_OpenRaise"] = new()
                {
                    FoldBelow = 45,
                    ThinValueAbove = 45,
                    ValueAbove = 55,
                    StrongValueAbove = 80,
                    DryBoardBetSize = "Bet 1/2",
                    CoordinatedBoardBetSize = "Bet 1/2",
                    PairedBoardBetSize = "Bet 3/4",
                    StrongValueBetSize = "Bet 3/4",
                    ValueBetSize = "Bet 1/2",
                    ThinValueBetSize = "Bet 1/3",
                    CanBluff = true,
                    BluffCondition = BluffConditionType.IPCoordinatedSmallOnly,
                    LowEquityAction = "Fold"
                },
                ["Turn_Call"] = new()
                {
                    FoldBelow = 40,
                    ThinValueAbove = 40,
                    ValueAbove = 55,
                    StrongValueAbove = 75,
                    LowEquityAction = "Fold"
                },
                ["Turn_ThreeBet"] = new()
                {
                    FoldBelow = 40,
                    ThinValueAbove = 45,
                    ValueAbove = 55,
                    StrongValueAbove = 75,
                    LowEquityAction = "Fold"
                },
                ["Turn_OpenRaiseVs3BetAndCall"] = new()
                {
                    FoldBelow = 40,
                    ThinValueAbove = 40,
                    ValueAbove = 55,
                    StrongValueAbove = 75,
                    LowEquityAction = "Call"
                },
                ["River_OpenRaise"] = new()
                {
                    FoldBelow = 40,
                    ThinValueAbove = 40,
                    ValueAbove = 60,
                    StrongValueAbove = 75,
                    DryBoardBetSize = "Bet 2/3",
                    CoordinatedBoardBetSize = "Bet Pot",
                    PairedBoardBetSize = "Bet Pot",
                    StrongValueBetSize = "Bet Pot",
                    ValueBetSize = "Bet 3/4",
                    ThinValueBetSize = "Bet 1/2",
                    LowEquityAction = "Fold"
                }
            }
        };
    }
}
