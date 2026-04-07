namespace OpenScrape.Domain.ValueObjects;

public class BankrollSnapshot
{
    public DateTime Timestamp { get; set; }
    public decimal Bankroll { get; set; }
    public decimal BigBlind { get; set; }
    public decimal SessionProfit { get; set; }
    public int SessionHands { get; set; }
    public string SessionId { get; set; } = string.Empty;
}

public class BankrollStats
{
    public decimal CurrentBankroll { get; set; }
    public decimal StartingBankroll { get; set; }
    public decimal PeakBankroll { get; set; }
    public decimal MaxDrawdown { get; set; }
    public double MaxDrawdownPercent { get; set; }
    public double WinRateBB100 { get; set; }
    public double StdDeviation { get; set; }
    public int TotalSessions { get; set; }
    public int WinningSessions { get; set; }
    public int TotalHands { get; set; }
    public double RiskOfRuin { get; set; }
    public decimal AverageSessionProfit { get; set; }
    public string LimitRecommendation { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Green";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class BankrollHistoryItem
{
    public DateTime Date { get; set; }
    public int Hands { get; set; }
    public decimal Profit { get; set; }
    public double BBPer100 { get; set; }
    public decimal BankrollAfter { get; set; }
}
