using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

[TestFixture]
public class ImpliedOddsCalculatorTests
{
    private StrategyProfile _profile;

    [SetUp]
    public void Setup()
    {
        _profile = new StrategyProfile();
    }

    #region CalculateImpliedOddsFactor

    [Test]
    public void ImpliedOdds_River_DeberiaRetornar1()
    {
        var result = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.River, true, false, 100m, 50m, _profile);

        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void ImpliedOdds_StackCero_DeberiaRetornar1()
    {
        var result = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, true, false, 0m, 50m, _profile);

        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void ImpliedOdds_PotCero_DeberiaRetornar1()
    {
        var result = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, true, false, 100m, 0m, _profile);

        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void ImpliedOdds_SPRDeep_DeberiaUsarFactorDeep()
    {
        // SPR = 500/50 = 10 → deep (>= 4.0)
        var result = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 500m, 50m, _profile);

        // DeepFactor(0.65) * FlopMultiplier(0.90) = 0.585
        Assert.That(result, Is.InRange(0.50, 0.70));
    }

    [Test]
    public void ImpliedOdds_SPRShallow_DeberiaUsarFactorShallow()
    {
        // SPR = 50/50 = 1.0 → shallow (<= 2.0)
        var result = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 50m, 50m, _profile);

        // ShallowFactor(0.95) * FlopMultiplier(0.90) = 0.855
        Assert.That(result, Is.InRange(0.80, 0.95));
    }

    [Test]
    public void ImpliedOdds_SPRMedio_DeberiaInterpolar()
    {
        // SPR = 150/50 = 3.0 → entre shallow(2.0) y deep(4.0)
        var result = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 150m, 50m, _profile);

        // Interpolación: entre shallow y deep factor
        var deep = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 500m, 50m, _profile);
        var shallow = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 50m, 50m, _profile);

        Assert.That(result, Is.InRange(deep, shallow));
    }

    [Test]
    public void ImpliedOdds_IP_DeberiaSerMenorQueOOP()
    {
        var ip = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, true, false, 200m, 50m, _profile);
        var oop = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 200m, 50m, _profile);

        // IP tiene bonus → factor menor = mejores implied odds
        Assert.That(ip, Is.LessThanOrEqualTo(oop));
    }

    [Test]
    public void ImpliedOdds_FlushDraw_DeberiaSerMenorQueSinDraw()
    {
        var conDraw = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, true, 200m, 50m, _profile);
        var sinDraw = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 200m, 50m, _profile);

        Assert.That(conDraw, Is.LessThanOrEqualTo(sinDraw));
    }

    [Test]
    public void ImpliedOdds_Flop_DeberiaSerMenorQueTurn()
    {
        var flop = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, false, false, 200m, 50m, _profile);
        var turn = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Turn, false, false, 200m, 50m, _profile);

        // Flop tiene mejor implied odds (2 calles) → factor menor
        Assert.That(flop, Is.LessThanOrEqualTo(turn));
    }

    [Test]
    public void ImpliedOdds_ResultadoSiempreEntre050Y1()
    {
        var extremo = ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            BoardPosition.Flop, true, true, 5000m, 10m, _profile);

        Assert.That(extremo, Is.InRange(0.50, 1.0));
    }

    #endregion

    #region CalculateReverseImpliedOdds

    [Test]
    public void ReverseImplied_NoTurn_DeberiaRetornar0()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        var result = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Flop, true, _profile);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ReverseImplied_NoFacingBet_DeberiaRetornar0()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        var result = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, false, _profile);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ReverseImplied_BoardChangeNull_DeberiaRetornar0()
    {
        var result = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            null, HandRank.OnePair, false, BoardPosition.Turn, true, _profile);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ReverseImplied_HeroFuerteThreeOfAKind_DeberiaRetornar0()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 3);

        var result = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.ThreeOfAKind, false, BoardPosition.Turn, true, _profile);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ReverseImplied_FlushDrawAppeared_SinHeroDraw_DeberiaPenalizar()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        var result = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.TwoPair, false, BoardPosition.Turn, true, _profile);

        Assert.That(result, Is.GreaterThan(0));
    }

    [Test]
    public void ReverseImplied_FlushDrawAppeared_ConHeroDraw_NoDeberiaPenalizar()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 1);

        var result = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.TwoPair, true, BoardPosition.Turn, true, _profile);

        // DangerLevel < 2 y hero tiene flush draw → no penalty
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void ReverseImplied_OnePairOverpair_MultiplicadorMenorQueBottomPair()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        var overpair = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            PairClassification.Overpair);

        var bottomPair = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            PairClassification.BottomPair);

        Assert.That(overpair, Is.LessThan(bottomPair));
    }

    [Test]
    public void ReverseImplied_OnePairBoardPaired_MultiplicadorMaximo()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        var boardPaired = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            PairClassification.BoardPaired);

        var topPair = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            PairClassification.TopPair);

        Assert.That(boardPaired, Is.GreaterThan(topPair));
    }

    #endregion

    #region S20.3 — Bluff Risk Penalty

    [Test]
    public void BluffRisk_TurnOnePair_WithDraws_AddsPenalty()
    {
        var change = new BoardChangeResult(false, FlushDrawAppeared: true, false, false, false, -1, 2);

        double penalty = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.Unknown, heroStack: 50m, potSize: 20m);

        // Base reverse + bluff risk (0.30 × 0.55 × 20/(20+50)) ≈ 0.047
        Assert.That(penalty, Is.GreaterThan(0),
            "Turn OnePair con flush draw → penalty incluye bluff risk");
    }

    [Test]
    public void BluffRisk_LAG_AmplificaPenalty()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        double penaltyLAG = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m);
        double penaltyUnknown = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.Unknown, heroStack: 50m, potSize: 20m);

        Assert.That(penaltyLAG, Is.GreaterThan(penaltyUnknown),
            "LAG amplifica bluff risk ×1.5");
    }

    [Test]
    public void BluffRisk_TP_ReducePenalty()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        double penaltyTP = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.TP, heroStack: 50m, potSize: 20m);
        double penaltyUnknown = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.Unknown, heroStack: 50m, potSize: 20m);

        Assert.That(penaltyTP, Is.LessThan(penaltyUnknown),
            "TP reduce bluff risk ×0.5");
    }

    [Test]
    public void BluffRisk_AllIn_NoPenalty()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        double penalty = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m,
            isAnyoneAllIn: true);

        // All-in: no bluff risk (base reverse puede seguir existiendo, pero bluff risk = 0)
        double penaltyNoAllIn = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m,
            isAnyoneAllIn: false);

        Assert.That(penalty, Is.LessThan(penaltyNoAllIn),
            "All-in elimina bluff risk component");
    }

    [Test]
    public void BluffRisk_ThreeOfAKind_NoPenalty()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        double penalty = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.ThreeOfAKind, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m);

        // ThreeOfAKind > TwoPair → no reverse implied odds (skip early)
        Assert.That(penalty, Is.EqualTo(0),
            "ThreeOfAKind+ no tiene reverse implied ni bluff risk");
    }

    [Test]
    public void BluffRisk_River_NoPenalty()
    {
        var change = new BoardChangeResult(false, true, false, false, false, -1, 2);

        double penaltyTurn = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m);
        double penaltyRiver = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.River, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m);

        // River: bluff risk no aplica (solo turn), pero base reverse sigue (×0.6)
        Assert.That(penaltyTurn, Is.GreaterThan(penaltyRiver),
            "Bluff risk solo en turn, river penalty menor");
    }

    [Test]
    public void BluffRisk_NoDraw_NoPenalty()
    {
        // Board sin draws → no bluff risk
        var change = BoardChangeResult.Safe;

        double penalty = ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            change, HandRank.OnePair, false, BoardPosition.Turn, true, _profile,
            heroBlocksDangerSuit: false,
            villainType: OpponentType.LAG, heroStack: 50m, potSize: 20m);

        Assert.That(penalty, Is.EqualTo(0),
            "Sin draws en board → no reverse implied ni bluff risk");
    }

    #endregion
}
