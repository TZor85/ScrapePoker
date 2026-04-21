using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Services;

/// <summary>
/// Implementación thread-safe de <see cref="IPostflopContextHolder"/>. La referencia
/// al contexto se protege con <c>lock</c> para que <c>Update</c> funcione de forma
/// atómica incluso si el game loop llama desde hilos distintos. Las lecturas usan
/// <see cref="Volatile.Read{T}(ref T)"/> para barrera de memoria sin bloquear.
/// </summary>
public sealed class PostflopContextHolder : IPostflopContextHolder
{
    private readonly object _gate = new();
    private PostflopGameContext _current = PostflopGameContext.NewHand();

    public PostflopGameContext Current => Volatile.Read(ref _current);

    public void Update(Func<PostflopGameContext, PostflopGameContext> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);

        lock (_gate)
        {
            _current = updater(_current);
        }
    }

    public void StartNewHand()
    {
        lock (_gate)
        {
            _current = PostflopGameContext.NewHand();
        }
    }
}
