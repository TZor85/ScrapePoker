using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.DecisionMaker.Preflop;
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
    PreflopEquityCalculator preflopEquity) : IDecisionEngine
{
    private readonly PreflopRequestFactory _preflopFactory =
        preflopFactory ?? throw new ArgumentNullException(nameof(preflopFactory));

    private readonly PostflopRequestFactory _postflopFactory =
        postflopFactory ?? throw new ArgumentNullException(nameof(postflopFactory));

    private readonly SetPreflopActionUseCase _preflop =
        preflop ?? throw new ArgumentNullException(nameof(preflop));

    private readonly PostflopDecisionEngine _postflop =
        postflop ?? throw new ArgumentNullException(nameof(postflop));

    private readonly PreflopEquityCalculator _preflopEquity =
        preflopEquity ?? throw new ArgumentNullException(nameof(preflopEquity));

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

        // El motor ajusta por número de rivales; el 0 del cliente significa "sin dato".
        return _preflopEquity.GetEquity(holeCards, Math.Max(0, request.NumOpponents)) * 100d;
    }
}
