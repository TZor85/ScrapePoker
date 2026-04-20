using OpenScrape.DecisionMaker.DTOs;

namespace OpenScrape.App.Services;

/// <summary>
/// Fachada que agrega el pipeline completo de decisión postflop en una sola
/// superficie: equity (IPokerCalculator) + textura (IBoardTextureAnalyzer) +
/// perfil (IOpponentTracker) + decisión (IPostflopDecisionService) +
/// sizing (IBetSizingService). Los consumidores no inyectan los 5 servicios
/// por separado; inyectan este facade.
/// </summary>
public interface IPokerDecisionFacade
{
    /// <summary>
    /// Evalúa una situación postflop completa y retorna la acción recomendada
    /// junto con la telemetría asociada.
    /// </summary>
    /// <param name="request">Contexto inmutable de la decisión.</param>
    /// <param name="cancellationToken">
    /// Token de cancelación. Si se cancela durante el cálculo Monte Carlo,
    /// el método lanza <see cref="OperationCanceledException"/>.
    /// </param>
    Task<DecisionResult> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}
