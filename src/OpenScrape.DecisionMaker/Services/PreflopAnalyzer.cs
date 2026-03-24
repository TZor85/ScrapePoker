using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Analiza contexto preflop para decisiones postflop:
/// detección de agresor, ventaja de rango, ajuste de c-bet, donk bet.
/// Extraído de FrmMain para separar lógica de dominio de UI.
/// </summary>
public class PreflopAnalyzer
{
    /// <summary>
    /// Determina si hero fue el agresor preflop basado en la HandSituation.
    /// </summary>
    public static bool IsPreflopAggressor(HandSituation situation) => situation switch
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
    /// Evalúa si hero tiene ventaja de rango en este board.
    /// Boards altos (A, K, Q) favorecen al raiser; boards bajos conectados favorecen al caller.
    /// </summary>
    public static bool HasRangeAdvantageOnBoard(
        List<int> flopRanks, BoardTextureResult boardTexture,
        bool isPreflopAggressor, HandSituation situation)
    {
        int highCards = flopRanks.Count(r => r >= 12);
        bool hasAceOrKing = flopRanks.Any(r => r >= 13);
        bool isLowBoard = flopRanks.All(r => r <= 9);
        bool isMediumBoard = flopRanks.All(r => r >= 7 && r <= 11);

        bool is3BetPot = situation is HandSituation.ThreeBet or HandSituation.FourBet
            or HandSituation.Cold4Bet or HandSituation.Squeeze
            or HandSituation.OpenRaiseVs3Bet or HandSituation.VsSqueeze;

        if (isPreflopAggressor)
        {
            if (is3BetPot)
            {
                // 3Bet range tiene ventaja en boards con A/K (overpairs + TPTK)
                if (hasAceOrKing)
                    return true;
                // Boards bajos → caller conecta con pocket pairs, suited connectors
                if (isLowBoard)
                    return false;
                // Board mixto: sin cartas altas (Q+) + carta baja (≤6) → caller conecta más
                bool isMixedLowBoard = highCards <= 0 && flopRanks.Min() <= 6;
                if (isMixedLowBoard)
                    return false;
                // 2+ cartas altas (Q, K sin A) → rango 3Bet conecta
                return highCards >= 2;
            }
            return hasAceOrKing || highCards >= 2;
        }
        else
        {
            if (is3BetPot)
                return isMediumBoard && boardTexture.IsConnected;
            return isLowBoard && boardTexture.IsConnected;
        }
    }

    /// <summary>
    /// Calcula el ajuste de equity para c-bet basado en rol preflop, range advantage y board.
    /// Positivo = más agresivo (agresor con ventaja), Negativo = más conservador (caller sin ventaja).
    /// </summary>
    public static double CalculateCbetAdjustment(
        bool isPreflopAggressor, bool hasRangeAdvantage,
        BoardTextureResult boardTexture,
        bool isInPosition, int numOpponents,
        StrategyProfile profile)
    {
        double adjustment = 0;

        if (isPreflopAggressor)
        {
            adjustment += profile.CbetAggressorBonus;
            if (hasRangeAdvantage)
                adjustment += profile.CbetRangeAdvantageBonus;
        }
        else
        {
            if (!hasRangeAdvantage)
                adjustment += profile.CbetCallerDisadvantage;
        }

        if (boardTexture.IsMonotone)
            adjustment *= profile.CbetMonotoneReduction;

        if (numOpponents > 1)
            adjustment -= profile.CbetMultiwayReduction * (numOpponents - 1);

        return adjustment;
    }

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

    /// <summary>
    /// Categoriza el tamaño de la apuesta del villano relativa al pot.
    /// </summary>
    public static BetSizeCategory CategorizeOpponentBet(decimal maxBet, decimal potSize)
    {
        if (maxBet == 0)
            return BetSizeCategory.NoBet;
        if (maxBet <= potSize * 0.3m)
            return BetSizeCategory.Small;
        if (maxBet <= potSize * 0.7m)
            return BetSizeCategory.Medium;
        return BetSizeCategory.Large;
    }
}
