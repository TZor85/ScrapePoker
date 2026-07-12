using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Entities;
using OpenScrape.App.Services;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del UiUpdateService. Verifica que la actualización de controles
/// WinForms (labels, textbox, picture boxes, overlay) funciona correctamente
/// cuando se reciben datos del pipeline de decisión.
/// </summary>
[TestFixture]
public class UiUpdateServiceTests
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
        public PokerCalculationResult? LastUpdateResult { get; private set; }
        public string? LastSituacion { get; private set; }
        public string? LastAction { get; private set; }

        public void UpdateAction(string action) => LastAction = action;
        public void UpdatePotOddsPercentage(string potOdds) { }
        public void UpdateEquityPercentage(string equity) { }
        public void UpdateStreetPhase(string phase) { }
        public void UpdateSituacion(string situacion) => LastSituacion = situacion;
        public void UpdateHandStrength(HandRank handRank, KickerStrength kicker, bool hasComboDraw) { }
        public void UpdateBoardTexture(BoardTextureCategory? category, double wetnessScore) { }
        public void UpdateStreetIndicator(string street) { }
        public void UpdateFoldEquity(double foldEquity) { }
        public void UpdateEVWithFoldEquity(double ev) { }
        public void UpdateSuggestedBetSize(double? betSize) { }
        public void UpdateTableName(string tableName) { }
        public void UpdateWithCalculationResult(PokerCalculationResult result) => LastUpdateResult = result;
        public void ClearAll() { }
    }

    private sealed class FakePokerCalculator : IPokerCalculator
    {
        public PokerCalculationResult ResultToReturn { get; set; } = new()
        {
            PotOddsPercentage = 25.0,
            EquityPercentage = 45.0,
            ShouldCall = true,
            TotalOuts = 8
        };

        public PokerCalculationResult Calculate(
            List<CardDataOuts> playerHand,
            List<CardDataOuts> communityCards,
            decimal currentPotSize,
            decimal betToCall,
            int numOpponents = 1,
            int? monteCarloIterations = null,
            bool isInPosition = false,
            decimal heroStack = 0,
            decimal villainStack = 0,
            string? handSituation = null,
            TablePosition villainPosition = TablePosition.None,
            OpponentProfile? opponentProfile = null)
        {
            return ResultToReturn;
        }
    }

    private sealed class FakeGameCoordinator : IGameCoordinator
    {
        public PokerCalculationResult? FlopResult { get; set; }
        public PokerCalculationResult? TurnResult { get; set; }
        public PokerCalculationResult? RiverResult { get; set; }
        public TurnBoardTexture TurnBoardTexture { get; set; }
        public RiverBoardTexture RiverBoardTexture { get; set; }

        public void ResetContext() { }
        public GameDecisionResult DetermineFlopAction(PlayerGameState state) => new("Fold", "");
        public GameDecisionResult DetermineTurnAction(PlayerGameState state) => new("Fold", "");
        public GameDecisionResult DetermineRiverAction(PlayerGameState state) => new("Fold", "");
        public void SetFlopResult(PokerCalculationResult result) { }
        public void SetTurnResult(PokerCalculationResult result) { }
        public void SetRiverResult(PokerCalculationResult result) { }
        public BetSizeCategory GetOpponentBetSize(decimal maxBet, decimal potSize) => BetSizeCategory.Small;
        public OpponentType GetVillainType(PlayerGameState state, bool? heroIsInPosition = null) => OpponentType.Unknown;
        public OpponentProfile? GetActiveVillainProfile(PlayerGameState state) => null;
        public void TrackVillainPostflopAction(PlayerGameState state, decimal maxBet, bool isPreflopAggressor, bool? heroIsInPosition = null) { }
        public decimal GetVillainStack(PlayerGameState state) => 100m;
        public string GetActiveVillainId(PlayerGameState state) => "V1";
        public bool HeroBlocksTopBoardCard(PlayerGameState state) => false;
        public string AdjustBetSize(string action, decimal heroStack, decimal potSize, int numOpponents, bool isPaired, bool isCoordinated, bool isDry, bool isInPosition) => action;
        public string FormatCardsForLog(PlayerGameState state, BoardPosition street) => "";
        public (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(PlayerGameState state, decimal maxBet, bool isHeroInPosition, HandSituation currentSituation) => (false, HandSituation.None);
        public BoardChangeResult AnalyzeBoardChange(List<BoardData> boardCards, int previousCardCount) => BoardChangeResult.Safe;
        public TurnBoardTexture AnalyzeTurnBoardTexture(List<BoardData> boardCards) => TurnBoardTexture.Dry;
        public RiverBoardTexture AnalyzeRiverBoardTexture(List<BoardData> boardCards) => RiverBoardTexture.Dry;
        public bool DetectNewHand(PlayerGameState state, bool handNumberChanged, string currentHand, ref string previousDealer, ref string previousSB, ref string previousBB) => false;
    }

    private sealed class FakeActionFormatter : IActionFormatter
    {
        public string EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind)
            => action.Contains('x') ? $"{action} ({bigBlind * 3:0}BB)" : action;
    }

    private static UiUpdateControls CreateControls()
    {
        return new UiUpdateControls(
            LbAction: new Label(),
            LbPositionAction: new Label(),
            TbResume: new TextBox(),
            PbButtonPlayerOne: new PictureBox(),
            PbButtonPlayerTwo: new PictureBox(),
            PbButtonPlayerThree: new PictureBox(),
            PbButtonPlayerFour: new PictureBox(),
            PbButtonPlayerFive: new PictureBox(),
            PbButtonHero: new PictureBox(),
            LbNamePlayerOne: new Label(),
            LbNamePlayerTwo: new Label(),
            LbNamePlayerThree: new Label(),
            LbNamePlayerFour: new Label(),
            LbNamePlayerFive: new Label(),
            LbBetPlayerOne: new Label(),
            LbBetPlayerTwo: new Label(),
            LbBetPlayerThree: new Label(),
            LbBetPlayerFour: new Label(),
            LbBetPlayerFive: new Label(),
            LbBetHero: new Label(),
            LbPot: new Label(),
            LbUserStack: new Label(),
            LbTableHand: new Label(),
            LbTableName: new Label(),
            PbHeroCard0: new PictureBox(),
            PbHeroCard1: new PictureBox(),
            PbBoard1: new PictureBox(),
            PbBoard2: new PictureBox(),
            PbBoard3: new PictureBox(),
            PbBoard4: new PictureBox(),
            PbBoard5: new PictureBox());
    }

    private static UiUpdateService CreateService(
        IPokerCalculator? calculator = null,
        IGameCoordinator? coordinator = null,
        IActionFormatter? formatter = null,
        GameLoopStateMachine? stateMachine = null,
        GameLoggerService? gameLogger = null,
        ImageCropperService? imageCropper = null)
    {
        return new UiUpdateService(
            calculator ?? new FakePokerCalculator(),
            gameLogger ?? CreateGameLogger(),
            formatter ?? new FakeActionFormatter(),
            imageCropper ?? new ImageCropperService(),
            coordinator ?? new FakeGameCoordinator(),
            stateMachine ?? new GameLoopStateMachine(NullLogger<GameLoopStateMachine>.Instance),
            NullLogger<UiUpdateService>.Instance);
    }

    private static GameLoggerService CreateGameLogger()
    {
        var store = Marten.DocumentStore.For("host=localhost;database=test;username=test;password=test");
        var metrics = new OpenScrape.App.Telemetry.MetricsCollector(NullLogger<OpenScrape.App.Telemetry.MetricsCollector>.Instance);
        return new GameLoggerService(store, NullLogger<GameLoggerService>.Instance, metrics);
    }

    private static PlayerGameState CreatePreflopState()
    {
        return new PlayerGameState
        {
            PotSize = 3.5m,
            CurrentBet = 1.0m,
            HeroStack = 98.5m,
            HoleCard1Face = "Ah",
            HoleCard2Face = "Kd",
            HoleCard1Rank = 14,
            HoleCard2Rank = 13,
            HoleCard1Suit = 3,
            HoleCard2Suit = 1,
            Position = TablePosition.Button,
            IsDealer = true,
            HandSituation = HandSituation.OpenRaise,
            Players = new List<Player>
            {
                new Player { Name = "P0", ValuePosition = 0, Empty = false, SitOut = false, Active = false },
                new Player { Name = "P1", Alias = "Villain1", Empty = false, SitOut = false, Active = true, Bet = 1.0m },
                new Player { Name = "P2", Alias = "Villain2", Empty = false, SitOut = false, Active = true, Bet = 1.5m },
            }
        };
    }

    // =============== TESTS ===============

    [Test]
    public void UpdateResults_ConDatosValidos_ActualizaControles()
    {
        // Arrange
        var stateMachine = new GameLoopStateMachine(NullLogger<GameLoopStateMachine>.Instance);
        stateMachine.ForceState(GameState.PreflopAction); // → IsPreflop = true

        using var service = CreateService(stateMachine: stateMachine);
        var controls = CreateControls();
        var overlay = new FakeOverlay();
        service.Attach(new FakeSyncTarget(), overlay, controls);

        var state = CreatePreflopState();
        var result = new PokerCalculationResult
        {
            PotOddsPercentage = 33.3,
            EquityPercentage = 55.0,
            ShouldCall = true,
            TotalOuts = 12,
            EffectiveOuts = 10,
            Street = "Preflop"
        };

        // Act
        service.UpdateResults(state, result, "Raise x3", "H12345", "Table 1");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(controls.LbAction.Text, Is.EqualTo("Raise x3"));
            Assert.That(controls.LbPositionAction.Text, Is.EqualTo("OpenRaise"));
            Assert.That(controls.TbResume.Text, Does.Contain("H12345"));
            Assert.That(controls.TbResume.Text, Does.Contain("Pot: "));
            Assert.That(controls.LbPot.Text, Does.Contain("3"));
            Assert.That(controls.LbTableHand.Text, Is.EqualTo("H12345"));
            Assert.That(controls.LbTableName.Text, Is.EqualTo("Table 1"));
        });
    }

    [Test]
    public void UpdateResults_ConResultadoVacio_NoLanzaExcepcion()
    {
        // Arrange
        using var service = CreateService();
        var controls = CreateControls();
        service.Attach(new FakeSyncTarget(), new FakeOverlay(), controls);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            service.UpdateResults(new PlayerGameState(), new PokerCalculationResult(), null, "", "");
        });
    }

    [Test]
    public void UpdateOverlay_ConResultado_LLamaAlOverlay()
    {
        // Arrange
        using var service = CreateService();
        var controls = CreateControls();
        var overlay = new FakeOverlay();
        service.Attach(new FakeSyncTarget(), overlay, controls);

        var result = new PokerCalculationResult
        {
            PotOddsPercentage = 25.0,
            EquityPercentage = 60.0,
            RecommendedAction = "Call",
        };

        // Act
        service.UpdateOverlay(result);

        // Assert
        Assert.That(overlay.LastUpdateResult, Is.Not.Null);
        Assert.That(overlay.LastUpdateResult.RecommendedAction, Is.EqualTo("Call"));
    }

    [Test]
    public void UpdateResults_ConCartasValidas_AsignaImagenes()
    {
        // Arrange
        using var bmp = new Bitmap(1, 1);
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var imageBase64 = Convert.ToBase64String(ms.ToArray());

        using var service = CreateService();
        var controls = CreateControls();
        service.Attach(new FakeSyncTarget(), new FakeOverlay(), controls);

        service.SetCardList(new List<Card>
        {
            new Card { Id = "Ah", ImageBase64 = imageBase64 },
            new Card { Id = "Kd", ImageBase64 = imageBase64 },
        });

        var state = CreatePreflopState();
        var result = new PokerCalculationResult();

        // Act
        service.UpdateResults(state, result, "Call", "H1", "");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(controls.PbHeroCard0.Image, Is.Not.Null, "HoleCard1 (Ah) debe tener imagen");
            Assert.That(controls.PbHeroCard1.Image, Is.Not.Null, "HoleCard2 (Kd) debe tener imagen");
        });
    }

    [Test]
    public void UpdateResults_SinCartasCoincidentes_ImagenesNulas()
    {
        // Arrange
        using var service = CreateService();
        var controls = CreateControls();
        service.Attach(new FakeSyncTarget(), new FakeOverlay(), controls);

        // No llamamos a SetCardList — lista vacía por defecto
        var state = CreatePreflopState();
        var result = new PokerCalculationResult();

        // Act
        service.UpdateResults(state, result, "Call", "H1", "");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(controls.PbHeroCard0.Image, Is.Null);
            Assert.That(controls.PbHeroCard1.Image, Is.Null);
            Assert.That(controls.PbBoard1.Image, Is.Null);
        });
    }

    [Test]
    public void Detach_LlamadaMultiple_EsIdempotente()
    {
        // Arrange
        using var service = CreateService();
        var controls = CreateControls();
        service.Attach(new FakeSyncTarget(), new FakeOverlay(), controls);

        // Act & Assert
        Assert.DoesNotThrow(() => service.Detach());
        Assert.DoesNotThrow(() => service.Detach());
    }

    [Test]
    public void Detach_LuegoUpdateResults_NoLanzaExcepcion()
    {
        // Arrange
        using var service = CreateService();
        var controls = CreateControls();
        service.Attach(new FakeSyncTarget(), new FakeOverlay(), controls);
        service.Detach();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            service.UpdateResults(new PlayerGameState(), new PokerCalculationResult(), null, "", "");
        });
    }

    [Test]
    public void Attach_SinDetach_ActualizaControlesNormalmente()
    {
        // Arrange
        using var service = CreateService();
        var controls = CreateControls();
        var overlay = new FakeOverlay();
        service.Attach(new FakeSyncTarget(), overlay, controls);

        var state = CreatePreflopState();
        var result = new PokerCalculationResult
        {
            PotOddsPercentage = 50.0,
            EquityPercentage = 70.0,
        };

        // Act
        service.UpdateResults(state, result, "Bet 1/2", "H999", "TestTable");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(controls.LbAction.Text, Is.EqualTo("Bet 1/2"));
            Assert.That(controls.LbTableName.Text, Is.EqualTo("TestTable"));
        });
    }
}
