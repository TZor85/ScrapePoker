using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services.Http;
using OpenScrape.App.Tests.Api;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El cliente HTTP sólo conoce el contrato. Cuando el motor no está disponible
/// debe cortar, no inventar una decisión ni caer a un cálculo local.
/// </summary>
[TestFixture]
public class HttpDecisionEngineTests
{
    [Test]
    public async Task DecideAsync_ApiEnProceso_DevuelveLaDecisionDelServidor()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);

        var result = await engine.DecideAsync(PreflopRequest());

        Assert.That(result.Action, Is.EqualTo("Raise"));
    }

    [Test]
    public void DecideAsync_ApiCaida_LanzaEngineUnavailable()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9") };
        var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);

        Assert.ThrowsAsync<EngineUnavailableException>(
            async () => await engine.DecideAsync(PreflopRequest()));
    }

    private static DecisionRequest PreflopRequest() => new()
    {
        Street = DecisionStreet.Preflop,
        HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
        HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
        PotSize = 3m,
        BetToCall = 3m,
        HeroStack = 100m,
        VillainStack = 100m,
        HeroPosition = TablePosition.Button,
        VillainPosition = TablePosition.BigBlind,
        IsInPosition = true,
        HandSituation = HandSituation.None
    };
}
