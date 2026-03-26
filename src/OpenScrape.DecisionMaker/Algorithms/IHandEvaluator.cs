using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

/// <summary>
/// Interfaz para evaluación de manos de póker.
/// Permite implementaciones alternativas (ej: lookup table vs combinatoria).
/// </summary>
public interface IHandEvaluator
{
    HandEvaluation EvaluateBestHand(List<CardDataOuts> cards);

    /// <summary>
    /// Evaluación ligera para Monte Carlo: retorna struct sin allocations en heap.
    /// Solo Score compuesto (rank + kickers codificados) y HandRank.
    /// </summary>
    HandScore EvaluateHandScore(List<CardDataOuts> cards);
}

/// <summary>
/// Struct ligero para comparación de manos en Monte Carlo.
/// CompositeScore codifica HandRank + kickers en un solo long para comparación O(1).
/// Zero heap allocations.
/// </summary>
public readonly struct HandScore : IComparable<HandScore>
{
    /// <summary>
    /// Score compuesto: bits [60-56]=HandRank, [48-36]=Kicker1, [35-24]=Kicker2,
    /// [23-12]=Kicker3, [11-0]=Kicker4+5.
    /// Permite comparación con un solo long.CompareTo().
    /// </summary>
    public long CompositeScore { get; }
    public HandRank Rank { get; }

    public HandScore(HandRank rank, long compositeScore)
    {
        Rank = rank;
        CompositeScore = compositeScore;
    }

    public int CompareTo(HandScore other) => CompositeScore.CompareTo(other.CompositeScore);

    /// <summary>
    /// Construye el score compuesto: rank en bits altos, luego hasta 5 kickers de 4 bits cada uno.
    /// </summary>
    public static long BuildComposite(int rankValue, int k1 = 0, int k2 = 0, int k3 = 0, int k4 = 0, int k5 = 0)
    {
        // rank (4 bits) | k1 (4 bits) | k2 (4 bits) | k3 (4 bits) | k4 (4 bits) | k5 (4 bits)
        // Total: 24 bits, cabe sobrado en long
        return ((long)rankValue << 20)
             | ((long)k1 << 16)
             | ((long)k2 << 12)
             | ((long)k3 << 8)
             | ((long)k4 << 4)
             | (long)k5;
    }
}
