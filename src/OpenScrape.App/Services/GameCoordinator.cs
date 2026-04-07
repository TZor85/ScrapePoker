using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
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
    private readonly IPostflopDecisionService _postflopDecisionService;
    private readonly IOpponentTracker _opponentTracker;
    private readonly IBoardTextureAnalyzer _boardTextureAnalyzer;
    private readonly IBetSizingService _betSizingService;
    private readonly IExploitabilityCalculator _exploitabilityCalculator;
    private readonly GameLoopStateMachine _gameLoopStateMachine;
    private readonly GameLoggerService _gameLoggerService;
    private readonly StrategyProfileService _strategyProfileService;

    public PostflopGameContext PostflopContext { get; } = new();
    public PokerCalculationResult? FlopResult { get; private set; }
    public PokerCalculationResult? TurnResult { get; private set; }
    public PokerCalculationResult? RiverResult { get; private set; }
    public TurnBoardTexture TurnBoardTexture { get; set; }
    public RiverBoardTexture RiverBoardTexture { get; set; }

    public GameCoordinator(
        IPostflopDecisionService postflopDecisionService,
        IOpponentTracker opponentTracker,
        IBoardTextureAnalyzer boardTextureAnalyzer,
        IBetSizingService betSizingService,
        IExploitabilityCalculator exploitabilityCalculator,
        GameLoopStateMachine gameLoopStateMachine,
        GameLoggerService gameLoggerService,
        StrategyProfileService strategyProfileService)
    {
        _postflopDecisionService = postflopDecisionService;
        _opponentTracker = opponentTracker;
        _boardTextureAnalyzer = boardTextureAnalyzer;
        _betSizingService = betSizingService;
        _exploitabilityCalculator = exploitabilityCalculator;
        _gameLoopStateMachine = gameLoopStateMachine;
        _gameLoggerService = gameLoggerService;
        _strategyProfileService = strategyProfileService;
    }

    public void ResetContext()
    {
        PostflopContext.Reset();
        FlopResult = null;
        TurnResult = null;
        RiverResult = null;
    }

    public void SetFlopResult(PokerCalculationResult result) => FlopResult = result;
    public void SetTurnResult(PokerCalculationResult result) => TurnResult = result;
    public void SetRiverResult(PokerCalculationResult result) => RiverResult = result;

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
        bool villainWasPreflopAggressor = state.Players
            .Any(p => p.Active && p.WasPreflopAggressor);

        bool heroWasPreviousStreetAggressor =
            (_gameLoopStateMachine.IsTurn && PostflopContext.HeroBetFlop) ||
            (_gameLoopStateMachine.IsRiver && PostflopContext.HeroBetTurn);

        bool effectiveVillainAggressor = villainWasPreflopAggressor && !heroWasPreviousStreetAggressor;
        return PreflopAnalyzer.DetectDonkBet(maxBet, effectiveVillainAggressor, currentSituation);
    }

    public string AdjustBetSize(string action, decimal heroStack, decimal potSize, int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition)
    {
        if (!action.StartsWith("Bet "))
            return action;

        var parts = action.Split(' ');
        double baseSize;

        if (parts[1] == "Pot")
            baseSize = 1.0;
        else if (parts[1].Contains('/'))
        {
            var frac = parts[1].Split('/');
            if (frac.Length >= 2 &&
                double.TryParse(frac[0], out var num) &&
                double.TryParse(frac[1], out var den) && den > 0)
                baseSize = num / den;
            else
                baseSize = 0.50;
        }
        else
            return action;

        var adjustedBet = _betSizingService.CalculateDynamicBetSize(baseSize, heroStack, potSize, numOpponents, isPaired, isCoordinated, isDry, isInPosition);
        var reason = action.Contains('(') ? action[action.IndexOf('(')..] : "";
        return adjustedBet + reason;
    }

    #endregion

    #region Board Analysis

    public BoardChangeResult AnalyzeBoardChange(List<BoardData> boardCards, int previousCardCount)
    {
        var communityCards = boardCards.Where(b => b.Position != BoardPosition.Hand).ToList();
        if (communityCards.Count <= previousCardCount)
            return BoardChangeResult.Safe;

        var previousRanks = communityCards.Take(previousCardCount).Select(c => c.Force).ToList();
        var previousSuits = communityCards.Take(previousCardCount).Select(c => c.Suit).ToList();
        var newCard = communityCards[previousCardCount];

        return _boardTextureAnalyzer.AnalyzeBoardChange(previousRanks, previousSuits, newCard.Force, newCard.Suit);
    }

    public TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> boardCards)
    {
        var turnCards = boardCards.Where(b => b.Position != BoardPosition.Hand).ToList();
        if (turnCards.Count < 4) return TurnBoardTexture.Dry;

        var suits = turnCards.Select(b => b.Suit).ToList();
        var ranks = turnCards.Select(b => b.Force).OrderBy(r => r).ToList();

        if (ranks.GroupBy(r => r).Any(g => g.Count() >= 2))
            return TurnBoardTexture.Paired;

        bool hasFlushDraw = suits.GroupBy(s => s).Any(g => g.Count() >= 3);
        bool hasStraightDraw = ranks.Count >= 3 && ranks.Zip(ranks.Skip(1), (a, b) => b - a).Any(diff => diff <= 4);

        if (hasFlushDraw || hasStraightDraw)
            return TurnBoardTexture.Coordinated;

        return TurnBoardTexture.Dry;
    }

    public RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> boardCards)
    {
        var communityCards = boardCards.Where(b => b.Position != BoardPosition.Hand).ToList();
        if (communityCards.Count < 5) return RiverBoardTexture.Dry;

        var suits = communityCards.Select(b => b.Suit).ToList();
        var ranks = communityCards.Select(b => b.Force).OrderBy(r => r).ToList();

        if (ranks.GroupBy(r => r).Any(g => g.Count() >= 3) || ranks.GroupBy(r => r).Count(g => g.Count() >= 2) >= 2)
            return RiverBoardTexture.Paired;

        bool hasFlush = suits.GroupBy(s => s).Any(g => g.Count() >= 5);
        bool hasStraight = ranks.Count >= 5 && ranks.Zip(ranks.Skip(1), (a, b) => b - a).Any(diff => diff <= 4);

        if (hasFlush || hasStraight)
            return RiverBoardTexture.Coordinated;

        return RiverBoardTexture.Dry;
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

    public GameDecisionResult DetermineFlopAction(PlayerGameState state)
    {
        if (FlopResult == null) return new GameDecisionResult("Check", "Error: sin resultado de flop");

        var flopResult = FlopResult;
        var boardCards = state.BoardCards.Where(b => b.Position == BoardPosition.Flop).ToList();
        var flopRanks = boardCards.Select(b => b.Force).ToList();
        var flopSuits = boardCards.Select(b => b.Suit).ToList();

        decimal maxBet = state.Players.Where(p => p.Active && p.Name != "P0").Select(p => p.Bet).DefaultIfEmpty(0).Max();
        decimal potSize = state.PotSize;
        bool inPosition = state.IsInPosition;
        int numOpponents = state.Players.Count(p => p.Active && p.Name != "P0" && !string.IsNullOrEmpty(p.Name));
        if (numOpponents < 1) numOpponents = 1;
        decimal villainStack = GetVillainStack(state);

        var betSize = GetOpponentBetSize(maxBet, potSize);
        bool villainAggro = maxBet > 0;

        var boardTexture = _boardTextureAnalyzer.Analyze(flopRanks, flopSuits);
        string texture = boardTexture.Category.ToString();
        var initialDanger = _boardTextureAnalyzer.AnalyzeInitialBoard(flopRanks, flopSuits);
        PostflopContext.InitialBoardDanger = initialDanger;
        var boardChange = AnalyzeBoardChange(state.BoardCards, 0);

        var effectiveSituation = state.HandSituation;
        bool isDonkBet = false;
        var donkResult = DetectDonkBet(state, maxBet, inPosition, effectiveSituation);
        if (donkResult.IsDonkBet)
        {
            isDonkBet = true;
            effectiveSituation = donkResult.DonkBetSituation;
        }

        bool isPreflopAggressor = PreflopAnalyzer.IsPreflopAggressor(state.HandSituation);
        bool hasRangeAdvantage = PreflopAnalyzer.HasRangeAdvantageOnBoard(flopRanks, boardTexture, isPreflopAggressor, state.HandSituation);
        double cbetAdjustment = PreflopAnalyzer.CalculateCbetAdjustment(isPreflopAggressor, hasRangeAdvantage, boardTexture, inPosition, numOpponents, _strategyProfileService.Profile);

        double rawEquity = flopResult.EquityPercentage;
        double effectiveEquity = rawEquity + cbetAdjustment;

        var decision = _postflopDecisionService.DetermineAction(new PostflopDecisionInput
        {
            Equity = effectiveEquity,
            Street = BoardPosition.Flop,
            Situation = effectiveSituation,
            BoardTexture = texture,
            IsInPosition = inPosition,
            VillainBetSize = betSize,
            PotOdds = flopResult.PotOddsPercentage,
            TotalOuts = flopResult.TotalOuts,
            VillainShowedAggression = villainAggro,
            BoardChange = boardChange,
            HeroStack = state.HeroStack,
            PotSize = potSize,
            HasFlushDraw = flopResult.DrawTypes.Contains("Flush Draw"),
            NumOpponents = numOpponents,
            HeroIsAggressor = isPreflopAggressor,
            HeroHandRank = flopResult.HeroHandRank,
            HasComboDraw = flopResult.HasComboDraw,
            PairClassification = flopResult.PairType,
            FoldEquity = _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(state), flopResult.FoldEquity),
            VillainType = GetVillainType(state, inPosition),
            VillainFoldToBetPct = _opponentTracker.GetFoldToBetPct(GetActiveVillainId(state)),
            HeroKickerStrength = flopResult.HeroKickerStrength,
            HeroBlocksTopCard = HeroBlocksTopBoardCard(state),
            IsAnyoneAllIn = PostflopContext.IsAnyoneAllIn
        });

        TrackVillainPostflopAction(state, maxBet, isPreflopAggressor, inPosition);

        // Actualizar contexto cross-street
        string action = decision.Action;
        PostflopContext.PreviousStreetWasBet = maxBet > 0;
        PostflopContext.HeroBetFlop = action.StartsWith("Bet") || action.StartsWith("Raise");
        PostflopContext.VillainBetFlop = maxBet > 0;
        PostflopContext.VillainBetSizeFlop = betSize;
        PostflopContext.HeroFloatedFlop = maxBet > 0 && action.StartsWith("Call");
        if (!isPreflopAggressor && maxBet == 0)
            PostflopContext.VillainAggressorCheckedFlop = true;

        // Exploitabilidad
        var foldEquity = _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(state), flopResult.FoldEquity);
        var flopAnalysis = _exploitabilityCalculator.AnalyzeDecision(
            decision.Action, effectiveEquity, flopResult.PotOddsPercentage, foldEquity,
            BoardPosition.Flop, effectiveSituation, inPosition, texture, potSize, betSize);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = decision.Action, Equity = effectiveEquity,
            PotOdds = flopResult.PotOddsPercentage, FoldEquity = foldEquity,
            Street = BoardPosition.Flop, Situation = effectiveSituation,
            IsInPosition = inPosition, BoardTexture = texture, PotSize = potSize,
            VillainBetSize = betSize, OurDecisionEV = flopAnalysis.OurDecisionEV,
            BestResponseEV = flopAnalysis.BestResponseEV,
            ExploitabilityMbb = flopAnalysis.ExploitabilityMbb
        });

        // Log
        double spr = potSize > 0 ? (double)(state.HeroStack / potSize) : 0;
        var draws = flopResult.DrawTypes.Count > 0 ? string.Join(", ", flopResult.DrawTypes) : "Ninguno";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ [FLOP] ═══════════════════════════════════════");
        sb.AppendLine($"  {FormatCardsForLog(state, BoardPosition.Flop)}");
        sb.AppendLine($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {state.HeroStack:F0}  |  SPR: {spr:F1}");
        sb.AppendLine($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
        sb.AppendLine($"  Equity: {rawEquity:F1}%  |  C-bet adj: {cbetAdjustment:+0.0;-0.0}  |  Equity efectiva: {effectiveEquity:F1}%  |  Pot odds: {flopResult.PotOddsPercentage:F1}%");
        sb.AppendLine($"  Mano: {flopResult.HeroHandRank} ({flopResult.PairType}, Kicker: {flopResult.HeroKickerStrength})  |  Draws: {draws}  |  Textura: {texture}");
        sb.AppendLine($"  ► Decisión: {action}{(decision.IsBluff ? " [BLUFF]" : "")}{(decision.IsBarrel ? " [BARREL]" : "")}{(decision.IsCheckRaise ? " [CHECK-RAISE]" : "")}");
        if (!string.IsNullOrEmpty(decision.Reason))
            sb.AppendLine($"    Razón: {decision.Reason}");

        // Log decisión en game logger
        _gameLoggerService.LogStreetDecision(new StreetDecision(
            Street: BoardPosition.Flop,
            EquityPercent: effectiveEquity,
            PotOddsPercent: flopResult.PotOddsPercentage,
            ExpectedValue: 0,
            RecommendedAction: action,
            ActionTaken: action,
            PotSizeAtDecision: potSize,
            BetSize: maxBet,
            Situation: effectiveSituation,
            IsInPosition: inPosition,
            Reason: decision.Reason,
            BoardTexture: texture,
            TotalOuts: flopResult.TotalOuts,
            SPR: spr
        ));
        _gameLoggerService.UpdateSituation(effectiveSituation);

        return new GameDecisionResult(action, sb.ToString(), decision, boardChange, texture);
    }

    public GameDecisionResult DetermineTurnAction(PlayerGameState state)
    {
        if (TurnResult == null) return new GameDecisionResult("Check", "Error: sin resultado de turn");

        var turnResult = TurnResult;
        decimal maxBet = state.Players.Where(p => p.Active && p.Name != "P0").Select(p => p.Bet).DefaultIfEmpty(0).Max();
        decimal potSize = state.PotSize;
        bool inPosition = state.IsInPosition;
        int numOpponents = Math.Max(1, state.Players.Count(p => p.Active && p.Name != "P0" && !string.IsNullOrEmpty(p.Name)));
        decimal villainStack = GetVillainStack(state);

        var betSize = GetOpponentBetSize(maxBet, potSize);
        bool villainAggro = maxBet > 0;

        var effectiveSituation = state.HandSituation;
        bool isDonkBet = false;
        var donkResult = DetectDonkBet(state, maxBet, inPosition, effectiveSituation);
        if (donkResult.IsDonkBet)
        {
            isDonkBet = true;
            effectiveSituation = donkResult.DonkBetSituation;
        }

        var boardChange = AnalyzeBoardChange(state.BoardCards, 3);
        var combinedBoardChange = PostflopGameContext.CombineBoardChanges(PostflopContext.InitialBoardDanger, boardChange);
        var texture = TurnBoardTexture.ToString();
        bool heroBlocks = state.BoardCards.Any(b => b.Position != BoardPosition.Hand &&
            (b.Suit == state.HoleCard1Suit || b.Suit == state.HoleCard2Suit));
        bool heroHasNutBlocker = state.HoleCard1Rank >= 13 || state.HoleCard2Rank >= 13;

        double equity = turnResult.EquityPercentage;
        var dangerPenalty = _postflopDecisionService.CalculateDangerPenalty(equity, boardChange, heroBlocks, maxBet > 0, BoardPosition.Turn, heroHasNutBlocker, turnResult.HeroHandRank);
        PostflopContext.LastBoardChange = combinedBoardChange;

        bool turnIsAggressor = PreflopAnalyzer.IsPreflopAggressor(effectiveSituation) || PostflopContext.HeroBetFlop;

        var decision = _postflopDecisionService.DetermineAction(new PostflopDecisionInput
        {
            Equity = equity,
            Street = BoardPosition.Turn,
            Situation = effectiveSituation,
            BoardTexture = texture,
            IsInPosition = inPosition,
            VillainBetSize = betSize,
            PotOdds = turnResult.PotOddsPercentage,
            TotalOuts = turnResult.TotalOuts,
            PreviousStreetBet = PostflopContext.PreviousStreetWasBet,
            VillainShowedAggression = villainAggro,
            BoardChange = boardChange,
            HeroBlocksDangerSuit = heroBlocks,
            HeroStack = state.HeroStack,
            PotSize = potSize,
            HasFlushDraw = turnResult.DrawTypes.Contains("Flush Draw"),
            NumOpponents = numOpponents,
            HeroIsAggressor = turnIsAggressor,
            HeroHandRank = turnResult.HeroHandRank,
            HasComboDraw = turnResult.HasComboDraw,
            VillainAggressorCheckedPreviousStreet = PostflopContext.VillainAggressorCheckedFlop,
            VillainBarreling = PostflopContext.VillainBetFlop && maxBet > 0,
            PairClassification = turnResult.PairType,
            FoldEquity = _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(state), turnResult.FoldEquity),
            VillainBetSizeFlop = PostflopContext.VillainBetSizeFlop,
            VillainType = GetVillainType(state, inPosition),
            HeroFloatedFlop = PostflopContext.HeroFloatedFlop,
            VillainFoldToBetPct = _opponentTracker.GetFoldToBetPct(GetActiveVillainId(state)),
            HeroKickerStrength = turnResult.HeroKickerStrength,
            HeroBlocksTopCard = HeroBlocksTopBoardCard(state),
            IsAnyoneAllIn = PostflopContext.IsAnyoneAllIn
        });

        TrackVillainPostflopAction(state, maxBet, turnIsAggressor, inPosition);

        // Actualizar contexto cross-street
        string action = decision.Action;
        PostflopContext.PreviousStreetWasBet = maxBet > 0;
        PostflopContext.HeroBetTurn = action.StartsWith("Bet") || action.StartsWith("Raise");
        PostflopContext.VillainBetTurn = maxBet > 0;
        PostflopContext.VillainBetSizeTurn = betSize;
        if (maxBet > 0 && action.StartsWith("Call") && boardChange.FlushDrawAppeared)
            PostflopContext.TurnCalledWithFlushDanger = true;

        // Exploitabilidad
        double effectiveEquity = equity - dangerPenalty;
        var foldEquity = _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(state), turnResult.FoldEquity);
        var turnAnalysis = _exploitabilityCalculator.AnalyzeDecision(
            decision.Action, effectiveEquity, turnResult.PotOddsPercentage, foldEquity,
            BoardPosition.Turn, effectiveSituation, inPosition, texture, potSize, betSize);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = decision.Action, Equity = effectiveEquity,
            PotOdds = turnResult.PotOddsPercentage, FoldEquity = foldEquity,
            Street = BoardPosition.Turn, Situation = effectiveSituation,
            IsInPosition = inPosition, BoardTexture = texture, PotSize = potSize,
            VillainBetSize = betSize, OurDecisionEV = turnAnalysis.OurDecisionEV,
            BestResponseEV = turnAnalysis.BestResponseEV,
            ExploitabilityMbb = turnAnalysis.ExploitabilityMbb
        });

        // Log
        double spr = potSize > 0 ? (double)(state.HeroStack / potSize) : 0;
        var dangerFlags = new List<string>();
        if (boardChange.FlushCompleted) dangerFlags.Add("Flush completado");
        if (boardChange.StraightCompleted) dangerFlags.Add("Straight completado");
        if (boardChange.FlushDrawAppeared) dangerFlags.Add("Flush draw apareció");
        if (boardChange.BoardPaired) dangerFlags.Add("Board pareado");
        if (boardChange.OvercardAppeared) dangerFlags.Add("Overcard");
        string dangerStr = dangerFlags.Count > 0 ? string.Join(", ", dangerFlags) : "Ninguno";
        var draws = turnResult.DrawTypes.Count > 0 ? string.Join(", ", turnResult.DrawTypes) : "Ninguno";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ [TURN] ═══════════════════════════════════════");
        sb.AppendLine($"  {FormatCardsForLog(state, BoardPosition.Turn)}");
        sb.AppendLine($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {state.HeroStack:F0}  |  SPR: {spr:F1}");
        sb.AppendLine($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
        sb.AppendLine($"  Equity: {equity:F1}%  |  Danger: -{dangerPenalty:F1} ({dangerStr})  |  Pot odds: {turnResult.PotOddsPercentage:F1}%");
        sb.AppendLine($"  Mano: {turnResult.HeroHandRank} ({turnResult.PairType}, Kicker: {turnResult.HeroKickerStrength})  |  Draws: {draws}  |  Textura: {texture}");
        sb.AppendLine($"  ► Decisión: {action}{(decision.IsBluff ? " [BLUFF]" : "")}{(decision.IsBarrel ? " [BARREL]" : "")}{(decision.IsCheckRaise ? " [CHECK-RAISE]" : "")}");
        if (!string.IsNullOrEmpty(decision.Reason))
            sb.AppendLine($"    Razón: {decision.Reason}");

        _gameLoggerService.LogStreetDecision(new StreetDecision(
            Street: BoardPosition.Turn,
            EquityPercent: effectiveEquity,
            PotOddsPercent: turnResult.PotOddsPercentage,
            ExpectedValue: 0,
            RecommendedAction: action,
            ActionTaken: action,
            PotSizeAtDecision: potSize,
            BetSize: maxBet,
            Situation: effectiveSituation,
            IsInPosition: inPosition,
            Reason: decision.Reason,
            BoardTexture: texture,
            TotalOuts: turnResult.TotalOuts,
            SPR: spr
        ));
        var turnCardNames = state.BoardCards
            .Where(b => b.Position == BoardPosition.Turn)
            .Select(b => b.Name ?? "").ToList();
        _gameLoggerService.UpdateBoard(turnCardNames);
        _gameLoggerService.UpdateSituation(effectiveSituation);

        return new GameDecisionResult(action, sb.ToString(), decision, boardChange, texture);
    }

    public GameDecisionResult DetermineRiverAction(PlayerGameState state)
    {
        if (RiverResult == null) return new GameDecisionResult("Check", "Error: sin resultado de river");

        var riverResult = RiverResult;
        decimal maxBet = state.Players.Where(p => p.Active && p.Name != "P0").Select(p => p.Bet).DefaultIfEmpty(0).Max();
        decimal potSize = state.PotSize;
        bool inPosition = state.IsInPosition;
        int numOpponents = Math.Max(1, state.Players.Count(p => p.Active && p.Name != "P0" && !string.IsNullOrEmpty(p.Name)));
        decimal villainStack = GetVillainStack(state);

        // All-in detection
        if (villainStack <= 0)
            PostflopContext.IsAnyoneAllIn = true;

        var betSize = GetOpponentBetSize(maxBet, potSize);
        bool villainAggro = maxBet > 0;
        string texture = RiverBoardTexture.ToString();

        var effectiveSituation = state.HandSituation;
        bool isDonkBet = false;
        var donkResult = DetectDonkBet(state, maxBet, inPosition, effectiveSituation);
        if (donkResult.IsDonkBet)
        {
            isDonkBet = true;
            effectiveSituation = donkResult.DonkBetSituation;
        }

        var boardChange = AnalyzeBoardChange(state.BoardCards, 4);
        var combinedBoardChange = PostflopGameContext.CombineBoardChanges(PostflopContext.LastBoardChange ?? BoardChangeResult.Safe, boardChange);
        PostflopContext.LastBoardChange = combinedBoardChange;

        bool heroBlocks = state.BoardCards.Any(b => b.Position != BoardPosition.Hand &&
            (b.Suit == state.HoleCard1Suit || b.Suit == state.HoleCard2Suit));
        bool heroHasNutBlocker = state.HoleCard1Rank >= 13 || state.HoleCard2Rank >= 13;
        bool isFacingBet = maxBet > 0;

        double equity = riverResult.EquityPercentage;
        var dangerPenalty = _postflopDecisionService.CalculateDangerPenalty(equity, boardChange, heroBlocks, isFacingBet, BoardPosition.River, heroHasNutBlocker, riverResult.HeroHandRank);

        bool riverIsAggressor = PreflopAnalyzer.IsPreflopAggressor(effectiveSituation) || PostflopContext.HeroBetFlop || PostflopContext.HeroBetTurn;

        var decision = _postflopDecisionService.DetermineAction(new PostflopDecisionInput
        {
            Equity = equity,
            Street = BoardPosition.River,
            Situation = effectiveSituation,
            BoardTexture = texture,
            IsInPosition = inPosition,
            VillainBetSize = betSize,
            PotOdds = riverResult.PotOddsPercentage,
            TotalOuts = riverResult.TotalOuts,
            PreviousStreetBet = PostflopContext.PreviousStreetWasBet,
            VillainShowedAggression = villainAggro,
            BoardChange = boardChange,
            HeroBlocksDangerSuit = heroBlocks,
            HeroStack = state.HeroStack,
            PotSize = potSize,
            HasFlushDraw = riverResult.DrawTypes.Contains("Flush Draw"),
            NumOpponents = numOpponents,
            HeroIsAggressor = riverIsAggressor,
            HeroHandRank = riverResult.HeroHandRank,
            HasComboDraw = riverResult.HasComboDraw,
            VillainAggressorCheckedPreviousStreet = !PostflopContext.VillainBetTurn && !riverIsAggressor,
            VillainBarreling = PostflopContext.VillainBetTurn && maxBet > 0,
            PairClassification = riverResult.PairType,
            FoldEquity = _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(state), riverResult.FoldEquity),
            VillainBetSizeTurn = PostflopContext.VillainBetSizeTurn,
            VillainCheckedMiddleStreet = PostflopContext.VillainCheckedMiddleStreet,
            VillainType = GetVillainType(state, inPosition),
            VillainFoldToBetPct = _opponentTracker.GetFoldToBetPct(GetActiveVillainId(state)),
            HeroKickerStrength = riverResult.HeroKickerStrength,
            TurnCalledWithFlushDanger = PostflopContext.TurnCalledWithFlushDanger,
            HeroBlocksTopCard = HeroBlocksTopBoardCard(state),
            HeroCheckedAllStreets = PostflopContext.HeroCheckedAllStreets,
            IsAnyoneAllIn = PostflopContext.IsAnyoneAllIn
        });

        if (maxBet > 0)
            _opponentTracker.RecordPostflopAction(GetActiveVillainId(state), PostflopAction.Bet);

        // Actualizar contexto
        string action = decision.Action;
        PostflopContext.PreviousStreetWasBet = maxBet > 0;

        // Exploitabilidad
        double effectiveEquity = equity - dangerPenalty;
        var foldEquity = _opponentTracker.GetAdjustedFoldEquity(GetActiveVillainId(state), riverResult.FoldEquity);
        var riverAnalysis = _exploitabilityCalculator.AnalyzeDecision(
            decision.Action, effectiveEquity, riverResult.PotOddsPercentage, foldEquity,
            BoardPosition.River, effectiveSituation, inPosition, texture, potSize, betSize);
        _exploitabilityCalculator.RecordDecision(new DecisionRecord
        {
            OurDecision = decision.Action, Equity = effectiveEquity,
            PotOdds = riverResult.PotOddsPercentage, FoldEquity = foldEquity,
            Street = BoardPosition.River, Situation = effectiveSituation,
            IsInPosition = inPosition, BoardTexture = texture, PotSize = potSize,
            VillainBetSize = betSize, OurDecisionEV = riverAnalysis.OurDecisionEV,
            BestResponseEV = riverAnalysis.BestResponseEV,
            ExploitabilityMbb = riverAnalysis.ExploitabilityMbb
        });

        // Log
        double spr = potSize > 0 ? (double)(state.HeroStack / potSize) : 0;
        var dangerFlags = new List<string>();
        if (combinedBoardChange.FlushCompleted) dangerFlags.Add("Flush completado");
        if (combinedBoardChange.StraightCompleted) dangerFlags.Add("Straight completado");
        if (combinedBoardChange.FlushDrawAppeared) dangerFlags.Add("Flush draw");
        if (combinedBoardChange.BoardPaired) dangerFlags.Add("Board pareado");
        if (combinedBoardChange.OvercardAppeared) dangerFlags.Add("Overcard");
        string dangerStr = dangerFlags.Count > 0 ? string.Join(", ", dangerFlags) : "Ninguno";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"═══ [RIVER] ═══════════════════════════════════════");
        sb.AppendLine($"  {FormatCardsForLog(state, BoardPosition.River)}");
        sb.AppendLine($"  Pot: {potSize:F0}  |  Bet villano: {maxBet:F0} ({betSize})  |  Stack hero: {state.HeroStack:F0}  |  SPR: {spr:F1}");
        sb.AppendLine($"  Situación: {effectiveSituation}{(isDonkBet ? " (DONK BET)" : "")}  |  Posición: {(inPosition ? "IP" : "OOP")}  |  Oponentes: {numOpponents}");
        sb.AppendLine($"  Equity: {equity:F1}%  |  Danger: -{dangerPenalty:F1} ({dangerStr})  |  Pot odds: {riverResult.PotOddsPercentage:F1}%");
        sb.AppendLine($"  Mano: {riverResult.HeroHandRank} ({riverResult.PairType}, Kicker: {riverResult.HeroKickerStrength})");
        sb.AppendLine($"  ► Decisión: {action}{(decision.IsBluff ? " [BLUFF]" : "")}{(decision.IsBarrel ? " [BARREL]" : "")}{(decision.IsCheckRaise ? " [CHECK-RAISE]" : "")}");
        if (!string.IsNullOrEmpty(decision.Reason))
            sb.AppendLine($"    Razón: {decision.Reason}");

        _gameLoggerService.LogStreetDecision(new StreetDecision(
            Street: BoardPosition.River,
            EquityPercent: effectiveEquity,
            PotOddsPercent: riverResult.PotOddsPercentage,
            ExpectedValue: 0,
            RecommendedAction: action,
            ActionTaken: action,
            PotSizeAtDecision: potSize,
            BetSize: maxBet,
            Situation: effectiveSituation,
            IsInPosition: inPosition,
            Reason: decision.Reason,
            BoardTexture: texture,
            TotalOuts: riverResult.TotalOuts,
            SPR: spr
        ));
        var riverCardNames = state.BoardCards
            .Where(b => b.Position == BoardPosition.River)
            .Select(b => b.Name ?? "").ToList();
        _gameLoggerService.UpdateBoard(riverCardNames);

        return new GameDecisionResult(action, sb.ToString(), decision, combinedBoardChange, texture);
    }

    #endregion
}
