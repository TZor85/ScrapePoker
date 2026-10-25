using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenScrape.Contracts;
using OpenScrape.App.Services;
using OpenScrape.App.Services.License;

namespace OpenScrape.App.Services.Http;

/// <summary>
/// Cliente del motor remoto. Sólo conoce el contrato: no calcula nada.
/// Sin reintentos, para no multiplicar la latencia del turno.
/// </summary>
public sealed class HttpDecisionEngine(
    HttpClient client,
    ILogger<HttpDecisionEngine> logger,
    LicenseTokenStore? licenseStore = null) : IDecisionEngine
{
    /// <summary>Tiempo máximo por decisión, impuesto acá y no en el HttpClient.</summary>
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

        var license = licenseStore?.Load();

        if (licenseStore is not null &&
            (license is null || string.IsNullOrEmpty(license.Token) || license.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            // Sin licencia vigente no se toca la red: no hay nada que consultar.
            licenseStore.Clear();
            throw new LicenseRequiredException("No hay una licencia vigente. Activá el motor con tu key.");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(DecisionTimeout);
        var token = timeoutSource.Token;

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, "/v1/decisions")
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };

            if (license is not null)
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", license.Token);

            using var response = await _client.SendAsync(message, token).ConfigureAwait(false);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                var error = await TryReadErrorAsync(response, token).ConfigureAwait(false);
                licenseStore?.Clear();
                throw new LicenseRequiredException(
                    error?.Message ?? "La licencia fue rechazada. Activá el motor de nuevo.");
            }

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                var error = await TryReadErrorAsync(response, token).ConfigureAwait(false);

                throw new ArgumentException(
                    error?.Message ?? "El motor rechazó la solicitud.",
                    nameof(request));
            }

            if (!response.IsSuccessStatusCode)
                throw new EngineUnavailableException($"El motor respondió {(int)response.StatusCode}.");

            return await response.Content
                .ReadFromJsonAsync<DecisionResult>(JsonOptions, token)
                .ConfigureAwait(false)
                ?? throw new EngineUnavailableException("El motor respondió sin decisión.");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "El motor no respondió dentro del tiempo de espera.");
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

    private static async Task<EngineError?> TryReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content
                .ReadFromJsonAsync<EngineError>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
