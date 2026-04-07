using Marten;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Services;

public class BankrollTrackerService : Interfaces.IBankrollTrackerService
{
    private readonly IDocumentStore _store;
    private readonly StrategyProfile _profile;
    private decimal _currentBankroll;
    private decimal _peakBankroll;
    private int MinSessionsForRecommendation => _profile.MinSessionsForRecommendation;
    private const int MaxSessionsForStats = 100;

    public BankrollTrackerService(IDocumentStore store, StrategyProfile profile)
    {
        _store = store;
        _profile = profile;
        InitializeBankroll();
    }

    private void InitializeBankroll()
    {
        _currentBankroll = _profile.InitialBankroll;
        _peakBankroll = _profile.InitialBankroll;
    }

    public BankrollStats GetBankrollStats(int sessionCount = 100)
    {
        var stats = new BankrollStats();

        try
        {
            using var session = _store.QuerySession();
            var sessions = session.Query<GameSession>()
                .OrderByDescending(s => s.EndTime)
                .Take(sessionCount)
                .ToList();

            if (sessions.Count == 0)
            {
                return GetDefaultStats();
            }

            stats.TotalSessions = sessions.Count;

            var sessionsOrdered = sessions.OrderBy(s => s.EndTime).ToList();
            stats.StartingBankroll = sessionsOrdered.First().StartingBankroll;
            stats.CurrentBankroll = sessionsOrdered.Last().EndingBankroll;

            if (stats.CurrentBankroll > 0)
            {
                _currentBankroll = stats.CurrentBankroll;
            }
            if (stats.CurrentBankroll > _peakBankroll || _peakBankroll == 0)
            {
                _peakBankroll = stats.CurrentBankroll;
                stats.PeakBankroll = stats.CurrentBankroll;
            }
            else
            {
                stats.PeakBankroll = _peakBankroll;
            }

            stats.MaxDrawdown = _peakBankroll - stats.CurrentBankroll;
            stats.MaxDrawdownPercent = _peakBankroll > 0
                ? (double)(_peakBankroll - stats.CurrentBankroll) / (double)_peakBankroll * 100
                : 0;

            var totalHands = 0;
            var totalProfit = 0m;
            var winningSessions = 0;
            var bbPer100List = new List<double>();

            foreach (var s in sessionsOrdered)
            {
                var hands = session.Query<HandRecord>()
                    .Where(h => h.GameSessionId == s.Id)
                    .ToList();

                var handCount = hands.Count;
                var profit = hands.Sum(h => h.HeroStackEnd - h.HeroStackStart);
                totalHands += handCount;
                totalProfit += profit;

                if (profit > 0)
                    winningSessions++;

                if (handCount > 0 && s.BigBlind > 0)
                {
                    var bb100 = (double)(profit / s.BigBlind) / handCount * 100;
                    bbPer100List.Add(bb100);
                }
            }

            stats.TotalHands = totalHands;
            stats.WinningSessions = winningSessions;
            stats.AverageSessionProfit = sessions.Count > 0 ? totalProfit / sessions.Count : 0;

            if (bbPer100List.Count > 0)
            {
                stats.WinRateBB100 = bbPer100List.Average();
                if (bbPer100List.Count > 1)
                {
                    var mean = bbPer100List.Average();
                    double sumSquares = 0;
                    foreach (var val in bbPer100List)
                    {
                        sumSquares += Math.Pow(val - mean, 2);
                    }
                    stats.StdDeviation = Math.Sqrt(sumSquares / (bbPer100List.Count - 1));
                }
            }

            var (ror, level) = CalculateRiskOfRuinInternal(stats);
            stats.RiskOfRuin = ror;
            stats.RiskLevel = level;

            stats.LimitRecommendation = GetLimitRecommendationInternal(stats);
            stats.LastUpdated = DateTime.UtcNow;
        }
        catch
        {
            return GetDefaultStats();
        }

        return stats;
    }

    private BankrollStats GetDefaultStats()
    {
        return new BankrollStats
        {
            CurrentBankroll = _currentBankroll,
            StartingBankroll = _currentBankroll,
            PeakBankroll = _peakBankroll,
            MaxDrawdown = 0,
            MaxDrawdownPercent = 0,
            WinRateBB100 = 0,
            StdDeviation = 0,
            TotalSessions = 0,
            WinningSessions = 0,
            TotalHands = 0,
            RiskOfRuin = 1.0,
            RiskLevel = "Green",
            LimitRecommendation = "DATOS: Collecting more data...",
            LastUpdated = DateTime.UtcNow
        };
    }

    public void RecordSessionEnd(GameSession session)
    {
        var profit = session.Hands.Sum(h => h.HeroStackEnd - h.HeroStackStart);
        _currentBankroll += profit;

        if (_currentBankroll > _peakBankroll)
            _peakBankroll = _currentBankroll;
    }

    public decimal GetCurrentBankroll() => _currentBankroll;

    public decimal GetPeakBankroll() => _peakBankroll;

    public (double riskOfRuin, string level) CalculateRiskOfRuin()
    {
        var stats = GetBankrollStats();
        return CalculateRiskOfRuinInternal(stats);
    }

    private (double riskOfRuin, string level) CalculateRiskOfRuinInternal(BankrollStats stats)
    {
        if (stats.TotalSessions < MinSessionsForRecommendation)
            return (0, "Green");

        if (stats.WinRateBB100 <= 0 || stats.StdDeviation <= 0)
            return (1.0, "Red");

        var bb = stats.CurrentBankroll / 2m;
        var brBb = (double)bb;

        if (brBb <= 0)
            return (1.0, "Red");

        var wr = stats.WinRateBB100 / 100.0;
        var sigma = stats.StdDeviation;

        if (sigma <= 0)
            return (1.0, "Red");

        var exponent = -2 * brBb * wr / (sigma * sigma);

        if (exponent < -700)
            exponent = -700;

        var ror = Math.Exp(exponent);

        var level = ror switch
        {
            < 0.05 => "Green",
            < 0.15 => "Yellow",
            _ => "Red"
        };

        return (ror, level);
    }

    public string GetLimitRecommendation()
    {
        var stats = GetBankrollStats();
        return GetLimitRecommendationInternal(stats);
    }

    private string GetLimitRecommendationInternal(BankrollStats stats)
    {
        if (stats.TotalSessions < MinSessionsForRecommendation)
            return $"DATOS: Collecting more data... ({stats.TotalSessions}/{MinSessionsForRecommendation})";

        if (stats.WinRateBB100 < 0)
            return "ANALIZAR: Win rate negativo";

        if (stats.RiskOfRuin > 0.15 || stats.MaxDrawdownPercent > 20)
            return "BAJAR: Riesgo alto";

        var buyInMax = _profile.BuyInMax;
        var minBankrollForUp = buyInMax * 25;

        if (stats.RiskOfRuin < _profile.RiskOfRuinThreshold && stats.CurrentBankroll >= minBankrollForUp)
            return "SUBIR: Bankroll sólido";

        if (stats.RiskOfRuin < 0.15)
            return "MANTENER: Riesgo aceptable";

        return "MANTENER: Necesita más datos";
    }

    public List<BankrollHistoryItem> GetHistory(int count = 50)
    {
        var result = new List<BankrollHistoryItem>();

        try
        {
            using var session = _store.QuerySession();
            var sessions = session.Query<GameSession>()
                .OrderByDescending(s => s.EndTime)
                .Take(count)
                .ToList();

            var bankroll = sessions.FirstOrDefault()?.StartingBankroll ?? 100m;

            foreach (var s in sessions.OrderBy(s => s.EndTime))
            {
                var hands = session.Query<HandRecord>()
                    .Where(h => h.GameSessionId == s.Id)
                    .ToList();

                var profit = hands.Sum(h => h.HeroStackEnd - h.HeroStackStart);
                bankroll += profit;

                result.Add(new BankrollHistoryItem
                {
                    Date = s.EndTime,
                    Hands = hands.Count,
                    Profit = profit,
                    BBPer100 = hands.Count > 0 && s.BigBlind > 0
                        ? (double)(profit / s.BigBlind) / hands.Count * 100
                        : 0,
                    BankrollAfter = bankroll
                });
            }
        }
        catch
        {
            // Return empty list on error
        }

        return result.OrderByDescending(r => r.Date).ToList();
    }

    public void SetInitialBankroll(decimal amount)
    {
        _currentBankroll = amount;
        _peakBankroll = amount;
    }
}
