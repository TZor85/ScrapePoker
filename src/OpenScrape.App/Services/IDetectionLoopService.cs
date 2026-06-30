using System.Drawing;
using OpenScrape.App.Services;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato para el servicio de loop de detección de píxeles.
/// Reemplaza el BackgroundWorker de FrmMain por un Task.Run con notificación por eventos.
/// </summary>
public interface IDetectionLoopService
{
    /// <summary>Emitido cuando se detecta el turno del héroe.</summary>
    event EventHandler<HeroTurnDetectedEventArgs>? HeroTurnDetected;

    /// <summary>Emitido cuando se detecta cambio de calle (flop).</summary>
    event EventHandler<FlopDetectedEventArgs>? FlopDetected;

    /// <summary>Emitido cuando el loop de detección se detiene.</summary>
    event EventHandler? DetectionStopped;

    /// <summary>Emitido tras cada iteración del loop, para tracking de ventana.</summary>
    event EventHandler? IterationCompleted;

    /// <summary>Indica si el loop de detección está activo.</summary>
    bool IsRunning { get; }

    /// <summary>Contador total de chequeos de píxeles.</summary>
    int TotalChecks { get; }

    /// <summary>Contador de detecciones de turno del héroe.</summary>
    int Detections { get; }

    /// <summary>Contador de errores en el loop.</summary>
    int Errors { get; }

    /// <summary>
    /// Inicia el loop de detección en background.
    /// </summary>
    /// <param name="windowHandle">Handle de la ventana de póker a monitorizar.</param>
    /// <param name="isOverlayVisible">Delegate que indica si el overlay está visible.</param>
    /// <param name="cancellationToken">Token de cancelación externo.</param>
    Task StartAsync(IntPtr windowHandle, Func<bool> isOverlayVisible, CancellationToken cancellationToken);

    /// <summary>
    /// Detiene el loop de detección y emite <see cref="DetectionStopped"/>.
    /// </summary>
    Task StopAsync();
}

/// <summary>
/// Argumentos del evento <see cref="IDetectionLoopService.HeroTurnDetected"/>.
/// </summary>
public sealed record HeroTurnDetectedEventArgs(
    Color ActionColor,
    GameState CurrentState,
    DateTimeOffset Timestamp);

/// <summary>
/// Argumentos del evento <see cref="IDetectionLoopService.FlopDetected"/>.
/// </summary>
public sealed record FlopDetectedEventArgs(
    Color FlopColor,
    DateTimeOffset Timestamp);
