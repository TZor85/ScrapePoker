namespace OpenScrape.Contracts.Licensing;

/// <summary>Respuesta de activación: token vigente y datos para mostrar al usuario.</summary>
public sealed record LicenseActivateResponse
{
    public required string Token { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required string Plan { get; init; }
}
