using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.ValueObjects;

public record StreetDecision(
    BoardPosition Street,
    double EquityPercent,
    double PotOddsPercent,
    double ExpectedValue,
    string RecommendedAction,
    string ActionTaken,
    decimal PotSizeAtDecision,
    decimal BetSize,
    HandSituation Situation,
    bool IsInPosition,
    string? Reason = null,
    string? BoardTexture = null,
    int TotalOuts = 0,
    double SPR = 0
);
