using System.Drawing;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El stream en vivo tiene que usar el mismo contrato de color que el histórico:
/// el prefijo del nivel pintado y el mensaje en gris.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class TextBoxLoggerColorTests
{
    [Test]
    public void Emit_ConAviso_PintaElPrefijoEnAmarillo()
    {
        var provider = CreateProvider(out var box);
        var logger = provider.CreateLogger("OpenScrape.App.Services.GameLoggerService");

        logger.LogWarning("algo raro");

        Assert.That(ColorAt(box, 0), Is.EqualTo(LogLineColors.For(LogLevel.Warning).ToArgb()));
    }

    [Test]
    public void Emit_ConInformacion_NoEscribeMarca()
    {
        var provider = CreateProvider(out var box);
        var logger = provider.CreateLogger("OpenScrape.App.Services.GameLoggerService");

        logger.LogInformation("arrancó");

        Assert.That(box.Lines[0], Does.StartWith("  ["));
    }

    private static TextBoxLoggerProvider CreateProvider(out RichTextBox box)
    {
        var options = Options.Create(new TextBoxLoggerOptions
        {
            MinimumLevel = LogLevel.Trace,
            MaxLines = 100
        });

        var provider = new TextBoxLoggerProvider(options);
        box = new RichTextBox
        {
            Multiline = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            Font = new Font("Consolas", 9F),
            ForeColor = LogLineColors.Message
        };

        provider.SetTextBoxTarget(box);
        return provider;
    }

    private static int ColorAt(RichTextBox box, int index)
    {
        box.Select(index, 1);
        return box.SelectionColor.ToArgb();
    }
}
