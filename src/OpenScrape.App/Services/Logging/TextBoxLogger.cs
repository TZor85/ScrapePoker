using System.Text;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Implementación de <see cref="ILogger"/> que renderiza cada evento a un
/// <see cref="TextBox"/> de WinForms respetando cross-thread vía
/// <see cref="Control.Invoke(Delegate)"/>. El destino concreto se registra
/// tardíamente vía <see cref="TextBoxLoggerProvider.SetTextBoxTarget(TextBox)"/>.
/// </summary>
public sealed class TextBoxLogger : ILogger
{
    private readonly string _categoryShortName;
    private readonly TextBoxLoggerProvider _provider;

    public TextBoxLogger(string categoryName, TextBoxLoggerProvider provider)
    {
        _categoryShortName = ShortenCategory(categoryName);
        _provider = provider;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _provider.ScopeProvider?.Push(state);

    public bool IsEnabled(LogLevel logLevel)
        => logLevel != LogLevel.None && logLevel >= _provider.Options.MinimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter is null)
            return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception is null)
            return;

        var line = RenderLine(logLevel, message, exception);
        _provider.Emit(line);
    }

    private string RenderLine(LogLevel logLevel, string message, Exception? exception)
    {
        var sb = new StringBuilder(128);
        sb.Append('[')
          .Append(DateTime.Now.ToString("HH:mm:ss"))
          .Append(' ')
          .Append(LevelToken(logLevel))
          .Append(' ')
          .Append(_categoryShortName)
          .Append(']');

        AppendScopes(sb);

        sb.Append(' ').Append(message);

        if (exception is not null)
        {
            sb.Append(" | ").Append(exception.GetType().Name).Append(": ").Append(exception.Message);
        }

        return sb.ToString();
    }

    private void AppendScopes(StringBuilder sb)
    {
        var scopes = _provider.ScopeProvider;
        if (scopes is null)
            return;

        var pairs = new List<string>();
        scopes.ForEachScope((scope, state) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> kvs)
            {
                foreach (var kv in kvs)
                    state.Add($"{kv.Key}={kv.Value}");
            }
            else if (scope is not null)
            {
                state.Add($"Scope={scope}");
            }
        }, pairs);

        if (pairs.Count == 0)
            return;

        sb.Append(" {").Append(string.Join(", ", pairs)).Append('}');
    }

    private static string LevelToken(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???"
    };

    private static string ShortenCategory(string categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return string.Empty;

        var lastDot = categoryName.LastIndexOf('.');
        return lastDot < 0 ? categoryName : categoryName[(lastDot + 1)..];
    }
}
