namespace OpenScrape.Api.Licensing;

/// <summary>Configuración del subsistema de licencias.</summary>
public sealed class LicenseOptions
{
    /// <summary>Secreto que se concatena a la key antes de hashearla.</summary>
    public string Pepper { get; set; } = string.Empty;

    /// <summary>
    /// Si está prendido, cada request revalida que la licencia no esté revocada.
    /// Apagado por defecto: los tokens viven hasta vencer.
    /// </summary>
    public bool CheckRevocationPerRequest { get; set; }
}
