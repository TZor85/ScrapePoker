using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OpenScrape.Api.Licensing;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// La decisión exige licencia: sin token no se evalúa nada, y la cuota depende del plan.
/// </summary>
[TestFixture]
public class DecisionAuthTests
{
    private ApiFactory _factory = null!;

    [SetUp]
    public void SetUp() => _factory = new ApiFactory();

    [TearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task SinToken_Devuelve401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        var error = await response.Content.ReadFromJsonAsync<EngineError>(JsonConfig.Options);
        Assert.That(error!.Code, Is.EqualTo("unauthorized"));
    }

    [Test]
    public async Task TokenConFirmaInvalida_Devuelve401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "no.es.un.jwt");

        var response = await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TokenVencido_Devuelve401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", Token(expiresIn: TimeSpan.FromHours(-1)));

        var response = await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task LicenciaRevocadaConCheckPerRequest_Devuelve403()
    {
        await _factory.LicenseStore.RevokeAsync(LicenseTestData.ProLicenseId);
        _factory.EnableRevocationCheck();

        using var client = LicensedClient("pro");
        var response = await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        var error = await response.Content.ReadFromJsonAsync<EngineError>(JsonConfig.Options);
        Assert.That(error!.Code, Is.EqualTo("license_revoked"));
    }

    [Test]
    public async Task LicenciaRevocadaSinCheckPerRequest_SigueEntrandoHastaVencer()
    {
        await _factory.LicenseStore.RevokeAsync(LicenseTestData.ProLicenseId);

        using var client = LicensedClient("pro");
        var response = await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task PlanFree_RequestNumeroOnceEnElMinuto_Devuelve429()
    {
        using var client = LicensedClient("free");

        HttpStatusCode last = HttpStatusCode.OK;
        for (var i = 0; i < 11; i++)
            last = (await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options)).StatusCode;

        Assert.That(last, Is.EqualTo(HttpStatusCode.TooManyRequests));
    }

    [Test]
    public async Task PlanPro_NoCortaEnLaOnce()
    {
        using var client = LicensedClient("pro");

        HttpStatusCode last = HttpStatusCode.OK;
        for (var i = 0; i < 11; i++)
            last = (await client.PostAsJsonAsync("/v1/decisions", PreflopRequest(), JsonConfig.Options)).StatusCode;

        Assert.That(last, Is.EqualTo(HttpStatusCode.OK));
    }

    private HttpClient LicensedClient(string plan)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(plan: plan));
        return client;
    }

    private string Token(string plan = "pro", TimeSpan? expiresIn = null)
    {
        var tokens = _factory.Services.GetRequiredService<ILicenseTokenService>();
        var id = plan == "pro" ? LicenseTestData.ProLicenseId : LicenseTestData.FreeLicenseId;
        var license = _factory.LicenseStore.GetAsync(id).GetAwaiter().GetResult()!;

        if (expiresIn is not null)
            license = new License
            {
                Id = license.Id,
                Plan = license.Plan,
                ExpiresAt = DateTimeOffset.UtcNow.Add(expiresIn.Value),
                KeyHash = license.KeyHash,
                CreatedAt = license.CreatedAt
            };

        return tokens.Issue(license).Token;
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
