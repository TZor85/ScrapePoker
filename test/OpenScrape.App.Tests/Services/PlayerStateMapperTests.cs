using System.Text.Json;
using OpenScrape.App.Services;
using OpenScrape.App.Tests.Api;
using OpenScrape.Contracts;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El mapper traduce el estado de mesa al contrato crudo: hechos, nunca
/// resultados calculados. Si viajara equity u outs, el motor seguiría en el cliente.
/// </summary>
[TestFixture]
public class PlayerStateMapperTests
{
    [Test]
    public void MapPostflop_EstadoConBoardDeTres_ArmaElRequestCrudo()
    {
        var request = PlayerStateMapper.MapPostflop(
            State(),
            DecisionStreet.Flop,
            maxBet: 5m,
            new DecisionContext { IsDonkBet = true },
            villain: null,
            heroIsAggressor: true);

        Assert.That(request.Street, Is.EqualTo(DecisionStreet.Flop));
        Assert.That(request.Board, Has.Count.EqualTo(3));
        Assert.That(request.PotSize, Is.EqualTo(10m));
        Assert.That(request.BetToCall, Is.EqualTo(5m));
        Assert.That(request.IsInPosition, Is.True);
        Assert.That(request.HeroWasPreflopAggressor, Is.True);
        Assert.That(request.HandSituation, Is.EqualTo(HandSituation.OpenRaise));
        Assert.That(request.Context.IsDonkBet, Is.True);
        Assert.That(request.VillainStack, Is.EqualTo(95m));
        Assert.That(request.NumOpponents, Is.EqualTo(1));
    }

    [Test]
    public void MapPostflop_NoIncluyeEquityNiOuts()
    {
        var request = PlayerStateMapper.MapPostflop(
            State(),
            DecisionStreet.Flop,
            maxBet: 5m,
            new DecisionContext(),
            villain: null,
            heroIsAggressor: true);

        var json = JsonSerializer.Serialize(request, JsonConfig.Options);

        Assert.That(json, Does.Not.Contain("equity"));
        Assert.That(json, Does.Not.Contain("totalOuts"));
        Assert.That(json, Does.Not.Contain("potOdds"));
    }

    private static PlayerGameState State() => new()
    {
        HoleCard1Rank = (int)Rank.Ace,
        HoleCard1Suit = (int)Suit.Spades,
        HoleCard2Rank = (int)Rank.King,
        HoleCard2Suit = (int)Suit.Spades,
        HoleCard1Face = "A",
        HoleCard2Face = "K",
        Position = TablePosition.Button,
        PotSize = 10m,
        HeroStack = 90m,
        IsInPosition = true,
        HandSituation = HandSituation.OpenRaise,
        Players =
        [
            new Player { Name = "P0", Active = true, Bet = 0m, Stack = 90m, Position = TablePosition.Button },
            new Player { Name = "V1", Active = true, Bet = 5m, Stack = 95m, Position = TablePosition.BigBlind }
        ],
        BoardCards =
        [
            new BoardData { Force = (int)Rank.King, Suit = (int)Suit.Hearts, Position = BoardPosition.Flop, Location = 1 },
            new BoardData { Force = (int)Rank.Seven, Suit = (int)Suit.Clubs, Position = BoardPosition.Flop, Location = 2 },
            new BoardData { Force = (int)Rank.Two, Suit = (int)Suit.Diamonds, Position = BoardPosition.Flop, Location = 3 }
        ]
    };
}
