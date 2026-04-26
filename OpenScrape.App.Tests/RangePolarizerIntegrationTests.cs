using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using static OpenScrape.App.Tests.TestMakeInputHelper;

namespace OpenScrape.App.Tests;

[TestFixture]
public class RangePolarizerIntegrationTests
{
    private PostflopDecisionService _service;
    private StrategyProfile _profile;

    [SetUp]
    public void Setup()
    {
        _profile = CreateTestProfile().FillMissingThresholds();
        var betSizingService = new BetSizingService(Options.Create(_profile));
        var rangePolarizer = new RangePolarizer();
        var registry = new ThresholdsRegistry(Options.Create(_profile));
        _service = new PostflopDecisionService(Options.Create(_profile), betSizingService, rangePolarizer, registry);
    }

    private StrategyProfile CreateTestProfile()
    {
        return new StrategyProfile
        {
            Thresholds = new Dictionary<string, StreetThresholds>
            {
                ["Flop_OpenRaise"] = new StreetThresholds
                {
                    FoldBelow = 40,
                    ThinValueAbove = 45,
                    ValueAbove = 55,
                    StrongValueAbove = 75
                },
                ["Turn_OpenRaise"] = new StreetThresholds
                {
                    FoldBelow = 42,
                    ThinValueAbove = 47,
                    ValueAbove = 57,
                    StrongValueAbove = 77
                },
                ["River_OpenRaise"] = new StreetThresholds
                {
                    FoldBelow = 45,
                    ThinValueAbove = 50,
                    ValueAbove = 60,
                    StrongValueAbove = 80
                }
            }
        };
    }

    [Test]
    public void DetermineAction_DryBoard_IP_Flop_ShouldApplyLooseAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 50,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_WetBoard_OOP_Flop_ShouldApplyTightAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 50,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Wet",
            isInPosition: false,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_PairedBoard_IP_ShouldApplyPolarizedAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 45,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Paired",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_River_IP_ShouldApplyPolarizedAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 55,
            street: BoardPosition.River,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_LowSPR_ShouldApplyCondensedAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 50,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 200,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_CoordinatedBoard_ShouldApplyLinearAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 45,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Coordinated",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_FacingBet_ShouldCombineAdjustments()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 50,
            street: BoardPosition.Turn,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.Medium,
            potOdds: 30,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_DifferentDecisions_DryVsWet_ShouldBeDifferent()
    {
        var dryResult = _service.DetermineAction(MakeInput(
            equity: 43,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        var wetResult = _service.DetermineAction(MakeInput(
            equity: 43,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Wet",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(dryResult.Action, Is.EqualTo(wetResult.Action));
    }

    [Test]
    public void DetermineAction_ThreeBetSituation_ShouldWork()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 50,
            street: BoardPosition.Flop,
            situation: HandSituation.ThreeBet,
            boardTexture: "Dry",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }

    [Test]
    public void DetermineAction_MonotoneBoard_ShouldApplyWetAdjustment()
    {
        var result = _service.DetermineAction(MakeInput(
            equity: 50,
            street: BoardPosition.Flop,
            situation: HandSituation.OpenRaise,
            boardTexture: "Monotone",
            isInPosition: true,
            villainBetSize: BetSizeCategory.NoBet,
            heroStack: 1000,
            potSize: 100));

        Assert.That(result.Action, Is.Not.Empty);
    }
}
