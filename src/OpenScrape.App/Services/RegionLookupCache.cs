using OpenScrape.Domain.Entities;
using Region = OpenScrape.Domain.ValueObjects.Region;

namespace OpenScrape.App.Services;

/// <summary>
/// Cache pre-computado de regiones con lookup O(1) por mapId y regionName.
/// Reemplaza las búsquedas O(n) con FirstOrDefault sobre _regionsTableMap.
/// </summary>
public class RegionLookupCache
{
    private Dictionary<string, Dictionary<string, Region>> _regionsByMapAndName = new();
    private Dictionary<string, List<Region>> _regionsByMap = new();

    /// <summary>
    /// Construye los diccionarios de lookup a partir de la lista de RegionTableMap.
    /// Debe llamarse cada vez que se carga o cambia el mapa de regiones.
    /// </summary>
    public void Initialize(IEnumerable<RegionTableMap> maps)
    {
        var byMapAndName = new Dictionary<string, Dictionary<string, Region>>(StringComparer.OrdinalIgnoreCase);
        var byMap = new Dictionary<string, List<Region>>(StringComparer.OrdinalIgnoreCase);

        foreach (var map in maps)
        {
            if (map.Regions == null || map.Regions.Count == 0)
                continue;

            var nameDict = new Dictionary<string, Region>(map.Regions.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var region in map.Regions)
            {
                nameDict.TryAdd(region.Name, region);
            }

            byMapAndName[map.Id] = nameDict;
            byMap[map.Id] = map.Regions;
        }

        _regionsByMapAndName = byMapAndName;
        _regionsByMap = byMap;
    }

    /// <summary>
    /// Obtiene una región específica por mapId y nombre. O(1).
    /// </summary>
    public Region? GetRegion(string mapId, string regionName)
    {
        if (_regionsByMapAndName.TryGetValue(mapId, out var nameDict) &&
            nameDict.TryGetValue(regionName, out var region))
        {
            return region;
        }
        return null;
    }

    /// <summary>
    /// Obtiene todas las regiones de un mapa. O(1).
    /// </summary>
    public List<Region>? GetRegions(string mapId)
    {
        return _regionsByMap.TryGetValue(mapId, out var regions) ? regions : null;
    }
}
