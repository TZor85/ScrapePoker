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
    bool IsCheckRaise = false,
    bool IsFloating = false);

public class PostflopDecisionService
{
    private readonly StrategyProfile _profile;
    private readonly BetSizingService _betSizingService;

    public PostflopDecisionService(IOptions<StrategyProfile> profileOptions, BetSizingService betSizingService)
    {
        _profile = profileOptions.Value;
        _betSizingService = betSizingService;
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
    public double CalculateDangerPenalty(double rawEquity, BoardChangeResult boardChange, bool heroBlocksDangerSuit, bool isFacingBet, BoardPosition street = BoardPosition.Turn, bool heroHasNutBlocker = false)
        => DangerPenaltyCalculator.Calculate(rawEquity, boardChange, heroBlocksDangerSuit, isFacingBet, _profile, street, heroHasNutBlocker);

    /// <summary>
    /// Calcula el factor de implied odds. Delega a ImpliedOddsCalculator.
    /// </summary>
    public double CalculateImpliedOddsFactor(
        BoardPosition street,
        bool isInPosition,
        bool hasFlushDraw,
        decimal heroStack = 0,
        decimal potSize = 0)
        => ImpliedOddsCalculator.CalculateImpliedOddsFactor(
            street, isInPosition, hasFlushDraw, heroStack, potSize, _profile);

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
        bool heroHasNutBlocker = false)
    {
        var thresholds = GetThresholds(street, situation);
        bool isFacingBet = villainBetSize != BetSizeCategory.NoBet;
        bool isMultiway = numOpponents >= 2;

        // Calcular implied odds factor
        double impliedOddsFactor = CalculateImpliedOddsFactor(
            street, isInPosition, hasFlushDraw, heroStack, potSize);

        // Aplicar penalización por carta peligrosa (escalada por street, blocker granular)
        double dangerPenalty = boardChange != null
            ? CalculateDangerPenalty(equity, boardChange, heroBlocksDangerSuit, isFacingBet, street, heroHasNutBlocker)
            : 0;
        double effectiveEquity = equity - dangerPenalty;

        // Combo draw bonus: flush + straight draw = semi-bluff premium.
        // No aplicar si hero ya completó el draw (bonus es para draws pendientes).
        if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
            effectiveEquity += _profile.ComboDrawEquityBonus;

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
        double reverseImpliedPenalty = CalculateReverseImpliedOdds(
            boardChange, heroHandRank, hasFlushDraw, street, isFacingBet, pairClassification,
            heroBlocksDangerSuit);
        effectiveEquity -= reverseImpliedPenalty;

        // Modo simplificado (RaiseOverLimper)
        if (thresholds.IsSimplified)
            return DetermineSimplifiedAction(effectiveEquity, thresholds, isInPosition, isFacingBet, boardTexture);

        // Ajustar thresholds si estamos facing a bet
        double adjustedFoldBelow = thresholds.FoldBelow;
        double adjustedThinValueAbove = thresholds.ThinValueAbove;
        if (isFacingBet)
        {
            double facingBetPenalty = villainBetSize switch
            {
                BetSizeCategory.Large => PokerConstants.FacingBetPenaltyLarge,
                BetSizeCategory.Medium => PokerConstants.FacingBetPenaltyMedium,
                BetSizeCategory.Small => PokerConstants.FacingBetPenaltySmall,
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

        // Multi-way penalty: IP puede aislar, OOP muy vulnerable
        if (isMultiway)
        {
            int extraOpponents = numOpponents - 1;
            double multiwayFoldPenalty = isInPosition
                ? PokerConstants.MultiwayFoldBelowIP
                : PokerConstants.MultiwayFoldBelowOOP;
            double multiwayValuePenalty = isInPosition
                ? PokerConstants.MultiwayThinValueIP
                : PokerConstants.MultiwayThinValueOOP;
            adjustedFoldBelow += extraOpponents * multiwayFoldPenalty;
            adjustedThinValueAbove += extraOpponents * multiwayValuePenalty;
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

        // Ajuste por tipo de oponente (requiere perfil fiable con 20+ manos)
        if (villainType != OpponentType.Unknown)
        {
            var (opponentFoldAdj, opponentValueAdj) = villainType switch
            {
                // Fish (LP): apuesta amplia, foldea mucho → bajar FoldBelow, bluffear más
                OpponentType.LP => (-4.0, -2.0),
                // Nit (TP): rango estrecho, apuesta = mano fuerte → subir FoldBelow
                OpponentType.TP when isFacingBet => (3.0, 2.0),
                OpponentType.TP => (0.0, 0.0),
                // TAG: juego sólido, resiste → ajuste neutro
                OpponentType.TAG => (0.0, 0.0),
                // LAG: agresivo, bluffea mucho → bajar FoldBelow para call más (atrapar bluffs)
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

        // Equity baja
        if (effectiveEquity < adjustedFoldBelow)
            return HandleLowEquity(effectiveEquity, thresholds, isInPosition, boardTexture,
                villainBetSize, street, potOdds, totalOuts, isFacingBet, impliedOddsFactor, isMultiway,
                heroHandRank, boardChange, heroBlocksDangerSuit, pairClassification, foldEquity,
                heroStack, potSize);

        // --- FACING BET ---
        if (isFacingBet)
        {
            // Push/fold mode: SPR corto con equity suficiente → all-in
            if (isPushFold && effectiveEquity > thresholds.ValueAbove && heroHandRank >= HandRank.OnePair)
                return new PostflopDecisionResult("All-In (Value)", $"Push — SPR corto, committed ({heroHandRank})");

            return HandleFacingBet(effectiveEquity, thresholds, isInPosition, villainBetSize,
                street, potOdds, adjustedThinValueAbove, previousStreetBet, impliedOddsFactor,
                heroIsAggressor, heroHandRank, totalOuts, pairClassification,
                hasFlushDraw, hasComboDraw);
        }

        // --- NO FACING BET ---
        // Push/fold mode: SPR corto con equity suficiente → all-in
        if (isPushFold && effectiveEquity > thresholds.ValueAbove && heroHandRank >= HandRank.OnePair)
            return new PostflopDecisionResult("All-In (Value)", $"Push — SPR corto, polarizado ({heroHandRank})");

        return HandleNoBet(effectiveEquity, thresholds, isInPosition, boardTexture,
            street, previousStreetBet, heroIsAggressor, heroHandRank, isMultiway,
            villainAggressorCheckedPreviousStreet, heroStack, potSize,
            boardChange, hasFlushDraw, numOpponents, pairClassification, villainType);
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
        bool hasComboDraw = false)
    {
        // Pot odds ajustadas por implied odds (factor < 1.0 = necesitas menos equity)
        double adjustedPotOdds = potOdds > 0 ? potOdds * impliedOddsFactor : 0;

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

            // OnePair: solo Overpair o TopPair con kicker fuerte pueden raise
            if (heroHandRank == HandRank.OnePair &&
                pairClassification >= PairClassification.TopPair)
            {
                var raiseSize = villainBetSize == BetSizeCategory.Large ? "Raise Pot" : "Raise 3x";
                return new PostflopDecisionResult(raiseSize + " (Value)",
                    $"Raise for value vs bet — {pairClassification}");
            }

            // Pares débiles con equity alta → call (no hinchar pote con mano vulnerable)
            string parDesc = pairClassification != PairClassification.None
                ? pairClassification.ToString()
                : heroHandRank.ToString();
            return new PostflopDecisionResult("Call",
                $"Call — equity alta pero mano vulnerable ({parDesc})");
        }

        // Hero agresor vs donk bet → raise con mano fuerte, call con pareja débil
        if (heroIsAggressor && equity > thresholds.ValueAbove)
        {
            if (heroHandRank >= HandRank.TwoPair)
                return new PostflopDecisionResult("Raise 3x (Value)",
                    $"Raise — hero agresor vs donk bet ({heroHandRank})");

            // OnePair: Overpair/TopPair pueden raise, el resto call
            if (heroHandRank == HandRank.OnePair &&
                pairClassification >= PairClassification.TopPair)
                return new PostflopDecisionResult("Raise 3x (Value)",
                    $"Raise — hero agresor vs donk bet ({pairClassification})");

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
        OpponentType villainType = OpponentType.Unknown)
    {
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

        // Check-raise: OOP con mano premium, esperando bet del villano para raise
        // Prioridad sobre slowplay: check-raise trap es más +EV que slowplay pasivo
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

        // Overbet en boards secos con mano premium
        // Flop/Turn: agresor con ventaja de rango. River: NUTS (TwoPair+) para máximo valor.
        if (thresholds.CanOverbet && equity > thresholds.OverbetMinEquity && boardTexture == "Dry")
        {
            bool canOverbetHere = street == BoardPosition.River
                ? heroHandRank >= HandRank.TwoPair
                : heroIsAggressor;

            if (canOverbetHere)
            {
                return new PostflopDecisionResult(
                    thresholds.OverbetBetSize + " (Value)",
                    street == BoardPosition.River
                        ? $"Overbet river — NUTS en board seco ({heroHandRank})"
                        : "Overbet — board seco con ventaja de rango");
            }
        }

        // Strong value → bet grande (con sizing boost para manos nuts o TPTK)
        if (equity > adjStrongValue)
        {
            bool isBarrel = previousStreetBet && (street == BoardPosition.Turn || street == BoardPosition.River);
            var betSize = heroHandRank >= HandRank.ThreeOfAKind
                ? IncreaseBetSize(thresholds.StrongValueBetSize)
                : thresholds.StrongValueBetSize;
            betSize = AdjustBetSizeForSPR(betSize, heroStack, potSize, street);
            return new PostflopDecisionResult(
                betSize + " (Value)",
                $"Bet — strong value ({heroHandRank})",
                IsBarrel: isBarrel);
        }

        // Value → bet (ajustar sizing por SPR)
        if (equity > adjValue)
        {
            var betSize = AdjustBetSizeForSPR(thresholds.ValueBetSize, heroStack, potSize, street);
            return new PostflopDecisionResult(
                betSize + " (Value)",
                "Bet — value");
        }

        // Thin value → bet solo IP (OOP check para proteger rango)
        if (equity > thresholds.ThinValueAbove)
        {
            if (!thresholds.ThinValueIPOnly || isInPosition)
            {
                var betSize = AdjustBetSizeForSPR(thresholds.ThinValueBetSize, heroStack, potSize, street);
                return new PostflopDecisionResult(
                    betSize + " (Thin Value)",
                    "Bet — thin value");
            }

            return new PostflopDecisionResult("Check", "Check — thin value OOP (showdown)");
        }

        // Double barrel: hero apostó en street anterior y tiene equity marginal
        // Seguir apostando por consistencia de rango
        if (thresholds.CanDoubleBarrel && previousStreetBet && heroIsAggressor &&
            !isMultiway && street != BoardPosition.Flop &&
            equity >= thresholds.FoldBelow && equity < thresholds.ValueAbove)
        {
            var barrelBet = boardTexture == "Dry"
                ? thresholds.ThinValueBetSize
                : thresholds.BluffBetSize;
            barrelBet = AdjustBetSizeForSPR(barrelBet, heroStack, potSize, street);
            return new PostflopDecisionResult(
                barrelBet + " (Barrel)",
                "Double barrel — consistencia de rango",
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

        if (spr < _profile.SPRPushFoldThreshold)
            return (-_profile.SPRPushFoldFoldReduction, _profile.SPRPushFoldValueIncrease, true);

        if (spr > _profile.SPRDeepCautionThreshold)
            return (_profile.SPRDeepFoldIncrease, 0, false);

        return (0, 0, false);
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
        decimal potSize = 0)
    {
        // Semi-bluff con draws (solo si NO estamos facing a bet y no multiway con muchos oponentes)
        if (totalOuts >= PokerConstants.MinOutsForDraw && street != BoardPosition.River && !isFacingBet && !isMultiway)
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

            // Blocker bonus: hero bloquea draws completados del villano → villano más probable bluffeando
            bool hasBlocker = heroBlocksDangerSuit ||
                (boardChange != null && boardChange.StraightCompleted && heroHandRank >= HandRank.Straight);
            if (hasBlocker)
                bluffCatchThreshold *= 0.85;

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
}
