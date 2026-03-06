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
    /// Calcula la penalización de equity por carta peligrosa en el board.
    /// Flush/straight usan penalización porcentual (proporcional a la equity).
    /// Facing bet multiplica la penalización (villano representa el draw completado).
    /// </summary>
    public double CalculateDangerPenalty(double rawEquity, BoardChangeResult boardChange, bool heroBlocksDangerSuit, bool isFacingBet)
    {
        if (boardChange.DangerLevel == 0)
            return 0;

        double penalty = 0;

        // Completaciones mayores: porcentual sobre equity (escala con la fuerza de la mano)
        if (boardChange.FlushCompleted)
            penalty += rawEquity * (_profile.DangerFlushCompletePct / 100.0);
        else if (boardChange.FlushDrawAppeared)
            penalty += _profile.DangerFlushDrawPenalty;

        if (boardChange.StraightCompleted)
            penalty += rawEquity * (_profile.DangerStraightCompletePct / 100.0);

        // Cambios menores: flat
        if (boardChange.BoardPaired) penalty += _profile.DangerBoardPairedPenalty;
        if (boardChange.OvercardAppeared) penalty += _profile.DangerOvercardPenalty;

        // Facing bet en board peligroso → villano representando el draw completado
        if (isFacingBet)
            penalty *= _profile.DangerFacingBetMultiplier;

        // Blocker effect: hero tiene carta del suit peligroso, reduce penalización
        if (heroBlocksDangerSuit)
            penalty *= _profile.DangerHeroBlocksReduction;

        return penalty;
    }

    /// <summary>
    /// Determina la acción postflop con contexto completo: facing bet, pot odds, outs, posición, agresión.
    /// </summary>
    public PostflopDecisionResult DetermineAction(
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
        bool heroBlocksDangerSuit = false)
    {
        var thresholds = GetThresholds(street, situation);
        bool isFacingBet = villainBetSize != BetSizeCategory.NoBet;

        // Aplicar penalización por carta peligrosa
        double dangerPenalty = boardChange != null
            ? CalculateDangerPenalty(equity, boardChange, heroBlocksDangerSuit, isFacingBet)
            : 0;
        double effectiveEquity = equity - dangerPenalty;

        // Tope de equity para APOSTAR en boards con draw completado que hero no tiene.
        // Apostar solo consigue que nos paguen flushes/straights (peores foldean, mejores pagan).
        if (!isFacingBet && boardChange != null && !heroBlocksDangerSuit &&
            (boardChange.FlushCompleted || boardChange.StraightCompleted))
        {
            effectiveEquity = Math.Min(effectiveEquity, _profile.DangerCompletedDrawNoBetCap);
        }

        // Modo simplificado (RaiseOverLimper)
        if (thresholds.IsSimplified)
            return DetermineSimplifiedAction(effectiveEquity, thresholds, isInPosition, isFacingBet);

        // Ajustar thresholds si estamos facing a bet (necesitamos más equity para continuar)
        double adjustedFoldBelow = thresholds.FoldBelow;
        double adjustedThinValueAbove = thresholds.ThinValueAbove;
        if (isFacingBet)
        {
            double facingBetPenalty = villainBetSize switch
            {
                BetSizeCategory.Large => 8.0,
                BetSizeCategory.Medium => 4.0,
                BetSizeCategory.Small => 1.0,
                _ => 0
            };
            adjustedFoldBelow += facingBetPenalty;
            adjustedThinValueAbove += facingBetPenalty / 2;

            // Villano agresivo postflop → necesitamos aún más equity
            if (villainShowedAggression)
            {
                adjustedFoldBelow += 3.0;
            }
        }

        // Equity baja (debajo del threshold ajustado)
        if (effectiveEquity < adjustedFoldBelow)
            return HandleLowEquity(effectiveEquity, thresholds, isInPosition, boardTexture,
                villainBetSize, street, potOdds, totalOuts, isFacingBet);

        // --- FACING BET: decidir entre Call y Raise ---
        if (isFacingBet)
            return HandleFacingBet(effectiveEquity, thresholds, isInPosition, villainBetSize,
                street, potOdds, adjustedThinValueAbove, previousStreetBet);

        // --- NO FACING BET: decidir entre Check y Bet ---
        return HandleNoBet(effectiveEquity, thresholds, isInPosition, boardTexture,
            street, previousStreetBet);
    }

    /// <summary>
    /// Cuando el villano apuesta: decidir Fold/Call/Raise.
    /// </summary>
    private PostflopDecisionResult HandleFacingBet(
        double equity,
        StreetThresholds thresholds,
        bool isInPosition,
        BetSizeCategory villainBetSize,
        BoardPosition street,
        double potOdds,
        double adjustedThinValueAbove,
        bool previousStreetBet)
    {
        // Equity muy alta → raise for value
        if (equity > thresholds.StrongValueAbove)
        {
            var raiseSize = villainBetSize == BetSizeCategory.Large
                ? "Raise Pot"
                : "Raise 3x";
            bool isBarrel = previousStreetBet && street == BoardPosition.River;
            return new PostflopDecisionResult(raiseSize + " (Value)", "Raise for value vs bet", IsBarrel: isBarrel);
        }

        // Equity buena → call (no raise, el villano ya mostró fuerza)
        if (equity > thresholds.ValueAbove)
        {
            return new PostflopDecisionResult("Call", "Call — equity buena vs bet");
        }

        // Thin value → call si pot odds favorables, sino depende de posición
        if (equity > adjustedThinValueAbove)
        {
            if (potOdds > 0 && equity >= potOdds)
                return new PostflopDecisionResult("Call", "Call — pot odds favorables");

            if (isInPosition)
                return new PostflopDecisionResult("Call", "Call — thin value IP");

            var fallback = thresholds.ThinValueOOPFallback == "CheckCall" ? "Call" : "Fold";
            return new PostflopDecisionResult(fallback, "Thin value OOP vs bet");
        }

        // Equity marginal pero pot odds buenos
        if (potOdds > 0 && equity >= potOdds)
            return new PostflopDecisionResult("Call", "Call — pot odds favorables");

        // Showdown value en river con bet pequeña
        if (street == BoardPosition.River && villainBetSize == BetSizeCategory.Small && equity >= thresholds.FoldBelow)
            return new PostflopDecisionResult("Call", "Call — showdown value vs bet pequeña");

        var lowFallback = thresholds.LowEquityAction == "Call" ? "Call" : "Fold";
        return new PostflopDecisionResult(lowFallback, "Equity insuficiente vs bet");
    }

    /// <summary>
    /// Sin apuesta del villano: decidir Check o Bet.
    /// </summary>
    private PostflopDecisionResult HandleNoBet(
        double equity,
        StreetThresholds thresholds,
        bool isInPosition,
        string boardTexture,
        BoardPosition street,
        bool previousStreetBet)
    {
        // Determinar bet size base por textura de board
        var baseBet = boardTexture switch
        {
            "Dry" => thresholds.DryBoardBetSize,
            "Coordinated" => thresholds.CoordinatedBoardBetSize,
            "Paired" => thresholds.PairedBoardBetSize,
            _ => thresholds.DryBoardBetSize
        };

        // Ajustar por OOP
        if (thresholds.ReduceSizeForOOP && !isInPosition)
            baseBet = ReduceBetSize(baseBet);

        // Strong value → bet grande
        if (equity > thresholds.StrongValueAbove)
        {
            bool isBarrel = previousStreetBet && street == BoardPosition.River;
            return new PostflopDecisionResult(
                thresholds.StrongValueBetSize + " (Value)",
                "Bet — strong value",
                IsBarrel: isBarrel);
        }

        // Value → bet
        if (equity > thresholds.ValueAbove)
        {
            return new PostflopDecisionResult(
                thresholds.ValueBetSize + " (Value)",
                "Bet — value");
        }

        // Thin value → bet solo IP (OOP check para proteger rango)
        if (equity > thresholds.ThinValueAbove)
        {
            if (!thresholds.ThinValueIPOnly || isInPosition)
            {
                return new PostflopDecisionResult(
                    thresholds.ThinValueBetSize + " (Thin Value)",
                    "Bet — thin value");
            }

            // OOP con thin value: check (showdown value, no hinchar pote OOP)
            return new PostflopDecisionResult("Check", "Check — thin value OOP (showdown)");
        }

        // Showdown value en river
        if (street == BoardPosition.River)
            return new PostflopDecisionResult("Check", "Check — showdown value");

        // Equity marginal sin facing bet → check
        return new PostflopDecisionResult("Check", "Check — equity marginal");
    }

    /// <summary>
    /// Equity baja: semi-bluff con draws, bluff puro, pot odds marginales, o fold.
    /// </summary>
    private PostflopDecisionResult HandleLowEquity(
        double equity,
        StreetThresholds thresholds,
        bool isInPosition,
        string boardTexture,
        BetSizeCategory villainBetSize,
        BoardPosition street,
        double potOdds,
        int totalOuts,
        bool isFacingBet)
    {
        // Semi-bluff con draws (solo si NO estamos facing a bet grande — no semi-bluff raise vs pot bet)
        if (totalOuts >= 8 && street != BoardPosition.River && !isFacingBet)
        {
            return new PostflopDecisionResult(
                thresholds.BluffBetSize + " (Semi-Bluff)",
                $"Semi-bluff con {totalOuts} outs",
                IsBluff: true);
        }

        // Con draws y facing bet → call si pot odds lo justifican
        if (totalOuts >= 8 && street != BoardPosition.River && isFacingBet)
        {
            // Implied odds: con draws fuertes, aceptamos odds peores
            double effectiveOdds = potOdds > 0 ? potOdds * 0.75 : 999;
            double drawEquity = totalOuts * (street == BoardPosition.Turn ? 2.17 : 4.35);
            if (drawEquity >= effectiveOdds)
                return new PostflopDecisionResult("Call", $"Call — draw con {totalOuts} outs (implied odds)");
        }

        // Bluff puro (solo sin facing bet — no bluffear contra una apuesta)
        if (!isFacingBet && thresholds.CanBluff &&
            ShouldBluff(thresholds, isInPosition, boardTexture, villainBetSize, street))
        {
            return new PostflopDecisionResult(
                thresholds.BluffBetSize + " (Bluff)",
                "Bluff según condiciones",
                IsBluff: true);
        }

        // Pot odds marginales (facing bet con equity baja pero odds)
        if (isFacingBet && potOdds > 0 && equity >= potOdds * 0.8)
        {
            return new PostflopDecisionResult("Call", "Call — pot odds marginales");
        }

        // Sin facing bet → check (no fold sin apuesta)
        if (!isFacingBet)
            return new PostflopDecisionResult("Check", "Check — equity baja");

        // Facing bet → fold o call según config
        var fallback = thresholds.LowEquityAction == "Call" ? "Call" : "Fold";
        return new PostflopDecisionResult(fallback, "Equity baja vs bet");
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

    private static PostflopDecisionResult DetermineSimplifiedAction(double equity, StreetThresholds thresholds, bool isInPosition, bool isFacingBet)
    {
        if (isInPosition)
        {
            if (equity > thresholds.StrongValueAbove)
                return new PostflopDecisionResult(
                    isFacingBet ? "Raise 3x (Value)" : thresholds.SimplifiedIPStrongBet,
                    "Strong value IP");
            if (equity > thresholds.ThinValueAbove)
                return new PostflopDecisionResult(
                    isFacingBet ? "Call" : thresholds.SimplifiedIPThinBet,
                    isFacingBet ? "Call — thin value IP" : "Thin value IP");
            return new PostflopDecisionResult(
                isFacingBet ? "Fold" : "Check",
                "Equity baja IP");
        }

        if (equity > thresholds.StrongValueAbove)
            return new PostflopDecisionResult(
                isFacingBet ? "Raise 3x (Value)" : thresholds.SimplifiedOOPStrongBet,
                "Strong value OOP");
        if (equity > thresholds.ValueAbove)
            return new PostflopDecisionResult(
                isFacingBet ? "Call" : thresholds.SimplifiedOOPValueBet,
                isFacingBet ? "Call — value OOP" : "Value OOP");
        if (equity > thresholds.ThinValueAbove)
            return new PostflopDecisionResult(
                isFacingBet ? "Call" : thresholds.SimplifiedOOPThinBet,
                isFacingBet ? "Call — thin value OOP" : "Thin value OOP");
        return new PostflopDecisionResult(
            isFacingBet ? "Fold" : "Check",
            "Equity baja OOP");
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
