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
        // Facing bet con draw → call por implied odds (no semi-bluff raise)
        // potOdds=25 para que el cálculo de implied odds funcione
        var result = _service.DetermineAction(
            equity: 25, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Coordinated", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            potOdds: 25, totalOuts: 9);

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
        // Facing bet con equity muy alta → raise for value
        var result = _service.DetermineAction(
            equity: 85, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Medium);

        Assert.That(result.Action, Does.Contain("Raise"));
        Assert.That(result.Action, Does.Contain("Value"));
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
        // Equity 47: small bet penalty +1 → adjustedFoldBelow=46, 47 > 46 → pasa
        var sinAggro = _service.DetermineAction(
            equity: 47, BoardPosition.Turn, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small,
            villainShowedAggression: false);

        // Con aggression: penalty +1 (small) +3 (aggro) = adjustedFoldBelow=49, 47 < 49 → fold
        var conAggro = _service.DetermineAction(
            equity: 47, BoardPosition.Turn, HandSituation.OpenRaise,
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
            equity: 42, BoardPosition.River, HandSituation.OpenRaise,
            boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.Small);

        // adjustedFoldBelow = 40 + 1 = 41, equity 42 > 41 → pasa
        // 42 > adjustedThinValueAbove (40 + 0.5 = 40.5) → HandleFacingBet → thin value
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
        // Equity 60, flush completed facing medium, hero con blocker
        // penalty = 60 * 0.25 * 1.4 * 0.5 = 10.5 → effEquity = 49.5 > adjustedFoldBelow(49) → no fold
        var flushBoard = new BoardChangeResult(
            FlushCompleted: true, FlushDrawAppeared: false, StraightCompleted: false,
            BoardPaired: false, OvercardAppeared: false, CompletedFlushSuit: 1, DangerLevel: 4);

        var result = _service.DetermineAction(
            equity: 60, BoardPosition.Turn, HandSituation.OpenRaise,
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
                    CanBluff = true, BluffCondition = "IPCoordinatedSmallOnly", LowEquityAction = "Fold"
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
}
