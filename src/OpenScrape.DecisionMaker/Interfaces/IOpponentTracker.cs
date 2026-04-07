using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de seguimiento de oponentes.
/// </summary>
public interface IOpponentTracker
{
    IReadOnlyDictionary<string, OpponentProfile> AllProfiles { get; }

    OpponentProfile GetProfile(string playerId);
    void RecordHandPlayed(string playerId);
    void RecordVPIP(string playerId);
    void RecordPFR(string playerId);
    void RecordThreeBet(string playerId);
    void RecordPostflopAction(string playerId, PostflopAction action, bool? isVillainInPosition = null);
    void RecordCBetOpportunity(string playerId, bool didCBet);
    void RecordFacedCBet(string playerId, bool folded);
    double GetAdjustedFoldEquity(string playerId, double baseFoldEquity);
    double GetFoldToBetPct(string playerId);
    void RegisterSeatAlias(string seatName, string alias);
    string? ResolveName(string seatName);
    void Reset();
}
