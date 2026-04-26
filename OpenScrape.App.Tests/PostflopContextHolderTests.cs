using Microsoft.Extensions.DependencyInjection;

using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Services;

namespace OpenScrape.App.Tests;

[TestFixture]
public class PostflopContextHolderTests
{
    [Test]
    public void Current_TrasConstruccion_EsNewHand()
    {
        var holder = new PostflopContextHolder();

        Assert.That(holder.Current, Is.EqualTo(PostflopGameContext.NewHand()));
    }

    [Test]
    public void Update_ReemplazaCurrentConResultadoDeUpdater()
    {
        var holder = new PostflopContextHolder();

        holder.Update(c => c with { HeroBetFlop = true });

        Assert.That(holder.Current.HeroBetFlop, Is.True);
    }

    [Test]
    public void Update_MultiplesLlamadas_ComponenEstado()
    {
        var holder = new PostflopContextHolder();

        holder.Update(c => c with { HeroBetFlop = true });
        holder.Update(c => c with { VillainBetTurn = true });

        Assert.That(holder.Current.HeroBetFlop, Is.True);
        Assert.That(holder.Current.VillainBetTurn, Is.True);
    }

    [Test]
    public void StartNewHand_ResetaContext()
    {
        var holder = new PostflopContextHolder();
        holder.Update(c => c with { HeroBetFlop = true, HeroStackPreRebuy = 100m });

        holder.StartNewHand();

        Assert.That(holder.Current.HeroBetFlop, Is.False);
        Assert.That(holder.Current.HeroStackPreRebuy, Is.EqualTo(0m));
    }

    [Test]
    public void Update_UpdaterNull_LanzaArgumentNullException()
    {
        var holder = new PostflopContextHolder();

        Assert.Throws<ArgumentNullException>(() => holder.Update(null!));
    }

    [Test]
    public void DI_MismoScope_DevuelveMismaInstancia()
    {
        var services = new ServiceCollection();
        services.AddScoped<PostflopContextHolder>();
        services.AddScoped<IPostflopContextHolder>(sp => sp.GetRequiredService<PostflopContextHolder>());
        using var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var a = scope.ServiceProvider.GetRequiredService<IPostflopContextHolder>();
        var b = scope.ServiceProvider.GetRequiredService<IPostflopContextHolder>();

        Assert.That(a, Is.SameAs(b));
    }

    [Test]
    public void DI_ScopesDistintos_DevuelvenInstanciasDistintas()
    {
        var services = new ServiceCollection();
        services.AddScoped<PostflopContextHolder>();
        services.AddScoped<IPostflopContextHolder>(sp => sp.GetRequiredService<PostflopContextHolder>());
        using var provider = services.BuildServiceProvider();

        IPostflopContextHolder a;
        IPostflopContextHolder b;
        using (var scope1 = provider.CreateScope())
            a = scope1.ServiceProvider.GetRequiredService<IPostflopContextHolder>();
        using (var scope2 = provider.CreateScope())
            b = scope2.ServiceProvider.GetRequiredService<IPostflopContextHolder>();

        Assert.That(a, Is.Not.SameAs(b));
    }
}
