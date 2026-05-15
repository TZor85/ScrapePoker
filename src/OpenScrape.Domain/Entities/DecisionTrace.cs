using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.Entities;

/// <summary>
/// Auditoría persistible de una decisión emitida por el motor postflop.
/// </summary>
public sealed class DecisionTrace
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public BoardPosition Street { get; set; }

    public HandSituation Situation { get; set; }

    public string? HandSituationTag { get; set; }

    public List<string> HeroCards { get; set; } = new();

    public List<string> CommunityCards { get; set; } = new();

    public string? VillainId { get; set; }

    public TablePosition HeroPosition { get; set; }

    public TablePosition VillainPosition { get; set; }

    public bool IsInPosition { get; set; }

    public int NumOpponents { get; set; }

    public decimal HeroStack { get; set; }

    public decimal VillainStack { get; set; }

    public decimal PotSize { get; set; }

    public decimal BetToCall { get; set; }

    public string RecommendedAction { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string? BoardTexture { get; set; }

    public double? BetSize { get; set; }

    public double EquityPercent { get; set; }

    public double PotOddsPercent { get; set; }

    public double ExpectedValue { get; set; }

    public int TotalOuts { get; set; }

    public bool IsBluff { get; set; }

    public bool IsBarrel { get; set; }

    public bool IsCheckRaise { get; set; }

    public bool IsFloating { get; set; }

    public string? HeroHandRank { get; set; }

    public string? HeroKickerStrength { get; set; }

    public string? PairClassification { get; set; }

    public bool HasComboDraw { get; set; }

    public double FoldEquity { get; set; }
}
