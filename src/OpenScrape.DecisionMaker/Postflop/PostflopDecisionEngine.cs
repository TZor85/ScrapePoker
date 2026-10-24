using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Postflop;

/// <summary>
/// Todo lo que el motor necesita para decidir una calle. El llamador resuelve los
/// valores que dependen de estado (tracking del villano, contexto cross-street,
/// textura del board) y el motor compone el input y decide.
/// </summary>
public sealed record PostflopEngineRequest
{
    public required PlayerGameState State { get; init; }
    public required PokerCalculationResult Result { get; init; }
    public required BoardPosition Street { get; init; }

    public HandSituation Situation { get; init; }
    public string BoardTexture { get; init; } = string.Empty;
    public BetSizeCategory VillainBetSize { get; init; }
    public bool VillainShowedAggression { get; init; }
    public BoardChangeResult? BoardChange { get; init; }
    public bool IsDonkBet { get; init; }

    /// <summary>Ajuste de c-bet que el coordinador aplica a la equity en flop.</summary>
    public double CbetAdjustment { get; init; }

    public double FoldEquity { get; init; }
    public OpponentType VillainType { get; init; }
    public double VillainFoldToBetPct { get; init; } = -1;
    public bool HeroBlocksTopCard { get; init; }
    public bool HeroIsAggressor { get; init; }
    public bool IsAnyoneAllIn { get; init; }
    public bool HasFlushDraw { get; init; }
    public int NumOpponents { get; init; } = 1;
    public bool HeroBlocksDangerSuit { get; init; }

    public bool PreviousStreetBet { get; init; }
    public bool VillainAggressorCheckedPreviousStreet { get; init; }
    public bool VillainBarreling { get; init; }
    public bool HeroFloatedFlop { get; init; }
    public BetSizeCategory VillainBetSizeFlop { get; init; }
    public BetSizeCategory VillainBetSizeTurn { get; init; }
    public bool VillainCheckedMiddleStreet { get; init; }
    public bool TurnCalledWithFlushDanger { get; init; }
    public bool HeroCheckedAllStreets { get; init; }
    public RiverCardType RiverCardType { get; init; } = RiverCardType.Neutral;
}

/// <summary>Resultado del motor: la decisión más los datos que el llamador necesita para loguear.</summary>
public sealed record PostflopDecisionEngineResult(
    PostflopDecisionResult Decision,
    string BoardTexture,
    BoardChangeResult? BoardChange,
    double EffectiveEquity,
    PokerCalculationResult Result)
{
    public string Action => Decision.Action;
}

/// <summary>
/// Compone el <see cref="PostflopDecisionInput"/> y decide. No toca contexto,
/// tracking, logging ni exploitability: eso queda en el coordinador.
/// </summary>
public sealed class PostflopDecisionEngine
{
    private readonly IPostflopDecisionService _decisionService;

    public PostflopDecisionEngine(IPostflopDecisionService decisionService)
    {
        _decisionService = decisionService ?? throw new ArgumentNullException(nameof(decisionService));
    }

    public PostflopDecisionEngineResult Decide(PostflopEngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = request.Result;
        var equity = request.Street == BoardPosition.Flop
            ? ClampEquity(result.EquityPercentage + request.CbetAdjustment)
            : result.EquityPercentage;

        var input = new PostflopDecisionInput
        {
            Equity = equity,
            Street = request.Street,
            Situation = request.Situation,
            BoardTexture = request.BoardTexture,
            IsInPosition = request.State.IsInPosition,
            VillainBetSize = request.VillainBetSize,
            PotOdds = result.PotOddsPercentage,
            TotalOuts = result.TotalOuts,
            EffectiveOuts = result.EffectiveOuts,
            PreviousStreetBet = request.PreviousStreetBet,
            VillainShowedAggression = request.VillainShowedAggression,
            BoardChange = request.BoardChange,
            HeroBlocksDangerSuit = request.HeroBlocksDangerSuit,
            HeroStack = request.State.HeroStack,
            PotSize = request.State.PotSize,
            HasFlushDraw = request.HasFlushDraw,
            NumOpponents = request.NumOpponents,
            HeroIsAggressor = request.HeroIsAggressor,
            HeroHandRank = result.HeroHandRank,
            HasComboDraw = result.HasComboDraw,
            VillainAggressorCheckedPreviousStreet = request.VillainAggressorCheckedPreviousStreet,
            VillainBarreling = request.VillainBarreling,
            VillainType = request.VillainType,
            PairClassification = result.PairType,
            FoldEquity = request.FoldEquity,
            VillainBetSizeFlop = request.VillainBetSizeFlop,
            VillainBetSizeTurn = request.VillainBetSizeTurn,
            VillainCheckedMiddleStreet = request.VillainCheckedMiddleStreet,
            HeroFloatedFlop = request.HeroFloatedFlop,
            VillainFoldToBetPct = request.VillainFoldToBetPct,
            HeroKickerStrength = result.HeroKickerStrength,
            TurnCalledWithFlushDanger = request.TurnCalledWithFlushDanger,
            HeroBlocksTopCard = request.HeroBlocksTopCard,
            HeroCheckedAllStreets = request.HeroCheckedAllStreets,
            IsAnyoneAllIn = request.IsAnyoneAllIn,
            IsDonkBet = request.IsDonkBet,
            RiverCardType = request.RiverCardType
        };

        var decision = _decisionService.DetermineAction(input);

        return new PostflopDecisionEngineResult(
            decision,
            request.BoardTexture,
            request.BoardChange,
            equity,
            result);
    }

    private static double ClampEquity(double equity) => Math.Clamp(equity, 0d, 100d);
}
