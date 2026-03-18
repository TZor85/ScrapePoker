using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class PostflopDecisionServiceTests
{
    private PostflopDecisionService _service;

    [SetUp]
    public void Setup()
    {
        var profile = CreateDefaultProfile();
        _service = new PostflopDecisionService(Options.Create(profile));
    }

    [Test]
    public void DetermineAction_StrongValue_DeberiaRecomendarValueBet()
    {
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Does.Contain("Value"));
        Assert.That(result.Reason, Does.Contain("strong value").IgnoreCase);
    }

    [Test]
    public void DetermineAction_ValueBet_DeberiaRecomendarValue()
    {
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Does.Contain("Value"));
    }

    [Test]
    public void DetermineAction_ThinValue_IP_DeberiaRecomendarThinValue()
    {
        var result = _service.DetermineAction(
            equity: 48, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Does.Contain("Thin Value"));
    }

    [Test]
    public void DetermineAction_ThinValue_OOP_DeberiaUsarFallback()
    {
        var result = _service.DetermineAction(
            equity: 48, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.Small);

        // ThinValueIPOnly = true, OOP fallback = "CheckFold" → "Fold"
        Assert.That(result.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_LowEquity_DeberiaRecomendarFold()
    {
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium);

        Assert.That(result.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_LowEquity_NoBet_DeberiaRecomendarCheck()
    {
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Is.EqualTo("Check"));
    }

    [Test]
    public void DetermineAction_LowEquityAction_Call_DeberiaRecomendarCall()
    {
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium);

        Assert.That(result.Action, Is.EqualTo("Call"));
    }

    [Test]
    public void DetermineAction_SemiBluff_SinFacingBet_DeberiaRecomendarSemiBluff()
    {
        // Sin facing bet → semi-bluff (apostar con draws)
        var result = _service.DetermineAction(
            equity: 25, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 9);

        Assert.That(result.Action, Does.Contain("Semi-Bluff"));
        Assert.That(result.IsBluff, Is.True);
    }

    [Test]
    public void DetermineAction_DrawConFacingBet_DeberiaCallImpliedOdds()
    {
        // Facing bet con draw en stacks profundos (SPR=5) → implied odds reducen pot odds requeridas
        // drawEquity = 9 * 2.17 = 19.53, adjustedPotOdds = 25 * ~0.65 ≈ 16.25 → 19.53 >= 16.25 → Call
        var result = _service.DetermineAction(
            equity: 25, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            potOdds: 25, totalOuts: 9,
            heroStack: 500, potSize: 100);

        Assert.That(result.Action, Is.EqualTo("Call"));
        Assert.That(result.Reason, Does.Contain("implied odds").IgnoreCase);
    }

    [Test]
    public void DetermineAction_River_ShowdownValue_NoBet_DeberiaCheck()
    {
        // Equity marginal en river sin apuesta, debajo de ThinValueAbove → check (showdown value)
        // River_OpenRaise: FoldBelow=40, ThinValueAbove=40 → equity 39 está debajo de ambos
        // Pero sin outs en river, y NoBet → check
        var result = _service.DetermineAction(
            equity: 39, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Is.EqualTo("Check"));
    }

    [Test]
    public void DetermineAction_PotOddsFavorables_DeberiaCall()
    {
        // Equity marginal pero pot odds buenos → call (via pot odds marginales en HandleLowEquity)
        var result = _service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            potOdds: 35);

        Assert.That(result.Action, Is.EqualTo("Call"));
        Assert.That(result.Reason, Does.Contain("pot odds").IgnoreCase);
    }

    [Test]
    public void DetermineAction_SimplifiedMode_IP_StrongValue()
    {
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.RaiseOverLimper,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Does.Contain("Value"));
    }

    [Test]
    public void DetermineAction_SimplifiedMode_OOP_LowEquity()
    {
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.RaiseOverLimper,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Does.Contain("Check"));
    }

    [Test]
    public void DetermineAction_Barrel_River_StrongValue_DeberiaMarcarBarrel()
    {
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            previousStreetBet: true);

        Assert.That(result.IsBarrel, Is.True);
    }

    [Test]
    public void DetermineAction_FacingBet_ValueEquity_DeberiaCall_NoRaise()
    {
        // Facing bet con equity buena → call (no raise, villano mostró fuerza)
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium);

        Assert.That(result.Action, Is.EqualTo("Call"));
    }

    [Test]
    public void DetermineAction_FacingBet_StrongValue_DeberiaRaise()
    {
        // Facing bet con equity muy alta y mano fuerte (TwoPair+) → raise for value
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Contain("Raise"));
        Assert.That(result.Action, Does.Contain("Value"));
    }

    [Test]
    public void DetermineAction_FacingBet_StrongEquity_OnePair_DeberiaCall()
    {
        // Facing bet con equity alta pero solo OnePair → call (no hinchar pote con mano vulnerable)
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Is.EqualTo("Call"));
        Assert.That(result.Reason, Does.Contain("vulnerable"));
    }

    [Test]
    public void DetermineAction_FacingLargeBet_NecesitaMasEquity()
    {
        // Equity 47: sin facing bet pasaría el FoldBelow=45, pero con Large bet (+8) → 45+8=53 > 47 → fold
        var resultNoBet = _service.DetermineAction(
            equity: 47, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        var resultLargeBet = _service.DetermineAction(
            equity: 47, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Large);

        // Sin bet: equity 47 > FoldBelow 45 → thin value o check
        Assert.That(resultNoBet.Action, Is.Not.EqualTo("Fold"));

        // Con large bet: equity 47 < adjustedFoldBelow 53 → fold (o check si no facing)
        Assert.That(resultLargeBet.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_VillainAggression_AumentaThreshold()
    {
        // Equity 49: small +1, callerVsCbet +2 → adjustedFoldBelow=48, 49 > 48 → pasa
        var sinAggro = _service.DetermineAction(
            equity: 49, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            villainShowedAggression: false);

        // Con aggression: +1 (small) +3 (aggro) +2 (callerVsCbet) → adjustedFoldBelow=51, 49 < 51 → fold
        var conAggro = _service.DetermineAction(
            equity: 49, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            villainShowedAggression: true);

        Assert.That(sinAggro.Action, Is.Not.EqualTo("Fold"));
        Assert.That(conAggro.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_NoBet_LowEquity_DeberiaCheck_NoFold()
    {
        // Sin facing bet con equity baja → check (no fold sin apuesta)
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet);

        Assert.That(result.Action, Is.EqualTo("Check"));
    }

    [Test]
    public void DetermineAction_FacingBet_ShowdownValue_River_SmallBet_DeberiaCall()
    {
        // River con bet pequeña y equity en FoldBelow → call por showdown value
        var result = _service.DetermineAction(
            equity: 44, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small);

        // adjustedFoldBelow = 40 + 1 + 2(callerVsCbet) = 43, equity 44 > 43 → pasa
        // 44 > adjustedThinValueAbove (45 + 0.5 = 45.5)? No → HandleFacingBet → showdown value river small
        Assert.That(result.Action, Is.EqualTo("Call"));
    }

    [Test]
    public void DetermineAction_SimplifiedMode_FacingBet_StrongValue_DeberiaRaise()
    {
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.RaiseOverLimper,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium);

        Assert.That(result.Action, Does.Contain("Raise"));
    }

    [Test]
    public void DetermineAction_SimplifiedMode_FacingBet_LowEquity_DeberiaFold()
    {
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.RaiseOverLimper,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.Medium);

        Assert.That(result.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void GetThresholds_Existente_DeberiaRetornarConfig()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(thresholds.FoldBelow, Is.EqualTo(45));
        Assert.That(thresholds.StrongValueAbove, Is.EqualTo(80));
    }

    [Test]
    public void GetThresholds_NoExistente_DeberiaRetornarFallback()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Flop, HandSituation.OpenRaise);

        Assert.That(thresholds.FoldBelow, Is.EqualTo(40));
        Assert.That(thresholds.CanBluff, Is.False);
    }

    // --- Tests de cartas peligrosas / danger penalty ---

    [Test]
    public void DetermineAction_FlushCompleted_FacingBet_DeberiaReducirEquityYFold()
    {
        // Equity 60, flush completed facing medium bet
        // penalty = 60 * 0.25 * 1.4 = 21.0 → effEquity = 39 < adjustedFoldBelow(45+4=49) → fold
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            boardChange: flushBoard, heroBlocksDangerSuit: false);

        Assert.That(result.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_FlushCompleted_HeroBlocksSuit_ReducePenalty()
    {
        // Equity 63, flush completed facing medium, hero con blocker
        // penalty = 63 * 0.25 * 1.4 * 0.5 = 11.025 → effEquity = 51.975
        // adjustedFoldBelow = 45 + 4(medium) + 2(callerVsCbet) = 51 → 51.975 > 51 → no fold
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 63, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            boardChange: flushBoard, heroBlocksDangerSuit: true);

        Assert.That(result.Action, Is.Not.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_FlushCompleted_NoBet_DeberiaCheck_NoValueBet()
    {
        // Equity 60, flush completed, no facing bet
        // penalty = 60 * 0.25 = 15.0 → effEquity = 45 = ThinValueAbove → check o thin
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            boardChange: flushBoard, heroBlocksDangerSuit: false);

        // effectiveEquity = 45, ThinValueAbove = 45 → check (no strong/value bet)
        Assert.That(result.Action, Does.Not.Contain("Value").Or.Contain("Thin Value"));
    }

    [Test]
    public void DetermineAction_StraightCompleted_DeberiaReducirEquity()
    {
        // Equity 55, straight completed facing medium
        // penalty = 55 * 0.18 * 1.4 = 13.86 → effEquity = 41.14 < adjustedFoldBelow(49) → fold
        var straightBoard = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: false, StraightCompleted: true,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 3);

        var result = _service.DetermineAction(
            equity: 55, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            boardChange: straightBoard, heroBlocksDangerSuit: false);

        Assert.That(result.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_SafeBoard_NoPenalty()
    {
        // BoardChangeResult.Safe → no penalty
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            boardChange: BoardChangeResult.Safe, heroBlocksDangerSuit: false);

        Assert.That(result.Action, Does.Contain("Value"));
    }

    [Test]
    public void DetermineAction_HighEquity_FlushCompleted_FacingBet_DeberiaCall_NoRaise()
    {
        // Simula la mano del usuario: AsQc en Qh3h7s-2h, equity ~94, facing small bet
        // penalty = 94 * 0.25 * 1.4 = 32.9 → effEquity = 61.1
        // StrongValueAbove = 80 → NOT strong → Call (no Raise)
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 94, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            boardChange: flushBoard, heroBlocksDangerSuit: false);

        Assert.That(result.Action, Does.Not.Contain("Raise"));
        Assert.That(result.Action, Is.EqualTo("Call"));
    }

    [Test]
    public void DetermineAction_HighEquity_FlushPersisted_River_NoBet_DeberiaCheck()
    {
        // River: flush sigue del turn (arrastrado), villain checks, hero no tiene flush
        // penalty = 99 * 0.25 = 24.75 → effEquity = 74.25
        // PERO: cap para no apostar en draw completado sin tenerlo → effEquity = min(74.25, 45) = 45
        // River_OpenRaise ThinValueAbove = 40, 45 > 40 → thin value, pero ThinValueIPOnly check OOP
        // Con IP: Thin Value bet (moderada). Sin cap sería Bet Pot.
        var flushPersisted = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 99, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            boardChange: flushPersisted, heroBlocksDangerSuit: false);

        // Capped a 45 → no value bet fuerte, como mucho thin value o check
        Assert.That(result.Action, Does.Not.Contain("Pot"));
        Assert.That(result.Action, Does.Not.Contain("3/4"));
    }

    [Test]
    public void DetermineAction_FlushCompleted_NoBet_HeroHasBlocker_PuedeApostar()
    {
        // Si hero tiene carta del flush suit (blocker), cap NO aplica → puede apostar
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 80, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            boardChange: flushBoard, heroBlocksDangerSuit: true);

        // Con blocker: penalty = 80*0.25*0.5 = 10 → effEquity = 70, cap NO aplica
        // 70 > ValueAbove(55) → Value bet
        Assert.That(result.Action, Does.Contain("Value"));
    }

    [Test]
    public void CalculateDangerPenalty_FlushCompleted_SinBlocker_Porcentual()
    {
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        // equity=80, no facing, no blocker → 80 * 0.25 = 20.0
        var penalty = _service.CalculateDangerPenalty(80, flushBoard, heroBlocksDangerSuit: false, isFacingBet: false);
        Assert.That(penalty, Is.EqualTo(20.0));
    }

    [Test]
    public void CalculateDangerPenalty_FlushCompleted_ConBlocker_Porcentual()
    {
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        // equity=80, no facing, blocker → 80 * 0.25 * 0.5 = 10.0
        var penalty = _service.CalculateDangerPenalty(80, flushBoard, heroBlocksDangerSuit: true, isFacingBet: false);
        Assert.That(penalty, Is.EqualTo(10.0));
    }

    [Test]
    public void CalculateDangerPenalty_FlushCompleted_FacingBet_Multiplica()
    {
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        // equity=80, facing bet, no blocker → 80 * 0.25 * 1.4 = 28.0
        var penalty = _service.CalculateDangerPenalty(80, flushBoard, heroBlocksDangerSuit: false, isFacingBet: true);
        Assert.That(penalty, Is.EqualTo(28.0));
    }

    [Test]
    public void CalculateDangerPenalty_MultipleDangers_DeberiaAcumular()
    {
        var dangerBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: true,
            BoardPaired: true, OvercardAppeared: true, CompletedFlushSuit: 1, DangerLevel: 10);

        // equity=80, no facing, no blocker
        // flush: 80*0.25=20, straight: 80*0.18=14.4, paired: 5, overcard: 3 → total = 42.4
        var penalty = _service.CalculateDangerPenalty(80, dangerBoard, heroBlocksDangerSuit: false, isFacingBet: false);
        Assert.That(penalty, Is.EqualTo(42.4));
    }

    // ============================================================
    // Tests de Implied Odds
    // ============================================================

    [Test]
    public void CalculateImpliedOddsFactor_SPRDeep_DeberiaReducirFactor()
    {
        // SPR = 500/100 = 5.0 (deep) → factor ≈ 0.65 base
        var factor = _service.CalculateImpliedOddsFactor(
            BoardPosition.Turn, isInPosition: false, hasFlushDraw: false,
            heroStack: 500, potSize: 100);

        Assert.That(factor, Is.LessThan(0.75));
        Assert.That(factor, Is.GreaterThan(0.50));
    }

    [Test]
    public void CalculateImpliedOddsFactor_SPRShallow_DeberiaSerCercaA1()
    {
        // SPR = 100/100 = 1.0 (shallow) → factor ≈ 0.95
        var factor = _service.CalculateImpliedOddsFactor(
            BoardPosition.Turn, isInPosition: false, hasFlushDraw: false,
            heroStack: 100, potSize: 100);

        Assert.That(factor, Is.GreaterThanOrEqualTo(0.85));
    }

    [Test]
    public void CalculateImpliedOddsFactor_River_DeberiaSerExactamente1()
    {
        // River: no hay más calles → factor = 1.0 (sin implied odds)
        var factor = _service.CalculateImpliedOddsFactor(
            BoardPosition.River, isInPosition: true, hasFlushDraw: true,
            heroStack: 1000, potSize: 100);

        Assert.That(factor, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateImpliedOddsFactor_SinStacks_DeberiaSerNeutro()
    {
        // Sin datos de stack → factor = 1.0 (neutro, no asumimos)
        var factor = _service.CalculateImpliedOddsFactor(
            BoardPosition.Turn, isInPosition: true, hasFlushDraw: true,
            heroStack: 0, potSize: 0);

        Assert.That(factor, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateImpliedOddsFactor_IPConFlushDraw_DeberiaSerMenorQueOOP()
    {
        // IP + flush draw debería dar mejor implied odds que OOP sin flush draw
        var factorIP = _service.CalculateImpliedOddsFactor(
            BoardPosition.Turn, isInPosition: true, hasFlushDraw: true,
            heroStack: 500, potSize: 100);
        var factorOOP = _service.CalculateImpliedOddsFactor(
            BoardPosition.Turn, isInPosition: false, hasFlushDraw: false,
            heroStack: 500, potSize: 100);

        Assert.That(factorIP, Is.LessThan(factorOOP));
    }

    [Test]
    public void CalculateImpliedOddsFactor_Flop_DeberiaSerMenorQueTurn()
    {
        // Flop tiene 2 calles futuras vs Turn con 1 → mejor implied odds en flop
        var factorFlop = _service.CalculateImpliedOddsFactor(
            BoardPosition.Flop, isInPosition: false, hasFlushDraw: false,
            heroStack: 500, potSize: 100);
        var factorTurn = _service.CalculateImpliedOddsFactor(
            BoardPosition.Turn, isInPosition: false, hasFlushDraw: false,
            heroStack: 500, potSize: 100);

        Assert.That(factorFlop, Is.LessThan(factorTurn));
    }

    [Test]
    public void DetermineAction_DrawSPRShallow_DeberiaFoldSinImpliedOdds()
    {
        // SPR shallow (1.0): implied odds casi neutro (~0.90)
        // drawEquity = 9 * 2.17 = 19.53, adjustedPotOdds = 30 * 0.90 ≈ 27 → 19.53 < 27 → no draw call
        // Marginal: equity 20 < potOdds 30 * 0.80 * 0.90 ≈ 21.66 → no marginal → Fold
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: false, villainBetSize: BetSizeCategory.Small,
            potOdds: 30, totalOuts: 9,
            heroStack: 100, potSize: 100);

        Assert.That(result.Action, Is.EqualTo("Fold"));
    }

    [Test]
    public void DetermineAction_DrawSPRDeep_DeberiaCallConImpliedOdds()
    {
        // SPR deep (5.0): implied odds ≈ 0.62
        // drawEquity = 9 * 2.17 = 19.53, adjustedPotOdds = 30 * 0.62 ≈ 18.5 → 19.53 >= 18.5 → Call
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: false, villainBetSize: BetSizeCategory.Small,
            potOdds: 30, totalOuts: 9,
            heroStack: 500, potSize: 100);

        Assert.That(result.Action, Is.EqualTo("Call"));
        Assert.That(result.Reason, Does.Contain("implied odds").IgnoreCase);
    }

    [Test]
    public void DetermineAction_FacingBetMarginalConImpliedOdds_DeberiaCall()
    {
        // Equity 38, potOdds 42 → sin implied: 38 < 42 * 0.80 = 33.6 → Call marginal
        // Con SPR deep: adjustedMarginal = 42 * 0.80 * 0.65 = 21.8 → 38 >= 21.8 → Call
        var result = _service.DetermineAction(
            equity: 38, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            potOdds: 42, totalOuts: 0,
            heroStack: 500, potSize: 100);

        Assert.That(result.Action, Is.EqualTo("Call"));
    }

    private static StrategyProfile CreateDefaultProfile()
    {
        return new StrategyProfile
        {
            Name = "PostflopTest",
            TurnBluffFrequency = 0.15,
            RiverBluffFrequency = 0.10,
            FlopBluffFrequency = 0.15,
            Thresholds = new Dictionary<string, StreetThresholds>
            {
                ["Turn_OpenRaise"] = new()
                {
                    FoldBelow = 45, ThinValueAbove = 45, ValueAbove = 55, StrongValueAbove = 80,
                    DryBoardBetSize = "Bet 1/2", CoordinatedBoardBetSize = "Bet 1/2", PairedBoardBetSize = "Bet 3/4",
                    StrongValueBetSize = "Bet 3/4", ValueBetSize = "Bet 1/2", ThinValueBetSize = "Bet 1/3",
                    CanBluff = true, BluffCondition = BluffConditionType.IPCoordinatedSmallOnly, LowEquityAction = "Fold"
                },
                ["Turn_OpenRaiseVs3BetAndCall"] = new()
                {
                    FoldBelow = 40, ThinValueAbove = 40, ValueAbove = 55, StrongValueAbove = 75,
                    LowEquityAction = "Call"
                },
                ["Turn_RaiseOverLimper"] = new()
                {
                    FoldBelow = 40, ThinValueAbove = 45, ValueAbove = 55, StrongValueAbove = 75,
                    IsSimplified = true,
                    SimplifiedIPStrongBet = "Bet 1/2 (Value)",
                    SimplifiedIPThinBet = "Bet 1/3 (Thin Value)",
                    SimplifiedOOPStrongBet = "Bet 3/4 (Value)",
                    SimplifiedOOPValueBet = "Bet 1/2 (Value)",
                    SimplifiedOOPThinBet = "Bet 1/3 (Thin Value)"
                },
                ["River_OpenRaise"] = new()
                {
                    FoldBelow = 40, ThinValueAbove = 40, ValueAbove = 60, StrongValueAbove = 75,
                    DryBoardBetSize = "Bet 2/3", CoordinatedBoardBetSize = "Bet Pot", PairedBoardBetSize = "Bet Pot",
                    StrongValueBetSize = "Bet Pot", ValueBetSize = "Bet 3/4", ThinValueBetSize = "Bet 1/2",
                    LowEquityAction = "Fold"
                }
            }
        };
    }

    // ─── Tests Multi-way ─────────────────────────────────────────────

    [Test]
    public void Multiway_FoldBelow_SubeConMasOponentes()
    {
        // Equity 48, FoldBelow base = 40. Con 1 oponente → no fold. Con 3 oponentes → +8 → fold.
        var result1 = _service.DetermineAction(
            equity: 48, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            numOpponents: 1);

        var result3 = _service.DetermineAction(
            equity: 48, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            numOpponents: 3);

        // Con 1 oponente, equity 48 > FoldBelow 40 → bet/check
        Assert.That(result1.Action, Does.Not.Contain("Fold"));
        // Con 3 oponentes, FoldBelow = 40 + 2*4 = 48 → equity justo en el límite
        Assert.That(result3.Action, Does.Contain("Check").Or.Contains("Fold"));
    }

    [Test]
    public void Multiway_NoBluffConMultiplesOponentes()
    {
        // Con 3 oponentes, no debería bluffear
        var result = _service.DetermineAction(
            equity: 15, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            numOpponents: 3);

        Assert.That(result.IsBluff, Is.False);
    }

    [Test]
    public void Multiway_NoSemiBluffConMultiplesOponentes()
    {
        // Equity baja con draws + multiway → no semi-bluff (sin facing bet)
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 9, numOpponents: 3);

        // Con multiway no debería semi-bluffear
        Assert.That(result.IsBluff, Is.False);
    }

    [Test]
    public void Multiway_HeadsUp_SiPermiteBluff()
    {
        // Heads-up con outs → sí puede semi-bluff
        var result = _service.DetermineAction(
            equity: 20, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 9, numOpponents: 1);

        Assert.That(result.Action, Does.Contain("Semi-Bluff"));
    }

    [Test]
    public void Multiway_FacingBet_NecesitaMasEquity()
    {
        // Facing medium bet + 3 oponentes, Turn_OpenRaise FoldBelow=45
        var resultHU = _service.DetermineAction(
            equity: 52, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            numOpponents: 1);

        var resultMW = _service.DetermineAction(
            equity: 52, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            numOpponents: 3);

        // HU: FoldBelow = 45 + 4(medium) + 2(callerVsCbet) = 51, equity 52 > 51 → call
        Assert.That(resultHU.Action, Does.Not.Contain("Fold"));
        // MW: FoldBelow = 45 + 4 + 8 + 2 = 59, equity 52 < 59 → fold
        Assert.That(resultMW.Action, Does.Contain("Fold"));
    }

    // ─── Tests Check-Raise (Mejora 1) ─────────────────────────────────

    [Test]
    public void CheckRaise_OOP_ManoFuerte_DeberiaCheckRaise()
    {
        var profile = CreateProfileConCheckRaise();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 82, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: false, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Contain("Check-Raise"));
        Assert.That(result.IsCheckRaise, Is.True);
    }

    [Test]
    public void CheckRaise_IP_NoDeberiaCheckRaise()
    {
        var profile = CreateProfileConCheckRaise();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 82, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: false, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.IsCheckRaise, Is.False);
    }

    [Test]
    public void CheckRaise_ManoDebil_NoDeberiaCheckRaise()
    {
        var profile = CreateProfileConCheckRaise();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 82, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: false, heroHandRank: HandRank.OnePair);

        Assert.That(result.IsCheckRaise, Is.False);
    }

    [Test]
    public void CheckRaise_HeroAgresor_NoDeberiaCheckRaise()
    {
        var profile = CreateProfileConCheckRaise();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 82, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: true, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.IsCheckRaise, Is.False);
    }

    [Test]
    public void CheckRaise_Multiway_NoDeberiaCheckRaise()
    {
        var profile = CreateProfileConCheckRaise();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 82, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: false, heroHandRank: HandRank.ThreeOfAKind, numOpponents: 3);

        Assert.That(result.IsCheckRaise, Is.False);
    }

    // ─── Tests Agresor vs Caller (Mejora 2) ───────────────────────────

    [Test]
    public void AgresorVsDonk_FacingBet_ManoFuerte_DeberiaRaise()
    {
        // Hero agresor con TwoPair+, equity buena (> ValueAbove) → raise vs donk
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroIsAggressor: true, heroHandRank: HandRank.TwoPair);

        Assert.That(result.Action, Does.Contain("Raise"));
        Assert.That(result.Reason, Does.Contain("donk"));
    }

    [Test]
    public void AgresorVsDonk_FacingBet_OnePair_DeberiaCall()
    {
        // Hero agresor con OnePair, equity buena → call (no raise con mano vulnerable)
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroIsAggressor: true, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Is.EqualTo("Call"));
    }

    [Test]
    public void AgresorVsDonk_FoldBelow_MasBajo()
    {
        // Hero agresor reduce FoldBelow: 45 + 4(medium) - 5(agresor) = 44
        // adjustedThinValueAbove: 45 + 2 - 3 = 44. Equity 45 > 44 → thin value call
        var resultAgresor = _service.DetermineAction(
            equity: 45, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroIsAggressor: true);

        Assert.That(resultAgresor.Action, Does.Not.Contain("Fold"));
    }

    [Test]
    public void CallerVsCbet_FoldBelow_MasAlto()
    {
        // Hero caller: 45 + 4(medium) + 2(callerVsCbet) = 51
        // Equity 50 < 51 → fold
        var resultCaller = _service.DetermineAction(
            equity: 50, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroIsAggressor: false);

        Assert.That(resultCaller.Action, Does.Contain("Fold"));
    }

    // ─── Tests Semi-bluff Sizing Agresivo (Mejora 3) ──────────────────

    [Test]
    public void SemiBluff_ComboDraw_Flop_SizingAgresivo()
    {
        var profile = CreateProfileConSemiBluffAgresivo();
        var service = new PostflopDecisionService(Options.Create(profile));

        // 14 outs en flop, sin facing bet → semi-bluff agresivo (3/4 pot)
        var result = service.DetermineAction(
            equity: 30, BoardPosition.Flop, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 14);

        Assert.That(result.Action, Does.Contain("3/4"));
        Assert.That(result.Reason, Does.Contain("combo draw"));
        Assert.That(result.IsBluff, Is.True);
    }

    [Test]
    public void SemiBluff_NoComboDraw_Flop_SizingNormal()
    {
        var profile = CreateProfileConSemiBluffAgresivo();
        var service = new PostflopDecisionService(Options.Create(profile));

        // 9 outs en flop → sizing normal
        var result = service.DetermineAction(
            equity: 30, BoardPosition.Flop, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 9);

        Assert.That(result.Action, Does.Contain("1/3"));
        Assert.That(result.IsBluff, Is.True);
    }

    [Test]
    public void SemiBluff_ComboDraw_Turn_SizingNormal()
    {
        var profile = CreateProfileConSemiBluffAgresivo();
        var service = new PostflopDecisionService(Options.Create(profile));

        // 14 outs en turn → no es combo draw agresivo (solo en flop)
        var result = service.DetermineAction(
            equity: 30, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 14);

        Assert.That(result.Action, Does.Contain("1/3"));
    }

    // ─── Tests Overbet (Mejora 4) ─────────────────────────────────────

    [Test]
    public void Overbet_BoardSeco_Agresor_DeberiaOverbet()
    {
        var profile = CreateProfileConOverbet();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 85, BoardPosition.Flop, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: true, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Contain("1.25x Pot"));
        Assert.That(result.Reason, Does.Contain("Overbet"));
    }

    [Test]
    public void Overbet_BoardCoordinado_NoDeberiaOverbet()
    {
        var profile = CreateProfileConOverbet();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 85, BoardPosition.Flop, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: true, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Not.Contain("1.25x Pot"));
    }

    [Test]
    public void Overbet_River_NutsDeberiaOverbet()
    {
        var profile = CreateProfileConOverbet();
        var service = new PostflopDecisionService(Options.Create(profile));

        // River con TwoPair+ en board seco → overbet por máximo valor
        var result = service.DetermineAction(
            equity: 85, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: true, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Contain("1.25x Pot"));
        Assert.That(result.Reason, Does.Contain("river"));
    }

    [Test]
    public void Overbet_River_OnePair_NoDeberiaOverbet()
    {
        var profile = CreateProfileConOverbet();
        var service = new PostflopDecisionService(Options.Create(profile));

        // River con OnePair → no overbet (mano vulnerable)
        var result = service.DetermineAction(
            equity: 85, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: true, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Not.Contain("1.25x Pot"));
    }

    [Test]
    public void Overbet_NoCaller_NoDeberiaOverbet()
    {
        var profile = CreateProfileConOverbet();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 85, BoardPosition.Flop, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroIsAggressor: false, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Not.Contain("1.25x Pot"));
    }

    // ─── Tests Hand Strength Relativa (Mejora 5) ──────────────────────

    [Test]
    public void HandStrength_Nuts_ReduceThresholdParaBetGrande()
    {
        // Set (ThreeOfAKind) → vulnerability adjustment = -4, adjStrongValue = 80-4 = 76
        // Equity 78 > 76 → strong value bet. Sin HandRank, 78 < 80 → solo value bet.
        var result = _service.DetermineAction(
            equity: 78, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.Action, Does.Contain("Value"));
        Assert.That(result.Reason, Does.Contain("strong value"));
    }

    [Test]
    public void HandStrength_ManoVulnerable_AumentaThreshold()
    {
        // OnePair → vulnerability adjustment = +2, adjStrongValue = 80+2 = 82
        // Equity 81 < 82 → no strong value (solo value bet)
        var result = _service.DetermineAction(
            equity: 81, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroHandRank: HandRank.OnePair);

        Assert.That(result.Reason, Does.Not.Contain("strong value"));
    }

    [Test]
    public void HandStrength_Nuts_IncreaseBetSize()
    {
        // ThreeOfAKind con strong value → IncreaseBetSize del StrongValueBetSize
        var result = _service.DetermineAction(
            equity: 90, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroHandRank: HandRank.FourOfAKind);

        // StrongValueBetSize = "Bet 3/4" → IncreaseBetSize → "Bet Pot"
        Assert.That(result.Action, Does.Contain("Pot"));
    }

    [Test]
    public void HandStrength_TwoPair_Coordinated_MasVulnerable()
    {
        // TwoPair en Coordinated → adjustment = +3, adjValue = 55+3 = 58
        // Equity 57 < 58 → thin value (no value bet)
        var result = _service.DetermineAction(
            equity: 57, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroHandRank: HandRank.TwoPair);

        Assert.That(result.Action, Does.Contain("Thin Value"));
    }

    // ─── Helpers para tests de mejoras ────────────────────────────────

    private static StrategyProfile CreateProfileConCheckRaise()
    {
        var profile = CreateDefaultProfile();
        profile.Thresholds["Turn_OpenRaiseVs3BetAndCall"] = new StreetThresholds
        {
            FoldBelow = 40, ThinValueAbove = 45, ValueAbove = 55, StrongValueAbove = 75,
            CanCheckRaise = true, CheckRaiseThreshold = 75, CheckRaiseBetSize = "Raise 3x",
            LowEquityAction = "Call"
        };
        return profile;
    }

    private static StrategyProfile CreateProfileConSemiBluffAgresivo()
    {
        var profile = CreateDefaultProfile();
        profile.Thresholds["Flop_OpenRaise"] = new StreetThresholds
        {
            FoldBelow = 40, ThinValueAbove = 45, ValueAbove = 55, StrongValueAbove = 75,
            BluffBetSize = "Bet 1/3", ComboDrawBetSize = "Bet 3/4", ComboDrawOutsThreshold = 12,
            CanBluff = true, BluffCondition = BluffConditionType.Always, LowEquityAction = "Fold"
        };
        return profile;
    }

    private static StrategyProfile CreateProfileConOverbet()
    {
        var profile = CreateDefaultProfile();
        profile.Thresholds["Flop_OpenRaise"] = new StreetThresholds
        {
            FoldBelow = 40, ThinValueAbove = 45, ValueAbove = 55, StrongValueAbove = 75,
            CanOverbet = true, OverbetBetSize = "Bet 1.25x Pot", OverbetMinEquity = 80,
            StrongValueBetSize = "Bet 3/4", ValueBetSize = "Bet 1/2",
            LowEquityAction = "Fold"
        };
        profile.Thresholds["River_OpenRaise"] = new StreetThresholds
        {
            FoldBelow = 40, ThinValueAbove = 40, ValueAbove = 60, StrongValueAbove = 75,
            CanOverbet = true, OverbetBetSize = "Bet 1.25x Pot", OverbetMinEquity = 80,
            StrongValueBetSize = "Bet Pot", ValueBetSize = "Bet 3/4",
            LowEquityAction = "Fold"
        };
        return profile;
    }

    // ─── Tests Combo Draw Equity Bonus (Mejora 3) ─────────────────────

    [Test]
    public void ComboDraw_Bonus_SaleDeHandleLowEquity()
    {
        // Turn_OpenRaise: FoldBelow=45
        // Equity 42 sin combo draw: < 45 → HandleLowEquity → semi-bluff
        var sinCombo = _service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 14, hasComboDraw: false);

        // Equity 42 + combo draw bonus (+6) = 48 > 45 → sale de HandleLowEquity
        var conCombo = _service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 14, hasComboDraw: true);

        Assert.That(sinCombo.IsBluff, Is.True); // Semi-bluff en HandleLowEquity
        Assert.That(conCombo.IsBluff, Is.False); // Sale de HandleLowEquity con bonus
    }

    [Test]
    public void ComboDraw_River_SinBonus()
    {
        // River_OpenRaise: FoldBelow=40. En river no hay bonus de combo draw
        // Equity 38 < 40 → HandleLowEquity → check (sin facing bet, river)
        var result = _service.DetermineAction(
            equity: 38, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            totalOuts: 0, hasComboDraw: true);

        Assert.That(result.Action, Does.Contain("Check"));
    }

    // ─── Tests Probe Bet (Mejora 4) ───────────────────────────────────

    [Test]
    public void ProbeBet_AgresorCheckeoFlop_OOP_DeberiaProbe()
    {
        var profile = CreateProfileConProbeBet();
        var service = new PostflopDecisionService(Options.Create(profile));

        // Equity 42 > FoldBelow(40) → llega a HandleNoBet → probe bet (equity > ProbeBetMinEquity 25)
        var result = service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            villainAggressorCheckedPreviousStreet: true);

        Assert.That(result.Action, Does.Contain("Probe"));
    }

    [Test]
    public void ProbeBet_AgresorAposto_SinProbe()
    {
        var profile = CreateProfileConProbeBet();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            villainAggressorCheckedPreviousStreet: false);

        Assert.That(result.Action, Does.Not.Contain("Probe"));
    }

    [Test]
    public void ProbeBet_IP_SinProbe()
    {
        var profile = CreateProfileConProbeBet();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            villainAggressorCheckedPreviousStreet: true);

        Assert.That(result.Action, Does.Not.Contain("Probe"));
    }

    [Test]
    public void ProbeBet_EquityMuyBaja_SinProbe()
    {
        var profile = CreateProfileConProbeBet();
        var service = new PostflopDecisionService(Options.Create(profile));

        // Equity 15 < ProbeBetMinEquity(25) → no probe
        var result = service.DetermineAction(
            equity: 15, BoardPosition.Turn, HandSituation.OpenRaiseVs3BetAndCall,
            boardTexture: "Dry", isInPosition: false, villainBetSize: BetSizeCategory.NoBet,
            villainAggressorCheckedPreviousStreet: true);

        Assert.That(result.Action, Does.Not.Contain("Probe"));
    }

    private static StrategyProfile CreateProfileConProbeBet()
    {
        var profile = CreateDefaultProfile();
        profile.Thresholds["Turn_OpenRaiseVs3BetAndCall"] = new StreetThresholds
        {
            FoldBelow = 40, ThinValueAbove = 45, ValueAbove = 55, StrongValueAbove = 75,
            CanProbeBet = true, ProbeBetSize = "Bet 1/3", ProbeBetMinEquity = 25,
            LowEquityAction = "Call"
        };
        return profile;
    }

    // ─── Tests Barrel Detection (Mejora Turn 1) ───────────────────────

    [Test]
    public void VillainBarreling_AumentaFoldBelow_DeberiaFold()
    {
        // Turn_OpenRaise FoldBelow=45, medium +4, callerVsCbet +2 = 51
        // + villain barrel +5 = 56. Equity 54 < 56 → fold
        var result = _service.DetermineAction(
            equity: 54, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            villainBarreling: true);

        Assert.That(result.Action, Does.Contain("Fold"));
    }

    [Test]
    public void VillainBarreling_EquityAlta_DeberiaCall()
    {
        // Equity 60 supera barrel penalty → call
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            villainBarreling: true);

        Assert.That(result.Action, Does.Not.Contain("Fold"));
    }

    [Test]
    public void VillainBarreling_NoBet_NoAfecta()
    {
        // Sin facing bet el barrel flag no afecta
        var result = _service.DetermineAction(
            equity: 50, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            villainBarreling: true);

        Assert.That(result.Action, Does.Not.Contain("Fold"));
    }

    [Test]
    public void HeroBarrel_Turn_DeberiaMarcarIsBarrel()
    {
        // Hero bet flop + bet turn = IsBarrel
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            previousStreetBet: true, heroHandRank: HandRank.ThreeOfAKind);

        Assert.That(result.IsBarrel, Is.True);
    }

    // ─── Tests SPR Push/Fold (Mejora Turn 2) ──────────────────────────

    [Test]
    public void SPRCorto_FacingBet_DeberiaAllIn()
    {
        // SPR 1.5 < 2.0, equity 60 > ValueAbove(55), OnePair → All-In
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroStack: 15, potSize: 10, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Contain("All-In"));
    }

    [Test]
    public void SPRCorto_NoBet_DeberiaAllIn()
    {
        // SPR 1.5, sin facing bet, equity > ValueAbove → All-In push
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroStack: 15, potSize: 10, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Contain("All-In"));
    }

    [Test]
    public void SPRCorto_HighCard_NoPush()
    {
        // SPR corto pero HighCard no hace all-in
        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroStack: 15, potSize: 10, heroHandRank: HandRank.HighCard);

        Assert.That(result.Action, Does.Not.Contain("All-In"));
    }

    [Test]
    public void SPRCorto_Flop_NoAfecta()
    {
        // En flop no se activa push/fold
        var profile = CreateProfileConSemiBluffAgresivo();
        var service = new PostflopDecisionService(Options.Create(profile));

        var result = service.DetermineAction(
            equity: 60, BoardPosition.Flop, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroStack: 15, potSize: 10, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Not.Contain("All-In"));
    }

    [Test]
    public void SPRDeep_AumentaFoldBelow()
    {
        // SPR 5 > 4.0, FoldBelow +3 = 48 + medium(4) + caller(2) = 54
        // Equity 53 < 54 → fold (vs sin SPR deep que sería 51 → 53 pasa)
        var result = _service.DetermineAction(
            equity: 53, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            heroStack: 500, potSize: 100);

        Assert.That(result.Action, Does.Contain("Fold"));
    }

    // ─── Tests Bet Sizing SPR (Mejora Turn 3) ─────────────────────────

    [Test]
    public void BetSizing_SPRCorto_AumentaBet()
    {
        // SPR 1.5, equity 50 (bajo ValueAbove=55 → no push/fold, pero sí bet sizing ajust)
        // equity > ThinValueAbove(45) → thin value bet. ThinValueBetSize "Bet 1/3" → +1 → "Bet 1/2"
        var result = _service.DetermineAction(
            equity: 50, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroStack: 30, potSize: 20, heroHandRank: HandRank.TwoPair);

        Assert.That(result.Action, Does.Contain("1/2"));
    }

    [Test]
    public void BetSizing_SPRNormal_SinCambio()
    {
        // SPR 2.5, sin ajuste
        var result = _service.DetermineAction(
            equity: 65, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            heroStack: 50, potSize: 20, heroHandRank: HandRank.TwoPair);

        Assert.That(result.Action, Does.Contain("1/2"));
    }

    // ─── Tests Double Barrel (Mejora Turn 5) ──────────────────────────

    [Test]
    public void DoubleBarrel_HeroBetFlop_EquityMarginal_DeberiaBarrel()
    {
        // Turn_Call: FoldBelow=40, ThinValueAbove=40, ValueAbove=55
        // Equity 42: > FoldBelow(40), > ThinValueAbove(40) → thin value primero
        // Necesitamos equity en [FoldBelow, ThinValueAbove) para barrel, pero son iguales
        // Usamos Turn_ThreeBet: FoldBelow=40, ThinValueAbove=45, ValueAbove=55
        // Equity 42: > FoldBelow(40), < ThinValueAbove(45), < ValueAbove(55) → barrel range
        var result = _service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.ThreeBet,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            previousStreetBet: true, heroIsAggressor: true);

        Assert.That(result.IsBarrel, Is.True);
        Assert.That(result.Action, Does.Contain("Barrel"));
    }

    [Test]
    public void DoubleBarrel_SinPreviousBet_NoBarrel()
    {
        var result = _service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.ThreeBet,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            previousStreetBet: false, heroIsAggressor: true);

        Assert.That(result.IsBarrel, Is.False);
    }

    [Test]
    public void DoubleBarrel_NoCaller_NoBarrel()
    {
        var result = _service.DetermineAction(
            equity: 42, BoardPosition.Turn, HandSituation.ThreeBet,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet,
            previousStreetBet: true, heroIsAggressor: false);

        Assert.That(result.IsBarrel, Is.False);
    }

    // ─── Tests Reverse Implied Odds (Mejora Turn 6) ───────────────────

    [Test]
    public void ReverseImplied_OnePair_FlushDraw_Turn_ReduceEquity()
    {
        var flushDrawBoard = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 2);

        // Equity 52, penalty = 4.0 * 1.5 (OnePair) = 6.0 → effectiveEquity = 46
        // + medium(4) + callerVsCbet(2) = adjustedFoldBelow 51. 46 < 51 → fold
        var result = _service.DetermineAction(
            equity: 52, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            boardChange: flushDrawBoard, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Contain("Fold"));
    }

    [Test]
    public void ReverseImplied_StrongHand_NoPenalty()
    {
        var flushDrawBoard = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 2);

        // Flush+ no se penaliza por reverse implied odds → no foldea
        var result = _service.DetermineAction(
            equity: 65, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            boardChange: flushDrawBoard, heroHandRank: HandRank.Flush);

        Assert.That(result.Action, Does.Not.Contain("Fold"));
    }

    [Test]
    public void ReverseImplied_River_NoPenalty()
    {
        var flushDrawBoard = new BoardChangeResult(
            FlushCompleted: false, FlushDrawAppeared: true, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: -1, DangerLevel: 2);

        // En river no aplica reverse implied odds → no penaliza extra
        var result = _service.DetermineAction(
            equity: 65, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Medium,
            boardChange: flushDrawBoard, heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Not.Contain("Fold"));
    }

    // ─── Tests Bluff Catching River ───────────────────────────────────

    [Test]
    public void BluffCatch_River_OnePair_SmallBet_DeberiaCall()
    {
        // River_OpenRaise FoldBelow=40. Equity 35 < 40, pero 35 >= 40*0.85=34 → bluff catch
        // OnePair + small/medium bet → call para atrapar bluffs
        var result = _service.DetermineAction(
            equity: 35, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            heroHandRank: HandRank.OnePair);

        Assert.That(result.Action, Does.Contain("Call"));
        Assert.That(result.Reason, Does.Contain("bluff catch"));
    }

    [Test]
    public void BluffCatch_River_LargeBet_NoBluffCatch()
    {
        // Large bet → no bluff catch (villano probablemente tiene valor)
        var result = _service.DetermineAction(
            equity: 35, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Large,
            heroHandRank: HandRank.OnePair);

        Assert.That(result.Reason, Does.Not.Contain("bluff catch"));
    }

    [Test]
    public void BluffCatch_River_HighCard_NoBluffCatch()
    {
        // HighCard → no bluff catch (necesita al menos OnePair)
        var result = _service.DetermineAction(
            equity: 35, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            heroHandRank: HandRank.HighCard);

        Assert.That(result.Reason, Does.Not.Contain("bluff catch"));
    }

    [Test]
    public void BluffCatch_Turn_NoAplica()
    {
        // Bluff catching solo en river
        var result = _service.DetermineAction(
            equity: 35, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            heroHandRank: HandRank.OnePair);

        Assert.That(result.Reason, Does.Not.Contain("bluff catch"));
    }
}
