using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services;

public interface IDecisionTraceStore
{
    Task SaveAsync(DecisionTrace trace, CancellationToken cancellationToken = default);
}
