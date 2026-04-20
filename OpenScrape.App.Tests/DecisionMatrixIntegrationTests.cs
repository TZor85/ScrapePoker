using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests;

/// <summary>
/// Matriz sistemática de integración para <see cref="PostflopDecisionService"/>:
/// recorre los 216 nodos (3 streets × 9 situations × 2 positions × 2 bet states
/// × 2 equity buckets) y verifica propiedades invariantes independientes de los
/// thresholds concretos. Cubre deuda documentada como
/// <c>decision-matrix-integration-tests</c>.
/// </summary>
[TestFixture]
public class DecisionMatrixIntegrationTests
{
    private PostflopDecisionService _service = null!;
    private StrategyProfile _profile = null!;

    private static readonly HandSituation[] Situations =
    {
        HandSituation.OpenRaise,
        HandSituation.RaiseOverLimper,
        HandSituation.Call,
        HandSituation.ThreeBet,
        HandSituation.FourBet,
        HandSituation.Squeeze,
        HandSituation.VsSqueeze,
        HandSituation.DonkBet,
        HandSituation.DonkBetVsOpenRaise,
    };

    private static readonly BoardPosition[] Streets =
    {
        BoardPosition.Flop,
        BoardPosition.Turn,
        BoardPosition.River,
    };

    [SetUp]
    public void SetUp()
    {
        // Cargar el appsettings.json REAL para reflejar el estado productivo.
        // El csproj de OpenScrape.App ya copia el archivo al bin; como OpenScrape.App.Tests
        // referencia a OpenScrape.App vía ProjectReference, el appsettings.json queda en
        // TestContext.CurrentContext.TestDirectory.
        var baseDir = TestContext.CurrentContext.TestDirectory;
        var settingsPath = Path.Combine(baseDir, "appsettings.json");
        Assert.That(File.Exists(settingsPath), Is.True,
            $"appsettings.json no encontrado en '{settingsPath}'. La matriz requiere el profile real.");

        var config = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _profile = new StrategyProfile();
        config.GetSection("StrategyProfile").Bind(_profile);

        var options = Options.Create(_profile);
        var betSizing = new BetSizingService(options);
        var rangePolarizer = new RangePolarizer();
        _service = new PostflopDecisionService(options, betSizing, rangePolarizer);
    }

    // ─── Generador de la matriz ────────────────────────────────────────────

    public static IEnumerable<TestCaseData> MatrixCases()
    {
        var positions = new[] { true, false };
        var betSizes = new[] { BetSizeCategory.NoBet, BetSizeCategory.Medium };
        var equities = new[] { 20.0, 80.0 };

        foreach (var street in Streets)
        {
            foreach (var situation in Situations)
            {
                foreach (var isInPosition in positions)
                {
                    foreach (var villainBetSize in betSizes)
                    {
                        foreach (var equity in equities)
                        {
                            yield return new TestCaseData(street, situation, isInPosition, villainBetSize, equity)
                                .SetName($"Matrix({street},{situation},IP={isInPosition},{villainBetSize},Eq={equity})");
                        }
                    }
                }
            }
        }
    }

    // ─── Helper de input neutral ───────────────────────────────────────────

    private static PostflopDecisionInput MakeNeutralInput(
        BoardPosition street,
        HandSituation situation,
        bool isInPosition,
        BetSizeCategory villainBetSize,
        double equity)
    {
        return new PostflopDecisionInput
        {
            Equity = equity,
            Street = street,
            Situation = situation,
            BoardTexture = "SemiDry",
            IsInPosition = isInPosition,
            VillainBetSize = villainBetSize,
            // Stacks y pote con SPR ~10 para evitar push/fold automáticos.
            HeroStack = 100m,
            PotSize = 10m,
            NumOpponents = 1,
            HeroHandRank = HandRank.HighCard,
            VillainType = OpponentType.Unknown,
        };
    }

    // ─── Test parametrizado principal ──────────────────────────────────────

    [TestCaseSource(nameof(MatrixCases))]
    public void Matrix_ProducesValidDecision(
        BoardPosition street,
        HandSituation situation,
        bool isInPosition,
        BetSizeCategory villainBetSize,
        double equity)
    {
        var input = MakeNeutralInput(street, situation, isInPosition, villainBetSize, equity);
        var result = _service.DetermineAction(input);

        var ctx = $"({street},{situation},IP={isInPosition},{villainBetSize},Eq={equity})";
        var action = result.Action ?? string.Empty;
        var firstToken = action.Split(' ')[0];
        var validTokens = new[] { "Bet", "Call", "Raise", "Check", "Fold", "All-In" };

        Assert.Multiple(() =>
        {
            Assert.That(action, Is.Not.Null.And.Not.Empty,
                $"Case {ctx}: Action null/vacío.");
            Assert.That(validTokens, Does.Contain(firstToken),
                $"Case {ctx}: primer token '{firstToken}' no pertenece a {{{string.Join(",", validTokens)}}} (Action='{action}').");

            // Invariante 1: NoBet → no-Call
            if (villainBetSize == BetSizeCategory.NoBet)
            {
                Assert.That(action, Does.Not.StartWith("Call"),
                    $"Case {ctx}: sin apuesta no puede callear pero emitió '{action}'.");
            }

            // Invariante 2: Equity alta + NoBet → no-Fold
            if (equity >= 80 && villainBetSize == BetSizeCategory.NoBet)
            {
                Assert.That(action, Does.Not.StartWith("Fold"),
                    $"Case {ctx}: equity alta sin facing bet no debe foldear pero emitió '{action}'.");
            }

            // Invariante 3: Equity alta + facing bet → no-Fold
            if (equity >= 80 && villainBetSize == BetSizeCategory.Medium)
            {
                Assert.That(action, Does.Not.StartWith("Fold"),
                    $"Case {ctx}: equity alta facing Medium no debe foldear pero emitió '{action}'.");
            }

            // Invariante 4: Equity baja + facing bet → no-Raise
            if (equity <= 20 && villainBetSize == BetSizeCategory.Medium)
            {
                Assert.That(action, Does.Not.StartWith("Raise"),
                    $"Case {ctx}: equity baja facing Medium no debe subir pero emitió '{action}'.");
            }
        });
    }

    // ─── Guardrail de configuración ────────────────────────────────────────

    [Test]
    public void Matrix_EachStreetThresholdsKeyHasEntry()
    {
        var missing = new List<string>();
        foreach (var street in Streets)
        {
            foreach (var situation in Situations)
            {
                var key = $"{street}_{situation}";
                if (!_profile.Thresholds.ContainsKey(key))
                    missing.Add(key);
            }
        }

        Assert.That(missing, Is.Empty,
            $"Claves faltantes en StrategyProfile.Thresholds (posible typo en appsettings.json): {string.Join(", ", missing)}");
    }
}
