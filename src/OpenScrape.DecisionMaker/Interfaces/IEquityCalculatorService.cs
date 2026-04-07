using OpenScrape.Domain.ValueObjects;

using static OpenScrape.DecisionMaker.Services.EquityCalculatorService;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de cálculo de equity completo.
/// </summary>
public interface IEquityCalculatorService
{
    FullEquityAnalysis CalculateFullEquity(
        List<CardDataOuts> myCards, List<CardDataOuts> communityCards,
        int numOpponents, double potSize, double callAmount,
        int? monteCarloIterations = null);

    string GetEquityAnalysisSummary(FullEquityAnalysis analysis);
}
