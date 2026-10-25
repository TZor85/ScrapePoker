namespace OpenScrape.App.Tests.Architecture;

/// <summary>
/// El cliente consume el motor por HTTP: no puede quedar ninguna referencia al
/// ensamblado del motor ni al camino local que lo instanciaba.
/// </summary>
[TestFixture]
public class AppEngineIsolationTests
{
    [Test]
    public void App_NoReferenciaDecisionMaker()
    {
        var csproj = File.ReadAllText(FindProject());

        Assert.That(csproj, Does.Not.Contain("OpenScrape.DecisionMaker"));
    }

    [Test]
    public void App_NoUsaLocalDecisionEngine()
    {
        var files = Directory.GetFiles(FindDir(), "*.cs", SearchOption.AllDirectories);

        Assert.That(files.Select(File.ReadAllText).Any(text => text.Contains("LocalDecisionEngine")), Is.False);
    }

    private static string FindProject() =>
        Path.Combine(FindDir(), "OpenScrape.App.csproj");

    private static string FindDir()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "OpenScrape.App");

            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró src/OpenScrape.App desde el directorio de tests.");
    }
}
