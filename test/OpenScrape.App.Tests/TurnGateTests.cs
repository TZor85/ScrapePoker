using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TurnGateTests
{
    [Test]
    public void Evaluate_ColorEnRango_SinLatch_DevuelveCaptureTurn()
    {
        var gate = new TurnGate();

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.True);
            Assert.That(decision.ShouldCaptureFlop, Is.False);
            Assert.That(gate.IsLatched, Is.True);
        });
    }

    [Test]
    public void Evaluate_ColorEnRangoConLatch_NoDevuelveCaptureTurn()
    {
        var gate = new TurnGate();
        gate.ForceLatched(true);

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.False);
            Assert.That(gate.IsLatched, Is.True);
        });
    }

    [Test]
    public void Evaluate_ColorFueraDeRango_LimpiaElLatch()
    {
        var gate = new TurnGate();
        gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        gate.Evaluate(isActionColorInRange: false, isFlopVisible: false);
        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.True, "vuelve a detectar al reentrar en rango");
            Assert.That(gate.IsLatched, Is.True);
        });
    }

    [Test]
    public void Evaluate_FlopVisibleYTurnDetectado_DevuelveCaptureFlop()
    {
        var gate = new TurnGate();

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: true);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.True);
            Assert.That(decision.ShouldCaptureFlop, Is.True);
        });
    }

    [Test]
    public void Evaluate_FlopVisiblePeroTurnLatcheado_NoDevuelveCaptureFlop()
    {
        var gate = new TurnGate();
        gate.ForceLatched(true);

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: true);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.False);
            Assert.That(decision.ShouldCaptureFlop, Is.False);
        });
    }

    [Test]
    public void Reset_LimpiaElLatch_YPermiteVolverADetectar()
    {
        var gate = new TurnGate();
        gate.ForceLatched(true);

        gate.Reset();
        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(gate.IsLatched, Is.True);
            Assert.That(decision.ShouldCaptureTurn, Is.True);
        });
    }
}
