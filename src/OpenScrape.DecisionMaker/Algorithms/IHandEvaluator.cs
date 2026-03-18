using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

/// <summary>
/// Interfaz para evaluación de manos de póker.
/// Permite implementaciones alternativas (ej: lookup table vs combinatoria).
/// </summary>
public interface IHandEvaluator
{
    HandEvaluation EvaluateBestHand(List<CardDataOuts> cards);
}
