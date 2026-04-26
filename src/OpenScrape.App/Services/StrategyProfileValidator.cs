using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Exceptions;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Services;

/// <summary>
/// Valida el <see cref="StrategyProfile"/> cargado al arrancar. Si detecta errores,
/// lanza <see cref="StrategyProfileValidationException"/> acumulando todos los
/// problemas en un único mensaje. El objetivo es fallar rápido antes de mostrar
/// <c>FrmMain</c>, en lugar de degradarse silenciosamente con fallbacks.
/// </summary>
public static class StrategyProfileValidator
{
    /// <summary>
    /// Combinaciones (Street, Situation) que el perfil DEBE cubrir.
    /// Derivado del contenido actual de <c>appsettings.json</c>: 36 entradas
    /// {Flop, Turn, River} × {OpenRaise, Call, RaiseOverLimper, ThreeBet,
    /// OpenRaiseVs3Bet, OpenRaiseVs3BetAndCall, FourBet, Cold4Bet, Squeeze,
    /// VsSqueeze, DonkBet, DonkBetVsOpenRaise}.
    /// </summary>
    private static readonly HandSituation[] RequiredSituations =
    [
        HandSituation.OpenRaise,
        HandSituation.Call,
        HandSituation.RaiseOverLimper,
        HandSituation.ThreeBet,
        HandSituation.OpenRaiseVs3Bet,
        HandSituation.OpenRaiseVs3BetAndCall,
        HandSituation.FourBet,
        HandSituation.Cold4Bet,
        HandSituation.Squeeze,
        HandSituation.VsSqueeze,
        HandSituation.DonkBet,
        HandSituation.DonkBetVsOpenRaise
    ];

    private static readonly BoardPosition[] RequiredStreets =
    [
        BoardPosition.Flop,
        BoardPosition.Turn,
        BoardPosition.River
    ];

    public static void Validate(StrategyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new List<string>();
        var parsedKeys = new HashSet<ThresholdKey>();

        foreach (var (rawKey, thresholds) in profile.Thresholds)
        {
            if (!ThresholdKey.TryParse(rawKey, out var key, out var parseError))
            {
                errors.Add($"Clave '{rawKey}' no es válida: {parseError}");
                continue;
            }

            parsedKeys.Add(key!);
            ValidateTiers(rawKey, thresholds, errors);
        }

        foreach (var street in RequiredStreets)
        {
            foreach (var situation in RequiredSituations)
            {
                var required = new ThresholdKey(street, situation);
                if (!parsedKeys.Contains(required))
                    errors.Add($"Falta la entrada obligatoria '{required}' en StrategyProfile.Thresholds.");
            }
        }

        if (errors.Count > 0)
            throw new StrategyProfileValidationException(errors);
    }

    private static void ValidateTiers(string rawKey, StreetThresholds t, List<string> errors)
    {
        if (t.FoldBelow < 0 || t.FoldBelow > 100)
            errors.Add($"'{rawKey}': FoldBelow ({t.FoldBelow}) fuera de rango [0, 100].");
        if (t.ThinValueAbove < 0 || t.ThinValueAbove > 100)
            errors.Add($"'{rawKey}': ThinValueAbove ({t.ThinValueAbove}) fuera de rango [0, 100].");
        if (t.ValueAbove < 0 || t.ValueAbove > 100)
            errors.Add($"'{rawKey}': ValueAbove ({t.ValueAbove}) fuera de rango [0, 100].");
        if (t.StrongValueAbove < 0 || t.StrongValueAbove > 100)
            errors.Add($"'{rawKey}': StrongValueAbove ({t.StrongValueAbove}) fuera de rango [0, 100].");

        if (t.FoldBelow > t.ThinValueAbove)
            errors.Add($"'{rawKey}': FoldBelow ({t.FoldBelow}) > ThinValueAbove ({t.ThinValueAbove}).");
        if (t.ThinValueAbove > t.ValueAbove)
            errors.Add($"'{rawKey}': ThinValueAbove ({t.ThinValueAbove}) > ValueAbove ({t.ValueAbove}).");
        if (t.ValueAbove > t.StrongValueAbove)
            errors.Add($"'{rawKey}': ValueAbove ({t.ValueAbove}) > StrongValueAbove ({t.StrongValueAbove}).");
    }
}
