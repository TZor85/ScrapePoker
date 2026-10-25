using System.Text.Json;
using System.Text.Json.Serialization;

using OpenScrape.App.Tests.Fixtures;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// El perfil que carga la API tiene que ser el mismo contra el que se
/// caracterizaron las decisiones; si se separan, la paridad deja de significar algo.
/// </summary>
[TestFixture]
public class StrategyProfileDriftTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public void PerfilDeLaApi_CoincideConElFixture()
    {
        var root = LocateRepositoryRoot();
        var appSettingsPath = Path.Combine(root, "src", "OpenScrape.Api", "appsettings.json");
        Assert.That(File.Exists(appSettingsPath), Is.True, $"No se encontró {appSettingsPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
        var apiProfile = document.RootElement
            .GetProperty("StrategyProfile")
            .Deserialize<StrategyProfile>(Options);

        var fixture = StrategyProfileFixture.LoadOrCreate();

        Assert.That(apiProfile, Is.Not.Null);
        Assert.That(apiProfile!.Name, Is.EqualTo(fixture.Name));
        Assert.That(apiProfile.Thresholds.Keys, Is.EquivalentTo(fixture.Thresholds.Keys));

        foreach (var (key, expected) in fixture.Thresholds)
        {
            var actual = apiProfile.Thresholds[key];
            Assert.Multiple(() =>
            {
                Assert.That(actual.FoldBelow, Is.EqualTo(expected.FoldBelow), $"{key}.FoldBelow");
                Assert.That(actual.ThinValueAbove, Is.EqualTo(expected.ThinValueAbove), $"{key}.ThinValueAbove");
                Assert.That(actual.ValueAbove, Is.EqualTo(expected.ValueAbove), $"{key}.ValueAbove");
                Assert.That(actual.StrongValueAbove, Is.EqualTo(expected.StrongValueAbove), $"{key}.StrongValueAbove");
            });
        }
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "OpenScrape.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró la raíz del repositorio.");
    }
}
