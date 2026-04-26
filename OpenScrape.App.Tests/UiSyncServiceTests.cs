using System.ComponentModel;
using System.Drawing;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Configuration;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del UiSyncService. Usa fakes de ISynchronizeInvoke (que invoca
/// directamente en el mismo thread) y de IFrmOverlay para verificar la
/// proyección de GameLoopResult a la UI.
/// </summary>
[TestFixture]
public class UiSyncServiceTests
{
    private sealed class FakeSyncTarget : ISynchronizeInvoke
    {
        public bool InvokeRequired => false;

        public IAsyncResult BeginInvoke(Delegate method, object?[]? args)
        {
            method.DynamicInvoke(args);
            return new FakeAsyncResult();
        }

        public object? EndInvoke(IAsyncResult result) => null;

        public object? Invoke(Delegate method, object?[]? args)
            => method.DynamicInvoke(args);

        private sealed class FakeAsyncResult : IAsyncResult
        {
            public object? AsyncState => null;
            public System.Threading.WaitHandle AsyncWaitHandle => new System.Threading.ManualResetEvent(true);
            public bool CompletedSynchronously => true;
            public bool IsCompleted => true;
        }
    }

    private sealed class FakeOverlay : IFrmOverlay
    {
        public List<string> Actions { get; } = new();
        public List<string> Equities { get; } = new();
        public List<string> PotOdds { get; } = new();
        public List<string> Streets { get; } = new();
        public int ClearCount { get; private set; }

        public void UpdateAction(string action) => Actions.Add(action);
        public void UpdatePotOddsPercentage(string potOdds) => PotOdds.Add(potOdds);
        public void UpdateEquityPercentage(string equity) => Equities.Add(equity);
        public void UpdateStreetPhase(string phase) { }
        public void UpdateSituacion(string situacion) { }
        public void UpdateHandStrength(HandRank handRank, KickerStrength kicker, bool hasComboDraw) { }
        public void UpdateBoardTexture(BoardTextureCategory? category, double wetnessScore) { }
        public void UpdateStreetIndicator(string street) => Streets.Add(street);
        public void UpdateFoldEquity(double foldEquity) { }
        public void UpdateEVWithFoldEquity(double ev) { }
        public void UpdateSuggestedBetSize(double? betSize) { }
        public void UpdateTableName(string tableName) { }
        public void UpdateWithCalculationResult(PokerCalculationResult result) { }
        public void ClearAll() => ClearCount++;
    }

    private static GameLoopCoordinator CreateCoord(int intervalMs = 999999)
    {
        // Intervalo absurdamente grande: solo nos interesa emitir manualmente via evento.
        var options = Options.Create(new GameLoopOptions { CaptureIntervalMs = intervalMs, StopTimeoutMs = 200 });
        return new GameLoopCoordinator(NullLogger<GameLoopCoordinator>.Instance, options);
    }

    [Test]
    public async Task Attach_ConResultadoDecision_ActualizaOverlay()
    {
        using var ui = new UiSyncService(NullLogger<UiSyncService>.Instance);
        await using var coord = CreateCoord();
        var overlay = new FakeOverlay();
        ui.Attach(coord, new FakeSyncTarget(), overlay, null);

        var result = new GameLoopResult
        {
            Street = BoardPosition.Flop,
            RecommendedAction = "Bet 1/2",
            DecisionResult = new DecisionResult
            {
                RecommendedAction = "Bet 1/2",
                EquityPercent = 65.3,
                Reason = "flop value",
                BoardTexture = "Dry",
                PotOddsPercent = 20.0,
            },
        };

        RaiseResultReady(coord, result);

        Assert.That(overlay.Actions, Has.Exactly(1).EqualTo("Bet 1/2"));
        Assert.That(overlay.Equities, Has.Exactly(1).EqualTo("65,3%").Or.Exactly(1).EqualTo("65.3%"));
        Assert.That(overlay.Streets, Has.Exactly(1).EqualTo("Flop"));
    }

    [Test]
    public async Task Attach_ResultadoEmpty_NoActualizaOverlay()
    {
        using var ui = new UiSyncService(NullLogger<UiSyncService>.Instance);
        await using var coord = CreateCoord();
        var overlay = new FakeOverlay();
        ui.Attach(coord, new FakeSyncTarget(), overlay, null);

        RaiseResultReady(coord, new GameLoopResult { Empty = true });

        Assert.That(overlay.Actions, Is.Empty);
        Assert.That(overlay.Equities, Is.Empty);
        Assert.That(overlay.Streets, Is.Empty);
    }

    [Test]
    public async Task Attach_LlamadaDoble_Lanza()
    {
        using var ui = new UiSyncService(NullLogger<UiSyncService>.Instance);
        await using var coord = CreateCoord();
        ui.Attach(coord, new FakeSyncTarget(), new FakeOverlay(), null);

        Assert.Throws<InvalidOperationException>(
            () => ui.Attach(coord, new FakeSyncTarget(), new FakeOverlay(), null));
    }

    [Test]
    public async Task Detach_DesuscribeYPuedeReattach()
    {
        using var ui = new UiSyncService(NullLogger<UiSyncService>.Instance);
        await using var coord = CreateCoord();
        var overlay = new FakeOverlay();

        ui.Attach(coord, new FakeSyncTarget(), overlay, null);
        ui.Detach();

        RaiseResultReady(coord, new GameLoopResult
        {
            RecommendedAction = "Bet 1/2",
            DecisionResult = new DecisionResult
            {
                RecommendedAction = "Bet 1/2",
                EquityPercent = 50,
                Reason = "x"
            },
        });
        Assert.That(overlay.Actions, Is.Empty, "Tras Detach, el overlay no debe recibir actualizaciones");

        // Reattach funcional
        ui.Attach(coord, new FakeSyncTarget(), overlay, null);
        RaiseResultReady(coord, new GameLoopResult
        {
            RecommendedAction = "Call",
            DecisionResult = new DecisionResult
            {
                RecommendedAction = "Call",
                EquityPercent = 50,
                Reason = "x"
            },
        });
        Assert.That(overlay.Actions, Has.Exactly(1).EqualTo("Call"));
    }

    [Test]
    public async Task Detach_SinAttach_NoLanza()
    {
        using var ui = new UiSyncService(NullLogger<UiSyncService>.Instance);
        Assert.DoesNotThrow(() => ui.Detach());
    }

    [Test]
    public async Task Dispose_DesuscribeSinError()
    {
        var ui = new UiSyncService(NullLogger<UiSyncService>.Instance);
        await using var coord = CreateCoord();
        ui.Attach(coord, new FakeSyncTarget(), new FakeOverlay(), null);

        ui.Dispose();
        ui.Dispose(); // idempotente

        Assert.Pass();
    }

    [Test]
    public async Task NullUiSyncService_IgnoraTodasLasLlamadas()
    {
        using var ui = new NullUiSyncService();
        await using var coord = CreateCoord();
        var overlay = new FakeOverlay();

        Assert.DoesNotThrow(() => ui.Attach(coord, new FakeSyncTarget(), overlay, null));
        Assert.DoesNotThrow(() => ui.Detach());
    }

    // Invoca el evento ResultReady del coordinator mediante reflection porque
    // la event add/remove son internas y el test no necesita arrancar el loop.
    private static void RaiseResultReady(GameLoopCoordinator coord, GameLoopResult result)
    {
        var field = typeof(GameLoopCoordinator).GetField(
            "ResultReady",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var handler = (EventHandler<GameLoopResult>?)field?.GetValue(coord);
        handler?.Invoke(coord, result);
    }
}
