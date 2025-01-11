namespace OpenScrape.Domain.Entities;

public class RegionTableMap
{
    public required string Id { get; set; }
    public List<ValueObjects.Region>? Regions { get; set; }
}
