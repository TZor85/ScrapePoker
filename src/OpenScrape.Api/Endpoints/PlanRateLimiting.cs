using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OpenScrape.Contracts;

namespace OpenScrape.Api.Endpoints;

/// <summary>
/// Cuotas por minuto. La activación se limita por IP; las decisiones, por
/// licencia y con el tope del plan.
/// </summary>
public static class PlanRateLimiting
{
    public const string ActivationPolicy = "activation";
    public const string DecisionsPolicy = "decisions";

    private const int ActivationPermitsPerMinute = 5;

    public static IServiceCollection AddPlanRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new EngineError("rate_limited", "Demasiados intentos. Esperá un minuto."),
                    cancellationToken);
            };

            options.AddPolicy(ActivationPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = ActivationPermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.AddPolicy(DecisionsPolicy, context =>
            {
                var subject = context.User.FindFirst("sub")?.Value ?? "anonimo";
                var plan = context.User.FindFirst("plan")?.Value ?? "free";
                var configured = configuration.GetValue<int?>($"RateLimit:Plans:{plan}:PermitsPerMinute");
                var permits = configured ?? (plan == "pro" ? 60 : 10);

                return RateLimitPartition.GetFixedWindowLimiter(
                    subject,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permits,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });
    }
}
