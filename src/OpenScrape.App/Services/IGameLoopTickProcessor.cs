namespace OpenScrape.App.Services;

/// <summary>
/// Procesa una iteración del game loop sin conocer UI ni temporización.
/// </summary>
public interface IGameLoopTickProcessor
{
    Task<GameLoopResult> ExecuteAsync(CancellationToken cancellationToken);
}

