namespace OpenScrape.App.Services.License;

/// <summary>
/// Falta la licencia, venció o el servidor la rechazó. La UI debe pedir la key;
/// no hay fallback local.
/// </summary>
public sealed class LicenseRequiredException(string message, Exception? inner = null)
    : Exception(message, inner);
