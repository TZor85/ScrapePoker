namespace OpenScrape.Domain.Dtos;

public record SessionStatsDto(
    string Id,
    string SessionId,
    string TableName,
    DateTime StartTime,
    DateTime EndTime,
    decimal BigBlind,
    int TotalHands,
    decimal TotalProfit,
    double BBPer100);
