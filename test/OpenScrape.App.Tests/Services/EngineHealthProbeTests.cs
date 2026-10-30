using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services.Http;
using OpenScrape.App.Tests.Api;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El cliente decide siempre por HTTP. Si la API del motor no está escuchando, el
/// arranque tiene que poder decirlo con la causa concreta en vez de dejar que cada
/// decisión muera en un timeout.
/// </summary>
[TestFixture]
public class EngineHealthProbeTests
{
    [Test]
    public async Task IsHealthyAsync_ApiEnProceso_DevuelveTrue()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var probe = new EngineHealthProbe(client, NullLogger<EngineHealthProbe>.Instance);

        Assert.That(await probe.IsHealthyAsync(), Is.True);
    }

    [Test]
    public async Task IsHealthyAsync_ApiCaida_DevuelveFalse()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9") };
        var probe = new EngineHealthProbe(client, NullLogger<EngineHealthProbe>.Instance);

        Assert.That(await probe.IsHealthyAsync(), Is.False);
    }

    [Test]
    public async Task IsHealthyAsync_ApiRespondeError_DevuelveFalse()
    {
        using var client = new HttpClient(new StatusHandler(HttpStatusCode.ServiceUnavailable))
        {
            BaseAddress = new Uri("http://127.0.0.1:5099")
        };
        var probe = new EngineHealthProbe(client, NullLogger<EngineHealthProbe>.Instance);

        Assert.That(await probe.IsHealthyAsync(), Is.False);
    }

    [Test]
    public void IsHealthyAsync_CancelacionDelLlamador_SePropagaComoCancelacion()
    {
        using var client = new HttpClient(new SlowHandler(TimeSpan.FromSeconds(30)))
        {
            BaseAddress = new Uri("http://127.0.0.1:5099")
        };
        var probe = new EngineHealthProbe(client, NullLogger<EngineHealthProbe>.Instance);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        Assert.That(
            async () => await probe.IsHealthyAsync(cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status));
    }

    private sealed class SlowHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage();
        }
    }
}
