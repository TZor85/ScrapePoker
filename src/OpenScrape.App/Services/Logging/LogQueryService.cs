using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Lee los documentos <see cref="LogDay"/> y los recorta a la fecha y la mano pedidas.
/// El panel en vivo no sirve para volver atrás: esto sí.
/// Sin almacén (contenedores sin base, como el del cliente en tests) devuelve vacío.
/// </summary>
public sealed class LogQueryService(ILogDayStore? store) : ILogQueryService
{
    private readonly ILogDayStore? _store = store;

    public async Task<IReadOnlyList<LogDaySummary>> GetDaysAsync(
        CancellationToken cancellationToken = default)
    {
        if (_store is null)
            return Array.Empty<LogDaySummary>();

        var days = await _store.LoadSummariesAsync(cancellationToken).ConfigureAwait(false);

        return days.OrderByDescending(day => day.Date).ToList();
    }

    public async Task<IReadOnlyList<long>> GetHandsAsync(
        string dayId,
        CancellationToken cancellationToken = default)
    {
        var day = await LoadDayAsync(dayId, cancellationToken).ConfigureAwait(false);

        if (day is null)
            return Array.Empty<long>();

        return day.Entries
            .Where(entry => entry.HandNumber.HasValue)
            .Select(entry => entry.HandNumber!.Value)
            .Distinct()
            .OrderBy(handNumber => handNumber)
            .ToList();
    }

    public async Task<IReadOnlyList<LogEntry>> GetEntriesAsync(
        string dayId,
        long handNumber,
        CancellationToken cancellationToken = default)
    {
        var day = await LoadDayAsync(dayId, cancellationToken).ConfigureAwait(false);

        if (day is null)
            return Array.Empty<LogEntry>();

        return day.Entries
            .Where(entry => entry.HandNumber == handNumber)
            .OrderBy(entry => entry.TimestampUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<LogEntry>> GetEntriesAsync(
        string dayId,
        CancellationToken cancellationToken = default)
    {
        var day = await LoadDayAsync(dayId, cancellationToken).ConfigureAwait(false);

        if (day is null)
            return Array.Empty<LogEntry>();

        return day.Entries
            .OrderBy(entry => entry.TimestampUtc)
            .ToList();
    }

    private async Task<LogDay?> LoadDayAsync(string dayId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dayId))
            return null;

        return await _store.LoadAsync(dayId, cancellationToken).ConfigureAwait(false);
    }
}
