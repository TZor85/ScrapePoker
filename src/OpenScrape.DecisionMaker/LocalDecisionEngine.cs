using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker;

/// <summary>
/// Motor local: traduce el contrato crudo al pedido interno de cada calle y
/// despacha. No agrega lógica propia, sólo normaliza la entrada y la salida.
/// </summary>
public sealed class LocalDecisionEngine(
    PreflopRequestFactory preflopFactory,
    PostflopRequestFactory postflopFactory,
    SetPreflopActionUseCase preflop,
    PostflopDecisionEngine postflop,
    IPokerCalculator calculator) : IDecisionEngine
{
    private readonly PreflopRequestFactory _preflopFactory =
        preflopFactory ?? throw new ArgumentNullException(nameof(preflopFactory));

    private readonly PostflopRequestFactory _postflopFactory =
        postflopFactory ?? throw new ArgumentNullException(nameof(postflopFactory));

    private readonly SetPreflopActionUseCase _preflop =
        preflop ?? throw new ArgumentNullException(nameof(preflop));

    private readonly PostflopDecisionEngine _postflop =
        postflop ?? throw new ArgumentNullException(nameof(postflop));

    private readonly IPokerCalculator _calculator =
        calculator ?? throw new ArgumentNullException(nameof(calculator));

    public async Task<DecisionResult> DecideAsync(
        DecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Street == DecisionStreet.Preflop)
        {
            var internalRequest = _preflopFactory.Create(request, request.PotSize);
            var response = await _preflop.Execute(internalRequest).ConfigureAwait(false);

            return new DecisionResult
            {
                Action = response.ResponseAction.Action ?? string.Empty,
                // La equity preflop viaja para el resumen de la mesa: el cliente ya
                // no calcula nada por su cuenta.
                Equity = CalculatePreflopEquity(request)
            };
        }

        var postflopRequest = _postflopFactory.Create(request);
        var result = _postflop.Decide(postflopRequest);
        var calculation = result.Result;

        return new DecisionResult
        {
            Action = result.Action,
            Equity = result.EffectiveEquity,
            RawEquity = calculation.EquityPercentage,
            PotOdds = calculation.PotOddsPercentage,
            BetSize = calculation.SuggestedBetSize,
            Reason = result.Decision.Reason,
            BoardTexture = result.BoardTexture,
            BoardWetnessScore = calculation.BoardWetnessScore,
            EVWithFoldEquity = calculation.EVWithFoldEquity,
            IsBluff = result.Decision.IsBluff,
            IsBarrel = result.Decision.IsBarrel,
            IsCheckRaise = result.Decision.IsCheckRaise,
            IsFloating = result.Decision.IsFloating,
            FoldEquity = calculation.FoldEquity,
            TotalOuts = calculation.TotalOuts,
            EffectiveOuts = calculation.EffectiveOuts,
            DrawTypes = calculation.DrawTypes,
            HasComboDraw = calculation.HasComboDraw,
            HeroHandRank = calculation.HeroHandRank.ToString(),
            PairClassification = calculation.PairType.ToString(),
            KickerStrength = calculation.HeroKickerStrength.ToString()
        };
    }

    private double CalculatePreflopEquity(DecisionRequest request)
    {
        List<CardDataOuts> holeCards =
        [
            new CardDataOuts((Suit)request.HeroCard1.Suit, (Rank)request.HeroCard1.Rank),
            new CardDataOuts((Suit)request.HeroCard2.Suit, (Rank)request.HeroCard2.Rank)
        ];

        // Mismo cálculo que el motor: Monte Carlo contra el rango del villano.
        // Las cartas comunitarias vacías son lo que marca la calle preflop.
        var result = _calculator.Calculate(
            holeCards,
            [],
            request.PotSize,
            request.BetToCall,
            numOpponents: Math.Max(1, request.NumOpponents),
            isInPosition: request.IsInPosition,
            heroStack: request.HeroStack,
            villainStack: request.VillainStack,
            handSituation: request.HandSituation.ToString(),
            villainPosition: request.VillainPosition,
            opponentProfile: ToOpponentProfile(request.VillainProfile));

        return result.Failed ? 0d : result.EquityPercentage;
    }

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
