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

    // Check-raise: OOP con mano premium espera bet del villano para raise
    public bool CanCheckRaise { get; init; }
    public double CheckRaiseThreshold { get; init; } = 75.0;
    public string CheckRaiseBetSize { get; init; } = "Raise 3x";

    // Overbet: boards muy secos con ventaja de rango
    public bool CanOverbet { get; init; }
    public string OverbetBetSize { get; init; } = "Bet 1.25x Pot";
    public double OverbetMinEquity { get; init; } = 80.0;

    // Semi-bluff sizing agresivo: combo draws (12+ outs) en flop
    public string ComboDrawBetSize { get; init; } = "Bet 3/4";
    public int ComboDrawOutsThreshold { get; init; } = 12;

    // Probe bet: bet pequeño cuando agresor preflop checkeó en street anterior
    public bool CanProbeBet { get; init; }
    public string ProbeBetSize { get; init; } = "Bet 1/3";
    public double ProbeBetMinEquity { get; init; } = 25.0;

    // Modo simplificado (RaiseOverLimper: IP/OOP con bets fijos, sin board texture)
    public bool IsSimplified { get; init; }
    public string SimplifiedIPStrongBet { get; init; } = "Bet 1/2 (Value)";
    public string SimplifiedIPThinBet { get; init; } = "Bet 1/3 (Thin Value)";
    public string SimplifiedOOPStrongBet { get; init; } = "Bet 3/4 (Value)";
    public string SimplifiedOOPValueBet { get; init; } = "Bet 1/2 (Value)";
    public string SimplifiedOOPThinBet { get; init; } = "Bet 1/3 (Thin Value)";
}
