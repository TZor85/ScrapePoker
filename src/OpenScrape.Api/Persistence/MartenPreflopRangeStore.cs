namespace OpenScrape.Api.Persistence;

using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Interfaces;


/// <summary>
/// Implementación de <see cref="IPreflopRangeStore"/> sobre Marten.
/// </summary>
public class MartenPreflopRangeStore(GetTable getTable) : IPreflopRangeStore
{
    public async Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default)
    {
        var result = await getTable.ExecuteAsync(situation);
        return result.IsSuccess ? result.Value : null;
    }
}
