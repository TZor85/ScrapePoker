using OpenScrape.Domain.Entities;

namespace OpenScrape.Domain.ValueObjects;

public class HandStrength
{
    public DrawProbability FlushDraw { get; set; } = new();
    public DrawProbability StraightDraw { get; set; } = new();
    public DrawProbability GutshotDraw { get; set; } = new();
    public DrawProbability SetDraw { get; set; } = new();
    public DrawProbability FullHouseDraw { get; set; } = new();
    public DrawProbability OvercardDraw { get; set; } = new();
    public DrawProbability TwoPairDraw { get; set; } = new();
    public DrawProbability DoubleGutshotDraw { get; set; } = new();
    public DrawProbability StraightFlushDraw { get; set; } = new();
    public DrawProbability FourOfAKindDraw { get; set; } = new();
    public DrawProbability ThreeOfAKindDraw { get; set; } = new();
    public DrawProbability OnePairDraw { get; set; } = new();
    public DrawProbability HighCardDraw { get; set; } = new();
}
