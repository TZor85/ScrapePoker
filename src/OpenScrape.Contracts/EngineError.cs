namespace OpenScrape.Contracts;

/// <summary>
/// Error estable del contrato. El código es el que el cliente interpreta; el mensaje es para el log.
/// </summary>
public sealed record EngineError(string Code, string Message);
