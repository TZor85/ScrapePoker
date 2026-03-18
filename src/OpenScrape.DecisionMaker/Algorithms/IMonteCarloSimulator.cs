using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

/// <summary>
/// Interfaz para simulación de equity por Monte Carlo.
/// </summary>
public interface IMonteCarloSimulator
{
    MonteCarloSimulator.EquityResult CalculateEquity(
        List<CardDataOuts> myCards,
        List<CardDataOuts> communityCards,
        int numOpponents,
        int? iterations = null,
        VillainRange? villainRange = null);
}
