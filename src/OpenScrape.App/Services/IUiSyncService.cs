using System.ComponentModel;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio que proyecta <see cref="GameLoopResult"/> emitido por el
/// <see cref="IGameLoopCoordinator"/> a los controles WinForms (overlay +
/// tab de logs) marshalleando la actualización al thread de UI vía
/// <see cref="ISynchronizeInvoke"/>. El coordinator no conoce qué
/// controles se actualizan; esta clase los encapsula.
/// </summary>
public interface IUiSyncService : IDisposable
{
    /// <summary>
    /// Conecta el servicio a la UI: se suscribe al evento <c>ResultReady</c>
    /// del coordinator proporcionado y enruta las actualizaciones al form,
    /// overlay y caja de logs indicados.
    /// </summary>
    /// <param name="coordinator">Origen de los <see cref="GameLoopResult"/>.</param>
    /// <param name="syncTarget">Control WinForms (típicamente <c>FrmMain</c>) para marshal al UI thread.</param>
    /// <param name="overlay">Overlay donde proyectar la decisión.</param>
    /// <param name="logsBox">Control de texto donde anexar logs estructurados.</param>
    void Attach(IGameLoopCoordinator coordinator, ISynchronizeInvoke syncTarget, IFrmOverlay overlay, object? logsBox);

    /// <summary>
    /// Desconecta el servicio del coordinator. Idempotente: puede llamarse
    /// varias veces sin efectos colaterales.
    /// </summary>
    void Detach();
}
