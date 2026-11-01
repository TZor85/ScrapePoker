using Microsoft.Extensions.Logging;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El panel en vivo y el histórico tienen que verse igual: mismo prefijo, mismo
/// token de nivel, misma etiqueta de mano. Si cada uno arma su línea, divergen.
/// </summary>
[TestFixture]
public class LogLineFormatterTests
{
    [Test]
    public void Format_EntradaPersistida_UsaElMismoFormatoQueElPanelVivo()
    {
        var entry = new LogEntry
        {
            TimestampUtc = new DateTime(2026, 10, 31, 15, 4, 5, DateTimeKind.Utc),
            Level = "Warning",
            Category = "OpenScrape.App.Services.GameLoggerService",
            Message = "sin sesión activa",
            HandNumber = 42
        };

        var line = LogLineFormatter.Format(entry);

        Assert.Multiple(() =>
        {
            Assert.That(line, Does.StartWith("● ["));
            Assert.That(line, Does.Contain(" WRN GameLoggerService]"));
            Assert.That(line, Does.Contain("{Mano=42}"));
            Assert.That(line, Does.EndWith(" sin sesión activa"));
        });
    }

    [Test]
    public void Format_EntradaInformacion_NoLlevaMarcaPeroMantieneLaAlineacion()
    {
        var entry = new LogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Information",
            Category = "OpenScrape.App.Services.GameLoggerService",
            Message = "arrancó la sesión"
        };

        var line = LogLineFormatter.Format(entry);

        Assert.That(line, Does.StartWith("  ["));
    }

    [Test]
    public void Split_SeparaElPrefijoDelMensaje()
    {
        var entry = new LogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Error",
            Category = "OpenScrape.App.Services.CaptureService",
            Message = "no hay ventana",
            HandNumber = 7
        };

        var (prefix, message) = LogLineFormatter.Split(entry);

        Assert.Multiple(() =>
        {
            Assert.That(prefix, Does.StartWith("● ["));
            Assert.That(prefix, Does.EndWith(" {Mano=7}"));
            Assert.That(message, Is.EqualTo("no hay ventana"));
        });
    }

    [Test]
    public void Format_EntradaSinMano_NoMuestraLaEtiquetaDeMano()
    {
        var entry = new LogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Error",
            Category = "OpenScrape.App.Services.CaptureService",
            Message = "falló la captura"
        };

        var line = LogLineFormatter.Format(entry);

        Assert.That(line, Does.Not.Contain("Mano="));
        Assert.That(line, Does.Contain("ERR CaptureService]"));
    }

    [Test]
    public void Format_EntradaConExcepcion_AgregaElTipoYElMensaje()
    {
        var entry = new LogEntry
        {
            TimestampUtc = DateTime.UtcNow,
            Level = "Error",
            Category = "OpenScrape.App.Services.HandProcessor",
            Message = "no pudo leer"
        };

        var line = LogLineFormatter.Format(entry, new InvalidOperationException("boom"));

        Assert.That(line, Does.Contain("| InvalidOperationException: boom"));
    }
}
