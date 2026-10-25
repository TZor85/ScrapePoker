using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Services;
using OpenScrape.App.Services.Exploitability;
using OpenScrape.App.Services.Tracking;
using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;

namespace OpenScrape.App.Tests.Fixtures;

/// <summary>
/// Construye el pipeline de decisión con las implementaciones reales y sin base de datos.
/// Cada llamada devuelve instancias nuevas: el coordinador guarda estado entre calles.
/// </summary>
internal static class GameCoordinatorFixtureBuilder
{
    public static GameCoordinator Create(StrategyProfile profile, IGameLoggerService logger, IDecisionEngine? engine = null, int monteCarloSeed = 1)
    {
        var options = Options.Create(profile);

        return new GameCoordinator(
            new PostflopDecisionService(
                options,
                new BetSizingService(options),
                new RangePolarizer(),
                new ThresholdsRegistry(options),
                new DeterministicRandomProvider()),
            new OpponentTracker(),
            new BoardTextureAnalyzer(),
            new BetSizingService(options),
            new ExploitabilityCalculator(),
            new GameLoopStateMachine(NullLogger<GameLoopStateMachine>.Instance),
            logger,
            new StrategyProfileService(options),
            new PostflopContextHolder(),
            engine ?? CreateLocalEngine(profile, monteCarloSeed));
    }

    public static PostflopDecisionEngine CreateEngine(StrategyProfile profile)
    {
        var options = Options.Create(profile);

        return new PostflopDecisionEngine(
            new PostflopDecisionService(
                options,
                new BetSizingService(options),
                new RangePolarizer(),
                new ThresholdsRegistry(options),
                new DeterministicRandomProvider()));
    }

    public static UnifiedPokerCalculator CreateCalculator(StrategyProfile profile, int monteCarloSeed, ILogger<UnifiedPokerCalculator>? logger = null)
    {
        var options = Options.Create(profile);

        return new UnifiedPokerCalculator(
            new MonteCarloSimulator(monteCarloSeed),
            new OutsCalculator(options),
            new PreflopEquityCalculator(),
            new BitHandEvaluator(),
            new BoardTextureAnalyzer(),
            options,
            logger ?? NullLogger<UnifiedPokerCalculator>.Instance);
    }
    private static LocalDecisionEngine CreateLocalEngine(StrategyProfile profile, int monteCarloSeed)
    {
        var options = Options.Create(profile);
        var preflop = PreflopTestCaseBuilder.Create(
            new FixedRangeStore(new TableDTO { Name = "sin-tabla", Positions = [] }),
            new FixedRandomProvider(0));

        return new LocalDecisionEngine(
            new PreflopRequestFactory(),
            new PostflopRequestFactory(
                CreateCalculator(profile, monteCarloSeed),
                new BoardTextureAnalyzer(),
                new PreflopAnalyzer(),
                options),
            preflop,
            CreateEngine(profile));
    }
}

/// <summary>Logger que escribe a la salida de NUnit, para ver las excepciones que el calculador traga.</summary>
internal sealed class TestLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => NUnit.Framework.TestContext.Progress.WriteLine($"[{logLevel}] {formatter(state, exception)} {exception}");
}

/// <summary>Proveedor determinista: siempre devuelve el primer número del rango.</summary>
internal sealed class DeterministicRandomProvider : IRandomProvider
{
    public double NextDouble() => 0.0;

    public int Next(int minValue, int maxValue) => minValue;
}
