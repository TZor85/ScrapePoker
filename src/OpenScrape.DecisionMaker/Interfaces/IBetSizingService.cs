using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de cálculo de tamaños de apuesta.
/// </summary>
public interface IBetSizingService
{
    List<BetSizingOption> GetValueBetSizes(
        double equity, double spr, BoardTextureCategory texture,
        bool isInPosition, bool isMultiway);

    List<BetSizingOption> GetBluffSizes(
        double foldEquity, double spr, BoardTextureCategory texture,
        bool isInPosition);

    double GetThinValueThreshold(BoardTextureCategory texture, double spr, bool isInPosition);

    string CalculateDynamicBetSize(
        double baseSize, decimal heroStack, decimal potSize,
        int numOpponents, bool isPaired, bool isCoordinated,
        bool isDry, bool isInPosition,
        BoardPosition street = BoardPosition.None);
}
