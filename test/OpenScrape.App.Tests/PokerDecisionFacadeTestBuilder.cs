using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenScrape.App.Aplication.UseCases;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.DecisionMaker.DTOs;
using OpenScrape.DecisionMaker.Interfaces;
using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

public sealed class PokerDecisionFacadeTestBuilder
{
    private IPokerCalculator _calculator = new FakePokerCalculator();
    private IPostflopDecisionService? _decisionService;
    private IBetSizingService? _betSizingService;
    private IBoardTextureAnalyzer _boardTextureAnalyzer = new BoardTextureAnalyzer();
    private IOpponentTracker _opponentTracker = new OpponentTracker();
    private MetricsCollector _metrics = new(NullLogger<MetricsCollector>.Instance);

    public PokerDecisionFacadeTestBuilder WithCalculator(IPokerCalculator calculator)
    {
        _calculator = calculator;
        return this;
    }

    public PokerDecisionFacadeTestBuilder WithDecisionService(IPostflopDecisionService decisionService)
    {
        _decisionService = decisionService;
        return this;
    }

    public PokerDecisionFacadeTestBuilder WithBetSizingService(IBetSizingService betSizingService)
    {
        _betSizingService = betSizingService;
        return this;
    }

    public PokerDecisionFacadeTestBuilder WithBoardTextureAnalyzer(IBoardTextureAnalyzer analyzer)
    {
        _boardTextureAnalyzer = analyzer;
        return this;
    }

    public PokerDecisionFacadeTestBuilder WithOpponentTracker(IOpponentTracker tracker)
    {
        _opponentTracker = tracker;
        return this;
    }

    public PokerDecisionFacadeTestBuilder WithMetrics(MetricsCollector metrics)
    {
        _metrics = metrics;
        return this;
    }

    public (PokerDecisionFacade facade, MetricsCollector metrics) Build()
    {
        EnsureServices();
        var facade = new PokerDecisionFacade(
            _calculator,
            _decisionService!,
            _betSizingService!,
            _boardTextureAnalyzer,
            _opponentTracker,
            _metrics);
        return (facade, _metrics);
    }

    private void EnsureServices()
    {
        if (_decisionService is null || _betSizingService is null)
        {
            var profile = Options.Create(new StrategyProfile().FillMissingThresholds());
            var betSizing = new BetSizingService(profile);
            _betSizingService ??= betSizing;
            var rangePolarizer = new RangePolarizer();
            var registry = new ThresholdsRegistry(profile);
            _decisionService ??= new PostflopDecisionService(profile, betSizing, rangePolarizer, registry);
        }
    }

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
}