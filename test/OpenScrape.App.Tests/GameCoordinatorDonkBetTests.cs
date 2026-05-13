using OpenScrape.App.Entities;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests para la lógica de detección de DonkBet.
/// Verifica la corrección del filtro que excluye hero (P0) del check de agresor villain,
/// y la condición de que hero debe ser el agresor para que exista donk bet.
/// </summary>
[TestFixture]
public class GameCoordinatorDonkBetTests
{
    /// <summary>
    /// Simula la lógica de DetectDonkBet de GameCoordinator sin instanciar todas las dependencias.
    /// Replica exactamente el código de GameCoordinator.DetectDonkBet.
    /// </summary>
    private static (bool IsDonkBet, HandSituation DonkBetSituation) SimulateDetectDonkBet(
        List<Player> players, decimal maxBet, HandSituation currentSituation,
        bool heroBetPreviousStreet = false, bool isTurnOrLater = false)
    {
        // Lógica replicada de GameCoordinator.DetectDonkBet (post-fix)
        bool villainWasPreflopAggressor = players
            .Any(p => p.Active && p.WasPreflopAggressor && p.ValuePosition != 0);

        bool heroWasPreviousStreetAggressor = isTurnOrLater && heroBetPreviousStreet;

        var heroPlayer = players.FirstOrDefault(p => p.ValuePosition == 0);
        bool heroWasPreflopAggressor = heroPlayer?.WasPreflopAggressor == true;
        bool heroIsAggressor = heroWasPreflopAggressor || heroWasPreviousStreetAggressor;

        if (!heroIsAggressor)
            return (false, currentSituation);

        bool effectiveVillainAggressor = villainWasPreflopAggressor && !heroWasPreviousStreetAggressor;
        return PreflopAnalyzer.DetectDonkBet(maxBet, effectiveVillainAggressor, currentSituation);
    }

    private static List<Player> CreatePlayers(bool heroAggressor, bool villainAggressor)
    {
        return new List<Player>
        {
            new() { Name = "Hero", ValuePosition = 0, Active = true, WasPreflopAggressor = heroAggressor },
            new() { Name = "Villain", ValuePosition = 1, Active = true, WasPreflopAggressor = villainAggressor }
        };
    }

    // ─── BUG 1: Pot limpeado → NO donk bet ──────────────────

    [Test]
    public void DetectDonkBet_PotLimpeado_NoDonkBet()
    {
        var players = CreatePlayers(heroAggressor: false, villainAggressor: false);

        var (isDonk, _) = SimulateDetectDonkBet(
            players, maxBet: 7m, HandSituation.RaiseOverLimper);

        Assert.That(isDonk, Is.False,
            "Pot limpeado sin agresor → no puede ser donk bet");
    }

    // ─── BUG 2: Hero raiseó → P0 excluido del check villain ─

    [Test]
    public void DetectDonkBet_HeroRaiseo_VillainBetea_EsDonkBet()
    {
        var players = CreatePlayers(heroAggressor: true, villainAggressor: false);

        var (isDonk, situation) = SimulateDetectDonkBet(
            players, maxBet: 5m, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.True,
            "Hero agresor + villain bet = donk bet");
        Assert.That(situation, Is.EqualTo(HandSituation.DonkBetVsOpenRaise));
    }

    [Test]
    public void DetectDonkBet_HeroP0ExcluidoDeVillainCheck()
    {
        // Hero (P0) con WasPreflopAggressor=true no debe contarse como villain
        var players = CreatePlayers(heroAggressor: true, villainAggressor: false);

        // Si P0 se contara como villain, villainWasPreflopAggressor=true → NO donk bet (bug anterior)
        var (isDonk, _) = SimulateDetectDonkBet(
            players, maxBet: 5m, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.True,
            "Hero P0 excluido del check villain → donk bet detectado correctamente");
    }

    // ─── Villain raiseó → c-bet, no donk bet ────────────────

    [Test]
    public void DetectDonkBet_VillainRaiseo_NoDonkBet()
    {
        var players = CreatePlayers(heroAggressor: false, villainAggressor: true);

        var (isDonk, _) = SimulateDetectDonkBet(
            players, maxBet: 5m, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.False,
            "Villain agresor → su bet es c-bet, no donk bet");
    }

    // ─── Cross-street: hero apostó flop → donk bet en turn ──

    [Test]
    public void DetectDonkBet_HeroBetFlop_VillainBeteaTurn_EsDonkBet()
    {
        // Nadie raiseó preflop, pero hero apostó en flop
        var players = CreatePlayers(heroAggressor: false, villainAggressor: false);

        var (isDonk, _) = SimulateDetectDonkBet(
            players, maxBet: 5m, HandSituation.RaiseOverLimper,
            heroBetPreviousStreet: true, isTurnOrLater: true);

        Assert.That(isDonk, Is.True,
            "Hero apostó flop → agresor cross-street → villain turn bet = donk bet");
    }

    // ─── Sin bet → no donk bet ──────────────────────────────

    [Test]
    public void DetectDonkBet_SinBet_NoDonkBet()
    {
        var players = CreatePlayers(heroAggressor: true, villainAggressor: false);

        var (isDonk, _) = SimulateDetectDonkBet(
            players, maxBet: 0m, HandSituation.OpenRaise);

        Assert.That(isDonk, Is.False,
            "maxBet=0 → nadie apostó → no donk bet");
    }

    // ─── Ambos agresores → no donk bet ──────────────────────

    [Test]
    public void DetectDonkBet_AmbosAgresores_NoDonkBet()
    {
        // Tanto hero como villain raisearon (4bet pot por ejemplo)
        var players = CreatePlayers(heroAggressor: true, villainAggressor: true);

        var (isDonk, _) = SimulateDetectDonkBet(
            players, maxBet: 5m, HandSituation.FourBet);

        Assert.That(isDonk, Is.False,
            "Villain también fue agresor → no es donk bet");
    }
}
