namespace OpenScrape.App.Services;

/// <summary>
/// Decisión de captura para una iteración del detector de píxeles.
/// </summary>
/// <param name="ShouldCaptureTurn">Es el turno del héroe y todavía no se procesó en esta ronda.</param>
/// <param name="ShouldCaptureFlop">Además del turno, hay flop visible.</param>
public readonly record struct TurnGateDecision(bool ShouldCaptureTurn, bool ShouldCaptureFlop);

/// <summary>
/// Compuerta de detección de turno. Implementa el latch que evita re-disparar
/// la captura mientras el color de acción siga dentro del rango esperado.
/// Vive fuera del loop de píxeles para poder consultarse y testearse sin capturar.
/// </summary>
public interface ITurnGate
{
    /// <summary>Indica si ya se procesó el turno actual y todavía no salió del rango.</summary>
    bool IsLatched { get; }

    /// <summary>
    /// Evalúa el color detectado y devuelve qué corresponde capturar.
    /// Si el turno se detecta, la compuerta queda latcheada; si el color sale
    /// del rango, se libera.
    /// </summary>
    TurnGateDecision Evaluate(bool isActionColorInRange, bool isFlopVisible);

    /// <summary>Libera el latch (arranque de sesión o fin de mano).</summary>
    void Reset();

    /// <summary>Solo para tests y replay: fuerza el estado del latch.</summary>
    void ForceLatched(bool value);
}
