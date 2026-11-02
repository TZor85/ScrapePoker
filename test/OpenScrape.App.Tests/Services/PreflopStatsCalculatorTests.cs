using OpenScrape.App.Services;
using OpenScrape.Contracts;

namespace OpenScrape.App.Tests.Services;

/// <summary>
/// El resumen preflop tiene que reportar los números que usó el motor. Cuando el
/// cliente recalculaba por su cuenta (equity + ajuste por posición, pot odds desde
/// la apuesta máxima) podía mostrar "Should Call: True" mientras la tabla decía fold.
/// </summary>
[TestFixture]
public class PreflopStatsCalculatorTests
{
    [Test]
    public void Resolve_ConDecision_UsaLaEquityYElPotOddsDelMotor()
    {
        var decision = new DecisionResult { Action = "Fold", Equity = 40d, PotOdds = 45d };

        var stats = PreflopStatsCalculator.Resolve(decision, potSize: 100m, maxBet: 20m);

        Assert.Multiple(() =>
        {
            Assert.That(stats.Equity, Is.EqualTo(40d), "no debe sumar ajustes que el motor no conoce");
            Assert.That(stats.PotOdds, Is.EqualTo(45d), "debe usar el pot odds del motor, no uno recalculado");
            Assert.That(stats.ShouldCall, Is.False);
        });
    }

    [Test]
    public void Resolve_CuandoElMotorDiceCall_RespetaSuDecision()
    {
        var decision = new DecisionResult { Action = "Call", Equity = 55d, PotOdds = 25d };

        var stats = PreflopStatsCalculator.Resolve(decision, potSize: 100m, maxBet: 20m);

        Assert.That(stats.ShouldCall, Is.True);
    }

    [Test]
    public void Resolve_SinDecision_CalculaElPotOddsLocalmente()
    {
        var stats = PreflopStatsCalculator.Resolve(decision: null, potSize: 10m, maxBet: 5m);

        Assert.Multiple(() =>
        {
            Assert.That(stats.Equity, Is.EqualTo(0d));
            Assert.That(stats.PotOdds, Is.EqualTo(5m / 15m * 100m).Within(0.0001m));
            Assert.That(stats.ShouldCall, Is.False);
        });
    }

    [Test]
    public void Resolve_SinApuestaAUbicar_PotOddsCero()
    {
        var stats = PreflopStatsCalculator.Resolve(decision: null, potSize: 10m, maxBet: 0m);

        Assert.That(stats.PotOdds, Is.EqualTo(0m));
    }
}
