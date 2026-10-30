using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using OpenScrape.App.Aplication;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.Domain.Enums;
using DomainRegion = OpenScrape.Domain.ValueObjects.Region;
using OpenScrape.DecisionMaker;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del HandProcessor (Fase 4 del refactor de FrmMain).
/// Cubre escenarios RF4: pipeline preflop, postflop, captura nula, cancelación.
/// </summary>
[TestFixture]
public class HandProcessorTests
{
    #region ExecuteAsync — Capture Null

    [Test]
    public async Task ExecuteAsync_ReturnsEmpty_When_CaptureReturnsNull()
    {
        // Arrange: capture returns null
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(null);

        var processor = CreateProcessor(captureService: captureService);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Empty, Is.True, "Debe retornar Empty=true cuando capture es null");
        Assert.That(result.Error, Is.Null, "No debe haber error cuando capture retorna null");
    }

    #endregion

    #region ExecuteAsync — Preflop Pipeline

    [Test]
    public async Task ExecuteAsync_ReturnsPopulatedResult_When_Preflop()
    {
        // Arrange: screenshot válido + preflop state + hole cards
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        stateMachine.TryTransition(GameState.HandDetected); // → IsPreflop = true

        var tableReader = new FakeTableReaderService();
        tableReader.SetPotValue = 15.25m;
        tableReader.HoleCard1Face = "Ace";
        tableReader.HoleCard1Rank = 14;
        tableReader.HoleCard2Face = "King";
        tableReader.HoleCard2Rank = 13;

        tableReader.HeroPosition = TablePosition.Button;

        var preflopUseCase = new FakeSetPreflopActionUseCase();
        preflopUseCase.ResponseActionText = "Raise 3BB";
        preflopUseCase.HandSituation = HandSituation.OpenRaise;

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine,
            tableReader: tableReader,
            preflopUseCase: preflopUseCase);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Empty, Is.False, "No debe ser Empty en preflop");
            Assert.That(result.HoleCards, Is.Not.Null, "Debe tener hole cards");
            Assert.That(result.ResponseAction, Is.EqualTo("Raise 3BB"), "Debe tener action text");
            Assert.That(result.HandSituation, Is.EqualTo(HandSituation.OpenRaise), "Debe tener hand situation");
        });
    }

    [Test]
    public async Task ExecuteAsync_ReturnsHoleCards_When_Preflop()
    {
        // Arrange
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        stateMachine.TryTransition(GameState.HandDetected);

        var tableReader = new FakeTableReaderService();
        tableReader.SetPotValue = 10m;
        tableReader.HoleCard1Face = "Ace";
        tableReader.HoleCard2Face = "King";
        tableReader.HeroPosition = TablePosition.Button;

        var preflopUseCase = new FakeSetPreflopActionUseCase();
        preflopUseCase.ResponseActionText = "Call";

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine,
            tableReader: tableReader,
            preflopUseCase: preflopUseCase);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert: verificar que hole cards se transmiten correctamente al GameLoopResult
        Assert.That(result.HoleCards, Is.Not.Null, "Debe tener hole cards");
        Assert.That(result.HoleCards.Value.Item1, Is.EqualTo("Ace"), "HoleCard1 debe ser Ace");
        Assert.That(result.HoleCards.Value.Item2, Is.EqualTo("King"), "HoleCard2 debe ser King");
    }

    #endregion

    #region ExecuteAsync — Postflop (retorna Empty en preflop-first scope)

    [Test]
    public async Task ExecuteAsync_ReturnsEmpty_When_Flop()
    {
        // Arrange: estado flop
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        stateMachine.TryTransition(GameState.HandDetected);
        stateMachine.TryTransition(GameState.PreflopAction);
        stateMachine.TryTransition(GameState.FlopDetected);

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Empty, Is.True, "Flop debe retornar Empty=true en preflop-first scope");
    }

    [Test]
    public async Task ExecuteAsync_ReturnsEmpty_When_Turn()
    {
        // Arrange: estado turn
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        stateMachine.ForceState(GameState.TurnDetected);

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Empty, Is.True, "Turn debe retornar Empty=true en preflop-first scope");
    }

    [Test]
    public async Task ExecuteAsync_ReturnsEmpty_When_River()
    {
        // Arrange: estado river
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        stateMachine.ForceState(GameState.RiverDetected);

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Empty, Is.True, "River debe retornar Empty=true en preflop-first scope");
    }

    [Test]
    public async Task ExecuteAsync_ReturnsEmpty_When_WaitingForHand()
    {
        // Arrange: estado inicial (WaitingForHand) — no hay mano activa
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        // Default state is WaitingForHand

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert: WaitingForHand no es preflop ni postflop — debe retornar Empty
        Assert.That(result.Empty, Is.True, "WaitingForHand debe retornar Empty=true");
    }

    #endregion

    #region ExecuteAsync — Cancellation

    [Test]
    public void ExecuteAsync_Throws_When_Cancelled()
    {
        // Arrange: token already cancelled
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var processor = CreateProcessor();

        // Act & Assert
        Assert.That(
            async () => await processor.ExecuteAsync(cts.Token),
            Throws.TypeOf<OperationCanceledException>(),
            "Debe lanzar OperationCanceledException cuando el token está cancelado");
    }

    #endregion

    #region ExecuteAsync — HeroPosition

    [Test]
    public async Task ExecuteAsync_ReturnsHeroPosition_When_Preflop()
    {
        // Arrange: preflop with specific position
        using var screenshot = CreateSyntheticBitmap(400, 300, Color.Green);
        var captureService = new FakeCaptureService();
        captureService.SetCaptureResult(screenshot);

        var stateMachine = new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>());
        stateMachine.TryTransition(GameState.HandDetected);

        var tableReader = new FakeTableReaderService();
        tableReader.SetPotValue = 10m;
        tableReader.HoleCard1Face = "Ace";
        tableReader.HoleCard2Face = "King";
        tableReader.HeroPosition = TablePosition.Button;

        var preflopUseCase = new FakeSetPreflopActionUseCase();
        preflopUseCase.ResponseActionText = "Raise 3BB";

        var processor = CreateProcessor(
            captureService: captureService,
            stateMachine: stateMachine,
            tableReader: tableReader,
            preflopUseCase: preflopUseCase);

        // Act
        var result = await processor.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.That(result.HeroPosition, Is.EqualTo(TablePosition.Button), "HeroPosition debe ser Button");
    }

    #endregion

    #region Helpers

    private static HandProcessor CreateProcessor(
        ICaptureService? captureService = null,
        ITableReaderService? tableReader = null,
        GameLoopStateMachine? stateMachine = null,
        FakeSetPreflopActionUseCase? preflopUseCase = null)
    {
        return new HandProcessor(
            captureService: captureService ?? new FakeCaptureService(),
            tableReaderService: tableReader ?? new FakeTableReaderService(),
            stateMachine: stateMachine ?? new GameLoopStateMachine(new FakeLogger<GameLoopStateMachine>()),
            decisionEngine: new FakePreflopDecisionEngine(preflopUseCase ?? new FakeSetPreflopActionUseCase()),
            logger: new FakeLogger<HandProcessor>());
    }

    private static Bitmap CreateSyntheticBitmap(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(color);
        return bitmap;
    }

    #endregion
}

#region Fake Implementations

internal sealed class FakeCaptureService : ICaptureService
{
    public IntPtr WindowHandle { get; set; }
    private Bitmap? _captureResult;

    public void SetCaptureResult(Bitmap? bitmap) => _captureResult = bitmap;

    public Bitmap? CaptureWindow(IntPtr handle) => _captureResult;
    public Bitmap? CaptureWindow() => _captureResult;
    public string? SaveFrame(Bitmap bitmap, string folderPath) => null;

    public DomainRegion GetScaledRegion(DomainRegion region)
    {
        return new DomainRegion(
            region.Category, region.Name,
            region.PosX, region.PosY, region.Width, region.Height,
            region.IsHash, region.IsColor, region.IsBoard,
            region.Color, region.IsOnlyNumber,
            region.InactiveUmbral, region.Umbral);
    }

    public void SaveReferenceDimensions(int width, int height) { }
    public string? GetJsonValue(string key, string section) => null;
}

internal sealed class FakeTableReaderService : ITableReaderService
{
    public decimal SetPotValue { get; set; }
    public string HoleCard1Face { get; set; } = string.Empty;
    public int HoleCard1Rank { get; set; }
    public string HoleCard2Face { get; set; } = string.Empty;
    public int HoleCard2Rank { get; set; }
    public TablePosition HeroPosition { get; set; } = TablePosition.None;
    public string HandNumber { get; set; } = string.Empty;
    public bool IsNewHand { get; set; }
    public int BoardCardCount { get; set; }

    void ITableReaderService.SetBetPlayer(Bitmap screenshot, PlayerGameState state) { }

    decimal ITableReaderService.SetHeroStack(Bitmap screenshot, PlayerGameState state)
    {
        state.HeroStack = 100m;
        return 100m;
    }

    void ITableReaderService.SetPotValue(Bitmap screenshot, PlayerGameState state)
    {
        state.PotSize = SetPotValue;
    }

    Task<(string HandNumber, bool IsNewHand)> ITableReaderService.SetTableHandAsync(Bitmap screenshot, PlayerGameState state)
    {
        return Task.FromResult((HandNumber, IsNewHand));
    }

    Task ITableReaderService.ObtainCardsPlayerAsync(Bitmap screenshot, PlayerGameState state)
    {
        state.HoleCard1Face = HoleCard1Face;
        state.HoleCard1Rank = HoleCard1Rank;
        state.HoleCard2Face = HoleCard2Face;
        state.HoleCard2Rank = HoleCard2Rank;
        state.Position = HeroPosition;
        return Task.CompletedTask;
    }

    bool ITableReaderService.IsBoardCardVisible(Bitmap screenshot, string cardRegionName) => true;

    int ITableReaderService.CountVisibleBoardCards(Bitmap screenshot) => BoardCardCount;
}

internal sealed class FakePreflopDecisionEngine(FakeSetPreflopActionUseCase preflop) : IDecisionEngine
{
    public Task<OpenScrape.Contracts.DecisionResult> DecideAsync(
        OpenScrape.Contracts.DecisionRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new OpenScrape.Contracts.DecisionResult
        {
            Action = preflop.ResponseActionText,
            Situation = preflop.HandSituation.ToString()
        });
}

internal sealed class FakeSetPreflopActionUseCase : ISetPreflopActionUseCase
{
    public string ResponseActionText { get; set; } = string.Empty;
    public HandSituation HandSituation { get; set; } = HandSituation.None;

    public Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest request)
    {
        request.ResponseAction.Action = ResponseActionText;
        request.ResponseAction.HandSituation = HandSituation;

        return Task.FromResult(new SetPreflopActionUseCaseResponse
        {
            ResponseAction = request.ResponseAction,
            PlayerState = request.PlayerState
        });
    }
}

internal sealed class FakeLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

#endregion
