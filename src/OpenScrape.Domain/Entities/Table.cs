using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.Domain.Entities;

public class Table
{
    public required string Id { get; set; }
    public List<PlayerActionSequence>? Positions { get; set; }
}
