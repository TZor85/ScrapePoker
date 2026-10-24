using Microsoft.Extensions.Options;
using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Postflop;

/// <summary>
/// Traduce el contrato crudo de postflop al pedido del motor. Todo lo que es
/// función de las cartas (equity, outs, pot odds, textura, ajuste de c-bet) se
/// calcula acá, del lado del servidor.
/// </summary>
public sealed class PostflopRequestFactory
{
    private readonly IPokerCalculator _calculator;
    private readonly IBoardTextureAnalyzer _boardTextureAnalyzer;
    private readonly IPreflopAnalyzer _preflopAnalyzer;
    private readonly IOptions<StrategyProfile> _options;

    public PostflopRequestFactory(
        IPokerCalculator calculator,
        IBoardTextureAnalyzer boardTextureAnalyzer,
        IPreflopAnalyzer preflopAnalyzer,
        IOptions<StrategyProfile> options)
    {
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _boardTextureAnalyzer = boardTextureAnalyzer ?? throw new ArgumentNullException(nameof(boardTextureAnalyzer));
        _preflopAnalyzer = preflopAnalyzer ?? throw new ArgumentNullException(nameof(preflopAnalyzer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public PostflopEngineRequest Create(DecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var street = ToBoardPosition(request.Street);
        var playerHand = ToCards(request.HeroCard1, request.HeroCard2);
        var communityCards = request.Board.Select(card => new CardDataOuts((Suit)card.Suit, (Rank)card.Rank)).ToList();

        // El cliente históricamente calculó la equity con un solo rival por defecto
        // y usó el conteo real solo para el sizing. Se replica tal cual para no
        // cambiar ninguna decisión congelada en los fixtures.
        var result = _calculator.Calculate(
            playerHand,
            communityCards,
            request.PotSize,
            request.BetToCall,
            numOpponents: 1,
            request.MonteCarloIterations,
            request.IsInPosition,
            request.HeroStack,
            request.VillainStack,
            request.HandSituation.ToString(),
            request.VillainPosition,
            ToOpponentProfile(request.VillainProfile));

        if (result.Failed)
            throw new CalculationFailedException(result.FailureReason ?? "error desconocido");

        var texture = _boardTextureAnalyzer.Analyze(communityCards);
        var boardChange = street switch
        {
            BoardPosition.Flop => _boardTextureAnalyzer.AnalyzeInitialBoard(communityCards),
            BoardPosition.Turn => _boardTextureAnalyzer.AnalyzeBoardChange(communityCards.Take(3).ToList(), communityCards[3]),
            _ => _boardTextureAnalyzer.AnalyzeBoardChange(communityCards.Take(4).ToList(), communityCards[4])
        };

        double cbetAdjustment = 0d;
        if (street == BoardPosition.Flop)
        {
            var flopRanks = communityCards.Select(card => (int)card.Rank).ToList();
            var hasRangeAdvantage = _preflopAnalyzer.HasRangeAdvantageOnBoard(
                flopRanks, texture, request.HeroWasPreflopAggressor, request.HandSituation);

            cbetAdjustment = _preflopAnalyzer.CalculateCbetAdjustment(
                request.HeroWasPreflopAggressor,
                hasRangeAdvantage,
                texture,
                request.IsInPosition,
                request.NumOpponents,
                _options.Value);
        }

        var heroSuits = new[] { request.HeroCard1.Suit, request.HeroCard2.Suit };
        var heroBlocksDangerSuit = communityCards.Any(card => heroSuits.Contains((int)card.Suit));
        var topBoardRank = communityCards.Count == 0 ? 0 : communityCards.Max(card => (int)card.Rank);
        var heroBlocksTopCard = topBoardRank >= 10
            && (request.HeroCard1.Rank == topBoardRank || request.HeroCard2.Rank == topBoardRank);

        return new PostflopEngineRequest
        {
            State = BuildState(request, street, communityCards),
            Result = result,
            Street = street,
            Situation = request.HandSituation,
            BoardTexture = texture.ToString(),
            VillainBetSize = _preflopAnalyzer.CategorizeOpponentBet(request.BetToCall, request.PotSize),
            VillainShowedAggression = request.BetToCall > 0,
            BoardChange = boardChange,
            IsDonkBet = request.Context.IsDonkBet,
            CbetAdjustment = cbetAdjustment,
            FoldEquity = request.VillainProfile is { AdjustedFoldEquity: >= 0 } profile
                ? profile.AdjustedFoldEquity
                : result.FoldEquity,
            VillainType = request.VillainProfile?.Type ?? OpponentType.Unknown,
            VillainFoldToBetPct = request.VillainProfile?.FoldToBetPct ?? -1,
            HeroIsAggressor = request.IsHeroAggressor,
            HasFlushDraw = result.DrawTypes.Contains("Flush Draw"),
            NumOpponents = request.NumOpponents,
            HeroBlocksDangerSuit = heroBlocksDangerSuit,
            HeroBlocksTopCard = heroBlocksTopCard,
            PreviousStreetBet = request.Context.PreviousStreetBet,
            VillainAggressorCheckedPreviousStreet = request.Context.VillainAggressorCheckedPreviousStreet,
            VillainBarreling = request.Context.VillainBarreling,
            HeroFloatedFlop = request.Context.HeroFloatedFlop,
            VillainBetSizeFlop = request.Context.VillainBetSizeFlop,
            VillainBetSizeTurn = request.Context.VillainBetSizeTurn,
            VillainCheckedMiddleStreet = request.Context.VillainCheckedMiddleStreet,
            TurnCalledWithFlushDanger = request.Context.TurnCalledWithFlushDanger,
            HeroCheckedAllStreets = request.Context.HeroCheckedAllStreets,
            IsAnyoneAllIn = request.Context.IsAnyoneAllIn,
            RiverCardType = street == BoardPosition.River
                ? _boardTextureAnalyzer.ClassifyRiverCard(boardChange)
                : RiverCardType.Neutral
        };
    }

    private static PlayerGameState BuildState(
        DecisionRequest request,
        BoardPosition street,
        List<CardDataOuts> communityCards)
    {
        return new PlayerGameState
        {
            HoleCard1Rank = request.HeroCard1.Rank,
            HoleCard1Suit = request.HeroCard1.Suit,
            HoleCard2Rank = request.HeroCard2.Rank,
            HoleCard2Suit = request.HeroCard2.Suit,
            HoleCard1Face = ((Rank)request.HeroCard1.Rank).ToString(),
            HoleCard2Face = ((Rank)request.HeroCard2.Rank).ToString(),
            Position = request.HeroPosition,
            PotSize = request.PotSize,
            CurrentBet = request.BetToCall,
            HeroStack = request.HeroStack,
            IsInPosition = request.IsInPosition,
            HandSituation = request.HandSituation,
            Players =
            [
                new Player
                {
                    Name = "Villain",
                    Active = true,
                    Bet = request.BetToCall,
                    Stack = request.VillainStack,
                    Position = request.VillainPosition,
                    WasPreflopAggressor = request.HeroWasPreflopAggressor is false
                }
            ],
            BoardCards = communityCards
                .Select((card, index) => new BoardData
                {
                    Force = (int)card.Rank,
                    Suit = (int)card.Suit,
                    Position = street,
                    Location = index + 1
                })
                .ToList()
        };
    }

    private static List<CardDataOuts> ToCards(CardRef first, CardRef second) =>
    [
        new CardDataOuts((Suit)first.Suit, (Rank)first.Rank),
        new CardDataOuts((Suit)second.Suit, (Rank)second.Rank)
    ];

    private static BoardPosition ToBoardPosition(DecisionStreet street) => street switch
    {
        DecisionStreet.Flop => BoardPosition.Flop,
        DecisionStreet.Turn => BoardPosition.Turn,
        DecisionStreet.River => BoardPosition.River,
        _ => throw new ArgumentException("La fábrica postflop no acepta pedidos de preflop.", nameof(street))
    };

    private static OpponentProfile? ToOpponentProfile(VillainProfile? villain) => villain is null
        ? null
        : new OpponentProfile
        {
            PlayerId = villain.VillainId,
            HandsPlayed = villain.HandsPlayed,
            TimesVoluntarilyPutMoneyIn = villain.TimesVoluntarilyPutMoneyIn,
            TimesPreflopRaised = villain.TimesPreflopRaised,
            TimesThreeBet = villain.TimesThreeBet
        };
}
