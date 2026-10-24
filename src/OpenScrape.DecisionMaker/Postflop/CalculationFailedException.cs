namespace OpenScrape.DecisionMaker.Postflop;

/// <summary>
/// La equity no se pudo calcular. El motor no debe emitir una decisión con
/// datos parciales: el llamador tiene que rechazar el pedido.
/// </summary>
public sealed class CalculationFailedException(string message)
    : Exception($"No se pudo calcular la decisión: {message}");
