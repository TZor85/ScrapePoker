using OpenScrape.Domain.Interfaces;

namespace OpenScrape.Domain.Services;

public sealed class SystemRandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();

    public int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
}
