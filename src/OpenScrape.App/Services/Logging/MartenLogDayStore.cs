using Marten;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Guarda cada día de logs como un único documento Marten cuyo Id es la fecha
/// local (<c>yyyy-MM-dd</c>).
/// </summary>
/// <remarks>
/// No usa <c>ILogger</c> a propósito: vive dentro del pipeline de logging y
/// loguear desde acá realimentaría el proveedor que lo está llamando.
/// </remarks>
public sealed class MartenLogDayStore : ILogDayStore
{
    private readonly IDocumentStore _store;

    public MartenLogDayStore(IDocumentStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<LogDay?> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        await using var session = _store.QuerySession();
        return await session.LoadAsync<LogDay>(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(LogDay day, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(day);

        await using var session = _store.LightweightSession();
        session.Store(day);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LogDaySummary>> LoadSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.QuerySession();

        // Proyección: no se traen los eventos de cada día, sólo lo que se necesita
        // para decidir qué borrar cuando el total supera el presupuesto.
        var rows = await session.Query<LogDay>()
            .Select(day => new { day.Id, day.Date, day.SizeBytes })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new LogDaySummary(row.Id, row.Date, row.SizeBytes))
            .ToList();
    }

    public async Task<int> DeleteBeforeAsync(
        DateTime cutoffLocalDate,
        CancellationToken cancellationToken = default)
    {
        var cutoff = cutoffLocalDate.Date;

        await using var session = _store.LightweightSession();

        // Marten no informa cuántas filas borró: se cuentan antes, una vez por día.
        var expired = await session.Query<LogDay>()
            .Where(day => day.Date < cutoff)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        if (expired == 0)
            return 0;

        session.HardDeleteWhere<LogDay>(day => day.Date < cutoff);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return expired;
    }
}
