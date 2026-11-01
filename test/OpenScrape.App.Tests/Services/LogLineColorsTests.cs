using System.Drawing;

using Microsoft.Extensions.Logging;

using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// Los colores del panel tienen que ser distinguibles entre sí pero apagados: el
/// objetivo es ubicar el nivel de un vistazo, no convertir el log en un semáforo.
/// </summary>
[TestFixture]
public class LogLineColorsTests
{
    [Test]
    public void CadaNivelVisibleUsaUnColorPropio()
    {
        var errores = LogLineColors.For(LogLevel.Error);
        var avisos = LogLineColors.For(LogLevel.Warning);
        var trazas = LogLineColors.For(LogLevel.Trace);
        var depuracion = LogLineColors.For(LogLevel.Debug);

        Assert.Multiple(() =>
        {
            Assert.That(errores, Is.EqualTo(Color.FromArgb(178, 58, 58)));
            Assert.That(avisos, Is.EqualTo(Color.FromArgb(138, 97, 0)));
            Assert.That(trazas, Is.EqualTo(Color.FromArgb(47, 107, 44)));
            Assert.That(depuracion, Is.EqualTo(Color.FromArgb(31, 95, 168)));
        });
    }

    [Test]
    public void CriticalComparteElRojoDelError()
        => Assert.That(LogLineColors.For(LogLevel.Critical), Is.EqualTo(LogLineColors.For(LogLevel.Error)));

    [Test]
    public void ElMensajeUsaElColorNeutroDelPanel()
        => Assert.That(LogLineColors.Message, Is.EqualTo(Color.FromArgb(26, 26, 26)));
}
