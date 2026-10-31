using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Logging;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// En modo test el cliente repite capturas grabadas: esos eventos no aportan
/// diagnóstico y la base tiene un límite de 1 GB. Mientras el modo test está
/// activo no se persiste nada, ni siquiera errores; al desactivarlo vuelve.
/// </summary>
[TestFixture]
public class MartenLogTestModeTests
{
    [Test]
    public async Task TestMode_Activo_NoPersisteNiLosErrores()
    {
        var writer = new RecordingLogEntryWriter();
        var mode = new AppModeState { IsTestMode = true };
        await using var provider = CreateProvider(writer, mode);

        provider.CreateLogger("OpenScrape.App.Services.CaptureService")
            .LogError("No se pudo capturar la ventana");

        await provider.FlushAsync();

        Assert.That(writer.Entries, Is.Empty);
    }

    [Test]
    public void TestMode_Activo_ElLoggerSeConsideraDeshabilitado()
    {
        var writer = new RecordingLogEntryWriter();
        var mode = new AppModeState { IsTestMode = true };
        var provider = CreateProvider(writer, mode);

        var logger = provider.CreateLogger("OpenScrape.App.Services.HandProcessor");

        Assert.That(logger.IsEnabled(LogLevel.Error), Is.False,
            "Si el logger se considera habilitado, el evento se formatea y se descarta después: " +
            "el filtro tiene que cortar antes.");
    }

    [Test]
    public async Task TestMode_SeDesactiva_VuelveAPersistir()
    {
        var writer = new RecordingLogEntryWriter();
        var mode = new AppModeState { IsTestMode = true };
        await using var provider = CreateProvider(writer, mode);

        var logger = provider.CreateLogger("OpenScrape.App.Services.HandProcessor");
        logger.LogInformation("en test no viaja");
        await provider.FlushAsync();

        mode.IsTestMode = false;
        logger.LogInformation("fuera de test sí viaja");
        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().Message, Is.EqualTo("fuera de test sí viaja"));
    }

    private static MartenLoggerProvider CreateProvider(
        RecordingLogEntryWriter writer,
        AppModeState mode)
    {
        var options = new MartenLoggerOptions { FlushIntervalMs = 10 };

        return new MartenLoggerProvider(() => writer, Options.Create(options), mode);
    }
}
