using OpenScrape.App.Enums;

namespace OpenScrape.App.Entities
{
    public class TableScrapeResult
    {
        public double P1Bet { get; set;} = 0;
        public double P2Bet { get; set; } = 0;
        public double P3Bet { get; set; } = 0;
        public double P4Bet { get; set; } = 0;
        public double P5Bet { get; set; } = 0;
        public bool P0Dealer { get; set; }
        public bool P1Dealer { get; set; }
        public bool P2Dealer { get; set; }
        public bool P3Dealer { get; set; }
        public bool P4Dealer { get; set; }
        public bool P5Dealer { get; set; }
        public bool P1SitOut { get; set; }
        public bool P2SitOut { get; set; }
        public bool P3SitOut { get; set; }
        public bool P4SitOut { get; set; }
        public bool P5SitOut { get; set; }
        public bool P1Empty { get; set; }
        public bool P2Empty { get; set; }
        public bool P3Empty { get; set; }
        public bool P4Empty { get; set; }
        public bool P5Empty { get; set; }
        public string U0CardFace0 { get; set; } = string.Empty;
        public string U0CardFace1 { get; set; } = string.Empty;
        public int U0CardForce0 { get; set; } = 0;
        public int U0CardForce1 { get; set; } = 0;
        public int U0CardSuit0 { get; set; } = 0;
        public int U0CardSuit1 { get; set; } = 0;
        public HeroPosition P0Position { get; set; }
     
    }

    public class BackgroundTableScrap
    {
        public bool IsFlop { get; set; }
        public bool UserAction { get; set; }
        public bool UserAction1 { get; set; }
        public bool UserPlay { get; set; }
    }
}
