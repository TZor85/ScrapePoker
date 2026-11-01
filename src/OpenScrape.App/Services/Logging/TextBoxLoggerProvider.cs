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
    private HandContext? _handContext;
    private bool _disposed;
    private volatile bool _paused;

    /// <summary>-1 significa "sin nivel elegido": se muestra lo que permita la configuración.</summary>
    private int _exactLevel = -1;

    public TextBoxLoggerProvider(IOptions<TextBoxLoggerOptions> options)
    {
        Options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    internal TextBoxLoggerOptions Options { get; }

    internal IExternalScopeProvider? ScopeProvider { get; private set; }

    /// <summary>
    /// Mano en curso para etiquetar la línea. Se registra tardíamente, igual que el
    /// <see cref="TextBox"/> destino, porque el provider nace antes que el contenedor.
    /// </summary>
    internal long? CurrentHandNumber => _handContext?.Current;

    public void SetHandContext(HandContext handContext)
    {
        ArgumentNullException.ThrowIfNull(handContext);
        _handContext = handContext;
    }

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
    /// <summary>
    /// Deja de escribir en el panel para que el usuario pueda leer un histórico.
    /// Las líneas que llegan mientras tanto se descartan (no se bufferizan): el
    /// evento igual queda persistido en la base y en el archivo.
    /// </summary>
    public void PauseLive() => _paused = true;

    /// <summary>Vuelve a escribir el stream en vivo en el panel.</summary>
    public void ResumeLive() => _paused = false;

    /// <summary>
    /// Nivel concreto que se muestra en el panel, o <c>null</c> para mostrar todos los
    /// que permita la configuración. La UI lo cambia en caliente y el filtro se aplica
    /// al stream en vivo.
    /// </summary>
    public LogLevel? ExactLevel
    {
        get => _exactLevel < 0 ? null : (LogLevel)_exactLevel;
        set => Volatile.Write(ref _exactLevel, value is { } level ? (int)level : -1);
    }

    /// <summary>
    /// Decide si un evento llega al panel. Se evalúa antes de formatear la línea,
    /// así un nivel filtrado no cuesta trabajo de renderizado.
    /// </summary>
    internal bool IsEnabled(LogLevel level)
        => level != LogLevel.None
           && (ExactLevel is { } exact ? level == exact : level >= Options.MinimumLevel);

    internal void Emit(string line)
    {
        if (_disposed || _paused)
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

            // Si el usuario scrolleó para leer hacia arriba, el evento nuevo no debe
            // arrastrarlo al final: sólo se sigue el stream cuando ya estaba abajo.
            var caretBefore = target.SelectionStart;
            var followingStream = caretBefore >= target.TextLength;

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

            if (followingStream)
            {
                target.SelectionStart = target.TextLength;
                target.ScrollToCaret();
            }
            else
            {
                target.SelectionStart = Math.Min(caretBefore, target.TextLength);
                target.SelectionLength = 0;
            }
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
