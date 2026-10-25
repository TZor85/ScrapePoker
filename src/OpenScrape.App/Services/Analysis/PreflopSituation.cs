using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services.Analysis;

/// <summary>
/// Lógica local de situación preflop que el cliente necesita para el contexto postflop.
/// Reemplaza al analizador del motor: el servidor recibe la situación ya resuelta en el request.
/// </summary>
public static class PreflopSituation
{
    /// <summary>
    /// Determina si hero fue el agresor preflop basado en la HandSituation.
    /// </summary>
    public static bool IsAggressor(HandSituation situation) => situation switch
    {
        HandSituation.OpenRaise => true,
        HandSituation.RaiseOverLimper => true,
        HandSituation.ThreeBet => true,
        HandSituation.FourBet => true,
        HandSituation.Cold4Bet => true,
        HandSituation.Squeeze => true,
        _ => false
    };

    /// <summary>
    /// Detecta si la apuesta del villano es un donk bet (villano no fue agresor preflop).
    /// </summary>
    public static (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(
        decimal maxBet, bool villainWasPreflopAggressor, HandSituation currentSituation)
    {
        if (maxBet == 0)
            return (false, currentSituation);

        if (!villainWasPreflopAggressor)
        {
            var donkSituation = currentSituation switch
            {
                HandSituation.OpenRaise => HandSituation.DonkBetVsOpenRaise,
                _ => HandSituation.DonkBet
            };
            return (true, donkSituation);
        }

        return (false, currentSituation);
    }
}
