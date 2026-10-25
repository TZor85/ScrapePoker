using OpenScrape.Contracts;

namespace OpenScrape.App.Services;

/// <summary>
/// Puerta única de decisión del cliente. La única implementación es
/// <see cref="Http.HttpDecisionEngine"/>: el motor vive fuera del binario.
/// </summary>
public interface IDecisionEngine
{
    Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}
