using OpenScrape.Domain.Entities;

namespace OpenScrape.Contracts;

/// <summary>
/// Perfil observable del villano. Es un hecho que manda el cliente: el servidor no acumula estadísticas.
/// </summary>
public sealed record VillainProfile
{
    public string VillainId { get; init; } = "Unknown";
    public OpponentType Type { get; init; } = OpponentType.Unknown;
    public double FoldToBetPct { get; init; } = -1;
    public double AdjustedFoldEquity { get; init; } = -1;
    public int HandsPlayed { get; init; }
    public int TimesVoluntarilyPutMoneyIn { get; init; }
}
