using System.Windows.Forms;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El panel de logs permite elegir un nivel concreto en caliente. El filtro tiene que
/// vivir en el proveedor: la UI sólo cambia la propiedad y el stream muestra ese nivel
/// y nada más, sin reiniciar ni perder el histórico ya cargado. Sin nivel elegido
/// ("Todos") se muestra lo que permita la configuración.
/// </summary>
[TestFixture]
public class TextBoxLoggerLevelFilterTests
{
    [Test]
    public void NivelElegido_MuestraSoloEseNivel()
    {
        var provider = CreateProvider(out var textBox);
        var logger = provider.CreateLogger("OpenScrape.App.Services.GameLoggerService");

        provider.ExactLevel = LogLevel.Warning;
        Log(logger, LogLevel.Information, "informacion que no debe verse");
        Log(logger, LogLevel.Warning, "warning que si debe verse");
        Log(logger, LogLevel.Error, "error que tampoco debe verse");

        Assert.Multiple(() =>
        {
            Assert.That(textBox.Text, Does.Not.Contain("informacion que no debe verse"));
            Assert.That(textBox.Text, Does.Contain("warning que si debe verse"));
            Assert.That(textBox.Text, Does.Not.Contain("error que tampoco debe verse"));
        });
    }

    [Test]
    public void SinNivelElegido_MuestraTodosLosNiveles()
    {
        var provider = CreateProvider(out var textBox);
        var logger = provider.CreateLogger("OpenScrape.App.Services.GameLoggerService");

        provider.ExactLevel = null;
        Log(logger, LogLevel.Information, "info visible");
        Log(logger, LogLevel.Warning, "warning visible");
        Log(logger, LogLevel.Error, "error visible");

        Assert.Multiple(() =>
        {
            Assert.That(textBox.Text, Does.Contain("info visible"));
            Assert.That(textBox.Text, Does.Contain("warning visible"));
            Assert.That(textBox.Text, Does.Contain("error visible"));
        });
    }

    [Test]
    public void NivelElegido_SePuedeCambiar()
    {
        var provider = CreateProvider(out var textBox);
        var logger = provider.CreateLogger("OpenScrape.App.Services.GameLoggerService");

        provider.ExactLevel = LogLevel.Error;
        Log(logger, LogLevel.Information, "todavia filtrada");

        provider.ExactLevel = LogLevel.Information;
        Log(logger, LogLevel.Information, "ahora visible");

        Assert.Multiple(() =>
        {
            Assert.That(textBox.Text, Does.Not.Contain("todavia filtrada"));
            Assert.That(textBox.Text, Does.Contain("ahora visible"));
        });
    }

    private static TextBoxLoggerProvider CreateProvider(out TextBox textBox)
    {
        var provider = new TextBoxLoggerProvider(Options.Create(new TextBoxLoggerOptions()));
        textBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false };
        provider.SetTextBoxTarget(textBox);

        return provider;
    }

    private static void Log(ILogger logger, LogLevel level, string message)
        => logger.Log(level, new EventId(0), message, exception: null, (state, _) => state);
}
