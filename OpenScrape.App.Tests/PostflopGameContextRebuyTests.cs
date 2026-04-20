using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del tracking de auto-rebuy en PostflopGameContext
/// (refactor-frmmain-coordinators Fase 4.3).
/// </summary>
[TestFixture]
public class PostflopGameContextRebuyTests
{
    [Test]
    public void TrackHeroStackForRebuy_PrimeraLectura_RegistraStackActual()
    {
        var ctx = new PostflopGameContext();

        var effective = ctx.TrackHeroStackForRebuy(80m);

        Assert.That(effective, Is.EqualTo(80m));
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(80m));
    }

    [Test]
    public void TrackHeroStackForRebuy_DescensoNormal_ActualizaStack()
    {
        var ctx = new PostflopGameContext();
        ctx.TrackHeroStackForRebuy(80m);

        var effective = ctx.TrackHeroStackForRebuy(60m);

        Assert.That(effective, Is.EqualTo(60m));
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(60m));
    }

    [Test]
    public void TrackHeroStackForRebuy_IncrementoPequeno_ActualizaStack()
    {
        var ctx = new PostflopGameContext();
        ctx.TrackHeroStackForRebuy(80m);

        var effective = ctx.TrackHeroStackForRebuy(90m); // +10, pequeño (típico de win de bote)

        Assert.That(effective, Is.EqualTo(90m));
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(90m));
    }

    [Test]
    public void TrackHeroStackForRebuy_IncrementoMasivo_DetectaRebuyYConservaPrevio()
    {
        var ctx = new PostflopGameContext();
        ctx.TrackHeroStackForRebuy(20m); // hero bajó tras perder

        var effective = ctx.TrackHeroStackForRebuy(100m); // auto-rebuy a 100BB

        Assert.That(effective, Is.EqualTo(20m),
            "Debe devolver el stack pre-rebuy para no contaminar el profit");
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(20m),
            "El stack pre-rebuy se conserva intacto tras detectar rebuy");
    }

    [Test]
    public void TrackHeroStackForRebuy_IncrementoJustoEnUmbral_CuentaComoRebuy()
    {
        var ctx = new PostflopGameContext();
        ctx.TrackHeroStackForRebuy(40m);

        var effective = ctx.TrackHeroStackForRebuy(90m); // +50 exacto

        Assert.That(effective, Is.EqualTo(40m));
        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(40m));
    }

    [Test]
    public void Reset_LimpiaHeroStackPreRebuy()
    {
        var ctx = new PostflopGameContext();
        ctx.TrackHeroStackForRebuy(80m);

        ctx.Reset();

        Assert.That(ctx.HeroStackPreRebuy, Is.EqualTo(0m));
    }
}
