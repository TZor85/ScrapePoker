namespace OpenScrape.App.Entities
{
    public class TableScrapeResult
    {
        public string P0Chips { get; set; } = string.Empty;
        public string P1Chips { get; set; } = string.Empty;
        public string P2Chips { get; set; } = string.Empty;
        public double P1Bet { get; set;} = 0;
        public double P2Bet { get; set; } = 0;
        public bool P0Dealer { get; set; }
        public bool P1Dealer { get; set; }
        public bool P2Dealer { get; set; }
        public bool P1Active { get; set; }
        public bool P2Active { get; set; }
        public bool P1Sit { get; set; }
        public bool P2Sit { get; set; }
        public string U0CardFace0 { get; set; } = string.Empty;
        public string U0CardFace1 { get; set; } = string.Empty;
        public string B0Card1 { get; set; } = string.Empty;
        public string B0Card2 { get; set; } = string.Empty;
        public string B0Card3 { get; set; } = string.Empty;
        public string B0Card4 { get; set; } = string.Empty;
        public string B0Card5 { get; set; } = string.Empty;
    }
}
