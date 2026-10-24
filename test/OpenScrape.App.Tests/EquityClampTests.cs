using OpenScrape.App.Services;
using OpenScrape.App.Tests.Fixtures;

namespace OpenScrape.App.Tests;

/// <summary>
/// La equity efectiva que consume el motor debe quedar siempre dentro de [0, 100].
/// El ajuste de c-bet y la penalidad por peligro pueden empujarla fuera del rango,
/// y el motor rechaza el input en ese caso.
/// </summary>
[TestFixture]
public class EquityClampTests
{
    [Test]
    public void ClampEquity_PorEncimaDeCien_DevuelveCien()
    {
        // Arrange
        const double equity = 102.5;

        // Act
        var resultado = GameCoordinator.ClampEquity(equity);

        // Assert
        Assert.That(resultado, Is.EqualTo(100));
    }

    [Test]
    public void ClampEquity_PorDebajoDeCero_DevuelveCero()
    {
        // Arrange
        const double equity = -3.2;

        // Act
        var resultado = GameCoordinator.ClampEquity(equity);

        // Assert
        Assert.That(resultado, Is.EqualTo(0));
    }

    [Test]
    public void ClampEquity_DentroDelRango_NoLaModifica()
    {
        // Arrange
        const double equity = 52.3;

        // Act
        var resultado = GameCoordinator.ClampEquity(equity);

        // Assert
        Assert.That(resultado, Is.EqualTo(52.3));
    }

    [Test]
    public void DetermineFlopAction_SetSobreBoard_NoLanzaConEquityEfectivaFueraDeRango()
    {
        // Arrange
        // Con KK en K72 la equity cruda ronda 97.4 y el ajuste de c-bet la lleva a 100.004.
        // Sin el clamp, PostflopDecisionService.ValidateInput rechaza el input y la decisión muere.
        var profile = Fixtures.StrategyProfileFixture.LoadOrCreate();
        var state = new PlayerGameState
        {
            PotSize = 10m,
            HeroStack = 90m,
            IsInPosition = true,
            HandSituation = OpenScrape.Domain.Enums.HandSituation.OpenRaise,
            HoleCard1Rank = (int)OpenScrape.Domain.Enums.Rank.King,
            HoleCard1Suit = (int)OpenScrape.Domain.Enums.Suit.Spades,
            HoleCard2Rank = (int)OpenScrape.Domain.Enums.Rank.King,
            HoleCard2Suit = (int)OpenScrape.Domain.Enums.Suit.Diamonds,
            Players =
            [
                new Player { Name = "P0", Active = true, Stack = 90m, Bet = 0m, ValuePosition = 0 },
                new Player { Name = "P1", Active = true, Stack = 100m, Bet = 0m, ValuePosition = 1 },
                new Player { Name = "P2", Active = true, Stack = 100m, Bet = 0m, ValuePosition = 2 }
            ],
            BoardCards =
            [
                new BoardData { Force = (int)OpenScrape.Domain.Enums.Rank.King, Suit = (int)OpenScrape.Domain.Enums.Suit.Hearts, Position = OpenScrape.Domain.Enums.BoardPosition.Flop, Location = 1 },
                new BoardData { Force = (int)OpenScrape.Domain.Enums.Rank.Seven, Suit = (int)OpenScrape.Domain.Enums.Suit.Clubs, Position = OpenScrape.Domain.Enums.BoardPosition.Flop, Location = 2 },
                new BoardData { Force = (int)OpenScrape.Domain.Enums.Rank.Two, Suit = (int)OpenScrape.Domain.Enums.Suit.Diamonds, Position = OpenScrape.Domain.Enums.BoardPosition.Flop, Location = 3 }
            ]
        };

        // Act
        var snapshot = Fixtures.PostflopDecisionRunner.RunStreet(
            profile, state, maxBet: 0m, monteCarloSeed: 20261023, street: OpenScrape.Domain.Enums.BoardPosition.Flop);

        // Assert
        Assert.That(snapshot.Action, Is.Not.Null.And.Not.Empty);
    }
}
