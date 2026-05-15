using Marten;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Services;

public sealed class MartenDecisionTraceStore : IDecisionTraceStore
{
    private readonly IDocumentStore _store;

    public MartenDecisionTraceStore(IDocumentStore store)
    {
        _store = store;
    }

    public async Task SaveAsync(DecisionTrace trace, CancellationToken cancellationToken = default)
    {
        await using var session = _store.LightweightSession();
        session.Store(trace);
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
