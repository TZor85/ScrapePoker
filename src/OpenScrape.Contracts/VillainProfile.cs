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

    /// <summary>
    /// Indica si el perfil tiene muestras suficientes de agresión postflop. El
    /// servidor lo usa para decidir si ajusta la fold equity por tipo de rival.
    /// </summary>
    public bool HasReliableAggressionData { get; init; }

    public int HandsPlayed { get; init; }
    public int TimesVoluntarilyPutMoneyIn { get; init; }

    public int TimesPreflopRaised { get; init; }

    public int TimesThreeBet { get; init; }
}
