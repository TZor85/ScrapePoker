using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests;

[TestFixture]
public class DangerPenaltyCalculatorTests
{
    private StrategyProfile _profile;

    [SetUp]
    public void Setup()
    {
        _profile = new StrategyProfile();
    }

    [Test]
    public void Calculate_DangerLevel0_DeberiaRetornar0()
    {
        var change = BoardChangeResult.Safe;

        var result = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Calculate_FlushCompleted_DeberiaPenalizarPorcentual()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 3);

        var result = DangerPenaltyCalculator.Calculate(80, change, false, false, _profile);

        // 80 * (25/100) = 20
        double expected = 80 * (_profile.DangerFlushCompletePct / 100.0);
        Assert.That(result, Is.EqualTo(expected).Within(0.01));
    }

    [Test]
    public void Calculate_StraightCompleted_DeberiaPenalizarPorcentual()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false,
            StraightCompleted: true, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 2);

        var result = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);

        // 60 * (18/100) = 10.8
        double expected = 60 * (_profile.DangerStraightCompletePct / 100.0);
        Assert.That(result, Is.EqualTo(expected).Within(0.01));
    }

    [Test]
    public void Calculate_FlushDrawAppeared_SinFlushComplete_DeberiaPenalizarFlat()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 1);

        var result = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);

        Assert.That(result, Is.EqualTo(_profile.DangerFlushDrawPenalty).Within(0.01));
    }

    [Test]
    public void Calculate_BoardPaired_DeberiaSumarPenaltyFlat()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: true,
            OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 1);

        var result = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);

        Assert.That(result, Is.EqualTo(_profile.DangerBoardPairedPenalty).Within(0.01));
    }

    [Test]
    public void Calculate_Overcard_DeberiaSumarPenaltyFlat()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: true, CompletedFlushSuit: -1, DangerLevel: 1);

        var result = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);

        Assert.That(result, Is.EqualTo(_profile.DangerOvercardPenalty).Within(0.01));
    }

    [Test]
    public void Calculate_FacingBet_DeberiaMultiplicarPenalty()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: true,
            OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 1);

        var sinBet = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);
        var conBet = DangerPenaltyCalculator.Calculate(60, change, false, true, _profile);

        Assert.That(conBet, Is.EqualTo(sinBet * _profile.DangerFacingBetMultiplier).Within(0.01));
    }

    [Test]
    public void Calculate_HeroBlocks_DeberiaReducirPenalty()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 3);

        var sinBlock = DangerPenaltyCalculator.Calculate(80, change, false, false, _profile);
        var conBlock = DangerPenaltyCalculator.Calculate(80, change, true, false, _profile);

        // Non-nut blocker por defecto (heroHasNutBlocker=false), DangerLevel=3 (no board4flush)
        Assert.That(conBlock, Is.EqualTo(sinBlock * _profile.DangerNonNutBlockerReduction).Within(0.01));
    }

    [Test]
    public void Calculate_FlushCompletedYBoardPaired_DeberiaSumarAmbas()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: true,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 3);

        var result = DangerPenaltyCalculator.Calculate(80, change, false, false, _profile);

        double expectedFlush = 80 * (_profile.DangerFlushCompletePct / 100.0);
        double expectedPaired = _profile.DangerBoardPairedPenalty;
        Assert.That(result, Is.EqualTo(expectedFlush + expectedPaired).Within(0.01));
    }

    [Test]
    public void Calculate_FacingBetConHeroBlocks_DeberiaAplicarAmbosMultiplicadores()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: true,
            OvercardAppeared: true, CompletedFlushSuit: -1, DangerLevel: 2);

        var result = DangerPenaltyCalculator.Calculate(60, change, true, true, _profile);

        double basePenalty = _profile.DangerBoardPairedPenalty + _profile.DangerOvercardPenalty;
        // Non-nut blocker (default), no flush completed → DangerNonNutBlockerReduction
        double expected = basePenalty * _profile.DangerFacingBetMultiplier * _profile.DangerNonNutBlockerReduction;
        Assert.That(result, Is.EqualTo(expected).Within(0.01));
    }

    [Test]
    public void Calculate_PenaltySiemprePositivaOCero()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: true,
            StraightCompleted: true, BoardPaired: true,
            OvercardAppeared: true, CompletedFlushSuit: 1, DangerLevel: 5);

        var result = DangerPenaltyCalculator.Calculate(50, change, false, false, _profile);

        Assert.That(result, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void Calculate_FlushYStraightCompletados_DeberiaUsarMaxNoSuma()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: true, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = DangerPenaltyCalculator.Calculate(80, change, false, false, _profile);

        double flushPenalty = 80 * (_profile.DangerFlushCompletePct / 100.0);
        double straightPenalty = 80 * (_profile.DangerStraightCompletePct / 100.0);
        double expected = Math.Max(flushPenalty, straightPenalty);
        Assert.That(result, Is.EqualTo(expected).Within(0.01),
            "Debe usar Math.Max de flush y straight, no la suma");
    }

    [Test]
    public void Calculate_FlushYStraightCompletados_NuncaSuperaSumaIndividual()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: true, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = DangerPenaltyCalculator.Calculate(70, change, false, false, _profile);

        double flushPenalty = 70 * (_profile.DangerFlushCompletePct / 100.0);
        double straightPenalty = 70 * (_profile.DangerStraightCompletePct / 100.0);
        Assert.That(result, Is.LessThanOrEqualTo(flushPenalty + straightPenalty),
            "No debe sumar ambas penalizaciones porcentuales");
    }
}
