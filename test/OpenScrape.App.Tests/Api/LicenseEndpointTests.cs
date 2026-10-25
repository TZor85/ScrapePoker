using System.Net;
using System.Net.Http.Json;
using OpenScrape.Contracts;
using OpenScrape.Contracts.Licensing;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Activación: la key entra una vez y sale un token. Ni la key ni su existencia
/// se filtran en los errores.
/// </summary>
[TestFixture]
public class LicenseEndpointTests
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Activate_KeyValida_DevuelveTokenPlanYVencimiento()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/license/activate",
            new LicenseActivateRequest { LicenseKey = LicenseTestData.ValidKey },
            JsonConfig.Options);

        var body = await response.Content.ReadAsStringAsync();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

        var result = await response.Content.ReadFromJsonAsync<LicenseActivateResponse>(JsonConfig.Options);
        Assert.That(result!.Plan, Is.EqualTo("pro"));
        Assert.That(result.Token, Is.Not.Empty);
        Assert.That(result.ExpiresAt, Is.GreaterThan(DateTimeOffset.UtcNow));
    }

    [Test]
    public async Task Activate_KeyInexistente_Devuelve401SinFiltrarLaKey()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/license/activate",
            new LicenseActivateRequest { LicenseKey = "OSPK-0000-0000-0000" },
            JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

        var error = await response.Content.ReadFromJsonAsync<EngineError>(JsonConfig.Options);
        Assert.That(error!.Code, Is.EqualTo("license_invalid"));
        Assert.That(error.Message, Does.Not.Contain("OSPK-"));
    }

    [Test]
    public async Task Activate_KeyRevocada_Devuelve401()
    {
        await _factory.LicenseStore.RevokeAsync(LicenseTestData.ProLicenseId);

        var response = await _client.PostAsJsonAsync(
            "/v1/license/activate",
            new LicenseActivateRequest { LicenseKey = LicenseTestData.ValidKey },
            JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Activate_KeyVacia_Devuelve400()
    {
        var response = await _client.PostAsJsonAsync(
            "/v1/license/activate",
            new LicenseActivateRequest { LicenseKey = "  " },
            JsonConfig.Options);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
