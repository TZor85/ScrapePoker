using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class UiUpdateServiceTests
{
    private UiUpdateService CreateService(Func<decimal>? bigBlindProvider = null)
    {
        return new UiUpdateService(
            new FakeActionFormatter(),
            new FakePokerCalculator(),
            bigBlindProvider ?? (() => 0.50m),
            new GameLoopStateMachine(new UiTestLogger<GameLoopStateMachine>()),
            new ImageCropperService(),
            new FakeGameCoordinator());
    }

    private UiUpdateControls CreateControls()
    {
        return new UiUpdateControls(
            new TextBox(),
            new PictureBox(), new PictureBox(),
            new PictureBox(), new PictureBox(), new PictureBox(), new PictureBox(), new PictureBox(),
            new Label(), new Label(), new Label(), new Label(), new Label(),
            new Label(), new Label(), new Label(), new Label(), new Label(),
            new Label(), new Label(),
            new PictureBox(), new PictureBox(), new PictureBox(), new PictureBox(), new PictureBox(), new PictureBox(),
            new Label(), new Label(), new Label(), new Label());
    }

    private PlayerGameState CreateState()
    {
        var state = new PlayerGameState
        {
            HandSituation = HandSituation.None,
            PotSize = 100m,
            HeroStack = 1000m,
            CurrentBet = 50m,
            Position = TablePosition.Button,
            IsDealer = true,
            BoardCards = new List<BoardData>()
        };
        state.Players.Add(new Player
        {
            Name = "P1", Alias = "Player1", Active = true,
            Bet = 20m, Stack = 500m
        });
        return state;
    }

    private ResponseAction CreateResponseAction()
    {
        return new ResponseAction { Action = "Call", HandSituation = HandSituation.OpenRaise };
    }

    [Test]
    public void Attach_WithControls_UpdateResultsDoesNotThrow()
    {
        var svc = CreateService();
        svc.Attach(CreateControls());
        Assert.DoesNotThrow(() =>
            svc.UpdateResults(CreateState(), new PokerCalculationResult(),
                CreateResponseAction(), "1", "T"));
    }

    [Test]
    public void UpdateResults_WhenDetached_DoesNotThrow()
    {
        var svc = CreateService();
        svc.Attach(CreateControls());
        svc.Detach();
        Assert.DoesNotThrow(() =>
            svc.UpdateResults(CreateState(), new PokerCalculationResult(),
                CreateResponseAction(), "1", "T"));
    }

    [Test]
    public void UpdateResults_NeverAttached_DoesNotThrow()
    {
        var svc = CreateService();
        Assert.DoesNotThrow(() =>
            svc.UpdateResults(CreateState(), new PokerCalculationResult(),
                CreateResponseAction(), "1", "T"));
    }

    [Test]
    public void UpdateResults_SetsLbAction()
    {
        var svc = CreateService();
        var ctrl = CreateControls();
        svc.Attach(ctrl);
        var resp = CreateResponseAction();
        resp.Action = "Raise 3bb";
        svc.UpdateResults(CreateState(), new PokerCalculationResult(), resp, "1", "T");
        Assert.That(ctrl.LbAction.Text, Is.EqualTo("Raise 3bb"));
    }

    [Test]
    public void UpdateResults_SetsLbPot()
    {
        var svc = CreateService();
        var ctrl = CreateControls();
        svc.Attach(ctrl);
        var st = CreateState();
        st.PotSize = 250m;
        svc.UpdateResults(st, new PokerCalculationResult(), CreateResponseAction(), "1", "T");
        Assert.That(ctrl.LbPot.Text, Is.EqualTo("250"));
    }

    [Test]
    public void UpdateResults_SetsTableHandAndTableName()
    {
        var svc = CreateService();
        var ctrl = CreateControls();
        svc.Attach(ctrl);
        svc.UpdateResults(CreateState(), new PokerCalculationResult(),
            CreateResponseAction(), "H001", "Mesa");
        Assert.Multiple(() =>
        {
            Assert.That(ctrl.LbTableHand.Text, Is.EqualTo("H001"));
            Assert.That(ctrl.LbTableName.Text, Is.EqualTo("Mesa"));
        });
    }

    [Test]
    public void SetCardList_DoesNotThrow()
    {
        var svc = CreateService();
        Assert.DoesNotThrow(() => svc.SetCardList(new List<Card>()));
    }

    [Test]
    public void SetOverlay_DoesNotThrow()
    {
        var svc = CreateService();
        Assert.DoesNotThrow(() => svc.SetOverlay(new FakeOverlay()));
    }
}

#region Test Doubles

internal sealed class FakeActionFormatter : IActionFormatter
{
    public string EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind)
        => action;
}

internal sealed class FakePokerCalculator : IPokerCalculator
{
    public PokerCalculationResult Calculate(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
        decimal currentPotSize, decimal betToCall, int numOpponents = 1, int? monteCarloIterations = null,
        bool isInPosition = false, decimal heroStack = 0, decimal villainStack = 0,
        string? handSituation = null, TablePosition villainPosition = TablePosition.None,
        OpponentProfile? opponentProfile = null)
        => new() { EquityPercentage = 50.0, PotOddsPercentage = 25.0, ShouldCall = true };
}

internal sealed class FakeGameCoordinator : IGameCoordinator
{
    public PokerCalculationResult? FlopResult { get; set; }
    public PokerCalculationResult? TurnResult { get; set; }
    public PokerCalculationResult? RiverResult { get; set; }
    public TurnBoardTexture TurnBoardTexture { get; set; }
    public RiverBoardTexture RiverBoardTexture { get; set; }

    public string AdjustBetSize(string action, decimal heroStack, decimal potSize,
        int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition)
        => action;

    public BoardChangeResult AnalyzeBoardChange(List<BoardData> board, int currentStreet)
        => BoardChangeResult.Safe;

    public TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> board)
        => TurnBoardTexture.Dry;

    public RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> board)
        => RiverBoardTexture.Dry;

    public (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(
        PlayerGameState state, decimal villainBet, bool isHeadsUp, HandSituation situation)
        => (false, HandSituation.None);

    public bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand,
        ref string previousDealer, ref string previousSB, ref string previousBB)
        => false;

    public GameDecisionResult DetermineFlopAction(PlayerGameState state)
        => new(string.Empty, string.Empty);

    public GameDecisionResult DetermineTurnAction(PlayerGameState state)
        => new(string.Empty, string.Empty);

    public GameDecisionResult DetermineRiverAction(PlayerGameState state)
        => new(string.Empty, string.Empty);

    public string FormatCardsForLog(PlayerGameState state, BoardPosition position)
        => string.Empty;

    public OpponentProfile? GetActiveVillainProfile(PlayerGameState state) => null;

    public string GetActiveVillainId(PlayerGameState state) => string.Empty;

    public BetSizeCategory GetOpponentBetSize(decimal potSize, decimal currentBet)
        => BetSizeCategory.NoBet;

    public decimal GetVillainStack(PlayerGameState state) => 500m;

    public OpponentType GetVillainType(PlayerGameState state, bool? isHeadsUp = null)
        => OpponentType.Unknown;

    public bool HeroBlocksTopBoardCard(PlayerGameState state) => false;

    public void ResetContext() { }
    public void SetFlopResult(PokerCalculationResult result) { }
    public void SetTurnResult(PokerCalculationResult result) { }
    public void SetRiverResult(PokerCalculationResult result) { }

    public void TrackVillainPostflopAction(PlayerGameState state, decimal villainBet,
        bool isPreflopAggressor, bool? heroIsInPosition = null) { }
}

internal sealed class FakeOverlay : IFrmOverlay
{
    public void ClearAll() { }
    public void UpdateAction(string a) { }
    public void UpdatePotOddsPercentage(string p) { }
    public void UpdateEquityPercentage(string e) { }
    public void UpdateStreetPhase(string p) { }
    public void UpdateSituacion(string s) { }
    public void UpdateHandStrength(HandRank r, KickerStrength k, bool c) { }
    public void UpdateBoardTexture(BoardTextureCategory? c, double w) { }
    public void UpdateStreetIndicator(string s) { }
    public void UpdateFoldEquity(double f) { }
    public void UpdateEVWithFoldEquity(double e) { }
    public void UpdateSuggestedBetSize(double? b) { }
    public void UpdateTableName(string n) { }
    public void UpdateWithCalculationResult(PokerCalculationResult r) { }
}

internal sealed class UiTestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

#endregion
