using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el calculador de penalizaciones por cartas peligrosas.
/// </summary>
public interface IDangerPenaltyCalculator
{
    double Calculate(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        StrategyProfile profile, BoardPosition street = BoardPosition.Turn,
        bool heroHasNutBlocker = false,
        HandRank heroHandRank = HandRank.HighCard,
        bool heroCompletedFlush = false,
        bool heroCompletedStraight = false);
}
