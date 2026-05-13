using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Tests;

[TestFixture]
public class PostflopGameContextImmutableTests
{
    [Test]
    public void NewHand_ProduceContextoEnEstadoInicial()
    {
        var ctx = PostflopGameContext.NewHand();

        Assert.That(ctx.HeroBetFlop, Is.False);
        Assert.That(ctx.HeroBetTurn, Is.False);
        Assert.That(ctx.VillainBetFlop, Is.False);
        Assert.That(ctx.VillainBetTurn, Is.False);
        Assert.That(ctx.PreviousStreetWasBet, Is.False);
        Assert.That(ctx.IsAnyoneAllIn, Is.False);
        Assert.That(ctx.TurnBetCommitsToRiver, Is.False);
        Assert.That(ctx.HeroFloatedFlop, Is.False);
        Assert.That(ctx.VillainBetSizeFlop, Is.EqualTo(BetSizeCategory.NoBet));
        Assert.That(ctx.VillainBetSizeTurn, Is.EqualTo(BetSizeCategory.NoBet));
        Assert.That(ctx.InitialBoardDanger.DangerLevel, Is.EqualTo(0));
        Assert.That(ctx.LastBoardChange.DangerLevel, Is.EqualTo(0));
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(0m));
    }

    [Test]
    public void WithFlopState_EsAtomicoYNoMutaElOriginal()
    {
        var ctx = PostflopGameContext.NewHand();

        var next = ctx.WithFlopState(heroBet: true, villainBet: false, isPreflopAggressor: true);

        Assert.That(next.HeroBetFlop, Is.True);
        Assert.That(next.VillainBetFlop, Is.False);
        Assert.That(next.PreviousStreetWasBet, Is.True);
        Assert.That(next.VillainAggressorCheckedFlop, Is.False, "Hero era el agresor preflop, no villain");

        Assert.That(ctx.HeroBetFlop, Is.False, "El contexto original no debe mutarse");
    }

    [Test]
    public void WithFlopState_VillainAggressorNoApuesta_MarcaAggressorChecked()
    {
        var ctx = PostflopGameContext.NewHand();

        var next = ctx.WithFlopState(heroBet: false, villainBet: false, isPreflopAggressor: false);

        Assert.That(next.VillainAggressorCheckedFlop, Is.True,
            "Villain era agresor preflop y no apostó en flop → señal de debilidad");
    }

    [Test]
    public void WithTurnState_DerivaVillainCheckedMiddleStreet()
    {
        var flopCtx = PostflopGameContext.NewHand() with { VillainBetFlop = true };

        var next = flopCtx.WithTurnState(heroBet: false, villainBet: false);

        Assert.That(next.VillainCheckedMiddleStreet, Is.True,
            "VillainBetFlop=true && !villainBetTurn → patrón bet-check");
    }

    [Test]
    public void WithTurnState_VillainSigueApostando_NoMarcaCheckedMiddleStreet()
    {
        var flopCtx = PostflopGameContext.NewHand() with { VillainBetFlop = true };

        var next = flopCtx.WithTurnState(heroBet: false, villainBet: true);

        Assert.That(next.VillainCheckedMiddleStreet, Is.False,
            "Villain siguió apostando → no es bet-check");
    }

    [Test]
    public void RecordEquality_ContextosConMismoEstado_SonIguales()
    {
        var a = PostflopGameContext.NewHand() with { HeroBetFlop = true, VillainBetFlop = true };
        var b = PostflopGameContext.NewHand() with { HeroBetFlop = true, VillainBetFlop = true };

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
    }

    [Test]
    public void RecordEquality_ContextosDistintos_NoSonIguales()
    {
        var a = PostflopGameContext.NewHand() with { HeroBetFlop = true };
        var b = PostflopGameContext.NewHand() with { HeroBetFlop = false };

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void HeroCheckedAllStreets_DerivaDeHeroBetFlopYTurn()
    {
        var initial = PostflopGameContext.NewHand();
        var flopBet = initial with { HeroBetFlop = true };
        var turnBet = initial with { HeroBetTurn = true };

        Assert.That(initial.HeroCheckedAllStreets, Is.True);
        Assert.That(flopBet.HeroCheckedAllStreets, Is.False);
        Assert.That(turnBet.HeroCheckedAllStreets, Is.False);
    }

    [Test]
    public void IsVillainBarreling_RequiereBetEnFlopYTurn()
    {
        var flopOnly = PostflopGameContext.NewHand() with { VillainBetFlop = true };
        var both = PostflopGameContext.NewHand() with { VillainBetFlop = true, VillainBetTurn = true };

        Assert.That(flopOnly.IsVillainBarreling, Is.False);
        Assert.That(both.IsVillainBarreling, Is.True);
    }
}
