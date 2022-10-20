namespace OpenScrape.App.Entities
{
    public class TableScrapeResult
    {
        public string P0Chips { get; set; } = string.Empty;
        public string P1Chips { get; set; } = string.Empty;
        public string P2Chips { get; set; } = string.Empty;
        public bool P0Dealer { get; set; }
        public bool P1Dealer { get; set; }
        public bool P2Dealer { get; set; }
        public bool P1Active { get; set; }
        public bool P2Active { get; set; }
        public bool P1Sit { get; set; }
        public bool P2Sit { get; set; }
        public string U0CardFace0 { get; set; } = string.Empty;
        public string U0CardFace1 { get; set; } = string.Empty;
    }
}
