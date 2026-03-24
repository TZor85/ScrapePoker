using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Calcula penalizaciones de equity por cartas peligrosas en el board.
/// Extraído de PostflopDecisionService para reducir responsabilidades.
/// </summary>
public class DangerPenaltyCalculator
{
    /// <summary>
    /// Calcula la penalización de equity por carta peligrosa en el board.
    /// Flush/straight usan penalización porcentual (proporcional a la equity).
    /// Facing bet multiplica la penalización (villano representa el draw completado).
    /// </summary>
    public static double Calculate(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        StrategyProfile profile)
    {
        if (boardChange.DangerLevel == 0)
            return 0;

        double penalty = 0;

        // Completaciones mayores: porcentual sobre equity.
        // Villano tiene UNA de las dos (flush o straight), no ambas → usar Math.Max.
        double flushCompletePenalty = boardChange.FlushCompleted
            ? rawEquity * (profile.DangerFlushCompletePct / 100.0)
            : 0;
        double straightCompletePenalty = boardChange.StraightCompleted
            ? rawEquity * (profile.DangerStraightCompletePct / 100.0)
            : 0;
        penalty += Math.Max(flushCompletePenalty, straightCompletePenalty);

        // Flush draw appeared (sin flush completado): flat penalty
        if (!boardChange.FlushCompleted && boardChange.FlushDrawAppeared)
            penalty += profile.DangerFlushDrawPenalty;

        // Cambios menores: flat
        if (boardChange.BoardPaired) penalty += profile.DangerBoardPairedPenalty;
        if (boardChange.OvercardAppeared) penalty += profile.DangerOvercardPenalty;

        // Facing bet → villano representando el draw completado
        if (isFacingBet)
            penalty *= profile.DangerFacingBetMultiplier;

        // Blocker effect
        if (heroBlocksDangerSuit)
            penalty *= profile.DangerHeroBlocksReduction;

        return penalty;
    }
}
