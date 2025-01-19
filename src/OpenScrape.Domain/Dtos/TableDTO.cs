using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.Domain.Dtos;

public class TableDTO
{
    public required string Name { get; set; }
    public List<PlayerActionSequence>? Positions { get; set; }
}
