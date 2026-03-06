using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.Domain.Entities;

/// <summary>
/// Perfil de estrategia que centraliza todos los thresholds y parámetros configurables.
/// Clave del diccionario: "{Street}_{HandSituation}" (ej: "Turn_OpenRaise", "River_Call").
/// </summary>
public class StrategyProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Umbrales de equity por Street x HandSituation
    public Dictionary<string, StreetThresholds> Thresholds { get; set; } = new();

    // Fold Equity (usado en UnifiedPokerCalculator)
    public double FoldEquityBase { get; set; } = 20.0;
    public double FoldEquityFlopBonus { get; set; } = 5.0;
    public double FoldEquityRiverPenalty { get; set; } = -5.0;
    public double FoldEquityIPBonus { get; set; } = 10.0;
    public double FoldEquityThreeBetPenalty { get; set; } = -10.0;
    public double FoldEquityMin { get; set; } = 5.0;
    public double FoldEquityMax { get; set; } = 60.0;

    // Bet Sizing Adjustments (usado en BetSizingService)
    public double BetSizingSPRDeepMultiplier { get; set; } = 1.25;
    public double BetSizingSPRDeepThreshold { get; set; } = 3.0;
    public double BetSizingSPRShallowMultiplier { get; set; } = 0.75;
    public double BetSizingSPRShallowThreshold { get; set; } = 1.0;
    public double BetSizingPairedMultiplier { get; set; } = 1.15;
    public double BetSizingCoordinatedMultiplier { get; set; } = 0.90;
    public double BetSizingOOPMultiplier { get; set; } = 0.90;
    public double BetSizingMultiOpponentMultiplier { get; set; } = 0.85;

    // Bluff Frequencies
    public double FlopBluffFrequency { get; set; } = 0.15;
    public double TurnBluffFrequency { get; set; } = 0.15;
    public double RiverBluffFrequency { get; set; } = 0.10;

    // Danger Card Penalties (usado en PostflopDecisionService)
    // Porcentual: reduce equity un X% cuando se completa draw (ej: 25 = -25% de equity)
    public double DangerFlushCompletePct { get; set; } = 25.0;
    public double DangerStraightCompletePct { get; set; } = 18.0;
    // Flat: penalización fija en puntos de equity
    public double DangerBoardPairedPenalty { get; set; } = 5.0;
    public double DangerOvercardPenalty { get; set; } = 3.0;
    public double DangerFlushDrawPenalty { get; set; } = 5.0;
    // Multiplicadores
    public double DangerFacingBetMultiplier { get; set; } = 1.4;
    public double DangerHeroBlocksReduction { get; set; } = 0.5;
    // Tope de equity para APOSTAR cuando flush/straight completado y hero no lo tiene
    // (apostar solo consigue que nos paguen manos que nos ganan)
    public double DangerCompletedDrawNoBetCap { get; set; } = 45.0;

    // Decision Adjustments (usado en UnifiedPokerCalculator)
    public double DrawEquityBonus { get; set; } = 2.0;
    public double RiverEquityPenalty { get; set; } = -1.0;
    public double IPEquityBonus { get; set; } = 3.0;
    public double DeepStackEquityBonus { get; set; } = 1.0;
    public double BetEVThreshold { get; set; } = 5.0;
}
