namespace OpenScrape.Contracts;

/// <summary>
/// Calle sobre la que se pide la decisión. Es un hecho que manda el cliente.
/// </summary>
public enum DecisionStreet
{
    Preflop = 0,
    Flop = 1,
    Turn = 2,
    River = 3
}
