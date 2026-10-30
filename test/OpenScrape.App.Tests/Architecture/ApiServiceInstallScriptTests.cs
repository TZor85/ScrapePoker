using System.Text.RegularExpressions;

namespace OpenScrape.App.Tests.Architecture;

/// <summary>
/// El servicio de la API se publica self-contained: en la PC destino no hay garantía
/// de que exista el runtime de .NET 10 y, sin él, el servicio no arranca al inicio de
/// Windows. El publish framework-dependent hacía que la instalación dependiera de un
/// prerequisito silencioso.
/// </summary>
[TestFixture]
public class ApiServiceInstallScriptTests
{
    [Test]
    public void El_Publish_Del_Script_Es_SelfContained_Para_WinX64()
    {
        var comando = PublishCommand(File.ReadAllText(ScriptPath()));

        Assert.Multiple(() =>
        {
            Assert.That(comando, Does.Match(@"-r\s+win-x64|--runtime\s+win-x64"),
                "El publish debe fijar el runtime win-x64.");
            Assert.That(comando, Does.Match(@"--self-contained(\s+true)?|-p:SelfContained=true"),
                "El publish debe ser self-contained: si no, la máquina destino necesita .NET 10 instalado.");
        });
    }

    private static string PublishCommand(string script)
    {
        var match = Regex.Match(script, @"dotnet\s+publish[^\r\n]*");
        Assert.That(match.Success, Is.True, "No se encontró el comando dotnet publish en el script.");

        return match.Value;
    }

    private static string ScriptPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "scripts", "install-api-service.ps1");

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No se encontró install-api-service.ps1 desde el directorio de tests.");
    }
}
