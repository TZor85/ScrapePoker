using OpenScrape.DecisionMaker.Services;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Interfaces;

/// <summary>
/// Contrato para el servicio de seguimiento de oponentes.
/// </summary>
public interface IOpponentTracker
{
    IReadOnlyDictionary<string, OpponentProfile> AllProfiles { get; }

    OpponentProfile GetProfile(string playerId);
    void RecordHandPlayed(string playerId, TablePosition position = TablePosition.None);
    void RecordVPIP(string playerId, TablePosition position = TablePosition.None);
    void RecordPFR(string playerId, TablePosition position = TablePosition.None);
    void RecordThreeBet(string playerId);
    void RecordPostflopAction(string playerId, PostflopAction action, bool? isVillainInPosition = null);
    void RecordCBetOpportunity(string playerId, bool didCBet);
    void RecordFacedCBet(string playerId, bool folded);
    double GetAdjustedFoldEquity(string playerId, double baseFoldEquity);
    double GetFoldToBetPct(string playerId);
    void TrackShowdownResult(string playerId, bool wentToSD, bool wonSD);
    void TrackCheckRaise(string playerId, bool didCR, bool hadOpportunity);
    void TrackDonkBet(string playerId, bool didDonk, bool hadOpportunity);
    void TrackBarrel(string playerId, bool didBarrel);
    void RegisterSeatAlias(string seatName, string alias);
    string? ResolveName(string seatName);
    void Reset();
}
