using OpenScrape.App.Enums;

namespace OpenScrape.App.Entities
{
    public class TableScrapeResult
    {
        public bool P0Dealer { get; set; }
        public string U0CardFace0 { get; set; } = string.Empty;
        public string U0CardFace1 { get; set; } = string.Empty;
        public int U0CardForce0 { get; set; } = 0;
        public int U0CardForce1 { get; set; } = 0;
        public int U0CardSuit0 { get; set; } = 0;
        public int U0CardSuit1 { get; set; } = 0;
        public bool U0InPosition {  get; set; } = false;
        public bool HighCardInFlop { get; set; }
        public bool FlopIsCoordinate { get; set; } = false;
        public bool TurnAndFlopIsCoordinate { get; set; } = false;
        public bool RiverAndTurnAndFlopIsCoordinate { get; set; } = false;
        public bool HavePairOnHand { get; set; }
        public HeroHand Hand { get; set; }
        public decimal U0Bet {  get; set; }
        public HeroPosition P0Position { get; set; }
        public HeroAction HeroAction { get; set; }
        public List<PlayerData> DataPlayer {  get; set; } = new List<PlayerData>();
        public List<BoardData> DataBoard { get; set; } = new List<BoardData>();
     
    }
    
    public class PlayerData
    {
        public string? Name { get; set; }
        public bool Dealer { get; set; }
        public decimal Bet { get; set; }
        public bool Active { get; set; }
        public bool SitOut { get; set; }
        public bool Empty { get; set; }
        public bool BigBlind { get; set; }
        public bool SmallBlind { get; set; }
        public HeroPosition Position { get; set; }
        public int ValuePosition { get; set; }
    }

    public class ResponseAction
    {
        public string? Action { get; set; }
        public HeroAction HeroAction { get; set; }
        public bool IsSecondAction { get; set; }
    }

    public class BoardData
    {
        public string? Name { get; set;}
        public int Force { get; set; }
        public int Suit { get; set; }
        public BoardPosition Position { get; set; }

    }
}
