using OpenScrape.Api.Licensing;
using OpenScrape.Contracts;
using OpenScrape.Contracts.Licensing;

namespace OpenScrape.Api.Endpoints;

/// <summary>Activación de licencias: anónima, con rate limit por IP.</summary>
public static class LicenseEndpoints
{
    public static IEndpointRouteBuilder MapLicenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/license");

        group.MapPost("/activate", ActivateAsync)
            .AllowAnonymous()
            .RequireRateLimiting(PlanRateLimiting.ActivationPolicy);

        return app;
    }

    private static async Task<IResult> ActivateAsync(
        LicenseActivateRequest request,
        ILicenseStore store,
        ILicenseKeyHasher hasher,
        ILicenseTokenService tokens,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.LicenseKey))
            return Results.BadRequest(new EngineError("invalid_request", "Falta la key de licencia."));

        var license = await store.FindByKeyHashAsync(hasher.Hash(request.LicenseKey), cancellationToken);

        if (license is null || license.Revoked || license.ExpiresAt <= DateTimeOffset.UtcNow)
            return Results.Json(
                new EngineError("license_invalid", "La licencia no es válida."),
                statusCode: StatusCodes.Status401Unauthorized);

        var (token, expiresAt) = tokens.Issue(license);
        loggerFactory.CreateLogger("Licensing").LogInformation(
            "Licencia {LicenseId} activada con plan {Plan}.", license.Id, license.Plan);

        return Results.Ok(new LicenseActivateResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Plan = license.Plan
        });
    }
}
