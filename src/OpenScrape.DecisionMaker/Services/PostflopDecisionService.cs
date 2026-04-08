using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Services;

public enum BetSizeCategory { NoBet, Underbet, Small, Medium, Large }

public record PostflopDecisionResult(
    string Action,
    string? Reason = null,
    bool IsBluff = false,
    bool IsBarrel = false,
    bool IsCheckRaise = false,
    bool IsFloating = false);

public class PostflopDecisionService : IPostflopDecisionService
{
    private readonly StrategyProfile _profile;
    private readonly BetSizingService _betSizingService;
    private readonly RangePolarizer _rangePolarizer;

    public PostflopDecisionService(IOptions<StrategyProfile> profileOptions, BetSizingService betSizingService, RangePolarizer rangePolarizer)
    {
        _profile = profileOptions.Value;
        _betSizingService = betSizingService;
        _rangePolarizer = rangePolarizer;
    }

    /// <summary>
    /// Obtiene los thresholds para una combinación de street y situación.
    /// </summary>
    public StreetThresholds GetThresholds(BoardPosition street, HandSituation situation)
    {
        var key = $"{street}_{situation}";
        if (_profile.Thresholds.TryGetValue(key, out var thresholds))
            return thresholds;

        // Fallback: loguear warning para detectar configuración faltante
        Console.WriteLine($"[WARNING] Threshold no encontrado: '{key}'. Usando fallback genérico.");

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
    /// Delega a DangerPenaltyCalculator.
    /// </summary>
    public double CalculateDangerPenalty(double rawEquity, BoardChangeResult boardChange, bool heroBlocksDangerSuit, bool isFacingBet, BoardPosition street = BoardPosition.Turn, bool heroHasNutBlocker = false, HandRank heroHandRank = HandRank.HighCard, bool heroCompletedFlush = false, bool heroCompletedStraight = false)
        => DangerPenaltyCalculator.Calculate(rawEquity, boardChange, heroBlocksDangerSuit, isFacingBet, _profile, street, heroHasNutBlocker, heroHandRank, heroCompletedFlush, heroCompletedStraight);

    /// <summary>
    /// Calcula el factor de implied odds. Delega a ImpliedOddsCalculator.
    /// </summary>
    public double CalculateImpliedOddsFactor(
        BoardPosition street,
        bool isInPosition,
        bool hasFlushDraw,
        decimal heroStack = 0,
        decimal potSize = 0,
        int numOpponents = 1)
        => ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            street, isInPosition, hasFlushDraw, heroStack, potSize, _profile, numOpponents);

    /// <summary>
    /// Determina la acción postflop a partir de un objeto de contexto inmutable.
    /// </summary>
    public PostflopDecisionResult DetermineAction(PostflopDecisionInput input)
    {
#pragma warning disable CS0618 // Suppress obsolete warning for internal delegation
        return DetermineAction(
            input.Equity, input.Street, input.Situation,
            input.BoardTexture, input.IsInPosition, input.VillainBetSize,
            input.PotOdds, input.TotalOuts,
            input.PreviousStreetBet, input.VillainShowedAggression,
            input.BoardChange, input.HeroBlocksDangerSuit,
            input.HeroStack, input.PotSize,
            input.HasFlushDraw, input.NumOpponents,
            input.HeroIsAggressor, input.HeroHandRank,
            input.HasComboDraw, input.VillainAggressorCheckedPreviousStreet,
            input.VillainBarreling, input.VillainType,
            input.PairClassification, input.FoldEquity,
            input.VillainBetSizeFlop, input.VillainBetSizeTurn,
            input.VillainCheckedMiddleStreet, input.HeroHasNutBlocker,
            input.HeroFloatedFlop, input.VillainFoldToBetPct,
            input.HeroKickerStrength, input.TurnCalledWithFlushDanger,
            input.HeroBlocksTopCard, input.HeroCheckedAllStreets,
            input.IsAnyoneAllIn);
#pragma warning restore CS0618
    }

    /// <summary>
    /// Determina la acción postflop con contexto completo: facing bet, pot odds, outs, posición, agresión, implied odds.
    /// </summary>
    [Obsolete("Usar DetermineAction(PostflopDecisionInput) en su lugar")]
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
        bool isAnyoneAllIn = false)
    {
        var thresholds = GetThresholds(street, situation);
        bool isFacingBet = villainBetSize != BetSizeCategory.NoBet;
        bool isMultiway = numOpponents >= 2;

        // Calcular implied odds factor (ajustado por multiway)
        double impliedOddsFactor = CalculateImpliedOddsFactor(
            street, isInPosition, hasFlushDraw, heroStack, potSize, numOpponents);

        // L3: Detectar si hero completó flush/straight para skip danger penalties
        bool heroCompletedFlush = heroHandRank >= HandRank.Flush;
        bool heroCompletedStraight = heroHandRank >= HandRank.Straight && heroHandRank < HandRank.Flush;

        // Aplicar penalización por carta peligrosa (escalada por street, blocker granular, mano hero)
        double dangerPenalty = boardChange != null
            ? CalculateDangerPenalty(equity, boardChange, heroBlocksDangerSuit, isFacingBet, street, heroHasNutBlocker, heroHandRank, heroCompletedFlush, heroCompletedStraight)
            : 0;
        double effectiveEquity = equity - dangerPenalty;

        // Combo draw bonus: flush + straight draw = semi-bluff premium.
        // No aplicar si hero ya completó el draw (bonus es para draws pendientes).
        // L5: Ajustar por textura del board (Dry más valioso, Wet/Monotone menos).
        if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
        {
            double comboTextureMultiplier = boardTexture switch
            {
                "Dry" => _profile.ComboDrawTextureDry,
                "SemiDry" => _profile.ComboDrawTextureSemiDry,
                "SemiWet" => _profile.ComboDrawTextureSemiWet,
                "Wet" => _profile.ComboDrawTextureWet,
                "Monotone" => _profile.ComboDrawTextureMonotone,
                _ => 1.0
            };
            effectiveEquity += _profile.ComboDrawEquityBonus * comboTextureMultiplier;
        }

        // Tope de equity para APOSTAR en boards con draw completado que hero no tiene.
        // Aplicar ANTES de reverse implied odds para que el cap sea más conservador.
        // No aplicar si hero completó la escalera (HandRank >= Straight) o tiene flush (HandRank >= Flush).
        bool heroHasCompletedDraw = (boardChange?.StraightCompleted == true && heroHandRank >= HandRank.Straight) ||
                                    (boardChange?.FlushCompleted == true && heroHandRank >= HandRank.Flush);
        if (!isFacingBet && boardChange != null && !heroBlocksDangerSuit && !heroHasCompletedDraw &&
            (boardChange.FlushCompleted || boardChange.StraightCompleted))
        {
            effectiveEquity = Math.Min(effectiveEquity, _profile.DangerCompletedDrawNoBetCap);
        }

        // Reverse implied odds: penalizar calls en turn con mano vulnerable en board con draws
        // All-in: desactivar reverse implied (villain no puede apostar más)
        double reverseImpliedPenalty = isAnyoneAllIn ? 0 : CalculateReverseImpliedOdds(
            boardChange, heroHandRank, hasFlushDraw, street, isFacingBet, pairClassification,
            heroBlocksDangerSuit);
        effectiveEquity -= reverseImpliedPenalty;

        // Floor: equity efectiva no puede ser negativa (evita corrupción de thresholds)
        effectiveEquity = Math.Max(0, effectiveEquity);

        // All-in: desactivar fold equity (villain no puede foldear)
        if (isAnyoneAllIn)
            foldEquity = 0;

        // Modo simplificado (RaiseOverLimper)
        if (thresholds.IsSimplified)
            return DetermineSimplifiedAction(effectiveEquity, thresholds, isInPosition, isFacingBet, boardTexture);

        // Ajustar thresholds si estamos facing a bet
        double adjustedFoldBelow = thresholds.FoldBelow;
        double adjustedThinValueAbove = thresholds.ThinValueAbove;

        // [NUEVO] Aplicar ajustes de RangePolarizer según board texture, posición, SPR y street
        if (street != BoardPosition.None)
        {
            var rangeAdjustment = GetRangeBasedThresholdAdjustment(boardTexture, isInPosition, potSize, heroStack, street);
            adjustedFoldBelow += rangeAdjustment.foldBelowAdjust;
            adjustedThinValueAbove += rangeAdjustment.thinValueAdjust;
        }

        if (isFacingBet)
        {
            double facingBetPenalty = villainBetSize switch
            {
                BetSizeCategory.Large => PokerConstants.FacingBetPenaltyLarge,
                BetSizeCategory.Medium => PokerConstants.FacingBetPenaltyMedium,
                BetSizeCategory.Small => PokerConstants.FacingBetPenaltySmall,
                BetSizeCategory.Underbet => PokerConstants.FacingBetPenaltyUnderbet,
                _ => 0
            };

            // Escalar por street: bets grandes en flop son normales, en river = rango fuerte
            double streetMultiplier = street switch
            {
                BoardPosition.Turn => PokerConstants.FacingBetTurnMultiplier,
                BoardPosition.River => PokerConstants.FacingBetRiverMultiplier,
                _ => 1.0
            };
            facingBetPenalty *= streetMultiplier;

            adjustedFoldBelow += facingBetPenalty;
            adjustedThinValueAbove += facingBetPenalty / 2;

            if (villainShowedAggression)
                adjustedFoldBelow += PokerConstants.VillainAggressionPenalty;
        }

        // Multi-way penalty: IP lineal, OOP cuadrático + street multiplier
        if (isMultiway)
        {
            int extraOpponents = numOpponents - 1;

            // Street multiplier: turn/river multiway más peligroso (ranges más estrechas)
            double streetMult = street switch
            {
                BoardPosition.Turn => _profile.MultiwayStreetMultiplierTurn,
                BoardPosition.River => _profile.MultiwayStreetMultiplierRiver,
                _ => 1.0
            };

            double multiwayFoldPenalty;
            double multiwayValuePenalty;

            if (isInPosition)
            {
                // IP: lineal (como antes)
                multiwayFoldPenalty = extraOpponents * PokerConstants.MultiwayFoldBelowIP;
                multiwayValuePenalty = extraOpponents * PokerConstants.MultiwayThinValueIP;
            }
            else
            {
                // OOP: cuadrático con factor de amortiguación configurable
                multiwayFoldPenalty = extraOpponents * extraOpponents * PokerConstants.MultiwayFoldBelowOOP * _profile.MultiwayOOPQuadraticDamping;
                multiwayValuePenalty = extraOpponents * extraOpponents * PokerConstants.MultiwayThinValueOOP * _profile.MultiwayOOPQuadraticDamping;
            }

            adjustedFoldBelow += multiwayFoldPenalty * streetMult;
            adjustedThinValueAbove += multiwayValuePenalty * streetMult;
        }

        // 3-Bet/4-Bet pot: rango villano más estrecho → umbrales más estrictos
        if (situation is HandSituation.ThreeBet or HandSituation.OpenRaiseVs3Bet or HandSituation.Squeeze)
        {
            adjustedFoldBelow += _profile.ThreeBetPostflopFoldIncrease;
            adjustedThinValueAbove += _profile.ThreeBetPostflopValueIncrease;
        }
        else if (situation == HandSituation.FourBet)
        {
            adjustedFoldBelow += _profile.FourBetPostflopFoldIncrease;
            adjustedThinValueAbove += _profile.FourBetPostflopValueIncrease;
        }

        // Agresor vs caller
        if (isFacingBet && heroIsAggressor)
        {
            adjustedFoldBelow -= PokerConstants.AggressorVsDonkFoldReduction;
            adjustedThinValueAbove -= PokerConstants.AggressorVsDonkThinValueReduction;
        }
        else if (isFacingBet && !heroIsAggressor)
        {
            adjustedFoldBelow += PokerConstants.CallerVsCbetFoldIncrease;
        }

        // Range narrowing: villain apostó en múltiples calles → rango más estrecho
        // Bet-check-bet indica debilidad (draw fallido), penaliza menos que bet-bet-bet
        if (isFacingBet && street >= BoardPosition.Turn)
        {
            int villainBetStreets = (villainBetSizeFlop != BetSizeCategory.NoBet ? 1 : 0)
                + (villainBetSizeTurn != BetSizeCategory.NoBet ? 1 : 0)
                + (isFacingBet && street == BoardPosition.River ? 1 : 0);
            if (villainBetStreets >= 2)
            {
                double narrowingMultiplier = villainCheckedMiddleStreet ? 0.5 : 1.0;
                adjustedFoldBelow += PokerConstants.RangeNarrowingPerStreet * (villainBetStreets - 1) * narrowingMultiplier;
            }
        }

        // Kicker quality adjustment en facing bet: TPTK más confiado, TPWK más cauto
        if (isFacingBet && heroHandRank == HandRank.OnePair)
        {
            if (heroKickerStrength == KickerStrength.Strong)
                adjustedFoldBelow -= _profile.KickerStrongEquityBonus; // TPTK: más fácil call
            else if (heroKickerStrength == KickerStrength.Weak && !isInPosition)
                adjustedFoldBelow += _profile.KickerWeakEquityPenalty; // TPWK OOP: más cautela
        }

        // Villain barreling: distinguir barrel real (bet-bet) de bet-check-bet (reactivation)
        if (villainBarreling && isFacingBet)
        {
            if (villainCheckedMiddleStreet)
            {
                // Bet-check-bet: draw fallido reintentando, rango más débil → penalty menor
                adjustedFoldBelow += _profile.VillainBetCheckBetPenalty;
            }
            else
            {
                // Barrel real: bet-bet consecutivo, rango fuerte
                adjustedFoldBelow += _profile.VillainBarrelFoldIncrease;
                adjustedThinValueAbove += _profile.VillainBarrelThinValueIncrease;
            }
        }

        // Sizing tell: villain escaló bet size entre streets → rango más fuerte
        if (isFacingBet && street >= BoardPosition.Turn)
        {
            bool villainEscalatedSizing =
                (street == BoardPosition.Turn && villainBetSizeFlop != BetSizeCategory.NoBet &&
                 villainBetSize > villainBetSizeFlop) ||
                (street == BoardPosition.River && villainBetSizeTurn != BetSizeCategory.NoBet &&
                 villainBetSize > villainBetSizeTurn);

            if (villainEscalatedSizing)
                adjustedFoldBelow += _profile.VillainSizingEscalationPenalty;
        }

        // Ajuste por oponente: stats reales si disponibles, fallback a tipo estático
        if (villainFoldToBetPct >= 0)
        {
            // Stats reales del OpponentTracker (10+ acciones facing bet)
            double foldAdj = villainFoldToBetPct > 60 ? -5.0
                           : villainFoldToBetPct > 45 ? -2.0
                           : villainFoldToBetPct < 30 ? 4.0
                           : villainFoldToBetPct < 40 ? 2.0
                           : 0.0;
            adjustedFoldBelow += foldAdj;
        }
        else if (villainType != OpponentType.Unknown)
        {
            // Fallback: multipliers estáticos por tipo de oponente
            var (opponentFoldAdj, opponentValueAdj) = villainType switch
            {
                OpponentType.LP => (-4.0, -2.0),
                OpponentType.TP when isFacingBet => (3.0, 2.0),
                OpponentType.TP => (0.0, 0.0),
                OpponentType.TAG => (0.0, 0.0),
                OpponentType.LAG when isFacingBet => (-5.0, -3.0),
                OpponentType.LAG => (2.0, 0.0),
                _ => (0.0, 0.0)
            };
            adjustedFoldBelow += opponentFoldAdj;
            adjustedThinValueAbove += opponentValueAdj;
        }

        // SPR-aware: ajustar thresholds por profundidad de stack (solo turn/river)
        var (sprFoldAdjust, sprValueAdjust, isPushFold) = GetSPRAdjustment(heroStack, potSize, street);
        adjustedFoldBelow += sprFoldAdjust;
        adjustedThinValueAbove += sprValueAdjust;

        // C-bet: agresor preflop con equity baja apuesta por continuación (frecuencia > bluff puro)
        if (!isFacingBet && heroIsAggressor && !isMultiway &&
            effectiveEquity < adjustedFoldBelow && effectiveEquity > adjustedFoldBelow - 15)
        {
            double cbetFreq = GetCbetFrequency(street);
            if (cbetFreq > 0 && Random.Shared.NextDouble() < cbetFreq)
            {
                var cbetSize = AdjustBetSizeForSPR(thresholds.BluffBetSize, heroStack, potSize, street);
                return new PostflopDecisionResult(
                    cbetSize + " (C-Bet)",
                    $"Bet — continuation bet como agresor ({cbetFreq:P0})",
                    IsBarrel: previousStreetBet);
            }
        }

        // Equity baja
        if (effectiveEquity < adjustedFoldBelow)
            return HandleLowEquity(effectiveEquity, thresholds, isInPosition, boardTexture,
                villainBetSize, street, potOdds, totalOuts, isFacingBet, impliedOddsFactor, isMultiway,
                heroHandRank, boardChange, heroBlocksDangerSuit, pairClassification, foldEquity,
                heroStack, potSize, villainType, heroBlocksTopCard);

        // --- FACING BET ---
        if (isFacingBet)
        {
            // Push/fold mode: SPR corto → all-in si EV > 0
            if (isPushFold)
            {
                double allinEV = CalculateAllinEV(effectiveEquity, heroStack, potSize);
                if (allinEV > 0)
                    return new PostflopDecisionResult("All-In (Value)",
                        $"Push +EV — SPR corto (EV={allinEV:F1}, equity={effectiveEquity:F1}%)");
            }

            return HandleFacingBet(effectiveEquity, thresholds, isInPosition, villainBetSize,
                street, potOdds, adjustedThinValueAbove, previousStreetBet, impliedOddsFactor,
                heroIsAggressor, heroHandRank, totalOuts, pairClassification,
                hasFlushDraw, hasComboDraw, heroStack, potSize,
                boardChange, heroBlocksDangerSuit);
        }

        // --- NO FACING BET ---
        // Push/fold mode: SPR corto → all-in si EV > 0
        if (isPushFold)
        {
            double allinEV = CalculateAllinEV(effectiveEquity, heroStack, potSize);
            if (allinEV > 0)
                return new PostflopDecisionResult("All-In (Value)",
                    $"Push +EV — SPR corto (EV={allinEV:F1}, equity={effectiveEquity:F1}%)");
        }

        // C-bet mixing: agresor con equity media puede chequear para proteger checking range
        // Solo HU (no multiway) y rango entre FoldBelow y ThinValueAbove
        if (heroIsAggressor && !isMultiway &&
            effectiveEquity >= adjustedFoldBelow && effectiveEquity < adjustedThinValueAbove)
        {
            double cbetFreq = GetCbetFrequency(street);
            if (cbetFreq > 0 && Random.Shared.NextDouble() >= cbetFreq)
            {
                return new PostflopDecisionResult("Check",
                    $"Check — protección de range como agresor ({1 - cbetFreq:P0} check freq)");
            }
        }

        return HandleNoBet(effectiveEquity, thresholds, isInPosition, boardTexture,
            street, previousStreetBet, heroIsAggressor, heroHandRank, isMultiway,
            villainAggressorCheckedPreviousStreet, heroStack, potSize,
            boardChange, hasFlushDraw, numOpponents, pairClassification, villainType,
            heroFloatedFlop, hasComboDraw, totalOuts, heroKickerStrength,
            heroBlocksDangerSuit, turnCalledWithFlushDanger, heroBlocksTopCard,
            heroCheckedAllStreets);
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
        HandRank heroHandRank = HandRank.HighCard,
        int totalOuts = 0,
        PairClassification pairClassification = PairClassification.None,
        bool hasFlushDraw = false,
        bool hasComboDraw = false,
        decimal heroStack = 0,
        decimal potSize = 0,
        BoardChangeResult? boardChange = null,
        bool heroBlocksDangerSuit = false)
    {
        // Pot odds ajustadas por implied odds (factor < 1.0 = necesitas menos equity)
        double adjustedPotOdds = potOdds > 0 ? potOdds * impliedOddsFactor : 0;

        // Board peligroso: 3+ cartas mismo palo o flush completado, hero sin blocker.
        // Flop: solo monotone (3 same suit, DangerLevel >= 3) es peligroso, no 2 same suit.
        // Turn/River: FlushDrawAppeared (3er palo cayó) o FlushCompleted (4º palo).
        bool dangerousFlushBoard = boardChange != null && !heroBlocksDangerSuit &&
            (boardChange.FlushCompleted ||
             (street == BoardPosition.Flop && boardChange.FlushDrawAppeared && boardChange.DangerLevel >= 3) ||
             (street != BoardPosition.Flop && boardChange.FlushDrawAppeared));

        // Raise vs underbet: villano muestra debilidad → explotar con raise
        if (villainBetSize == BetSizeCategory.Underbet && equity > thresholds.ThinValueAbove)
        {
            return new PostflopDecisionResult("Raise 3x (Value)",
                $"Raise vs underbet — villano muestra debilidad ({heroHandRank})");
        }

        // Equity muy alta → raise solo con mano fuerte
        // Con OnePair: solo puede raise si es TopPair o Overpair; pares débiles solo call
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

            // OnePair: solo Overpair o TopPair pueden raise, SALVO board con flush posible
            if (heroHandRank == HandRank.OnePair &&
                pairClassification >= PairClassification.TopPair &&
                !dangerousFlushBoard)
            {
                var raiseSize = villainBetSize == BetSizeCategory.Large ? "Raise Pot" : "Raise 3x";
                return new PostflopDecisionResult(raiseSize + " (Value)",
                    $"Raise for value vs bet — {pairClassification}");
            }

            // Pares débiles, o OnePair en board con flush posible → call (pot control)
            string parDesc = pairClassification != PairClassification.None
                ? pairClassification.ToString()
                : heroHandRank.ToString();
            string callReason = dangerousFlushBoard
                ? $"Call — pot control, flush posible en board ({parDesc})"
                : $"Call — equity alta pero mano vulnerable ({parDesc})";
            return new PostflopDecisionResult("Call", callReason);
        }

        // Hero agresor vs donk bet → raise con mano fuerte, call con pareja débil
        // En board con flush posible: OnePair solo call (pot control)
        if (heroIsAggressor && equity > thresholds.ValueAbove)
        {
            if (heroHandRank >= HandRank.TwoPair)
                return new PostflopDecisionResult("Raise 3x (Value)",
                    $"Raise — hero agresor vs donk bet ({heroHandRank})");

            // OnePair: Overpair/TopPair pueden raise, SALVO board con flush posible
            if (heroHandRank == HandRank.OnePair &&
                pairClassification >= PairClassification.TopPair &&
                !dangerousFlushBoard)
                return new PostflopDecisionResult("Raise 3x (Value)",
                    $"Raise — hero agresor vs donk bet ({pairClassification})");

            string donkCallReason = dangerousFlushBoard
                ? $"Call — hero agresor vs donk bet, flush posible ({pairClassification})"
                : "Call — hero agresor vs donk bet, mano vulnerable";
            return new PostflopDecisionResult("Call", donkCallReason);
        }

        // Equity buena → call (no raise, el villano ya mostró fuerza)
        if (equity > thresholds.ValueAbove)
        {
            return new PostflopDecisionResult("Call", "Call — equity buena vs bet");
        }

        // Thin value → call si pot odds favorables o mano con potencial
        // Manos fuertes (TopPair+/TwoPair+) son mejores calls que BottomPair/MiddlePair
        if (equity > adjustedThinValueAbove)
        {
            if (adjustedPotOdds > 0 && equity >= adjustedPotOdds)
                return new PostflopDecisionResult("Call",
                    $"Call — implied odds favorables (SPR factor={impliedOddsFactor:F2})");

            // Mano fuerte (TwoPair+ o TopPair): call en cualquier posición
            bool isStrongThinValue = heroHandRank >= HandRank.TwoPair ||
                (heroHandRank == HandRank.OnePair && pairClassification >= PairClassification.TopPair);

            if (isInPosition || isStrongThinValue)
                return new PostflopDecisionResult("Call",
                    isStrongThinValue ? $"Call — thin value ({pairClassification})" : "Call — thin value IP");

            // OOP con mano débil (BottomPair/MiddlePair): más cautela
            bool isWeakHolding = heroHandRank == HandRank.OnePair &&
                pairClassification <= PairClassification.MiddlePair;
            if (isWeakHolding)
                return new PostflopDecisionResult("Fold", $"Fold — thin value OOP con {pairClassification}");

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

        // Floating IP: call en flop con posición y equity marginal para robar en turn
        // Requiere: IP + flop + bet small/medium + sin mano hecha + draw real + equity en rango flotante
        bool hasRealDraw = hasFlushDraw || hasComboDraw || totalOuts >= _profile.FloatingIPMinOuts;
        if (isInPosition && street == BoardPosition.Flop &&
            villainBetSize != BetSizeCategory.Large &&
            heroHandRank <= HandRank.OnePair && totalOuts >= 4 && hasRealDraw &&
            equity >= _profile.FloatingIPMinEquity && equity <= _profile.FloatingIPMaxEquity)
        {
            return new PostflopDecisionResult("Call",
                $"Float IP — call para robar en turn (equity={equity:F1}%, outs={totalOuts})",
                IsFloating: true);
        }

        // Pot commitment: si hero ya está committed (SPR < 0.5) y EV(call) > 0 → call
        if (heroStack > 0 && potSize > 0)
        {
            double spr = (double)(heroStack / potSize);
            if (spr < PokerConstants.PotCommitmentSPRThreshold)
            {
                double callAmount = (double)heroStack;
                double totalPot = (double)potSize + callAmount;
                double evCall = (equity / 100.0) * totalPot - (1.0 - equity / 100.0) * callAmount;
                if (evCall > 0)
                    return new PostflopDecisionResult("Call",
                        $"Call — pot committed (SPR={spr:F2}, EV call={evCall:F1})");
            }
        }

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
        bool isMultiway = false,
        bool villainAggressorCheckedPreviousStreet = false,
        decimal heroStack = 0,
        decimal potSize = 0,
        BoardChangeResult? boardChange = null,
        bool hasFlushDraw = false,
        int numOpponents = 1,
        PairClassification pairClassification = PairClassification.None,
        OpponentType villainType = OpponentType.Unknown,
        bool heroFloatedFlop = false,
        bool hasComboDraw = false,
        int totalOuts = 0,
        KickerStrength heroKickerStrength = KickerStrength.None,
        bool heroBlocksDangerSuit = false,
        bool turnCalledWithFlushDanger = false,
        bool heroBlocksTopCard = false,
        bool heroCheckedAllStreets = false)
    {
        // Turn-river plan: hero calleó turn con flush danger → si river completa flush → check
        if (street == BoardPosition.River && turnCalledWithFlushDanger && boardChange != null &&
            boardChange.FlushCompleted && heroHandRank < HandRank.Flush)
        {
            return new PostflopDecisionResult("Check",
                "Check — flush completó en river, hero calleó turn con peligro");
        }

        // River opportunity: hero completó su draw → bet for value
        // El board cambió a favor de hero (flush/straight completado Y hero lo tiene)
        if (street == BoardPosition.River && boardChange != null &&
            ((boardChange.FlushCompleted && (heroHandRank >= HandRank.Flush || hasFlushDraw)) ||
             (boardChange.StraightCompleted && heroHandRank >= HandRank.Straight)))
        {
            var valueBet = AdjustBetSizeForSPR(thresholds.StrongValueBetSize, heroStack, potSize, street);
            return new PostflopDecisionResult(valueBet + " (Value)",
                $"River opportunity — hero completó draw ({heroHandRank})");
        }

        // River delayed value: hero checkeó todas las calles previas → apostar con mano decente
        if (street == BoardPosition.River && heroCheckedAllStreets &&
            heroHandRank >= HandRank.OnePair && pairClassification >= PairClassification.TopPair &&
            equity >= thresholds.ThinValueAbove)
        {
            var delayedBet = AdjustBetSizeForSPR("Bet 1/3", heroStack, potSize, street);
            return new PostflopDecisionResult(delayedBet + " (Value)",
                $"Bet — delayed value tras check-check ({pairClassification})");
        }

        // Check-raise: OOP con mano premium, esperando bet del villano para raise
        // Prioridad sobre slowplay: check-raise trap es más +EV que slowplay pasivo
        // Check-raise: OOP con mano premium O draw fuerte en flop
        bool hasStrongMade = heroHandRank >= HandRank.TwoPair;
        bool hasStrongDraw = street == BoardPosition.Flop &&
            (hasComboDraw || (hasFlushDraw && totalOuts >= 9)) &&
            equity >= _profile.CheckRaiseDrawMinEquity;

        // IP trap: check-raise con mano premium en board seguro (no Wet/Monotone)
        bool ipTrap = isInPosition && hasStrongMade && !isMultiway &&
            boardTexture != "Wet" && boardTexture != "Monotone";

        // SPR guard: si check-raise compromete el stack, exigir equity premium
        double checkRaiseSPR = heroStack > 0 && potSize > 0 ? (double)(heroStack / potSize) : 99;
        bool lowSPRBlocksCheckRaise = checkRaiseSPR < _profile.CheckRaiseSPRMinThreshold &&
            equity < _profile.CheckRaiseLowSPRMinEquity;

        if (thresholds.CanCheckRaise && !isMultiway &&
            equity > thresholds.CheckRaiseThreshold &&
            !heroIsAggressor && !lowSPRBlocksCheckRaise)
        {
            // OOP: check-raise con mano fuerte o draw fuerte (prioridad)
            if (!isInPosition && (hasStrongMade || hasStrongDraw))
            {
                string reason = hasStrongDraw && !hasStrongMade
                    ? $"Check-raise semi-bluff — {totalOuts} outs OOP"
                    : $"Check-raise trap — {heroHandRank} OOP";
                return new PostflopDecisionResult(
                    "Check (Check-Raise)",
                    reason,
                    IsCheckRaise: true);
            }

            // IP: check-raise trap con TwoPair+ en board seguro
            if (ipTrap)
            {
                return new PostflopDecisionResult(
                    "Check (Check-Raise)",
                    $"Check-raise IP trap — {heroHandRank}",
                    IsCheckRaise: true);
            }
        }

        // Slow play: check con nuts en board seco para inducir bluff del villano
        // Flop: ThreeOfAKind+ en Dry, no agresor, no multiway (cualquier villainType, cualquier posición)
        // Turn: ThreeOfAKind+ en Dry, no agresor, no multiway, OOP, villano LAG o Unknown
        bool isSlowPlayStreet = street == BoardPosition.Flop ||
            (street == BoardPosition.Turn && !isInPosition &&
             (villainType == OpponentType.LAG || villainType == OpponentType.Unknown));

        if (isSlowPlayStreet && boardTexture == "Dry" && !isMultiway &&
            !heroIsAggressor &&
            heroHandRank >= HandRank.ThreeOfAKind &&
            equity >= _profile.SlowPlayMinEquity)
        {
            return new PostflopDecisionResult("Check",
                $"Slow play — {heroHandRank} en board seco{(street == BoardPosition.Turn ? " (turn)" : "")}, inducir bluff");
        }

        // Float exit: hero floateó flop → apostar turn si villano chequea Y runout es favorable
        if (heroFloatedFlop && street == BoardPosition.Turn && !isMultiway)
        {
            // Si hero mejoró a pair+ → value bet, ya no es float exit bluff
            if (heroHandRank >= HandRank.OnePair)
            {
                var valueBet = AdjustBetSizeForSPR("Bet 1/2", heroStack, potSize, street);
                return new PostflopDecisionResult(
                    valueBet + " (Value)",
                    $"Bet — value (mejoró a {heroHandRank} tras float)");
            }

            // Bad runout → abort float exit
            bool badRunout = boardChange != null &&
                (boardChange.OvercardAppeared ||
                 (boardChange.FlushCompleted && !heroBlocksDangerSuit) ||
                 boardChange.StraightCompleted);

            if (badRunout)
            {
                return new PostflopDecisionResult("Check",
                    "Float exit abortado — bad runout");
            }

            var floatBet = AdjustBetSizeForSPR("Bet 1/2", heroStack, potSize, street);
            return new PostflopDecisionResult(
                floatBet + " (Float Exit)",
                "Bet — exit strategy del float IP");
        }

        // Probe bet: villano agresor checkeó en street anterior → debilidad
        // Permitido tanto IP (sizing mayor) como OOP (sizing original)
        if (thresholds.CanProbeBet && villainAggressorCheckedPreviousStreet &&
            !isMultiway &&
            equity >= thresholds.ProbeBetMinEquity)
        {
            var probeSize = isInPosition
                ? thresholds.ProbeBetIPSize
                : thresholds.ProbeBetSize;
            return new PostflopDecisionResult(
                probeSize + " (Probe)",
                $"Probe bet {(isInPosition ? "IP" : "OOP")} — agresor checkeó en street anterior");
        }

        // Determinar bet size base por textura de board y aplicar sizing dinámico
        var baseBetThreshold = boardTexture switch
        {
            "Dry" => thresholds.DryBoardBetSize,
            "Coordinated" => thresholds.CoordinatedBoardBetSize,
            "Paired" => thresholds.PairedBoardBetSize,
            "Monotone" => thresholds.MonotoneBoardBetSize,
            "Wet" => thresholds.WetBoardBetSize,
            _ => thresholds.DryBoardBetSize
        };

        // Ajustar por OOP (antes de dynamic sizing)
        if (thresholds.ReduceSizeForOOP && !isInPosition)
            baseBetThreshold = ReduceBetSize(baseBetThreshold);

        // Sizing dinámico: ajusta por SPR, oponentes, textura, posición
        var baseBet = ApplyDynamicSizing(baseBetThreshold, heroStack, potSize,
            numOpponents, boardTexture, isInPosition);

        // Board paired c-bet reduction: en boards paired, subir umbral de value bet
        // porque villano conecta con trips más a menudo y c-bet es menos efectivo
        double boardPairedAdjust = 0;
        if (boardTexture == "Paired" && heroIsAggressor && heroHandRank < HandRank.ThreeOfAKind)
            boardPairedAdjust = _profile.BoardPairedCbetReduction;

        // Hand strength relativa: ajustar thresholds según vulnerabilidad de la mano
        double vulnerabilityAdjust = GetHandVulnerabilityAdjustment(heroHandRank, boardTexture, pairClassification);
        double adjStrongValue = thresholds.StrongValueAbove + vulnerabilityAdjust + boardPairedAdjust;
        double adjValue = thresholds.ValueAbove + vulnerabilityAdjust + boardPairedAdjust;

        // Overbet con mano premium
        // Flop/Turn: solo boards secos con ventaja de rango.
        // River: NUTS (TwoPair+) en CUALQUIER textura para máximo valor.
        if (thresholds.CanOverbet && equity > thresholds.OverbetMinEquity)
        {
            bool canOverbetHere;
            if (street == BoardPosition.River)
                canOverbetHere = heroHandRank >= HandRank.TwoPair;
            else
                canOverbetHere = boardTexture == "Dry" && heroIsAggressor;

            if (canOverbetHere)
            {
                return new PostflopDecisionResult(
                    thresholds.OverbetBetSize + " (Value)",
                    street == BoardPosition.River
                        ? $"Overbet river — nuts ({heroHandRank})"
                        : "Overbet — board seco con ventaja de rango");
            }
        }

        // River sizing contextual:
        // - Board con flush draw sin blocker → sizing menor (no over-commit)
        // - OnePair → sizing merged (menor), TwoPair+ → sizing polarizado (mayor)
        bool riverDangerBoard = street == BoardPosition.River && boardChange != null &&
            (boardChange.FlushDrawAppeared || boardChange.FlushCompleted) &&
            !heroBlocksDangerSuit;
        bool riverMergedSizing = street == BoardPosition.River && heroHandRank == HandRank.OnePair;

        // Strong value → bet grande (con sizing boost para manos nuts o TPTK)
        if (equity > adjStrongValue)
        {
            bool isBarrel = previousStreetBet && (street == BoardPosition.Turn || street == BoardPosition.River);
            var betSize = heroHandRank >= HandRank.ThreeOfAKind
                ? IncreaseBetSize(thresholds.StrongValueBetSize)
                : thresholds.StrongValueBetSize;
            // River: reducir sizing si board peligroso con OnePair
            if (riverDangerBoard && riverMergedSizing)
                betSize = ReduceBetSize(betSize);
            betSize = AdjustBetSizeForSPR(betSize, heroStack, potSize, street);
            return new PostflopDecisionResult(
                betSize + " (Value)",
                $"Bet — strong value ({heroHandRank}){(riverDangerBoard ? " (sizing reducido, board peligroso)" : "")}",
                IsBarrel: isBarrel);
        }

        // Value → bet (ajustar sizing por SPR)
        if (equity > adjValue)
        {
            var betSize = thresholds.ValueBetSize;
            // Card removal: hero bloquea top card → villain tiene menos combos → sizing mayor
            if (heroBlocksTopCard && heroHandRank >= HandRank.OnePair)
                betSize = IncreaseBetSize(betSize);
            // Turn: vulnerability sizing (OnePair vulnerable en boards peligrosos → sizing menor)
            bool turnVulnerableSizing = street == BoardPosition.Turn &&
                heroHandRank == HandRank.OnePair &&
                (boardTexture == "Coordinated" || boardTexture == "Wet" || boardTexture == "Monotone");
            if (turnVulnerableSizing)
                betSize = ReduceBetSize(betSize);
            // River merged sizing: OnePair → sizing menor, TwoPair+ → normal
            if (riverMergedSizing)
                betSize = ReduceBetSize(betSize);
            // River danger board: reducir sizing adicional
            if (riverDangerBoard && heroHandRank <= HandRank.OnePair)
                betSize = ReduceBetSize(betSize);
            betSize = AdjustBetSizeForSPR(betSize, heroStack, potSize, street);
            string reason = turnVulnerableSizing ? "Bet — value (sizing protectivo)"
                : riverMergedSizing ? "Bet — value (merged sizing)"
                : heroBlocksTopCard ? "Bet — value (blocker, sizing mayor)"
                : "Bet — value";
            return new PostflopDecisionResult(betSize + " (Value)", reason);
        }

        // Pot control: turn con equity marginal en board volátil, no agresor → check-back
        bool isPotControlSpot = street == BoardPosition.Turn && !heroIsAggressor &&
            equity >= PokerConstants.PotControlMinEquity && equity <= PokerConstants.PotControlMaxEquity &&
            (boardTexture == "Coordinated" || boardTexture == "Wet" || boardTexture == "Monotone");
        if (isPotControlSpot)
            return new PostflopDecisionResult("Check",
                "Check — pot control, equity marginal en board volátil");

        // Randomización: equity justo encima del threshold → a veces check (anti-exploit)
        if (equity > thresholds.ThinValueAbove &&
            equity <= thresholds.ThinValueAbove + PokerConstants.RandomizationMargin)
        {
            // Randomización adaptativa por villain type:
            // vs LAG: bet más (él ajusta → randomizar menos, explotar su call frequency)
            // vs TAG/TP: check más (proteger rango, no regalar info)
            double betFreq = villainType switch
            {
                OpponentType.LAG => 0.85,  // Bet 85% vs LAG (menos randomización)
                OpponentType.LP => 0.80,   // Bet 80% vs fish (valor directo)
                OpponentType.TP => 0.55,   // Check 45% vs nit (proteger rango)
                OpponentType.TAG => 0.60,  // Check 40% vs reg (balance GTO)
                _ => PokerConstants.RandomizationBetFrequency // 0.70 default
            };

            if (Random.Shared.NextDouble() > betFreq)
            {
                return new PostflopDecisionResult("Check",
                    $"Check — randomización adaptativa ({villainType}, bet freq {betFreq:P0})");
            }
        }

        // Thin value → bet solo IP (OOP check para proteger rango)
        // River: NO thin value si board tiene draws completados y hero no los tiene
        if (equity > thresholds.ThinValueAbove)
        {
            bool heroHasCompletedDraw = boardChange != null &&
                ((boardChange.FlushCompleted && (heroHandRank >= HandRank.Flush || hasFlushDraw)) ||
                 (boardChange.StraightCompleted && heroHandRank >= HandRank.Straight));
            bool riverDrawCompleted = street == BoardPosition.River && boardChange != null &&
                (boardChange.FlushCompleted || boardChange.StraightCompleted) &&
                !heroHasCompletedDraw;

            if (riverDrawCompleted)
                return new PostflopDecisionResult("Check",
                    "Check — thin value peligroso, draw completado en river");

            // Kicker influence: con OnePair, kicker fuerte apuesta más, débil check
            // TPTK (Strong) → bet normal. TPWK (Weak) OOP → check (showdown value)
            bool weakKickerOOP = heroHandRank == HandRank.OnePair &&
                heroKickerStrength == KickerStrength.Weak && !isInPosition;

            if (weakKickerOOP)
                return new PostflopDecisionResult("Check",
                    "Check — thin value OOP, kicker débil (showdown)");

            if (!thresholds.ThinValueIPOnly || isInPosition)
            {
                // Kicker fuerte → sizing un nivel más alto
                var betSize = thresholds.ThinValueBetSize;
                if (heroHandRank == HandRank.OnePair && heroKickerStrength == KickerStrength.Strong)
                    betSize = IncreaseBetSize(betSize);
                betSize = AdjustBetSizeForSPR(betSize, heroStack, potSize, street);
                bool isBarrel = previousStreetBet && (street == BoardPosition.Turn || street == BoardPosition.River);
                return new PostflopDecisionResult(
                    betSize + " (Thin Value)",
                    heroKickerStrength == KickerStrength.Strong
                        ? "Bet — thin value (kicker fuerte)"
                        : "Bet — thin value",
                    IsBarrel: isBarrel);
            }

            return new PostflopDecisionResult("Check", "Check — thin value OOP (showdown)");
        }

        // Double barrel: hero apostó en street anterior y tiene equity marginal
        // Seguir apostando por consistencia de rango, salvo bad runout
        if (thresholds.CanDoubleBarrel && previousStreetBet && heroIsAggressor &&
            !isMultiway && street != BoardPosition.Flop &&
            equity >= thresholds.FoldBelow && equity < thresholds.ValueAbove)
        {
            // Bad runout: overcard, draw completado, board paireó → check (no barrelear)
            bool badRunout = boardChange != null &&
                (boardChange.OvercardAppeared || boardChange.FlushCompleted ||
                 boardChange.StraightCompleted || boardChange.BoardPaired);

            if (badRunout)
            {
                return new PostflopDecisionResult("Check",
                    "Check — bad runout, no barrel (overcard/draw/pair)");
            }

            var barrelBet = boardTexture == "Dry"
                ? thresholds.ThinValueBetSize
                : thresholds.BluffBetSize;
            barrelBet = AdjustBetSizeForSPR(barrelBet, heroStack, potSize, street);
            return new PostflopDecisionResult(
                barrelBet + " (Barrel)",
                "Double barrel — brick, consistencia de rango",
                IsBarrel: true);
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
    /// Para OnePair usa PairClassification para distinguir Overpair de BottomPair.
    /// </summary>
    private static double GetHandVulnerabilityAdjustment(HandRank rank, string boardTexture,
        PairClassification pairClassification = PairClassification.None)
    {
        if (rank == HandRank.OnePair)
        {
            return pairClassification switch
            {
                PairClassification.Overpair => 0.8,
                PairClassification.TopPair => 1.5,
                PairClassification.MiddlePair => 2.0,
                PairClassification.PocketPairUnder => 2.5,
                PairClassification.BottomPair => 3.0,
                PairClassification.BoardPaired => 3.5,
                _ => 2.0  // None / fallback
            };
        }

        double factor = rank switch
        {
            HandRank.RoyalFlush or HandRank.StraightFlush => -8.0,
            HandRank.FourOfAKind => -8.0,
            HandRank.FullHouse => -5.0,
            HandRank.Flush => boardTexture == "Paired" ? -2.0 : -4.0,
            HandRank.Straight => boardTexture == "Coordinated" ? 2.0 : -2.0,
            HandRank.ThreeOfAKind => -4.0,
            HandRank.TwoPair => boardTexture == "Coordinated" ? 3.0 : 0.0,
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
    /// Ajusta bet sizing por profundidad de stack (SPR). Solo en turn/river.
    /// SPR corto → apostar más grande (pot commit). SPR profundo → más pequeño (control).
    /// </summary>
    private string AdjustBetSizeForSPR(string baseBet, decimal heroStack, decimal potSize, BoardPosition street)
    {
        if (heroStack <= 0 || potSize <= 0 || street == BoardPosition.Flop)
            return baseBet;

        double spr = (double)(heroStack / potSize);

        if (spr <= 1.0)
            return IncreaseBetSize(IncreaseBetSize(baseBet));
        if (spr <= _profile.SPRPushFoldThreshold)
            return IncreaseBetSize(baseBet);
        if (spr >= _profile.SPRDeepCautionThreshold)
            return ReduceBetSize(baseBet);

        return baseBet;
    }

    /// <summary>
    /// Calcula ajustes de threshold por SPR. Solo en turn/river.
    /// SPR corto: bajar FoldBelow (commit más fácil). SPR profundo: subir FoldBelow (más cautela).
    /// </summary>
    private (double foldAdjust, double valueAdjust, bool isPushFold) GetSPRAdjustment(
        decimal heroStack, decimal potSize, BoardPosition street)
    {
        if (heroStack <= 0 || potSize <= 0 || street == BoardPosition.Flop)
            return (0, 0, false);

        double spr = (double)(heroStack / potSize);

        // Interpolación suave en zona push/fold (SPR < threshold)
        if (spr < _profile.SPRPushFoldThreshold)
        {
            // factor va de 1.0 (SPR=0) a 0.0 (SPR=threshold)
            double factor = 1.0 - spr / _profile.SPRPushFoldThreshold;
            double foldAdj = -_profile.SPRPushFoldFoldReduction * factor;
            double valueAdj = _profile.SPRPushFoldValueIncrease * factor;
            // isPushFold solo en mitad inferior de la zona (SPR muy corto)
            bool isPF = spr < _profile.SPRPushFoldThreshold * 0.5;
            return (foldAdj, valueAdj, isPF);
        }

        // Interpolación suave en zona deep (SPR > threshold)
        if (spr > _profile.SPRDeepCautionThreshold)
        {
            // factor va de 0.0 (SPR=threshold) a 1.0 (SPR=threshold+2)
            double factor = Math.Min((spr - _profile.SPRDeepCautionThreshold) / 2.0, 1.0);
            return (_profile.SPRDeepFoldIncrease * factor, 0, false);
        }

        return (0, 0, false);
    }

    /// <summary>
    /// Calcula EV de ir all-in vs fold. Usado cuando SPR &lt; 2.0.
    /// EV = P(win) × ganancia_neta - P(lose) × pérdida
    ///    = (equity/100) × (pot + stack) - (1 - equity/100) × stack
    ///    = (equity/100) × (pot + 2×stack) - stack
    /// Si EV > 0, all-in es +EV independientemente de HandRank.
    /// </summary>
    internal static double CalculateAllinEV(double equity, decimal heroStack, decimal potSize)
    {
        if (heroStack <= 0 || potSize <= 0) return 0;
        double pot = (double)potSize;
        double stack = (double)heroStack;
        double equityFraction = equity / 100.0;
        return equityFraction * (pot + stack) - (1.0 - equityFraction) * stack;
    }

    /// <summary>
    /// Calcula penalización por reverse implied odds. Delega a ImpliedOddsCalculator.
    /// </summary>
    public double CalculateReverseImpliedOdds(
        BoardChangeResult? boardChange, HandRank heroHandRank, bool hasFlushDraw,
        BoardPosition street, bool isFacingBet,
        PairClassification pairClassification = PairClassification.None,
        bool heroBlocksDangerSuit = false)
        => ImpliedOddsCalculator.CalculateReverseImpliedOdds(
            boardChange, heroHandRank, hasFlushDraw, street, isFacingBet, _profile, pairClassification,
            heroBlocksDangerSuit);

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
        bool isMultiway = false,
        HandRank heroHandRank = HandRank.HighCard,
        BoardChangeResult? boardChange = null,
        bool heroBlocksDangerSuit = false,
        PairClassification pairClassification = PairClassification.None,
        double foldEquity = 0,
        decimal heroStack = 0,
        decimal potSize = 0,
        OpponentType villainType = OpponentType.Unknown,
        bool heroBlocksTopCard = false)
    {
        // Semi-bluff con draws (solo si NO estamos facing a bet y no multiway)
        // Verificar fold equity: semi-bluff debe ser +EV considerando equity del draw como backup
        if (totalOuts >= PokerConstants.MinOutsForDraw && street != BoardPosition.River && !isFacingBet && !isMultiway)
        {
            bool isComboDrawOnFlop = totalOuts >= thresholds.ComboDrawOutsThreshold
                && street == BoardPosition.Flop;
            var semiBluffSize = isComboDrawOnFlop
                ? thresholds.ComboDrawBetSize
                : thresholds.BluffBetSize;

            // Fold equity check: draw equity reduce el breakeven FE necesario
            double betFraction = BetStringToFraction(semiBluffSize);
            double breakevenFE = betFraction / (1.0 + betFraction);
            double drawEquity = totalOuts * (street == BoardPosition.Turn
                ? PokerConstants.TurnOutsMultiplier
                : PokerConstants.RiverOutsMultiplier) / 100.0;
            double adjustedBreakevenFE = Math.Max(0, breakevenFE - drawEquity);
            double actualFE = foldEquity / 100.0;

            if (actualFE >= adjustedBreakevenFE)
            {
                return new PostflopDecisionResult(
                    semiBluffSize + " (Semi-Bluff)",
                    isComboDrawOnFlop
                        ? $"Semi-bluff agresivo: combo draw {totalOuts} outs (FE={foldEquity:F0}%)"
                        : $"Semi-bluff +EV: {totalOuts} outs (FE={foldEquity:F0}% >= {adjustedBreakevenFE * 100:F0}%)",
                    IsBluff: true);
            }
            // Fold equity insuficiente → no semi-bluff, seguir al siguiente path
        }

        // Con draws y facing bet → call si implied odds lo justifican
        if (totalOuts >= PokerConstants.MinOutsForDraw && street != BoardPosition.River && isFacingBet)
        {
            double adjustedPotOdds = potOdds > 0 ? potOdds * impliedOddsFactor : 999;
            double drawEquity = totalOuts * (street == BoardPosition.Turn ? PokerConstants.TurnOutsMultiplier : PokerConstants.RiverOutsMultiplier);
            if (drawEquity >= adjustedPotOdds)
                return new PostflopDecisionResult("Call",
                    $"Call — draw con {totalOuts} outs (implied odds, SPR factor={impliedOddsFactor:F2})");
        }

        // Bluff puro (sin facing bet, no bluffear multiway OOP)
        // Verificar fold equity mínima: bluff debe ser +EV (fold equity >= breakeven threshold)
        bool canBluffHere = !isFacingBet && thresholds.CanBluff &&
            !(isMultiway && !isInPosition);
        if (canBluffHere && ShouldBluff(thresholds, isInPosition, boardTexture, street))
        {
            double betFraction = BetStringToFraction(thresholds.BluffBetSize);
            double breakevenFoldEquity = betFraction / (1.0 + betFraction);

            // Modular por SPR: short stacks → bluff menos rentable, deep → más rentable
            double sprBluffMultiplier = 1.0;
            if (heroStack > 0 && potSize > 0)
            {
                double spr = (double)(heroStack / potSize);
                if (spr < _profile.BluffSPRShortThreshold)
                    sprBluffMultiplier = _profile.BluffSPRShortMultiplier;
                else if (spr > _profile.BluffSPRDeepThreshold)
                    sprBluffMultiplier = _profile.BluffSPRDeepMultiplier;
            }
            double adjustedBreakevenFE = breakevenFoldEquity / sprBluffMultiplier;
            double actualFoldEquity = foldEquity / 100.0;

            if (actualFoldEquity >= adjustedBreakevenFE)
            {
                return new PostflopDecisionResult(
                    thresholds.BluffBetSize + " (Bluff)",
                    $"Bluff +EV (fold equity={foldEquity:F0}% >= {breakevenFoldEquity * 100:F0}%)",
                    IsBluff: true);
            }
        }

        // Pot odds marginales con implied odds
        if (isFacingBet && potOdds > 0 && equity >= potOdds * PokerConstants.MarginalPotOddsFactor * impliedOddsFactor)
        {
            return new PostflopDecisionResult("Call",
                $"Call — pot odds marginales (implied factor={impliedOddsFactor:F2})");
        }

        // Sin facing bet → check (no fold sin apuesta)
        if (!isFacingBet)
            return new PostflopDecisionResult("Check", "Check — equity baja");

        // Bluff catching en turn+river: hero con pareja decente puede call para atrapar bluffs
        // Turn: solo Small bet, MiddlePair+ (más riesgo con 1 calle por venir)
        // River: Small/Medium bet, cualquier OnePair+ (incluye BottomPair con umbral ajustado)
        // BoardPaired: hero no tiene par real — no bluff catch
        bool isTurnBluffCatch = street == BoardPosition.Turn &&
            villainBetSize == BetSizeCategory.Small;
        bool isRiverBluffCatch = street == BoardPosition.River &&
            villainBetSize != BetSizeCategory.Large;

        if ((isTurnBluffCatch || isRiverBluffCatch) &&
            heroHandRank >= HandRank.OnePair &&
            pairClassification != PairClassification.BoardPaired)
        {
            // Turn: BottomPair demasiado débil con 1 calle por venir → skip
            if (isTurnBluffCatch && pairClassification == PairClassification.BottomPair)
                goto skipBluffCatch;

            // Umbral más estricto en turn (0.90) que en river (0.75)
            double baseMultiplier = isTurnBluffCatch
                ? _profile.BluffCatchTurnEquityMultiplier
                : _profile.BluffCatchFoldBelowMultiplier;
            double bluffCatchThreshold = thresholds.FoldBelow * baseMultiplier;

            // Ajustar por tipo de oponente: LAG bluffea más → call más amplio, TP → fold más
            double opponentBluffMultiplier = villainType switch
            {
                OpponentType.LAG => PokerConstants.BluffCatchLAGMultiplier,
                OpponentType.LP => PokerConstants.BluffCatchLPMultiplier,
                OpponentType.TAG => PokerConstants.BluffCatchTAGMultiplier,
                OpponentType.TP => PokerConstants.BluffCatchTPMultiplier,
                _ => 1.0
            };
            bluffCatchThreshold *= opponentBluffMultiplier;

            // Runout factor river: brick = villain falló draw → bluff catch más amplio
            if (isRiverBluffCatch && boardChange != null)
            {
                bool isBrickRiver = !boardChange.FlushCompleted && !boardChange.StraightCompleted &&
                    !boardChange.BoardPaired && !boardChange.OvercardAppeared;
                if (isBrickRiver)
                    bluffCatchThreshold *= PokerConstants.BluffCatchBrickRunoutMultiplier;
                else if (boardChange.FlushCompleted || boardChange.StraightCompleted)
                    bluffCatchThreshold *= PokerConstants.BluffCatchScareRunoutMultiplier;
            }

            // Blocker bonus: hero bloquea draws completados del villano → villano más probable bluffeando
            bool hasBlocker = heroBlocksDangerSuit ||
                (boardChange != null && boardChange.StraightCompleted && heroHandRank >= HandRank.Straight);
            if (hasBlocker)
                bluffCatchThreshold *= 0.85;

            // Card removal: hero bloquea top card del board → villain tiene menos combos value
            if (heroBlocksTopCard)
                bluffCatchThreshold *= 0.90;

            // BottomPair sin blocker en river: umbral más exigente (fold más a menudo)
            if (isRiverBluffCatch && pairClassification == PairClassification.BottomPair && !hasBlocker)
                bluffCatchThreshold *= 1.15;

            if (equity >= bluffCatchThreshold)
            {
                string parLabel = pairClassification != PairClassification.None
                    ? pairClassification.ToString()
                    : heroHandRank.ToString();
                string streetLabel = isTurnBluffCatch ? "turn" : "river";
                var reason = hasBlocker
                    ? $"Call — bluff catch {streetLabel} con blocker ({parLabel})"
                    : $"Call — bluff catch {streetLabel} ({parLabel})";
                return new PostflopDecisionResult("Call", reason);
            }
        }
    skipBluffCatch:

        // Pot commitment: si hero ya está committed (SPR < 0.5) y EV(call) > 0 → call
        if (isFacingBet && heroStack > 0 && potSize > 0)
        {
            double spr = (double)(heroStack / potSize);
            if (spr < PokerConstants.PotCommitmentSPRThreshold)
            {
                double callAmount = (double)heroStack;
                double totalPot = (double)potSize + callAmount;
                double evCall = (equity / 100.0) * totalPot - (1.0 - equity / 100.0) * callAmount;
                if (evCall > 0)
                    return new PostflopDecisionResult("Call",
                        $"Call — pot committed (SPR={spr:F2}, EV call={evCall:F1})");
            }
        }

        // Facing bet → fold o call según config
        var fallback = thresholds.LowEquityAction == "Call" ? "Call" : "Fold";
        return new PostflopDecisionResult(fallback, "Equity baja vs bet");
    }

    private bool ShouldBluff(StreetThresholds thresholds, bool isInPosition, string boardTexture, BoardPosition street)
    {
        var bluffFreq = GetBluffFrequency(street) * thresholds.BluffFrequencyMultiplier;

        return thresholds.BluffCondition switch
        {
            BluffConditionType.Always => Random.Shared.NextDouble() < bluffFreq,
            BluffConditionType.OOPOnly => !isInPosition && Random.Shared.NextDouble() < bluffFreq,
            // Antes requería betSize == Small (imposible sin facing bet). Ahora: IP + Coordinated board.
            BluffConditionType.IPCoordinatedSmallOnly => isInPosition && boardTexture == "Coordinated" && Random.Shared.NextDouble() < bluffFreq,
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

    private double GetCbetFrequency(BoardPosition street) => street switch
    {
        BoardPosition.Flop => _profile.CbetFrequencyFlop,
        BoardPosition.Turn => _profile.CbetFrequencyTurn,
        BoardPosition.River => _profile.CbetFrequencyRiver,
        _ => 0.0
    };

    private static PostflopDecisionResult DetermineSimplifiedAction(double equity, StreetThresholds thresholds,
        bool isInPosition, bool isFacingBet, string boardTexture = "Dry")
    {
        // Adaptar sizing por board texture en modo simplificado (Monotone/Wet → sizing menor)
        string AdjustForTexture(string defaultBet) => !isFacingBet ? boardTexture switch
        {
            "Monotone" => "Bet 1/4 (Value)",
            "Wet" => "Bet 1/3 (Value)",
            _ => defaultBet
        } : defaultBet;

        if (isInPosition)
        {
            if (equity > thresholds.StrongValueAbove)
                return new PostflopDecisionResult(
                    isFacingBet ? "Raise 3x (Value)" : AdjustForTexture(thresholds.SimplifiedIPStrongBet),
                    "Strong value IP");
            if (equity > thresholds.ThinValueAbove)
                return new PostflopDecisionResult(
                    isFacingBet ? "Call" : AdjustForTexture(thresholds.SimplifiedIPThinBet),
                    isFacingBet ? "Call — thin value IP" : "Thin value IP");
            return new PostflopDecisionResult(
                isFacingBet ? "Fold" : "Check",
                "Equity baja IP");
        }

        if (equity > thresholds.StrongValueAbove)
            return new PostflopDecisionResult(
                isFacingBet ? "Raise 3x (Value)" : AdjustForTexture(thresholds.SimplifiedOOPStrongBet),
                "Strong value OOP");
        if (equity > thresholds.ValueAbove)
            return new PostflopDecisionResult(
                isFacingBet ? "Call" : AdjustForTexture(thresholds.SimplifiedOOPValueBet),
                isFacingBet ? "Call — value OOP" : "Value OOP");
        if (equity > thresholds.ThinValueAbove)
            return new PostflopDecisionResult(
                isFacingBet ? "Call" : AdjustForTexture(thresholds.SimplifiedOOPThinBet),
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

    /// <summary>
    /// Convierte un bet string del threshold a un tamaño numérico base para BetSizingService.
    /// </summary>
    private static double BetStringToFraction(string bet)
    {
        if (bet.Contains("Pot")) return 1.0;
        if (bet.Contains("3/4")) return 0.75;
        if (bet.Contains("2/3")) return 0.67;
        if (bet.Contains("1/2")) return 0.50;
        if (bet.Contains("1/3")) return 0.33;
        if (bet.Contains("1/4")) return 0.25;
        if (bet.Contains("1.25x")) return 1.25;
        return 0.50;
    }

    /// <summary>
    /// Aplica BetSizingService dinámico al bet string del threshold.
    /// Ajusta por SPR, oponentes, textura, posición.
    /// </summary>
    private string ApplyDynamicSizing(string baseBet, decimal heroStack, decimal potSize,
        int numOpponents, string boardTexture, bool isInPosition)
    {
        double baseFraction = BetStringToFraction(baseBet);
        bool isPaired = boardTexture == "Paired";
        bool isCoordinated = boardTexture == "Coordinated";
        bool isDry = boardTexture == "Dry";

        var dynamicBet = _betSizingService.CalculateDynamicBetSize(
            baseFraction, heroStack, potSize, numOpponents, isPaired, isCoordinated, isDry, isInPosition);

        return dynamicBet;
    }

    /// <summary>
    /// Obtiene el ajuste de thresholds según el tipo de rango determinado por RangePolarizer.
    /// </summary>
    private (double foldBelowAdjust, double thinValueAdjust) GetRangeBasedThresholdAdjustment(
        string boardTexture, bool isInPosition, decimal potSize, decimal heroStack, BoardPosition street)
    {
        var textureCategory = ConvertToBoardTextureCategory(boardTexture);
        double spr = potSize > 0 ? (double)(heroStack / potSize) : 10;
        return _rangePolarizer.GetThresholdAdjustmentBySituation(textureCategory, isInPosition, spr, street);
    }

    /// <summary>
    /// Convierte el string de board texture a BoardTextureCategory enum.
    /// </summary>
    private static BoardTextureCategory ConvertToBoardTextureCategory(string texture)
    {
        return texture?.ToLower() switch
        {
            "dry" => BoardTextureCategory.Dry,
            "paired" => BoardTextureCategory.Paired,
            "wet" => BoardTextureCategory.Wet,
            "coordinated" => BoardTextureCategory.SemiWet,
            "monotone" => BoardTextureCategory.Wet,
            "semidry" => BoardTextureCategory.SemiDry,
            "semiwet" => BoardTextureCategory.SemiWet,
            _ => BoardTextureCategory.Dry
        };
    }
}
