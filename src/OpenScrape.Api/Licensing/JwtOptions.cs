namespace OpenScrape.Api.Licensing;

/// <summary>Configuración de firma y vigencia de los tokens.</summary>
public sealed class JwtOptions
{
    /// <summary>Clave HS256; el arranque exige al menos 32 bytes.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "openscrape";

    public int LifetimeHours { get; set; } = 24;
}
