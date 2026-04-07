using OpenScrape.Domain.Enums;

namespace OpenScrape.Domain.ValueObjects
{
    public class HandEvaluation
    {
        public HandRank Rank { get; set; }
        public long Score { get; set; }
        public List<CardDataOuts> Cards { get; set; }
        public List<int> Kickers { get; set; }

        public HandEvaluation()
        {
            Cards = new List<CardDataOuts>();
            Kickers = new List<int>();
        }
    }
}
