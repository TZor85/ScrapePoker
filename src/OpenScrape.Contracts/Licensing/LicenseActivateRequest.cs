namespace OpenScrape.Contracts.Licensing;

/// <summary>Pedido de activación: la key en claro viaja una sola vez.</summary>
public sealed record LicenseActivateRequest
{
    public required string LicenseKey { get; init; }
}
