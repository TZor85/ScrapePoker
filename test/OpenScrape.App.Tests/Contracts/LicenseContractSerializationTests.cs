using System.Text.Json;
using OpenScrape.App.Tests.Api;
using OpenScrape.Contracts.Licensing;

namespace OpenScrape.App.Tests.Contracts;

/// <summary>
/// Contrato de activación: lo comparten el cliente y la API, así que su forma
/// JSON es parte del acuerdo y se fija acá.
/// </summary>
[TestFixture]
public class LicenseContractSerializationTests
{
    [Test]
    public void Request_CamelCase_DeserializaLicenseKey()
    {
        var request = JsonSerializer.Deserialize<LicenseActivateRequest>(
            """{"licenseKey":"OSPK-1111-2222-3333"}""", JsonConfig.Options);

        Assert.That(request!.LicenseKey, Is.EqualTo("OSPK-1111-2222-3333"));
    }

    [Test]
    public void Response_RoundTrip_ConservaTokenPlanYVencimiento()
    {
        var original = new LicenseActivateResponse
        {
            Token = "abc.def.ghi",
            Plan = "pro",
            ExpiresAt = new DateTimeOffset(2026, 10, 26, 15, 0, 0, TimeSpan.Zero)
        };

        var json = JsonSerializer.Serialize(original, JsonConfig.Options);

        Assert.That(json, Does.Contain("\"plan\":\"pro\""));
        Assert.That(
            JsonSerializer.Deserialize<LicenseActivateResponse>(json, JsonConfig.Options),
            Is.EqualTo(original));
    }
}
