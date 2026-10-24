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
            Players = BuildPlayers(request)
        };

        return new SetPreflopActionUseCaseRequest
        {
            PlayerState = state,
            ResponseAction = new ResponseAction
            {
                HandSituation = request.HandSituation,
                IsSecondAction = request.IsSecondAction
            },
            PreflopHeroPosition = BuildHeroBets(request)
        };
    }

    private static List<Player> BuildPlayers(DecisionRequest request)
    {
        if (request.PreflopBets.Count == 0)
        {
            return
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
            ];
        }

        return request.PreflopBets
            .Select((seat, index) => new Player
            {
                Name = seat.Key == request.HeroPosition ? "P0" : $"P{index + 1}",
                Active = true,
                Bet = seat.Value,
                Stack = seat.Key == request.HeroPosition ? request.HeroStack : request.VillainStack,
                Position = seat.Key,
                WasPreflopAggressor = seat.Key != request.HeroPosition && request.HeroWasPreflopAggressor is false
            })
            .ToList();
    }

    private static Dictionary<TablePosition, Dictionary<TablePosition, decimal>> BuildHeroBets(DecisionRequest request)
    {
        if (request.PreflopBets.Count == 0)
            return new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();

        var bets = request.PreflopBets
            .Where(seat => seat.Key != TablePosition.None)
            .ToDictionary(seat => seat.Key, seat => seat.Value);

        var map = new Dictionary<TablePosition, Dictionary<TablePosition, decimal>>();
        foreach (var position in Enum.GetValues<TablePosition>())
        {
            if (position != TablePosition.None)
                map[position] = new Dictionary<TablePosition, decimal>(bets);
        }

        return map;
    }
}
