using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.Api;
using OpenScrape.Contracts;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// La primera decisión de una sesión pagaba el arranque en frío del motor (JIT,
/// inicialización y el primer Monte Carlo) y se pasaba del timeout de 1,5 s del
/// cliente: la mano se perdía con "El motor no respondió dentro del tiempo de
/// espera". El precalentamiento mueve ese costo al arranque de la API.
/// </summary>
[TestFixture]
public class EngineWarmupServiceTests
{
    [Test]
    public async Task RunAsync_EjercitaElMotorConPreflopYFlop()
    {
        var engine = new CountingEngine();
        var service = CreateService(engine);

        await service.RunAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(engine.Requests, Has.Count.EqualTo(2),
                "El precalentamiento debe tocar preflop y flop, no sólo un camino.");
            Assert.That(engine.Requests.Select(request => request.Street),
                Is.EquivalentTo(new[] { DecisionStreet.Preflop, DecisionStreet.Flop }));
        });
    }

    [Test]
    public async Task RunAsync_CuandoUnEjemploFalla_CalientaLosDemas()
    {
        // En produccion el precalentamiento moria en la primera peticion y no
        // calentaba nada: un fallo suelto no puede cancelar el resto.
        var engine = new PartialFailingEngine(DecisionStreet.Preflop);
        var service = CreateService(engine);

        await service.RunAsync(CancellationToken.None);

        Assert.That(engine.AskedStreets, Has.Count.EqualTo(2),
            "Debe intentar todas las calles aunque una falle.");
        Assert.That(engine.AskedStreets, Does.Contain(DecisionStreet.Flop));
    }

    [Test]
    public async Task RunAsync_CuandoElMotorFalla_NoPropagaLaExcepcion()
    {
        var service = CreateService(new ThrowingEngine());

        Assert.DoesNotThrowAsync(async () => await service.RunAsync(CancellationToken.None));
    }

    [Test]
    public async Task StartAsync_ConElPrecalentamientoDeshabilitado_NoLlamaAlMotor()
    {
        var engine = new CountingEngine();
        var service = CreateService(engine, enabled: false);

        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        Assert.That(engine.Requests, Is.Empty);
    }

    [Test]
    public async Task LasPeticionesDePrecalentamiento_ElMotorRealLasAcepta()
    {
        // El doble no valida el contenido: si la petición no sirve, el
        // precalentamiento sólo deja un warning y no calienta nada.
        using var factory = new ApiFactory();
        using var scope = factory.Services.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDecisionEngine>();

        foreach (var request in EngineWarmupService.SampleRequests())
        {
            var result = await engine.DecideAsync(request);

            Assert.That(result.Action, Is.Not.Empty,
                $"El motor no devolvió acción para {request.Street}.");
        }
    }

    private static EngineWarmupService CreateService(IDecisionEngine engine, bool enabled = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(engine);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Engine:WarmupEnabled"] = enabled.ToString()
            })
            .Build();

        return new EngineWarmupService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            configuration,
            NullLogger<EngineWarmupService>.Instance);
    }

    private sealed class CountingEngine : IDecisionEngine
    {
        public List<DecisionRequest> Requests { get; } = [];

        public Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            return Task.FromResult(new DecisionResult { Action = "Check" });
        }
    }

    private sealed class PartialFailingEngine : IDecisionEngine
    {
        private readonly DecisionStreet _failing;

        public PartialFailingEngine(DecisionStreet failing) => _failing = failing;

        public List<DecisionStreet> AskedStreets { get; } = [];

        public Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
        {
            AskedStreets.Add(request.Street);

            if (request.Street == _failing)
                throw new InvalidOperationException($"motor roto en {request.Street}");

            return Task.FromResult(new DecisionResult { Action = "Check" });
        }
    }

    private sealed class ThrowingEngine : IDecisionEngine
    {
        public Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("motor roto");
    }
}
