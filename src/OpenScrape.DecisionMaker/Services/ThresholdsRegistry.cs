using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Implementación de <see cref="IThresholdsRegistry"/>. Parsea las claves string del
/// <see cref="StrategyProfile.Thresholds"/> a <see cref="ThresholdKey"/> una única vez
/// al construirse (singleton). Si una clave string no parsea, lanza en el constructor.
/// </summary>
public sealed class ThresholdsRegistry : IThresholdsRegistry
{
    private readonly Dictionary<ThresholdKey, StreetThresholds> _map;

    public ThresholdsRegistry(IOptions<StrategyProfile> profileOptions)
    {
        var profile = profileOptions.Value;
        _map = new Dictionary<ThresholdKey, StreetThresholds>();

        foreach (var (rawKey, thresholds) in profile.Thresholds)
        {
            if (!ThresholdKey.TryParse(rawKey, out var key, out var parseError))
            {
                throw new InvalidOperationException(
                    $"Clave de StrategyProfile.Thresholds inválida: '{rawKey}'. {parseError}");
            }

            _map[key!] = thresholds;
        }
    }

    public StreetThresholds Get(ThresholdKey key)
    {
        if (_map.TryGetValue(key, out var thresholds))
            return thresholds;

        throw new KeyNotFoundException(
            $"No hay thresholds configurados para '{key}'. El validador de arranque debería haber detectado esto.");
    }

    public bool TryGet(ThresholdKey key, out StreetThresholds thresholds)
    {
        if (_map.TryGetValue(key, out var found))
        {
            thresholds = found;
            return true;
        }

        thresholds = default!;
        return false;
    }

    public bool Contains(ThresholdKey key) => _map.ContainsKey(key);

    public IReadOnlyCollection<ThresholdKey> Keys => _map.Keys;
}
