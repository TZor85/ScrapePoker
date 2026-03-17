using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class StrategyAnalyzerServiceTests
{
    private StrategyAnalyzerService _analyzer;

    [SetUp]
    public void Setup()
    {
        _analyzer = new StrategyAnalyzerService();
    }

    private GameRound CreateRound(
        HandResult result, decimal stackStart, decimal stackEnd,
        TablePosition position = TablePosition.Button,
        HandSituation situation = HandSituation.OpenRaise,
        string sessionId = "session1",
        List<StreetDecision>? decisions = null)
    {
        return new GameRound
        {
            HandNumber = Random.Shared.Next(1, 100000),
            Timestamp = DateTime.UtcNow,
            TableName = "Test",
            HeroPosition = position,
            HeroStackStart = stackStart,
            HeroStackEnd = stackEnd,
            Result = result,
            Situation = situation,
            SessionId = sessionId,
            Decisions = decisions ?? new List<StreetDecision>()
        };
    }

    private StreetDecision CreateDecision(
        BoardPosition street, double equity, string action,
        double potOdds = 20, decimal potSize = 10, decimal betSize = 2)
    {
        return new StreetDecision(
            street, equity, potOdds, 0, action, action,
            potSize, betSize, HandSituation.OpenRaise, true);
    }

    [Test]
    public void Analyze_SinManos_DeberiaRetornarVacio()
    {
        var result = _analyzer.Analyze(new List<GameRound>());

        Assert.That(result.TotalHands, Is.EqualTo(0));
        Assert.That(result.BBPer100, Is.EqualTo(0));
    }

    [Test]
    public void Analyze_ConManos_DeberiaContarCorrectamente()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 115),
            CreateRound(HandResult.Won, 100, 108),
            CreateRound(HandResult.Lost, 100, 90),
            CreateRound(HandResult.Push, 100, 100),
            CreateRound(HandResult.Unknown, 100, 100)
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.TotalHands, Is.EqualTo(5));
        Assert.That(result.HandsWon, Is.EqualTo(2));
        Assert.That(result.HandsLost, Is.EqualTo(1));
        Assert.That(result.HandsPush, Is.EqualTo(1));
        Assert.That(result.HandsUnknown, Is.EqualTo(1));
    }

    [Test]
    public void Analyze_Profit_DeberiaCalcularCorrectamente()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 120),   // +20
            CreateRound(HandResult.Lost, 100, 85),    // -15
            CreateRound(HandResult.Won, 100, 105),    // +5
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.TotalProfit, Is.EqualTo(10m)); // +20-15+5 = 10
        Assert.That(result.BiggestWin, Is.EqualTo(20m));
        Assert.That(result.BiggestLoss, Is.EqualTo(-15m));
    }

    [Test]
    public void Analyze_BBPer100_DeberiaCalcularCorrectamente()
    {
        // 10 manos, profit = +5, BB = 0.50 → 10 BB total, BB/100 = 100
        var rounds = Enumerable.Range(0, 10)
            .Select(i => CreateRound(
                i < 5 ? HandResult.Won : HandResult.Lost,
                100, i < 5 ? 101m : 99m)) // 5 * +1 + 5 * -1 = 0
            .ToList();

        // Forzar profit conocido: 5 wins de +2, 5 losses de -1 = +5
        rounds[0] = CreateRound(HandResult.Won, 100, 102);
        rounds[1] = CreateRound(HandResult.Won, 100, 102);
        rounds[2] = CreateRound(HandResult.Won, 100, 102);
        rounds[3] = CreateRound(HandResult.Won, 100, 102);
        rounds[4] = CreateRound(HandResult.Won, 100, 102);
        // losses: 5 * -1 = -5. Total = 10 - 5 = 5
        var result = _analyzer.Analyze(rounds, bigBlind: 0.50m);

        // Profit = +5, BB = 0.50 → 10 BB, 10 manos → BB/100 = 100
        Assert.That(result.BBPer100, Is.EqualTo(100.0).Within(0.1));
    }

    [Test]
    public void Analyze_PorPosicion_DeberiaAgrupar()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 110, TablePosition.Button),
            CreateRound(HandResult.Won, 100, 105, TablePosition.Button),
            CreateRound(HandResult.Lost, 100, 90, TablePosition.BigBlind),
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.StatsByPosition.ContainsKey(TablePosition.Button), Is.True);
        Assert.That(result.StatsByPosition[TablePosition.Button].Hands, Is.EqualTo(2));
        Assert.That(result.StatsByPosition[TablePosition.Button].Won, Is.EqualTo(2));
        Assert.That(result.StatsByPosition[TablePosition.Button].Profit, Is.EqualTo(15m));

        Assert.That(result.StatsByPosition[TablePosition.BigBlind].Hands, Is.EqualTo(1));
        Assert.That(result.StatsByPosition[TablePosition.BigBlind].Profit, Is.EqualTo(-10m));
    }

    [Test]
    public void Analyze_PorStreet_DeberiaContarAcciones()
    {
        var decisions = new List<StreetDecision>
        {
            CreateDecision(BoardPosition.Flop, 65, "Bet 2/3 (Value)"),
            CreateDecision(BoardPosition.Turn, 55, "Call"),
            CreateDecision(BoardPosition.River, 70, "Bet Pot (Value)")
        };
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 120, decisions: decisions)
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.StatsByStreet[BoardPosition.Flop].Bets, Is.EqualTo(1));
        Assert.That(result.StatsByStreet[BoardPosition.Turn].Calls, Is.EqualTo(1));
        Assert.That(result.StatsByStreet[BoardPosition.River].Bets, Is.EqualTo(1));
        Assert.That(result.StatsByStreet[BoardPosition.Flop].AvgEquity, Is.EqualTo(65));
    }

    [Test]
    public void Analyze_PorSituacion_DeberiaAgrupar()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 110, situation: HandSituation.OpenRaise),
            CreateRound(HandResult.Won, 100, 105, situation: HandSituation.OpenRaise),
            CreateRound(HandResult.Lost, 100, 85, situation: HandSituation.ThreeBet),
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.StatsBySituation[HandSituation.OpenRaise].Hands, Is.EqualTo(2));
        Assert.That(result.StatsBySituation[HandSituation.OpenRaise].WinRate, Is.EqualTo(100));
        Assert.That(result.StatsBySituation[HandSituation.ThreeBet].Profit, Is.EqualTo(-15m));
    }

    [Test]
    public void Analyze_Sesiones_DeberiaAgrupar()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 110, sessionId: "s1"),
            CreateRound(HandResult.Won, 100, 105, sessionId: "s1"),
            CreateRound(HandResult.Lost, 100, 90, sessionId: "s2"),
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.Sessions.Count, Is.EqualTo(2));
        var s1 = result.Sessions.First(s => s.SessionId == "s1");
        Assert.That(s1.Hands, Is.EqualTo(2));
        Assert.That(s1.Profit, Is.EqualTo(15m));
    }

    [Test]
    public void Analyze_EquityVsOutcome_DeberiaCrearPares()
    {
        var decisions = new List<StreetDecision>
        {
            CreateDecision(BoardPosition.Turn, 70, "Bet 2/3 (Value)")
        };
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 120, decisions: decisions),
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.EquityVsOutcomes.Count, Is.EqualTo(1));
        Assert.That(result.EquityVsOutcomes[0].Equity, Is.EqualTo(70));
        Assert.That(result.EquityVsOutcomes[0].Won, Is.True);
    }

    [Test]
    public void GenerateReport_DeberiaRetornarTexto()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 115),
            CreateRound(HandResult.Lost, 100, 90),
        };

        var analysis = _analyzer.Analyze(rounds);
        var report = _analyzer.GenerateReport(analysis);

        Assert.That(report, Does.Contain("ANÁLISIS DE ESTRATEGIA"));
        Assert.That(report, Does.Contain("Manos totales:"));
        Assert.That(report, Does.Contain("BB/100:"));
        Assert.That(report, Does.Contain("Profit total:"));
    }

    [Test]
    public void Analyze_WinRate_DeberiaCalcularCorrectamente()
    {
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Won, 100, 110),
            CreateRound(HandResult.Won, 100, 105),
            CreateRound(HandResult.Lost, 100, 90),
            CreateRound(HandResult.Lost, 100, 85),
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.WinRate, Is.EqualTo(50.0));
    }

    [Test]
    public void Analyze_FoldActions_DeberiaContarCorrectamente()
    {
        var decisions = new List<StreetDecision>
        {
            CreateDecision(BoardPosition.Turn, 25, "Fold"),
        };
        var rounds = new List<GameRound>
        {
            CreateRound(HandResult.Lost, 100, 95, decisions: decisions),
        };

        var result = _analyzer.Analyze(rounds);

        Assert.That(result.StatsByStreet[BoardPosition.Turn].Folds, Is.EqualTo(1));
    }
}
