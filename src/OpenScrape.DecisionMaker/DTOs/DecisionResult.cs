using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.DTOs;

/// <summary>
/// Salida inmutable del facade de decisión postflop. Agrega los resultados
/// de equity, textura, decisión y sizing, junto con telemetría de latencia
/// por fase para auditoría y detección de regresiones de rendimiento.
/// </summary>
public sealed record DecisionResult
{
    /// <summary>Acción recomendada en formato legible (p.ej. "Bet 1/2", "Call", "Fold").</summary>
    public required string RecommendedAction { get; init; }

    /// <summary>Equity del hero en porcentaje (0-100).</summary>
    public required double EquityPercent { get; init; }

    /// <summary>
    /// Explicación no-null del path de decisión tomado (p.ej. "FacingBet → Raise (TwoPair+)").
    /// Requerido para backtest y debugging.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>Textura del board en string legible (p.ej. "Dry", "Wet").</summary>
    public string? BoardTexture { get; init; }

    /// <summary>Tamaño de apuesta sugerido (porcentaje del pote), si aplica.</summary>
    public double? BetSize { get; init; }

    /// <summary>Pot odds en porcentaje (0-100).</summary>
    public double PotOddsPercent { get; init; }

    /// <summary>Expected value de la acción recomendada.</summary>
    public double ExpectedValue { get; init; }

    /// <summary>Flags descriptivos del path tomado.</summary>
    public bool IsBluff { get; init; }
    public bool IsBarrel { get; init; }
    public bool IsCheckRaise { get; init; }
    public bool IsFloating { get; init; }

    /// <summary>
    /// Resultado crudo del cálculo de equity (para consumidores que necesitan detalle).
    /// Se expone como opaque para no filtrar implementación.
    /// </summary>
    public object? CalculationDetail { get; init; }

    /// <summary>
    /// Latencia de cada fase del pipeline: "equity", "texture", "profile",
    /// "decision", "sizing". Útil para detectar regresiones.
    /// </summary>
    public IReadOnlyDictionary<string, TimeSpan> PhaseTimings { get; init; }
        = new Dictionary<string, TimeSpan>();
}
