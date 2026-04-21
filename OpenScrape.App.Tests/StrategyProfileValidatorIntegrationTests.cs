using Microsoft.Extensions.Configuration;

using OpenScrape.App.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests;

[TestFixture]
public class StrategyProfileValidatorIntegrationTests
{
    [Test]
    public void Validate_AppSettingsRealDelRepositorio_NoLanza()
    {
        var appSettingsPath = LocateAppSettings();
        Assert.That(File.Exists(appSettingsPath), Is.True, $"No se encuentra appsettings.json en {appSettingsPath}");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
            .Build();

        var profile = new StrategyProfile();
        configuration.GetSection("StrategyProfile").Bind(profile);

        Assert.That(profile.Thresholds, Is.Not.Empty, "El binding no cargó thresholds desde appsettings.json.");

        Assert.DoesNotThrow(() => StrategyProfileValidator.Validate(profile));
    }

    private static string LocateAppSettings()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "OpenScrape.App", "appsettings.json");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(
            "No se pudo localizar src/OpenScrape.App/appsettings.json subiendo desde AppContext.BaseDirectory.");
    }
}
