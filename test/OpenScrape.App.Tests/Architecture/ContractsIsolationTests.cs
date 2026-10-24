using System.Text.RegularExpressions;

namespace OpenScrape.App.Tests.Architecture;

/// <summary>
/// El contrato es lo único que el cliente y el servidor comparten: si arrastra
/// Marten o ASP.NET deja de poder publicarse como ensamblado de contratos.
/// </summary>
[TestFixture]
public class ContractsIsolationTests
{
    [Test]
    public void Contracts_NoReferenciaMartenNiAspNetCore()
    {
        var csproj = File.ReadAllText(ProjectPath());

        Assert.That(csproj, Does.Not.Contain("Marten"));
        Assert.That(csproj, Does.Not.Contain("AspNetCore"));
    }

    [Test]
    public void Contracts_SoloReferenciaDomain()
    {
        var csproj = File.ReadAllText(ProjectPath());

        var references = Regex
            .Matches(csproj, @"ProjectReference Include=""([^""]+)""")
            .Select(match => Path.GetFileNameWithoutExtension(match.Groups[1].Value))
            .ToArray();

        Assert.That(references, Is.EqualTo(new[] { "OpenScrape.Domain" }));
    }

    private static string ProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "OpenScrape.Contracts", "OpenScrape.Contracts.csproj");

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No se encontró OpenScrape.Contracts.csproj desde el directorio de tests.");
    }
}
