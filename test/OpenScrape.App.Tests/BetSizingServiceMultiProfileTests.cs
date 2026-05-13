using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

[TestFixture]
public class BetSizingServiceMultiProfileTests
{
    private BetSizingService _service;

    [SetUp]
    public void Setup()
    {
        _service = new BetSizingService(Options.Create(new StrategyProfile()));
    }

    [Test]
    public void GetValueBetSizes_HighEquity_IP_ShouldReturnMultipleSizes()
    {
        var result = _service.GetValueBetSizes(
            equity: 85,
            spr: 10,
            texture: BoardTextureCategory.Dry,
            isInPosition: true,
            isMultiway: false);

        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Count, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void GetValueBetSizes_Multiway_ShouldReduceSizes()
    {
        var result1 = _service.GetValueBetSizes(70, 10, BoardTextureCategory.Dry, true, false);
        var result2 = _service.GetValueBetSizes(70, 10, BoardTextureCategory.Dry, true, true);

        foreach (var r in result2)
        {
            var matching = result1.FirstOrDefault(x => x.Label == r.Label);
            if (matching != null)
                Assert.That(r.Size, Is.LessThan(matching.Size));
        }
    }

    [Test]
    public void GetValueBetSizes_OOP_ShouldIncreaseSizes()
    {
        var resultIP = _service.GetValueBetSizes(70, 10, BoardTextureCategory.Dry, true, false);
        var resultOOP = _service.GetValueBetSizes(70, 10, BoardTextureCategory.Dry, false, false);

        Assert.That(resultOOP[0].Size, Is.GreaterThan(resultIP[0].Size));
    }

    [Test]
    public void GetBluffSizes_DryBoard_HighFoldEquity_ShouldReturnLargerSizes()
    {
        var result = _service.GetBluffSizes(
            foldEquity: 60,
            spr: 10,
            texture: BoardTextureCategory.Dry,
            isInPosition: true);

        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Any(x => x.Size >= 0.66), Is.True);
    }

    [Test]
    public void GetBluffSizes_WetBoard_ShouldReturnSmallerSizes()
    {
        var result = _service.GetBluffSizes(
            foldEquity: 40,
            spr: 10,
            texture: BoardTextureCategory.Wet,
            isInPosition: true);

        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Any(x => x.Size <= 0.5), Is.True);
    }

    [Test]
    public void GetBluffSizes_OOP_ShouldIncreaseSizes()
    {
        var resultIP = _service.GetBluffSizes(40, 10, BoardTextureCategory.Dry, true);
        var resultOOP = _service.GetBluffSizes(40, 10, BoardTextureCategory.Dry, false);

        Assert.That(resultOOP[0].Size, Is.GreaterThan(resultIP[0].Size));
    }

    [Test]
    public void GetThinValueThreshold_DryBoard_IP_ShouldBeLower()
    {
        var result = _service.GetThinValueThreshold(
            BoardTextureCategory.Dry,
            spr: 10,
            isInPosition: true);

        Assert.That(result, Is.LessThan(55));
    }

    [Test]
    public void GetThinValueThreshold_WetBoard_OOP_ShouldBeHigher()
    {
        var result = _service.GetThinValueThreshold(
            BoardTextureCategory.Wet,
            spr: 10,
            isInPosition: false);

        Assert.That(result, Is.GreaterThan(55));
    }

    [Test]
    public void GetThinValueThreshold_LowSPR_ShouldBeHigher()
    {
        var resultNormal = _service.GetThinValueThreshold(BoardTextureCategory.Dry, 10, true);
        var resultShallow = _service.GetThinValueThreshold(BoardTextureCategory.Dry, 2, true);

        Assert.That(resultShallow, Is.GreaterThan(resultNormal));
    }

    [Test]
    public void GetValueBetSizes_ContainsOverbetForVeryStrongHands()
    {
        var result = _service.GetValueBetSizes(
            equity: 95,
            spr: 15,
            texture: BoardTextureCategory.Dry,
            isInPosition: true,
            isMultiway: false);

        Assert.That(result.Any(x => x.Type == BetSizingType.Overbet), Is.True);
    }

    [Test]
    public void GetValueBetSizes_MediumEquity_ShouldIncludeThinValue()
    {
        var result = _service.GetValueBetSizes(
            equity: 60,
            spr: 10,
            texture: BoardTextureCategory.Dry,
            isInPosition: true,
            isMultiway: false);

        Assert.That(result.Any(x => x.Type == BetSizingType.ThinValue), Is.True);
    }

    [Test]
    public void GetBluffSizes_LowFoldEquity_ShouldBeSmall()
    {
        var result = _service.GetBluffSizes(
            foldEquity: 25,
            spr: 10,
            texture: BoardTextureCategory.Wet,
            isInPosition: true);

        Assert.That(result.All(x => x.Size <= 0.5), Is.True);
    }
}
