using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// Helper de tests: rellena en un <see cref="StrategyProfile"/> las claves obligatorias
/// de <c>Thresholds</c> que no estén presentes, usando el mismo value object que antes
/// devolvía el fallback silencioso del servicio. Sin esto, los tests que construyen
/// perfiles parciales rompen tras eliminar el fallback.
/// </summary>
internal static class TestProfileDefaults
{
    private static readonly BoardPosition[] Streets =
    [
        BoardPosition.Flop,
        BoardPosition.Turn,
        BoardPosition.River
    ];

    private static readonly HandSituation[] Situations =
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
        HandSituation.DonkBetVsOpenRaise,
        HandSituation.LimpRaise
    ];

    private static StreetThresholds Fallback() => new()
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

    public static StrategyProfile FillMissingThresholds(this StrategyProfile profile)
    {
        foreach (var street in Streets)
        {
            foreach (var situation in Situations)
            {
                var key = $"{street}_{situation}";
                if (!profile.Thresholds.ContainsKey(key))
                    profile.Thresholds[key] = Fallback();
            }
        }
        return profile;
    }
}
