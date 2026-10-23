namespace OpenScrape.App.Services;

/// <summary>
/// Latch de detección de turno extraído de <c>DetectionLoopService._executeCapture</c>.
/// Sin dependencias: es lógica pura sobre el estado del último turno detectado.
/// </summary>
public sealed class TurnGate : ITurnGate
{
    // volatile: el loop de detección escribe; la UI y los tests leen.
    private volatile bool _latched;

    public bool IsLatched => _latched;

    public TurnGateDecision Evaluate(bool isActionColorInRange, bool isFlopVisible)
    {
        bool shouldCaptureTurn = isActionColorInRange && !_latched;
        bool shouldCaptureFlop = shouldCaptureTurn && isFlopVisible;

        if (shouldCaptureTurn)
        {
            _latched = true;
        }
        else if (!isActionColorInRange)
        {
            // El color salió del rango: el héroe ya actuó, habilitar la próxima detección.
            _latched = false;
        }

        return new TurnGateDecision(shouldCaptureTurn, shouldCaptureFlop);
    }

    public void Reset() => _latched = false;

    public void ForceLatched(bool value) => _latched = value;
}
