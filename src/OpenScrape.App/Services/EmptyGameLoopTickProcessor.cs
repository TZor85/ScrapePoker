namespace OpenScrape.App.Services;

/// <summary>
/// Procesador temporal mientras la lógica real de captura y decisión se migra al coordinador.
/// </summary>
public sealed class EmptyGameLoopTickProcessor : IGameLoopTickProcessor
{
    public static EmptyGameLoopTickProcessor Instance { get; } = new();

    private EmptyGameLoopTickProcessor()
    {
    }

    public Task<GameLoopResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new GameLoopResult { Empty = true });
    }
}
