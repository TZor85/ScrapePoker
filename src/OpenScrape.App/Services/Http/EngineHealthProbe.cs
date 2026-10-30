using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Http;

/// <summary>
/// Pregunta una sola vez por <c>/health</c>. Sin esto, una API caída sólo se nota
/// cuando ya se está jugando: cada decisión termina en timeout y el usuario ve el
/// síntoma (fold o congelamiento) sin la causa.
/// </summary>
public sealed class EngineHealthProbe(
    HttpClient client,
    ILogger<EngineHealthProbe> logger) : IEngineHealthProbe
{
    /// <summary>Tope del chequeo: es arranque de app, no una decisión.</summary>
    public static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

    private readonly ILogger<EngineHealthProbe> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ProbeTimeout);

        try
        {
            using var response = await _client
                .GetAsync("/health", timeoutSource.Token)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return true;

            _logger.LogWarning(
                "El motor respondió {StatusCode} en {Endpoint}.",
                (int)response.StatusCode,
                Endpoint);

            return false;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "El motor no respondió al chequeo de salud en {Endpoint}.", Endpoint);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo contactar al motor en {Endpoint}.", Endpoint);
            return false;
        }
    }

    private string Endpoint => _client.BaseAddress?.ToString() ?? "(sin BaseAddress)";
}
