using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services;

public sealed class NullDecisionTraceStore : IDecisionTraceStore
{
    public static NullDecisionTraceStore Instance { get; } = new();

    private NullDecisionTraceStore()
    {
    }

    public Task SaveAsync(DecisionTrace trace, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
