using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Preflop
{
    public class BaseRequest
    {
        public string Hand { get; set; } = default!;
        public TablePosition Position { get; set; }
    }
}
