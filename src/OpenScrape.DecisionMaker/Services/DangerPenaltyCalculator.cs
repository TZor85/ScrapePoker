using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Calcula penalizaciones de equity por cartas peligrosas en el board.
/// Extraído de PostflopDecisionService para reducir responsabilidades.
/// </summary>
public class DangerPenaltyCalculator : Interfaces.IDangerPenaltyCalculator
{
    /// <summary>
    /// Wrapper de instancia que delega al método estático. Permite inyección via interfaz.
    /// </summary>
    double Interfaces.IDangerPenaltyCalculator.Calculate(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        StrategyProfile profile, BoardPosition street,
        bool heroHasNutBlocker, HandRank heroHandRank,
        bool heroCompletedFlush, bool heroCompletedStraight)
        => Calculate(rawEquity, boardChange, heroBlocksDangerSuit, isFacingBet, profile, street, heroHasNutBlocker, heroHandRank, heroCompletedFlush, heroCompletedStraight);

    /// <summary>
    /// Calcula la penalización de equity por carta peligrosa en el board.
    /// Flush/straight usan penalización porcentual escalada por street (flop más, river menos).
    /// Facing bet multiplica la penalización (villano representa el draw completado).
    /// Skip penalty si hero completó el draw (L3).
    /// Blocker adjustment en flush draw (L2).
    /// </summary>
    public static double Calculate(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        StrategyProfile profile, BoardPosition street = BoardPosition.Turn,
        bool heroHasNutBlocker = false,
        HandRank heroHandRank = HandRank.HighCard,
        bool heroCompletedFlush = false,
        bool heroCompletedStraight = false)
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
        // Skip si hero completó el draw (L3: no penalizar cuando hero se beneficia).
        double flushCompletePenalty = (boardChange.FlushCompleted && !heroCompletedFlush)
            ? rawEquity * (profile.DangerFlushCompletePct / 100.0) * streetDangerMultiplier
            : 0;
        double straightCompletePenalty = (boardChange.StraightCompleted && !heroCompletedStraight)
            ? rawEquity * (profile.DangerStraightCompletePct / 100.0) * streetDangerMultiplier
            : 0;
        penalty += Math.Max(flushCompletePenalty, straightCompletePenalty);

        // Flush draw en board (3 del mismo palo): villain solo necesita 1 carta para flush.
        // Penalty proporcional (como flush completado pero menor) en vez de flat.
        // ~40% de combos villain tienen al menos 1 carta del palo, pero no todos apuestan flush.
        if (!boardChange.FlushCompleted && boardChange.FlushDrawAppeared)
        {
            // Penalty proporcional: 8% de la equity (vs 35% de flush completado)
            double flushDrawPenalty = rawEquity * 0.08 * streetDangerMultiplier;
            // Reducir si hero tiene mano fuerte
            if (heroHandRank >= HandRank.TwoPair)
                flushDrawPenalty *= 0.5;
            else if (heroHandRank == HandRank.OnePair)
                flushDrawPenalty *= 0.75;

            // L2: Blocker adjustment para flush draw (antes solo aplicaba a flush completado)
            if (heroBlocksDangerSuit)
            {
                flushDrawPenalty *= heroHasNutBlocker
                    ? profile.DangerFlushDrawNutBlockerReduction
                    : profile.DangerFlushDrawNonNutBlockerReduction;
            }

            penalty += flushDrawPenalty;
        }

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
