using System.Net;
using System.Net.Http.Json;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Contrato HTTP de la fase 3a: una sola puerta para las cuatro calles y
/// errores de entrada traducidos a 422 con código estable.
/// </summary>
[TestFixture]
public class DecisionEndpointTests
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateLicensedClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task PostDecisions_PreflopAks_DevuelveRaise()
    {
        var response = await _client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options);

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

        var result = await response.Content.ReadFromJsonAsync<DecisionResult>(JsonConfig.Options);
        Assert.That(result!.Action, Is.EqualTo("Raise"));
    }

    [Test]
    public async Task PostDecisions_FlopConDosCartas_Devuelve422()
    {
        var request = PreflopRequest() with
        {
            Street = DecisionStreet.Flop,
            Board =
            [
                new CardRef((int)Rank.King, (int)Suit.Hearts),
                new CardRef((int)Rank.Seven, (int)Suit.Clubs)
            ]
        };

        var response = await _client.PostAsJsonAsync("/v1/decisions", request, JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
        var error = await response.Content.ReadFromJsonAsync<EngineError>(JsonConfig.Options);
        Assert.That(error!.Code, Is.EqualTo("invalid_request"));
    }

    [Test]
    public async Task GetHealth_SinBaseDeDatos_DevuelveOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Contain("ok"));
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
