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

    #region DetermineP0Position con SitOut

    [Test]
    public void DetermineP0Position_6Jugadores_DealerP3_SitOutFueraDelContinuo_NoMueveCiegas()
    {
        // Escenario real de bug: Hero P0, dealer P3, P2 SitOut (CutOff, fuera del continuo).
        // Resultado correcto: ciegas físicas P4=SB, P5=BB, Hero=Early.
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 2).SitOut = true;

        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.Early));
    }

    [Test]
    public void DetermineP0Position_6Jugadores_DealerP3_SitOutEnSB_DesplazaSBaP5()
    {
        // P4 SitOut (justo a la izquierda del dealer): SB se desplaza a P5, BB cae en P0 (Hero).
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 4).SitOut = true;

        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void DetermineP0Position_6Jugadores_DealerP3_DosSitOutConsecutivos_DesplazaAmbasCiegas()
    {
        // P4 y P5 SitOut (continuo de dos): SB=P0 (Hero), BB=P1.
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 4).SitOut = true;
        players.First(p => p.ValuePosition == 5).SitOut = true;

        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.SmallBlind));
    }

    [Test]
    public void DetermineP0Position_6Jugadores_DealerP3_SitOutEnBB_DesplazaBBaP0()
    {
        // P4 activo (SB), P5 SitOut: la BB salta hasta el siguiente activo (P0=Hero).
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 5).SitOut = true;

        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void DetermineP0Position_DealerP3_P4Empty_SBPasaAP5()
    {
        // Asiento P4 vacío (nadie sentado): SB salta físicamente a P5.
        var players = new List<Player>
        {
            new Player { Name = "P0", ValuePosition = 0, Active = true },
            new Player { Name = "P1", ValuePosition = 1, Active = true },
            new Player { Name = "P2", ValuePosition = 2, Active = true },
            new Player { Name = "P3", ValuePosition = 3, Active = true },
            new Player { Name = "P4", ValuePosition = 4, Empty = true },
            new Player { Name = "P5", ValuePosition = 5, Active = true }
        };

        var result = PositionCalculator.DetermineP0Position(3, players);

        // Anillo efectivo 5-handed: P3=Button, P5=SB, P0=BB, P1=Middle, P2=CutOff.
        Assert.That(result, Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void DetermineP0Position_DealerP3_P4EmptyP5SitOut_SBPasaAP0()
    {
        // P4 vacío y P5 SitOut: ambos consumen el tramo, SB llega a P0 (Hero).
        var players = new List<Player>
        {
            new Player { Name = "P0", ValuePosition = 0, Active = true },
            new Player { Name = "P1", ValuePosition = 1, Active = true },
            new Player { Name = "P2", ValuePosition = 2, Active = true },
            new Player { Name = "P3", ValuePosition = 3, Active = true },
            new Player { Name = "P4", ValuePosition = 4, Empty = true },
            new Player { Name = "P5", ValuePosition = 5, SitOut = true }
        };

        var result = PositionCalculator.DetermineP0Position(3, players);

        Assert.That(result, Is.EqualTo(TablePosition.SmallBlind));
    }

    [Test]
    public void DetermineP0Position_6Jugadores_DealerP0_SitOutEntreSByBB_DesplazaBB()
    {
        // Caso real del log: dealer P0 (Hero=Button), P2 SitOut entre SB y BB.
        // SB=P1 (activo, primer left del dealer), BB salta P2 y cae en P3.
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 2).SitOut = true;

        var result = PositionCalculator.DetermineP0Position(0, players);

        Assert.That(result, Is.EqualTo(TablePosition.Button));
    }

    #endregion

    #region AssignVillainPositions Tests

    [Test]
    public void AssignVillainPositions_6Jugadores_DealerP2_DebeAsignarCorrectamente()
    {
        // Dealer P2 → Hero=Middle. Ciegas a la izquierda: P3=SB, P4=BB.
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.AssignVillainPositions(2, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.CutOff));
        Assert.That(result[2], Is.EqualTo(TablePosition.Button));
        Assert.That(result[3], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[4], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[5], Is.EqualTo(TablePosition.Early));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_DealerP0_HeroButton()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.AssignVillainPositions(0, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[2], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[3], Is.EqualTo(TablePosition.Early));
        Assert.That(result[4], Is.EqualTo(TablePosition.Middle));
        Assert.That(result[5], Is.EqualTo(TablePosition.CutOff));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_DealerP1_HeroCutOff()
    {
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        var result = PositionCalculator.AssignVillainPositions(1, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.Button));
        Assert.That(result[2], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[3], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[4], Is.EqualTo(TablePosition.Early));
        Assert.That(result[5], Is.EqualTo(TablePosition.Middle));
    }

    [Test]
    public void AssignVillainPositions_5Jugadores_DealerP3_HeroBigBlind()
    {
        // 5 activos: posiciones [Button, SB, BB, Middle, CutOff] (sin Early).
        // Dealer P3 → Hero=BB. Yendo a la izquierda desde BB: P1=Middle, P2=CutOff.
        var players = CreatePlayers(0, 1, 2, 3, 4);
        var result = PositionCalculator.AssignVillainPositions(3, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.Middle));
        Assert.That(result[2], Is.EqualTo(TablePosition.CutOff));
        Assert.That(result[3], Is.EqualTo(TablePosition.Button));
        Assert.That(result[4], Is.EqualTo(TablePosition.SmallBlind));
    }

    [Test]
    public void AssignVillainPositions_4Jugadores_NoDebeContenerMiddleNiEarly()
    {
        // 4 activos: posiciones [Button, SB, BB, CutOff].
        var players = CreatePlayers(0, 1, 2, 3);
        var result = PositionCalculator.AssignVillainPositions(2, players);

        Assert.That(result.Values, Does.Not.Contains(TablePosition.Middle));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.Early));
    }

    [Test]
    public void AssignVillainPositions_3Jugadores_SoloButtonSBBB()
    {
        var players = CreatePlayers(0, 1, 2);
        var result = PositionCalculator.AssignVillainPositions(0, players);

        Assert.That(result.Values, Does.Not.Contains(TablePosition.Middle));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.Early));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.CutOff));
        Assert.That(result[1], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[2], Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void AssignVillainPositions_2Jugadores_HeadsUp()
    {
        var players = CreatePlayers(0, 1);
        var result = PositionCalculator.AssignVillainPositions(0, players);

        Assert.That(result.Values, Does.Not.Contains(TablePosition.Middle));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.Early));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.CutOff));
        Assert.That(result.Values, Does.Not.Contains(TablePosition.Button));
        Assert.That(result[1], Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void AssignVillainPositions_SoloHero_RetornaDiccionarioVacio()
    {
        var players = CreatePlayers(0);
        var result = PositionCalculator.AssignVillainPositions(0, players);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void AssignVillainPositions_EmptyExcluido_SitOutFueraDelTramoMantienePosicion()
    {
        // P1 Empty (fuera del anillo). P5 SitOut pero fuera del tramo dealer→SB→BB:
        // mantiene asiento y posición CutOff.
        var players = new List<Player>
        {
            new Player { Name = "P0", ValuePosition = 0, Empty = false, SitOut = false, Active = true },
            new Player { Name = "P1", ValuePosition = 1, Empty = true, SitOut = false, Active = true },
            new Player { Name = "P2", ValuePosition = 2, Empty = false, SitOut = false, Active = true },
            new Player { Name = "P3", ValuePosition = 3, Empty = false, SitOut = false, Active = true },
            new Player { Name = "P4", ValuePosition = 4, Empty = false, SitOut = false, Active = true },
            new Player { Name = "P5", ValuePosition = 5, Empty = false, SitOut = true, Active = true }
        };

        // Dealer P0 (Hero). Anillo útil: [P0, P2, P3, P4, P5]. SB=P2, BB=P3, P4=Middle, P5=CutOff (SitOut).
        var result = PositionCalculator.AssignVillainPositions(0, players);

        Assert.That(result.Keys, Does.Not.Contains(1));
        Assert.That(result[2], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[3], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[4], Is.EqualTo(TablePosition.Middle));
        Assert.That(result.Keys, Does.Contain(5));
        Assert.That(result[5], Is.EqualTo(TablePosition.CutOff));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_DealerP3_P2SitOut_AsignacionCompleta()
    {
        // Regresión del bug reportado: dealer P3, P2 SitOut (fuera del tramo dealer→SB→BB).
        // Esperado: P3=Button, P4=SB, P5=BB, P0=Early (Hero), P1=Middle, P2=CutOff.
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 2).SitOut = true;

        var result = PositionCalculator.AssignVillainPositions(3, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.Middle));
        Assert.That(result[2], Is.EqualTo(TablePosition.CutOff));
        Assert.That(result[3], Is.EqualTo(TablePosition.Button));
        Assert.That(result[4], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result[5], Is.EqualTo(TablePosition.BigBlind));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_DealerP0_P2SitOut_BBSaltaAP3()
    {
        // Regresión bug #2: dealer P0 (Hero=Button), P2 SitOut entre SB y BB.
        // Mesa efectiva 5-handed: P0=Button, P1=SB, P3=BB, P4=Middle, P5=CutOff.
        // P2 queda sin posición (asiento muerto).
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 2).SitOut = true;

        var result = PositionCalculator.AssignVillainPositions(0, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result.ContainsKey(2), Is.False, "P2 SitOut consumido por salto de BB queda sin posición");
        Assert.That(result[3], Is.EqualTo(TablePosition.BigBlind));
        Assert.That(result[4], Is.EqualTo(TablePosition.Middle));
        Assert.That(result[5], Is.EqualTo(TablePosition.CutOff));
    }

    [Test]
    public void AssignVillainPositions_6Jugadores_DealerP3_P5SitOut_BBSaltaAP0()
    {
        // Dealer P3, P4 activo (SB), P5 SitOut: BB salta a P0 (Hero).
        // Mesa efectiva 5-handed: P3=Button, P4=SB, P0=BB, P1=Middle, P2=CutOff. P5 sin posición.
        var players = CreatePlayers(0, 1, 2, 3, 4, 5);
        players.First(p => p.ValuePosition == 5).SitOut = true;

        var result = PositionCalculator.AssignVillainPositions(3, players);

        Assert.That(result[1], Is.EqualTo(TablePosition.Middle));
        Assert.That(result[2], Is.EqualTo(TablePosition.CutOff));
        Assert.That(result[3], Is.EqualTo(TablePosition.Button));
        Assert.That(result[4], Is.EqualTo(TablePosition.SmallBlind));
        Assert.That(result.ContainsKey(5), Is.False, "P5 SitOut consumido por salto de BB queda sin posición");
    }

    #endregion
}
