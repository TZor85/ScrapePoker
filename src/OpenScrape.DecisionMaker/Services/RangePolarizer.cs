using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Services;

public enum RangeType
{
    Linear,
    Polarized,
    Condensed
}

public class RangePolarizer
{
    private const double SPRCondensedThreshold = 3.0;
    private const double DryTextureThreshold = 25.0;
    private const double WetTextureThreshold = 50.0;

    public RangeType GetOptimalRangeType(
        BoardTextureCategory texture,
        bool isInPosition,
        double spr,
        BoardPosition street)
    {
        if (spr < SPRCondensedThreshold)
            return RangeType.Condensed;

        if (street == BoardPosition.River)
            return isInPosition ? RangeType.Polarized : RangeType.Linear;

        if (texture == BoardTextureCategory.Dry)
            return isInPosition ? RangeType.Polarized : RangeType.Linear;

        if (texture == BoardTextureCategory.Paired)
            return isInPosition ? RangeType.Polarized : RangeType.Linear;

        if (texture == BoardTextureCategory.Wet || texture == BoardTextureCategory.SemiWet)
            return RangeType.Linear;

        return isInPosition ? RangeType.Polarized : RangeType.Linear;
    }

    public RangeType GetOptimalRangeTypeByWetness(double boardWetnessScore, bool isInPosition, double spr, BoardPosition street)
    {
        if (spr < SPRCondensedThreshold)
            return RangeType.Condensed;

        if (street == BoardPosition.River)
            return isInPosition ? RangeType.Polarized : RangeType.Linear;

        if (boardWetnessScore < DryTextureThreshold)
            return isInPosition ? RangeType.Polarized : RangeType.Linear;

        if (boardWetnessScore > WetTextureThreshold)
            return RangeType.Linear;

        return isInPosition ? RangeType.Polarized : RangeType.Linear;
    }

    public (double foldBelowAdjust, double thinValueAdjust) GetThresholdAdjustment(
        RangeType rangeType,
        bool isInPosition)
    {
        return rangeType switch
        {
            RangeType.Polarized when isInPosition => (-4.0, -3.0),
            RangeType.Polarized => (-2.0, -2.0),
            RangeType.Linear when isInPosition => (0.0, 0.0),
            RangeType.Linear => (3.0, 2.0),
            RangeType.Condensed => (6.0, 4.0),
            _ => (0.0, 0.0)
        };
    }

    public (double foldBelowAdjust, double thinValueAdjust) GetThresholdAdjustmentBySituation(
        BoardTextureCategory texture,
        bool isInPosition,
        double spr,
        BoardPosition street)
    {
        var rangeType = GetOptimalRangeType(texture, isInPosition, spr, street);
        return GetThresholdAdjustment(rangeType, isInPosition);
    }
}
