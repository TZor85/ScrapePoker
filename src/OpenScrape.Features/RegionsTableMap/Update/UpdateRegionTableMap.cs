using Ardalis.Result;
using Marten;

namespace OpenScrape.Features.RegionsTableMap.Update;

public class UpdateRegionTableMap
{
    readonly IDocumentStore _documentStore;

    public UpdateRegionTableMap(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<Result> ExecuteAsync(UpdateRegionTableMapRequest request, CancellationToken ct = default)
    {
        try
        {
            using var session = _documentStore.LightweightSession();
            var region = await session.LoadAsync<Domain.Entities.RegionTableMap>(request.Category, ct);
            if (region == null)
                return Result.NotFound();

            var regionToRemove = region.Regions?.FirstOrDefault(r => r.Name == request.Name);
            if (regionToRemove != null)
            {
                region.Regions?.Remove(regionToRemove);
            }
            
            var regionCategory = new Domain.ValueObjects.Region(
                request.Category,
                request.Name,
                request.PosX,
                request.PosY,
                request.Width,
                request.Height,
                regionToRemove?.IsHash,
                regionToRemove?.IsColor,
                regionToRemove?.IsBoard,
                request?.Color,
                regionToRemove?.IsOnlyNumber,
                request?.InactiveUmbral,
                request?.Umbral
            );

            region.Regions?.Add(regionCategory);

            session.Store(region);
            await session.SaveChangesAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.CriticalError(ex.Message);
        }
    }
}
