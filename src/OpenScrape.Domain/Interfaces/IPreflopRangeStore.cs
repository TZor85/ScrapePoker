namespace OpenScrape.Domain.Interfaces;

using OpenScrape.Domain.Dtos;

/// <summary>
/// Acceso a la tabla de rangos de una situación preflop.
/// Existe para poder sustituir la persistencia en los tests de caracterización.
/// </summary>
public interface IPreflopRangeStore
{
    /// <summary>Devuelve la tabla de la situación indicada, o <c>null</c> si no existe.</summary>
    Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default);
}
