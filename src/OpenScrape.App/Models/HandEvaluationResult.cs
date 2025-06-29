using OpenScrape.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OpenScrape.App.Models;

/// <summary>
/// Resultado de la evaluación de una mano
/// </summary>
public sealed record HandEvaluationResult
{
    [Required]
    public HandRanking HandRanking { get; init; }

    [Required]
    public List<NormalizedCard> BestFiveCards { get; init; } = [];

    [Required]
    public string HandDescription { get; init; } = string.Empty;

    [Range(0.0, 1.0)]
    public double HandStrength { get; init; }

    public List<NormalizedCard> Kickers { get; init; } = [];

    public List<NormalizedCard> AllCards { get; init; } = [];

    public bool IsStrongHand => HandStrength >= 0.75;
    public bool IsWeakHand => HandStrength <= 0.25;
}
