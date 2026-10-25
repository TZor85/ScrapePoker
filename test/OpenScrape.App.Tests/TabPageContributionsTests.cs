using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Forms.Controls;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Bankroll;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TabPageContributionsTests
{
    #region HistorialTabControl Tests

    [Test]
    public void HistorialTabControl_Initialize_ConfiguraColumnas()
    {
        var control = CreateHistorialTabControl();
        var dgvSessions = new DataGridView();
        var dgvSessionHands = new DataGridView();

        control.Initialize(dgvSessions, dgvSessionHands, new Button(), new Label());

        Assert.That(dgvSessions.Columns.Count, Is.EqualTo(4));
        Assert.That(dgvSessions.Columns[0].Name, Is.EqualTo("TableName"));
        Assert.That(dgvSessions.Columns[1].Name, Is.EqualTo("StartTime"));
        Assert.That(dgvSessions.Columns[2].Name, Is.EqualTo("Duration"));
        Assert.That(dgvSessions.Columns[3].Name, Is.EqualTo("TotalHands"));
        Assert.That(dgvSessionHands.Columns.Count, Is.EqualTo(6));
        Assert.That(dgvSessionHands.Columns[0].Name, Is.EqualTo("HandNumber"));
        Assert.That(dgvSessionHands.Columns[1].Name, Is.EqualTo("Cards"));
        Assert.That(dgvSessionHands.Columns[2].Name, Is.EqualTo("Position"));
        Assert.That(dgvSessionHands.Columns[3].Name, Is.EqualTo("LastStreet"));
        Assert.That(dgvSessionHands.Columns[4].Name, Is.EqualTo("Result"));
        Assert.That(dgvSessionHands.Columns[5].Name, Is.EqualTo("ProfitLoss"));
    }

    [Test]
    public void HistorialTabControl_Initialize_ConDgvSessionsNull_LanzaArgumentNullException()
    {
        var control = CreateHistorialTabControl();
        var dgvSessionHands = new DataGridView();

        Assert.That(() => control.Initialize(null!, dgvSessionHands, new Button(), new Label()),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("dgvSessions"));
    }

    #endregion

    #region BankrollTabControl Tests

    [Test]
    public void BankrollTabControl_Initialize_ConfiguraColumnas()
    {
        var control = CreateBankrollTabControl();
        var dgv = new DataGridView();
        var tp = new TabPage();

        control.Initialize(dgv, tp,
            new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(),
            new Label());

        Assert.That(dgv.Columns.Count, Is.EqualTo(5));
        Assert.That(dgv.Columns[0].Name, Is.EqualTo("Date"));
        Assert.That(dgv.Columns[1].Name, Is.EqualTo("Hands"));
        Assert.That(dgv.Columns[2].Name, Is.EqualTo("Profit"));
        Assert.That(dgv.Columns[3].Name, Is.EqualTo("BBPer100"));
        Assert.That(dgv.Columns[4].Name, Is.EqualTo("BankrollAfter"));
    }

    [Test]
    public void BankrollTabControl_Initialize_ConDgvNull_LanzaArgumentNullException()
    {
        var control = CreateBankrollTabControl();

        Assert.That(() => control.Initialize(null!, new TabPage(),
            new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(),
            new Label()),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("dgvBankrollHistory"));
    }

    [Test]
    public void BankrollTabControl_UpdateBankrollDashboard_NoLanzaExcepcion()
    {
        var control = CreateBankrollTabControl();
        var dgv = new DataGridView();
        var tp = new TabPage();

        control.Initialize(dgv, tp,
            new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(),
            new Label());

        Assert.DoesNotThrow(() => control.UpdateBankrollDashboard());
    }

    #endregion

    #region MetricsTabControl Tests

    [Test]
    public void MetricsTabControl_Initialize_ConfiguraColumnas()
    {
        var control = CreateMetricsTabControl();
        var dgv = new DataGridView();
        var tbControl = new TabControl();
        var tabMetrics = new TabPage();

        control.Initialize(dgv, new Button(), new Label(), new Label(), new Label(),
            tbControl, tabMetrics);

        Assert.That(dgv.Columns.Count, Is.GreaterThan(0));
        Assert.That(dgv.Columns[0].Name, Is.EqualTo("Fase"));
        // Excluir la fila de nueva entrada (AllowUserToAddRows=true por defecto)
        int dataRowCount = dgv.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow);
        Assert.That(dataRowCount, Is.EqualTo(TelemetryCategories.DisplayOrder.Count));
    }

    [Test]
    public void MetricsTabControl_Initialize_ConDgvNull_LanzaArgumentNullException()
    {
        var control = CreateMetricsTabControl();

        Assert.That(() => control.Initialize(null!, new Button(), new Label(), new Label(), new Label(),
            new TabControl(), new TabPage()),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("dgvMetrics"));
    }

    #endregion

    #region Factory Methods

    private static HistorialTabControl CreateHistorialTabControl()
    {
        return new HistorialTabControl(
            new FakeGameLoggerService(),
            new TabPageTestLogger<HistorialTabControl>(),
            new FakePostflopDecisionService());
    }

    private static BankrollTabControl CreateBankrollTabControl()
    {
        return new BankrollTabControl(
            new FakeBankrollTrackerService(),
            new TabPageTestLogger<BankrollTabControl>());
    }

    private static MetricsTabControl CreateMetricsTabControl()
    {
        return new MetricsTabControl(
            new FakeMetricsCollector(),
            new TabPageTestLogger<MetricsTabControl>());
    }

    #endregion
}

#region Fake Implementations

internal sealed class FakeGameLoggerService : GameLoggerService
{
    public FakeGameLoggerService()
        : base(Marten.DocumentStore.For("host=localhost;database=fake;password=fake;username=fake"),
              new TabPageTestLogger<GameLoggerService>(),
              new FakeMetricsCollector())
    {
    }

    public override async Task<List<SessionStatsDto>> GetRecentSessionsWithStatsAsync(int count = 50)
    {
        await Task.CompletedTask;
        return new List<SessionStatsDto>();
    }

    public override async Task<List<HandRecord>> GetRecentHandsAsync(int maxHands = 500)
    {
        await Task.CompletedTask;
        return new List<HandRecord>();
    }

    public override async Task<List<HandRecord>> GetHandsForSessionAsync(string gameSessionId)
    {
        await Task.CompletedTask;
        return new List<HandRecord>();
    }
}

internal sealed class FakeBankrollTrackerService : IBankrollTrackerService
{
    public BankrollStats GetBankrollStats(int sessionCount = 100)
        => new BankrollStats
        {
            CurrentBankroll = 1000m,
            PeakBankroll = 1200m,
            MaxDrawdownPercent = 5.0,
            WinRateBB100 = 3.5,
            StdDeviation = 15.0,
            TotalSessions = 50,
            WinningSessions = 30,
            TotalHands = 5000,
            RiskOfRuin = 0.05,
            AverageSessionProfit = 10.0m,
            LimitRecommendation = "MANTENER",
            RiskLevel = "Green"
        };

    public void RecordSessionEnd(GameSession session) { }
    public decimal GetCurrentBankroll() => 1000m;
    public decimal GetPeakBankroll() => 1200m;
    public (double riskOfRuin, string level) CalculateRiskOfRuin() => (0.05, "Green");
    public string GetLimitRecommendation() => "MANTENER";
    public void SetInitialBankroll(decimal amount) { }

    public List<BankrollHistoryItem> GetHistory(int count = 50)
        => new List<BankrollHistoryItem>
        {
            new BankrollHistoryItem
            {
                Date = DateTime.Today, Hands = 100,
                Profit = 25.50m, BBPer100 = 5.0, BankrollAfter = 1025.50m
            }
        };
}

internal sealed class FakeMetricsCollector : IMetricsCollector
{
    public ScopedMeasurement Measure(string category) => default;
    public void Record(string category, TimeSpan elapsed) { }
    public ScopedMeasurement MeasureSessionOnly(string category) => default;
    public void RecordSessionOnly(string category, TimeSpan elapsed) { }
    public void StartHand(string handId) { }
    public TelemetryAggregate? EndHand() => null;
    public MetricsSnapshot SnapshotSession()
        => new MetricsSnapshot("hand-1",
            new Dictionary<string, CategoryStats>(),
            new Dictionary<string, CategoryStats>());
    public void ResetSession() { }
}

internal sealed class TabPageTestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

internal sealed class FakePostflopDecisionService : IPostflopDecisionService
{
    public double CalculateDangerPenalty(
        double rawEquity, BoardChangeResult boardChange,
        bool heroBlocksDangerSuit, bool isFacingBet,
        BoardPosition street = BoardPosition.Turn,
        bool heroHasNutBlocker = false,
        HandRank heroHandRank = HandRank.HighCard,
        bool heroCompletedFlush = false,
        bool heroCompletedStraight = false) => 0.0;

    public double CalculateImpliedOddsFactor(
        BoardPosition street, bool isInPosition, bool hasFlushDraw,
        decimal heroStack = 0, decimal potSize = 0, int numOpponents = 1) => 1.0;

    public double CalculateReverseImpliedOdds(
        BoardChangeResult? boardChange, HandRank heroHandRank,
        bool hasFlushDraw, BoardPosition street, bool isFacingBet,
        PairClassification pairClassification = PairClassification.None,
        bool heroBlocksDangerSuit = false) => 0.0;

    public PostflopDecisionResult DetermineAction(PostflopDecisionInput input)
        => new PostflopDecisionResult(Action: "Check");
}

#endregion
