using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el analizador de contexto preflop.
/// </summary>
public interface IPreflopAnalyzer
{
    bool IsPreflopAggressor(HandSituation situation);

    bool HasRangeAdvantageOnBoard(
        List<int> flopRanks, BoardTextureResult boardTexture,
        bool isPreflopAggressor, HandSituation situation);

    double CalculateCbetAdjustment(
        bool isPreflopAggressor, bool hasRangeAdvantage,
        BoardTextureResult boardTexture, bool isInPosition,
        int numOpponents, StrategyProfile profile);

    (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(
        decimal maxBet, bool villainWasPreflopAggressor,
        HandSituation currentSituation);

    BetSizeCategory CategorizeOpponentBet(decimal maxBet, decimal potSize);
}
