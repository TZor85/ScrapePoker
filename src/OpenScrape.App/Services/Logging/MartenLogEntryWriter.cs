using Marten;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Persiste los eventos de log como documentos Marten en Postgres.
/// </summary>
public sealed class MartenLogEntryWriter : ILogEntryWriter
{
    private readonly IDocumentStore _store;

    public MartenLogEntryWriter(IDocumentStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task WriteAsync(
        IReadOnlyList<LogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
            return;

        await using var session = _store.LightweightSession();
        session.Store(entries.ToArray());
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteOlderThanAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        session.HardDeleteWhere<LogEntry>(entry => entry.TimestampUtc < cutoffUtc);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
