namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Snapshot de telemetría asociado a una mano concreta. Se persiste dentro del
/// <c>HandRecord</c> correspondiente en <c>HandRecord.Telemetry</c>.
/// </summary>
public sealed record TelemetryAggregate(
    string HandId,
    DateTime CapturedAt,
    IReadOnlyDictionary<string, CategoryStats> Phases);
