using System.Text;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Formato único de una línea de log, compartido por el panel en vivo y por la
/// consulta histórica. La marca inicial permite ubicar el nivel sin leer el texto y,
/// al ser de ancho fijo, mantiene las líneas alineadas aunque no lleve color.
/// </summary>
public static class LogLineFormatter
{
    /// <summary>Marca de los niveles que se pintan.</summary>
    public const string Marker = "● ";

    /// <summary>Relleno de los niveles sin marca, para conservar la alineación.</summary>
    public const string NoMarker = "  ";

    public static string Format(LogEntry entry) => Format(entry, exception: null);

    public static string Format(LogEntry entry, Exception? exception)
    {
        var (prefix, message) = Split(entry, exception);

        return message.Length == 0 ? prefix : prefix + ' ' + message;
    }

    /// <summary>
    /// Separa el prefijo (que el panel pinta con el color del nivel) del cuerpo del
    /// mensaje (que queda en gris).
    /// </summary>
    public static (string Prefix, string Message) Split(LogEntry entry)
        => Split(entry, exception: null);

    public static (string Prefix, string Message) Split(LogEntry entry, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var level = ParseLevel(entry.Level);

        var prefix = new StringBuilder(128);
        prefix.Append(level == LogLevel.Information ? NoMarker : Marker)
              .Append('[')
              .Append(entry.TimestampUtc.ToLocalTime().ToString("HH:mm:ss"))
              .Append(' ')
              .Append(LevelToken(level))
              .Append(' ')
              .Append(ShortenCategory(entry.Category))
              .Append(']');

        if (entry.HandNumber is { } handNumber)
            prefix.Append(" {Mano=").Append(handNumber).Append('}');

        var message = entry.Message;

        var (exceptionType, exceptionMessage) = exception is null
            ? (entry.ExceptionType, entry.ExceptionMessage)
            : (exception.GetType().Name, exception.Message);

        if (!string.IsNullOrEmpty(exceptionType))
            message += " | " + exceptionType + ": " + exceptionMessage;

        return (prefix.ToString(), message);
    }

    /// <summary>
    /// Nivel de un evento ya persistido. Un valor desconocido se trata como
    /// <see cref="LogLevel.Trace"/> para que nunca se pierda una entrada.
    /// </summary>
    public static LogLevel ParseLevel(string? level)
        => Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed) ? parsed : LogLevel.Trace;

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
        => Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed) ? LevelToken(parsed) : "???";

    public static string ShortenCategory(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return string.Empty;

        var lastDot = categoryName.LastIndexOf('.');
        return lastDot < 0 ? categoryName : categoryName[(lastDot + 1)..];
    }
}
