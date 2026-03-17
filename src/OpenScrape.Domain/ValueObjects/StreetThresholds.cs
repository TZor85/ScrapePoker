namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Umbrales de equity y tamaños de apuesta por tier para una combinación de street + situación.
/// </summary>
public record StreetThresholds
{
    // Umbrales de equity
    public double FoldBelow { get; init; }
    public double ThinValueAbove { get; init; }
    public double ValueAbove { get; init; }
    public double StrongValueAbove { get; init; }

    // Bet sizes por tier de equity
    public string StrongValueBetSize { get; init; } = "Bet 3/4";
    public string ValueBetSize { get; init; } = "Bet 1/2";
    public string ThinValueBetSize { get; init; } = "Bet 1/3";

    // Bet sizes por textura de board (base bet antes de ajustes)
    public string DryBoardBetSize { get; init; } = "Bet 1/2";
    public string CoordinatedBoardBetSize { get; init; } = "Bet 1/2";
    public string PairedBoardBetSize { get; init; } = "Bet 3/4";

    // Comportamiento de bluff
    public bool CanBluff { get; init; }
    public double BluffFrequencyMultiplier { get; init; } = 1.0;
    public string BluffBetSize { get; init; } = "Bet 1/3";
    public string BluffCondition { get; init; } = "None"; // "None", "Always", "OOPOnly", "IPCoordinatedSmallOnly"

    // Accion por defecto cuando equity < FoldBelow y no se blufea
    public string LowEquityAction { get; init; } = "Fold"; // "Fold" o "Call"

    // Si thin value solo se apuesta IP (OOP hace check/fold o check/call)
    public bool ThinValueIPOnly { get; init; } = true;
    public string ThinValueOOPFallback { get; init; } = "CheckFold"; // "CheckFold" o "CheckCall"

    // Ajustes de sizing
    public bool ReduceSizeForLargeBet { get; init; } = true;
    public bool ReduceSizeForOOP { get; init; }

    // Modo simplificado (RaiseOverLimper: IP/OOP con bets fijos, sin board texture)
    public bool IsSimplified { get; init; }
    public string SimplifiedIPStrongBet { get; init; } = "Bet 1/2 (Value)";
    public string SimplifiedIPThinBet { get; init; } = "Bet 1/3 (Thin Value)";
    public string SimplifiedOOPStrongBet { get; init; } = "Bet 3/4 (Value)";
    public string SimplifiedOOPValueBet { get; init; } = "Bet 1/2 (Value)";
    public string SimplifiedOOPThinBet { get; init; } = "Bet 1/3 (Thin Value)";
}
