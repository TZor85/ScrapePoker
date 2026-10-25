namespace OpenScrape.App.Tests;

/// <summary>
/// La caché de equity tiene que estar acotada por construcción: cuando se llena
/// desaloja las entradas más viejas, no vacía todo. Un Clear() total tira el
/// trabajo de todas las manos anteriores y vuelve a pagar Monte Carlo en cada
/// mano nueva, que es justo lo que la caché existe para evitar.
/// </summary>
[TestFixture]
public class EquityCacheTests
{
    [Test]
    public void AlLlegarAlTope_DesalojaLasMasViejasYRespetaElLimite()
    {
        var cache = new OpenScrape.DecisionMaker.Postflop.EquityCache(capacity: 3);

        cache.Set("a", 1);
        cache.Set("b", 2);
        cache.Set("c", 3);
        cache.Set("d", 4);

        Assert.That(cache.Count, Is.EqualTo(3));
        Assert.That(cache.TryGet("a", out _), Is.False, "la entrada más vieja tenía que salir");
        Assert.That(cache.TryGet("d", out var d), Is.True);
        Assert.That(d, Is.EqualTo(4));
    }

    [Test]
    public void VolverAGuardarUnaClaveExistente_NoDuplicaNiDesalojaDeMas()
    {
        var cache = new OpenScrape.DecisionMaker.Postflop.EquityCache(capacity: 2);

        cache.Set("a", 1);
        cache.Set("a", 10);
        cache.Set("b", 2);

        Assert.That(cache.Count, Is.EqualTo(2));
        Assert.That(cache.TryGet("a", out var a), Is.True);
        Assert.That(a, Is.EqualTo(10));
    }

    [Test]
    public void Get_SobreUnaClaveInexistente_DevuelveFalseSinAgregar()
    {
        var cache = new OpenScrape.DecisionMaker.Postflop.EquityCache(capacity: 2);

        Assert.That(cache.TryGet("nada", out _), Is.False);
        Assert.That(cache.Count, Is.Zero);
    }
}
