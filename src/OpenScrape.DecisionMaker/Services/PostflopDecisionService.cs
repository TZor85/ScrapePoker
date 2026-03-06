using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Services;

public enum BetSizeCategory { NoBet, Small, Medium, Large }

public record PostflopDecisionResult(
    string Action,
    string? Reason = null,
    bool IsBluff = false,
    bool IsBarrel = false);

public class PostflopDecisionService
{
    private readonly StrategyProfile _profile;

    public PostflopDecisionService(IOptions<StrategyProfile> profileOptions)
    {
        _profile = profileOptions.Value;
    }

    /// <summary>
    /// Obtiene los thresholds para una combinación de street y situación.
    /// </summary>
    public StreetThresholds GetThresholds(BoardPosition street, HandSituation situation)
    {
        var key = $"{street}_{situation}";
        if (_profile.Thresholds.TryGetValue(key, out var thresholds))
            return thresholds;

        return new StreetThresholds
        {
            FoldBelow = 40,
            ThinValueAbove = 45,
            ValueAbove = 55,
            StrongValueAbove = 75,
            CanBluff = false,
            LowEquityAction = "Fold",
            ThinValueIPOnly = true,
            ThinValueOOPFallback = "CheckFold"
        };
    }

    /// <summary>
    /// Determina la acción postflop basándose en equity, street, situación y contexto.
    /// </summary>
    public PostflopDecisionResult DetermineAction(
        double equity,
        BoardPosition street,
        HandSituation situation,
        string boardTexture,
        bool isInPosition,
        BetSizeCategory betSize,
        double potOdds = 0,
        int totalOuts = 0,
        bool previousStreetBet = false)
    {
        var thresholds = GetThresholds(street, situation);

        // Modo simplificado (RaiseOverLimper)
        if (thresholds.IsSimplified)
            return DetermineSimplifiedAction(equity, thresholds, isInPosition);

        // Equity baja
        if (equity < thresholds.FoldBelow)
            return HandleLowEquity(equity, thresholds, isInPosition, boardTexture, betSize, street, potOdds, totalOuts);

        // Determinar bet base por textura
        var baseBet = boardTexture switch
        {
            "Dry" => thresholds.DryBoardBetSize,
            "Coordinated" => thresholds.CoordinatedBoardBetSize,
            "Paired" => thresholds.PairedBoardBetSize,
            _ => thresholds.DryBoardBetSize
        };

        // Ajustes de sizing
        if (thresholds.ReduceSizeForLargeBet && betSize == BetSizeCategory.Large)
            baseBet = ReduceBetSize(baseBet);
        if (thresholds.ReduceSizeForOOP && !isInPosition)
            baseBet = ReduceBetSize(baseBet);

        // Determinar acción por tier de equity
        if (equity > thresholds.StrongValueAbove)
        {
            bool isBarrel = previousStreetBet && street == BoardPosition.River;
            return new PostflopDecisionResult(
                thresholds.StrongValueBetSize + " (Value)",
                "Strong value",
                IsBarrel: isBarrel);
        }

        if (equity > thresholds.ValueAbove)
        {
            return new PostflopDecisionResult(
                thresholds.ValueBetSize + " (Value)",
                "Value bet");
        }

        if (equity > thresholds.ThinValueAbove)
        {
            if (!thresholds.ThinValueIPOnly || isInPosition)
            {
                return new PostflopDecisionResult(
                    thresholds.ThinValueBetSize + " (Thin Value)",
                    "Thin value");
            }

            var fallback = thresholds.ThinValueOOPFallback == "CheckCall" ? "Call" : "Fold";
            var action = betSize == BetSizeCategory.NoBet ? "Check" : fallback;
            return new PostflopDecisionResult(action, "Thin value OOP fallback");
        }

        // Equity entre FoldBelow y ThinValueAbove (zona marginal)
        // Integrar pot odds para calls marginales
        if (betSize != BetSizeCategory.NoBet && potOdds > 0 && equity >= potOdds)
        {
            return new PostflopDecisionResult("Call", "Pot odds favorables");
        }

        // Showdown value: en river sin apuesta, check con equity marginal
        if (street == BoardPosition.River && betSize == BetSizeCategory.NoBet)
        {
            return new PostflopDecisionResult("Check", "Showdown value");
        }

        var defaultFallback = thresholds.LowEquityAction == "Call" ? "Call" : "Fold";
        var defaultAction = betSize == BetSizeCategory.NoBet ? "Check" : defaultFallback;
        return new PostflopDecisionResult(defaultAction, "Equity marginal");
    }

    private PostflopDecisionResult HandleLowEquity(
        double equity,
        StreetThresholds thresholds,
        bool isInPosition,
        string boardTexture,
        BetSizeCategory betSize,
        BoardPosition street,
        double potOdds,
        int totalOuts)
    {
        // Semi-bluff con draws: si tenemos outs y pot odds aceptables
        if (totalOuts >= 8 && street != BoardPosition.River)
        {
            return new PostflopDecisionResult(
                thresholds.BluffBetSize + " (Semi-Bluff)",
                $"Semi-bluff con {totalOuts} outs",
                IsBluff: true);
        }

        // Bluff puro según condiciones
        if (thresholds.CanBluff && ShouldBluff(thresholds, isInPosition, boardTexture, betSize, street))
        {
            return new PostflopDecisionResult(
                thresholds.BluffBetSize + " (Bluff)",
                "Bluff según condiciones",
                IsBluff: true);
        }

        // Pot odds check: si getting good odds, call
        if (betSize != BetSizeCategory.NoBet && potOdds > 0 && equity >= potOdds * 0.8)
        {
            return new PostflopDecisionResult("Call", "Pot odds marginales");
        }

        var fallback = thresholds.LowEquityAction == "Call" ? "Call" : "Fold";
        var action = betSize == BetSizeCategory.NoBet ? "Check" : fallback;
        return new PostflopDecisionResult(action, "Equity baja");
    }

    private bool ShouldBluff(StreetThresholds thresholds, bool isInPosition, string boardTexture, BetSizeCategory betSize, BoardPosition street)
    {
        var bluffFreq = GetBluffFrequency(street) * thresholds.BluffFrequencyMultiplier;

        return thresholds.BluffCondition switch
        {
            "Always" => Random.Shared.NextDouble() < bluffFreq,
            "OOPOnly" => !isInPosition && Random.Shared.NextDouble() < bluffFreq,
            "IPCoordinatedSmallOnly" => isInPosition && boardTexture == "Coordinated" && betSize == BetSizeCategory.Small && Random.Shared.NextDouble() < bluffFreq,
            _ => false
        };
    }

    private double GetBluffFrequency(BoardPosition street) => street switch
    {
        BoardPosition.Flop => _profile.FlopBluffFrequency,
        BoardPosition.Turn => _profile.TurnBluffFrequency,
        BoardPosition.River => _profile.RiverBluffFrequency,
        _ => 0.0
    };

    private static PostflopDecisionResult DetermineSimplifiedAction(double equity, StreetThresholds thresholds, bool isInPosition)
    {
        if (isInPosition)
        {
            if (equity > thresholds.StrongValueAbove)
                return new PostflopDecisionResult(thresholds.SimplifiedIPStrongBet, "Strong value IP");
            if (equity > thresholds.ThinValueAbove)
                return new PostflopDecisionResult(thresholds.SimplifiedIPThinBet, "Thin value IP");
            return new PostflopDecisionResult("Check (Fold)", "Equity baja IP");
        }

        if (equity > thresholds.StrongValueAbove)
            return new PostflopDecisionResult(thresholds.SimplifiedOOPStrongBet, "Strong value OOP");
        if (equity > thresholds.ValueAbove)
            return new PostflopDecisionResult(thresholds.SimplifiedOOPValueBet, "Value OOP");
        if (equity > thresholds.ThinValueAbove)
            return new PostflopDecisionResult(thresholds.SimplifiedOOPThinBet, "Thin value OOP");
        return new PostflopDecisionResult("Check (Fold)", "Equity baja OOP");
    }

    private static string ReduceBetSize(string bet)
    {
        return bet
            .Replace("Pot", "3/4")
            .Replace("3/4", "2/3")
            .Replace("2/3", "1/2")
            .Replace("1/2", "1/3");
    }
}
