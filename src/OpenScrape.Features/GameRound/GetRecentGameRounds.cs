using Marten;
using OpenScrape.Domain.Entities;

namespace OpenScrape.Features.GameRound;

public class GetRecentGameRounds
{
    private readonly IDocumentStore _documentStore;

    public GetRecentGameRounds(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<GameSession>> Execute(int count = 20)
    {
        await using var session = _documentStore.QuerySession();
        var results = await session.Query<GameSession>()
            .OrderByDescending(s => s.EndTime)
            .Take(count)
            .ToListAsync();
        return results.ToList();
    }
}
