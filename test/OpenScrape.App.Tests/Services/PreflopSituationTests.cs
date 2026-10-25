using OpenScrape.App.Services.Analysis;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Services;

[TestFixture]
public class PreflopSituationTests
{
    [TestCase(HandSituation.None, false)]
    [TestCase(HandSituation.OpenRaise, true)]
    [TestCase(HandSituation.RaiseOverLimper, true)]
    [TestCase(HandSituation.Call, false)]
    [TestCase(HandSituation.ThreeBet, true)]
    [TestCase(HandSituation.OpenRaiseVs3Bet, false)]
    [TestCase(HandSituation.OpenRaiseVs3BetAndCall, false)]
    [TestCase(HandSituation.FourBet, true)]
    [TestCase(HandSituation.Cold4Bet, true)]
    [TestCase(HandSituation.Squeeze, true)]
    [TestCase(HandSituation.VsSqueeze, false)]
    [TestCase(HandSituation.DonkBet, false)]
    [TestCase(HandSituation.DonkBetVsOpenRaise, false)]
    [TestCase(HandSituation.LimpRaise, false)]
    public void IsAggressor_DeberiaRetornarCorrectamente(HandSituation situation, bool expected)
    {
        Assert.That(PreflopSituation.IsAggressor(situation), Is.EqualTo(expected));
    }

    [Test]
    public void DetectDonkBet_SinApuesta_NoEsDonkBet()
    {
        var (isDonkBet, situation) = PreflopSituation.DetectDonkBet(0m, false, HandSituation.OpenRaise);

        Assert.That(isDonkBet, Is.False);
        Assert.That(situation, Is.EqualTo(HandSituation.OpenRaise));
    }

    [Test]
    public void DetectDonkBet_VillanoNoAgresorTrasOpenRaise_EsDonkBetVsOpenRaise()
    {
        var (isDonkBet, situation) = PreflopSituation.DetectDonkBet(5m, false, HandSituation.OpenRaise);

        Assert.That(isDonkBet, Is.True);
        Assert.That(situation, Is.EqualTo(HandSituation.DonkBetVsOpenRaise));
    }

    [Test]
    public void DetectDonkBet_VillanoNoAgresorEnOtraSituacion_EsDonkBetGenerico()
    {
        var (isDonkBet, situation) = PreflopSituation.DetectDonkBet(5m, false, HandSituation.ThreeBet);

        Assert.That(isDonkBet, Is.True);
        Assert.That(situation, Is.EqualTo(HandSituation.DonkBet));
    }

    [Test]
    public void DetectDonkBet_VillanoAgresor_NoEsDonkBet()
    {
        var (isDonkBet, situation) = PreflopSituation.DetectDonkBet(5m, true, HandSituation.OpenRaise);

        Assert.That(isDonkBet, Is.False);
        Assert.That(situation, Is.EqualTo(HandSituation.OpenRaise));
    }
}
