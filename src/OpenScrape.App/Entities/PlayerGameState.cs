using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Entities
{
    public class PlayerGameState
    {
        // Dealer information
        public bool IsDealer { get; set; }

        // Pot information
        public decimal PotSize { get; set; }

        // Player's hole cards
        public string HoleCard1Face { get; set; } = string.Empty;
        public string HoleCard2Face { get; set; } = string.Empty;
        public int HoleCard1Rank { get; set; } = 0;
        public int HoleCard2Rank { get; set; } = 0;
        public int HoleCard1Suit { get; set; } = 0;
        public int HoleCard2Suit { get; set; } = 0;
        public int Kicker { get; set; } = 0;

        // Position information
        public bool IsInPosition { get; set; } = false;
        public TablePosition Position { get; set; }

        // Betting information
        public decimal CurrentBet { get; set; }
        public decimal HeroStack { get; set; }

        // Game state
        public HandSituation HandSituation { get; set; }

        // Game data
        public List<Player> Players { get; set; } = new List<Player>();
        public List<BoardData> BoardCards { get; set; } = new List<BoardData>();

        // Computed properties
        public bool HavePocketPair => HoleCard1Rank == HoleCard2Rank;
        public bool IsSuited => HoleCard1Suit == HoleCard2Suit;
    }


    public class ResponseAction
    {
        public string? Action { get; set; }
        public HandSituation HandSituation { get; set; }
        public bool IsSecondAction { get; set; }
    }

    public class BoardData
    {
        public string? Name { get; set; }
        public int Force { get; set; }
        public int Suit { get; set; }
        public BoardPosition Position { get; set; }
        public int Location { get; set; }
    }


}
