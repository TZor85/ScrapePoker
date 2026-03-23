using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

[TestFixture]
public class PreflopAnalyzerTests
{
    private StrategyProfile _profile;

    [SetUp]
    public void Setup()
    {
        _profile = new StrategyProfile();
    }

    #region IsPreflopAggressor

    [TestCase(HandSituation.OpenRaise, true)]
    [TestCase(HandSituation.RaiseOverLimper, true)]
    [TestCase(HandSituation.ThreeBet, true)]
    [TestCase(HandSituation.FourBet, true)]
    [TestCase(HandSituation.Cold4Bet, true)]
    [TestCase(HandSituation.Squeeze, true)]
    [TestCase(HandSituation.Call, false)]
    [TestCase(HandSituation.OpenRaiseVs3Bet, false)]
    [TestCase(HandSituation.DonkBet, false)]
    [TestCase(HandSituation.None, false)]
    public void IsPreflopAggressor_DeberiaRetornarCorrectamente(HandSituation situation, bool expected)
    {
        Assert.That(PreflopAnalyzer.IsPreflopAggressor(situation), Is.EqualTo(expected));
    }

    #endregion

    #region HasRangeAdvantageOnBoard

    [Test]
    public void RangeAdvantage_AgresorConAce_DeberiaSerTrue()
    {
        var ranks = new List<int> { 14, 7, 3 }; // A-7-3
        var texture = new BoardTextureResult(BoardTextureCategory.Dry, 10, false, false, true, false, false, false, true, false, false);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, true, HandSituation.OpenRaise);

        Assert.That(result, Is.True);
    }

    [Test]
    public void RangeAdvantage_AgresorConKingQueen_DeberiaSerTrue()
    {
        var ranks = new List<int> { 13, 12, 5 }; // K-Q-5
        var texture = new BoardTextureResult(BoardTextureCategory.SemiDry, 20, false, true, false, false, false, true, false, false, false);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, true, HandSituation.OpenRaise);

        Assert.That(result, Is.True);
    }

    [Test]
    public void RangeAdvantage_AgresorBoardBajo_DeberiaSerFalse()
    {
        var ranks = new List<int> { 4, 6, 8 }; // 4-6-8
        var texture = new BoardTextureResult(BoardTextureCategory.SemiWet, 40, false, true, false, false, true, false, true, false, true);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, true, HandSituation.OpenRaise);

        Assert.That(result, Is.False);
    }

    [Test]
    public void RangeAdvantage_CallerBoardBajoConectado_DeberiaSerTrue()
    {
        var ranks = new List<int> { 5, 6, 7 }; // 5-6-7 bajo y conectado
        var texture = new BoardTextureResult(BoardTextureCategory.Wet, 65, false, true, false, false, true, false, true, false, true);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, false, HandSituation.Call);

        Assert.That(result, Is.True);
    }

    [Test]
    public void RangeAdvantage_CallerBoardAlto_DeberiaSerFalse()
    {
        var ranks = new List<int> { 14, 12, 10 }; // A-Q-T
        var texture = new BoardTextureResult(BoardTextureCategory.SemiWet, 35, false, true, false, false, false, true, false, false, true);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, false, HandSituation.Call);

        Assert.That(result, Is.False);
    }

    [Test]
    public void RangeAdvantage_3BetPotAgresorBoardAlto_DeberiaSerTrue()
    {
        var ranks = new List<int> { 14, 11, 3 }; // A-J-3
        var texture = new BoardTextureResult(BoardTextureCategory.SemiDry, 20, false, true, false, false, false, false, true, false, false);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, true, HandSituation.ThreeBet);

        Assert.That(result, Is.True);
    }

    [Test]
    public void RangeAdvantage_3BetPotAgresorBoardBajoConectado_DeberiaSerFalse()
    {
        var ranks = new List<int> { 5, 6, 7 }; // 5-6-7
        var texture = new BoardTextureResult(BoardTextureCategory.Wet, 65, false, true, false, false, true, false, true, false, true);

        var result = PreflopAnalyzer.HasRangeAdvantageOnBoard(ranks, texture, true, HandSituation.ThreeBet);

        Assert.That(result, Is.False);
    }

    #endregion

    #region CalculateCbetAdjustment

    [Test]
    public void CbetAdjustment_AgresorConRangeAdvantage_DeberiaSerPositivo()
    {
        var texture = new BoardTextureResult(BoardTextureCategory.Dry, 10, false, false, true, false, false, false, true, false, false);

        var result = PreflopAnalyzer.CalculateCbetAdjustment(true, true, texture, true, 1, _profile);

        Assert.That(result, Is.GreaterThan(0));
        Assert.That(result, Is.EqualTo(_profile.CbetAggressorBonus + _profile.CbetRangeAdvantageBonus).Within(0.01));
    }

    [Test]
    public void CbetAdjustment_AgresorSinRangeAdvantage_SoloBonus()
    {
        var texture = new BoardTextureResult(BoardTextureCategory.SemiWet, 40, false, true, false, false, true, false, true, false, true);

        var result = PreflopAnalyzer.CalculateCbetAdjustment(true, false, texture, true, 1, _profile);

        Assert.That(result, Is.EqualTo(_profile.CbetAggressorBonus).Within(0.01));
    }

    [Test]
    public void CbetAdjustment_CallerSinVentaja_DeberiaSerNegativo()
    {
        var texture = new BoardTextureResult(BoardTextureCategory.Dry, 10, false, false, true, false, false, false, true, false, false);

        var result = PreflopAnalyzer.CalculateCbetAdjustment(false, false, texture, true, 1, _profile);

        Assert.That(result, Is.LessThan(0));
    }

    [Test]
    public void CbetAdjustment_Monotone_DeberiaReducir()
    {
        var monotone = new BoardTextureResult(BoardTextureCategory.Wet, 70, true, false, false, false, false, false, false, true, false);

        var result = PreflopAnalyzer.CalculateCbetAdjustment(true, true, monotone, true, 1, _profile);

        double sinMonotone = _profile.CbetAggressorBonus + _profile.CbetRangeAdvantageBonus;
        double conMonotone = sinMonotone * _profile.CbetMonotoneReduction;
        Assert.That(result, Is.EqualTo(conMonotone).Within(0.01));
    }

    [Test]
    public void CbetAdjustment_Multiway_DeberiaReducir()
    {
        var texture = new BoardTextureResult(BoardTextureCategory.Dry, 10, false, false, true, false, false, false, true, false, false);

        var headsUp = PreflopAnalyzer.CalculateCbetAdjustment(true, true, texture, true, 1, _profile);
        var multiway = PreflopAnalyzer.CalculateCbetAdjustment(true, true, texture, true, 3, _profile);

        Assert.That(multiway, Is.LessThan(headsUp));
    }

    #endregion

    #region DetectDonkBet

    [Test]
    public void DetectDonkBet_MaxBetCero_NoEsDonk()
    {
        var (isDonk, _) = PreflopAnalyzer.DetectDonkBet(0m, false, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.False);
    }

    [Test]
    public void DetectDonkBet_VillainNoAgresor_EsDonk()
    {
        var (isDonk, situation) = PreflopAnalyzer.DetectDonkBet(5m, false, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.True);
        Assert.That(situation, Is.EqualTo(HandSituation.DonkBetVsOpenRaise));
    }

    [Test]
    public void DetectDonkBet_VillainNoAgresorOtraSituacion_DonkGenerico()
    {
        var (isDonk, situation) = PreflopAnalyzer.DetectDonkBet(5m, false, HandSituation.ThreeBet);

        Assert.That(isDonk, Is.True);
        Assert.That(situation, Is.EqualTo(HandSituation.DonkBet));
    }

    [Test]
    public void DetectDonkBet_VillainAgresor_NoEsDonk()
    {
        var (isDonk, situation) = PreflopAnalyzer.DetectDonkBet(5m, true, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.False);
        Assert.That(situation, Is.EqualTo(HandSituation.OpenRaise));
    }

    #endregion

    #region CategorizeOpponentBet

    [Test]
    public void CategorizeOpponentBet_Cero_NoBet()
    {
        Assert.That(PreflopAnalyzer.CategorizeOpponentBet(0m, 100m), Is.EqualTo(BetSizeCategory.NoBet));
    }

    [TestCase(10, 100, BetSizeCategory.Small)]     // 10% del pot
    [TestCase(30, 100, BetSizeCategory.Small)]      // 30% del pot (límite)
    [TestCase(31, 100, BetSizeCategory.Medium)]     // 31% del pot
    [TestCase(50, 100, BetSizeCategory.Medium)]     // 50% del pot
    [TestCase(70, 100, BetSizeCategory.Medium)]     // 70% del pot (límite)
    [TestCase(71, 100, BetSizeCategory.Large)]      // 71% del pot
    [TestCase(100, 100, BetSizeCategory.Large)]     // pot size bet
    public void CategorizeOpponentBet_DeberiaCategorizarCorrectamente(decimal bet, decimal pot, BetSizeCategory expected)
    {
        Assert.That(PreflopAnalyzer.CategorizeOpponentBet(bet, pot), Is.EqualTo(expected));
    }

    #endregion
}
