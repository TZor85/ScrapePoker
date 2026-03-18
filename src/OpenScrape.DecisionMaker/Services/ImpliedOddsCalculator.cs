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
        StrategyProfile profile)
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
            sprFactor = profile.ImpliedOddsSPRShallowFactor +
                (position * (profile.ImpliedOddsSPRDeepFactor - profile.ImpliedOddsSPRShallowFactor));
        }

        double streetFactor = street == BoardPosition.Turn
            ? profile.ImpliedOddsTurnMultiplier
            : profile.ImpliedOddsFlopMultiplier;
        sprFactor *= streetFactor;

        if (isInPosition)
            sprFactor *= profile.ImpliedOddsIPBonus;

        if (hasFlushDraw)
            sprFactor *= profile.ImpliedOddsFlushDrawBonus;

        return Math.Max(0.50, Math.Min(1.0, sprFactor));
    }

    /// <summary>
    /// Calcula penalización por reverse implied odds.
    /// Solo en turn facing bet con mano vulnerable (OnePair/TwoPair) en board con draws.
    /// </summary>
    public static double CalculateReverseImpliedOdds(
        BoardChangeResult? boardChange, HandRank heroHandRank, bool hasFlushDraw,
        BoardPosition street, bool isFacingBet,
        StrategyProfile profile)
    {
        if (street != BoardPosition.Turn || !isFacingBet || boardChange == null)
            return 0;

        if (heroHandRank > HandRank.TwoPair)
            return 0;

        double penalty = 0;

        if (boardChange.FlushDrawAppeared && !hasFlushDraw)
            penalty += profile.ReverseImpliedFlushDrawPenalty;

        if (boardChange.DangerLevel >= 2 && !boardChange.FlushCompleted)
            penalty += profile.ReverseImpliedCoordinatedPenalty;

        if (heroHandRank == HandRank.OnePair)
            penalty *= profile.ReverseImpliedOnePairMultiplier;

        return penalty;
    }
}
