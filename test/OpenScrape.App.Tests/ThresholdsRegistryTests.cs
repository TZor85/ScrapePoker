using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class ThresholdsRegistryTests
{
    private static StrategyProfile BuildProfile(params (string key, StreetThresholds value)[] entries)
    {
        var profile = new StrategyProfile { Name = "Test" };
        foreach (var (key, value) in entries)
            profile.Thresholds[key] = value;
        return profile;
    }

    private static ThresholdsRegistry BuildRegistry(StrategyProfile profile)
        => new(Options.Create(profile));

    private static StreetThresholds Sample(double foldBelow = 40) => new()
    {
        FoldBelow = foldBelow,
        ThinValueAbove = foldBelow + 5,
        ValueAbove = foldBelow + 15,
        StrongValueAbove = foldBelow + 35
    };

    [Test]
    public void Get_ClaveExistente_RetornaThresholds()
    {
        var profile = BuildProfile(("Flop_OpenRaise", Sample(35)));
        var registry = BuildRegistry(profile);

        var result = registry.Get(new ThresholdKey(BoardPosition.Flop, HandSituation.OpenRaise));

        Assert.That(result.FoldBelow, Is.EqualTo(35));
    }

    [Test]
    public void Get_ClaveInexistente_LanzaKeyNotFoundException()
    {
        var profile = BuildProfile(("Flop_OpenRaise", Sample()));
        var registry = BuildRegistry(profile);

        var ex = Assert.Throws<KeyNotFoundException>(
            () => registry.Get(new ThresholdKey(BoardPosition.River, HandSituation.DonkBet)));

        Assert.That(ex!.Message, Does.Contain("River_DonkBet"));
    }

    [Test]
    public void TryGet_ClaveInexistente_RetornaFalseSinLanzar()
    {
        var profile = BuildProfile(("Flop_OpenRaise", Sample()));
        var registry = BuildRegistry(profile);

        var found = registry.TryGet(
            new ThresholdKey(BoardPosition.River, HandSituation.DonkBet),
            out var thresholds);

        Assert.That(found, Is.False);
        Assert.That(thresholds, Is.Null);
    }

    [Test]
    public void TryGet_ClaveExistente_RetornaTrueConValor()
    {
        var profile = BuildProfile(("Turn_Squeeze", Sample(42)));
        var registry = BuildRegistry(profile);

        var found = registry.TryGet(
            new ThresholdKey(BoardPosition.Turn, HandSituation.Squeeze),
            out var thresholds);

        Assert.That(found, Is.True);
        Assert.That(thresholds.FoldBelow, Is.EqualTo(42));
    }

    [Test]
    public void Contains_ClaveExistente_RetornaTrue()
    {
        var profile = BuildProfile(("Flop_Call", Sample()));
        var registry = BuildRegistry(profile);

        Assert.That(registry.Contains(new ThresholdKey(BoardPosition.Flop, HandSituation.Call)), Is.True);
        Assert.That(registry.Contains(new ThresholdKey(BoardPosition.River, HandSituation.Call)), Is.False);
    }

    [Test]
    public void Keys_DevuelveTodasLasClavesParseadas()
    {
        var profile = BuildProfile(
            ("Flop_OpenRaise", Sample()),
            ("Turn_ThreeBet", Sample()));
        var registry = BuildRegistry(profile);

        var keys = registry.Keys;

        Assert.That(keys, Has.Count.EqualTo(2));
        Assert.That(keys, Does.Contain(new ThresholdKey(BoardPosition.Flop, HandSituation.OpenRaise)));
        Assert.That(keys, Does.Contain(new ThresholdKey(BoardPosition.Turn, HandSituation.ThreeBet)));
    }

    [Test]
    public void Constructor_ClaveStringConStreetDesconocido_LanzaInvalidOperationException()
    {
        var profile = BuildProfile(("Preflop_OpenRaise", Sample()));

        var ex = Assert.Throws<InvalidOperationException>(() => BuildRegistry(profile));

        Assert.That(ex!.Message, Does.Contain("Preflop_OpenRaise"));
        Assert.That(ex.Message, Does.Contain("BoardPosition"));
    }

    [Test]
    public void Constructor_ClaveStringConSituationDesconocida_LanzaInvalidOperationException()
    {
        var profile = BuildProfile(("Turn_Foobar", Sample()));

        var ex = Assert.Throws<InvalidOperationException>(() => BuildRegistry(profile));

        Assert.That(ex!.Message, Does.Contain("Turn_Foobar"));
        Assert.That(ex.Message, Does.Contain("HandSituation"));
    }

    [Test]
    public void Constructor_ClaveStringMalformada_LanzaInvalidOperationException()
    {
        var profile = BuildProfile(("FlopOpenRaise", Sample()));

        var ex = Assert.Throws<InvalidOperationException>(() => BuildRegistry(profile));

        Assert.That(ex!.Message, Does.Contain("FlopOpenRaise"));
    }

    [Test]
    public void Constructor_ClaveConBoardPositionProhibida_LanzaInvalidOperationException()
    {
        var profile = BuildProfile(("Hand_OpenRaise", Sample()));

        Assert.Throws<InvalidOperationException>(() => BuildRegistry(profile));
    }

    [Test]
    public void DI_ResolucionMultipleDevuelveMismaInstancia()
    {
        var services = new ServiceCollection();
        services.Configure<StrategyProfile>(options =>
        {
            options.Thresholds["Flop_OpenRaise"] = Sample();
        });
        services.AddSingleton<ThresholdsRegistry>();
        services.AddSingleton<IThresholdsRegistry>(sp => sp.GetRequiredService<ThresholdsRegistry>());

        using var provider = services.BuildServiceProvider();

        var a = provider.GetRequiredService<IThresholdsRegistry>();
        var b = provider.GetRequiredService<IThresholdsRegistry>();

        Assert.That(a, Is.SameAs(b));
    }
}
