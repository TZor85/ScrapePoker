namespace OpenScrape.DecisionMaker;

/// <summary>
/// Calle sobre la que se pide una decisión. Es independiente de la posición de una
/// carta en el board, que es lo que modela <c>BoardPosition</c>.
/// </summary>
public enum DecisionStreet
{
    Preflop = 0,
    Flop = 1,
    Turn = 2,
    River = 3
}

/// <summary>
/// Pedido de decisión. Cada calle trae su propio bloque de entrada ya resuelto:
/// el preflop usa el estado de mesa y el postflop los valores calculados por el coordinador.
/// </summary>
public sealed record DecisionRequest
{
    public DecisionStreet Street { get; init; }

    public Preflop.SetPreflopActionUseCaseRequest? Preflop { get; init; }

    public Postflop.PostflopEngineRequest? Postflop { get; init; }
}

/// <summary>
/// Resultado uniforme de una decisión, sin importar la calle que la produjo.
/// </summary>
public sealed record DecisionResult(
    string Action,
    double Equity = 0d,
    string? Reason = null,
    string? BoardTexture = null,
    bool IsBluff = false,
    bool IsBarrel = false,
    bool IsCheckRaise = false);

/// <summary>
/// Puerta única de decisión. En esta fase la implementación es local; en la fase 3
/// el cliente la resuelve por HTTP contra la API.
/// </summary>
public interface IDecisionEngine
{
    Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default);
}
