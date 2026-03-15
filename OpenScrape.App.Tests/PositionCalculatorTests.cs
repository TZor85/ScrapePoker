using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

[TestFixture]
public class PositionCalculatorTests
{
    private List<Player> CreatePlayers(params int[] valuePositions)
    {
        return valuePositions.Select(vp => new Player
        {
            Name = $"P{vp}",
            ValuePosition = vp,
            Empty = false,
            SitOut = false,
            Active = true
        }).ToList();
    }

    #region DetermineP0Position Tests

    [Test]
    public void DetermineP0Position_2Jugadores_DealerEnP0_DebeRetornarSmallBlind()
    {
        var players = CreatePlayers(0, 1);
        var result = PositionCalculator.DetermineP0Position(0, players);

        Assert.That(result, Is.EqualTo(TablePosition.SmallBlind));
    }

    [Test]
    public void DetermineP0Position_2Jugadores_DealerEnP1_DebeRetornarBigBlind()
    {
        var players = CreatePlayers(0, 1);
        var result = PositionCalculator.DetermineP0Position(1, players);

        Assert.That(result, Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void DetermineP0Position_3Jugadores_DealerEnP0_DebeRetornarButton()
    {
        var players = CreatePlayers(0, 1, 2);
        var result = PositionCalculator.DetermineP0Position(0, players);

        Assert.That(result, Is.EqualTo(TablePosition.Button));
    }

    [Test]
    public void DetermineP0Position_3Jugadores_DealerEnP1_DebeRetornarBigBlind()
    {
        var players = CreatePlayers(0, 1, 2);
        var result = PositionCalculator.DetermineP0Position(1, players);

        Assert.That(result, Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void DetermineP0Position_3Jugadores_DealerEnP2_DebeRetornarSmallBlind()
    {
        var players = CreatePlayers(0, 1, 2);
        var result = PositionCalculator.DetermineP0Position(2, players);

        Assert.That(result, Is.EqualTo(TablePosition.SmallBlind));
    }

    [Test]
    public void DetermineP0Position_4Jugadores_DealerEnP2_DebeRetornarBigBlind()
    {
        var players = CreatePlayers(0, 1, 2, 3);
        var result = PositionCalculator.DetermineP0Position(2, players);

        Assert.That(result, Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void DetermineP0Position_4Jugadores_DealerEnP3_DebeRetornarSmallBlind()
    {
        var players = CreatePlayers(0, 1, 2, 3);
        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.SmallBlind));
    }

    [Test]
    public void DetermineP0Position_5Jugadores_DealerEnP2_DebeRetornarMiddle()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4);
        var result = PositionCalculator.DetermineP0Position(2, players);

        Assert.That(result, Is.EqualTo(TablePosition.Middle));
    }

    [Test]
    public void DetermineP0Position_5Jugadores_DealerEnP1_DebeRetornarCutOff()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4);
        var result = PositionCalculator.DetermineP0Position(1, players);

        Assert.That(result, Is.EqualTo(TablePosition.CutOff));
    }

    [Test]
    public void DetermineP0Position_6Jugadores_DealerEnP2_DebeRetornarMiddle()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.DetermineP0Position(2, players);

        Assert.That(result, Is.EqualTo(TablePosition.Middle));
    }

    [Test]
    public void DetermineP0Position_6Jugadores_DealerEnP3_DebeRetornarEarly()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.Early));
    }

    [Test]
    public void DetermineP0Position_DealerNoActivo_DebeRetornarNone()
    {
        var players = new List<Player>
        {
            new Player { Name = "P0", ValuePosition = 0, Empty = false, SitOut = false },
            new Player { Name = "P1", ValuePosition = 1, Empty = true, SitOut = false },
            new Player { Name = "P2", ValuePosition = 2, Empty = false, SitOut = false }
        };

        var result = PositionCalculator.DetermineP0Position(1, players);

        Assert.That(result, Is.EqualTo(TablePosition.None));
    }

    [Test]
    public void DetermineP0Position_HeroNoActivo_DebeRetornarNone()
    {
        var players = new List<Player>
        {
            new Player { Name = "P0", ValuePosition = 0, Empty = true, SitOut = false },
            new Player { Name = "P1", ValuePosition = 1, Empty = false, SitOut = false },
            new Player { Name = "P2", ValuePosition = 2, Empty = false, SitOut = false }
        };

        var result = PositionCalculator.DetermineP0Position(1, players);

        Assert.That(result, Is.EqualTo(TablePosition.None));
    }

    #endregion

    #region AssignVillainPositions Tests

    [Test]
    public void AssignVillainPositions_6Jugadores_HeroMiddle_DebeAsignarCorrectamente()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.Middle, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.CutOff));
        Assert.That(result[2], Is.EqualTo(TablePosition.Button));
        Assert.That(result[3], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[4], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[5], Is.EqualTo(TablePosition.Early));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_HeroButton_DebeAsignarCorrectamente()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.Button, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[2], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[3], Is.EqualTo(TablePosition.Early));
        Assert.That(result[4], Is.EqualTo(TablePosition.Middle));
        Assert.That(result[5], Is.EqualTo(TablePosition.CutOff));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_HeroCutOff_DebeAsignarCorrectamente()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.CutOff, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.Button));
        Assert.That(result[2], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[3], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[4], Is.EqualTo(TablePosition.Early));
        Assert.That(result[5], Is.EqualTo(TablePosition.Middle));
    }

    [Test]
    public void AssignVillainPositions_5Jugadores_HeroBigBlind_DebeAsignarCorrectamente()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.BigBlind, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[2], Is.EqualTo(TablePosition.Button));
        Assert.That(result[3], Is.EqualTo(TablePosition.CutOff));
        Assert.That(result[4], Is.EqualTo(TablePosition.Early));
    }

    [Test]
    public void AssignVillainPositions_4Jugadores_DebeExcluirMiddle()
    {
        var players = CreatePlayers(0, 1, 2, 3);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.Middle, players);

        Assert.That(result.Values, Does.Not.Contains(TablePosition.Middle));
    }

    [Test]
    public void AssignVillainPositions_3Jugadores_DebeExcluirMiddleyEarly()
    {
        var players = CreatePlayers(0, 1, 2);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.Button, players);

        Assert.That(result.Values, Does.Not.Contains(TablePosition.Middle));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.Early));
    }

    [Test]
    public void AssignVillainPositions_2Jugadores_DebeExcluirMiddleEarlyCutOff()
    {
        var players = CreatePlayers(0, 1);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.SmallBlind, players);

        Assert.That(result.Values, Does.Not.Contains(TablePosition.Middle));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.Early));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.CutOff));
    }

    [Test]
    public void AssignVillainPositions_SinJugadoresActivos_DebeRetornarDiccionarioVacio()
    {
        var players = CreatePlayers(0);
        var result = PositionCalculator.AssignVillainPositions(TablePosition.Button, players);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void AssignVillainPositions_JugadoresConEmpty_DebeExcluirirlos()
    {
        var players = new List<Player>
        {
            new Player { Name = "P0", ValuePosition = 0, Empty = false, SitOut = false, Active = true },
            new Player { Name = "P1", ValuePosition = 1, Empty = true, SitOut = false, Active = true },
            new Player { Name = "P2", ValuePosition = 2, Empty = false, SitOut = false, Active = true },
            new Player { Name = "P3", ValuePosition = 3, Empty = false, SitOut = true, Active = true }
        };

        var result = PositionCalculator.AssignVillainPositions(TablePosition.Button, players);

        Assert.That(result.Keys, Does.Not.Contains(1));
        Assert.That(result.Keys, Does.Not.Contains(3));
    }

    #endregion
}
