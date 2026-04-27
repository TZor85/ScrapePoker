using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Recolector de métricas de rendimiento del pipeline. Singleton compartido entre
/// servicios scoped y la UI. Thread-safe.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Devuelve una medición ambiente. Uso: <c>using var _ = metrics.Measure("OCR.Cards");</c>
    /// </summary>
    ScopedMeasurement Measure(string category);

    /// <summary>
    /// Registra una medida manual. Usar cuando <c>using</c> no aplica (tests, código async
    /// con await en medio).
    /// </summary>
    void Record(string category, TimeSpan elapsed);

    /// <summary>
    /// Como <see cref="Measure"/> pero solo acumula en sesión, no en última mano.
    /// Útil para persistencia (fuera del bucket de mano).
    /// </summary>
    ScopedMeasurement MeasureSessionOnly(string category);

    /// <summary>
    /// Como <see cref="Record"/> pero solo acumula en el agregado de sesión, no en el de
    /// la última mano. Útil para <c>Persistence.SaveHand</c>, que se mide después del
    /// snapshot de mano y por tanto no cabe en <c>HandRecord.Telemetry</c>.
    /// </summary>
    void RecordSessionOnly(string category, TimeSpan elapsed);

    /// <summary>
    /// Marca el inicio de una nueva mano. Descarta el bucket de "última mano" previo
    /// (con log si tenía muestras) y reinicia los contadores de mano.
    /// </summary>
    void StartHand(string handId);

    /// <summary>
    /// Cierra la mano en curso: devuelve snapshot agregado, lo fusiona en el bucket de
    /// sesión y reinicia el bucket de última mano. Devuelve <c>null</c> si no había mano.
    /// </summary>
    TelemetryAggregate? EndHand();

    /// <summary>
    /// Snapshot inmutable del estado actual: última mano en curso + acumulado de sesión.
    /// Consumido por la UI de métricas.
    /// </summary>
    MetricsSnapshot SnapshotSession();

    /// <summary>
    /// Reinicia el acumulado global de sesión y el bucket de última mano.
    /// No afecta a datos ya persistidos.
    /// </summary>
    void ResetSession();
}
