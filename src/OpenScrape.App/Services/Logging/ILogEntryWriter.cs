using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Destino persistente de los eventos de log. Se abstrae para poder testear
/// el proveedor sin una base de datos real.
/// </summary>
public interface ILogEntryWriter
{
    Task WriteAsync(IReadOnlyList<LogEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>Elimina las entradas anteriores a <paramref name="cutoffUtc"/>.</summary>
    Task DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
}
