using OpenScrape.Contracts;

namespace OpenScrape.Api.Endpoints;

/// <summary>
/// Puerta única del motor por HTTP: recibe hechos y devuelve la decisión ya derivada.
/// </summary>
public static class DecisionEndpoints
{
    public static IEndpointRouteBuilder MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1");

        group.MapPost("/decisions", async (
            DecisionRequest request,
            OpenScrape.Contracts.IDecisionEngine engine,
            CancellationToken cancellationToken) =>
        {
            var validationError = RequestValidator.Validate(request);
            if (validationError is not null)
                return Results.UnprocessableEntity(new EngineError("invalid_request", validationError));

            try
            {
                return Results.Ok(await engine.DecideAsync(request, cancellationToken));
            }
            catch (OpenScrape.DecisionMaker.Preflop.PreflopSituationNotFoundException ex)
            {
                return Results.UnprocessableEntity(new EngineError("situation_not_found", ex.Message));
            }
        })
            .RequireAuthorization()
            .RequireRateLimiting(PlanRateLimiting.DecisionsPolicy);

        return app;
    }
}
