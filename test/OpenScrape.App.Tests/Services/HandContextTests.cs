using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Windows.Forms;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Logging;
using OpenScrape.Domain.Entities;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El <c>BeginScope</c> de la mano vive en el contexto de <c>StartNewHandAsync</c> y no
/// alcanza a quien loguea después, así que la mano se identifica con un estado compartido.
/// Sin esto los eventos de HandProcessor, TableLayoutService y FrmMain quedan sin mano.
/// </summary>
[TestFixture]
public class HandContextTests
{
    [Test]
    public async Task StartNewHandAsync_MarcaLaManoEnCurso()
    {
        var context = new HandContext();
        var service = new GameLoggerService(NullLogger<GameLoggerService>.Instance, new NullMetrics(), context);

        await service.StartSessionAsync("sesion-1", "Mesa 1", 0.50m);
        await service.StartNewHandAsync(42, "As", "Kd", TablePosition.Button, 100, 5);

        Assert.That(context.Current, Is.EqualTo(42));
    }

    [Test]
    public async Task SaveSessionAsync_LimpiaLaManoEnCurso()
    {
        var context = new HandContext();
        var service = new GameLoggerService(NullLogger<GameLoggerService>.Instance, new NullMetrics(), context);

        await service.StartSessionAsync("sesion-1", "Mesa 1", 0.50m);
        await service.StartNewHandAsync(42, "As", "Kd", TablePosition.Button, 100, 5);
        await service.SaveSessionAsync();

        Assert.That(context.Current, Is.Null);
    }

    [Test]
    public async Task MartenLogger_EstampaElHandNumberVigente()
    {
        var writer = new RecordingWriter();
        var context = new HandContext { Current = 77 };
        using var provider = CreateProvider(writer, context);
        var logger = provider.CreateLogger("OpenScrape.App.Services.HandProcessor");

        logger.LogInformation("Decisión de flop");
        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().HandNumber, Is.EqualTo(77));
    }

    [Test]
    public async Task MartenLogger_SinManoEnCurso_NoEstampaHandNumber()
    {
        var writer = new RecordingWriter();
        using var provider = CreateProvider(writer, new HandContext());
        var logger = provider.CreateLogger("OpenScrape.App.Services.HandProcessor");

        logger.LogInformation("Lectura de mesa");
        await provider.FlushAsync();

        Assert.That(writer.Entries.Single().HandNumber, Is.Null);
    }

    [Test]
    public void TextBoxLogger_MuestraLaManoEnCurso()
    {
        var options = Options.Create(new TextBoxLoggerOptions { MinimumLevel = LogLevel.Trace });
        using var provider = new TextBoxLoggerProvider(options);
        using var textBox = new TextBox();
        provider.SetTextBoxTarget(textBox);
        provider.SetHandContext(new HandContext { Current = 77 });

        provider.CreateLogger("OpenScrape.App.Services.HandProcessor").LogInformation("Decisión de flop");

        Assert.That(textBox.Text, Does.Contain("Mano=77"));
    }

    private static MartenLoggerProvider CreateProvider(RecordingWriter writer, HandContext context)
        => new(
            () => writer,
            Options.Create(new MartenLoggerOptions { FlushIntervalMs = 10 }),
            null,
            context);

    private sealed class RecordingWriter : ILogEntryWriter
    {
        public List<LogEntry> Entries { get; } = new();

        public Task WriteAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken)
        {
            Entries.AddRange(entries);
            return Task.CompletedTask;
        }

        public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken)
            => Task.FromResult(0);

        public Task<int> EnforceBudgetAsync(CancellationToken cancellationToken)
            => Task.FromResult(0);
    }

    private sealed class NullMetrics : IMetricsCollector
    {
        public ScopedMeasurement Measure(string category) => default;

        public void Record(string category, TimeSpan elapsed) { }

        public ScopedMeasurement MeasureSessionOnly(string category) => default;

        public void RecordSessionOnly(string category, TimeSpan elapsed) { }

        public void StartHand(string handId) { }

        public TelemetryAggregate? EndHand() => null;

        public MetricsSnapshot SnapshotSession() => new(
            null,
            new Dictionary<string, CategoryStats>(),
            new Dictionary<string, CategoryStats>());

        public void ResetSession() { }
    }
}
