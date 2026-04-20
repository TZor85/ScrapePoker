using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenScrape.App.Configuration;
using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del ciclo de vida del GameLoopCoordinator (esqueleto Fase 3):
/// arranque, parada, cancelación, idempotencia, dispose.
/// El TickAsync en esta fase emite un GameLoopResult.Empty=true; la lógica
/// real se verifica en la Fase 4 con tests de integración adicionales.
/// </summary>
[TestFixture]
public class GameLoopCoordinatorLifecycleTests
{
    private static GameLoopCoordinator Create(int intervalMs = 20, int stopTimeoutMs = 500)
    {
        var options = Options.Create(new GameLoopOptions
        {
            CaptureIntervalMs = intervalMs,
            StopTimeoutMs = stopTimeoutMs,
        });
        return new GameLoopCoordinator(NullLogger<GameLoopCoordinator>.Instance, options);
    }

    [Test]
    public async Task StartAsync_ArrancaLoopYEmiteResultados()
    {
        await using var coord = Create(intervalMs: 10);
        int received = 0;
        coord.ResultReady += (_, _) => Interlocked.Increment(ref received);

        using var cts = new CancellationTokenSource();
        await coord.StartAsync(cts.Token);
        Assert.That(coord.IsRunning, Is.True);

        // Esperar varias iteraciones.
        await Task.Delay(100);

        await coord.StopAsync();
        Assert.That(coord.IsRunning, Is.False);
        Assert.That(received, Is.GreaterThan(0),
            "El loop debería haber emitido al menos un GameLoopResult");
    }

    [Test]
    public async Task StartAsync_LlamadasDuplicadas_SonIdempotentes()
    {
        await using var coord = Create(intervalMs: 10);
        using var cts = new CancellationTokenSource();

        await coord.StartAsync(cts.Token);
        await coord.StartAsync(cts.Token); // segunda llamada no debe lanzar ni duplicar loop
        Assert.That(coord.IsRunning, Is.True);

        await coord.StopAsync();
    }

    [Test]
    public async Task StopAsync_LlamadasDuplicadas_SonIdempotentes()
    {
        await using var coord = Create(intervalMs: 10);
        using var cts = new CancellationTokenSource();

        await coord.StartAsync(cts.Token);
        await coord.StopAsync();
        await coord.StopAsync(); // no debe lanzar
        Assert.That(coord.IsRunning, Is.False);
    }

    [Test]
    public async Task CancelacionToken_ParaLoopSinError()
    {
        await using var coord = Create(intervalMs: 10);
        int received = 0;
        coord.ResultReady += (_, r) =>
        {
            Assert.That(r.Error, Is.Null, "No debería haber error en cancelación limpia");
            Interlocked.Increment(ref received);
        };

        using var cts = new CancellationTokenSource();
        await coord.StartAsync(cts.Token);
        await Task.Delay(50);

        cts.Cancel();

        // Dar tiempo para que el loop observe la cancelación.
        await Task.Delay(100);
        await coord.StopAsync();

        Assert.That(coord.IsRunning, Is.False);
        Assert.That(received, Is.GreaterThan(0));
    }

    [Test]
    public async Task StopAsync_SinStart_NoLanza()
    {
        await using var coord = Create();
        await coord.StopAsync(); // no debe lanzar
        Assert.That(coord.IsRunning, Is.False);
    }

    [Test]
    public async Task DisposeAsync_DetieneLoopAutomaticamente()
    {
        var coord = Create(intervalMs: 10);
        using var cts = new CancellationTokenSource();
        await coord.StartAsync(cts.Token);
        Assert.That(coord.IsRunning, Is.True);

        await coord.DisposeAsync();
        Assert.That(coord.IsRunning, Is.False);
    }

    [Test]
    public async Task StartAsync_TrasDispose_Lanza()
    {
        var coord = Create();
        await coord.DisposeAsync();

        using var cts = new CancellationTokenSource();
        Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await coord.StartAsync(cts.Token));
    }

    [Test]
    public async Task SuscriptorQueLanza_NoDerribaElLoop()
    {
        await using var coord = Create(intervalMs: 10);
        int survivedEvents = 0;

        coord.ResultReady += (_, _) => throw new InvalidOperationException("boom");
        coord.ResultReady += (_, _) => Interlocked.Increment(ref survivedEvents);

        using var cts = new CancellationTokenSource();
        await coord.StartAsync(cts.Token);
        await Task.Delay(80);
        await coord.StopAsync();

        Assert.That(survivedEvents, Is.GreaterThan(0),
            "La excepción del primer suscriptor no debe impedir que el segundo reciba eventos posteriores");
    }

    [Test]
    public async Task ResultadoEmitidoTipicamente_EsEmpty()
    {
        await using var coord = Create(intervalMs: 10);
        GameLoopResult? last = null;
        coord.ResultReady += (_, r) => last = r;

        using var cts = new CancellationTokenSource();
        await coord.StartAsync(cts.Token);
        await Task.Delay(50);
        await coord.StopAsync();

        Assert.That(last, Is.Not.Null);
        Assert.That(last!.Empty, Is.True);
        Assert.That(last.Error, Is.Null);
    }
}
