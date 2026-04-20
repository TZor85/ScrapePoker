namespace OpenScrape.App.Services;

/// <summary>
/// Coordinador del bucle de captura/decisión. Expone ciclo de vida y emite
/// <see cref="GameLoopResult"/> por cada iteración. Es independiente de UI:
/// no referencia <c>Control</c>, <c>Form</c> ni ensamblados WinForms.
/// </summary>
public interface IGameLoopCoordinator : IAsyncDisposable
{
    /// <summary>
    /// Evento publicado tras cada iteración del loop (exitosa o con error).
    /// Los suscriptores reciben el evento en el thread del pool, no en el UI.
    /// </summary>
    event EventHandler<GameLoopResult>? ResultReady;

    /// <summary>True mientras el loop está activo.</summary>
    bool IsRunning { get; }

    /// <summary>
    /// Inicia el loop con el intervalo configurado. Es idempotente: una segunda
    /// llamada mientras <see cref="IsRunning"/> es true retorna inmediatamente.
    /// </summary>
    /// <param name="cancellationToken">Cancelación externa del loop.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Detiene el loop. Espera a que la iteración en curso termine (con
    /// timeout configurable). Es idempotente.
    /// </summary>
    Task StopAsync();
}
