using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Logger interno del <see cref="MartenLoggerProvider"/>. Convierte el evento en
/// un <see cref="LogEntry"/> estructurado y lo encola; el worker es quien escribe.
/// </summary>
internal sealed class MartenLogger : ILogger
{
    private readonly MartenLoggerProvider _provider;
    private readonly string _category;
    private readonly int _maxMessageLength;

    public MartenLogger(MartenLoggerProvider provider, string category, int maxMessageLength)
    {
        _provider = provider;
        _category = category;
        _maxMessageLength = maxMessageLength;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(_category, logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || formatter is null)
            return;

        try
        {
            var scopes = new Dictionary<string, string>();

            // Argumentos estructurados del mensaje ("Se procesó {TableName}").
            if (state is IReadOnlyList<KeyValuePair<string, object?>> values)
            {
                foreach (var pair in values)
                {
                    if (!pair.Key.StartsWith('{'))
                        scopes[pair.Key] = MartenLoggerProvider.FormatValue(pair.Value);
                }
            }

            // Los scopes ambientales (sesión, mesa, mano) mandan sobre los argumentos.
            _provider.CaptureScopes(scopes);

            _provider.Enqueue(new LogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = logLevel.ToString(),
                Category = _category,
                EventId = eventId.Id,
                ThreadId = Environment.CurrentManagedThreadId,
                Message = Truncate(formatter(state, exception)),
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message,
                ExceptionStackTrace = exception?.StackTrace,
                Scopes = scopes
            });
        }
        catch (Exception ex)
        {
            // Un logger jamás debe romper al emisor.
            Debug.WriteLine($"[MartenLogger] Evento descartado: {ex}");
        }
    }

    private string Truncate(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return string.Empty;

        return message.Length <= _maxMessageLength
            ? message
            : message[.._maxMessageLength] + "…";
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
