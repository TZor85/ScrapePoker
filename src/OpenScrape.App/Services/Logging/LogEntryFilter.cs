using Microsoft.Extensions.Logging;

using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Filtro de niveles para el histórico de logs. El nivel viene guardado como texto,
/// así que se parsea y, si no se reconoce, se trata como <see cref="LogLevel.Trace"/>
/// para no esconder eventos por un dato corrupto. Sin nivel elegido no se descarta nada.
/// </summary>
public static class LogEntryFilter
{
    public static IReadOnlyList<LogEntry> ByLevel(
        IEnumerable<LogEntry> entries,
        LogLevel? level)
    {
        ArgumentNullException.ThrowIfNull(entries);

        if (level is not { } wanted)
            return entries.ToArray();

        return entries.Where(entry => LevelOf(entry) == wanted).ToArray();
    }

    private static LogLevel LevelOf(LogEntry entry)
        => Enum.TryParse<LogLevel>(entry.Level, ignoreCase: true, out var level)
            ? level
            : LogLevel.Trace;
}
