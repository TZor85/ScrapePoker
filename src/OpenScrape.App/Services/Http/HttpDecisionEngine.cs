using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenScrape.Contracts;
using OpenScrape.DecisionMaker;

namespace OpenScrape.App.Services.Http;

/// <summary>
/// Cliente del motor remoto. Sólo conoce el contrato: no calcula nada.
/// Sin reintentos, para no multiplicar la latencia del turno.
/// </summary>
public sealed class HttpDecisionEngine(HttpClient client, ILogger<HttpDecisionEngine> logger) : IDecisionEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

    private readonly ILogger<HttpDecisionEngine> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<DecisionResult> DecideAsync(
        DecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            using var response = await _client
                .PostAsJsonAsync("/v1/decisions", request, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                var error = await response.Content
                    .ReadFromJsonAsync<EngineError>(JsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                throw new ArgumentException(
                    error?.Message ?? "El motor rechazó la solicitud.",
                    nameof(request));
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new EngineUnavailableException(
                    $"El motor respondió {(int)response.StatusCode}.");
            }

            return await response.Content
                .ReadFromJsonAsync<DecisionResult>(JsonOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new EngineUnavailableException("El motor respondió sin decisión.");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "El motor no respondió dentro del tiempo de espera.");
            throw new EngineUnavailableException("El motor no respondió a tiempo.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo contactar al motor.");
            throw new EngineUnavailableException("No se pudo contactar al motor.", ex);
        }
    }
}
