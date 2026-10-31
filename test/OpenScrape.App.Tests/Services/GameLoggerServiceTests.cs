using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El historial dejó de persistirse: la sesión y las manos viven sólo en memoria para
/// alimentar métricas y estimaciones. Si alguna mano no se cierra en <see cref="IMetricsCollector.EndHand"/>,
/// la pestaña Métricas pierde el bucket de esa mano.
/// </summary>
[TestFixture]
public class GameLoggerServiceTests
{
    [Test]
    public async Task SaveSessionAsync_CierraLaManoEnMetricas()
    {
        var metrics = new CountingMetricsCollector();
        var service = new GameLoggerService(NullLogger<GameLoggerService>.Instance, metrics);

        await service.StartSessionAsync("sesion-1", "Mesa 1", 0.50m);
        await service.StartNewHandAsync(1, "As", "Kd", TablePosition.Button, 100, 5);

        await service.SaveSessionAsync();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.EndHandCalls, Is.EqualTo(1), "La mano en curso debe cerrarse en métricas.");
            Assert.That(service.HasActiveHand, Is.False);
        });
    }

    [Test]
    public async Task StartNewHandAsync_CierraLaManoAnteriorEnMetricas()
    {
        var metrics = new CountingMetricsCollector();
        var service = new GameLoggerService(NullLogger<GameLoggerService>.Instance, metrics);

        await service.StartSessionAsync("sesion-1", "Mesa 1", 0.50m);
        await service.StartNewHandAsync(1, "As", "Kd", TablePosition.Button, 100, 5);
        await service.StartNewHandAsync(2, "Qs", "Qd", TablePosition.Button, 99, 5);

        Assert.Multiple(() =>
        {
            Assert.That(metrics.EndHandCalls, Is.EqualTo(1), "La mano anterior debe cerrarse al abrir la siguiente.");
            Assert.That(service.HasActiveHand, Is.True);
        });
    }

    [Test]
    public async Task SaveSessionAsync_SinManoEnCurso_NoCierraNada()
    {
        var metrics = new CountingMetricsCollector();
        var service = new GameLoggerService(NullLogger<GameLoggerService>.Instance, metrics);

        await service.StartSessionAsync("sesion-1", "Mesa 1", 0.50m);
        await service.SaveSessionAsync();

        Assert.That(metrics.EndHandCalls, Is.Zero);
    }

    [Test]
    public void GameLoggerService_NoTocaLaBaseDeDatos()
    {
        var source = File.ReadAllText(ServicePath());

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Not.Contain("IDocumentStore"));
            Assert.That(source, Does.Not.Contain("LightweightSession"));
            Assert.That(source, Does.Not.Contain("SaveChangesAsync"));
        });
    }

    private static string ServicePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName, "src", "OpenScrape.App", "Services", "GameLoggerService.cs");

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException("No se encontró GameLoggerService.cs desde el directorio de tests.");
    }

    private sealed class CountingMetricsCollector : IMetricsCollector
    {
        public int EndHandCalls { get; private set; }

        public ScopedMeasurement Measure(string category) => default;

        public void Record(string category, TimeSpan elapsed) { }

        public ScopedMeasurement MeasureSessionOnly(string category) => default;

        public void RecordSessionOnly(string category, TimeSpan elapsed) { }

        public void StartHand(string handId) { }

        public TelemetryAggregate? EndHand()
        {
            EndHandCalls++;
            return null;
        }

        public MetricsSnapshot SnapshotSession() => new(
            null,
            new Dictionary<string, CategoryStats>(),
            new Dictionary<string, CategoryStats>());

        public void ResetSession() { }
    }
}
