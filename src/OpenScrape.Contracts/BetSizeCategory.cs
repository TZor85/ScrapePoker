namespace OpenScrape.Contracts;

/// <summary>
/// Categoría del tamaño de apuesta del villano. Es un derivado que calcula el servidor
/// a partir del monto; el cliente sólo copia la categoría ya observada en calles previas.
/// </summary>
public enum BetSizeCategory
{
    NoBet,
    Underbet,
    Small,
    Medium,
    Large
}
