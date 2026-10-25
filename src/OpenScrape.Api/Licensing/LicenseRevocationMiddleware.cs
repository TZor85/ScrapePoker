using Microsoft.Extensions.Options;
using OpenScrape.Contracts;

namespace OpenScrape.Api.Licensing;

/// <summary>
/// Corte inmediato opcional: con <see cref="LicenseOptions.CheckRevocationPerRequest"/>
/// prendido, una licencia revocada deja de entrar sin esperar a que venza el token.
/// </summary>
public sealed class LicenseRevocationMiddleware(
    RequestDelegate next,
    IOptions<LicenseOptions> options)
{
    private readonly bool _enabled = options.Value.CheckRevocationPerRequest;

    public async Task InvokeAsync(HttpContext context, ILicenseStore store)
    {
        if (_enabled && context.User.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirst("sub")?.Value;
            var license = Guid.TryParse(subject, out var id)
                ? await store.GetAsync(id, context.RequestAborted)
                : null;

            if (license is null || license.Revoked)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    new EngineError("license_revoked", "La licencia fue revocada."),
                    context.RequestAborted);
                return;
            }
        }

        await next(context);
    }
}
