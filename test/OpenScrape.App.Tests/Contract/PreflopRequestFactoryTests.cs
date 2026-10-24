using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Contract;

/// <summary>
/// El preflop no se decide sólo con las cartas: raiser, caller y squeezer salen
/// de las apuestas de los asientos. Sin ellos, multiway no se puede reproducir.
/// </summary>
[TestFixture]
public class PreflopRequestFactoryTests
{
    [Test]
    public void Create_ConApuestas_ArmaElMapaParaTodasLasPosiciones()
    {
        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            PotSize = 7m,
            BetToCall = 3m,
            HeroStack = 100m,
            VillainStack = 100m,
            HeroPosition = TablePosition.Button,
            IsInPosition = true,
            HandSituation = HandSituation.RaiseOverLimper,
            PreflopBets = new Dictionary<TablePosition, decimal>
            {
                [TablePosition.Button] = 0m,
                [TablePosition.CutOff] = 3m,
                [TablePosition.Middle] = 1m
            }
        };

        var internalRequest = new PreflopRequestFactory().Create(request, request.PotSize);

        Assert.That(internalRequest.PlayerState.Players, Has.Count.EqualTo(3));
        Assert.That(internalRequest.PreflopHeroPosition, Contains.Key(TablePosition.Button));
        Assert.That(internalRequest.PreflopHeroPosition, Contains.Key(TablePosition.BigBlind));

        var fromButton = internalRequest.PreflopHeroPosition[TablePosition.Button];
        Assert.That(fromButton[TablePosition.CutOff], Is.EqualTo(3m));
        Assert.That(fromButton[TablePosition.Middle], Is.EqualTo(1m));

        var fromBigBlind = internalRequest.PreflopHeroPosition[TablePosition.BigBlind];
        Assert.That(fromBigBlind[TablePosition.CutOff], Is.EqualTo(3m));
    }

    [Test]
    public void Create_SinApuestas_MantieneElMapaVacio()
    {
        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            PotSize = 3m,
            BetToCall = 3m,
            HeroStack = 100m,
            HeroPosition = TablePosition.Button,
            HandSituation = HandSituation.OpenRaise
        };

        var internalRequest = new PreflopRequestFactory().Create(request, request.PotSize);

        Assert.That(internalRequest.PreflopHeroPosition, Is.Empty);
        Assert.That(internalRequest.PlayerState.Players, Has.Count.EqualTo(1));
    }
}
