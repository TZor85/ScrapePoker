using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// Helper compartido para tests que antes pasaban ~40 parámetros posicionales
/// al overload obsoleto de <c>PostflopDecisionService.DetermineAction</c>.
/// Mantiene el mismo nombre y orden de parámetros que el overload eliminado
/// (con defaults idénticos) para permitir migración mecánica: envolver la
/// invocación anterior en <c>MakeInput(...)</c>.
/// </summary>
internal static class TestMakeInputHelper
{
    public static PostflopDecisionInput MakeInput(
        double equity,
        BoardPosition street,
        HandSituation situation,
        string boardTexture,
        bool isInPosition,
        BetSizeCategory villainBetSize,
        double potOdds = 0,
        int totalOuts = 0,
        bool previousStreetBet = false,
        bool villainShowedAggression = false,
        BoardChangeResult? boardChange = null,
        bool heroBlocksDangerSuit = false,
        decimal heroStack = 0,
        decimal potSize = 0,
        bool hasFlushDraw = false,
        int numOpponents = 1,
        bool heroIsAggressor = false,
        HandRank heroHandRank = HandRank.HighCard,
        bool hasComboDraw = false,
        bool villainAggressorCheckedPreviousStreet = false,
        bool villainBarreling = false,
        OpponentType villainType = OpponentType.Unknown,
        PairClassification pairClassification = PairClassification.None,
        double foldEquity = 0,
        BetSizeCategory villainBetSizeFlop = BetSizeCategory.NoBet,
        BetSizeCategory villainBetSizeTurn = BetSizeCategory.NoBet,
        bool villainCheckedMiddleStreet = false,
        bool heroHasNutBlocker = false,
        bool heroFloatedFlop = false,
        double villainFoldToBetPct = -1,
        KickerStrength heroKickerStrength = KickerStrength.None,
        bool turnCalledWithFlushDanger = false,
        bool heroBlocksTopCard = false,
        bool heroCheckedAllStreets = false,
        bool isAnyoneAllIn = false,
        bool isDonkBet = false,
        OpponentProfile? villainProfile = null,
        TablePosition heroPosition = TablePosition.None,
        TablePosition villainPosition = TablePosition.None,
        bool isBroadwayWet = false,
        double effectiveOuts = 0,
        RiverCardType riverCardType = RiverCardType.Neutral)
    {
        return new PostflopDecisionInput
        {
            Equity = equity,
            Street = street,
            Situation = situation,
            BoardTexture = boardTexture,
            IsInPosition = isInPosition,
            VillainBetSize = villainBetSize,
            PotOdds = potOdds,
            TotalOuts = totalOuts,
            PreviousStreetBet = previousStreetBet,
            VillainShowedAggression = villainShowedAggression,
            BoardChange = boardChange,
            HeroBlocksDangerSuit = heroBlocksDangerSuit,
            HeroStack = heroStack,
            PotSize = potSize,
            HasFlushDraw = hasFlushDraw,
            NumOpponents = numOpponents,
            HeroIsAggressor = heroIsAggressor,
            HeroHandRank = heroHandRank,
            HasComboDraw = hasComboDraw,
            VillainAggressorCheckedPreviousStreet = villainAggressorCheckedPreviousStreet,
            VillainBarreling = villainBarreling,
            VillainType = villainType,
            PairClassification = pairClassification,
            FoldEquity = foldEquity,
            VillainBetSizeFlop = villainBetSizeFlop,
            VillainBetSizeTurn = villainBetSizeTurn,
            VillainCheckedMiddleStreet = villainCheckedMiddleStreet,
            HeroHasNutBlocker = heroHasNutBlocker,
            HeroFloatedFlop = heroFloatedFlop,
            VillainFoldToBetPct = villainFoldToBetPct,
            HeroKickerStrength = heroKickerStrength,
            TurnCalledWithFlushDanger = turnCalledWithFlushDanger,
            HeroBlocksTopCard = heroBlocksTopCard,
            HeroCheckedAllStreets = heroCheckedAllStreets,
            IsAnyoneAllIn = isAnyoneAllIn,
            IsDonkBet = isDonkBet,
            VillainProfile = villainProfile,
            HeroPosition = heroPosition,
            VillainPosition = villainPosition,
            IsBroadwayWet = isBroadwayWet,
            EffectiveOuts = effectiveOuts,
            RiverCardType = riverCardType,
        };
    }
}
