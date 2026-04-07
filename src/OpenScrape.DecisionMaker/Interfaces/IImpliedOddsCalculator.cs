using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el calculador de implied odds y reverse implied odds.
/// </summary>
public interface IImpliedOddsCalculator
{
    double CalculateImpliedOddsFactor(
        BoardPosition street, bool isInPosition, bool hasFlushDraw,
        decimal heroStack, decimal potSize, StrategyProfile profile,
        int numOpponents = 1);

    double CalculateReverseImpliedOdds(
        BoardChangeResult? boardChange, HandRank heroHandRank,
        bool hasFlushDraw, BoardPosition street, bool isFacingBet,
        StrategyProfile profile,
        PairClassification pairClassification = PairClassification.None,
        bool heroBlocksDangerSuit = false);
}
