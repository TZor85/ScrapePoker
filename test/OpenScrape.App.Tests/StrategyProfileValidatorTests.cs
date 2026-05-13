using OpenScrape.App.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Exceptions;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class StrategyProfileValidatorTests
{
    private static readonly HandSituation[] RequiredSituations =
    [
        HandSituation.OpenRaise,
        HandSituation.Call,
        HandSituation.RaiseOverLimper,
        HandSituation.ThreeBet,
        HandSituation.OpenRaiseVs3Bet,
        HandSituation.OpenRaiseVs3BetAndCall,
        HandSituation.FourBet,
        HandSituation.Cold4Bet,
        HandSituation.Squeeze,
        HandSituation.VsSqueeze,
        HandSituation.DonkBet,
        HandSituation.DonkBetVsOpenRaise
    ];

    private static readonly BoardPosition[] RequiredStreets =
    [
        BoardPosition.Flop,
        BoardPosition.Turn,
        BoardPosition.River
    ];

    private static StreetThresholds ValidThresholds() => new()
    {
        FoldBelow = 40,
        ThinValueAbove = 45,
        ValueAbove = 55,
        StrongValueAbove = 75
    };

    private static StrategyProfile BuildCompleteProfile()
    {
        var profile = new StrategyProfile { Name = "Test" };
        foreach (var street in RequiredStreets)
        {
            foreach (var situation in RequiredSituations)
            {
                profile.Thresholds[$"{street}_{situation}"] = ValidThresholds();
            }
        }
        return profile;
    }

    [Test]
    public void Validate_PerfilCompletoYCoherente_NoLanza()
    {
        var profile = BuildCompleteProfile();

        Assert.DoesNotThrow(() => StrategyProfileValidator.Validate(profile));
    }

    [Test]
    public void Validate_FaltaClaveObligatoria_Lanza()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds.Remove("Flop_OpenRaise");

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Message, Does.Contain("Flop_OpenRaise"));
    }

    [Test]
    public void Validate_MultiplesAusenciasReportadasJuntas()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds.Remove("Flop_OpenRaise");
        profile.Thresholds.Remove("Turn_Squeeze");
        profile.Thresholds.Remove("River_DonkBet");

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Errors, Has.Count.GreaterThanOrEqualTo(3));
        Assert.That(ex.Message, Does.Contain("Flop_OpenRaise"));
        Assert.That(ex.Message, Does.Contain("Turn_Squeeze"));
        Assert.That(ex.Message, Does.Contain("River_DonkBet"));
    }

    [Test]
    public void Validate_ClaveConStreetInvalido_Lanza()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds["Preflop_OpenRaise"] = ValidThresholds();

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Message, Does.Contain("Preflop_OpenRaise"));
    }

    [Test]
    public void Validate_ClaveMalformadaSinGuion_Lanza()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds["FlopOpenRaise"] = ValidThresholds();

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Message, Does.Contain("FlopOpenRaise"));
        Assert.That(ex.Message, Does.Contain("Formato"));
    }

    [Test]
    public void Validate_ClaveConSituationDesconocida_Lanza()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds["Turn_Foobar"] = ValidThresholds();

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Message, Does.Contain("Turn_Foobar"));
        Assert.That(ex.Message, Does.Contain("HandSituation"));
    }

    [Test]
    public void Validate_TiersInvertidosFoldBelowMayor_Lanza()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds["Flop_OpenRaise"] = new StreetThresholds
        {
            FoldBelow = 50,
            ThinValueAbove = 40,
            ValueAbove = 55,
            StrongValueAbove = 75
        };

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Message, Does.Contain("Flop_OpenRaise"));
        Assert.That(ex.Message, Does.Contain("FoldBelow"));
        Assert.That(ex.Message, Does.Contain("ThinValueAbove"));
    }

    [Test]
    public void Validate_TierFueraDeRango_Lanza()
    {
        var profile = BuildCompleteProfile();
        profile.Thresholds["Turn_ThreeBet"] = new StreetThresholds
        {
            FoldBelow = 40,
            ThinValueAbove = 45,
            ValueAbove = 55,
            StrongValueAbove = 120
        };

        var ex = Assert.Throws<StrategyProfileValidationException>(
            () => StrategyProfileValidator.Validate(profile));

        Assert.That(ex!.Message, Does.Contain("Turn_ThreeBet"));
        Assert.That(ex.Message, Does.Contain("StrongValueAbove"));
        Assert.That(ex.Message, Does.Contain("120"));
    }
}
