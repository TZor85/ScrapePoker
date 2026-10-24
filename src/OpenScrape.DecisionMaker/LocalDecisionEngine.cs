using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker;

/// <summary>
/// Motor local: despacha la calle al camino preflop o al motor postflop.
/// No agrega lógica propia, sólo normaliza la entrada y la salida.
/// </summary>
public sealed class LocalDecisionEngine(
    SetPreflopActionUseCase preflop,
    PostflopDecisionEngine postflop) : IDecisionEngine
{
    private readonly SetPreflopActionUseCase _preflop =
        preflop ?? throw new ArgumentNullException(nameof(preflop));

    private readonly PostflopDecisionEngine _postflop =
        postflop ?? throw new ArgumentNullException(nameof(postflop));

    public async Task<DecisionResult> DecideAsync(
        DecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Street == DecisionStreet.Preflop)
        {
            if (request.Preflop is null)
                throw new ArgumentException("El pedido preflop no trae su bloque de entrada.", nameof(request));

            var response = await _preflop.Execute(request.Preflop).ConfigureAwait(false);

            return new DecisionResult(
                Action: response.ResponseAction.Action ?? string.Empty,
                Equity: 0d);
        }

        if (request.Postflop is null)
            throw new ArgumentException("El pedido postflop no trae su bloque de entrada.", nameof(request));

        var postflopRequest = request.Postflop with { Street = ToBoardPosition(request.Street) };
        var result = _postflop.Decide(postflopRequest);

        return new DecisionResult(
            Action: result.Action,
            Equity: result.EffectiveEquity,
            Reason: result.Decision.Reason,
            BoardTexture: result.BoardTexture,
            IsBluff: result.Decision.IsBluff,
            IsBarrel: result.Decision.IsBarrel,
            IsCheckRaise: result.Decision.IsCheckRaise);
    }

    private static BoardPosition ToBoardPosition(DecisionStreet street) => street switch
    {
        DecisionStreet.Flop => BoardPosition.Flop,
        DecisionStreet.Turn => BoardPosition.Turn,
        DecisionStreet.River => BoardPosition.River,
        _ => throw new ArgumentOutOfRangeException(nameof(street), street, "La calle no es postflop")
    };
}
