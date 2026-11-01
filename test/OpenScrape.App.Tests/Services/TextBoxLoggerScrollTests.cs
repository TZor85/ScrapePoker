using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El panel de logs agrega una línea por evento. Si el usuario scrolleó para leer
/// hacia arriba, el próximo evento no debe arrastrarlo al final: el panel sólo
/// sigue el stream cuando ya estaba abajo.
/// </summary>
[TestFixture]
public class TextBoxLoggerScrollTests
{
    [Test]
    public void Append_ConElCursorAlFinal_SigueElStream()
    {
        var provider = CreateProvider(out var textBox);
        Log(provider, "primera");
        textBox.SelectionStart = textBox.TextLength;

        Log(provider, "segunda");

        Assert.That(textBox.SelectionStart, Is.EqualTo(textBox.TextLength),
            "Si el cursor estaba al final, el panel debe seguir mostrando lo último.");
    }

    [Test]
    public void Append_ConElCursorArriba_NoMueveAlUsuario()
    {
        var provider = CreateProvider(out var textBox);
        Log(provider, "primera");
        Log(provider, "segunda");
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 0;

        Log(provider, "tercera");

        Assert.That(textBox.SelectionStart, Is.EqualTo(0),
            "Si el usuario está leyendo arriba, el evento nuevo no debe robarle el scroll.");
        Assert.That(textBox.Text, Does.Contain("tercera"), "El evento se sigue registrando.");
    }

    private static TextBoxLoggerProvider CreateProvider(out TextBox textBox)
    {
        var provider = new TextBoxLoggerProvider(Options.Create(new TextBoxLoggerOptions()));
        textBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false };
        provider.SetTextBoxTarget(textBox);

        return provider;
    }

    private static void Log(TextBoxLoggerProvider provider, string message)
    {
        var logger = provider.CreateLogger("OpenScrape.App.Services.GameLoggerService");
        logger.LogInformation("{Message}", message);
    }
}
