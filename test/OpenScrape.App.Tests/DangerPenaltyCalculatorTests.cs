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
    public void Calculate_FlushDrawAppeared_SinFlushComplete_DeberiaPenalizarProporcional()
    {
        // Flush draw (3 same suit) → penalty proporcional: equity × 15% × streetMultiplier
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 1);

        var result = DangerPenaltyCalculator.Calculate(60, change, false, false, _profile);

        // 60 × 0.08 × 1.0 (turn default street multiplier) = 4.8
        Assert.That(result, Is.EqualTo(4.8).Within(0.1),
            "Flush draw penalty proporcional a equity (8%)");
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

    // ─── L3: Hero completó el draw → skip penalty ──────────────

    [Test]
    public void Calculate_HeroCompletoFlush_SkipFlushPenalty()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 3);

        var result = DangerPenaltyCalculator.Calculate(
            75, change, false, false, _profile,
            heroCompletedFlush: true);

        Assert.That(result, Is.EqualTo(0),
            "Hero completó flush → no debe penalizar");
    }

    [Test]
    public void Calculate_VillainCompletoFlush_PenaltyNormal()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 3);

        var result = DangerPenaltyCalculator.Calculate(
            40, change, false, false, _profile,
            heroCompletedFlush: false);

        double expected = 40 * (_profile.DangerFlushCompletePct / 100.0);
        Assert.That(result, Is.EqualTo(expected).Within(0.01),
            "Villain completó flush → penalty normal");
    }

    [Test]
    public void Calculate_HeroCompletoStraight_SkipStraightPenalty()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false,
            StraightCompleted: true, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 0, DangerLevel: 2);

        var result = DangerPenaltyCalculator.Calculate(
            70, change, false, false, _profile,
            heroCompletedStraight: true);

        Assert.That(result, Is.EqualTo(0),
            "Hero completó straight → no debe penalizar");
    }

    [Test]
    public void Calculate_AmbasCompletadas_HeroSoloFlush_StraightPenaltyAplica()
    {
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: true, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = DangerPenaltyCalculator.Calculate(
            65, change, false, false, _profile,
            heroCompletedFlush: true, heroCompletedStraight: false);

        // Flush skip, straight aplica
        double expectedStraight = 65 * (_profile.DangerStraightCompletePct / 100.0);
        Assert.That(result, Is.EqualTo(expectedStraight).Within(0.01),
            "Hero tiene flush → skip flush penalty, pero straight penalty aplica");
    }

    [Test]
    public void Calculate_SinCompletion_ComportamientoIdentico()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 0, DangerLevel: 2);

        var sinParams = DangerPenaltyCalculator.Calculate(50, change, false, false, _profile);
        var conParams = DangerPenaltyCalculator.Calculate(
            50, change, false, false, _profile,
            heroCompletedFlush: false, heroCompletedStraight: false);

        Assert.That(conParams, Is.EqualTo(sinParams).Within(0.001),
            "Parámetros false por defecto → mismo resultado");
    }

    // ─── L2: Flush draw blocker adjustment ──────────────────────

    [Test]
    public void Calculate_FlushDraw_SinBlocker_PenaltyCompleto()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 0, DangerLevel: 2);

        var result = DangerPenaltyCalculator.Calculate(
            60, change, false, false, _profile);

        double expected = 60 * 0.08; // streetMult = 1.0 (Turn default)
        Assert.That(result, Is.EqualTo(expected).Within(0.01),
            "Sin blocker → penalty flush draw completo");
    }

    [Test]
    public void Calculate_FlushDraw_NutBlocker_PenaltyReducido()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 0, DangerLevel: 2);

        var result = DangerPenaltyCalculator.Calculate(
            60, change, heroBlocksDangerSuit: true, isFacingBet: false, _profile,
            heroHasNutBlocker: true);

        // Flush draw penalty: 60 × 0.08 × 1.0 = 4.8
        // Nut blocker on draw: × 0.50 = 2.4
        // Global blocker: todo × 0.35 (nut blocker para flush completado, pero este no completó)
        // Blocker global: penalty total × DangerNutBlockerReduction
        // Pero wait: el blocker global (lines 82-93) se aplica al penalty TOTAL
        // flushDrawPenalty = 4.8 × 0.50 = 2.4, luego penalty total = 2.4
        // Luego blocker global: 2.4 × 0.35 (nutBlocker) = 0.84
        // Hmm no, el blocker global cubre TODA la penalty, no solo flush draw
        double flushDrawPenalty = 60 * 0.08 * _profile.DangerFlushDrawNutBlockerReduction; // 4.8 * 0.50 = 2.4
        // Luego blocker global se aplica
        double afterGlobal = flushDrawPenalty * _profile.DangerNutBlockerReduction; // 2.4 * 0.35 = 0.84
        Assert.That(result, Is.EqualTo(afterGlobal).Within(0.01),
            "Nut blocker reduce flush draw penalty × 0.50, luego global × 0.35");
    }

    [Test]
    public void Calculate_FlushDraw_NonNutBlocker_PenaltyReducido()
    {
        var change = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 0, DangerLevel: 2);

        var result = DangerPenaltyCalculator.Calculate(
            60, change, heroBlocksDangerSuit: true, isFacingBet: false, _profile,
            heroHasNutBlocker: false);

        // FlushDraw: 60 × 0.08 = 4.8
        // Non-nut blocker on draw: × 0.70 = 3.36
        // Global non-nut blocker: 3.36 × 0.55 = 1.848
        double flushDrawPenalty = 60 * 0.08 * _profile.DangerFlushDrawNonNutBlockerReduction;
        double afterGlobal = flushDrawPenalty * _profile.DangerNonNutBlockerReduction;
        Assert.That(result, Is.EqualTo(afterGlobal).Within(0.01),
            "Non-nut blocker reduce flush draw penalty × 0.70, luego global × 0.55");
    }

    [Test]
    public void Calculate_FlushCompletado_BlockerNoAfectado()
    {
        // Regresión: flush completado con blocker usa la lógica existente
        var change = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false,
            StraightCompleted: false, BoardPaired: false,
            OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 3);

        var result = DangerPenaltyCalculator.Calculate(
            60, change, heroBlocksDangerSuit: true, isFacingBet: false, _profile,
            heroHasNutBlocker: true);

        double flushPenalty = 60 * (_profile.DangerFlushCompletePct / 100.0);
        double afterBlocker = flushPenalty * _profile.DangerNutBlockerReduction;
        Assert.That(result, Is.EqualTo(afterBlocker).Within(0.01),
            "Flush completado → blocker global aplica como antes");
    }
}
