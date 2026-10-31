using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// La base tiene un límite de 1 GB: sólo se persiste lo que pasa en la mano
/// (decisiones, cartas, board, bote, acción) y, de cualquier categoría, los
/// problemas. El OCR y el resto del ruido técnico sólo viajan si son aviso o error.
/// </summary>
[TestFixture]
public class MartenLogFilterTests
{
    [Test]
    public async Task Log_EventoDeJuego_SePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.GameLoggerService")
            .LogInformation("Nueva mano {HandNumber}", 12);

        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().Message, Is.EqualTo("Nueva mano 12"));
    }

    [Test]
    public async Task Log_EventoDeSistemaInformativo_NoSePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.ScreenReaderService")
            .LogInformation("[STACK] OCR lecturas: P1=1200");

        await provider.FlushAsync();

        Assert.That(writer.Entries, Is.Empty);
    }

    [Test]
    public async Task Log_AdvertenciaDeSistema_SePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.ScreenReaderService")
            .LogWarning("[BET] Input OCR inválido");

        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().Level, Is.EqualTo("Warning"));
    }

    [Test]
    public async Task Log_ErrorDeSistema_SePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.CaptureService")
            .LogError("No se pudo capturar la ventana");

        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().Level, Is.EqualTo("Error"));
    }

    [Test]
    public async Task Log_DebugDeJuego_SePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.HandProcessor")
            .LogDebug("HandProcessor: estado Preflop");

        await provider.FlushAsync();

        Assert.That(writer.Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Log_TraceDeJuego_NoSePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.HandProcessor")
            .LogTrace("detalle fino");

        await provider.FlushAsync();

        Assert.That(writer.Entries, Is.Empty);
    }

    [Test]
    public async Task Log_CategoriasDeJuego_SeTomanDeLaConfiguracion()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(
            writer,
            options => options.HandCategories = ["OpenScrape.App.Services.ScreenReaderService"]);

        var logger = provider.CreateLogger("OpenScrape.App.Services.ScreenReaderService");
        logger.LogInformation("ahora sí viaja");

        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().Message, Is.EqualTo("ahora sí viaja"));
    }

    [Test]
    public async Task Log_CategoriaHijaDeUnaDeJuego_SePersiste()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Services.GameLoggerService.Hand")
            .LogInformation("detalle de la mano");

        await provider.FlushAsync();

        Assert.That(writer.Entries, Has.Count.EqualTo(1));
    }

    private static MartenLoggerProvider CreateProvider(
        RecordingLogEntryWriter writer,
        Action<MartenLoggerOptions>? configure = null)
    {
        var options = new MartenLoggerOptions { FlushIntervalMs = 10 };

        configure?.Invoke(options);

        return new MartenLoggerProvider(() => writer, Options.Create(options));
    }
}
