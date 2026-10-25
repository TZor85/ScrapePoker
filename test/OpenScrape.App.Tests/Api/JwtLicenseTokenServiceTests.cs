using Microsoft.Extensions.Options;
using OpenScrape.Api.Licensing;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// El token es la credencial de cada request: firma, sujeto y vencimiento
/// tienen que fallar cerrado.
/// </summary>
[TestFixture]
public class JwtLicenseTokenServiceTests
{
    private const string Key = "clave-de-prueba-con-mas-de-32-bytes-0001";
    private const string OtherKey = "otra-clave-de-prueba-con-32-bytes-000002";

    [Test]
    public void Issue_YLuegoValidate_DevuelveElSujetoYElPlan()
    {
        var service = Service();
        var license = new License
        {
            Id = Guid.NewGuid(),
            Plan = "pro",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        var (token, expiresAt) = service.Issue(license);

        Assert.That(service.TryValidate(token, out var claims), Is.True);
        Assert.That(claims.Subject, Is.EqualTo(license.Id));
        Assert.That(claims.Plan, Is.EqualTo("pro"));
        Assert.That(expiresAt, Is.GreaterThan(DateTimeOffset.UtcNow));
    }

    [Test]
    public void TryValidate_FirmaDeOtraKey_EsFalse()
    {
        var (token, _) = Service().Issue(new License
        {
            Id = Guid.NewGuid(),
            Plan = "pro",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        });

        Assert.That(Service(OtherKey).TryValidate(token, out _), Is.False);
    }

    [Test]
    public void TryValidate_TokenVencido_EsFalse()
    {
        var (token, _) = Service().Issue(new License
        {
            Id = Guid.NewGuid(),
            Plan = "pro",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        });

        Assert.That(Service().TryValidate(token, out _), Is.False);
    }

    [Test]
    public void TryValidate_TextoQueNoEsJwt_EsFalse()
    {
        Assert.That(Service().TryValidate("no-es-un-token", out _), Is.False);
    }

    private static JwtLicenseTokenService Service(string key = Key, int lifetimeHours = 24) =>
        new(Options.Create(new JwtOptions { SigningKey = key, LifetimeHours = lifetimeHours }));
}
