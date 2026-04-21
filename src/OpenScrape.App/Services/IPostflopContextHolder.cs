using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Services;

/// <summary>
/// Poseedor único del <see cref="PostflopGameContext"/> durante la mano en curso.
/// Reemplaza las dos instancias paralelas que antes vivían en <c>FrmMain</c> y
/// <c>GameCoordinator</c>. Registrado como scoped en DI.
/// </summary>
public interface IPostflopContextHolder
{
    /// <summary>Snapshot actual del contexto.</summary>
    PostflopGameContext Current { get; }

    /// <summary>Aplica una transformación al contexto actual y reemplaza la referencia interna.</summary>
    void Update(Func<PostflopGameContext, PostflopGameContext> updater);

    /// <summary>Reemplaza el contexto por uno recién creado para una nueva mano.</summary>
    void StartNewHand();
}
