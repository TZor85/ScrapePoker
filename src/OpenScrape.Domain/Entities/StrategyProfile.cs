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

    // Implied Odds (usado en PostflopDecisionService)
    // Factor que reduce las pot odds requeridas según SPR (< 1.0 = necesitas menos equity)
    public double ImpliedOddsSPRDeepFactor { get; set; } = 0.65;       // SPR > 4: alto implied odds
    public double ImpliedOddsSPRMediumFactor { get; set; } = 0.80;     // SPR 2-4: moderado
    public double ImpliedOddsSPRShallowFactor { get; set; } = 0.95;    // SPR < 2: casi sin implied odds
    public double ImpliedOddsSPRDeepThreshold { get; set; } = 4.0;     // Umbral para "deep"
    public double ImpliedOddsSPRShallowThreshold { get; set; } = 2.0;  // Umbral para "shallow"
    public double ImpliedOddsFlushDrawBonus { get; set; } = 0.90;      // Flush draws son más ocultos
    public double ImpliedOddsIPBonus { get; set; } = 0.92;             // IP controla tamaño del pote
    public double ImpliedOddsFlopMultiplier { get; set; } = 0.90;      // Flop: 2 calles por extraer valor
    public double ImpliedOddsTurnMultiplier { get; set; } = 0.95;      // Turn: 1 calle

    // C-bet y Range Advantage (usado en DetermineFlopActionUnified)
    // Bonus de equity cuando hero fue agresor preflop y tiene ventaja de rango
    public double CbetRangeAdvantageBonus { get; set; } = 8.0;
    // Bonus de equity base para c-bet como agresor preflop (incluso sin range advantage)
    public double CbetAggressorBonus { get; set; } = 4.0;
    // Penalización cuando hero es caller y el board favorece al raiser
    public double CbetCallerDisadvantage { get; set; } = -3.0;
    // Reducción del bonus en boards monotone (flush possible equaliza rangos)
    public double CbetMonotoneReduction { get; set; } = 0.5;
    // Reducción por cada oponente extra en multiway (c-bet menos efectivo multiway)
    public double CbetMultiwayReduction { get; set; } = 3.0;

    // Barrel Detection (usado en PostflopDecisionService)
    // Penalización cuando villano apuesta en 2 calles consecutivas (rango más estrecho)
    public double VillainBarrelFoldIncrease { get; set; } = 5.0;
    public double VillainBarrelThinValueIncrease { get; set; } = 3.0;

    // SPR Push/Fold (usado en PostflopDecisionService)
    // Con SPR corto, decisiones más binarias (commit o fold)
    public double SPRPushFoldThreshold { get; set; } = 2.0;
    public double SPRPushFoldFoldReduction { get; set; } = 8.0;
    public double SPRPushFoldValueIncrease { get; set; } = 10.0;
    public double SPRDeepCautionThreshold { get; set; } = 4.0;
    public double SPRDeepFoldIncrease { get; set; } = 3.0;

    // Reverse Implied Odds (penalización en turn al facing bet con mano vulnerable en board con draws)
    public double ReverseImpliedFlushDrawPenalty { get; set; } = 4.0;
    public double ReverseImpliedCoordinatedPenalty { get; set; } = 2.0;
    public double ReverseImpliedOnePairMultiplier { get; set; } = 1.5;

    // Combo Draw Bonus (usado en PostflopDecisionService)
    // Bonus de equity para combo draws (flush + straight draw) como semi-bluff premium
    public double ComboDrawEquityBonus { get; set; } = 6.0;

    // Tainted Outs (usado en OutsCalculator)
    // Descuento por out que también mejora la mano del villano (0.5 = vale la mitad)
    public double TaintedOutsDiscount { get; set; } = 0.5;

    // Decision Adjustments (usado en UnifiedPokerCalculator)
    public double DrawEquityBonus { get; set; } = 2.0;
    public double RiverEquityPenalty { get; set; } = -1.0;
    public double IPEquityBonus { get; set; } = 3.0;
    public double DeepStackEquityBonus { get; set; } = 1.0;
    public double BetEVThreshold { get; set; } = 5.0;
}
