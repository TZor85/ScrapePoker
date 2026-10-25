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
    /// <summary>Tiempo máximo por decisión: un turno no puede esperar más.</summary>
    public static readonly TimeSpan DecisionTimeout = TimeSpan.FromSeconds(1.5);

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

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(DecisionTimeout);
        var token = timeoutSource.Token;

        try
        {
            using var response = await _client
                .PostAsJsonAsync("/v1/decisions", request, JsonOptions, token)
                .ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                var error = await response.Content
                    .ReadFromJsonAsync<EngineError>(JsonOptions, token)
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
                .ReadFromJsonAsync<DecisionResult>(JsonOptions, token)
                .ConfigureAwait(false)
                ?? throw new EngineUnavailableException("El motor respondió sin decisión.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "El motor no respondió dentro de {Timeout}.", DecisionTimeout);
            throw new EngineUnavailableException("El motor no respondió a tiempo.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo contactar al motor.");
            throw new EngineUnavailableException("No se pudo contactar al motor.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "El motor respondió con un cuerpo inválido.");
            throw new EngineUnavailableException("El motor respondió con un cuerpo inválido.", ex);
        }
    }
}
