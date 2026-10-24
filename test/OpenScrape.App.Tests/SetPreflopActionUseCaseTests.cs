using OpenScrape.App.Aplication;
using OpenScrape.App.Entities;
using OpenScrape.App.Tests.Fixtures;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// El caso de uso preflop no debe mutar el request que recibe: el llamador puede
/// reutilizarlo y, sobre todo, un fixture guardado como "entrada" no puede salir
/// con la respuesta ya horneada.
/// </summary>
[TestFixture]
public class SetPreflopActionUseCaseTests
{
    [Test]
    public async Task Execute_NoMutaElRequest()
    {
        var state = new PlayerGameState
        {
            Position = TablePosition.Button,
            HoleCard1Face = "A",
            HoleCard1Rank = (int)Rank.Ace,
            HoleCard1Suit = (int)Suit.Spades,
            HoleCard2Face = "K",
            HoleCard2Rank = (int)Rank.King,
            HoleCard2Suit = (int)Suit.Spades,
            PotSize = 3m,
            HeroStack = 100m
        };
        var request = new SetPreflopActionUseCaseRequest { PlayerState = state };

        var table = new TableDTO
        {
            Name = "fixture-no-mutacion",
            Positions =
            [
                new PlayerActionSequence(
                    Name: "fixture-no-mutacion",
                    HeroPosition: "Button",
                    OpenRaiser: null,
                    ThreeBetPosition: null,
                    Limper: null,
                    Caller: null,
                    Squeezer: null,
                    BetSize: null,
                    IsGreater: null,
                    RaiserFolds: null,
                    Hands: [new Hand("AK", true, "Raise", 100)])
            ]
        };

        var sut = PreflopTestCaseBuilder.Create(new FixedRangeStore(table), new FixedRandomProvider(50));

        var response = await sut.Execute(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.ResponseAction.Action, Is.EqualTo("Raise"));
            Assert.That(request.ResponseAction.Action, Is.Null, "El request de entrada quedo mutado");
        });
    }
}
