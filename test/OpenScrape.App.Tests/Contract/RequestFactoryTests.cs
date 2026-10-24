using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Contract;

/// <summary>
/// Traducción del contrato crudo al modelo interno del motor.
/// </summary>
[TestFixture]
public class RequestFactoryTests
{
    [Test]
    public void Create_PreflopAksEnButton_ArmaElEstadoConLasCartasYLaPosicion()
    {
        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            PotSize = 3m,
            BetToCall = 3m,
            HeroStack = 100m,
            VillainStack = 100m,
            HeroPosition = TablePosition.Button,
            VillainPosition = TablePosition.BigBlind,
            IsInPosition = true,
            HandSituation = HandSituation.None
        };

        var internalRequest = new PreflopRequestFactory().Create(request, request.PotSize);

        Assert.That(internalRequest.PlayerState.HoleCard1Rank, Is.EqualTo((int)Rank.Ace));
        Assert.That(internalRequest.PlayerState.HoleCard1Suit, Is.EqualTo((int)Suit.Spades));
        Assert.That(internalRequest.PlayerState.Position, Is.EqualTo(TablePosition.Button));
        Assert.That(internalRequest.PlayerState.Players, Has.Count.EqualTo(1));
        Assert.That(internalRequest.PlayerState.Players[0].Bet, Is.EqualTo(3m));
        Assert.That(internalRequest.ResponseAction.IsSecondAction, Is.False);
    }
}
