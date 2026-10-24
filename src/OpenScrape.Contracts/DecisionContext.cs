namespace OpenScrape.Contracts;

/// <summary>
/// Contexto cross-street que sólo el cliente conoce. Son hechos, no derivados.
/// </summary>
public sealed record DecisionContext
{
    public bool PreviousStreetBet { get; init; }
    public bool VillainAggressorCheckedPreviousStreet { get; init; }
    public bool VillainBarreling { get; init; }
    public bool HeroFloatedFlop { get; init; }
    public BetSizeCategory VillainBetSizeFlop { get; init; }
    public BetSizeCategory VillainBetSizeTurn { get; init; }
    public bool VillainCheckedMiddleStreet { get; init; }
    public bool TurnCalledWithFlushDanger { get; init; }
    public bool HeroCheckedAllStreets { get; init; }
    public bool IsAnyoneAllIn { get; init; }
    public bool IsDonkBet { get; init; }
}
