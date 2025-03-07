using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Aplication;

public interface IOutsCalculatorUseCase
{
    HandStrength CalculateAllOuts(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards);
}
