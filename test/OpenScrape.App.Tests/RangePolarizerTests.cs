using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class RangePolarizerTests
{
    private RangePolarizer _polarizer;

    [SetUp]
    public void Setup()
    {
        _polarizer = new RangePolarizer();
    }

    [Test]
    public void GetOptimalRangeType_DryBoard_IP_ShouldBePolarized()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: true, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Polarized));
    }

    [Test]
    public void GetOptimalRangeType_DryBoard_OOP_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: false, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetOptimalRangeType_WetBoard_IP_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Wet, isInPosition: true, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetOptimalRangeType_WetBoard_OOP_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Wet, isInPosition: false, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetOptimalRangeType_PairedBoard_IP_ShouldBePolarized()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Paired, isInPosition: true, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Polarized));
    }

    [Test]
    public void GetOptimalRangeType_PairedBoard_OOP_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Paired, isInPosition: false, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetOptimalRangeType_SemiWetBoard_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.SemiWet, isInPosition: true, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetOptimalRangeType_SemiDryBoard_ShouldBePolarized()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.SemiDry, isInPosition: true, spr: 10, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Polarized));
    }

    [Test]
    public void GetOptimalRangeType_SPRLow_ShouldBeCondensed()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: true, spr: 2, street: BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Condensed));
    }

    [Test]
    public void GetOptimalRangeType_SPRHigh_ShouldNotBeCondensed()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: true, spr: 20, street: BoardPosition.Flop);

        Assert.That(result, Is.Not.EqualTo(RangeType.Condensed));
    }

    [Test]
    public void GetOptimalRangeType_River_IP_ShouldBePolarized()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: true, spr: 10, street: BoardPosition.River);

        Assert.That(result, Is.EqualTo(RangeType.Polarized));
    }

    [Test]
    public void GetOptimalRangeType_River_OOP_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: false, spr: 10, street: BoardPosition.River);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetThresholdAdjustment_Polarized_IP_ShouldAdjustLoose()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustment(RangeType.Polarized, isInPosition: true);

        Assert.That(foldBelow, Is.EqualTo(-4.0));
        Assert.That(thinValue, Is.EqualTo(-3.0));
    }

    [Test]
    public void GetThresholdAdjustment_Polarized_OOP_ShouldAdjustLoose()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustment(RangeType.Polarized, isInPosition: false);

        Assert.That(foldBelow, Is.EqualTo(-2.0));
        Assert.That(thinValue, Is.EqualTo(-2.0));
    }

    [Test]
    public void GetThresholdAdjustment_Linear_IP_NoAdjustment()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustment(RangeType.Linear, isInPosition: true);

        Assert.That(foldBelow, Is.EqualTo(0.0));
        Assert.That(thinValue, Is.EqualTo(0.0));
    }

    [Test]
    public void GetThresholdAdjustment_Linear_OOP_ShouldAdjustTight()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustment(RangeType.Linear, isInPosition: false);

        Assert.That(foldBelow, Is.EqualTo(3.0));
        Assert.That(thinValue, Is.EqualTo(2.0));
    }

    [Test]
    public void GetThresholdAdjustment_Condensed_ShouldAdjustVeryTight()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustment(RangeType.Condensed, isInPosition: true);

        Assert.That(foldBelow, Is.EqualTo(6.0));
        Assert.That(thinValue, Is.EqualTo(4.0));
    }

    [Test]
    public void GetOptimalRangeTypeByWetness_DryScore_IP_ShouldBePolarized()
    {
        var result = _polarizer.GetOptimalRangeTypeByWetness(20, isInPosition: true, 10, BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Polarized));
    }

    [Test]
    public void GetOptimalRangeTypeByWetness_WetScore_IP_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeTypeByWetness(60, isInPosition: true, 10, BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetThresholdAdjustmentBySituation_Dry_IP_ShouldAdjustLoose()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustmentBySituation(
            BoardTextureCategory.Dry, isInPosition: true, 10, BoardPosition.Flop);

        Assert.That(foldBelow, Is.EqualTo(-4.0));
    }

    [Test]
    public void GetThresholdAdjustmentBySituation_Wet_OOP_ShouldAdjustTight()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustmentBySituation(
            BoardTextureCategory.Wet, isInPosition: false, 10, BoardPosition.Flop);

        Assert.That(foldBelow, Is.EqualTo(3.0));
    }

    [Test]
    public void GetThresholdAdjustmentBySituation_SPRLow_ShouldBeCondensed()
    {
        var (foldBelow, thinValue) = _polarizer.GetThresholdAdjustmentBySituation(
            BoardTextureCategory.Dry, isInPosition: true, 2, BoardPosition.Flop);

        Assert.That(foldBelow, Is.EqualTo(6.0));
    }

    [Test]
    public void GetOptimalRangeType_Turn_ShouldFollowTexture()
    {
        var resultIP = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: true, 10, BoardPosition.Turn);
        var resultOOP = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Dry, isInPosition: false, 10, BoardPosition.Turn);

        Assert.That(resultIP, Is.EqualTo(RangeType.Polarized));
        Assert.That(resultOOP, Is.EqualTo(RangeType.Linear));
    }

    [Test]
    public void GetOptimalRangeType_Monotone_ShouldBeLinear()
    {
        var result = _polarizer.GetOptimalRangeType(
            BoardTextureCategory.Wet, isInPosition: true, 10, BoardPosition.Flop);

        Assert.That(result, Is.EqualTo(RangeType.Linear));
    }
}
