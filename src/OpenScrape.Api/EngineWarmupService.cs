using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.Api;

/// <summary>
/// Precalienta el motor al arrancar la API.
///
/// La primera decisión de una sesión paga el arranque en frío (JIT, inicialización
/// de rangos y el primer Monte Carlo) y superaba el timeout de 1,5 s del cliente:
/// la mano se perdía con "El motor no respondió dentro del tiempo de espera".
/// Corriendo un par de decisiones acá, la primera petición real ya encuentra todo
/// caliente y el timeout del cliente se mantiene honesto.
/// </summary>
public sealed class EngineWarmupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<EngineWarmupService> logger) : IHostedService
{
    /// <summary>Se ejecuta en segundo plano: no retrasa el arranque del servicio.</summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("Engine:WarmupEnabled", true))
        {
            logger.LogInformation("Precalentamiento del motor deshabilitado por configuracion");

            return Task.CompletedTask;
        }

        _ = Task.Run(() => RunAsync(CancellationToken.None), CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Ejercita preflop y flop: son los dos caminos que el cliente puede pedir
    /// primero y los que tienen costos de inicialización distintos.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IDecisionEngine>();

            foreach (var request in SampleRequests())
                await engine.DecideAsync(request, cancellationToken);

            logger.LogInformation("Motor precalentado: preflop y flop responden sin arranque en frio");
        }
        catch (Exception ex)
        {
            // Un fallo acá no puede tumbar la API: sin precalentamiento el motor
            // sigue funcionando, sólo que la primera decisión vuelve a ser lenta.
            logger.LogWarning(ex, "No se pudo precalentar el motor");
        }
    }

    public static IReadOnlyList<DecisionRequest> SampleRequests() =>
    [
        new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef(14, 4),
            HeroCard2 = new CardRef(13, 4),
            PotSize = 1.5m,
            BetToCall = 1m,
            HeroStack = 100m,
            VillainStack = 100m,
            IsInPosition = true,
            NumOpponents = 1,
            HeroPosition = TablePosition.Button,
            VillainPosition = TablePosition.BigBlind,
            HandSituation = HandSituation.OpenRaise
        },
        new DecisionRequest
        {
            Street = DecisionStreet.Flop,
            HeroCard1 = new CardRef(14, 4),
            HeroCard2 = new CardRef(13, 4),
            Board = [new CardRef(13, 3), new CardRef(7, 1), new CardRef(2, 2)],
            PotSize = 6m,
            BetToCall = 3m,
            HeroStack = 97m,
            VillainStack = 97m,
            IsInPosition = true,
            NumOpponents = 1,
            HeroPosition = TablePosition.Button,
            VillainPosition = TablePosition.BigBlind,
            HandSituation = HandSituation.OpenRaise
        }
    ];
}
