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
}
