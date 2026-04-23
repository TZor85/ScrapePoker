using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Vista inmutable del estado del recolector en un instante dado.
/// </summary>
public sealed record MetricsSnapshot(
    string? CurrentHandId,
    IReadOnlyDictionary<string, CategoryStats> LastHand,
    IReadOnlyDictionary<string, CategoryStats> Session);
