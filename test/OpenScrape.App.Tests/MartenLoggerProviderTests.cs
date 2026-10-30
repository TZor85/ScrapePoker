using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenScrape.App.Services.Logging;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests;

/// <summary>
/// El proveedor de Marten debe persistir los eventos sin bloquear al emisor y
/// sin romper la aplicación si la base falla.
/// </summary>
[TestFixture]
public class MartenLoggerProviderTests
{
    [Test]
    public async Task FlushAsync_PersisteTodosLosEventosEncolados()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);
        var logger = provider.CreateLogger("OpenScrape.App.Tests");

        logger.LogInformation("primero");
        logger.LogWarning("segundo");
        logger.LogError(new InvalidOperationException("boom"), "tercero");

        await provider.FlushAsync();

        Assert.That(writer.Entries, Has.Count.EqualTo(3));
        Assert.That(
            writer.Entries.Select(entry => entry.Level),
            Is.EquivalentTo(new[] { "Information", "Warning", "Error" }));
    }

    [Test]
    public async Task Log_AlmacenaTraceSinNivelMinimo()
    {
        // El usuario pidió guardar todo: el proveedor no filtra por nivel.
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Tests").LogTrace("detalle fino");
        await provider.FlushAsync();

        Assert.That(writer.Entries, Has.Count.EqualTo(1));
        Assert.That(writer.Entries[0].Level, Is.EqualTo("Trace"));
    }

    [Test]
    public async Task Log_CapturaScopesArgumentosYDatosDeLaExcepcion()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer);

        var scopeProvider = new LoggerExternalScopeProvider();
        provider.SetScopeProvider(scopeProvider);
        var logger = provider.CreateLogger("OpenScrape.App.Tests");

        // La excepción se lanza para que tenga stack trace real.
        Exception captured;
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (Exception ex)
        {
            captured = ex;
        }

        using (scopeProvider.Push(new Dictionary<string, object>
        {
            ["SessionId"] = "ses-1",
            ["HandNumber"] = 7
        }))
        {
            logger.LogError(captured, "Fallo en {Street}", "Flop");
        }

        await provider.FlushAsync();

        var entry = writer.Entries.Single();
        Assert.That(entry.Scopes["SessionId"], Is.EqualTo("ses-1"));
        Assert.That(entry.Scopes["HandNumber"], Is.EqualTo("7"));
        Assert.That(entry.Scopes["Street"], Is.EqualTo("Flop"));
        Assert.That(entry.Category, Is.EqualTo("OpenScrape.App.Tests"));
        Assert.That(entry.ExceptionType, Is.EqualTo("System.InvalidOperationException"));
        Assert.That(entry.ExceptionMessage, Is.EqualTo("boom"));
        Assert.That(entry.ExceptionStackTrace, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task FlushAsync_NoPropagaErroresDelWriter()
    {
        var writer = new RecordingLogEntryWriter { ThrowOnWrite = true };
        await using var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Tests").LogInformation("se pierde");
        await provider.FlushAsync();

        Assert.That(writer.Entries, Is.Empty);
    }

    [Test]
    public async Task DisposeAsync_NoPropagaErroresDelWriter()
    {
        var writer = new RecordingLogEntryWriter { ThrowOnWrite = true };
        var provider = CreateProvider(writer);

        provider.CreateLogger("OpenScrape.App.Tests").LogInformation("se pierde");

        Assert.DoesNotThrowAsync(async () => await provider.DisposeAsync());
    }

    [Test]
    public async Task ApplyRetentionNowAsync_EliminaConCutoffDeSieteDias()
    {
        var writer = new RecordingLogEntryWriter();
        // Sin worker: el test ejerce la retención de forma directa y determinista.
        await using var provider = CreateProvider(writer, options =>
        {
            options.Enabled = false;
            options.RetentionDays = 7;
        });

        await provider.ApplyRetentionNowAsync(CancellationToken.None);

        var cutoff = writer.Cutoffs.Single();
        Assert.That(cutoff, Is.EqualTo(DateTime.UtcNow.AddDays(-7)).Within(TimeSpan.FromMinutes(1)));
    }

    [Test]
    public async Task Log_Deshabilitado_NoPersisteNada()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer, options => options.Enabled = false);

        provider.CreateLogger("OpenScrape.App.Tests").LogInformation("no viaja");
        await provider.FlushAsync();

        Assert.That(writer.Entries, Is.Empty);
    }

    [Test]
    public async Task Log_RecortaMensajesLargos()
    {
        var writer = new RecordingLogEntryWriter();
        await using var provider = CreateProvider(writer, options => options.MaxMessageLength = 10);

        provider.CreateLogger("OpenScrape.App.Tests").LogInformation(new string('x', 50));
        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().Message, Is.EqualTo("xxxxxxxxxx…"));
    }

    private static MartenLoggerProvider CreateProvider(
        RecordingLogEntryWriter writer,
        Action<MartenLoggerOptions>? configure = null)
    {
        var options = new MartenLoggerOptions
        {
            // Ventana corta para que los tests no esperen un segundo.
            FlushIntervalMs = 10
        };

        configure?.Invoke(options);

        return new MartenLoggerProvider(() => writer, Options.Create(options));
    }
}

/// <summary>Writer en memoria para verificar qué recibió el proveedor.</summary>
internal sealed class RecordingLogEntryWriter : ILogEntryWriter
{
    public List<LogEntry> Entries { get; } = new();

    public List<DateTime> Cutoffs { get; } = new();

    public bool ThrowOnWrite { get; set; }

    public Task WriteAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken = default)
    {
        if (ThrowOnWrite)
            throw new InvalidOperationException("base caída");

        Entries.AddRange(entries);
        return Task.CompletedTask;
    }

    public Task DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        Cutoffs.Add(cutoffUtc);
        return Task.CompletedTask;
    }
}
