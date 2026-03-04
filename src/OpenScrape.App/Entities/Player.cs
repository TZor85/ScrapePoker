using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Entities
{
    public class Player
    {
        public string? Name { get; set; }
        public string? Alias { get; set; }
        public bool Dealer { get; set; }
        public decimal Bet { get; set; }
        public decimal Stack { get; set; }
        public bool Active { get; set; }
        public bool SitOut { get; set; }
        public bool Empty { get; set; }
        public bool BigBlind { get; set; }
        public bool SmallBlind { get; set; }
        public TablePosition Position { get; set; }
        public int ValuePosition { get; set; }
    }
}
