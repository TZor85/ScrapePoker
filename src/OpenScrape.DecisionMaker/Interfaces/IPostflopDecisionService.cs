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

    [Obsolete("Usar DetermineAction(PostflopDecisionInput) en su lugar")]
    PostflopDecisionResult DetermineAction(
        double equity, BoardPosition street, HandSituation situation,
        string boardTexture, bool isInPosition,
        BetSizeCategory villainBetSize,
        double potOdds = 0, int totalOuts = 0,
        bool previousStreetBet = false, bool villainShowedAggression = false,
        BoardChangeResult? boardChange = null, bool heroBlocksDangerSuit = false,
        decimal heroStack = 0, decimal potSize = 0,
        bool hasFlushDraw = false, int numOpponents = 1,
        bool heroIsAggressor = false, HandRank heroHandRank = HandRank.HighCard,
        bool hasComboDraw = false, bool villainAggressorCheckedPreviousStreet = false,
        bool villainBarreling = false, OpponentType villainType = OpponentType.Unknown,
        PairClassification pairClassification = PairClassification.None,
        double foldEquity = 0,
        BetSizeCategory villainBetSizeFlop = BetSizeCategory.NoBet,
        BetSizeCategory villainBetSizeTurn = BetSizeCategory.NoBet,
        bool villainCheckedMiddleStreet = false, bool heroHasNutBlocker = false,
        bool heroFloatedFlop = false, double villainFoldToBetPct = -1,
        KickerStrength heroKickerStrength = KickerStrength.None,
        bool turnCalledWithFlushDanger = false, bool heroBlocksTopCard = false,
        bool heroCheckedAllStreets = false, bool isAnyoneAllIn = false,
        bool isDonkBet = false, OpponentProfile? villainProfile = null,
        TablePosition heroPosition = TablePosition.None,
        TablePosition villainPosition = TablePosition.None,
        bool isBroadwayWet = false,
        double effectiveOuts = 0,
        RiverCardType riverCardType = RiverCardType.Neutral);
}
