using Microsoft.Extensions.Options;

using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Servicio que proporciona acceso al perfil de estrategia configurado.
/// Carga los thresholds desde appsettings.json via IOptions.
/// </summary>
public class StrategyProfileService
{
    private readonly StrategyProfile _profile;

    public StrategyProfileService(IOptions<StrategyProfile> options)
    {
        _profile = options.Value;
    }

    public StrategyProfile Profile => _profile;

    /// <summary>
    /// Obtiene los thresholds para una combinación de street y situación.
    /// </summary>
    public StreetThresholds GetThresholds(BoardPosition street, HandSituation situation)
    {
        var key = $"{street}_{situation}";
        if (_profile.Thresholds.TryGetValue(key, out var thresholds))
            return thresholds;

        // Fallback conservador si no hay config
        return new StreetThresholds
        {
            FoldBelow = 40,
            ThinValueAbove = 45,
            ValueAbove = 55,
            StrongValueAbove = 75,
            CanBluff = false,
            LowEquityAction = "Fold",
            ThinValueIPOnly = true,
            ThinValueOOPFallback = "CheckFold"
        };
    }

    /// <summary>
    /// Obtiene la frecuencia de bluff para un street dado.
    /// </summary>
    public double GetBluffFrequency(BoardPosition street) => street switch
    {
        BoardPosition.Flop => _profile.FlopBluffFrequency,
        BoardPosition.Turn => _profile.TurnBluffFrequency,
        BoardPosition.River => _profile.RiverBluffFrequency,
        _ => 0.0
    };
}
