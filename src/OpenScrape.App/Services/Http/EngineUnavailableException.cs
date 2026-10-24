namespace OpenScrape.App.Services.Http;

/// <summary>
/// El motor remoto no respondió: red caída, timeout o error del servidor.
/// No hay fallback local; el loop debe detenerse y avisar.
/// </summary>
public sealed class EngineUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
