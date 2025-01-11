using Marten;

namespace OpenScrape.Features.RegionsTableMap.GetAll;

public class GetAllRegionTableMap
{
    private IDocumentStore _store;

    public GetAllRegionTableMap(IDocumentStore store)
    {
        _store = store;
    }

    //public async Task<IEnumerable<RegionTableMap>> Execute()
    //{
    //    using var session = _store.LightweightSession();
    //    return await session.Query<RegionTableMap>().ToListAsync();
    //}

}
