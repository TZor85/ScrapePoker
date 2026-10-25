using OpenScrape.Domain.Enums;

namespace OpenScrape.Contracts;

/// <summary>
/// Pedido de decisión. Todos sus campos son hechos: cartas, montos, posición y
/// contexto. El servidor deriva equity, outs, pot odds, textura y ajustes.
/// </summary>
public sealed record DecisionRequest
{
    public required DecisionStreet Street { get; init; }
    public required CardRef HeroCard1 { get; init; }
    public required CardRef HeroCard2 { get; init; }

    public IReadOnlyList<CardRef> Board { get; init; } = [];

    public decimal PotSize { get; init; }
    public decimal BetToCall { get; init; }
    public decimal HeroStack { get; init; }
    public decimal VillainStack { get; init; }

    public bool IsInPosition { get; init; }
    public int NumOpponents { get; init; } = 1;
    public TablePosition HeroPosition { get; init; }
    public TablePosition VillainPosition { get; init; } = TablePosition.None;

    public HandSituation HandSituation { get; init; }
    public bool IsSecondAction { get; init; }
    public bool HeroWasPreflopAggressor { get; init; }
    public bool IsHeroAggressor { get; init; }

    public VillainProfile? VillainProfile { get; init; }
    public DecisionContext Context { get; init; } = new();

    /// <summary>Apuestas preflop por posición: reconstruyen raiser, caller y squeezer.</summary>
    public IReadOnlyDictionary<TablePosition, decimal> PreflopBets { get; init; }
        = new Dictionary<TablePosition, decimal>();
    public int? MonteCarloIterations { get; init; }
}
