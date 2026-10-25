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

    /// <summary>
    /// El cliente ya no tiene camino local: el flag RemoteEngine no puede seguir
    /// decidiendo si se pide licencia o si se arranca el motor.
    /// </summary>
    [Test]
    public void App_NoLeeElFlagRemoteEngine()
    {
        var sources = Directory.GetFiles(FindDir(), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(FindDir(), "appsettings*.json", SearchOption.AllDirectories));

        Assert.That(
            sources.Select(File.ReadAllText).Any(text => text.Contains("RemoteEngine")),
            Is.False);
    }

    /// <summary>
    /// El csproj no alcanza: una referencia transitiva (por ejemplo, si Features
    /// referenciara el motor) metería DecisionMaker.dll en el publish. El grafo
    /// restaurado es la fuente de verdad de lo que viaja al artefacto.
    /// </summary>
    [Test]
    public void App_NoArrastraDecisionMakerEnSusDependencias()
    {
        var assets = Path.Combine(FindDir(), "obj", "project.assets.json");

        if (!File.Exists(assets))
            Assert.Ignore("Falta obj/project.assets.json: restaurá el proyecto antes de correr este test.");

        Assert.That(File.ReadAllText(assets), Does.Not.Contain("OpenScrape.DecisionMaker"));
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
