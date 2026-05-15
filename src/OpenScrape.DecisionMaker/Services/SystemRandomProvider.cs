using OpenScrape.DecisionMaker.Interfaces;

namespace OpenScrape.DecisionMaker.Services;

public sealed class SystemRandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();

    public int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
}
