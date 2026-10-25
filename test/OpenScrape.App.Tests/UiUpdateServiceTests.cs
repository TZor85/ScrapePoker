using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Contracts;
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
            Name = "P1",
            Alias = "Player1",
            Active = true,
            Bet = 20m,
            Stack = 500m
        });
        return state;
    }

    private ResponseAction CreateResponseAction()
    {
        return new ResponseAction { Action = "Call", HandSituation = HandSituation.OpenRaise };
    }

    private static DecisionResult CreateDecision() => new()
    {
        Action = "Call",
        Equity = 55.0,
        PotOdds = 25.0
    };

    [Test]
    public void Attach_WithControls_UpdateResultsDoesNotThrow()
    {
        var svc = CreateService();
        svc.Attach(CreateControls());
        Assert.DoesNotThrow(() =>
            svc.UpdateResults(CreateState(), CreateDecision(),
                CreateResponseAction(), "1", "T"));
    }

    [Test]
    public void UpdateResults_WhenDetached_DoesNotThrow()
    {
        var svc = CreateService();
        svc.Attach(CreateControls());
        svc.Detach();
        Assert.DoesNotThrow(() =>
            svc.UpdateResults(CreateState(), CreateDecision(),
                CreateResponseAction(), "1", "T"));
    }

    [Test]
    public void UpdateResults_NeverAttached_DoesNotThrow()
    {
        var svc = CreateService();
        Assert.DoesNotThrow(() =>
            svc.UpdateResults(CreateState(), CreateDecision(),
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
        svc.UpdateResults(CreateState(), CreateDecision(), resp, "1", "T");
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
        svc.UpdateResults(st, CreateDecision(), CreateResponseAction(), "1", "T");
        Assert.That(ctrl.LbPot.Text, Is.EqualTo("250"));
    }

    [Test]
    public void UpdateResults_SetsTableHandAndTableName()
    {
        var svc = CreateService();
        var ctrl = CreateControls();
        svc.Attach(ctrl);
        svc.UpdateResults(CreateState(), CreateDecision(),
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

internal sealed class FakeGameCoordinator : IGameCoordinator
{
    public (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(
        PlayerGameState state, decimal villainBet, bool isHeadsUp, HandSituation situation)
        => (false, HandSituation.None);

    public bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand,
        ref string previousDealer, ref string previousSB, ref string previousBB)
        => false;

    public Task<GameDecisionResult> DetermineFlopAction(PlayerGameState state, CancellationToken cancellationToken = default)
        => Task.FromResult(new GameDecisionResult(string.Empty, string.Empty, new DecisionResult { Action = string.Empty }));

    public Task<GameDecisionResult> DetermineTurnAction(PlayerGameState state, CancellationToken cancellationToken = default)
        => Task.FromResult(new GameDecisionResult(string.Empty, string.Empty, new DecisionResult { Action = string.Empty }));

    public Task<GameDecisionResult> DetermineRiverAction(PlayerGameState state, CancellationToken cancellationToken = default)
        => Task.FromResult(new GameDecisionResult(string.Empty, string.Empty, new DecisionResult { Action = string.Empty }));

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

    public void TrackVillainPostflopAction(PlayerGameState state, decimal villainBet,
        bool isPreflopAggressor, bool? heroIsInPosition = null)
    { }
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
    public void UpdateBoardTexture(string? c, double w) { }
    public void UpdateStreetIndicator(string s) { }
    public void UpdateFoldEquity(double f) { }
    public void UpdateEVWithFoldEquity(double e) { }
    public void UpdateSuggestedBetSize(double? b) { }
    public void UpdateTableName(string n) { }
    public void UpdateWithDecision(DecisionResult d, string s) { }
}

internal sealed class UiTestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    { }
}

#endregion
