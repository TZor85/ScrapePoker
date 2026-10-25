using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Services.Http;
using OpenScrape.App.Services.License;
using OpenScrape.App.Tests.Api;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El cliente HTTP sólo conoce el contrato. Cuando el motor no está disponible
/// debe cortar, no inventar una decisión ni caer a un cálculo local.
/// </summary>
[TestFixture]
public class HttpDecisionEngineTests
{
    [Test]
    public async Task DecideAsync_ApiEnProceso_DevuelveLaDecisionDelServidor()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateLicensedClient();
        var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);

        var result = await engine.DecideAsync(PreflopRequest());

        Assert.That(result.Action, Is.EqualTo("Raise"));
    }

    [Test]
    public void DecideAsync_ApiCaida_LanzaEngineUnavailable()
    {
        using var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9") };
        var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);

        Assert.ThrowsAsync<EngineUnavailableException>(
            async () => await engine.DecideAsync(PreflopRequest()));
    }

    [Test]
    public async Task DecideAsync_MotorLento_CortaPorTimeoutYNoDevuelveDecision()
    {
        using var client = new HttpClient(new SlowHandler(TimeSpan.FromSeconds(30)))
        {
            BaseAddress = new Uri("http://127.0.0.1:5099")
        };
        var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);

        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        Assert.ThrowsAsync<EngineUnavailableException>(
            async () => await engine.DecideAsync(PreflopRequest()));
        elapsed.Stop();

        Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
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

    [Test]
    public void DecideAsync_CancelacionDelLlamador_SePropagaComoCancelacion()
    {
        using var client = new HttpClient(new SlowHandler(TimeSpan.FromSeconds(30)))
        {
            BaseAddress = new Uri("http://127.0.0.1:5099")
        };
        var engine = new HttpDecisionEngine(client, NullLogger<HttpDecisionEngine>.Instance);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        Assert.That(
            async () => await engine.DecideAsync(PreflopRequest(), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private static DecisionRequest PreflopRequest() => new()
    {
        Street = DecisionStreet.Preflop,
        HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
        HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
        PotSize = 3m,
        BetToCall = 3m,
        HeroStack = 100m,
        VillainStack = 100m,
        HeroPosition = TablePosition.Button,
        VillainPosition = TablePosition.BigBlind,
        IsInPosition = true,
        HandSituation = HandSituation.None
    };

    [Test]
    public void DecideAsync_SinLicencia_LanzaLicenseRequiredYSinRequest()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "{}");
        var path = TempLicensePath();
        var store = new LicenseTokenStore(path, "secreto");
        var engine = new HttpDecisionEngine(
            new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5099") },
            NullLogger<HttpDecisionEngine>.Instance,
            store);

        try
        {
            Assert.ThrowsAsync<LicenseRequiredException>(
                async () => await engine.DecideAsync(PreflopRequest()));

            Assert.That(handler.Count, Is.Zero, "Sin licencia no se debe llamar al motor.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DecideAsync_TokenVencido_NoLlamaAlMotorYLimpiaElStore()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, "{}");
        var path = TempLicensePath();
        var store = new LicenseTokenStore(path, "secreto");
        store.Save(new StoredLicense("k", "t", DateTimeOffset.UtcNow.AddMinutes(-1), "pro"));
        var engine = new HttpDecisionEngine(
            new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5099") },
            NullLogger<HttpDecisionEngine>.Instance,
            store);

        try
        {
            Assert.ThrowsAsync<LicenseRequiredException>(
                async () => await engine.DecideAsync(PreflopRequest()));

            Assert.That(handler.Count, Is.Zero);
            Assert.That(store.Load(), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void DecideAsync_LicenciaRechazada_LimpiaElStore()
    {
        var handler = new CapturingHandler(
            HttpStatusCode.Unauthorized,
            """{"code":"unauthorized","message":"Token vencido"}""");
        var path = TempLicensePath();
        var store = new LicenseTokenStore(path, "secreto");
        store.Save(new StoredLicense("k", "t", DateTimeOffset.UtcNow.AddHours(1), "pro"));
        var engine = new HttpDecisionEngine(
            new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:5099") },
            NullLogger<HttpDecisionEngine>.Instance,
            store);

        try
        {
            Assert.ThrowsAsync<LicenseRequiredException>(
                async () => await engine.DecideAsync(PreflopRequest()));

            Assert.That(handler.Count, Is.EqualTo(1));
            Assert.That(store.Load(), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string TempLicensePath() =>
        Path.Combine(Path.GetTempPath(), $"lic-http-{Guid.NewGuid():N}.dat");

    private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public int Count { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Count++;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
