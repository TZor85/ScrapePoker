using OpenScrape.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OpenScrape.App.Entities
{
    /// <summary>
    /// Representa el análisis completo de una situación post-flop en poker
    /// </summary>
    public class TableScrapeFlopResult
    {
        // === ANÁLISIS DEL BOARD ===
        [Required]
        public BoardTexture BoardTexture { get; set; } = new();

        // === ANÁLISIS DE LA MANO DEL HÉROE ===
        [Required]
        public HeroHandStrength HeroStrength { get; set; } = new();

        // === ANÁLISIS DE DRAWS ===
        [Required]
        public DrawingOpportunities Draws { get; set; } = new();

        // === MÉTODOS DE CONVENIENCIA ===
        public bool HasStrongHand => HeroStrength.HasTopPair ||
                                   HeroStrength.HasOverPair ||
                                   HeroStrength.HasTwoPair ||
                                   HeroStrength.Hand >= HeroHand.DoblePareja;

        public bool HasWeakHand => HeroStrength.Hand <= HeroHand.CartaAlta;

        public bool ShouldContinue => HasStrongHand || Draws.HasStrongDraw;

    }


    /// <summary>
    /// Textura y características del board
    /// </summary>
    public class BoardTexture
    {
        public bool IsCoordinated { get; set; }
        public bool IsRainbow { get; set; }
        public bool IsConnected { get; set; }
        public bool IsPaired { get; set; }
        public bool IsDry { get; set; }

        [Range(2, 14)]
        public int HighestRank { get; set; }

        [Range(2, 14)]
        public int LowestRank { get; set; }

        public bool HasAce { get; set; }
        public bool HasKing { get; set; }
    }

    /// <summary>
    /// Fuerza de la mano del héroe
    /// </summary>
    public class HeroHandStrength
    {
        [Required]
        public HeroHand Hand { get; set; }

        public bool HasHighCard { get; set; }
        public bool HasTopPair { get; set; }
        public bool HasMiddlePair { get; set; }
        public bool HasBottomPair { get; set; }
        public bool HasOverPair { get; set; }
        public bool HasTwoPair { get; set; }
        public bool HasOverCards { get; set; }
        public bool HasNoOverCards { get; set; }
        public bool HandIsConnected { get; set; }
    }

    /// <summary>
    /// Oportunidades de mejora (draws)
    /// </summary>
    public class DrawingOpportunities
    {
        public bool HasFlushDraw { get; set; }
        public bool HasStraightDraw { get; set; }
        public bool HasBackdoorFlushDraw { get; set; }
        public bool HasDrawingHand { get; set; }

        public bool HasStrongDraw => HasFlushDraw || HasStraightDraw;
        public bool HasWeakDraw => HasBackdoorFlushDraw && !HasStrongDraw;
    }
}
