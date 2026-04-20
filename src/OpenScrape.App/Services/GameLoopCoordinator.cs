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
        IOptions<GameLoopOptions> options)
    {
        _logger = logger;
        _options = options.Value;
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
                await TickAsync(token).ConfigureAwait(false);
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
    private Task TickAsync(CancellationToken token)
    {
        lock (_tickLock)
        {
            if (_tickInProgress)
                return Task.CompletedTask;
            _tickInProgress = true;
        }

        try
        {
            token.ThrowIfCancellationRequested();
            SafeEmit(new GameLoopResult { Empty = true });
            return Task.CompletedTask;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en iteración del loop");
            SafeEmit(new GameLoopResult { Error = ex });
            return Task.CompletedTask;
        }
        finally
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
