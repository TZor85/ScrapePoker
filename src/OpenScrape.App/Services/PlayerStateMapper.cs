using OpenScrape.Contracts;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Services;

/// <summary>
/// Traduce el estado de mesa al contrato crudo. Manda hechos (cartas, pozo,
/// stacks, contexto); el servidor deriva equity, outs, textura y pot odds.
/// </summary>
public static class PlayerStateMapper
{
    public static DecisionRequest MapPreflop(
        PlayerGameState state,
        bool isSecondAction,
        VillainProfile? villain,
        IReadOnlyDictionary<TablePosition, decimal>? preflopBets = null) =>
        Build(
            state,
            DecisionStreet.Preflop,
            maxBet: state.CurrentBet,
            isSecondAction,
            new DecisionContext(),
            villain,
            heroIsAggressor: state.Players.Any(player => player.Name == HeroName && player.WasPreflopAggressor),
            preflopBets: preflopBets);

    public static DecisionRequest MapPostflop(
        PlayerGameState state,
        DecisionStreet street,
        decimal maxBet,
        DecisionContext context,
        VillainProfile? villain,
        bool heroIsAggressor,
        HandSituation? effectiveSituation = null) =>
        Build(state, street, maxBet, isSecondAction: false, context, villain, heroIsAggressor, effectiveSituation);

    private const string HeroName = "P0";

    private static DecisionRequest Build(
        PlayerGameState state,
        DecisionStreet street,
        decimal maxBet,
        bool isSecondAction,
        DecisionContext context,
        VillainProfile? villain,
        bool heroIsAggressor,
        HandSituation? effectiveSituation = null,
        IReadOnlyDictionary<TablePosition, decimal>? preflopBets = null) => new()
        {
            Street = street,
            HeroCard1 = new CardRef(state.HoleCard1Rank, state.HoleCard1Suit),
            HeroCard2 = new CardRef(state.HoleCard2Rank, state.HoleCard2Suit),
            Board = state.BoardCards
            .Where(card => card.Position != BoardPosition.Hand)
            .OrderBy(card => card.Location)
            .Take(BoardCardsFor(street))
            .Select(card => new CardRef(card.Force, card.Suit))
            .ToList(),
            PotSize = state.PotSize,
            BetToCall = maxBet,
            HeroStack = state.HeroStack,
            VillainStack = VillainStack(state),
            IsInPosition = state.IsInPosition,
            NumOpponents = Math.Max(0, state.Players.Count(player => player.Active) - 1),
            HeroPosition = state.Position,
            VillainPosition = Villain(state)?.Position ?? TablePosition.None,
            HandSituation = effectiveSituation ?? state.HandSituation,
            IsSecondAction = isSecondAction,
            HeroWasPreflopAggressor = heroIsAggressor,
            IsHeroAggressor = heroIsAggressor,
            VillainProfile = villain,
            Context = context,
            PreflopBets = preflopBets ?? new Dictionary<TablePosition, decimal>()
        };

    private static int BoardCardsFor(DecisionStreet street) => street switch
    {
        DecisionStreet.Flop => 3,
        DecisionStreet.Turn => 4,
        DecisionStreet.River => 5,
        _ => 0
    };

    private static Player? Villain(PlayerGameState state) => state.Players
        .Where(player => player.Active && !string.IsNullOrEmpty(player.Name) && player.Name != HeroName)
        .OrderByDescending(player => player.Bet)
        .FirstOrDefault();

    private static decimal VillainStack(PlayerGameState state)
    {
        var candidates = state.Players
            .Where(player => player.Active && player.Name != null && player.Name != HeroName)
            .ToList();

        if (candidates.Count == 0)
            return state.HeroStack;

        var stack = candidates.Max(player => player.Stack);
        return stack <= 0 && state.HeroStack > 0 ? state.HeroStack : stack;
    }
}
