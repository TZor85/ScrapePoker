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
    bool IsBarrel = false,
    bool IsCheckRaise = false);

public class PostflopDecisionService
{
    private readonly StrategyProfile _profile;

    // Penalización de equity al facing bet según tamaño (se suma a FoldBelow)
    private const double FacingBetPenaltyLarge = 8.0;
    private const double FacingBetPenaltyMedium = 4.0;
    private const double FacingBetPenaltySmall = 1.0;
    private const double VillainAggressionPenalty = 3.0;

    // Regla del 2 y del 4: multiplicador de outs → equity aproximada
    private const double TurnOutsMultiplier = 2.17;
    private const double RiverOutsMultiplier = 4.35;

    // Outs mínimos para considerar semi-bluff o call con draws
    private const int MinOutsForDraw = 8;

    // Margen para pot odds marginales (80% de las pot odds requeridas)
    private const double MarginalPotOddsFactor = 0.80;

    // Multi-way: penalización por oponente adicional (más de 1)
    private const double MultiwayFoldBelowPerOpponent = 4.0;
    private const double MultiwayThinValuePerOpponent = 3.0;
    // Multi-way: no bluffear con 3+ oponentes
    private const int MaxOpponentsForBluff = 2;

    // Agresor vs caller: ajustes de threshold al facing bet
    private const double AggressorVsDonkFoldReduction = 5.0;
    private const double AggressorVsDonkThinValueReduction = 3.0;
    private const double CallerVsCbetFoldIncrease = 2.0;

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
    /// Calcula el factor de implied odds basado en SPR, posición, street y tipo de draw.
    /// Retorna un valor entre 0 y 1: menor = mejores implied odds (necesitas menos equity).
    /// En river no hay implied odds (no hay más calles).
    /// </summary>
    public double CalculateImpliedOddsFactor(
        BoardPosition street,
        bool isInPosition,
        bool hasFlushDraw,
        decimal heroStack = 0,
        decimal potSize = 0)
    {
        // River: no hay implied odds (última calle)
        if (street == BoardPosition.River)
            return 1.0;

        // Sin datos de stack/pot: factor neutro
        if (heroStack <= 0 || potSize <= 0)
            return 1.0;

        // 1. Factor base por SPR (Stack-to-Pot Ratio)
        double spr = (double)(heroStack / potSize);
        double sprFactor;
        if (spr >= _profile.ImpliedOddsSPRDeepThreshold)
            sprFactor = _profile.ImpliedOddsSPRDeepFactor;
        else if (spr <= _profile.ImpliedOddsSPRShallowThreshold)
            sprFactor = _profile.ImpliedOddsSPRShallowFactor;
        else
        {
            // Interpolación lineal entre shallow y deep
            double range = _profile.ImpliedOddsSPRDeepThreshold - _profile.ImpliedOddsSPRShallowThreshold;
            double position = (spr - _profile.ImpliedOddsSPRShallowThreshold) / range;
            sprFactor = _profile.ImpliedOddsSPRShallowFactor +
                (position * (_profile.ImpliedOddsSPRDeepFactor - _profile.ImpliedOddsSPRShallowFactor));
        }

        // 2. Multiplicar por factor de calle (flop tiene 2 calles por extraer, turn solo 1)
        double streetFactor = street == BoardPosition.Turn
            ? _profile.ImpliedOddsTurnMultiplier
            : _profile.ImpliedOddsFlopMultiplier;
        sprFactor *= streetFactor;

        // 3. Bonus por posición (IP controla tamaño del pote futuro)
        if (isInPosition)
            sprFactor *= _profile.ImpliedOddsIPBonus;

        // 4. Bonus por flush draw (más difícil de leer para el villano)
        if (hasFlushDraw)
            sprFactor *= _profile.ImpliedOddsFlushDrawBonus;

        // Limitar entre 0.5 y 1.0 (no reducir más del 50% las odds requeridas)
        return Math.Max(0.50, Math.Min(1.0, sprFactor));
    }

    /// <summary>
    /// Determina la acción postflop con contexto completo: facing bet, pot odds, outs, posición, agresión, implied odds.
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
        bool heroBlocksDangerSuit = false,
        decimal heroStack = 0,
        decimal potSize = 0,
        bool hasFlushDraw = false,
        int numOpponents = 1,
        bool heroIsAggressor = false,
        HandRank heroHandRank = HandRank.HighCard)
    {
        var thresholds = GetThresholds(street, situation);
        bool isFacingBet = villainBetSize != BetSizeCategory.NoBet;
        bool isMultiway = numOpponents >= 2;

        // Calcular implied odds factor
        double impliedOddsFactor = CalculateImpliedOddsFactor(
            street, isInPosition, hasFlushDraw, heroStack, potSize);

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
                BetSizeCategory.Large => FacingBetPenaltyLarge,
                BetSizeCategory.Medium => FacingBetPenaltyMedium,
                BetSizeCategory.Small => FacingBetPenaltySmall,
                _ => 0
            };
            adjustedFoldBelow += facingBetPenalty;
            adjustedThinValueAbove += facingBetPenalty / 2;

            // Villano agresivo postflop → necesitamos aún más equity
            if (villainShowedAggression)
            {
                adjustedFoldBelow += VillainAggressionPenalty;
            }
        }

        // Multi-way: necesitamos más equity con más oponentes activos
        if (isMultiway)
        {
            int extraOpponents = numOpponents - 1;
            adjustedFoldBelow += extraOpponents * MultiwayFoldBelowPerOpponent;
            adjustedThinValueAbove += extraOpponents * MultiwayThinValuePerOpponent;
        }

        // Agresor vs caller: donk bet del villano contra hero agresor → respuesta agresiva
        if (isFacingBet && heroIsAggressor)
        {
            adjustedFoldBelow -= AggressorVsDonkFoldReduction;
            adjustedThinValueAbove -= AggressorVsDonkThinValueReduction;
        }
        else if (isFacingBet && !heroIsAggressor)
        {
            adjustedFoldBelow += CallerVsCbetFoldIncrease;
        }

        // Equity baja (debajo del threshold ajustado)
        if (effectiveEquity < adjustedFoldBelow)
            return HandleLowEquity(effectiveEquity, thresholds, isInPosition, boardTexture,
                villainBetSize, street, potOdds, totalOuts, isFacingBet, impliedOddsFactor, isMultiway);

        // --- FACING BET: decidir entre Call y Raise ---
        if (isFacingBet)
            return HandleFacingBet(effectiveEquity, thresholds, isInPosition, villainBetSize,
                street, potOdds, adjustedThinValueAbove, previousStreetBet, impliedOddsFactor,
                heroIsAggressor, heroHandRank);

        // --- NO FACING BET: decidir entre Check y Bet ---
        return HandleNoBet(effectiveEquity, thresholds, isInPosition, boardTexture,
            street, previousStreetBet, heroIsAggressor, heroHandRank, isMultiway);
    }

    /// <summary>
    /// Cuando el villano apuesta: decidir Fold/Call/Raise.
    /// Implied odds reducen las pot odds necesarias para continuar.
    /// </summary>
    private PostflopDecisionResult HandleFacingBet(
        double equity,
        StreetThresholds thresholds,
        bool isInPosition,
        BetSizeCategory villainBetSize,
        BoardPosition street,
        double potOdds,
        double adjustedThinValueAbove,
        bool previousStreetBet,
        double impliedOddsFactor,
        bool heroIsAggressor = false,
        HandRank heroHandRank = HandRank.HighCard)
    {
        // Pot odds ajustadas por implied odds (factor < 1.0 = necesitas menos equity)
        double adjustedPotOdds = potOdds > 0 ? potOdds * impliedOddsFactor : 0;

        // Equity muy alta → raise solo con mano fuerte (TwoPair+), call con parejas
        // Con OnePair raise hincha el pote con mano vulnerable, foldea peores y solo nos pagan mejores
        if (equity > thresholds.StrongValueAbove)
        {
            if (heroHandRank >= HandRank.TwoPair)
            {
                var raiseSize = villainBetSize == BetSizeCategory.Large
                    ? "Raise Pot"
                    : "Raise 3x";
                bool isBarrel = previousStreetBet && street == BoardPosition.River;
                return new PostflopDecisionResult(raiseSize + " (Value)",
                    $"Raise for value vs bet — {heroHandRank}", IsBarrel: isBarrel);
            }

            // OnePair o menos con equity alta → call (proteger, no hinchar pote)
            return new PostflopDecisionResult("Call",
                $"Call — equity alta pero mano vulnerable ({heroHandRank})");
        }

        // Hero agresor vs donk bet → raise con mano fuerte, call con pareja
        if (heroIsAggressor && equity > thresholds.ValueAbove)
        {
            if (heroHandRank >= HandRank.TwoPair)
                return new PostflopDecisionResult("Raise 3x (Value)",
                    $"Raise — hero agresor vs donk bet ({heroHandRank})");

            return new PostflopDecisionResult("Call",
                "Call — hero agresor vs donk bet, mano vulnerable");
        }

        // Equity buena → call (no raise, el villano ya mostró fuerza)
        if (equity > thresholds.ValueAbove)
        {
            return new PostflopDecisionResult("Call", "Call — equity buena vs bet");
        }

        // Thin value → call si pot odds (con implied) favorables, sino depende de posición
        if (equity > adjustedThinValueAbove)
        {
            if (adjustedPotOdds > 0 && equity >= adjustedPotOdds)
                return new PostflopDecisionResult("Call",
                    $"Call — implied odds favorables (SPR factor={impliedOddsFactor:F2})");

            if (isInPosition)
                return new PostflopDecisionResult("Call", "Call — thin value IP");

            var fallback = thresholds.ThinValueOOPFallback == "CheckCall" ? "Call" : "Fold";
            return new PostflopDecisionResult(fallback, "Thin value OOP vs bet");
        }

        // Equity marginal pero implied odds buenos
        if (adjustedPotOdds > 0 && equity >= adjustedPotOdds)
            return new PostflopDecisionResult("Call",
                $"Call — implied odds favorables (SPR factor={impliedOddsFactor:F2})");

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
        bool previousStreetBet,
        bool heroIsAggressor = false,
        HandRank heroHandRank = HandRank.HighCard,
        bool isMultiway = false)
    {
        // Check-raise: OOP con mano premium, esperando bet del villano para raise
        if (thresholds.CanCheckRaise && !isInPosition && !isMultiway &&
            equity > thresholds.CheckRaiseThreshold &&
            heroHandRank >= HandRank.TwoPair &&
            !heroIsAggressor)
        {
            return new PostflopDecisionResult(
                "Check (Check-Raise)",
                $"Check-raise trap — {heroHandRank} OOP",
                IsCheckRaise: true);
        }

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

        // Hand strength relativa: ajustar thresholds según vulnerabilidad de la mano
        double vulnerabilityAdjust = GetHandVulnerabilityAdjustment(heroHandRank, boardTexture);
        double adjStrongValue = thresholds.StrongValueAbove + vulnerabilityAdjust;
        double adjValue = thresholds.ValueAbove + vulnerabilityAdjust;

        // Overbet en boards muy secos con mano premium (solo agresor, no river)
        if (thresholds.CanOverbet && heroIsAggressor &&
            equity > thresholds.OverbetMinEquity &&
            boardTexture == "Dry" && street != BoardPosition.River)
        {
            return new PostflopDecisionResult(
                thresholds.OverbetBetSize + " (Value)",
                "Overbet — board seco con ventaja de rango");
        }

        // Strong value → bet grande (con sizing boost para manos nuts)
        if (equity > adjStrongValue)
        {
            bool isBarrel = previousStreetBet && street == BoardPosition.River;
            var betSize = heroHandRank >= HandRank.ThreeOfAKind
                ? IncreaseBetSize(thresholds.StrongValueBetSize)
                : thresholds.StrongValueBetSize;
            return new PostflopDecisionResult(
                betSize + " (Value)",
                $"Bet — strong value ({heroHandRank})",
                IsBarrel: isBarrel);
        }

        // Value → bet
        if (equity > adjValue)
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
    /// Calcula ajuste de vulnerabilidad basado en la fuerza relativa de la mano.
    /// Positivo = mano vulnerable (necesita más equity), Negativo = mano nuts (necesita menos).
    /// </summary>
    private static double GetHandVulnerabilityAdjustment(HandRank rank, string boardTexture)
    {
        double factor = rank switch
        {
            HandRank.RoyalFlush or HandRank.StraightFlush => -8.0,
            HandRank.FourOfAKind => -8.0,
            HandRank.FullHouse => -5.0,
            HandRank.Flush => boardTexture == "Paired" ? -2.0 : -4.0,
            HandRank.Straight => boardTexture == "Coordinated" ? 2.0 : -2.0,
            HandRank.ThreeOfAKind => -4.0,
            HandRank.TwoPair => boardTexture == "Coordinated" ? 3.0 : 0.0,
            HandRank.OnePair => 2.0,
            _ => 4.0
        };
        return factor;
    }

    /// <summary>
    /// Aumenta el tamaño de apuesta un nivel: 1/3→1/2→2/3→3/4→Pot.
    /// </summary>
    private static string IncreaseBetSize(string bet)
    {
        if (bet.Contains("1/3")) return bet.Replace("1/3", "1/2");
        if (bet.Contains("1/2")) return bet.Replace("1/2", "2/3");
        if (bet.Contains("2/3")) return bet.Replace("2/3", "3/4");
        if (bet.Contains("3/4")) return bet.Replace("3/4", "Pot");
        return bet;
    }

    /// <summary>
    /// Equity baja: semi-bluff con draws, bluff puro, pot odds marginales (con implied odds), o fold.
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
        bool isFacingBet,
        double impliedOddsFactor,
        bool isMultiway = false)
    {
        // Semi-bluff con draws (solo si NO estamos facing a bet y no multiway con muchos oponentes)
        if (totalOuts >= MinOutsForDraw && street != BoardPosition.River && !isFacingBet && !isMultiway)
        {
            // Combo draw (12+ outs) en flop: sizing agresivo (3/4 pot)
            bool isComboDrawOnFlop = totalOuts >= thresholds.ComboDrawOutsThreshold
                && street == BoardPosition.Flop;
            var semiBluffSize = isComboDrawOnFlop
                ? thresholds.ComboDrawBetSize
                : thresholds.BluffBetSize;

            return new PostflopDecisionResult(
                semiBluffSize + " (Semi-Bluff)",
                isComboDrawOnFlop
                    ? $"Semi-bluff agresivo: combo draw con {totalOuts} outs"
                    : $"Semi-bluff con {totalOuts} outs",
                IsBluff: true);
        }

        // Con draws y facing bet → call si implied odds lo justifican
        if (totalOuts >= MinOutsForDraw && street != BoardPosition.River && isFacingBet)
        {
            double adjustedPotOdds = potOdds > 0 ? potOdds * impliedOddsFactor : 999;
            double drawEquity = totalOuts * (street == BoardPosition.Turn ? TurnOutsMultiplier : RiverOutsMultiplier);
            if (drawEquity >= adjustedPotOdds)
                return new PostflopDecisionResult("Call",
                    $"Call — draw con {totalOuts} outs (implied odds, SPR factor={impliedOddsFactor:F2})");
        }

        // Bluff puro (solo sin facing bet, no multiway — no bluffear contra una apuesta ni multiway)
        if (!isFacingBet && !isMultiway && thresholds.CanBluff &&
            ShouldBluff(thresholds, isInPosition, boardTexture, villainBetSize, street))
        {
            return new PostflopDecisionResult(
                thresholds.BluffBetSize + " (Bluff)",
                "Bluff según condiciones",
                IsBluff: true);
        }

        // Pot odds marginales con implied odds
        if (isFacingBet && potOdds > 0 && equity >= potOdds * MarginalPotOddsFactor * impliedOddsFactor)
        {
            return new PostflopDecisionResult("Call",
                $"Call — pot odds marginales (implied factor={impliedOddsFactor:F2})");
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

    /// <summary>
    /// Reduce el tamaño de apuesta un nivel: Pot→3/4→2/3→1/2→1/3.
    /// Usa matching exclusivo para evitar reducciones en cascada.
    /// </summary>
    private static string ReduceBetSize(string bet)
    {
        if (bet.Contains("Pot")) return bet.Replace("Pot", "3/4");
        if (bet.Contains("3/4")) return bet.Replace("3/4", "2/3");
        if (bet.Contains("2/3")) return bet.Replace("2/3", "1/2");
        if (bet.Contains("1/2")) return bet.Replace("1/2", "1/3");
        return bet;
    }
}
