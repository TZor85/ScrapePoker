using System.ComponentModel;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación Null Object de <see cref="IUiSyncService"/> para tests de
/// integración que no requieren UI. Ignora todas las llamadas.
/// </summary>
public sealed class NullUiSyncService : IUiSyncService
{
    public void Attach(IGameLoopCoordinator coordinator, ISynchronizeInvoke syncTarget, IFrmOverlay overlay, object? logsBox)
    {
    }

    public void Detach() { }

    public void Dispose() { }
}
