namespace OpenScrape.Contracts;

/// <summary>
/// Decisión devuelta por el servidor. Todos sus campos son derivados: el cliente
/// los usa para loguear y para su telemetría, nunca para decidir.
/// </summary>
public sealed record DecisionResult
{
    public required string Action { get; init; }

    public double Equity { get; init; }
    public double PotOdds { get; init; }
    public double? BetSize { get; init; }
    public string? Reason { get; init; }
    public string? BoardTexture { get; init; }

    /// <summary>Puntuación de humedad del board (0-100), para el overlay.</summary>
    public double? BoardWetnessScore { get; init; }

    /// <summary>
    /// Equity calculada antes de los ajustes del motor (c-bet en flop). El overlay
    /// la muestra tal como la mostraba el cliente; si no viaja, cae en <see cref="Equity"/>.
    /// </summary>
    public double? RawEquity { get; init; }

    /// <summary>EV considerando fold equity, para el overlay.</summary>
    public double? EVWithFoldEquity { get; init; }

    /// <summary>Situación preflop resultante (Call, ThreeBet, ...), para la calle siguiente.</summary>
    public string? Situation { get; init; }

    public bool IsBluff { get; init; }
    public bool IsBarrel { get; init; }
    public bool IsCheckRaise { get; init; }
    public bool IsFloating { get; init; }

    public double FoldEquity { get; init; }
    public int TotalOuts { get; init; }
    public double EffectiveOuts { get; init; }
    public IReadOnlyList<string> DrawTypes { get; init; } = [];
    public bool HasComboDraw { get; init; }

    public string? HeroHandRank { get; init; }
    public string? PairClassification { get; init; }
    public string? KickerStrength { get; init; }
}
