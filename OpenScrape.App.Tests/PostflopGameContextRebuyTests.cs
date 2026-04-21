using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del tracking de auto-rebuy en <see cref="PostflopGameContext"/> (record inmutable).
/// La API antigua <c>TrackHeroStackForRebuy</c> mutaba y devolvía decimal;
/// la nueva <c>TrackHeroStack</c> devuelve <c>(NewContext, EffectiveStack)</c> sin mutar.
/// </summary>
[TestFixture]
public class PostflopGameContextRebuyTests
{
    [Test]
    public void TrackHeroStack_PrimeraLectura_RegistraStackActual()
    {
        var ctx = PostflopGameContext.NewHand();

        var (next, effective) = ctx.TrackHeroStack(80m);

        Assert.That(effective, Is.EqualTo(80m));
        Assert.That(next.HeroStackPreRebuy, Is.EqualTo(80m));
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(0m), "El contexto original no debe mutar");
    }

    [Test]
    public void TrackHeroStack_DescensoNormal_ActualizaStack()
    {
        var (ctx, _) = PostflopGameContext.NewHand().TrackHeroStack(80m);

        var (next, effective) = ctx.TrackHeroStack(60m);

        Assert.That(effective, Is.EqualTo(60m));
        Assert.That(next.HeroStackPreRebuy, Is.EqualTo(60m));
    }

    [Test]
    public void TrackHeroStack_IncrementoPequeno_ActualizaStack()
    {
        var (ctx, _) = PostflopGameContext.NewHand().TrackHeroStack(80m);

        var (next, effective) = ctx.TrackHeroStack(90m); // +10, típico win de bote

        Assert.That(effective, Is.EqualTo(90m));
        Assert.That(next.HeroStackPreRebuy, Is.EqualTo(90m));
    }

    [Test]
    public void TrackHeroStack_IncrementoMasivo_DetectaRebuyYConservaPrevio()
    {
        var (ctx, _) = PostflopGameContext.NewHand().TrackHeroStack(20m); // hero bajó tras perder

        var (next, effective) = ctx.TrackHeroStack(100m); // auto-rebuy a 100BB

        Assert.That(effective, Is.EqualTo(20m),
            "Debe devolver el stack pre-rebuy para no contaminar el profit");
        Assert.That(next.HeroStackPreRebuy, Is.EqualTo(20m),
            "El stack pre-rebuy se conserva intacto tras detectar rebuy");
    }

    [Test]
    public void TrackHeroStack_IncrementoJustoEnUmbral_CuentaComoRebuy()
    {
        var (ctx, _) = PostflopGameContext.NewHand().TrackHeroStack(40m);

        var (next, effective) = ctx.TrackHeroStack(90m); // +50 exacto

        Assert.That(effective, Is.EqualTo(40m));
        Assert.That(next.HeroStackPreRebuy, Is.EqualTo(40m));
    }

    [Test]
    public void NewHand_InicializaHeroStackPreRebuyAZero()
    {
        var ctx = PostflopGameContext.NewHand();

        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(0m));
    }
}
