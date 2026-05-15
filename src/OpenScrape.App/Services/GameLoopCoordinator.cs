using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenScrape.App.Configuration;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación del coordinador del game loop. En esta fase (esqueleto)
/// gestiona solo el ciclo de vida (start/stop/cancel/dispose) y emite un
/// <see cref="GameLoopResult"/> vacío por cada tick.
///
/// La lógica real (captura → OCR → decisión → resultado) se migra en la
/// Fase 4 del change <c>refactor-frmmain-coordinators</c>. Las dependencias
/// necesarias para esa fase se añadirán al constructor entonces.
/// </summary>
public sealed class GameLoopCoordinator : IGameLoopCoordinator
{
    private readonly ILogger<GameLoopCoordinator> _logger;
    private readonly GameLoopOptions _options;
    private readonly IGameLoopTickProcessor _tickProcessor;

    private readonly SemaphoreSlim _startStopLock = new(1, 1);
    private readonly object _tickLock = new();

    private CancellationTokenSource? _internalCts;
    private Task? _loopTask;
    private volatile bool _tickInProgress;
    private volatile bool _isRunning;
    private bool _disposed;

    public event EventHandler<GameLoopResult>? ResultReady;

    public bool IsRunning => _isRunning;

    public GameLoopCoordinator(
        ILogger<GameLoopCoordinator> logger,
        IOptions<GameLoopOptions> options,
        IGameLoopTickProcessor? tickProcessor = null)
    {
        _logger = logger;
        _options = options.Value;
        _tickProcessor = tickProcessor ?? EmptyGameLoopTickProcessor.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _startStopLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isRunning)
            {
                _logger.LogDebug("StartAsync ignorado: loop ya activo");
                return;
            }

            _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = _internalCts.Token;
            _isRunning = true;
            _loopTask = Task.Run(() => RunLoopAsync(linkedToken), linkedToken);
            _logger.LogInformation(
                "GameLoopCoordinator iniciado (intervalo {IntervalMs}ms)",
                _options.CaptureIntervalMs);
        }
        finally
        {
            _startStopLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await _startStopLock.WaitAsync().ConfigureAwait(false);
        Task? loopTask;
        try
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _internalCts?.Cancel();
            loopTask = _loopTask;
        }
        finally
        {
            _startStopLock.Release();
        }

        if (loopTask is not null)
        {
            try
            {
                var timeout = TimeSpan.FromMilliseconds(_options.StopTimeoutMs);
                var completed = await Task.WhenAny(loopTask, Task.Delay(timeout)).ConfigureAwait(false);
                if (completed != loopTask)
                {
                    _logger.LogWarning(
                        "GameLoopCoordinator.StopAsync: timeout tras {TimeoutMs}ms, forzando dispose",
                        _options.StopTimeoutMs);
                }
                else
                {
                    // Observa excepciones no-cancelación.
                    await loopTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Esperado al cancelar.
            }
        }

        DisposeCts();
        _loopTask = null;
        _logger.LogInformation("GameLoopCoordinator detenido");
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            _disposed = true;
            _startStopLock.Dispose();
            DisposeCts();
        }
    }

    private void DisposeCts()
    {
        var cts = _internalCts;
        _internalCts = null;
        cts?.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.CaptureIntervalMs));
        try
        {
            do
            {
                if (token.IsCancellationRequested) break;
                await RunTickWithWatchdogAsync(token).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Cancelación limpia.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo inesperado en el loop; emitiendo GameLoopResult con error y parando");
            SafeEmit(new GameLoopResult { Error = ex });
        }
    }

    /// <summary>
    /// Procesa una iteración. En el esqueleto emite un resultado vacío.
    /// La Fase 4 reemplazará esta implementación con la lógica real del
    /// pipeline (captura → OCR → decisión).
    /// </summary>
    private async Task RunTickWithWatchdogAsync(CancellationToken token)
    {
        if (!_options.WatchdogEnabled || _options.WatchdogTimeoutMs <= 0)
        {
            await TickAsync(token).ConfigureAwait(false);
            return;
        }

        var tickCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var tickTask = TickAsync(tickCts.Token);
        var timeout = TimeSpan.FromMilliseconds(_options.WatchdogTimeoutMs);
        var timeoutTask = Task.Delay(timeout, token);
        var completed = await Task.WhenAny(tickTask, timeoutTask).ConfigureAwait(false);

        if (completed == tickTask)
        {
            try
            {
                await tickTask.ConfigureAwait(false);
                return;
            }
            finally
            {
                tickCts.Dispose();
            }
        }

        tickCts.Cancel();
        if (token.IsCancellationRequested)
        {
            ObserveTickCompletion(tickTask, tickCts);
            token.ThrowIfCancellationRequested();
        }

        ResetTickGate();

        var exception = new TimeoutException(
            $"Game loop tick bloqueado durante más de {_options.WatchdogTimeoutMs}ms");
        _logger.LogWarning(
            exception,
            "Watchdog del GameLoopCoordinator disparado tras {TimeoutMs}ms",
            _options.WatchdogTimeoutMs);
        SafeEmit(new GameLoopResult { Error = exception });

        ObserveTickCompletion(tickTask, tickCts);
    }

    private static void ObserveTickCompletion(Task tickTask, CancellationTokenSource tickCts)
    {
        _ = tickTask.ContinueWith(
            task =>
            {
                _ = task.Exception;
                tickCts.Dispose();
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task TickAsync(CancellationToken token)
    {
        lock (_tickLock)
        {
            if (_tickInProgress)
                return;
            _tickInProgress = true;
        }

        try
        {
            token.ThrowIfCancellationRequested();
            var result = await _tickProcessor.ExecuteAsync(token).ConfigureAwait(false);
            SafeEmit(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en iteración del loop");
            SafeEmit(new GameLoopResult { Error = ex });
        }
        finally
        {
            ResetTickGate();
        }
    }

    private void ResetTickGate()
    {
        lock (_tickLock)
        {
            _tickInProgress = false;
        }
    }

    private void SafeEmit(GameLoopResult result)
    {
        var handler = ResultReady;
        if (handler is null) return;
        foreach (var sub in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<GameLoopResult>)sub)(this, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Un suscriptor de ResultReady lanzó excepción; ignorada para no parar el loop ni otros suscriptores");
            }
        }
    }
}
