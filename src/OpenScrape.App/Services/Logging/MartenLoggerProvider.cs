using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services.Logging;

/// <summary>
/// Proveedor que persiste los eventos de log en Marten/Postgres sin bloquear la
/// UI: encola en un canal acotado y un worker en segundo plano escribe por lotes.
/// Si la base falla, el evento se descarta y se reporta por <see cref="Debug"/>;
/// nunca se vuelve a usar <see cref="ILogger"/> para no recursar.
/// </summary>
[ProviderAlias("Marten")]
public sealed class MartenLoggerProvider : ILoggerProvider, ISupportExternalScope, IAsyncDisposable
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetentionInterval = TimeSpan.FromDays(1);
    private static readonly TimeSpan RetentionRetryInterval = TimeSpan.FromHours(1);

    private readonly Func<ILogEntryWriter> _writerAccessor;
    private readonly MartenLoggerOptions _options;
    private readonly Channel<LogEntry>? _channel;
    private readonly SemaphoreSlim _drainLock = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _worker = Task.CompletedTask;
    private IExternalScopeProvider? _scopeProvider;
    private int _disposed;

    public MartenLoggerProvider(
        Func<ILogEntryWriter> writerAccessor,
        IOptions<MartenLoggerOptions> options)
    {
        _writerAccessor = writerAccessor ?? throw new ArgumentNullException(nameof(writerAccessor));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        if (!_options.Enabled)
            return;

        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(Math.Max(1, _options.ChannelCapacity))
        {
            // Si la cola se llena se descarta el evento nuevo: el logging jamás
            // puede frenar el hilo que lo emite.
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });

        _worker = Task.Run(() => ProcessAsync(_shutdown.Token));
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopeProvider = scopeProvider;

    public ILogger CreateLogger(string categoryName) =>
        _channel is null
            ? NullLogger.Instance
            : new MartenLogger(this, categoryName, Math.Max(1, _options.MaxMessageLength));

    /// <summary>
    /// Vacía la cola pendiente. Se usa al cerrar la aplicación para no perder
    /// los últimos eventos; cualquier error se descarta silenciosamente.
    /// </summary>
    public async Task FlushAsync()
    {
        if (_channel is null)
            return;

        try
        {
            await DrainAsync(CancellationToken.None).WaitAsync(FlushTimeout).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MartenLogger] No se pudo vaciar la cola: {ex}");
        }
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        var channel = _channel;
        if (channel is null)
        {
            _shutdown.Dispose();
            _drainLock.Dispose();
            return;
        }

        // Cerrar el canal impide nuevos eventos y deja drenar los pendientes.
        channel.Writer.TryComplete();
        await FlushAsync().ConfigureAwait(false);

        _shutdown.Cancel();
        try
        {
            await _worker.WaitAsync(FlushTimeout).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MartenLogger] El worker no terminó a tiempo: {ex}");
        }

        _shutdown.Dispose();
        _drainLock.Dispose();
    }

    internal bool IsEnabled(LogLevel level) =>
        level != LogLevel.None && Volatile.Read(ref _disposed) == 0;

    internal void Enqueue(LogEntry entry)
    {
        // TryWrite nunca bloquea; con DropWrite el evento se pierde si está llena.
        _channel?.Writer.TryWrite(entry);
    }

    /// <summary>
    /// Copia los scopes activos (SessionId, TableName, HandNumber...) al evento.
    /// </summary>
    internal void CaptureScopes(Dictionary<string, string> destination)
    {
        _scopeProvider?.ForEachScope(
            static (scope, scopes) =>
            {
                switch (scope)
                {
                    case IEnumerable<KeyValuePair<string, object?>> values:
                        foreach (var pair in values)
                            scopes[pair.Key] = FormatValue(pair.Value);
                        break;

                    case KeyValuePair<string, object?> pair:
                        scopes[pair.Key] = FormatValue(pair.Value);
                        break;

                    default:
                        var text = FormatValue(scope);
                        if (text.Length > 0)
                            scopes["Scope"] = text;
                        break;
                }
            },
            destination);
    }

    internal static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var channel = _channel!;
        var flushInterval = Math.Max(0, _options.FlushIntervalMs);

        try
        {
            // Retención al arrancar: limpia lo que quedó de sesiones anteriores.
            var nextRetentionUtc = DateTime.UtcNow
                .Add(await ApplyRetentionNowAsync(cancellationToken).ConfigureAwait(false));

            while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // Ventana de agrupación: junta varios eventos en una sola escritura.
                if (flushInterval > 0)
                    await Task.Delay(flushInterval, cancellationToken).ConfigureAwait(false);

                await DrainAsync(cancellationToken).ConfigureAwait(false);

                if (DateTime.UtcNow >= nextRetentionUtc)
                {
                    nextRetentionUtc = DateTime.UtcNow
                        .Add(await ApplyRetentionNowAsync(cancellationToken).ConfigureAwait(false));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cierre normal de la aplicación.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MartenLogger] El worker de persistencia se detuvo: {ex}");
        }
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        var channel = _channel;
        if (channel is null)
            return;

        await _drainLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var batchSize = Math.Max(1, _options.BatchSize);
            var batch = new List<LogEntry>(batchSize);
            var writer = _writerAccessor();

            while (channel.Reader.TryRead(out var entry))
            {
                batch.Add(entry);
                if (batch.Count >= batchSize)
                {
                    await writer.WriteAsync(batch.ToArray(), cancellationToken).ConfigureAwait(false);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await writer.WriteAsync(batch.ToArray(), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // La cola queda para el próximo intento.
        }
        catch (Exception ex)
        {
            // Un fallo de base no debe tumbar la aplicación ni recursar en ILogger.
            Debug.WriteLine($"[MartenLogger] No se pudieron escribir los eventos: {ex}");
        }
        finally
        {
            _drainLock.Release();
        }
    }

    /// <summary>
    /// Aplica la retención una vez y devuelve cuánto esperar antes del próximo
    /// intento. Un fallo de base no detiene la aplicación: se reintenta antes.
    /// </summary>
    internal async Task<TimeSpan> ApplyRetentionNowAsync(CancellationToken cancellationToken)
    {
        var cutoffUtc = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));

        try
        {
            var writer = _writerAccessor();
            await writer.DeleteOlderThanAsync(cutoffUtc, cancellationToken).ConfigureAwait(false);
            return RetentionInterval;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MartenLogger] No se pudo aplicar la retención: {ex}");
            return RetentionRetryInterval;
        }
    }
}
