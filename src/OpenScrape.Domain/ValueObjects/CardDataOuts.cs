using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.ValueObjects;

public class CardDataOuts
{
    public required Suit Suit { get; set; }
    public required Rank Rank { get; set; }
    public string Id => $"{Rank}_{Suit}";

}

public class DrawProbability
{
    public List<CardDataOuts> Outs { get; set; } = new List<CardDataOuts>();
    public double Probability { get; set; } // En porcentaje (ej: 34.97)
    public string ProbabilityDescription => $"{Probability:F2}%"; // Ej: "34.97%"
}
