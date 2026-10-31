using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Consulta de los logs ya persistidos, para volver a una fecha y una mano concretas.
/// </summary>
public interface ILogQueryService
{
    Task<IReadOnlyList<LogDaySummary>> GetDaysAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<long>> GetHandsAsync(string dayId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogEntry>> GetEntriesAsync(
        string dayId,
        long handNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Todas las entradas del día, sin acotar a una mano. Las líneas conservan su
    /// <c>HandNumber</c>, así se distingue a qué mano pertenece cada evento.
    /// </summary>
    Task<IReadOnlyList<LogEntry>> GetEntriesAsync(
        string dayId,
        CancellationToken cancellationToken = default);
}
