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
        // La máquina puede tener Jwt__SigningKey en el entorno (lo deja
        // install-api-service.ps1 al cargar .env.dev). Este test necesita que el
        // host no la vea, así que la quita mientras dura y la restaura después.
        const string variable = "Jwt__SigningKey";
        var previous = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, null);

        try
        {
            using var factory = new WebApplicationFactory<ApiMarker>();

            var exception = Assert.Catch(() => factory.CreateClient());

            Assert.That(exception!.ToString(), Does.Contain("Jwt:SigningKey"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }
}
