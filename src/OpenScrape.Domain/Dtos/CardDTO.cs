namespace OpenScrape.Domain.Dtos;

public class CardDTO
{
    public required string Name { get; set; }
    public string? ImageBase64 { get; set; }
    public string? BinaryValue { get; set; }
    public List<string>? Hall { get; set; }
    public int Force { get; set; }
    public int Suit { get; set; }

}
