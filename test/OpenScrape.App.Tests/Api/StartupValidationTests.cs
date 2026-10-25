using Microsoft.AspNetCore.Mvc.Testing;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// Un servicio de licencias mal configurado no debe levantar: firmar tokens con
/// una key corta es peor que un arranque fallido.
/// </summary>
[TestFixture]
public class StartupValidationTests
{
    [Test]
    public void Host_SinSigningKey_NoArrancaYLoDice()
    {
        using var factory = new WebApplicationFactory<ApiMarker>();

        var exception = Assert.Catch(() => factory.CreateClient());

        Assert.That(exception!.ToString(), Does.Contain("Jwt:SigningKey"));
    }
}
