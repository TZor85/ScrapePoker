namespace OpenScrape.DecisionMaker;

/// <summary>
/// Constantes algorítmicas puras del motor de póker.
/// Valores derivados de la teoría de póker, no configurables por estrategia.
/// </summary>
public static class PokerConstants
{
    // === HandEvaluator: multiplicadores de rango para scoring ===
    public const long HighCardMultiplier = 1;
    public const long PairMultiplier = 1_000_000;
    public const long TwoPairMultiplier = 10_000_000;
    public const long ThreeOfAKindMultiplier = 100_000_000;
    public const long StraightMultiplier = 1_000_000_000;
    public const long FlushMultiplier = 10_000_000_000;
    public const long FullHouseMultiplier = 100_000_000_000;
    public const long FourOfAKindMultiplier = 1_000_000_000_000;
    public const long StraightFlushMultiplier = 10_000_000_000_000;

    // === OutsCalculator: valores estándar de draws ===
    public const int FlushDrawOuts = 9;
    public const int BackdoorFlushImpliedOuts = 1;
    public const int BackdoorStraightImpliedOuts = 1;
    public const int OvercardOutsPerCard = 3;

    // === MonteCarloSimulator ===
    public const int DeckSize = 52;
    public const int DefaultMonteCarloIterations = 10_000;

    // === PostflopDecisionService: regla del 2 y del 4 ===
    public const double TurnOutsMultiplier = 2.17;
    public const double RiverOutsMultiplier = 4.35;

    // === PostflopDecisionService: umbrales de decisión ===
    public const int MinOutsForDraw = 8;
    public const double MarginalPotOddsFactor = 0.80;
    public const int MaxOpponentsForBluff = 2;

    // === PostflopDecisionService: penalizaciones por facing bet ===
    public const double FacingBetPenaltyLarge = 8.0;
    public const double FacingBetPenaltyMedium = 4.0;
    public const double FacingBetPenaltySmall = 1.0;
    public const double VillainAggressionPenalty = 3.0;

    // === PostflopDecisionService: escalado de facing bet penalty por street ===
    // Bets grandes en flop son normales (c-bets); en river representan rango fuerte
    public const double FacingBetTurnMultiplier = 1.15;
    public const double FacingBetRiverMultiplier = 1.30;

    // === PostflopDecisionService: ajustes multiway (IP puede aislar, OOP vulnerable) ===
    public const double MultiwayFoldBelowIP = 2.0;
    public const double MultiwayFoldBelowOOP = 6.0;
    public const double MultiwayThinValueIP = 2.0;
    public const double MultiwayThinValueOOP = 4.0;

    // === PostflopDecisionService: ajustes agresor vs caller ===
    public const double AggressorVsDonkFoldReduction = 5.0;
    public const double AggressorVsDonkThinValueReduction = 3.0;
    public const double CallerVsCbetFoldIncrease = 2.0;

    // === BoardTextureAnalyzer: pesos de wetness ===
    public const double WetnessMonotoneScore = 35.0;
    public const double WetnessTwoToneScore = 15.0;
    public const double WetnessConnectedScore = 20.0;
    public const double WetnessConnectedPerCount = 8.0;
    public const double WetnessFlushPossibilityScore = 15.0;
    public const double WetnessStraightPossibilityScore = 15.0;
    public const double WetnessBroadwayScore = 10.0;
    public const double WetnessPairedReduction = -10.0;
    public const double WetnessTripsReduction = -15.0;
    public const double WetnessExtraCardsBonus = 5.0;

    // === BoardTextureAnalyzer: umbrales de categorización ===
    public const double WetnessDryMax = 15.0;
    public const double WetnessSemiDryMax = 35.0;
    public const double WetnessSemiWetMax = 60.0;

    // === UnifiedPokerCalculator: kicker strength ===
    public const int StrongKickerMinRank = 13;
    public const int MediumKickerMinRank = 10;
    public const int DrawMinOuts = 8;
    public const double DrawEquityThreshold = 15.0;
}
