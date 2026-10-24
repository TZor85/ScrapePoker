namespace OpenScrape.Contracts;

/// <summary>
/// Carta del mazo por rango y palo. Es un hecho que manda el cliente.
/// </summary>
public readonly record struct CardRef(int Rank, int Suit);
