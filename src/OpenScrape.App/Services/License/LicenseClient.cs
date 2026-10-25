using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenScrape.Contracts;
using OpenScrape.Contracts.Licensing;

namespace OpenScrape.App.Services.License;

/// <summary>
/// Llama a <c>/v1/license/activate</c> y devuelve la licencia lista para guardar.
/// No persiste nada: eso lo decide la pantalla de activación.
/// </summary>
public sealed class LicenseClient(HttpClient client)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));

    public async Task<StoredLicense> ActivateAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        using var response = await _client
            .PostAsJsonAsync(
                "/v1/license/activate",
                new LicenseActivateRequest { LicenseKey = key.Trim() },
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var error = await TryReadErrorAsync(response, cancellationToken).ConfigureAwait(false);
            var message = response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                ? error?.Message ?? "La licencia no es válida."
                : error?.Message ?? $"No se pudo activar la licencia ({(int)response.StatusCode}).";

            throw new LicenseRequiredException(message);
        }

        var body = await response.Content
            .ReadFromJsonAsync<LicenseActivateResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new LicenseRequiredException("El servidor no devolvió la licencia.");

        return new StoredLicense(key.Trim(), body.Token, body.ExpiresAt, body.Plan);
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
