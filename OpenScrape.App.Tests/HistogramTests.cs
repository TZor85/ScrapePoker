using NUnit.Framework;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

[TestFixture]
public class HistogramTests
{
    [Test]
    public void Add_MuestraUnica_CuentaUno()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(10));
        Assert.That(h.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_MuestraUnica_MaxEsEsaMuestra()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(10));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.FromMilliseconds(10)));
    }

    [Test]
    public void Max_ConMultiplesMuestras_DevuelveLaMayor()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(5));
        h.Add(TimeSpan.FromMilliseconds(50));
        h.Add(TimeSpan.FromMilliseconds(10));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.FromMilliseconds(50)));
    }

    [Test]
    public void GetPercentile_MuestrasIguales_PercentilCaeEnMismoBucket()
    {
        var h = new Histogram();
        for (int i = 0; i < 1000; i++)
            h.Add(TimeSpan.FromMilliseconds(10));

        var p50 = h.GetPercentile(0.5).TotalMilliseconds;
        // Bucket logarítmico: la muestra 10ms cae en el bucket que la contiene.
        // El rango 9-12.5 cubre la posición del upper bound del bucket correspondiente.
        Assert.That(p50, Is.InRange(9.0, 12.5));
    }

    [Test]
    public void GetPercentile_DistribucionBimodal_SeparaP50YP95()
    {
        var h = new Histogram();
        for (int i = 0; i < 500; i++) h.Add(TimeSpan.FromMilliseconds(10));
        for (int i = 0; i < 500; i++) h.Add(TimeSpan.FromMilliseconds(100));

        var p50 = h.GetPercentile(0.5).TotalMilliseconds;
        var p95 = h.GetPercentile(0.95).TotalMilliseconds;

        // Con 50/50 de 10ms y 100ms, p50 cae en el bloque de 10ms (o frontera), p95 en el de 100ms
        Assert.That(p50, Is.LessThan(20.0), "p50 debería estar cerca de 10ms");
        Assert.That(p95, Is.GreaterThan(80.0), "p95 debería estar cerca de 100ms");
    }

    [Test]
    public void Add_ValorMayorQueUltimoBucket_NoDesborda()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromSeconds(60));
        Assert.That(h.Count, Is.EqualTo(1));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.FromSeconds(60)));
    }

    [Test]
    public void Add_ValorMenorQuePrimerBucket_CaeEnPrimerBucket()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromTicks(1)); // <10μs
        Assert.That(h.Count, Is.EqualTo(1));
        Assert.That(h.GetPercentile(0.5).TotalMicroseconds, Is.LessThanOrEqualTo(15));
    }

    [Test]
    public void CountCero_Percentiles_DevuelveZero()
    {
        var h = new Histogram();
        Assert.That(h.GetPercentile(0.5), Is.EqualTo(TimeSpan.Zero));
        Assert.That(h.GetPercentile(0.95), Is.EqualTo(TimeSpan.Zero));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Reset_VuelveAEstadoInicial()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(10));
        h.Add(TimeSpan.FromMilliseconds(20));
        h.Reset();
        Assert.That(h.Count, Is.EqualTo(0));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.Zero));
        Assert.That(h.GetPercentile(0.5), Is.EqualTo(TimeSpan.Zero));
    }
}
