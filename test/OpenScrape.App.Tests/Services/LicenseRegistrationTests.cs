using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenScrape.App.Services.License;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El cliente siempre decide por HTTP, así que la licencia no puede depender de
/// un flag: el store del token y el cliente de activación tienen que estar
/// registrados para que <c>HttpDecisionEngine</c> mande el Bearer.
/// </summary>
[TestFixture]
public class LicenseRegistrationTests
{
    [Test]
    public void AddLicense_RegistraElStoreYElClienteDeActivacion()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encrypter:Key"] = "secreto-de-test",
                ["Api:BaseUrl"] = "http://localhost:5000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLicense(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<LicenseTokenStore>(), Is.Not.Null);
        Assert.That(provider.GetRequiredService<LicenseClient>(), Is.Not.Null);
    }

    [Test]
    public void AddLicense_SinSecretoDeCifrado_FallaAlResolver()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:BaseUrl"] = "http://localhost:5000"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLicense(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<LicenseTokenStore>());
    }
}
