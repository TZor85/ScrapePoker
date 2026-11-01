using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Algorithms;

/// <summary>
/// La enumeración exacta del river/turn sólo sabe comparar contra un rival. Cuando
/// la mesa es multiway hay que usar el simulador, que sí modela a todos; si no, la
/// equity de un bote de tres jugadores sale calculada como si fuera heads-up.
/// </summary>
[TestFixture]
public class MonteCarloMultiwayRiverTests
{
    [Test]
    public void River_Multiway_BajaLaEquityRespectoDeHeadsUp()
    {
        var simulator = new MonteCarloSimulator(42);

        var headsUp = simulator.CalculateEquity(Hand(), Board(), numOpponents: 1, iterations: 20_000);
        var multiway = simulator.CalculateEquity(Hand(), Board(), numOpponents: 2, iterations: 20_000);

        Assert.Multiple(() =>
        {
            Assert.That(headsUp.Equity, Is.GreaterThan(multiway.Equity),
                "Con dos rivales la equity de las ases tiene que bajar.");
            Assert.That(multiway.Equity, Is.LessThan(0.95),
                "Un bote de tres jugadores no puede devolver la equity de uno contra uno.");
        });
    }

    [Test]
    public void River_HeadsUp_SigueCalculandoseDeFormaExacta()
    {
        var simulator = new MonteCarloSimulator(42);

        var pocasIteraciones = simulator.CalculateEquity(Hand(), Board(), numOpponents: 1, iterations: 100);
        var muchasIteraciones = simulator.CalculateEquity(Hand(), Board(), numOpponents: 1, iterations: 50_000);

        Assert.That(pocasIteraciones.Equity, Is.EqualTo(muchasIteraciones.Equity),
            "Heads-up el river se enumera exacto: las iteraciones no pueden cambiar el resultado.");
    }

    private static List<CardDataOuts> Hand() =>
    [
        new(Suit.Spades, Rank.Ace),
        new(Suit.Diamonds, Rank.Ace)
    ];

    private static List<CardDataOuts> Board() =>
    [
        new(Suit.Spades, Rank.King),
        new(Suit.Clubs, Rank.Seven),
        new(Suit.Diamonds, Rank.Two),
        new(Suit.Hearts, Rank.Nine),
        new(Suit.Spades, Rank.Three)
    ];
}
