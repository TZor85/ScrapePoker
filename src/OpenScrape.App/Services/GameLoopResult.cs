using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

/// <summary>
/// Resultado inmutable de una iteración del game loop. Emitido por
/// <see cref="IGameLoopCoordinator.ResultReady"/> para que los suscriptores
/// (UI, logging) lo proyecten sin conocer el pipeline interno.
/// </summary>
public sealed record GameLoopResult
{
    /// <summary>Cuándo se produjo la iteración. Útil para ordenar eventos.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>True si la iteración no produjo decisión (p.ej. mesa vacía, estado de espera).</summary>
    public bool Empty { get; init; }

    /// <summary>Excepción no-null si la iteración falló; en ese caso los demás campos son por defecto.</summary>
    public Exception? Error { get; init; }

    /// <summary>Calle detectada por el state machine en esta iteración.</summary>
    public BoardPosition? Street { get; init; }

    /// <summary>Acción recomendada en texto legible (espejo de <see cref="DecisionResult.RecommendedAction"/>).</summary>
    public string? RecommendedAction { get; init; }

    /// <summary>Resultado completo del facade cuando hubo decisión postflop.</summary>
    public DecisionResult? DecisionResult { get; init; }

    /// <summary>Texto de log asociado (bloque estructurado por street).</summary>
    public string? LogText { get; init; }

    /// <summary>True si la mano actual finalizó en esta iteración.</summary>
    public bool HandCompleted { get; init; }

    /// <summary>True si se detectó el inicio de una nueva mano.</summary>
    public bool NewHandDetected { get; init; }

    // ==================== Campos RF4 (HandProcessor) ====================

    /// <summary>Pot odds calculadas para la decisión actual.</summary>
    public decimal? PotOdds { get; init; }

    /// <summary>Equity calculado para la mano del héroe.</summary>
    public double? Equity { get; init; }

    /// <summary>Cartas del héroe (face1, face2).</summary>
    public (string, string)? HoleCards { get; init; }

    /// <summary>Cartas comunitarias visibles en el board.</summary>
    public IReadOnlyList<string> BoardCards { get; init; } = Array.Empty<string>();

    /// <summary>Apuestas de cada jugador (nombre → valor).</summary>
    public IReadOnlyDictionary<string, decimal> PlayerBets { get; init; } = new Dictionary<string, decimal>();

    /// <summary>Posición del héroe en la mesa.</summary>
    public TablePosition? HeroPosition { get; init; }

    /// <summary>Situación de mano detectada (OpenRaise, ThreeBet, etc.).</summary>
    public HandSituation? HandSituation { get; init; }

    /// <summary>Acción recomendada en formato texto (p.ej. "Bet 1/2", "Fold").</summary>
    public string? ResponseAction { get; init; }

    /// <summary>Nombre de la mesa detectado.</summary>
    public string? TableName { get; init; }

    /// <summary>Texto de resumen para el log / tbResume.</summary>
    public string? SummaryText { get; init; }
}
