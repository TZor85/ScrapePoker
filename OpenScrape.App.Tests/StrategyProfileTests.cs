using Microsoft.Extensions.Options;

using OpenScrape.App.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class StrategyProfileTests
{
    private StrategyProfileService _service;
    private StrategyProfile _profile;

    [SetUp]
    public void Setup()
    {
        _profile = new StrategyProfile
        {
            Name = "Test",
            TurnBluffFrequency = 0.15,
            RiverBluffFrequency = 0.10,
            FlopBluffFrequency = 0.15,
            Thresholds = new Dictionary<string, StreetThresholds>
            {
                ["Turn_OpenRaise"] = new StreetThresholds
                {
                    FoldBelow = 45,
                    ThinValueAbove = 45,
                    ValueAbove = 55,
                    StrongValueAbove = 80,
                    StrongValueBetSize = "Bet 3/4",
                    ValueBetSize = "Bet 1/2",
                    ThinValueBetSize = "Bet 1/3",
                    DryBoardBetSize = "Bet 1/2",
                    CoordinatedBoardBetSize = "Bet 1/2",
                    PairedBoardBetSize = "Bet 3/4",
                    CanBluff = true,
                    BluffCondition = "IPCoordinatedSmallOnly",
                    LowEquityAction = "Fold",
                    ThinValueIPOnly = true,
                    ThinValueOOPFallback = "CheckFold",
                    ReduceSizeForLargeBet = true,
                    ReduceSizeForOOP = true
                },
                ["River_OpenRaiseVs3BetAndCall"] = new StreetThresholds
                {
                    FoldBelow = 40,
                    ThinValueAbove = 40,
                    ValueAbove = 55,
                    StrongValueAbove = 75,
                    LowEquityAction = "Call",
                    ThinValueIPOnly = true,
                    ThinValueOOPFallback = "CheckCall"
                },
                ["Turn_RaiseOverLimper"] = new StreetThresholds
                {
                    FoldBelow = 0,
                    ThinValueAbove = 45,
                    ValueAbove = 55,
                    StrongValueAbove = 70,
                    IsSimplified = true,
                    SimplifiedIPStrongBet = "Bet 1/2 (Value)",
                    SimplifiedIPThinBet = "Bet 1/3 (Thin Value)",
                    SimplifiedOOPStrongBet = "Bet 3/4 (Value)",
                    SimplifiedOOPValueBet = "Bet 1/2 (Value)",
                    SimplifiedOOPThinBet = "Bet 1/3 (Thin Value)"
                }
            }
        };

        _service = new StrategyProfileService(Options.Create(_profile));
    }

    [Test]
    public void GetThresholds_ExistingKey_DeberiaRetornarConfigCorrecta()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(thresholds.FoldBelow, Is.EqualTo(45));
        Assert.That(thresholds.StrongValueAbove, Is.EqualTo(80));
        Assert.That(thresholds.ValueAbove, Is.EqualTo(55));
        Assert.That(thresholds.ThinValueAbove, Is.EqualTo(45));
    }

    [Test]
    public void GetThresholds_KeyInexistente_DeberiaRetornarFallback()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Flop, HandSituation.FourBet);

        Assert.That(thresholds.FoldBelow, Is.EqualTo(40));
        Assert.That(thresholds.StrongValueAbove, Is.EqualTo(75));
    }

    [Test]
    public void GetThresholds_BoardTextureSizing_DeberiaSerCorrecta()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(thresholds.DryBoardBetSize, Is.EqualTo("Bet 1/2"));
        Assert.That(thresholds.CoordinatedBoardBetSize, Is.EqualTo("Bet 1/2"));
        Assert.That(thresholds.PairedBoardBetSize, Is.EqualTo("Bet 3/4"));
    }

    [Test]
    public void GetThresholds_BluffConfig_DeberiaSerCorrecta()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(thresholds.CanBluff, Is.True);
        Assert.That(thresholds.BluffCondition, Is.EqualTo("IPCoordinatedSmallOnly"));
    }

    [Test]
    public void GetThresholds_LowEquityAction_FoldPorDefecto()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(thresholds.LowEquityAction, Is.EqualTo("Fold"));
    }

    [Test]
    public void GetThresholds_LowEquityAction_CallParaVs3BetAndCall()
    {
        var thresholds = _service.GetThresholds(BoardPosition.River, HandSituation.OpenRaiseVs3BetAndCall);

        Assert.That(thresholds.LowEquityAction, Is.EqualTo("Call"));
    }

    [Test]
    public void GetThresholds_Simplified_RaiseOverLimper()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.RaiseOverLimper);

        Assert.That(thresholds.IsSimplified, Is.True);
        Assert.That(thresholds.SimplifiedIPStrongBet, Is.EqualTo("Bet 1/2 (Value)"));
        Assert.That(thresholds.SimplifiedOOPStrongBet, Is.EqualTo("Bet 3/4 (Value)"));
    }

    [Test]
    public void GetBluffFrequency_Turn_DeberiaRetornarValorConfigurado()
    {
        var freq = _service.GetBluffFrequency(BoardPosition.Turn);

        Assert.That(freq, Is.EqualTo(0.15));
    }

    [Test]
    public void GetBluffFrequency_River_DeberiaRetornarValorConfigurado()
    {
        var freq = _service.GetBluffFrequency(BoardPosition.River);

        Assert.That(freq, Is.EqualTo(0.10));
    }

    [Test]
    public void Profile_ParametrosGlobales_DeberianTenerValoresCorrectos()
    {
        Assert.That(_service.Profile.FoldEquityBase, Is.EqualTo(20.0));
        Assert.That(_service.Profile.BetSizingSPRDeepMultiplier, Is.EqualTo(1.25));
        Assert.That(_service.Profile.BetEVThreshold, Is.EqualTo(5.0));
    }

    [Test]
    public void GetThresholds_ThinValueIPOnly_DeberiaRespetarConfig()
    {
        var thresholds = _service.GetThresholds(BoardPosition.Turn, HandSituation.OpenRaise);

        Assert.That(thresholds.ThinValueIPOnly, Is.True);
        Assert.That(thresholds.ThinValueOOPFallback, Is.EqualTo("CheckFold"));
    }

    [Test]
    public void GetThresholds_ThinValueOOPFallback_CheckCallParaVs3BetAndCall()
    {
        var thresholds = _service.GetThresholds(BoardPosition.River, HandSituation.OpenRaiseVs3BetAndCall);

        Assert.That(thresholds.ThinValueOOPFallback, Is.EqualTo("CheckCall"));
    }
}
