using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de decisiones postflop.
/// </summary>
public interface IPostflopDecisionService
{
    StreetThresholds GetThresholds(BoardPosition street, HandSituation situation);

    double CalculateDangerPenalty(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        BoardPosition street = BoardPosition.Turn,
        bool heroHasNutBlocker = false,
        HandRank heroHandRank = HandRank.HighCard,
        bool heroCompletedFlush = false,
        bool heroCompletedStraight = false);

    double CalculateImpliedOddsFactor(
        BoardPosition street, bool isInPosition, bool hasFlushDraw,
        decimal heroStack = 0, decimal potSize = 0, int numOpponents = 1);

    double CalculateReverseImpliedOdds(
        BoardChangeResult? boardChange, HandRank heroHandRank,
        bool hasFlushDraw, BoardPosition street, bool isFacingBet,
        PairClassification pairClassification = PairClassification.None,
        bool heroBlocksDangerSuit = false);

    PostflopDecisionResult DetermineAction(PostflopDecisionInput input);
}
