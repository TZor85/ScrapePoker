using Marten;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Services;

public sealed class MartenOpponentProfileStore : IOpponentProfileStore
{
    private readonly IDocumentStore _store;

    public MartenOpponentProfileStore(IDocumentStore store)
    {
        _store = store;
    }

    public OpponentProfile? Load(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return null;

        using var session = _store.QuerySession();
        return session.LoadAsync<OpponentProfile>(playerId).GetAwaiter().GetResult();
    }

    public void Save(OpponentProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.PlayerId))
            return;

        using var session = _store.LightweightSession();
        session.Store(profile);
        session.SaveChangesAsync().GetAwaiter().GetResult();
    }
}
