using OpenScrape.App.Tests.Fixtures;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// El caché de equity no puede devolver el número de otro villano: la posición y
/// los stats cambian el rango contra el que corre Monte Carlo, así que forman
/// parte de la identidad del cálculo.
/// </summary>
[TestFixture]
public class UnifiedPokerCalculatorEquityCacheTests
{
    [Test]
    public void Calculate_Preflop_MismaManoConDistintaPosicionDelVillano_NoReusaElCache()
    {
        var calculator = GameCoordinatorFixtureBuilder.CreateCalculator(StrategyProfileFixture.LoadOrCreate(), monteCarloSeed: 7);
        var hand = Mano();

        var contraButton = calculator.Calculate(hand, [], 100m, 50m,
            numOpponents: 1,
            handSituation: nameof(HandSituation.OpenRaise),
            villainPosition: TablePosition.Button);

        var contraEarly = calculator.Calculate(hand, [], 100m, 50m,
            numOpponents: 1,
            handSituation: nameof(HandSituation.OpenRaise),
            villainPosition: TablePosition.Early);

        Assert.That(contraEarly.EquityPercentage, Is.Not.EqualTo(contraButton.EquityPercentage).Within(0.0001));
    }

    [Test]
    public void Calculate_Postflop_MismaMesaConDistintoPerfilDelVillano_NoReusaElCache()
    {
        var calculator = GameCoordinatorFixtureBuilder.CreateCalculator(StrategyProfileFixture.LoadOrCreate(), monteCarloSeed: 7);
        var hand = Mano();
        var flop = Flop();

        var contraNit = calculator.Calculate(hand, flop, 100m, 50m,
            numOpponents: 1,
            handSituation: nameof(HandSituation.Call),
            villainPosition: TablePosition.Button,
            opponentProfile: new OpponentProfile
            {
                HandsPlayed = 100,
                TimesVoluntarilyPutMoneyIn = 12,
                TimesPreflopRaised = 6,
                TimesThreeBet = 1
            });

        var contraManiaco = calculator.Calculate(hand, flop, 100m, 50m,
            numOpponents: 1,
            handSituation: nameof(HandSituation.Call),
            villainPosition: TablePosition.Button,
            opponentProfile: new OpponentProfile
            {
                HandsPlayed = 100,
                TimesVoluntarilyPutMoneyIn = 85,
                TimesPreflopRaised = 60,
                TimesThreeBet = 30
            });

        Assert.That(contraManiaco.EquityPercentage, Is.Not.EqualTo(contraNit.EquityPercentage).Within(0.0001));
    }

    [Test]
    public void Calculate_MismosInputsDosVeces_DevuelveElMismoNumero()
    {
        var calculator = GameCoordinatorFixtureBuilder.CreateCalculator(StrategyProfileFixture.LoadOrCreate(), monteCarloSeed: 7);

        var primera = calculator.Calculate(Mano(), Flop(), 100m, 50m,
            numOpponents: 1,
            handSituation: nameof(HandSituation.Call),
            villainPosition: TablePosition.Button);

        var segunda = calculator.Calculate(Mano(), Flop(), 100m, 50m,
            numOpponents: 1,
            handSituation: nameof(HandSituation.Call),
            villainPosition: TablePosition.Button);

        Assert.That(segunda.EquityPercentage, Is.EqualTo(primera.EquityPercentage));
    }

    private static List<CardDataOuts> Mano() =>
    [
        new(Suit.Spades, (Rank)14),
        new(Suit.Spades, (Rank)13)
    ];

    private static List<CardDataOuts> Flop() =>
    [
        new(Suit.Hearts, (Rank)9),
        new(Suit.Clubs, (Rank)5),
        new(Suit.Diamonds, (Rank)2)
    ];
}
