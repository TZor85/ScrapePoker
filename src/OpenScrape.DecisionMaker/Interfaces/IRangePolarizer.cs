using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de polarización de rangos.
/// </summary>
public interface IRangePolarizer
{
    RangeType GetOptimalRangeType(
        BoardTextureCategory texture, bool isInPosition,
        double spr, BoardPosition street);

    RangeType GetOptimalRangeTypeByWetness(
        double boardWetnessScore, bool isInPosition,
        double spr, BoardPosition street);

    (double foldBelowAdjust, double thinValueAdjust) GetThresholdAdjustment(
        RangeType rangeType, bool isInPosition);

    (double foldBelowAdjust, double thinValueAdjust) GetThresholdAdjustmentBySituation(
        BoardTextureCategory texture, bool isInPosition,
        double spr, BoardPosition street);
}
