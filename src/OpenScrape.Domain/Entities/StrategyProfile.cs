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
    public double TurnBluffFrequency { get; set; } = 0.12;
    public double RiverBluffFrequency { get; set; } = 0.10;

    // C-Bet Frequencies (agresor preflop que apuesta por continuación — distinto de bluff puro)
    public double CbetFrequencyFlop { get; set; } = 0.65;
    public double CbetFrequencyTurn { get; set; } = 0.45;
    public double CbetFrequencyRiver { get; set; } = 0.30;

    // Kicker quality adjustment en facing bet (TPTK vs TPWK)
    public double KickerStrongEquityBonus { get; set; } = 3.0;
    public double KickerWeakEquityPenalty { get; set; } = 2.0;

    // Bluff frequency modulada por SPR (usado en HandleLowEquity)
    public double BluffSPRShortThreshold { get; set; } = 2.0;
    public double BluffSPRShortMultiplier { get; set; } = 0.5;
    public double BluffSPRDeepThreshold { get; set; } = 4.0;
    public double BluffSPRDeepMultiplier { get; set; } = 1.2;

    // Danger Card Penalties (usado en PostflopDecisionService)
    // Porcentual: reduce equity un X% cuando se completa draw (ej: 25 = -25% de equity)
    public double DangerFlushCompletePct { get; set; } = 35.0;
    public double DangerStraightCompletePct { get; set; } = 18.0;
    // Flat: penalización fija en puntos de equity
    public double DangerBoardPairedPenalty { get; set; } = 5.0;
    public double DangerOvercardPenalty { get; set; } = 3.0;
    public double DangerFlushDrawPenalty { get; set; } = 5.0;
    // Multiplicadores
    public double DangerFacingBetMultiplier { get; set; } = 1.4;
    public double DangerHeroBlocksReduction { get; set; } = 0.5;
    // Blocker granular: nut blocker (As del palo) elimina más combos que non-nut
    public double DangerNutBlockerReduction { get; set; } = 0.35;
    public double DangerNonNutBlockerReduction { get; set; } = 0.55;
    // Board con 4+ cartas del palo: flush casi segura, blocker menos relevante
    public double DangerBlockerBoard4FlushReduction { get; set; } = 0.7;
    // Escalado de danger penalty por street: flop más riesgo (2 calles por venir), river menos (definitivo)
    public double DangerPenaltyFlopMultiplier { get; set; } = 1.3;
    public double DangerPenaltyRiverMultiplier { get; set; } = 0.8;
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
    // Sizing tell: penalización cuando villano escala tamaño de apuesta entre streets
    public double VillainSizingEscalationPenalty { get; set; } = 4.0;
    // Bet-check-bet: penalty menor que barrel (draw fallido reintentando)
    public double VillainBetCheckBetPenalty { get; set; } = 2.0;

    // SPR Push/Fold (usado en PostflopDecisionService)
    // Con SPR corto, decisiones más binarias (commit o fold)
    public double SPRPushFoldThreshold { get; set; } = 2.0;
    public double SPRPushFoldFoldReduction { get; set; } = 8.0;
    public double SPRPushFoldValueIncrease { get; set; } = 10.0;
    public double SPRDeepCautionThreshold { get; set; } = 4.0;
    public double SPRDeepFoldIncrease { get; set; } = 3.0;

    // Multiway street multipliers (turn/river más peligroso en multiway)
    public double MultiwayStreetMultiplierTurn { get; set; } = 1.2;
    public double MultiwayStreetMultiplierRiver { get; set; } = 1.4;

    // 3-Bet/4-Bet pot postflop adjustments (rango villano más estrecho)
    public double ThreeBetPostflopFoldIncrease { get; set; } = 5.0;
    public double ThreeBetPostflopValueIncrease { get; set; } = 3.0;
    public double FourBetPostflopFoldIncrease { get; set; } = 8.0;
    public double FourBetPostflopValueIncrease { get; set; } = 5.0;

    // Check-raise SPR guard (no check-raise cuando SPR compromete el stack)
    public double CheckRaiseSPRMinThreshold { get; set; } = 1.5;
    public double CheckRaiseLowSPRMinEquity { get; set; } = 60.0;

    // Reverse Implied Odds (penalización en turn al facing bet con mano vulnerable en board con draws)
    public double ReverseImpliedFlushDrawPenalty { get; set; } = 7.0;
    public double ReverseImpliedCoordinatedPenalty { get; set; } = 4.0;
    public double ReverseImpliedOnePairMultiplier { get; set; } = 1.5;
    // Reducción de reverse implied odds cuando hero bloquea el palo del draw
    public double ReverseImpliedBlockerReduction { get; set; } = 0.5;

    // Bluff Catching (usado en PostflopDecisionService)
    // Multiplicador sobre FoldBelow: equity >= FoldBelow * multiplier → call para atrapar bluffs
    public double BluffCatchFoldBelowMultiplier { get; set; } = 0.75;
    // Turn: umbral más estricto que river (más riesgo con 1 calle por venir)
    public double BluffCatchTurnEquityMultiplier { get; set; } = 0.90;

    // Combo Draw Bonus (usado en PostflopDecisionService)
    // Bonus de equity para combo draws (flush + straight draw) como semi-bluff premium
    public double ComboDrawEquityBonus { get; set; } = 6.0;

    // Tainted Outs (usado en OutsCalculator)
    // Descuento por out que también mejora la mano del villano (0.5 = vale la mitad)
    public double TaintedOutsDiscount { get; set; } = 0.5;
    // Descuento variable: hero con flush draw (mejora más) → 0.7; sin flush draw → 0.3
    public double TaintedOutsDiscountHeroStrong { get; set; } = 0.7;
    public double TaintedOutsDiscountHeroWeak { get; set; } = 0.3;

    // Floating IP (call con posición para robar en turn)
    public double FloatingIPMinEquity { get; set; } = 25.0;
    public double FloatingIPMaxEquity { get; set; } = 35.0;
    // Outs mínimos para considerar draw real sin flush/combo draw (evitar floats con overcards)
    public int FloatingIPMinOuts { get; set; } = 6;

    // Slow Play (check con nuts en board seco para inducir bluff)
    public double SlowPlayMinEquity { get; set; } = 72.0;

    // Check-raise con draws fuertes: equity mínima para check-raise semi-bluff en flop OOP
    public double CheckRaiseDrawMinEquity { get; set; } = 40.0;

    // Board Paired c-bet reduction (reducir c-bet frequency en boards paired)
    public double BoardPairedCbetReduction { get; set; } = 8.0;

    // River opportunity (hero completó draw → bet for value)
    public double RiverCompletedDrawBonus { get; set; } = 10.0;

    // Decision Adjustments (usado en UnifiedPokerCalculator)
    public double DrawEquityBonus { get; set; } = 2.0;
    public double RiverEquityPenalty { get; set; } = -1.0;
    public double IPEquityBonus { get; set; } = 3.0;
    public double DeepStackEquityBonus { get; set; } = 1.0;
    public double BetEVThreshold { get; set; } = 5.0;

    /// <summary>
    /// Valida que los parámetros del perfil sean consistentes.
    /// Retorna lista de errores encontrados (vacía si todo es correcto).
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        // Rango de equity: FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove
        foreach (var (key, t) in Thresholds)
        {
            if (t.FoldBelow >= t.ThinValueAbove)
                errors.Add($"{key}: FoldBelow ({t.FoldBelow}) debe ser menor que ThinValueAbove ({t.ThinValueAbove})");
            if (t.ThinValueAbove >= t.ValueAbove)
                errors.Add($"{key}: ThinValueAbove ({t.ThinValueAbove}) debe ser menor que ValueAbove ({t.ValueAbove})");
            if (t.ValueAbove >= t.StrongValueAbove)
                errors.Add($"{key}: ValueAbove ({t.ValueAbove}) debe ser menor que StrongValueAbove ({t.StrongValueAbove})");
        }

        // Fold equity en rango razonable
        if (FoldEquityMin >= FoldEquityMax)
            errors.Add($"FoldEquityMin ({FoldEquityMin}) debe ser menor que FoldEquityMax ({FoldEquityMax})");

        // SPR thresholds
        if (SPRPushFoldThreshold >= SPRDeepCautionThreshold)
            errors.Add($"SPRPushFoldThreshold ({SPRPushFoldThreshold}) debe ser menor que SPRDeepCautionThreshold ({SPRDeepCautionThreshold})");

        // Danger penalties: no negativos y porcentuales <= 100
        if (DangerFlushCompletePct < 0 || DangerFlushCompletePct > 100)
            errors.Add($"DangerFlushCompletePct ({DangerFlushCompletePct}) debe estar entre 0 y 100");
        if (DangerStraightCompletePct < 0 || DangerStraightCompletePct > 100)
            errors.Add($"DangerStraightCompletePct ({DangerStraightCompletePct}) debe estar entre 0 y 100");

        // TaintedOutsDiscount en rango [0, 1]
        if (TaintedOutsDiscount < 0 || TaintedOutsDiscount > 1)
            errors.Add($"TaintedOutsDiscount ({TaintedOutsDiscount}) debe estar entre 0 y 1");

        // Bluff frequencies en rango [0, 1]
        if (FlopBluffFrequency < 0 || FlopBluffFrequency > 1)
            errors.Add($"FlopBluffFrequency ({FlopBluffFrequency}) debe estar entre 0 y 1");
        if (TurnBluffFrequency < 0 || TurnBluffFrequency > 1)
            errors.Add($"TurnBluffFrequency ({TurnBluffFrequency}) debe estar entre 0 y 1");
        if (RiverBluffFrequency < 0 || RiverBluffFrequency > 1)
            errors.Add($"RiverBluffFrequency ({RiverBluffFrequency}) debe estar entre 0 y 1");

        // BluffCatch multipliers en rango (0, 1]
        if (BluffCatchFoldBelowMultiplier <= 0 || BluffCatchFoldBelowMultiplier > 1)
            errors.Add($"BluffCatchFoldBelowMultiplier ({BluffCatchFoldBelowMultiplier}) debe estar entre 0 (excl.) y 1");
        if (BluffCatchTurnEquityMultiplier <= 0 || BluffCatchTurnEquityMultiplier > 1)
            errors.Add($"BluffCatchTurnEquityMultiplier ({BluffCatchTurnEquityMultiplier}) debe estar entre 0 (excl.) y 1");

        // Bet sizing multipliers deben ser > 0
        if (BetSizingSPRDeepMultiplier <= 0)
            errors.Add($"BetSizingSPRDeepMultiplier ({BetSizingSPRDeepMultiplier}) debe ser > 0");
        if (BetSizingSPRShallowMultiplier <= 0)
            errors.Add($"BetSizingSPRShallowMultiplier ({BetSizingSPRShallowMultiplier}) debe ser > 0");

        // Implied odds factors en rango (0, 1]
        if (ImpliedOddsSPRDeepFactor <= 0 || ImpliedOddsSPRDeepFactor > 1)
            errors.Add($"ImpliedOddsSPRDeepFactor ({ImpliedOddsSPRDeepFactor}) debe estar entre 0 (excl.) y 1");
        if (ImpliedOddsSPRShallowFactor <= 0 || ImpliedOddsSPRShallowFactor > 1)
            errors.Add($"ImpliedOddsSPRShallowFactor ({ImpliedOddsSPRShallowFactor}) debe estar entre 0 (excl.) y 1");

        // Implied odds SPR thresholds coherentes
        if (ImpliedOddsSPRShallowThreshold >= ImpliedOddsSPRDeepThreshold)
            errors.Add($"ImpliedOddsSPRShallowThreshold ({ImpliedOddsSPRShallowThreshold}) debe ser menor que Deep ({ImpliedOddsSPRDeepThreshold})");

        return errors;
    }
}
