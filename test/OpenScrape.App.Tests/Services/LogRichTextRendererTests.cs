using System.Drawing;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El panel sólo puede pintar por línea si es un RichTextBox. El prefijo lleva el color
/// del nivel y el mensaje queda en el gris del tema para que un error largo siga siendo
/// legible.
/// </summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class LogRichTextRendererTests
{
    private const string Indent = "    ";

    [Test]
    public void Render_PintaElPrefijoConElColorDelNivelYElMensajeEnGris()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();
        var entry = Entry(LogLevel.Error, "falló la captura");

        renderer.Render(box, entry);

        var line = box.Lines[0];
        var split = line.IndexOf("falló", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(split, Is.GreaterThan(0));
            Assert.That(ColorAt(box, 0), Is.EqualTo(LogLineColors.For(LogLevel.Error).ToArgb()));
            Assert.That(ColorAt(box, split), Is.EqualTo(LogLineColors.Message.ToArgb()));
        });
    }

    [Test]
    public void Render_TraceSePintaEnVerde()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        renderer.Render(box, Entry(LogLevel.Trace, "detalle"));

        Assert.That(ColorAt(box, 0), Is.EqualTo(LogLineColors.For(LogLevel.Trace).ToArgb()));
    }

    [Test]
    public void Render_InformacionNoLlevaMarcaPeroConservaLaAlineacion()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        renderer.Render(box, Entry(LogLevel.Information, "arrancó"));

        Assert.That(box.Lines[0], Does.StartWith("  ["));
    }

    [Test]
    public void Render_RespetaElMaximoDeLineas()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        for (var i = 0; i < 5; i++)
            renderer.Render(box, Entry(LogLevel.Information, $"linea {i}"), maxLines: 3);

        Assert.Multiple(() =>
        {
            Assert.That(box.Lines.Length, Is.LessThanOrEqualTo(3));
            Assert.That(box.Text, Does.Contain("linea 4"));
            Assert.That(box.Text, Does.Not.Contain("linea 0"));
        });
    }

    [Test]
    public void Render_TrasRecortar_SiguePintandoElNivel()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        for (var i = 0; i < 4; i++)
            renderer.Render(box, Entry(LogLevel.Information, $"linea {i}"), maxLines: 3);

        renderer.Render(box, Entry(LogLevel.Warning, "aviso nuevo"), maxLines: 3);

        Assert.That(ColorAt(box, box.Text.LastIndexOf('[')), Is.EqualTo(LogLineColors.For(LogLevel.Warning).ToArgb()));
    }

    [Test]
    public void Render_BloqueMultilinea_IndentaLasLineasDeContinuacion()
    {
        // El resumen de la mano llega en un solo evento con varios saltos de línea. Sin
        // sangría, las líneas de continuación arrancan en la columna 0 y se confunden
        // con el log siguiente.
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();
        var entry = Entry(LogLevel.Information, "═══ [FLOP] ═══\n  Hero: [Qd Ac]\n  Pot: 19");

        renderer.Render(box, entry);

        Assert.Multiple(() =>
        {
            Assert.That(box.Lines.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(box.Lines[1], Does.StartWith(Indent));
            Assert.That(box.Lines[1], Does.Contain("Hero: [Qd Ac]"));
            Assert.That(box.Lines[2], Does.StartWith(Indent));
            Assert.That(ColorAt(box, box.Text.IndexOf("Hero", StringComparison.Ordinal)),
                Is.EqualTo(LogLineColors.Message.ToArgb()));
        });
    }

    [Test]
    public void Render_CuandoCambiaLaMano_AgregaUnSeparador()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        renderer.Render(box, Entry(LogLevel.Information, "primera", hand: 100));
        renderer.Render(box, Entry(LogLevel.Information, "segunda", hand: 101));

        Assert.Multiple(() =>
        {
            Assert.That(box.Text, Does.Contain("Mano 100"));
            Assert.That(box.Text, Does.Contain("Mano 101"));
            Assert.That(CountOf(box.Text, "Mano 101"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Render_MismaMano_NoRepiteElSeparador()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        renderer.Render(box, Entry(LogLevel.Information, "a", hand: 100));
        renderer.Render(box, Entry(LogLevel.Information, "b", hand: 100));

        Assert.That(CountOf(box.Text, "Mano 100"), Is.EqualTo(1));
    }

    [Test]
    public void Render_SinMano_NoAgregaSeparador()
    {
        using var box = NewBox();
        var renderer = new LogRichTextRenderer();

        renderer.Render(box, Entry(LogLevel.Information, "arranque"));

        Assert.That(box.Text, Does.Not.Contain("Mano "));
    }

    private static RichTextBox NewBox() => new()
    {
        Multiline = true,
        WordWrap = false,
        ScrollBars = RichTextBoxScrollBars.Both,
        Font = new Font("Consolas", 9F),
        ForeColor = LogLineColors.Message
    };

    private static int ColorAt(RichTextBox box, int index)
    {
        // Se compara el ARGB: WinForms devuelve el color con nombre (Gainsboro) cuando
        // coincide con uno conocido, y Color.Equals compara identidad, no valor.
        box.Select(index, 1);
        return box.SelectionColor.ToArgb();
    }

    private static LogEntry Entry(LogLevel level, string message, long? hand = null) => new()
    {
        TimestampUtc = DateTime.UtcNow,
        Level = level.ToString(),
        Category = "OpenScrape.App.Services.GameLoggerService",
        Message = message,
        HandNumber = hand
    };

    private static int CountOf(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}
