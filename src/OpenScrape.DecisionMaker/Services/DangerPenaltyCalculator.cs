using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Calcula penalizaciones de equity por cartas peligrosas en el board.
/// Extraído de PostflopDecisionService para reducir responsabilidades.
/// </summary>
public class DangerPenaltyCalculator
{
    /// <summary>
    /// Calcula la penalización de equity por carta peligrosa en el board.
    /// Flush/straight usan penalización porcentual escalada por street (flop más, river menos).
    /// Facing bet multiplica la penalización (villano representa el draw completado).
    /// </summary>
    public static double Calculate(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        StrategyProfile profile, BoardPosition street = BoardPosition.Turn,
        bool heroHasNutBlocker = false)
    {
        if (boardChange.DangerLevel == 0)
            return 0;

        double penalty = 0;

        // Escalado por street: flop más riesgo (2 calles por venir), river menos (definitivo)
        double streetDangerMultiplier = street switch
        {
            BoardPosition.Flop => profile.DangerPenaltyFlopMultiplier,
            BoardPosition.River => profile.DangerPenaltyRiverMultiplier,
            _ => 1.0
        };

        // Completaciones mayores: porcentual sobre equity × multiplicador de street.
        // Villano tiene UNA de las dos (flush o straight), no ambas → usar Math.Max.
        double flushCompletePenalty = boardChange.FlushCompleted
            ? rawEquity * (profile.DangerFlushCompletePct / 100.0) * streetDangerMultiplier
            : 0;
        double straightCompletePenalty = boardChange.StraightCompleted
            ? rawEquity * (profile.DangerStraightCompletePct / 100.0) * streetDangerMultiplier
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

        // Blocker effect granular: nut blocker > non-nut > board 4+ flush
        if (heroBlocksDangerSuit)
        {
            bool isBoard4Flush = boardChange.FlushCompleted && boardChange.DangerLevel >= 4;
            double blockerReduction;
            if (isBoard4Flush)
                blockerReduction = profile.DangerBlockerBoard4FlushReduction;
            else if (heroHasNutBlocker)
                blockerReduction = profile.DangerNutBlockerReduction;
            else
                blockerReduction = profile.DangerNonNutBlockerReduction;
            penalty *= blockerReduction;
        }

        return penalty;
    }
}
