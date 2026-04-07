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

    private HandRecord CreateHand(
        HandResult result, decimal stackStart, decimal stackEnd,
        TablePosition position = TablePosition.Button,
        HandSituation situation = HandSituation.OpenRaise,
        List<StreetDecision>? decisions = null)
    {
        return new HandRecord
        {
            HandNumber = Random.Shared.Next(1, 100000),
            Timestamp = DateTime.UtcNow,
            HeroPosition = position,
            HeroStackStart = stackStart,
            HeroStackEnd = stackEnd,
            Result = result,
            Situation = situation,
            Decisions = decisions ?? new List<StreetDecision>()
        };
    }

    private GameSession CreateSession(string sessionId, string tableName, List<HandRecord> hands)
    {
        var session = new GameSession
        {
            SessionId = sessionId,
            TableName = tableName,
            StartTime = hands.Min(h => h.Timestamp),
            EndTime = hands.Max(h => h.Timestamp),
            Hands = hands
        };
        // Asignar FK de cada mano al Id del GameSession
        foreach (var hand in hands)
            hand.GameSessionId = session.Id;
        return session;
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
        var result = _analyzer.Analyze(new List<HandRecord>());

        Assert.That(result.TotalHands, Is.EqualTo(0));
        Assert.That(result.BBPer100, Is.EqualTo(0));
    }

    [Test]
    public void Analyze_ConManos_DeberiaContarCorrectamente()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 115),
            CreateHand(HandResult.Won, 100, 108),
            CreateHand(HandResult.Lost, 100, 90),
            CreateHand(HandResult.Push, 100, 100),
            CreateHand(HandResult.Unknown, 100, 100)
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.TotalHands, Is.EqualTo(5));
        Assert.That(result.HandsWon, Is.EqualTo(2));
        Assert.That(result.HandsLost, Is.EqualTo(1));
        Assert.That(result.HandsPush, Is.EqualTo(1));
        Assert.That(result.HandsUnknown, Is.EqualTo(1));
    }

    [Test]
    public void Analyze_Profit_DeberiaCalcularCorrectamente()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 120),   // +20
            CreateHand(HandResult.Lost, 100, 85),    // -15
            CreateHand(HandResult.Won, 100, 105),    // +5
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.TotalProfit, Is.EqualTo(10m)); // +20-15+5 = 10
        Assert.That(result.BiggestWin, Is.EqualTo(20m));
        Assert.That(result.BiggestLoss, Is.EqualTo(-15m));
    }

    [Test]
    public void Analyze_BBPer100_DeberiaCalcularCorrectamente()
    {
        var hands = new List<HandRecord>();
        for (int i = 0; i < 5; i++)
            hands.Add(CreateHand(HandResult.Won, 100, 102));  // 5 * +2 = +10
        for (int i = 0; i < 5; i++)
            hands.Add(CreateHand(HandResult.Lost, 100, 99));  // 5 * -1 = -5

        // Profit = +5, BB = 0.50 → 10 BB, 10 manos → BB/100 = 100
        var result = _analyzer.Analyze(hands, bigBlind: 0.50m);

        Assert.That(result.BBPer100, Is.EqualTo(100.0).Within(0.1));
    }

    [Test]
    public void Analyze_PorPosicion_DeberiaAgrupar()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 110, TablePosition.Button),
            CreateHand(HandResult.Won, 100, 105, TablePosition.Button),
            CreateHand(HandResult.Lost, 100, 90, TablePosition.BigBlind),
        };

        var result = _analyzer.Analyze(hands);

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
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 120, decisions: decisions)
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.StatsByStreet[BoardPosition.Flop].Bets, Is.EqualTo(1));
        Assert.That(result.StatsByStreet[BoardPosition.Turn].Calls, Is.EqualTo(1));
        Assert.That(result.StatsByStreet[BoardPosition.River].Bets, Is.EqualTo(1));
        Assert.That(result.StatsByStreet[BoardPosition.Flop].AvgEquity, Is.EqualTo(65));
    }

    [Test]
    public void Analyze_PorSituacion_DeberiaAgrupar()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 110, situation: HandSituation.OpenRaise),
            CreateHand(HandResult.Won, 100, 105, situation: HandSituation.OpenRaise),
            CreateHand(HandResult.Lost, 100, 85, situation: HandSituation.ThreeBet),
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.StatsBySituation[HandSituation.OpenRaise].Hands, Is.EqualTo(2));
        Assert.That(result.StatsBySituation[HandSituation.OpenRaise].WinRate, Is.EqualTo(100));
        Assert.That(result.StatsBySituation[HandSituation.ThreeBet].Profit, Is.EqualTo(-15m));
    }

    [Test]
    public void AnalyzeSessions_DeberiaAgruparPorSesion()
    {
        var s1Hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 110),
            CreateHand(HandResult.Won, 100, 105),
        };
        var s2Hands = new List<HandRecord>
        {
            CreateHand(HandResult.Lost, 100, 90),
        };

        var sessions = new List<GameSession>
        {
            CreateSession("s1", "Mesa1", s1Hands),
            CreateSession("s2", "Mesa2", s2Hands),
        };

        var allHands = s1Hands.Concat(s2Hands).ToList();
        var result = _analyzer.AnalyzeSessions(sessions, allHands);

        Assert.That(result.TotalHands, Is.EqualTo(3));
        Assert.That(result.Sessions.Count, Is.EqualTo(2));
        var s1 = result.Sessions.First(s => s.SessionId == "s1");
        Assert.That(s1.Hands, Is.EqualTo(2));
        Assert.That(s1.Profit, Is.EqualTo(15m));
        Assert.That(s1.TableName, Is.EqualTo("Mesa1"));
    }

    [Test]
    public void Analyze_EquityVsOutcome_DeberiaCrearPares()
    {
        var decisions = new List<StreetDecision>
        {
            CreateDecision(BoardPosition.Turn, 70, "Bet 2/3 (Value)")
        };
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 120, decisions: decisions),
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.EquityVsOutcomes.Count, Is.EqualTo(1));
        Assert.That(result.EquityVsOutcomes[0].Equity, Is.EqualTo(70));
        Assert.That(result.EquityVsOutcomes[0].Won, Is.True);
    }

    [Test]
    public void GenerateReport_DeberiaRetornarTexto()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 115),
            CreateHand(HandResult.Lost, 100, 90),
        };

        var analysis = _analyzer.Analyze(hands);
        var report = _analyzer.GenerateReport(analysis);

        Assert.That(report, Does.Contain("ANÁLISIS DE ESTRATEGIA"));
        Assert.That(report, Does.Contain("Manos totales:"));
        Assert.That(report, Does.Contain("BB/100:"));
        Assert.That(report, Does.Contain("Profit total:"));
    }

    [Test]
    public void Analyze_WinRate_DeberiaCalcularCorrectamente()
    {
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Won, 100, 110),
            CreateHand(HandResult.Won, 100, 105),
            CreateHand(HandResult.Lost, 100, 90),
            CreateHand(HandResult.Lost, 100, 85),
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.WinRate, Is.EqualTo(50.0));
    }

    [Test]
    public void Analyze_FoldActions_DeberiaContarCorrectamente()
    {
        var decisions = new List<StreetDecision>
        {
            CreateDecision(BoardPosition.Turn, 25, "Fold"),
        };
        var hands = new List<HandRecord>
        {
            CreateHand(HandResult.Lost, 100, 95, decisions: decisions),
        };

        var result = _analyzer.Analyze(hands);

        Assert.That(result.StatsByStreet[BoardPosition.Turn].Folds, Is.EqualTo(1));
    }
}
