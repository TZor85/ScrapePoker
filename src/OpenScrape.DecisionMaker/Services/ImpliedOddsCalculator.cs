using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Calcula el factor de implied odds y reverse implied odds.
/// Extraído de PostflopDecisionService para reducir responsabilidades.
/// </summary>
public class ImpliedOddsCalculator
{
    /// <summary>
    /// Calcula el factor de implied odds basado en SPR, posición, street y tipo de draw.
    /// Retorna un valor entre 0.5 y 1.0: menor = mejores implied odds.
    /// En river no hay implied odds (no hay más calles).
    /// </summary>
    public static double CalculateImpliedOddsFactor(
        BoardPosition street,
        bool isInPosition,
        bool hasFlushDraw,
        decimal heroStack,
        decimal potSize,
        StrategyProfile profile,
        int numOpponents = 1)
    {
        if (street == BoardPosition.River)
            return 1.0;

        if (heroStack <= 0 || potSize <= 0)
            return 1.0;

        double spr = (double)(heroStack / potSize);
        double sprFactor;
        if (spr >= profile.ImpliedOddsSPRDeepThreshold)
            sprFactor = profile.ImpliedOddsSPRDeepFactor;
        else if (spr <= profile.ImpliedOddsSPRShallowThreshold)
            sprFactor = profile.ImpliedOddsSPRShallowFactor;
        else
        {
            double range = profile.ImpliedOddsSPRDeepThreshold - profile.ImpliedOddsSPRShallowThreshold;
            double position = (spr - profile.ImpliedOddsSPRShallowThreshold) / range;
            // Interpolación cuadrática: implied odds crecen más rápido acercándose a deep
            double curvedPosition = Math.Sqrt(position);
            sprFactor = profile.ImpliedOddsSPRShallowFactor +
                (curvedPosition * (profile.ImpliedOddsSPRDeepFactor - profile.ImpliedOddsSPRShallowFactor));
        }

        double streetFactor = street == BoardPosition.Turn
            ? profile.ImpliedOddsTurnMultiplier
            : profile.ImpliedOddsFlopMultiplier;
        sprFactor *= streetFactor;

        if (isInPosition)
            sprFactor *= profile.ImpliedOddsIPBonus;

        if (hasFlushDraw)
            sprFactor *= profile.ImpliedOddsFlushDrawBonus;

        // Multiway: OOP implied odds peores (villain detrás puede raise)
        // IP con draw: implied odds mejores (más gente que pagar)
        if (numOpponents >= 2)
        {
            if (!isInPosition)
                sprFactor *= 1.0 + 0.05 * (numOpponents - 1);
            else if (hasFlushDraw)
                sprFactor *= 1.0 - 0.03 * (numOpponents - 1);
        }

        return Math.Max(0.50, Math.Min(1.0, sprFactor));
    }

    /// <summary>
    /// Calcula penalización por reverse implied odds.
    /// Turn y river facing bet con mano vulnerable (OnePair/TwoPair) en board con draws.
    /// River aplica penalización reducida (×0.6) porque ya no hay más cartas peligrosas,
    /// pero el villano puede representar draws completados en el river card.
    /// Cuando pairClassification está disponible, el multiplicador de OnePair varía por sub-tipo:
    ///   Overpair (×1.0) → MiddlePair (×1.5) → BottomPair (×1.8) → BoardPaired (×2.0)
    /// </summary>
    public static double CalculateReverseImpliedOdds(
        BoardChangeResult? boardChange, HandRank heroHandRank, bool hasFlushDraw,
        BoardPosition street, bool isFacingBet,
        StrategyProfile profile,
        PairClassification pairClassification = PairClassification.None,
        bool heroBlocksDangerSuit = false)
    {
        if ((street != BoardPosition.Turn && street != BoardPosition.River) || !isFacingBet || boardChange == null)
            return 0;

        if (heroHandRank > HandRank.TwoPair)
            return 0;

        double penalty = 0;

        if (boardChange.FlushDrawAppeared && !hasFlushDraw)
            penalty += profile.ReverseImpliedFlushDrawPenalty;

        if (boardChange.DangerLevel >= 2 && !boardChange.FlushCompleted)
            penalty += profile.ReverseImpliedCoordinatedPenalty;

        if (heroHandRank == HandRank.OnePair)
        {
            // Multiplicador diferenciado por sub-tipo de par
            double multiplier = pairClassification switch
            {
                PairClassification.Overpair => 1.0,
                PairClassification.TopPair => 1.2,
                PairClassification.MiddlePair => 1.5,
                PairClassification.PocketPairUnder => 1.5,
                PairClassification.BottomPair => 1.8,
                PairClassification.BoardPaired => 2.0,
                _ => profile.ReverseImpliedOnePairMultiplier
            };
            penalty *= multiplier;
        }

        // Hero bloquea el draw del villano → reduce penalización
        if (heroBlocksDangerSuit && penalty > 0)
            penalty *= profile.ReverseImpliedBlockerReduction;

        // River: penalización reducida (no hay más cartas por venir)
        if (street == BoardPosition.River)
            penalty *= 0.6;

        return penalty;
    }
}
