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
    ///
    /// Cada petición se intenta por separado: en producción un fallo suelto
    /// abortaba el bucle y el motor quedaba frío igual.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var engine = scope.ServiceProvider.GetRequiredService<IDecisionEngine>();

        var samples = SampleRequests();
        var fallos = new List<string>();

        foreach (var request in samples)
        {
            try
            {
                await engine.DecideAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Un fallo acá no puede tumbar la API ni cancelar el resto:
                // sin precalentamiento el motor funciona, sólo que lento.
                fallos.Add(request.Street.ToString());
                logger.LogWarning(ex, "No se pudo precalentar el motor con la calle {Street}", request.Street);
            }
        }

        if (fallos.Count == 0)
            logger.LogInformation("Motor precalentado: preflop y flop responden sin arranque en frio");
        else if (fallos.Count < samples.Count)
            logger.LogInformation(
                "Motor precalentado parcialmente: fallaron {Failures} de {Total}",
                string.Join(", ", fallos),
                samples.Count);
        else
            logger.LogWarning(
                "No se pudo precalentar el motor: fallaron {Failures}",
                string.Join(", ", fallos));
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
