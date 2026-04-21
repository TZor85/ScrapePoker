using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// <see cref="ILoggerProvider"/> que entrega <see cref="TextBoxLogger"/> por categoría.
/// El <see cref="TextBox"/> destino se registra <b>tardíamente</b> vía
/// <see cref="SetTextBoxTarget(TextBox)"/> porque se construye antes de que
/// <see cref="Forms.FrmMain"/> exista.
/// </summary>
[ProviderAlias("TextBox")]
public sealed class TextBoxLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, TextBoxLogger> _loggers = new();
    private readonly ConcurrentQueue<string> _pendingLines = new();
    private readonly object _targetGate = new();
    private TextBox? _target;
    private bool _disposed;

    public TextBoxLoggerProvider(IOptions<TextBoxLoggerOptions> options)
    {
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    internal TextBoxLoggerOptions Options { get; }

    internal IExternalScopeProvider? ScopeProvider { get; private set; }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        => ScopeProvider = scopeProvider;

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name => new TextBoxLogger(name, this));

    /// <summary>
    /// Registra el <see cref="TextBox"/> destino. Sólo puede llamarse una vez;
    /// llamadas posteriores lanzan <see cref="InvalidOperationException"/>.
    /// Si <see cref="TextBoxLoggerOptions.BufferUntilTargetReady"/> está activo,
    /// vuelca los eventos pendientes al registrar.
    /// </summary>
    public void SetTextBoxTarget(TextBox target)
    {
        ArgumentNullException.ThrowIfNull(target);

        lock (_targetGate)
        {
            if (_target is not null)
                throw new InvalidOperationException("TextBox target already registered.");

            _target = target;
        }

        if (Options.BufferUntilTargetReady)
        {
            while (_pendingLines.TryDequeue(out var line))
                WriteToTarget(target, line);
        }
        else
        {
            // Descartar cualquier buffer residual.
            while (_pendingLines.TryDequeue(out _)) { }
        }
    }

    /// <summary>Emite una línea pre-formateada al TextBox (o la bufferiza / descarta).</summary>
    internal void Emit(string line)
    {
        if (_disposed)
            return;

        var target = _target;
        if (target is null)
        {
            if (Options.BufferUntilTargetReady)
            {
                _pendingLines.Enqueue(line);
                while (_pendingLines.Count > Options.BufferCapacity && _pendingLines.TryDequeue(out _)) { }
            }
            return;
        }

        WriteToTarget(target, line);
    }

    private void WriteToTarget(TextBox target, string line)
    {
        try
        {
            if (target.IsDisposed) return;

            if (target.InvokeRequired)
            {
                target.BeginInvoke(() => AppendWithRotation(target, line));
            }
            else
            {
                AppendWithRotation(target, line);
            }
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    private void AppendWithRotation(TextBox target, string line)
    {
        try
        {
            if (target.IsDisposed) return;

            var maxLines = Options.MaxLines;
            if (maxLines > 0)
            {
                var currentLines = target.Lines;
                if (currentLines.Length >= maxLines)
                {
                    var keep = Math.Max(0, maxLines - 1);
                    target.Lines = currentLines[^keep..];
                }
            }

            target.AppendText(line + Environment.NewLine);
            target.SelectionStart = target.TextLength;
            target.ScrollToCaret();
        }
        catch (ObjectDisposedException) { }
        catch (InvalidOperationException) { }
    }

    public void Dispose()
    {
        _disposed = true;
        _loggers.Clear();
    }
}
