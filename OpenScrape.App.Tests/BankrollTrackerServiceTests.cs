using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class BankrollTrackerServiceTests
{
    private StrategyProfile _profile = null!;

    [SetUp]
    public void Setup()
    {
        _profile = new StrategyProfile
        {
            InitialBankroll = 100m,
            BuyInMax = 2.0m,
            MinSessionsForRecommendation = 20,
            RiskOfRuinThreshold = 0.05
        };
    }

    #region CalculateRiskOfRuinInternal

    [Test]
    public void CalculateRiskOfRuin_HighWinRate_ReturnsLowRisk()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 50,
            CurrentBankroll = 200m,
            WinRateBB100 = 10.0,
            StdDeviation = 20.0
        };

        var (ror, level) = sut.CalculateRiskOfRuinInternal(stats);

        // brBb=200/0.02=10000, wr=0.1, sigma=20: exp(-2*10000*0.1/400) = exp(-5) ≈ 0.0067
        Assert.That(ror, Is.LessThan(0.05), "RoR con win rate alto debería ser bajo");
        Assert.That(level, Is.EqualTo("Green"));
    }

    [Test]
    public void CalculateRiskOfRuin_NegativeWinRate_ReturnsHighRisk()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 50,
            CurrentBankroll = 100m,
            WinRateBB100 = -5.0,
            StdDeviation = 30.0
        };

        var (ror, level) = sut.CalculateRiskOfRuinInternal(stats);

        Assert.That(ror, Is.EqualTo(1.0));
        Assert.That(level, Is.EqualTo("Red"));
    }

    [Test]
    public void CalculateRiskOfRuin_ZeroStdDev_ReturnsHighRisk()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 30,
            CurrentBankroll = 100m,
            WinRateBB100 = 5.0,
            StdDeviation = 0
        };

        var (ror, level) = sut.CalculateRiskOfRuinInternal(stats);

        Assert.That(ror, Is.EqualTo(1.0));
        Assert.That(level, Is.EqualTo("Red"));
    }

    [Test]
    public void CalculateRiskOfRuin_FewSessions_ReturnsGreen()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 5,
            CurrentBankroll = 100m,
            WinRateBB100 = -10.0,
            StdDeviation = 50.0
        };

        var (ror, level) = sut.CalculateRiskOfRuinInternal(stats);

        Assert.That(ror, Is.EqualTo(0));
        Assert.That(level, Is.EqualTo("Green"), "Pocos datos no deberían generar alarma");
    }

    [Test]
    public void CalculateRiskOfRuin_MediumRisk_ReturnsYellow()
    {
        var sut = CreateService();
        // Ajustamos para obtener RoR entre 5% y 15%
        // Con BB=0.02, bankroll=100, brBb=5000, WR=2/100=0.02, sigma=80
        // exp(-2 * 5000 * 0.02 / (80*80)) = exp(-200/6400) = exp(-0.03125) ≈ 0.969
        // Eso es demasiado alto. Mejor con bankroll bajo y WR medio
        // brBb=50 (bankroll=1m), WR=5/100=0.05, sigma=40
        // exp(-2*50*0.05/(40*40)) = exp(-5/1600) = exp(-0.003125) ≈ 0.997
        // Necesitamos brBb bajo. Con BB=1.0 y bankroll=10:
        sut.SetInitialBankroll(10m);
        // _lastBigBlind=0.02, brBb = 10/0.02 = 500
        // WR=1/100=0.01, sigma=30: exp(-2*500*0.01/(30*30)) = exp(-10/900) = exp(-0.011) ≈ 0.989
        // Sigue alto. Para Yellow necesitamos RoR 0.05-0.15
        // exp(x) = 0.10 → x = ln(0.10) = -2.302
        // -2*brBb*wr/sigma^2 = -2.302
        // Con brBb=500, wr=0.05, sigma=s → -50/s^2 = -2.302 → s^2 = 21.72 → s = 4.66
        var stats = new BankrollStats
        {
            TotalSessions = 30,
            CurrentBankroll = 10m,
            WinRateBB100 = 5.0,
            StdDeviation = 4.66
        };

        var (ror, level) = sut.CalculateRiskOfRuinInternal(stats);

        Assert.That(ror, Is.GreaterThanOrEqualTo(0.05));
        Assert.That(ror, Is.LessThan(0.15));
        Assert.That(level, Is.EqualTo("Yellow"));
    }

    #endregion

    #region GetLimitRecommendationInternal

    [Test]
    public void GetLimitRecommendation_BankrollSolid_ReturnsSubir()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 30,
            CurrentBankroll = 100m, // 100 >= 25 * 2.0 = 50
            WinRateBB100 = 8.0,
            RiskOfRuin = 0.02, // < 0.05
            MaxDrawdownPercent = 5.0
        };

        var result = sut.GetLimitRecommendationInternal(stats);

        Assert.That(result, Does.StartWith("SUBIR"));
    }

    [Test]
    public void GetLimitRecommendation_Downward_ReturnsBajar()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 30,
            CurrentBankroll = 50m,
            WinRateBB100 = 2.0,
            RiskOfRuin = 0.25, // > 0.15
            MaxDrawdownPercent = 25.0
        };

        var result = sut.GetLimitRecommendationInternal(stats);

        Assert.That(result, Does.StartWith("BAJAR"));
    }

    [Test]
    public void GetLimitRecommendation_NegativeWinRate_ReturnsAnalizar()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 30,
            CurrentBankroll = 80m,
            WinRateBB100 = -3.0,
            RiskOfRuin = 0.5,
            MaxDrawdownPercent = 10.0
        };

        var result = sut.GetLimitRecommendationInternal(stats);

        Assert.That(result, Does.StartWith("ANALIZAR"));
    }

    [Test]
    public void GetLimitRecommendation_FewSessions_ReturnsDatos()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 5,
            CurrentBankroll = 100m,
            WinRateBB100 = 10.0,
            RiskOfRuin = 0.01
        };

        var result = sut.GetLimitRecommendationInternal(stats);

        Assert.That(result, Does.StartWith("DATOS"));
    }

    [Test]
    public void GetLimitRecommendation_AcceptableRisk_ReturnsMantener()
    {
        var sut = CreateService();
        var stats = new BankrollStats
        {
            TotalSessions = 30,
            CurrentBankroll = 30m, // 30 < 50 (25*2), no alcanza para SUBIR
            WinRateBB100 = 3.0,
            RiskOfRuin = 0.10, // entre 0.05 y 0.15
            MaxDrawdownPercent = 8.0
        };

        var result = sut.GetLimitRecommendationInternal(stats);

        Assert.That(result, Does.StartWith("MANTENER"));
    }

    #endregion

    #region RecordSessionEnd & Bankroll Tracking

    [Test]
    public void RecordSessionEnd_UpdatesBankrollAndPeak()
    {
        var sut = CreateService();
        sut.SetInitialBankroll(100m);

        var session = CreateSession(profitPerHand: 5m, handCount: 3);
        sut.RecordSessionEnd(session);

        Assert.That(sut.GetCurrentBankroll(), Is.EqualTo(115m)); // 100 + 3*5
        Assert.That(sut.GetPeakBankroll(), Is.EqualTo(115m));
    }

    [Test]
    public void RecordSessionEnd_LossDoesNotUpdatePeak()
    {
        var sut = CreateService();
        sut.SetInitialBankroll(100m);

        var winSession = CreateSession(profitPerHand: 10m, handCount: 2);
        sut.RecordSessionEnd(winSession);
        Assert.That(sut.GetPeakBankroll(), Is.EqualTo(120m));

        var lossSession = CreateSession(profitPerHand: -5m, handCount: 3);
        sut.RecordSessionEnd(lossSession);

        Assert.That(sut.GetCurrentBankroll(), Is.EqualTo(105m));
        Assert.That(sut.GetPeakBankroll(), Is.EqualTo(120m), "Peak no debería bajar");
    }

    [Test]
    public void SetInitialBankroll_SetsCurrentAndPeak()
    {
        var sut = CreateService();
        sut.SetInitialBankroll(250m);

        Assert.That(sut.GetCurrentBankroll(), Is.EqualTo(250m));
        Assert.That(sut.GetPeakBankroll(), Is.EqualTo(250m));
    }

    #endregion

    #region MaxDrawdown (spec: Peak - Minimum después del peak)

    [Test]
    public void GetMaxDrawdown_TracksPeakAndValley()
    {
        var sut = CreateService();
        sut.SetInitialBankroll(100m);

        // Subir a 120, bajar a 105, subir a 130, bajar a 110
        sut.RecordSessionEnd(CreateSession(profitPerHand: 20m, handCount: 1)); // 120
        sut.RecordSessionEnd(CreateSession(profitPerHand: -15m, handCount: 1)); // 105
        sut.RecordSessionEnd(CreateSession(profitPerHand: 25m, handCount: 1)); // 130
        sut.RecordSessionEnd(CreateSession(profitPerHand: -20m, handCount: 1)); // 110

        // Peak=130, valley después del peak=110, drawdown=20
        Assert.That(sut.GetPeakBankroll(), Is.EqualTo(130m));
        Assert.That(sut.GetCurrentBankroll(), Is.EqualTo(110m));
    }

    #endregion

    #region Helpers

    private BankrollTrackerService CreateService()
    {
        // Usar un store nulo — solo testamos métodos internos que no acceden a BD
        return new BankrollTrackerService(null!, _profile);
    }

    private static GameSession CreateSession(decimal profitPerHand, int handCount)
    {
        var session = new GameSession
        {
            BigBlind = 0.02m
        };

        for (int i = 0; i < handCount; i++)
        {
            session.Hands.Add(new HandRecord
            {
                HeroStackStart = 100m,
                HeroStackEnd = 100m + profitPerHand
            });
        }

        return session;
    }

    #endregion
}
