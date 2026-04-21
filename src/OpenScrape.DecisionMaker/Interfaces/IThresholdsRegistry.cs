using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Lookup tipado de <see cref="StreetThresholds"/> por <see cref="ThresholdKey"/>.
/// Sustituye el acceso por clave string al <c>Dictionary&lt;string, StreetThresholds&gt;</c>
/// del <c>StrategyProfile</c>, eliminando el fallback silencioso.
/// </summary>
public interface IThresholdsRegistry
{
    /// <summary>
    /// Obtiene los thresholds para la clave indicada.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Si la clave no existe. El validador de arranque garantiza que esto no ocurre en runtime normal.</exception>
    StreetThresholds Get(ThresholdKey key);

    /// <summary>
    /// Obtiene los thresholds sin lanzar si la clave no existe.
    /// </summary>
    bool TryGet(ThresholdKey key, out StreetThresholds thresholds);

    /// <summary>
    /// Indica si el registro contiene una entrada para la clave.
    /// </summary>
    bool Contains(ThresholdKey key);

    /// <summary>
    /// Claves cargadas desde el perfil de estrategia.
    /// </summary>
    IReadOnlyCollection<ThresholdKey> Keys { get; }
}
