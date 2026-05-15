using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class AutoCalibrationServiceTests
{
    private AutoCalibrationService _service;
    private ExploitabilityCalculator _exploitabilityCalculator;

    [SetUp]
    public void Setup()
    {
        _service = new AutoCalibrationService();
        _exploitabilityCalculator = new ExploitabilityCalculator();
    }

    [TearDown]
    public void TearDown()
    {
        _exploitabilityCalculator.ClearRecords();
        _service.ResetCalibrationHistory();
    }

    [Test]
    public void ShouldRecalibrate_WithFewDecisions_ShouldReturnFalse()
    {
        for (int i = 0; i < 10; i++)
        {
            var analysis = _exploitabilityCalculator.AnalyzeDecision(
                "Bet", 50, 25, 30,
                BoardPosition.Flop, HandSituation.OpenRaise,
                true, "Dry", 100, BetSizeCategory.NoBet);
            _exploitabilityCalculator.RecordDecision(new DecisionRecord
            {
                OurDecision = "Bet",
                Equity = 50,
                PotOdds = 25,
                FoldEquity = 30,
                Street = BoardPosition.Flop,
                Situation = HandSituation.OpenRaise,
                IsInPosition = true,
                BoardTexture = "Dry",
                PotSize = 100,
                VillainBetSize = BetSizeCategory.NoBet,
                OurDecisionEV = analysis.OurDecisionEV,
                BestResponseEV = analysis.BestResponseEV,
                ExploitabilityMbb = analysis.ExploitabilityMbb
            });
        }

        var result = _service.ShouldRecalibrate(_exploitabilityCalculator);

        Assert.That(result, Is.False);
    }

    [Test]
    public void ShouldRecalibrate_WithManyDecisions_ShouldReturnTrue()
    {
        _service = new AutoCalibrationService();

        for (int i = 0; i < 100; i++)
        {
            var analysis = _exploitabilityCalculator.AnalyzeDecision(
                "Bet", 30, 25, 30,
                BoardPosition.Flop, HandSituation.OpenRaise,
                true, "Coordinated", 100, BetSizeCategory.NoBet);
            _exploitabilityCalculator.RecordDecision(new DecisionRecord
            {
                OurDecision = "Bet",
                Equity = 30,
                PotOdds = 25,
                FoldEquity = 30,
                Street = BoardPosition.Flop,
                Situation = HandSituation.OpenRaise,
                IsInPosition = true,
                BoardTexture = "Coordinated",
                PotSize = 100,
                VillainBetSize = BetSizeCategory.NoBet,
                OurDecisionEV = analysis.OurDecisionEV,
                BestResponseEV = analysis.BestResponseEV,
                ExploitabilityMbb = analysis.ExploitabilityMbb
            });
            _service.RecordDecision();
        }

        var shouldRecal = _service.ShouldRecalibrate(_exploitabilityCalculator);

        Assert.That(shouldRecal, Is.True);
    }

    [Test]
    public void Calibrate_WithLowExploitability_ShouldReturnFalse()
    {
        for (int i = 0; i < 25; i++)
        {
            var analysis = _exploitabilityCalculator.AnalyzeDecision(
                "Bet", 70, 25, 30,
                BoardPosition.Flop, HandSituation.OpenRaise,
                true, "Dry", 100, BetSizeCategory.NoBet);
            _exploitabilityCalculator.RecordDecision(new DecisionRecord
            {
                OurDecision = "Bet",
                Equity = 70,
                PotOdds = 25,
                FoldEquity = 30,
                Street = BoardPosition.Flop,
                Situation = HandSituation.OpenRaise,
                IsInPosition = true,
                BoardTexture = "Dry",
                PotSize = 100,
                VillainBetSize = BetSizeCategory.NoBet,
                OurDecisionEV = analysis.OurDecisionEV,
                BestResponseEV = analysis.BestResponseEV,
                ExploitabilityMbb = 5
            });
        }

        var profile = new StrategyProfile();
        var result = _service.Calibrate(_exploitabilityCalculator, profile);

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Calibrate_WithOverBluffingLeak_ShouldProposeAdjustment()
    {
        for (int i = 0; i < 25; i++)
        {
            var analysis = _exploitabilityCalculator.AnalyzeDecision(
                "Bet", 25, 25, 20,
                BoardPosition.Flop, HandSituation.OpenRaise,
                true, "Coordinated", 100, BetSizeCategory.NoBet);
            _exploitabilityCalculator.RecordDecision(new DecisionRecord
            {
                OurDecision = "Bet",
                Equity = 25,
                PotOdds = 25,
                FoldEquity = 20,
                Street = BoardPosition.Flop,
                Situation = HandSituation.OpenRaise,
                IsInPosition = true,
                BoardTexture = "Coordinated",
                PotSize = 100,
                VillainBetSize = BetSizeCategory.NoBet,
                OurDecisionEV = analysis.OurDecisionEV,
                BestResponseEV = analysis.BestResponseEV,
                ExploitabilityMbb = analysis.ExploitabilityMbb
            });
        }

        var profile = new StrategyProfile();
        var result = _service.Calibrate(_exploitabilityCalculator, profile);

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void Calibrate_WithInsufficientData_ShouldReturnFalse()
    {
        var analysis = _exploitabilityCalculator.AnalyzeDecision(
            "Bet", 50, 25, 30,
            BoardPosition.Flop, HandSituation.OpenRaise,
            true, "Dry", 100, BetSizeCategory.NoBet);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = "Bet",
            Equity = 50,
            Street = BoardPosition.Flop,
            PotSize = 100,
            OurDecisionEV = analysis.OurDecisionEV,
            BestResponseEV = analysis.BestResponseEV,
            ExploitabilityMbb = 10
        });

        var profile = new StrategyProfile();
        var result = _service.Calibrate(_exploitabilityCalculator, profile);

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void RecordDecision_ShouldIncrementCounter()
    {
        _service.RecordDecision();
        _service.RecordDecision();
        _service.RecordDecision();

        Assert.That(_service.GetDecisionsSinceLastCalibration(), Is.EqualTo(3));
    }

    [Test]
    public void Calibrate_AfterCalibration_ShouldResetCounter()
    {
        for (int i = 0; i < 30; i++)
        {
            var analysis = _exploitabilityCalculator.AnalyzeDecision(
                "Bet", 30, 25, 30,
                BoardPosition.Flop, HandSituation.OpenRaise,
                true, "Coordinated", 100, BetSizeCategory.NoBet);
            _exploitabilityCalculator.RecordDecision(new DecisionRecord
            {
                OurDecision = "Bet",
                Equity = 30,
                Street = BoardPosition.Flop,
                PotSize = 100,
                OurDecisionEV = analysis.OurDecisionEV,
                BestResponseEV = analysis.BestResponseEV,
                ExploitabilityMbb = 25
            });
        }

        var profile = new StrategyProfile();
        _service.Calibrate(_exploitabilityCalculator, profile);

        Assert.That(_service.GetDecisionsSinceLastCalibration(), Is.EqualTo(0));
    }

    [Test]
    public void GetPreview_WithNoData_ShouldReturnEmptyAdjustments()
    {
        var profile = new StrategyProfile();
        var preview = _service.GetPreview(_exploitabilityCalculator, profile);

        Assert.That(preview.ProposedAdjustments, Is.Empty);
    }

    [Test]
    public void GetPreview_UsaThresholdActualComoOldValue()
    {
        for (int i = 0; i < 25; i++)
        {
            _exploitabilityCalculator.RecordDecision(new DecisionRecord
            {
                OurDecision = "Call",
                Equity = 20,
                PotOdds = 35,
                FoldEquity = 0,
                Street = BoardPosition.Turn,
                Situation = HandSituation.OpenRaise,
                IsInPosition = true,
                BoardTexture = "Dry",
                PotSize = 100,
                VillainBetSize = BetSizeCategory.Medium,
                OurDecisionEV = -10,
                BestResponseEV = 10,
                ExploitabilityMbb = 50
            });
        }

        var profile = new StrategyProfile
        {
            Thresholds = new Dictionary<string, StreetThresholds>
            {
                ["Turn_OpenRaise"] = new()
                {
                    FoldBelow = 33,
                    ThinValueAbove = 52,
                    ValueAbove = 65,
                    StrongValueAbove = 80
                }
            }
        };

        var preview = _service.GetPreview(_exploitabilityCalculator, profile);

        var adjustment = preview.ProposedAdjustments.Single(a => a.ParameterName == "FoldBelow");
        Assert.That(adjustment.OldValue, Is.EqualTo(33));
        Assert.That(adjustment.NewValue, Is.EqualTo(38));
    }
}
