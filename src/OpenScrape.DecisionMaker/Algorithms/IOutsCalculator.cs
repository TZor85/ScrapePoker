using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

/// <summary>
/// Interfaz para cálculo de outs y detección de draws.
/// </summary>
public interface IOutsCalculator
{
    OutsCalculator.OutsResult CalculateOuts(List<CardDataOuts> myCards, List<CardDataOuts> communityCards);
}
