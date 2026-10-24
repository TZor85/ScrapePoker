using OpenScrape.Contracts;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop;

/// <summary>
/// Traduce el contrato crudo de preflop al modelo que consume
/// <see cref="SetPreflopActionUseCase"/>. No decide nada: sólo arma entrada.
/// </summary>
public sealed class PreflopRequestFactory
{
    public SetPreflopActionUseCaseRequest Create(DecisionRequest request, decimal potSize)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Street != DecisionStreet.Preflop)
            throw new ArgumentException("La fábrica preflop sólo acepta pedidos de preflop.", nameof(request));

        var state = new PlayerGameState
        {
            HoleCard1Rank = request.HeroCard1.Rank,
            HoleCard1Suit = request.HeroCard1.Suit,
            HoleCard2Rank = request.HeroCard2.Rank,
            HoleCard2Suit = request.HeroCard2.Suit,
            HoleCard1Face = ((Rank)request.HeroCard1.Rank).ToString(),
            HoleCard2Face = ((Rank)request.HeroCard2.Rank).ToString(),
            Position = request.HeroPosition,
            PotSize = potSize,
            CurrentBet = request.BetToCall,
            HeroStack = request.HeroStack,
            IsInPosition = request.IsInPosition,
            HandSituation = request.HandSituation,
            Players =
            [
                new Player
                {
                    Name = "Villain",
                    Active = true,
                    Bet = request.BetToCall,
                    Stack = request.VillainStack,
                    Position = request.VillainPosition,
                    WasPreflopAggressor = request.HeroWasPreflopAggressor is false
                }
            ]
        };

        return new SetPreflopActionUseCaseRequest
        {
            PlayerState = state,
            ResponseAction = new ResponseAction
            {
                HandSituation = request.HandSituation,
                IsSecondAction = request.IsSecondAction
            },
            PreflopHeroPosition = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>()
        };
    }
}
