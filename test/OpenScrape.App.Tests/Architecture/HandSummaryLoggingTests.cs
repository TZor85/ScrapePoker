using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenScrape.App.Tests.Architecture;

/// <summary>
/// El resumen de cada calle (Hero, pot, equity, decisión) es información de la mano,
/// no un error. Se logueaba con LogError como atajo para saltarse el filtro de
/// persistencia, y eso hacía que el panel lo mostrara en rojo y que el filtro de
/// nivel Error lo mezclara con fallos reales.
/// </summary>
[TestFixture]
public class HandSummaryLoggingTests
{
    [Test]
    public void LosResumenesDeLaMano_SeLogueanComoInformacion()
    {
        var source = File.ReadAllText(FrmMainPath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("LogError(\"{LogText}\""),
                "El resumen de la calle debe registrarse como información, no como error.");
            Assert.That(CountOf(source, "LogInformation(\"{LogText}\", result.LogText)"), Is.EqualTo(3),
                "Los tres resúmenes (preflop, flop y turn) deben usar LogInformation.");
        });
    }

    [Test]
    public void ElAgresorPreflop_SeLogueaComoInformacion()
    {
        var source = File.ReadAllText(FrmMainPath());

        Assert.That(source, Does.Not.Contain("LogError(\"[PREFLOP] Aggressors set"),
            "Saber quién agredió es información de la mano, no un error.");
    }

    [Test]
    public void LaCategoriaDeFrmMain_EstaEnLasCategoriasDeLaMano()
    {
        // Sin esto los resúmenes pasados a Information no se persistirían: el filtro
        // sólo guarda Information/Debug de las categorías listadas en HandCategories.
        using var document = JsonDocument.Parse(File.ReadAllText(AppSettingsPath()));

        var categorias = document.RootElement
            .GetProperty("Logging").GetProperty("Marten").GetProperty("HandCategories")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        // La categoría real es el namespace de FrmMain, no una ruta de carpetas: ya nos
        // pasó de listar "OpenScrape.App.Forms.FrmMain" y no persistir ningún Information.
        var categoriaEsperada = $"{NamespaceOf(FrmMainPath())}.FrmMain";

        Assert.That(categorias, Does.Contain(categoriaEsperada),
            $"FrmMain debe estar en HandCategories como '{categoriaEsperada}' o sus " +
            "resúmenes de mano no se guardan.");
    }

    private static string NamespaceOf(string path)
    {
        var match = Regex.Match(File.ReadAllText(path), @"^namespace\s+([\w.]+)", RegexOptions.Multiline);
        Assert.That(match.Success, Is.True, "No se encontró el namespace de FrmMain.cs");

        return match.Groups[1].Value;
    }

    private static int CountOf(string source, string value)
    {
        var count = 0;
        var index = source.IndexOf(value, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = source.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string FrmMainPath() => RepoFile("src", "OpenScrape.App", "Forms", "FrmMain.cs");

    private static string AppSettingsPath() => RepoFile("src", "OpenScrape.App", "appsettings.json");

    private static string RepoFile(params string[] relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativePath).ToArray());

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"No se encontró {Path.Combine(relativePath)} desde el directorio de tests.");
    }
}
