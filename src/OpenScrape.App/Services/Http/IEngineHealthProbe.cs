namespace OpenScrape.App.Services.Http;

/// <summary>
/// Chequeo de disponibilidad del motor remoto antes de arrancar la UI.
/// </summary>
public interface IEngineHealthProbe
{
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
