using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Aplication;

public interface IPotOddsCalculator
{
    PotOddsResult Calculate(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards, decimal currentPotSize,
                            decimal betToCall,
                            List<CardDataOuts>? blockedCards = null);
}
