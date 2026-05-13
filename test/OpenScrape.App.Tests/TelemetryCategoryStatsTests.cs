using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TelemetryCategoryStatsTests
{
    [Test]
    public void SnapshotSession_ConMuestraUnica_CategoryStatsCoherente()
    {
        var sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        sut.Record("X", TimeSpan.FromMilliseconds(10));

        var snap = sut.SnapshotSession();
        var stats = snap.Session["X"];

        Assert.That(stats.Count, Is.EqualTo(1));
        // Con una sola muestra, p50/p95/max coinciden con el bucket que la contiene
        Assert.That(stats.P50Ms, Is.InRange(8.0, 16.0));
        Assert.That(stats.P95Ms, Is.InRange(8.0, 16.0));
        Assert.That(stats.MaxMs, Is.EqualTo(10.0).Within(0.001));
    }

    [Test]
    public void SnapshotSession_CountLargo_NoSeTrunca()
    {
        var sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        const int n = 2_000_000;
        for (int i = 0; i < n; i++)
            sut.Record("X", TimeSpan.FromMilliseconds(1));

        var snap = sut.SnapshotSession();
        Assert.That(snap.Session["X"].Count, Is.EqualTo(n));
    }

    [Test]
    public void SnapshotSession_SinMuestras_DiccionarioVacio()
    {
        var sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        var snap = sut.SnapshotSession();
        Assert.That(snap.Session, Is.Empty);
        Assert.That(snap.LastHand, Is.Empty);
        Assert.That(snap.CurrentHandId, Is.Null);
    }
}
