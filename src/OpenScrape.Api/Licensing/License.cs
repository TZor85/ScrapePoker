namespace OpenScrape.Api.Licensing;

/// <summary>
/// Licencia emitida a una instalación. La key en claro no se guarda:
/// sólo su hash con pepper.
/// </summary>
public sealed class License
{
    public Guid Id { get; set; }

    public string KeyHash { get; set; } = string.Empty;

    public string Plan { get; set; } = "free";

    public DateTimeOffset ExpiresAt { get; set; }

    public bool Revoked { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
