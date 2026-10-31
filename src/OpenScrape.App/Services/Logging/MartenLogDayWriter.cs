using System.Globalization;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Agrupa el lote por día local y lo acumula dentro del documento de ese día.
/// El día en curso se mantiene en memoria para no releerlo en cada flush.
/// </summary>
/// <remarks>
/// La base tiene un tope de 1 GB: además de la retención por días, el writer aplica
/// un máximo de eventos por día y un presupuesto total en bytes.
/// </remarks>
public sealed class MartenLogDayWriter : ILogEntryWriter
{
    /// <summary>Costo fijo estimado por evento (campos que no se miden acá).</summary>
    private const int EntryOverheadBytes = 200;

    private readonly ILogDayStore _store;
    private readonly MartenLoggerOptions _options;

    private LogDay? _current;

    public MartenLogDayWriter(ILogDayStore store, MartenLoggerOptions? options = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options ?? new MartenLoggerOptions();
    }

    public async Task WriteAsync(
        IReadOnlyList<LogEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
            return;

        // Un lote puede cruzar medianoche: primero se separa por día local.
        foreach (var group in entries.GroupBy(DayId))
        {
            var day = await GetDayAsync(group.Key, cancellationToken).ConfigureAwait(false);
            var added = group.ToList();

            day.Entries.AddRange(added);
            day.SizeBytes += added.Sum(EstimateBytes);
            TrimToDailyLimit(day);

            await _store.SaveAsync(day, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<int> DeleteOlderThanAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        var cutoffLocalDate = cutoffUtc.ToLocalTime().Date;
        var deleted = await _store.DeleteBeforeAsync(cutoffLocalDate, cancellationToken).ConfigureAwait(false);

        // La retención nunca alcanza al día en curso, pero si el reloj salta o se
        // configuran 0 días, la caché deja de ser válida.
        if (_current is not null && _current.Date < cutoffLocalDate)
            _current = null;

        return deleted;
    }

    /// <summary>
    /// Borra los días completos más viejos hasta entrar en el presupuesto de bytes.
    /// La retención por días no alcanza cuando el volumen de una sesión es alto.
    /// </summary>
    public async Task<int> EnforceBudgetAsync(CancellationToken cancellationToken = default)
    {
        if (_options.MaxTotalBytes <= 0)
            return 0;

        var summaries = await _store.LoadSummariesAsync(cancellationToken).ConfigureAwait(false);
        var totalBytes = summaries.Sum(day => day.SizeBytes);

        if (totalBytes <= _options.MaxTotalBytes)
            return 0;

        var cutoff = (DateTime?)null;

        foreach (var day in summaries.OrderBy(day => day.Date))
        {
            if (totalBytes <= _options.MaxTotalBytes)
                break;

            totalBytes -= day.SizeBytes;
            cutoff = day.Date.AddDays(1);
        }

        if (cutoff is null)
            return 0;

        var deleted = await _store.DeleteBeforeAsync(cutoff.Value, cancellationToken).ConfigureAwait(false);

        if (_current is not null && _current.Date < cutoff.Value)
            _current = null;

        return deleted;
    }

    /// <summary>
    /// Descarta los eventos más viejos del día: lo reciente es lo que sirve para
    /// diagnosticar, y así el documento no crece sin límite dentro del mismo día.
    /// </summary>
    private void TrimToDailyLimit(LogDay day)
    {
        var maxEntries = _options.MaxEntriesPerDay;
        if (maxEntries <= 0)
            return;

        var excess = day.Entries.Count - maxEntries;
        if (excess <= 0)
            return;

        for (var index = 0; index < excess; index++)
            day.SizeBytes -= EstimateBytes(day.Entries[index]);

        day.Entries.RemoveRange(0, excess);
        day.SizeBytes = Math.Max(0, day.SizeBytes);
    }

    private static long EstimateBytes(LogEntry entry)
    {
        var scopes = entry.Scopes?.Sum(scope => (long)scope.Key.Length + scope.Value.Length) ?? 0;

        return EntryOverheadBytes
            + (entry.Message?.Length ?? 0)
            + (entry.Category?.Length ?? 0)
            + (entry.ExceptionType?.Length ?? 0)
            + (entry.ExceptionMessage?.Length ?? 0)
            + (entry.ExceptionStackTrace?.Length ?? 0)
            + scopes;
    }

    private async Task<LogDay> GetDayAsync(string id, CancellationToken cancellationToken)
    {
        if (_current is not null && _current.Id == id)
            return _current;

        _current = await _store.LoadAsync(id, cancellationToken).ConfigureAwait(false)
            ?? new LogDay { Id = id, Date = ParseDay(id) };

        return _current;
    }

    private static string DayId(LogEntry entry) =>
        entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime ParseDay(string id) =>
        DateTime.ParseExact(id, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
