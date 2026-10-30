using OpenScrape.Api.Endpoints;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Api;

/// <summary>
/// El validador rechaza pedidos postflop sin situación preflop: el motor no puede
/// resolver thresholds sin saber si el héroe abrió, pagó o hizo 3-bet.
/// </summary>
[TestFixture]
public class RequestValidatorTests
{
    [Test]
    public void Validate_PostflopSinSituacion_DevuelveError()
    {
        var request = CreateFlopRequest(HandSituation.None);

        var error = RequestValidator.Validate(request);

        Assert.That(error, Is.Not.Null.And.Contains("situación"));
    }

    [Test]
    public void Validate_PostflopConSituacion_EsValido()
    {
        var request = CreateFlopRequest(HandSituation.OpenRaise);

        Assert.That(RequestValidator.Validate(request), Is.Null);
    }

    [Test]
    public void Validate_PreflopSinSituacion_EsValido()
    {
        // En preflop la situación todavía no existe: la deriva el motor.
        var request = CreateFlopRequest(HandSituation.None) with
        {
            Street = DecisionStreet.Preflop,
            Board = []
        };

        Assert.That(RequestValidator.Validate(request), Is.Null);
    }

    private static DecisionRequest CreateFlopRequest(HandSituation situation) => new()
    {
        Street = DecisionStreet.Flop,
        HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
        HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
        Board =
        [
            new CardRef((int)Rank.Two, (int)Suit.Hearts),
            new CardRef((int)Rank.Seven, (int)Suit.Clubs),
            new CardRef((int)Rank.Nine, (int)Suit.Diamonds)
        ],
        PotSize = 10m,
        BetToCall = 0m,
        HeroStack = 100m,
        VillainStack = 100m,
        HandSituation = situation
    };
}
