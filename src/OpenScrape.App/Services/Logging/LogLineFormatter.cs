using System.Text;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Convierte una entrada persistida (<see cref="LogEntry"/>) a la misma línea que
/// muestra el panel en vivo, para que el histórico se lea igual que el stream.
/// </summary>
public static class LogLineFormatter
{
    public static string Format(LogEntry entry) => Format(entry, exception: null);

    public static string Format(LogEntry entry, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var sb = new StringBuilder(128);
        sb.Append('[')
          .Append(entry.TimestampUtc.ToLocalTime().ToString("HH:mm:ss"))
          .Append(' ')
          .Append(LevelToken(entry.Level))
          .Append(' ')
          .Append(ShortenCategory(entry.Category))
          .Append(']');

        if (entry.HandNumber is { } handNumber)
            sb.Append(" {Mano=").Append(handNumber).Append('}');

        sb.Append(' ').Append(entry.Message);

        var (exceptionType, exceptionMessage) = exception is null
            ? (entry.ExceptionType, entry.ExceptionMessage)
            : (exception.GetType().Name, exception.Message);

        if (!string.IsNullOrEmpty(exceptionType))
            sb.Append(" | ").Append(exceptionType).Append(": ").Append(exceptionMessage);

        return sb.ToString();
    }

    public static string LevelToken(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???"
    };

    public static string LevelToken(string? level)
        => Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed)
            ? LevelToken(parsed)
            : "???";

    public static string ShortenCategory(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return string.Empty;

        var lastDot = categoryName.LastIndexOf('.');
        return lastDot < 0 ? categoryName : categoryName[(lastDot + 1)..];
    }
}
