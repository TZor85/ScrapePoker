using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenScrape.App.Tests.Fixtures;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Postflop;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests.Postflop;

/// <summary>
/// En un bote multiway sólo el agresor tiene un rango conocido: aplicar el rango
/// del 3bet a todos los rivales supone que todos llegaron igual de fuertes. Se
/// replica lo que ya hace el preflop: un rival juega con rango y el resto, aleatorio.
/// </summary>
[TestFixture]
public class UnifiedPokerCalculatorRangedOpponentsTests
{
    [Test]
    public void Calculate_PostflopMultiway_UsaElRangoEnUnSoloRival()
    {
        var simulator = new RecordingSimulator();
        var calculator = new UnifiedPokerCalculator(
            simulator,
            new OutsCalculator(Options.Create(StrategyProfileFixture.LoadOrCreate())),
            new PreflopEquityCalculator(),
            new BitHandEvaluator(),
            new BoardTextureAnalyzer(),
            Options.Create(StrategyProfileFixture.LoadOrCreate()),
            NullLogger<UnifiedPokerCalculator>.Instance);

        calculator.Calculate(
            [
                new CardDataOuts(Suit.Spades, Rank.Ace),
                new CardDataOuts(Suit.Spades, Rank.King)
            ],
            [
                new CardDataOuts(Suit.Hearts, Rank.King),
                new CardDataOuts(Suit.Clubs, Rank.Seven),
                new CardDataOuts(Suit.Diamonds, Rank.Two)
            ],
            currentPotSize: 10m,
            betToCall: 5m,
            numOpponents: 2,
            monteCarloIterations: 100,
            isInPosition: true,
            heroStack: 90m,
            villainStack: 95m,
            handSituation: HandSituation.OpenRaise.ToString(),
            villainPosition: TablePosition.BigBlind);

        Assert.Multiple(() =>
        {
            Assert.That(simulator.OpponentsReceived, Is.EqualTo(2),
                "El simulador tiene que repartir contra los dos rivales de la mesa.");
            Assert.That(simulator.RangedOpponentsReceived, Is.EqualTo(1),
                "Sólo el rival agresor tiene rango conocido; el resto juega aleatorio.");
        });
    }

    private sealed class RecordingSimulator : IMonteCarloSimulator
    {
        public int OpponentsReceived { get; private set; }

        public int RangedOpponentsReceived { get; private set; }

        public MonteCarloSimulator.EquityResult CalculateEquity(
            List<CardDataOuts> myCards,
            List<CardDataOuts> communityCards,
            int numOpponents,
            int? iterations = null,
            VillainRange? villainRange = null,
            int? randomSeed = null,
            int rangedOpponents = int.MaxValue)
        {
            OpponentsReceived = numOpponents;
            RangedOpponentsReceived = rangedOpponents;
            return new MonteCarloSimulator.EquityResult();
        }
    }
}
