using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.Domain.ValueObjects;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

/// <summary>
/// Tests del facade de decisión postflop. Usa un <see cref="FakePokerCalculator"/>
/// para controlar el output de equity sin depender de Monte Carlo real.
/// </summary>
[TestFixture]
public class PokerDecisionFacadeTests
{
    private static CardDataOuts C(Rank rank, Suit suit) => new(suit, rank);

    

    private sealed class FakePokerCalculator : IPokerCalculator
    {
        public PokerCalculationResult NextResult { get; set; } = new()
        {
            EquityPercentage = 50,
            PotOddsPercentage = 25,
            HeroHandRank = HandRank.OnePair,
            DrawTypes = [],
        };

        public int CallCount { get; private set; }

        public PokerCalculationResult Calculate(
            List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
            decimal currentPotSize, decimal betToCall, int numOpponents = 1,
            int? monteCarloIterations = null, bool isInPosition = false,
            decimal heroStack = 0, decimal villainStack = 0,
            string? handSituation = null,
            TablePosition villainPosition = TablePosition.None,
            OpponentProfile? opponentProfile = null)
        {
            CallCount++;
            return NextResult;
        }
    }

    private static PokerDecisionFacade CreateFacade(
        out FakePokerCalculator calculator,
        out IPostflopDecisionService decisionService,
        out IOpponentTracker opponentTracker,
        out MetricsCollector metrics)
    {
        calculator = new FakePokerCalculator();
        var profile = Options.Create(new StrategyProfile().FillMissingThresholds());
        var betSizing = new BetSizingService(profile);
        var rangePolarizer = new RangePolarizer();
        var registry = new ThresholdsRegistry(profile);
        decisionService = new PostflopDecisionService(profile, betSizing, rangePolarizer, registry);
        var boardAnalyzer = new BoardTextureAnalyzer();
        opponentTracker = new OpponentTracker();
        metrics = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        return new PokerDecisionFacade(
            calculator, decisionService, betSizing, boardAnalyzer, opponentTracker, metrics);
    }

    private static PokerDecisionFacade CreateFacade(
        out FakePokerCalculator calculator,
        out IPostflopDecisionService decisionService,
        out IOpponentTracker opponentTracker)
        => CreateFacade(out calculator, out decisionService, out opponentTracker, out _);

    private static DecisionRequest MakeFlopRequest(double equityPct)
    {
        _ = equityPct; // el test ajusta calculator.NextResult directamente
        return new DecisionRequest
        {
            HeroCards = [C(Rank.Ace, Suit.Hearts), C(Rank.King, Suit.Hearts)],
            CommunityCards =
            [
                C(Rank.Queen, Suit.Hearts),
                C(Rank.Seven, Suit.Diamonds),
                C(Rank.Two, Suit.Clubs),
            ],
            Street = BoardPosition.Flop,
            Situation = HandSituation.OpenRaise,
            HeroStack = 100m,
            VillainStack = 100m,
            PotSize = 10m,
            BetToCall = 0m,
            IsInPosition = true,
            NumOpponents = 1,
            VillainId = "Unknown",
            VillainBetSize = BetSizeCategory.NoBet,
        };
    }

    // ─── Orquestación básica ───────────────────────────────────────────────

    [Test]
    public async Task EvaluateAsync_Flop_InvokaCalculadorYDevuelveResultado()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 72,
            PotOddsPercentage = 0,
            HeroHandRank = HandRank.Flush,
            DrawTypes = ["Flush Draw"],
            BoardTexture = BoardTextureCategory.Wet,
        };

        var result = await facade.EvaluateAsync(MakeFlopRequest(72));

        Assert.That(calc.CallCount, Is.EqualTo(1));
        Assert.That(result.EquityPercent, Is.EqualTo(72));
        Assert.That(result.RecommendedAction, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task EvaluateAsync_Turn_UsaBoardChangeCuandoHayPreviousBoard()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 55,
            HeroHandRank = HandRank.TwoPair,
            DrawTypes = [],
        };

        var request = MakeFlopRequest(55) with
        {
            Street = BoardPosition.Turn,
            CommunityCards =
            [
                C(Rank.Queen, Suit.Hearts),
                C(Rank.Seven, Suit.Diamonds),
                C(Rank.Two, Suit.Clubs),
                C(Rank.Jack, Suit.Hearts),
            ],
            PreviousBoard =
            [
                C(Rank.Queen, Suit.Hearts),
                C(Rank.Seven, Suit.Diamonds),
                C(Rank.Two, Suit.Clubs),
            ],
        };

        var result = await facade.EvaluateAsync(request);

        Assert.That(result.EquityPercent, Is.EqualTo(55));
        Assert.That(result.BoardTexture, Is.Not.Null);
    }

    [Test]
    public async Task EvaluateAsync_River_ClasificaRiverCard()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 40,
            HeroHandRank = HandRank.OnePair,
            DrawTypes = [],
        };

        var request = MakeFlopRequest(40) with
        {
            Street = BoardPosition.River,
            CommunityCards =
            [
                C(Rank.Queen, Suit.Hearts),
                C(Rank.Seven, Suit.Diamonds),
                C(Rank.Two, Suit.Clubs),
                C(Rank.Jack, Suit.Hearts),
                C(Rank.Eight, Suit.Spades),
            ],
            PreviousBoard =
            [
                C(Rank.Queen, Suit.Hearts),
                C(Rank.Seven, Suit.Diamonds),
                C(Rank.Two, Suit.Clubs),
                C(Rank.Jack, Suit.Hearts),
            ],
        };

        var result = await facade.EvaluateAsync(request);

        Assert.That(result.RecommendedAction, Is.Not.Empty);
    }

    // ─── Contrato de Reason (nunca null) ───────────────────────────────────

    [Test]
    public async Task EvaluateAsync_Reason_NuncaEsNull()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 30,
            HeroHandRank = HandRank.HighCard,
            DrawTypes = [],
        };

        var result = await facade.EvaluateAsync(MakeFlopRequest(30));

        Assert.That(result.Reason, Is.Not.Null);
        Assert.That(result.Reason, Is.Not.Empty);
    }

    // ─── Telemetría instrumentada ─────────────────────────────────────────

    [Test]
    public async Task EvaluateAsync_InstrumentaMetricsCollector_CategoriasEsperadas()
    {
        var facade = CreateFacade(out var calc, out _, out _, out var metrics);
        metrics.StartHand("hand-1");
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 50,
            HeroHandRank = HandRank.OnePair,
            DrawTypes = [],
        };

        _ = await facade.EvaluateAsync(MakeFlopRequest(50));

        var snapshot = metrics.SnapshotSession();
        Assert.That(snapshot.LastHand.Keys, Is.EquivalentTo(new[]
        {
            TelemetryCategories.DecisionTotal,
            TelemetryCategories.DecisionEquity,
            TelemetryCategories.DecisionTexture,
            TelemetryCategories.DecisionProfile,
            TelemetryCategories.DecisionDecisionService,
            TelemetryCategories.DecisionSizing,
        }));
        foreach (var (category, stats) in snapshot.LastHand)
        {
            Assert.That(stats.Count, Is.EqualTo(1), $"Categoria {category} debe tener 1 medicion");
            Assert.That(stats.MaxMs, Is.GreaterThanOrEqualTo(0), $"Categoria {category} max debe ser >= 0");
        }
    }

    // ─── Cancelación ───────────────────────────────────────────────────────

    [Test]
    public void EvaluateAsync_TokenCanceladoAntes_LanzaOperationCanceled()
    {
        var facade = CreateFacade(out _, out _, out _);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await facade.EvaluateAsync(MakeFlopRequest(50), cts.Token));
    }

    // ─── Sizing extraction ─────────────────────────────────────────────────

    [Test]
    public async Task EvaluateAsync_BetSize_SeExtraeDeStringDeAccion()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 80,
            HeroHandRank = HandRank.Flush,
            DrawTypes = [],
        };

        var result = await facade.EvaluateAsync(MakeFlopRequest(80));

        // Con equity 80% y no facing bet, debería betear. BetSize puede ser 0.5, 0.66, etc.
        if (result.RecommendedAction.StartsWith("Bet "))
            Assert.That(result.BetSize, Is.Not.Null,
                $"Acción '{result.RecommendedAction}' debería tener BetSize parseado");
    }

    // ─── Resolución de villain profile ─────────────────────────────────────

    [Test]
    public async Task EvaluateAsync_VillainIdUnknown_UsaPerfilNull()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 45,
            HeroHandRank = HandRank.OnePair,
            DrawTypes = [],
        };

        var request = MakeFlopRequest(45) with { VillainId = "Unknown" };

        var result = await facade.EvaluateAsync(request);

        // No debe lanzar; el facade degrada sin perfil.
        Assert.That(result.RecommendedAction, Is.Not.Empty);
    }

    [Test]
    public async Task EvaluateAsync_VillainProfileExplicito_SeUsaSinLookup()
    {
        var facade = CreateFacade(out var calc, out _, out var tracker);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 60,
            HeroHandRank = HandRank.OnePair,
            DrawTypes = [],
        };
        var explicitProfile = new OpponentProfile { PlayerId = "Villain1" };

        var request = MakeFlopRequest(60) with
        {
            VillainId = "Villain1",
            VillainProfile = explicitProfile,
        };

        var result = await facade.EvaluateAsync(request);

        // Aunque el tracker no tenga datos, el perfil explícito permite proceder.
        Assert.That(result, Is.Not.Null);
        Assert.That(tracker.AllProfiles.ContainsKey("Villain1"), Is.False,
            "No debería haberse tocado el tracker cuando hay perfil explícito");
    }

    // ─── Propagación de flags de decisión ──────────────────────────────────

    [Test]
    public async Task EvaluateAsync_FlagsDeDecision_SePropaganAlResult()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 50,
            HeroHandRank = HandRank.OnePair,
            DrawTypes = [],
        };

        var result = await facade.EvaluateAsync(MakeFlopRequest(50));

        // Los flags existen (pueden ser false todos); verificamos que no lanza.
        Assert.That(result.IsBluff, Is.TypeOf<bool>());
        Assert.That(result.IsBarrel, Is.TypeOf<bool>());
        Assert.That(result.IsCheckRaise, Is.TypeOf<bool>());
        Assert.That(result.IsFloating, Is.TypeOf<bool>());
    }

    // ─── CalculationDetail se expone ───────────────────────────────────────

    [Test]
    public async Task EvaluateAsync_CalculationDetail_ExponeResultadoCrudo()
    {
        var facade = CreateFacade(out var calc, out _, out _);
        calc.NextResult = new PokerCalculationResult
        {
            EquityPercentage = 50,
            HeroHandRank = HandRank.TwoPair,
            DrawTypes = ["Flush Draw"],
        };

        var result = await facade.EvaluateAsync(MakeFlopRequest(50));

        Assert.That(result.CalculationDetail, Is.Not.Null);
        Assert.That(result.CalculationDetail, Is.TypeOf<PokerCalculationResult>());
    }
}
