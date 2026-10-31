using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Destino persistente de los eventos de log. Se abstrae para poder testear
/// el proveedor sin una base de datos real.
/// </summary>
public interface ILogEntryWriter
{
    Task WriteAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina los días anteriores a <paramref name="cutoffUtc"/> y devuelve
    /// cuántos documentos borró.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra los días más viejos hasta que el total estimado entre en el presupuesto
    /// configurado. Devuelve cuántos documentos borró.
    /// </summary>
    Task<int> EnforceBudgetAsync(CancellationToken cancellationToken = default);
}
