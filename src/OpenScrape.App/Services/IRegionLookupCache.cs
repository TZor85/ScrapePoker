using OpenScrape.Domain.Entities;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Services;

/// <summary>
/// Contrato mínimo para RegionLookupCache — solo expone lo que TableReaderService necesita.
/// </summary>
public interface IRegionLookupCache
{
    DomainRegion? GetRegion(string mapId, string regionName);
    List<DomainRegion>? GetRegions(string mapId);
}
