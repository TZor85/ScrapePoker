using System.ComponentModel;
using System.Windows.Forms;

using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación de <see cref="IUiSyncService"/> para WinForms. Se suscribe
/// a <see cref="IGameLoopCoordinator.ResultReady"/> y proyecta cada
/// <see cref="GameLoopResult"/> a overlay + logs usando
/// <see cref="ISynchronizeInvoke"/> para saltar al thread UI.
/// </summary>
public sealed class UiSyncService : IUiSyncService
{
    private readonly ILogger<UiSyncService> _logger;
    private readonly object _lock = new();

    private IGameLoopCoordinator? _coordinator;
    private ISynchronizeInvoke? _syncTarget;
    private IFrmOverlay? _overlay;
    private TextBox? _logsBox;
    private bool _disposed;

    public UiSyncService(ILogger<UiSyncService> logger)
    {
        _logger = logger;
    }

    public void Attach(IGameLoopCoordinator coordinator, ISynchronizeInvoke syncTarget, IFrmOverlay overlay, object? logsBox)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        lock (_lock)
        {
            if (_coordinator is not null)
                throw new InvalidOperationException("UiSyncService ya está conectado. Llama a Detach antes de reconectar.");

            _coordinator = coordinator;
            _syncTarget = syncTarget;
            _overlay = overlay;
            _logsBox = logsBox as TextBox;
            _coordinator.ResultReady += OnResultReady;
        }
    }

    public void Detach()
    {
        lock (_lock)
        {
            if (_coordinator is null) return;
            _coordinator.ResultReady -= OnResultReady;
            _coordinator = null;
            _syncTarget = null;
            _overlay = null;
            _logsBox = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
    }

    private void OnResultReady(object? sender, GameLoopResult result)
    {
        var target = _syncTarget;
        if (target is null) return;

        try
        {
            if (target.InvokeRequired)
                target.BeginInvoke(new Action(() => ApplyResult(result)), null);
            else
                ApplyResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrutando GameLoopResult al thread UI");
        }
    }

    private void ApplyResult(GameLoopResult result)
    {
        try
        {
            if (result.Error is not null)
            {
                AppendLog($"[ERROR] {result.Error.GetType().Name}: {result.Error.Message}");
                return;
            }

            if (result.Empty)
                return;

            var overlay = _overlay;
            if (overlay is not null)
            {
                if (!string.IsNullOrEmpty(result.RecommendedAction))
                    overlay.UpdateAction(result.RecommendedAction);
                if (result.DecisionResult is { } decision)
                {
                    overlay.UpdateEquityPercentage($"{decision.EquityPercent:F1}%");
                    overlay.UpdatePotOddsPercentage($"{decision.PotOddsPercent:F1}%");
                    if (!string.IsNullOrEmpty(decision.BoardTexture))
                        overlay.UpdateStreetPhase(decision.BoardTexture);
                }
                if (result.Street is { } street)
                    overlay.UpdateStreetIndicator(street.ToString());
            }

            if (!string.IsNullOrEmpty(result.LogText))
                AppendLog(result.LogText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo proyectando GameLoopResult a la UI");
        }
    }

    private void AppendLog(string text)
    {
        var box = _logsBox;
        if (box is null) return;
        box.AppendText(text + Environment.NewLine);
        box.SelectionStart = box.TextLength;
        box.ScrollToCaret();
    }
}
