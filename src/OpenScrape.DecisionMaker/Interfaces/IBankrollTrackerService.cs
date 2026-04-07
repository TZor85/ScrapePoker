using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de seguimiento de bankroll.
/// </summary>
public interface IBankrollTrackerService
{
    BankrollStats GetBankrollStats(int sessionCount = 100);
    void RecordSessionEnd(GameSession session);
    decimal GetCurrentBankroll();
    decimal GetPeakBankroll();
    (double riskOfRuin, string level) CalculateRiskOfRuin();
    string GetLimitRecommendation();
    List<BankrollHistoryItem> GetHistory(int count = 50);
    void SetInitialBankroll(decimal amount);
}
