using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Fachada que compone equity → textura → perfil → decisión → sizing en una
/// única llamada. No contiene lógica de poker propia: delega en los servicios
/// subyacentes. Cualquier nueva heurística debe añadirse al servicio
/// correspondiente, no aquí.
/// </summary>
public sealed class PokerDecisionFacade : IPokerDecisionFacade
{
    private readonly IPokerCalculator _calculator;
    private readonly IPostflopDecisionService _decisionService;
    private readonly IBetSizingService _betSizingService;
    private readonly IBoardTextureAnalyzer _boardTextureAnalyzer;
    private readonly IOpponentTracker _opponentTracker;
    private readonly IMetricsCollector _metrics;
    private readonly IDecisionTraceStore _decisionTraceStore;
    private readonly ILogger<PokerDecisionFacade> _logger;

    public PokerDecisionFacade(
        IPokerCalculator calculator,
        IPostflopDecisionService decisionService,
        IBetSizingService betSizingService,
        IBoardTextureAnalyzer boardTextureAnalyzer,
        IOpponentTracker opponentTracker,
        IMetricsCollector metrics,
        IDecisionTraceStore? decisionTraceStore = null,
        ILogger<PokerDecisionFacade>? logger = null)
    {
        _calculator = calculator;
        _decisionService = decisionService;
        _betSizingService = betSizingService;
        _boardTextureAnalyzer = boardTextureAnalyzer;
        _opponentTracker = opponentTracker;
        _metrics = metrics;
        _decisionTraceStore = decisionTraceStore ?? NullDecisionTraceStore.Instance;
        _logger = logger ?? NullLogger<PokerDecisionFacade>.Instance;
    }

    public async Task<DecisionResult> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var _totalTimer = _metrics.Measure(TelemetryCategories.DecisionTotal);

        // --- Fase 1: equity y outs (Monte Carlo / enumeración) ---
        PokerCalculationResult calculation;
        OpponentProfile? profile;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionEquity))
        {
            profile = ResolveVillainProfile(request);
            calculation = _calculator.Calculate(
                playerHand: [.. request.HeroCards],
                communityCards: [.. request.CommunityCards],
                currentPotSize: request.PotSize,
                betToCall: request.BetToCall,
                numOpponents: request.NumOpponents,
                monteCarloIterations: request.MonteCarloIterations,
                isInPosition: request.IsInPosition,
                heroStack: request.HeroStack,
                villainStack: request.VillainStack,
                handSituation: request.HandSituationTag ?? request.Situation.ToString(),
                villainPosition: request.VillainPosition,
                opponentProfile: profile);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 2: textura del board y board change ---
        BoardTextureResult textureResult;
        BoardChangeResult? boardChange;
        RiverCardType riverCardType;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionTexture))
        {
            textureResult = _boardTextureAnalyzer.Analyze([.. request.CommunityCards]);
            boardChange = ComputeBoardChange(request);
            riverCardType = request.Street == BoardPosition.River && boardChange != null
                ? _boardTextureAnalyzer.ClassifyRiverCard(boardChange)
                : RiverCardType.Neutral;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 3: perfil del oponente ---
        OpponentType villainType;
        double villainFoldToBetPct;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionProfile))
        {
            villainType = profile?.HasReliablePreflopData == true
                ? (request.IsInPosition ? profile.GetTypeForPosition(false) : profile.GetTypeForPosition(true))
                : OpponentType.Unknown;
            villainFoldToBetPct = profile?.HasReliableFoldData == true
                ? _opponentTracker.GetFoldToBetPct(request.VillainId)
                : -1.0;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 4: decisión postflop ---
        PostflopDecisionResult decision;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionDecisionService))
        {
            var hasFlushDraw = calculation.DrawTypes?.Any(d =>
                d.Contains("Flush", StringComparison.OrdinalIgnoreCase) &&
                !d.Contains("Backdoor", StringComparison.OrdinalIgnoreCase)) ?? false;

            var input = new PostflopDecisionInput
            {
                Equity = calculation.EquityPercentage,
                Street = request.Street,
                Situation = request.Situation,
                BoardTexture = textureResult.SimplifiedTexture,
                IsInPosition = request.IsInPosition,
                VillainBetSize = request.VillainBetSize,
                PotOdds = calculation.PotOddsPercentage,
                TotalOuts = calculation.TotalOuts,
                PreviousStreetBet = request.PreviousStreetBet,
                VillainShowedAggression = request.VillainShowedAggression,
                BoardChange = boardChange,
                HeroBlocksDangerSuit = request.HeroBlocksDangerSuit,
                HeroStack = request.HeroStack,
                PotSize = request.PotSize,
                HasFlushDraw = hasFlushDraw,
                NumOpponents = request.NumOpponents,
                HeroIsAggressor = request.HeroIsAggressor,
                HeroHandRank = calculation.HeroHandRank,
                HasComboDraw = calculation.HasComboDraw,
                VillainAggressorCheckedPreviousStreet = request.VillainAggressorCheckedPreviousStreet,
                VillainBarreling = request.VillainBarreling,
                VillainType = villainType,
                PairClassification = calculation.PairType,
                FoldEquity = calculation.FoldEquity,
                VillainBetSizeFlop = request.VillainBetSizeFlop,
                VillainBetSizeTurn = request.VillainBetSizeTurn,
                VillainCheckedMiddleStreet = request.VillainCheckedMiddleStreet,
                HeroHasNutBlocker = request.HeroHasNutBlocker,
                HeroFloatedFlop = request.HeroFloatedFlop,
                VillainFoldToBetPct = villainFoldToBetPct,
                HeroKickerStrength = calculation.HeroKickerStrength,
                TurnCalledWithFlushDanger = request.TurnCalledWithFlushDanger,
                HeroBlocksTopCard = request.HeroBlocksTopCard,
                HeroCheckedAllStreets = request.HeroCheckedAllStreets,
                IsAnyoneAllIn = request.IsAnyoneAllIn,
                IsDonkBet = request.IsDonkBet,
                VillainProfile = profile,
                HeroPosition = request.HeroPosition,
                VillainPosition = request.VillainPosition,
                IsBroadwayWet = request.IsBroadwayWet,
                EffectiveOuts = calculation.EffectiveOuts,
                RiverCardType = riverCardType,
            };

            decision = _decisionService.DetermineAction(input);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 5: sizing (extracción del string de acción) ---
        double? betSize;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionSizing))
        {
            betSize = ExtractBetSize(decision.Action);
        }

        var result = new DecisionResult
        {
            RecommendedAction = decision.Action,
            EquityPercent = calculation.EquityPercentage,
            Reason = decision.Reason ?? $"{request.Street}/{request.Situation}",
            BoardTexture = textureResult.SimplifiedTexture,
            BetSize = betSize,
            PotOddsPercent = calculation.PotOddsPercentage,
            ExpectedValue = calculation.ExpectedValue,
            IsBluff = decision.IsBluff,
            IsBarrel = decision.IsBarrel,
            IsCheckRaise = decision.IsCheckRaise,
            IsFloating = decision.IsFloating,
            CalculationDetail = calculation,
        };

        await TryPersistDecisionTraceAsync(
            request,
            result,
            calculation,
            profile,
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async Task TryPersistDecisionTraceAsync(
        DecisionRequest request,
        DecisionResult result,
        PokerCalculationResult calculation,
        OpponentProfile? profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var trace = new DecisionTrace
        {
            Street = request.Street,
            Situation = request.Situation,
            HandSituationTag = request.HandSituationTag,
            HeroCards = request.HeroCards.Select(FormatCard).ToList(),
            CommunityCards = request.CommunityCards.Select(FormatCard).ToList(),
            VillainId = profile?.PlayerId ?? request.VillainId,
            HeroPosition = request.HeroPosition,
            VillainPosition = request.VillainPosition,
            IsInPosition = request.IsInPosition,
            NumOpponents = request.NumOpponents,
            HeroStack = request.HeroStack,
            VillainStack = request.VillainStack,
            PotSize = request.PotSize,
            BetToCall = request.BetToCall,
            RecommendedAction = result.RecommendedAction,
            Reason = result.Reason,
            BoardTexture = result.BoardTexture,
            BetSize = result.BetSize,
            EquityPercent = result.EquityPercent,
            PotOddsPercent = result.PotOddsPercent,
            ExpectedValue = result.ExpectedValue,
            TotalOuts = calculation.TotalOuts,
            IsBluff = result.IsBluff,
            IsBarrel = result.IsBarrel,
            IsCheckRaise = result.IsCheckRaise,
            IsFloating = result.IsFloating,
            HeroHandRank = calculation.HeroHandRank.ToString(),
            HeroKickerStrength = calculation.HeroKickerStrength.ToString(),
            PairClassification = calculation.PairType.ToString(),
            HasComboDraw = calculation.HasComboDraw,
            FoldEquity = calculation.FoldEquity,
        };

        try
        {
            await _decisionTraceStore.SaveAsync(trace, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persistiendo DecisionTrace para {Street}/{Situation}", request.Street, request.Situation);
        }
    }

    private static string FormatCard(CardDataOuts card) => card.Id;

    private OpponentProfile? ResolveVillainProfile(DecisionRequest request)
    {
        if (request.VillainProfile is not null)
            return request.VillainProfile;
        if (string.IsNullOrEmpty(request.VillainId) || request.VillainId == "Unknown")
            return null;
        var profile = _opponentTracker.GetProfile(request.VillainId);
        return profile.HasReliablePreflopData ? profile : null;
    }

    private BoardChangeResult? ComputeBoardChange(DecisionRequest request)
    {
        if (request.CommunityCards.Count == 0)
            return null;

        return request.Street switch
        {
            BoardPosition.Flop => _boardTextureAnalyzer.AnalyzeInitialBoard([.. request.CommunityCards]),
            BoardPosition.Turn or BoardPosition.River when request.PreviousBoard is { Count: > 0 }
                => _boardTextureAnalyzer.AnalyzeBoardChange(
                    [.. request.PreviousBoard],
                    request.CommunityCards[^1]),
            _ => null,
        };
    }

    private static double? ExtractBetSize(string action)
    {
        if (string.IsNullOrEmpty(action))
            return null;
        var idx = action.IndexOf(' ');
        if (idx < 0)
            return null;
        var size = action[(idx + 1)..];
        return size switch
        {
            "1/4" => 0.25,
            "1/3" => 0.33,
            "1/2" => 0.5,
            "2/3" => 0.66,
            "3/4" => 0.75,
            "Pot" => 1.0,
            _ => null,
        };
    }
}
