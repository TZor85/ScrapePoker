using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests;

[TestFixture]
public class OpponentTrackerTests
{
    private OpponentTracker _tracker;

    [SetUp]
    public void Setup()
    {
        _tracker = new OpponentTracker();
    }

    [Test]
    public void GetProfile_NuevoJugador_DeberiaCrearPerfil()
    {
        var profile = _tracker.GetProfile("Player1");

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile.PlayerId, Is.EqualTo("Player1"));
        Assert.That(profile.HandsPlayed, Is.EqualTo(0));
    }

    [Test]
    public void GetProfile_MismoJugador_DeberiaRetornarMismoObjeto()
    {
        var profile1 = _tracker.GetProfile("Player1");
        profile1.HandsPlayed = 5;
        var profile2 = _tracker.GetProfile("Player1");

        Assert.That(profile2.HandsPlayed, Is.EqualTo(5));
    }

    [Test]
    public void GetProfile_CaseInsensitive()
    {
        _tracker.RecordHandPlayed("player1");
        var profile = _tracker.GetProfile("PLAYER1");

        Assert.That(profile.HandsPlayed, Is.EqualTo(1));
    }

    [Test]
    public void RecordVPIP_DeberiaAcumularse()
    {
        _tracker.RecordHandPlayed("P1");
        _tracker.RecordHandPlayed("P1");
        _tracker.RecordHandPlayed("P1");
        _tracker.RecordVPIP("P1");
        _tracker.RecordVPIP("P1");

        var profile = _tracker.GetProfile("P1");
        Assert.That(profile.VPIP, Is.EqualTo(2.0 / 3.0 * 100).Within(0.1));
    }

    [Test]
    public void AggressionFactor_ConBetsYCalls_DeberiaCalcularCorrectamente()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesPostflopBet = 6;
        profile.TimesPostflopRaised = 4;
        profile.TimesPostflopCalled = 5;

        // AF Laplace = (10+1) / (5+1) = 11/6 ≈ 1.833
        Assert.That(profile.AggressionFactor, Is.EqualTo(11.0 / 6.0).Within(0.01));
    }

    [Test]
    public void AggressionFactor_SinCalls_DeberiaRetornarLaplaceSmoothed()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesPostflopBet = 5;

        // AF Laplace = (5+1) / (0+1) = 6.0
        Assert.That(profile.AggressionFactor, Is.EqualTo(6.0));
    }

    [Test]
    public void OpponentType_TAG_VPIP20_AF2()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 50;
        profile.TimesVoluntarilyPutMoneyIn = 10; // VPIP = 20% (tight)
        profile.TimesPostflopBet = 8;
        profile.TimesPostflopRaised = 4;
        profile.TimesPostflopCalled = 5; // AF = 2.4 (aggressive)

        Assert.That(profile.Type, Is.EqualTo(OpponentType.TAG));
    }

    [Test]
    public void OpponentType_LP_VPIP50_AF05()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 50;
        profile.TimesVoluntarilyPutMoneyIn = 25; // VPIP = 50% (loose)
        profile.TimesPostflopBet = 2;
        profile.TimesPostflopCalled = 10; // AF = 0.2 (passive)

        Assert.That(profile.Type, Is.EqualTo(OpponentType.LP));
    }

    [Test]
    public void OpponentType_Unknown_PocasManos()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 5;
        profile.TimesVoluntarilyPutMoneyIn = 3;

        Assert.That(profile.Type, Is.EqualTo(OpponentType.Unknown));
    }

    [Test]
    public void IsReliable_MenosDe20_NoEsFiable()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 15;

        Assert.That(profile.IsReliable, Is.False);
    }

    [Test]
    public void IsReliable_20OMas_EsFiable()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 20;

        Assert.That(profile.IsReliable, Is.True);
    }

    [Test]
    public void GetAdjustedFoldEquity_Fish_IncrementaFoldEquity()
    {
        var profile = _tracker.GetProfile("Fish");
        profile.HandsPlayed = 30;
        profile.TimesVoluntarilyPutMoneyIn = 18; // VPIP = 60% (loose)
        profile.TimesPostflopBet = 2;
        profile.TimesPostflopCalled = 10; // AF = 0.2 (passive) → LP

        double adjusted = _tracker.GetAdjustedFoldEquity("Fish", 20.0);

        Assert.That(adjusted, Is.EqualTo(25.0)); // 20 * 1.25
    }

    [Test]
    public void GetAdjustedFoldEquity_LAG_ReduceFoldEquity()
    {
        var profile = _tracker.GetProfile("LAG");
        profile.HandsPlayed = 30;
        profile.TimesVoluntarilyPutMoneyIn = 15; // VPIP = 50% (loose)
        profile.TimesPostflopBet = 10;
        profile.TimesPostflopRaised = 5;
        profile.TimesPostflopCalled = 3; // AF = 5.0 (aggressive) → LAG

        double adjusted = _tracker.GetAdjustedFoldEquity("LAG", 20.0);

        Assert.That(adjusted, Is.EqualTo(14.0)); // 20 * 0.70
    }

    [Test]
    public void GetAdjustedFoldEquity_Unknown_NoAjusta()
    {
        _tracker.RecordHandPlayed("New");

        double adjusted = _tracker.GetAdjustedFoldEquity("New", 20.0);

        Assert.That(adjusted, Is.EqualTo(20.0));
    }

    [Test]
    public void RecordPostflopAction_Bet_DeberiaIncrementar()
    {
        _tracker.RecordPostflopAction("P1", PostflopAction.Bet);
        _tracker.RecordPostflopAction("P1", PostflopAction.Bet);
        _tracker.RecordPostflopAction("P1", PostflopAction.Call);

        var profile = _tracker.GetProfile("P1");
        Assert.That(profile.TimesPostflopBet, Is.EqualTo(2));
        Assert.That(profile.TimesPostflopCalled, Is.EqualTo(1));
    }

    [Test]
    public void FoldToCBet_DeberiaCalcularCorrectamente()
    {
        _tracker.RecordFacedCBet("P1", folded: true);
        _tracker.RecordFacedCBet("P1", folded: true);
        _tracker.RecordFacedCBet("P1", folded: false);

        var profile = _tracker.GetProfile("P1");
        Assert.That(profile.FoldToCBetPct, Is.EqualTo(2.0 / 3.0 * 100).Within(0.1));
    }

    [Test]
    public void Reset_DeberiaLimpiarTodo()
    {
        _tracker.RecordHandPlayed("P1");
        _tracker.RecordHandPlayed("P2");

        _tracker.Reset();

        Assert.That(_tracker.AllProfiles.Count, Is.EqualTo(0));
    }

    [Test]
    public void GetProfile_Vacio_DeberiaRetornarPerfilNuevo()
    {
        var profile = _tracker.GetProfile("");
        Assert.That(profile.HandsPlayed, Is.EqualTo(0));
    }

    // ─── Seat-Alias Cache ─────────────────────────────────────

    [Test]
    public void RegisterSeatAlias_ResolveName_RetornaAlias()
    {
        _tracker.RegisterSeatAlias("P3", "PlayerA");

        Assert.That(_tracker.ResolveName("P3"), Is.EqualTo("PlayerA"));
    }

    [Test]
    public void RegisterSeatAlias_MigraPerfilExistente()
    {
        // Acumular stats bajo seat name
        for (int i = 0; i < 10; i++)
            _tracker.RecordHandPlayed("P3");

        // Registrar alias → migra perfil
        _tracker.RegisterSeatAlias("P3", "PlayerA");

        var profile = _tracker.GetProfile("PlayerA");
        Assert.That(profile.HandsPlayed, Is.EqualTo(10),
            "Perfil migrado de P3 a PlayerA con 10 manos");
        Assert.That(_tracker.AllProfiles.ContainsKey("P3"), Is.False,
            "Perfil P3 ya no existe");
    }

    [Test]
    public void ResolveName_SeatDesconocido_RetornaNull()
    {
        Assert.That(_tracker.ResolveName("P5"), Is.Null);
    }

    [Test]
    public void Reset_LimpiaSeatAliasCache()
    {
        _tracker.RegisterSeatAlias("P3", "PlayerA");
        _tracker.Reset();

        Assert.That(_tracker.ResolveName("P3"), Is.Null,
            "Reset limpia el cache de aliases");
    }

    [Test]
    public void RegisterSeatAlias_NuevoJugadorMismoSeat()
    {
        _tracker.RegisterSeatAlias("P3", "PlayerA");
        _tracker.RecordHandPlayed("PlayerA");

        // Nuevo jugador en P3
        _tracker.RegisterSeatAlias("P3", "PlayerB");

        Assert.That(_tracker.ResolveName("P3"), Is.EqualTo("PlayerB"));
        Assert.That(_tracker.GetProfile("PlayerA").HandsPlayed, Is.EqualTo(1),
            "PlayerA mantiene su perfil");
    }

    // ─── L4: AF Laplace Smoothing ────────────────────────────

    [Test]
    public void AF_PassiveZero_UnaAccion_RetornaSmoothed()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesPostflopBet = 1;
        profile.TimesPostflopCalled = 0;

        // Laplace: (1+1)/(0+1) = 2.0 (antes era 3.0)
        Assert.That(profile.AggressionFactor, Is.EqualTo(2.0));
    }

    [Test]
    public void AF_SinAcciones_RetornaUno()
    {
        var profile = _tracker.GetProfile("P1");

        // Laplace: (0+1)/(0+1) = 1.0
        Assert.That(profile.AggressionFactor, Is.EqualTo(1.0));
    }

    [Test]
    public void AF_PassiveZero_MultiplesAcciones_RetornaSmoothed()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesPostflopBet = 3;
        profile.TimesPostflopRaised = 1;

        // Laplace: (4+1)/(0+1) = 5.0 (antes era 3.0)
        Assert.That(profile.AggressionFactor, Is.EqualTo(5.0));
    }

    [Test]
    public void AF_ConvergeConSampleGrande()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesPostflopBet = 25;
        profile.TimesPostflopRaised = 5;
        profile.TimesPostflopCalled = 10;

        // Laplace: (30+1)/(10+1) = 31/11 ≈ 2.818
        // Real sin smoothing: 30/10 = 3.0
        double af = profile.AggressionFactor;
        Assert.That(af, Is.EqualTo(31.0 / 11.0).Within(0.01));
        Assert.That(Math.Abs(af - 3.0), Is.LessThan(0.2), "Converge al valor real");
    }

    [Test]
    public void AF_IP_DatosInsuficientes_RetornaMenosUno()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesAggressiveIP = 2;
        profile.TimesPassiveIP = 1;

        Assert.That(profile.AggressionFactorIP, Is.EqualTo(-1));
    }

    [Test]
    public void AF_IP_PassiveZero_DatosSuficientes_RetornaSmoothed()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesAggressiveIP = 5;
        profile.TimesPassiveIP = 0;

        // Laplace: (5+1)/(0+1) = 6.0
        Assert.That(profile.AggressionFactorIP, Is.EqualTo(6.0));
    }

    [Test]
    public void AF_OOP_PassiveZero_DatosSuficientes_RetornaSmoothed()
    {
        var profile = _tracker.GetProfile("P1");
        profile.TimesAggressiveOOP = 6;
        profile.TimesPassiveOOP = 0;

        // Laplace: (6+1)/(0+1) = 7.0
        Assert.That(profile.AggressionFactorOOP, Is.EqualTo(7.0));
    }

    [Test]
    public void GetTypeForPosition_LaplacePrevieneFalsoAgresivo()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 15;
        profile.TimesVoluntarilyPutMoneyIn = 3; // VPIP 20% = tight
        profile.TimesPostflopBet = 1;
        profile.TimesPostflopCalled = 0;

        // AF Laplace = (1+1)/(0+1) = 2.0 > 1.5 → aún agresivo con 1 acción
        // Pero con fallback a IP/OOP que retorna -1 → usa global
        var type = profile.GetTypeForPosition(villainIsInPosition: true);
        // AF IP = -1 (insuf), fallback AF global = 2.0 > 1.5 → TAG
        Assert.That(type, Is.EqualTo(OpponentType.TAG));
    }

    [Test]
    public void GetTypeForPosition_FallbackAFGlobal()
    {
        var profile = _tracker.GetProfile("P1");
        profile.HandsPlayed = 20;
        profile.TimesVoluntarilyPutMoneyIn = 12; // VPIP 60% = loose
        profile.TimesPostflopBet = 1;
        profile.TimesPostflopCalled = 5;
        // AF global Laplace = (1+1)/(5+1) = 2/6 ≈ 0.33 < 1.5 → pasivo
        // AF IP = -1 (datos insuficientes) → fallback a global

        var type = profile.GetTypeForPosition(villainIsInPosition: true);
        Assert.That(type, Is.EqualTo(OpponentType.LP));
    }
}
