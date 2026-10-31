using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Resumen de un día de logs: lo mínimo para decidir qué borrar sin traer los
/// miles de eventos que contiene.
/// </summary>
public sealed record LogDaySummary(string Id, DateTime Date, long SizeBytes);

/// <summary>
/// Acceso a los documentos diarios de log. Se abstrae del <see cref="Marten.IDocumentStore"/>
/// para poder testear el agrupado y la acumulación sin una base real.
/// </summary>
public interface ILogDayStore
{
    /// <summary>Carga el día indicado (<c>yyyy-MM-dd</c> local) o null si no existe.</summary>
    Task<LogDay?> LoadAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Guarda el documento completo del día.</summary>
    Task SaveAsync(LogDay day, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina los días anteriores a <paramref name="cutoffLocalDate"/> y devuelve
    /// cuántos documentos borró.
    /// </summary>
    Task<int> DeleteBeforeAsync(DateTime cutoffLocalDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve fecha y tamaño de cada día guardado, sin cargar sus eventos.
    /// </summary>
    Task<IReadOnlyList<LogDaySummary>> LoadSummariesAsync(CancellationToken cancellationToken = default);
}
