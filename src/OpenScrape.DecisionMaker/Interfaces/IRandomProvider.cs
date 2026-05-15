namespace OpenScrape.DecisionMaker.Interfaces;

public interface IRandomProvider
{
    double NextDouble();

    int Next(int minValue, int maxValue);
}
