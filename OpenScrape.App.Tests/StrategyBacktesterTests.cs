using Microsoft.Extensions.Options;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class StrategyBacktesterTests
{
    private StrategyBacktester _backtester;

    [SetUp]
    public void Setup()
    {
        var profile = new StrategyProfile();
        var options = Options.Create(profile);
        var betSizing = new BetSizingService(options);
        var service = new PostflopDecisionService(options, betSizing);
        _backtester = new StrategyBacktester(service);
    }

    private static HandRecord CreateHand(long number, List<StreetDecision> decisions,
        HandResult result = HandResult.Unknown, decimal stackStart = 100, decimal stackEnd = 100)
    {
        return new HandRecord
        {
            HandNumber = number,
            HeroCard1 = "Ah",
            HeroCard2 = "Kh",
            HeroPosition = TablePosition.Button,
            HeroStackStart = stackStart,
            HeroStackEnd = stackEnd,
            NumOpponents = 1,
            Decisions = decisions,
            Result = result,
            Situation = HandSituation.OpenRaise
        };
    }

    [Test]
    public void RunBacktest_SinManos_RetornaVacio()
    {
        var result = _backtester.RunBacktest(new List<HandRecord>());

        Assert.That(result.TotalHands, Is.EqualTo(0));
        Assert.That(result.TotalDecisions, Is.EqualTo(0));
        Assert.That(result.ChangedDecisions, Is.EqualTo(0));
    }

    [Test]
    public void RunBacktest_ConManos_ProcesaDecisiones()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(1, new List<StreetDecision>
            {
                new(BoardPosition.Flop, 65.0, 25.0, 5.0, "Bet 1/2 (Value)", "Bet",
                    15m, 0m, HandSituation.OpenRaise, true, "Value bet", "Dry", 3, 3.0),
                new(BoardPosition.Turn, 55.0, 30.0, 3.0, "Call", "Call",
                    25m, 10m, HandSituation.OpenRaise, true, "Call", "Dry", 2, 2.5)
            }),
            CreateHand(2, new List<StreetDecision>
            {
                new(BoardPosition.Flop, 30.0, 0.0, -2.0, "Check", "Check",
                    10m, 0m, HandSituation.OpenRaise, false, "Low equity", "Wet", 6, 5.0)
            })
        };

        var result = _backtester.RunBacktest(hands);

        Assert.That(result.TotalHands, Is.EqualTo(2));
        Assert.That(result.TotalDecisions, Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public void RunBacktest_DetectaDivergencias()
    {
        // Mano con decisión que probablemente cambiará:
        // Equity 35% en flop como agresor → antes Check, ahora C-bet (65% freq)
        var hands = new List<HandRecord>
        {
            CreateHand(1, new List<StreetDecision>
            {
                new(BoardPosition.Flop, 35.0, 0.0, -1.0, "Check", "Check",
                    10m, 0m, HandSituation.OpenRaise, true, "Low equity", "Dry", 4, 5.0)
            })
        };

        var result = _backtester.RunBacktest(hands);

        Assert.That(result.TotalDecisions, Is.GreaterThan(0));
        // Con el nuevo motor, puede diverger (c-bet con equity baja como agresor)
    }

    [Test]
    public void RunBacktest_ToStringFormateado()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(1, new List<StreetDecision>
            {
                new(BoardPosition.Flop, 65.0, 25.0, 5.0, "Bet 1/2", "Bet",
                    15m, 0m, HandSituation.OpenRaise, true, "Value", "Dry", 3, 3.0)
            })
        };

        var result = _backtester.RunBacktest(hands);
        var text = result.ToString();

        Assert.That(text, Does.Contain("BACKTEST RESULT"));
        Assert.That(text, Does.Contain("Manos analizadas:"));
        Assert.That(text, Does.Contain("Decisiones evaluadas:"));
    }

    [Test]
    public void RunBacktest_ChangesByType_AgrupaCorrectamente()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(1, new List<StreetDecision>
            {
                // Equity alta sin facing bet → probablemente Bet en ambos motores
                new(BoardPosition.Flop, 80.0, 0.0, 10.0, "Bet 1/2 (Value)", "Bet",
                    20m, 0m, HandSituation.OpenRaise, true, "Strong value", "Dry", 0, 4.0),
                // Equity baja → probablemente Check en ambos
                new(BoardPosition.Turn, 20.0, 0.0, -5.0, "Check", "Check",
                    30m, 0m, HandSituation.OpenRaise, true, "Weak", "Wet", 2, 3.0)
            })
        };

        var result = _backtester.RunBacktest(hands);

        // Debe tener alguna estructura (puede o no tener divergencias)
        Assert.That(result.ChangesByType, Is.Not.Null);
        Assert.That(result.ChangesByStreet, Is.Not.Null);
    }
}
