using OpenScrape.Contracts;

namespace OpenScrape.DecisionMaker;

/// <summary>
/// Puerta única de decisión. En esta fase la implementación es local; en la fase 3b
/// el cliente la resuelve por HTTP contra la API.
/// </summary>
public interface IDecisionEngine
{
    Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}
