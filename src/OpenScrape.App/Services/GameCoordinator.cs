using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Services.Analysis;
using OpenScrape.App.Services.Exploitability;
using OpenScrape.App.Services.Tracking;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Coordinador del game loop: centraliza decisiones, análisis de board y tracking.
/// Independiente de UI — recibe estado y retorna resultados.
/// </summary>
public class GameCoordinator : IGameCoordinator
{
    private readonly IDecisionEngine _decisionEngine;
    private readonly IOpponentTracker _opponentTracker;
    private readonly IExploitabilityCalculator _exploitabilityCalculator;
    private readonly GameLoopStateMachine _gameLoopStateMachine;
    private readonly IGameLoggerService _gameLoggerService;
    private readonly IPostflopContextHolder _contextHolder;

    public GameCoordinator(
        IOpponentTracker opponentTracker,
        IExploitabilityCalculator exploitabilityCalculator,
        GameLoopStateMachine gameLoopStateMachine,
        IGameLoggerService gameLoggerService,
        IPostflopContextHolder contextHolder,
        IDecisionEngine decisionEngine)
    {
        _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
        _opponentTracker = opponentTracker;
        _exploitabilityCalculator = exploitabilityCalculator;
        _gameLoopStateMachine = gameLoopStateMachine;
        _gameLoggerService = gameLoggerService;
        _contextHolder = contextHolder;
    }

    public void ResetContext()
    {
        _contextHolder.StartNewHand();
    }

    #region Helpers

    public BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize)
    {
        if (maxBet == 0) return BetSizeCategory.NoBet;
        if (maxBet <= potSize * 0.15m) return BetSizeCategory.Underbet;
        if (maxBet <= potSize * 0.3m) return BetSizeCategory.Small;
        if (maxBet <= potSize * 0.7m) return BetSizeCategory.Medium;
        return BetSizeCategory.Large;
    }

    public string GetActiveVillainId(PlayerGameState state)
    {
        var villain = state.Players
            .Where(p => p.Active && !string.IsNullOrEmpty(p.Name))
            .OrderByDescending(p => p.Bet)
            .FirstOrDefault();
        if (villain == null) return "Unknown";

        if (!string.IsNullOrEmpty(villain.Alias))
            return villain.Alias;

        return _opponentTracker.ResolveName(villain.Name!) ?? villain.Name!;
    }

    public OpponentType GetVillainType(PlayerGameState state, bool? heroIsInPosition = null)
    {
        var villainId = GetActiveVillainId(state);
        if (villainId == "Unknown") return OpponentType.Unknown;
        var profile = _opponentTracker.GetProfile(villainId);
        if (!profile.HasReliablePreflopData) return OpponentType.Unknown;

        if (heroIsInPosition.HasValue)
            return profile.GetTypeForPosition(!heroIsInPosition.Value);

        return profile.Type;
    }

    public OpponentProfile? GetActiveVillainProfile(PlayerGameState state)
    {
        var villainId = GetActiveVillainId(state);
        if (villainId == "Unknown") return null;
        var profile = _opponentTracker.GetProfile(villainId);
        return profile.HasReliablePreflopData ? profile : null;
    }

    public void TrackVillainPostflopAction(PlayerGameState state, decimal maxBet, bool isPreflopAggressor, bool? heroIsInPosition = null)
    {
        var villainId = GetActiveVillainId(state);
        if (villainId == "Unknown") return;

        bool? villainIsIP = heroIsInPosition.HasValue ? !heroIsInPosition.Value : null;

        if (maxBet > 0)
        {
            _opponentTracker.RecordPostflopAction(villainId, PostflopAction.Bet, villainIsIP);
            if (isPreflopAggressor)
                _opponentTracker.RecordCBetOpportunity(villainId, didCBet: true);
        }
        else if (isPreflopAggressor)
        {
            _opponentTracker.RecordCBetOpportunity(villainId, didCBet: false);
        }
    }

    public decimal GetVillainStack(PlayerGameState state)
    {
        var activeVillains = state.Players
            .Where(p => p.Active && !string.IsNullOrEmpty(p.Name) && p.Name != "P0");
        var villainStack = activeVillains.Any() ? activeVillains.Max(p => p.Stack) : 0;

        if (villainStack <= 0 && state.HeroStack > 0)
            return state.HeroStack;

        return villainStack;
    }

    public bool HeroBlocksTopBoardCard(PlayerGameState state)
    {
        var boardCards = state.BoardCards;
        if (boardCards == null || boardCards.Count == 0) return false;

        int topBoardRank = boardCards.Max(c => c.Force);
        if (topBoardRank < 10) return false;

        return state.HoleCard1Rank == topBoardRank || state.HoleCard2Rank == topBoardRank;
    }

    public string FormatCardsForLog(PlayerGameState state, BoardPosition street)
    {
        var hero = $"[{state.HoleCard1Face} {state.HoleCard2Face}]";
        var boardCards = state.BoardCards
            .Where(b => b.Position != BoardPosition.Hand)
            .OrderBy(b => b.Location)
            .ToList();

        var flopCards = boardCards.Where(b => b.Position == BoardPosition.Flop)
            .Select(b => b.Name ?? "??").ToList();
        var flop = flopCards.Count > 0 ? $"[{string.Join(" ", flopCards)}]" : "";

        if (street == BoardPosition.Flop)
            return $"Hero: {hero}  Board: {flop}";

        var turnCard = boardCards.FirstOrDefault(b => b.Position == BoardPosition.Turn);
        var turn = turnCard != null ? $"[{turnCard.Name ?? "??"}]" : "";

        if (street == BoardPosition.Turn)
            return $"Hero: {hero}  Board: {flop} + {turn}";

        var riverCard = boardCards.FirstOrDefault(b => b.Position == BoardPosition.River);
        var river = riverCard != null ? $"[{riverCard.Name ?? "??"}]" : "";

        return $"Hero: {hero}  Board: {flop} + {turn} + {river}";
    }

    public (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(PlayerGameState state, decimal maxBet, bool isHeroInPosition, HandSituation currentSituation)
    {
        // Excluir hero (P0, ValuePosition=0) del check de agresor villain
        bool villainWasPreflopAggressor = state.Players
            .Any(p => p.Active && p.WasPreflopAggressor && p.ValuePosition != 0);

        bool heroWasPreviousStreetAggressor =
            (_gameLoopStateMachine.IsTurn && _contextHolder.Current.HeroBetFlop) ||
            (_gameLoopStateMachine.IsRiver && _contextHolder.Current.HeroBetTurn);

        // Hero es agresor si: raiseó preflop (WasPreflopAggressor en P0) O apostó calle anterior
        var heroPlayer = state.Players.FirstOrDefault(p => p.ValuePosition == 0);
        bool heroWasPreflopAggressor = heroPlayer?.WasPreflopAggressor == true;
        bool heroIsAggressor = heroWasPreflopAggressor || heroWasPreviousStreetAggressor;

        // DonkBet requiere que hero sea el agresor; sin agresor no hay donk bet
        if (!heroIsAggressor)
            return (false, currentSituation);

        bool effectiveVillainAggressor = villainWasPreflopAggressor && !heroWasPreviousStreetAggressor;
        return PreflopSituation.DetectDonkBet(maxBet, effectiveVillainAggressor, currentSituation);
    }

    #endregion

    #region Board Analysis

    /// <summary>
    /// True si la última carta del board completa un flush draw (tres del mismo palo).
    /// Es sólo contexto de log: la decisión llega del servidor.
    /// </summary>
    private static bool FlushDrawAppeared(List<BoardData> boardCards)
    {
        var suits = boardCards
            .Where(b => b.Position != BoardPosition.Hand)
            .Select(b => b.Suit);

        return suits.GroupBy(suit => suit).Any(group => group.Count() >= 3);
    }

    #endregion

    #region Detection

    public bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand, ref string previousDealer, ref string previousSB, ref string previousBB)
    {
        bool indicator1 = handNumberChanged;
        bool indicator2 = !string.IsNullOrEmpty(state.HoleCard1Face) && !string.IsNullOrEmpty(state.HoleCard2Face);
        bool indicator3 = state.PotSize < 10;
        bool indicator4 = state.BoardCards.Count(c => c.Position != BoardPosition.Hand) == 0;

        string currentDealerPlayerName = state.Players.FirstOrDefault(d => d.Dealer == true)?.Name ?? "";
        bool indicator5 = !string.IsNullOrEmpty(currentDealerPlayerName) && currentDealerPlayerName != previousDealer;

        string currentSBPlayerName = state.Players.FirstOrDefault(f => f.Position == TablePosition.SmallBlind)?.Name ?? "";
        bool indicator6 = !string.IsNullOrEmpty(currentSBPlayerName) && currentSBPlayerName != previousSB;

        string currentBBPlayerName = state.Players.FirstOrDefault(f => f.Position == TablePosition.BigBlind)?.Name ?? "";
        bool indicator7 = !string.IsNullOrEmpty(currentBBPlayerName) && currentBBPlayerName != previousBB;

        int secondaryCount = (indicator2 ? 1 : 0) + (indicator3 ? 1 : 0) + (indicator4 ? 1 : 0) + (indicator5 ? 1 : 0) + (indicator6 ? 1 : 0) + (indicator7 ? 1 : 0);

        bool isNewHand = indicator1
            ? secondaryCount >= 1
            : secondaryCount >= 3;

        if (isNewHand)
        {
            previousDealer = currentDealerPlayerName;
            previousSB = currentSBPlayerName;
            previousBB = currentBBPlayerName;
        }

        return isNewHand;
    }

    #endregion

    #region Determine Actions

    /// <summary>
    /// Mantiene la equity efectiva dentro del rango que acepta el motor.
    /// El ajuste de c-bet (flop) y la penalidad por peligro (turn y river)
    /// pueden empujarla por encima de 100 o por debajo de 0.
    /// </summary>
    internal static double ClampEquity(double equity) => Math.Clamp(equity, 0d, 100d);

    public async Task<GameDecisionResult> DetermineFlopAction(PlayerGameState state, CancellationToken cancellationToken = default)
    {
        decimal maxBet = state.Players.Where(p => p.Active && p.Name != "P0").Select(p => p.Bet).DefaultIfEmpty(0).Max();
        decimal potSize = state.PotSize;
        bool inPosition = state.IsInPosition;
        int numOpponents = state.Players.Count(p => p.Active && p.Name != "P0" && !string.IsNullOrEmpty(p.Name));
        if (numOpponents < 1) numOpponents = 1;

        var betSize = GetOpponentBetSize(maxBet, potSize);

        var effectiveSituation = state.HandSituation;
        bool isDonkBet = false;
        var donkResult = DetectDonkBet(state, maxBet, inPosition, effectiveSituation);
        if (donkResult.IsDonkBet)
        {
            isDonkBet = true;
            effectiveSituation = donkResult.DonkBetSituation;
        }

        bool isPreflopAggressor = PreflopSituation.IsAggressor(state.HandSituation);

        var doorResult = await _decisionEngine.DecideAsync(
            PlayerStateMapper.MapPostflop(
                state,
                DecisionStreet.Flop,
                maxBet,
                new DecisionContext { IsDonkBet = isDonkBet },
                BuildVillainProfile(state, inPosition),
                isPreflopAggressor,
                effectiveSituation),
            cancellationToken);

        double effectiveEquity = doorResult.Equity;
        string texture = doorResult.BoardTexture ?? "Unknown";

        TrackVillainPostflopAction(state, maxBet, isPreflopAggressor, inPosition);

        // Actualizar contexto cross-street
        string action = doorResult.Action;
        bool heroBet = action.StartsWith("Bet") || action.StartsWith("Raise");
        bool villainBet = maxBet > 0;
        _contextHolder.Update(c => c with
        {
            PreviousStreetWasBet = villainBet,
            HeroBetFlop = heroBet,
            VillainBetFlop = villainBet,
            VillainBetSizeFlop = betSize,
            HeroFloatedFlop = villainBet && action.StartsWith("Call"),
            VillainAggressorCheckedFlop = (!isPreflopAggressor && !villainBet) || c.VillainAggressorCheckedFlop
        });

        // Exploitabilidad
        var foldEquity = doorResult.FoldEquity;
        var flopAnalysis = _exploitabilityCalculator.AnalyzeDecision(
            action, effectiveEquity, doorResult.PotOdds, foldEquity,
            BoardPosition.Flop, effectiveSituation, inPosition, texture, potSize, betSize);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = action,
            Equity = effectiveEquity,
            PotOdds = doorResult.PotOdds,
            FoldEquity = foldEquity,
            Street = BoardPosition.Flop,
            Situation = effectiveSituation,
            IsInPosition = inPosition,
            BoardTexture = texture,
            PotSize = potSize,
            VillainBetSize = betSize,
            OurDecisionEV = flopAnalysis.OurDecisionEV,
            BestResponseEV = flopAnalysis.BestResponseEV,
            ExploitabilityMbb = flopAnalysis.ExploitabilityMbb
        });

        // Log
        double spr = potSize > 0 ? (double)(state.HeroStack / potSize) : 0;
        var draws = doorResult.DrawTypes.Count > 0 ? string.Join(", ", doorResult.DrawTypes) : "Ninguno";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ [FLOP] ═══════════════════════════════════════");
        sb.AppendLine($"  {FormatCardsForLog(state, BoardPosition.Flop)}");
        sb.AppendLine($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {state.HeroStack:F0}  |  SPR: {spr:F1}");
        sb.AppendLine($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
        sb.AppendLine($"  Equity: {effectiveEquity:F1}%  |  Pot odds: {doorResult.PotOdds:F1}%");
        sb.AppendLine($"  Mano: {doorResult.HeroHandRank ?? "Desconocida"} ({doorResult.PairClassification ?? "Sin par"}, Kicker: {doorResult.KickerStrength ?? "Desconocido"})  |  Draws: {draws}  |  Textura: {texture}");
        sb.AppendLine($"  ► Decisión: {action}{(doorResult.IsBluff ? " [BLUFF]" : "")}{(doorResult.IsBarrel ? " [BARREL]" : "")}{(doorResult.IsCheckRaise ? " [CHECK-RAISE]" : "")}");
        if (!string.IsNullOrEmpty(doorResult.Reason))
            sb.AppendLine($"    Razón: {doorResult.Reason}");

        // Log decisión en game logger
        _gameLoggerService.LogStreetDecision(new StreetDecision(
            Street: BoardPosition.Flop,
            EquityPercent: effectiveEquity,
            PotOddsPercent: doorResult.PotOdds,
            ExpectedValue: 0,
            RecommendedAction: action,
            ActionTaken: action,
            PotSizeAtDecision: potSize,
            BetSize: maxBet,
            Situation: effectiveSituation,
            IsInPosition: inPosition,
            Reason: doorResult.Reason,
            BoardTexture: texture,
            TotalOuts: doorResult.TotalOuts,
            SPR: spr
        ));
        _gameLoggerService.UpdateSituation(effectiveSituation);

        return new GameDecisionResult(action, sb.ToString(), doorResult, texture);
    }

    public async Task<GameDecisionResult> DetermineTurnAction(PlayerGameState state, CancellationToken cancellationToken = default)
    {
        decimal maxBet = state.Players.Where(p => p.Active && p.Name != "P0").Select(p => p.Bet).DefaultIfEmpty(0).Max();
        decimal potSize = state.PotSize;
        bool inPosition = state.IsInPosition;
        int numOpponents = Math.Max(1, state.Players.Count(p => p.Active && p.Name != "P0" && !string.IsNullOrEmpty(p.Name)));

        var betSize = GetOpponentBetSize(maxBet, potSize);

        var effectiveSituation = state.HandSituation;
        bool isDonkBet = false;
        var donkResult = DetectDonkBet(state, maxBet, inPosition, effectiveSituation);
        if (donkResult.IsDonkBet)
        {
            isDonkBet = true;
            effectiveSituation = donkResult.DonkBetSituation;
        }

        var turnCtx = _contextHolder.Current;
        bool turnIsAggressor = PreflopSituation.IsAggressor(effectiveSituation) || turnCtx.HeroBetFlop;

        var doorResult = await _decisionEngine.DecideAsync(
            PlayerStateMapper.MapPostflop(
                state,
                DecisionStreet.Turn,
                maxBet,
                new DecisionContext
                {
                    IsDonkBet = isDonkBet,
                    PreviousStreetBet = turnCtx.PreviousStreetWasBet,
                    VillainAggressorCheckedPreviousStreet = turnCtx.VillainAggressorCheckedFlop,
                    VillainBarreling = turnCtx.VillainBetFlop && maxBet > 0,
                    VillainBetSizeFlop = turnCtx.VillainBetSizeFlop,
                    HeroFloatedFlop = turnCtx.HeroFloatedFlop,
                    IsAnyoneAllIn = turnCtx.IsAnyoneAllIn
                },
                BuildVillainProfile(state, inPosition),
                turnIsAggressor,
                effectiveSituation),
            cancellationToken);

        string texture = doorResult.BoardTexture ?? "Unknown";

        TrackVillainPostflopAction(state, maxBet, turnIsAggressor, inPosition);

        // Actualizar contexto cross-street
        string action = doorResult.Action;
        bool heroBetTurn = action.StartsWith("Bet") || action.StartsWith("Raise");
        bool villainBetTurn = maxBet > 0;
        bool newFlushDanger = maxBet > 0 && action.StartsWith("Call") && FlushDrawAppeared(state.BoardCards);
        _contextHolder.Update(c => c with
        {
            PreviousStreetWasBet = villainBetTurn,
            HeroBetTurn = heroBetTurn,
            VillainBetTurn = villainBetTurn,
            VillainBetSizeTurn = betSize,
            TurnCalledWithFlushDanger = newFlushDanger || c.TurnCalledWithFlushDanger
        });

        // Exploitabilidad
        double effectiveEquity = doorResult.Equity;
        var foldEquity = doorResult.FoldEquity;
        var turnAnalysis = _exploitabilityCalculator.AnalyzeDecision(
            action, effectiveEquity, doorResult.PotOdds, foldEquity,
            BoardPosition.Turn, effectiveSituation, inPosition, texture, potSize, betSize);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = action,
            Equity = effectiveEquity,
            PotOdds = doorResult.PotOdds,
            FoldEquity = foldEquity,
            Street = BoardPosition.Turn,
            Situation = effectiveSituation,
            IsInPosition = inPosition,
            BoardTexture = texture,
            PotSize = potSize,
            VillainBetSize = betSize,
            OurDecisionEV = turnAnalysis.OurDecisionEV,
            BestResponseEV = turnAnalysis.BestResponseEV,
            ExploitabilityMbb = turnAnalysis.ExploitabilityMbb
        });

        // Log
        double spr = potSize > 0 ? (double)(state.HeroStack / potSize) : 0;
        var draws = doorResult.DrawTypes.Count > 0 ? string.Join(", ", doorResult.DrawTypes) : "Ninguno";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ [TURN] ═══════════════════════════════════════");
        sb.AppendLine($"  {FormatCardsForLog(state, BoardPosition.Turn)}");
        sb.AppendLine($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {state.HeroStack:F0}  |  SPR: {spr:F1}");
        sb.AppendLine($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
        sb.AppendLine($"  Equity: {effectiveEquity:F1}%  |  Pot odds: {doorResult.PotOdds:F1}%");
        sb.AppendLine($"  Mano: {doorResult.HeroHandRank ?? "Desconocida"} ({doorResult.PairClassification ?? "Sin par"}, Kicker: {doorResult.KickerStrength ?? "Desconocido"})  |  Draws: {draws}  |  Textura: {texture}");
        sb.AppendLine($"  ► Decisión: {action}{(doorResult.IsBluff ? " [BLUFF]" : "")}{(doorResult.IsBarrel ? " [BARREL]" : "")}{(doorResult.IsCheckRaise ? " [CHECK-RAISE]" : "")}");
        if (!string.IsNullOrEmpty(doorResult.Reason))
            sb.AppendLine($"    Razón: {doorResult.Reason}");

        _gameLoggerService.LogStreetDecision(new StreetDecision(
            Street: BoardPosition.Turn,
            EquityPercent: effectiveEquity,
            PotOddsPercent: doorResult.PotOdds,
            ExpectedValue: 0,
            RecommendedAction: action,
            ActionTaken: action,
            PotSizeAtDecision: potSize,
            BetSize: maxBet,
            Situation: effectiveSituation,
            IsInPosition: inPosition,
            Reason: doorResult.Reason,
            BoardTexture: texture,
            TotalOuts: doorResult.TotalOuts,
            SPR: spr
        ));
        var turnCardNames = state.BoardCards
            .Where(b => b.Position == BoardPosition.Turn)
            .Select(b => b.Name ?? "").ToList();
        _gameLoggerService.UpdateBoard(turnCardNames);
        _gameLoggerService.UpdateSituation(effectiveSituation);

        return new GameDecisionResult(action, sb.ToString(), doorResult, texture);
    }

    public async Task<GameDecisionResult> DetermineRiverAction(PlayerGameState state, CancellationToken cancellationToken = default)
    {
        decimal maxBet = state.Players.Where(p => p.Active && p.Name != "P0").Select(p => p.Bet).DefaultIfEmpty(0).Max();
        decimal potSize = state.PotSize;
        bool inPosition = state.IsInPosition;
        int numOpponents = Math.Max(1, state.Players.Count(p => p.Active && p.Name != "P0" && !string.IsNullOrEmpty(p.Name)));
        decimal villainStack = GetVillainStack(state);

        // All-in detection
        if (villainStack <= 0)
            _contextHolder.Update(c => c with { IsAnyoneAllIn = true });

        var betSize = GetOpponentBetSize(maxBet, potSize);

        var effectiveSituation = state.HandSituation;
        bool isDonkBet = false;
        var donkResult = DetectDonkBet(state, maxBet, inPosition, effectiveSituation);
        if (donkResult.IsDonkBet)
        {
            isDonkBet = true;
            effectiveSituation = donkResult.DonkBetSituation;
        }

        var riverCtx = _contextHolder.Current;
        bool riverIsAggressor = PreflopSituation.IsAggressor(effectiveSituation) || riverCtx.HeroBetFlop || riverCtx.HeroBetTurn;

        var doorResult = await _decisionEngine.DecideAsync(
            PlayerStateMapper.MapPostflop(
                state,
                DecisionStreet.River,
                maxBet,
                new DecisionContext
                {
                    IsDonkBet = isDonkBet,
                    PreviousStreetBet = riverCtx.PreviousStreetWasBet,
                    VillainAggressorCheckedPreviousStreet = !riverCtx.VillainBetTurn && !riverIsAggressor,
                    VillainBarreling = riverCtx.VillainBetTurn && maxBet > 0,
                    VillainBetSizeTurn = riverCtx.VillainBetSizeTurn,
                    VillainCheckedMiddleStreet = riverCtx.VillainCheckedMiddleStreet,
                    TurnCalledWithFlushDanger = riverCtx.TurnCalledWithFlushDanger,
                    HeroCheckedAllStreets = riverCtx.HeroCheckedAllStreets,
                    IsAnyoneAllIn = riverCtx.IsAnyoneAllIn
                },
                BuildVillainProfile(state, inPosition),
                riverIsAggressor,
                effectiveSituation),
            cancellationToken);

        string texture = doorResult.BoardTexture ?? "Unknown";

        if (maxBet > 0)
            _opponentTracker.RecordPostflopAction(GetActiveVillainId(state), PostflopAction.Bet);

        // Actualizar contexto
        string action = doorResult.Action;
        _contextHolder.Update(c => c with { PreviousStreetWasBet = maxBet > 0 });

        // Exploitabilidad
        double effectiveEquity = doorResult.Equity;
        var foldEquity = doorResult.FoldEquity;
        var riverAnalysis = _exploitabilityCalculator.AnalyzeDecision(
            action, effectiveEquity, doorResult.PotOdds, foldEquity,
            BoardPosition.River, effectiveSituation, inPosition, texture, potSize, betSize);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = action,
            Equity = effectiveEquity,
            PotOdds = doorResult.PotOdds,
            FoldEquity = foldEquity,
            Street = BoardPosition.River,
            Situation = effectiveSituation,
            IsInPosition = inPosition,
            BoardTexture = texture,
            PotSize = potSize,
            VillainBetSize = betSize,
            OurDecisionEV = riverAnalysis.OurDecisionEV,
            BestResponseEV = riverAnalysis.BestResponseEV,
            ExploitabilityMbb = riverAnalysis.ExploitabilityMbb
        });

        // Log
        double spr = potSize > 0 ? (double)(state.HeroStack / potSize) : 0;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ [RIVER] ═══════════════════════════════════════");
        sb.AppendLine($"  {FormatCardsForLog(state, BoardPosition.River)}");
        sb.AppendLine($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {state.HeroStack:F0}  |  SPR: {spr:F1}");
        sb.AppendLine($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
        sb.AppendLine($"  Equity: {effectiveEquity:F1}%  |  Pot odds: {doorResult.PotOdds:F1}%");
        sb.AppendLine($"  Mano: {doorResult.HeroHandRank ?? "Desconocida"} ({doorResult.PairClassification ?? "Sin par"}, Kicker: {doorResult.KickerStrength ?? "Desconocido"})");
        sb.AppendLine($"  ► Decisión: {action}{(doorResult.IsBluff ? " [BLUFF]" : "")}{(doorResult.IsBarrel ? " [BARREL]" : "")}{(doorResult.IsCheckRaise ? " [CHECK-RAISE]" : "")}");
        if (!string.IsNullOrEmpty(doorResult.Reason))
            sb.AppendLine($"    Razón: {doorResult.Reason}");

        _gameLoggerService.LogStreetDecision(new StreetDecision(
            Street: BoardPosition.River,
            EquityPercent: effectiveEquity,
            PotOddsPercent: doorResult.PotOdds,
            ExpectedValue: 0,
            RecommendedAction: action,
            ActionTaken: action,
            PotSizeAtDecision: potSize,
            BetSize: maxBet,
            Situation: effectiveSituation,
            IsInPosition: inPosition,
            Reason: doorResult.Reason,
            BoardTexture: texture,
            TotalOuts: doorResult.TotalOuts,
            SPR: spr
        ));
        var riverCardNames = state.BoardCards
            .Where(b => b.Position == BoardPosition.River)
            .Select(b => b.Name ?? "").ToList();
        _gameLoggerService.UpdateBoard(riverCardNames);

        return new GameDecisionResult(action, sb.ToString(), doorResult, texture);
    }

    #endregion

    private VillainProfile BuildVillainProfile(PlayerGameState state, bool? inPosition)
    {
        var villainId = GetActiveVillainId(state);
        var villainStats = GetActiveVillainProfile(state);

        // El cliente ya no calcula fold equity: manda -1 y el servidor aplica el
        // multiplicador por tipo sobre su propio cálculo.
        return new VillainProfile
        {
            VillainId = villainId,
            Type = GetVillainType(state, inPosition),
            FoldToBetPct = _opponentTracker.GetFoldToBetPct(villainId),
            AdjustedFoldEquity = -1,
            HasReliableAggressionData = villainStats?.HasReliableAFData ?? false,
            HandsPlayed = villainStats?.HandsPlayed ?? 0,
            TimesVoluntarilyPutMoneyIn = villainStats?.TimesVoluntarilyPutMoneyIn ?? 0,
            TimesPreflopRaised = villainStats?.TimesPreflopRaised ?? 0,
            TimesThreeBet = villainStats?.TimesThreeBet ?? 0
        };
    }
}