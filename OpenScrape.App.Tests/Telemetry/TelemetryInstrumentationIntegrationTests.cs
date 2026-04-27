using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Telemetry;

public class TelemetryInstrumentationIntegrationTests
{
    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    private static DecisionRequest MakePostflopRequest() => new()
    {
        HeroCards = [C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts)],
        CommunityCards =
        [
            C(Rank.Queen, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Two, Suit.Clubs),
        ],
        Street = BoardPosition.Flop,
        Situation = HandSituation.OpenRaise,
        HeroStack = 100m,
        VillainStack = 100m,
        PotSize = 10m,
        BetToCall = 0m,
        IsInPosition = true,
        NumOpponents = 1,
        VillainId = "Unknown",
        VillainBetSize = BetSizeCategory.NoBet,
    };

    [Test]
    public async Task DecisionCategoriesPresente_WhenPostflopDecision_ThenAllDecisionCategoriesRecorded()
    {
        var metrics = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        var (facade, _) = new PokerDecisionFacadeTestBuilder()
            .WithMetrics(metrics)
            .Build();

        var request = MakePostflopRequest();

        var result = await facade.EvaluateAsync(request);

        Assert.That(result, Is.Not.Null);

        var snapshot = metrics.SnapshotSession();
        var lastHand = snapshot.LastHand;

        Assert.That(lastHand.ContainsKey(TelemetryCategories.DecisionTotal), Is.True,
            $"Categoría {TelemetryCategories.DecisionTotal} no encontrada");
        Assert.That(lastHand[TelemetryCategories.DecisionTotal].Count, Is.GreaterThan(0),
            $"{TelemetryCategories.DecisionTotal} debería tener Count > 0");

        Assert.That(lastHand.ContainsKey(TelemetryCategories.DecisionEquity), Is.True);
        Assert.That(lastHand[TelemetryCategories.DecisionEquity].Count, Is.GreaterThan(0));

        Assert.That(lastHand.ContainsKey(TelemetryCategories.DecisionTexture), Is.True);
        Assert.That(lastHand[TelemetryCategories.DecisionTexture].Count, Is.GreaterThan(0));

        Assert.That(lastHand.ContainsKey(TelemetryCategories.DecisionProfile), Is.True);
        Assert.That(lastHand[TelemetryCategories.DecisionProfile].Count, Is.GreaterThan(0));

        Assert.That(lastHand.ContainsKey(TelemetryCategories.DecisionDecisionService), Is.True);
        Assert.That(lastHand[TelemetryCategories.DecisionDecisionService].Count, Is.GreaterThan(0));

        Assert.That(lastHand.ContainsKey(TelemetryCategories.DecisionSizing), Is.True);
        Assert.That(lastHand[TelemetryCategories.DecisionSizing].Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task ExactCountPerSubphase_WhenCompleteDecisionCycle_ThenExactCountPerCategory()
    {
        var metrics = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        var (facade, _) = new PokerDecisionFacadeTestBuilder()
            .WithMetrics(metrics)
            .Build();

        var request = MakePostflopRequest();

        var result = await facade.EvaluateAsync(request);

        Assert.That(result, Is.Not.Null);

        var snapshot = metrics.SnapshotSession();
        var lastHand = snapshot.LastHand;

        Assert.That(lastHand[TelemetryCategories.DecisionTotal].Count, Is.EqualTo(1),
            $"{TelemetryCategories.DecisionTotal} debería tener exactamente 1");
        Assert.That(lastHand[TelemetryCategories.DecisionEquity].Count, Is.EqualTo(1));
        Assert.That(lastHand[TelemetryCategories.DecisionTexture].Count, Is.EqualTo(1));
        Assert.That(lastHand[TelemetryCategories.DecisionProfile].Count, Is.EqualTo(1));
        Assert.That(lastHand[TelemetryCategories.DecisionDecisionService].Count, Is.EqualTo(1));
        Assert.That(lastHand[TelemetryCategories.DecisionSizing].Count, Is.EqualTo(1));
    }
}