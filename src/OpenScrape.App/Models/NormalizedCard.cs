using OpenScrape.App.Entities;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OpenScrape.App.Models;

/// <summary>
/// Carta normalizada para evaluación
/// </summary>
public sealed record NormalizedCard
{
    [Required]
    public BoardData OriginalCard { get; init; } = null!;

    [Required]
    public Rank Rank { get; init; }

    [Required]
    public Suit Suit { get; init; }

    public override string ToString() => $"{Rank}{Suit}";
}
