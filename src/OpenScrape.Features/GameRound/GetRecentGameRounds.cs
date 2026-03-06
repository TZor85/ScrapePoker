using Marten;

namespace OpenScrape.Features.GameRound;

public class GetRecentGameRounds
{
    private readonly IDocumentStore _documentStore;

    public GetRecentGameRounds(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<Domain.Entities.GameRound>> Execute(int count = 50)
    {
        await using var session = _documentStore.QuerySession();
        var results = await session.Query<Domain.Entities.GameRound>()
            .OrderByDescending(r => r.Timestamp)
            .Take(count)
            .ToListAsync();
        return results.ToList();
    }
}
