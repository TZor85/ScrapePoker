using Microsoft.Extensions.Logging;

using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El histórico guarda el nivel como texto. Al consultar una mano con un nivel elegido
/// se muestran sólo los eventos de ese nivel, igual que el stream en vivo; sin nivel
/// elegido ("Todos") no se descarta nada.
/// </summary>
[TestFixture]
public class LogEntryFilterTests
{
    [Test]
    public void Filtra_SoloElNivelPedido()
    {
        var entries = new[]
        {
            Entry(LogLevel.Information, "info"),
            Entry(LogLevel.Warning, "warning"),
            Entry(LogLevel.Error, "error")
        };

        var filtradas = LogEntryFilter.ByLevel(entries, LogLevel.Warning);

        Assert.That(filtradas.Select(entry => entry.Message), Is.EqualTo(new[] { "warning" }));
    }

    [Test]
    public void SinNivel_NoDescartaNada()
    {
        var entries = new[]
        {
            Entry(LogLevel.Trace, "trace"),
            Entry(LogLevel.Debug, "debug"),
            Entry(LogLevel.Information, "info")
        };

        var filtradas = LogEntryFilter.ByLevel(entries, level: null);

        Assert.That(filtradas, Has.Count.EqualTo(3));
    }

    [Test]
    public void NivelDesconocido_SeTrataComoTrace()
    {
        var entry = Entry(LogLevel.Information, "raro");
        entry.Level = "sin-nivel";

        Assert.Multiple(() =>
        {
            Assert.That(LogEntryFilter.ByLevel(new[] { entry }, LogLevel.Trace), Has.Count.EqualTo(1));
            Assert.That(LogEntryFilter.ByLevel(new[] { entry }, LogLevel.Information), Is.Empty);
        });
    }

    private static LogEntry Entry(LogLevel level, string message) => new()
    {
        TimestampUtc = DateTime.UtcNow,
        Level = level.ToString(),
        Category = "OpenScrape.App.Services.GameLoggerService",
        Message = message
    };
}
