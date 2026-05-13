using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

[TestFixture]
public class MetricsCollectorTests
{
    private IMetricsCollector _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
    }

    [Test]
    public void Record_UnaMuestra_ApareceEnSesion()
    {
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));
        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session.ContainsKey("OCR.Cards"), Is.True);
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(1));
    }

    [Test]
    public void Measure_ConUsing_RegistraMuestraAlDispose()
    {
        using (_ = _sut.Measure("OCR.Cards"))
        {
            Thread.Sleep(5);
        }
        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(1));
        Assert.That(snap.Session["OCR.Cards"].MaxMs, Is.GreaterThan(0));
    }

    [Test]
    public void Record_MultiplesThreads_NoPierdeMuestras()
    {
        const int iterations = 1000;
        Parallel.For(0, iterations, i =>
        {
            _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(1));
        });
        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(iterations));
    }

    [Test]
    public void StartHand_YRecord_SeAcumulaEnLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));
        var snap = _sut.SnapshotSession();
        Assert.That(snap.CurrentHandId, Is.EqualTo("hand-1"));
        Assert.That(snap.LastHand["OCR.Cards"].Count, Is.EqualTo(1));
    }

    [Test]
    public void EndHand_DevuelveAgregadoYLimpiaLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(20));

        var agg = _sut.EndHand();

        Assert.That(agg, Is.Not.Null);
        Assert.That(agg!.HandId, Is.EqualTo("hand-1"));
        Assert.That(agg.Phases["OCR.Cards"].Count, Is.EqualTo(2));

        var snap = _sut.SnapshotSession();
        Assert.That(snap.LastHand.ContainsKey("OCR.Cards"), Is.False,
            "last-hand debe haberse reseteado tras EndHand");
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(2),
            "sesión debe acumular las dos muestras");
    }

    [Test]
    public void StartHand_SinEndHandPrevio_DescartaLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));

        _sut.StartHand("hand-2");

        var snap = _sut.SnapshotSession();
        Assert.That(snap.CurrentHandId, Is.EqualTo("hand-2"));
        Assert.That(snap.LastHand.ContainsKey("OCR.Cards"), Is.False);
    }

    [Test]
    public void RecordSessionOnly_NoApareceEnLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.RecordSessionOnly("Persistence.SaveHand", TimeSpan.FromMilliseconds(50));

        var snap = _sut.SnapshotSession();
        Assert.That(snap.LastHand.ContainsKey("Persistence.SaveHand"), Is.False);
        Assert.That(snap.Session["Persistence.SaveHand"].Count, Is.EqualTo(1));

        var agg = _sut.EndHand();
        Assert.That(agg!.Phases.ContainsKey("Persistence.SaveHand"), Is.False,
            "RecordSessionOnly no debe persistirse en TelemetryAggregate");
    }

    [Test]
    public void ResetSession_LimpiaSesionYLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));

        _sut.ResetSession();

        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session.Count, Is.EqualTo(0));
        Assert.That(snap.LastHand.Count, Is.EqualTo(0));
    }

    [Test]
    public void EndHand_SinStartHand_DevuelveNull()
    {
        var agg = _sut.EndHand();
        Assert.That(agg, Is.Null);
    }
}
