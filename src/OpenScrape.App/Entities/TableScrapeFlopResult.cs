using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Entities
{
    public class TableScrapeFlopResult
    {
        public bool HasHighCard { get; set; }
        public bool FlushDrawInFlop { get; set; }
        public bool StraightDrawInFlop { get; set; }
        public bool IsCoordinated { get; set; }
        //public bool HavePocketPair { get; set; }
        //public bool HandIsSuited { get; set; }
        public bool HandIsConnected { get; set; }
        public bool HasTopPair { get; set; }
        public bool HasMiddlePair { get; set; }
        public bool HasBottomPair { get; set; }
        public bool HasOverPair { get; set; }
        public bool HasTwoPair { get; set; }
        public bool HaveBackdoorFlushDraw { get; set; }
        public bool HaveHighCards { get; set; }
        public bool HasOverCards { get; set; }
        public bool IsRainbow { get; set; }
        public bool IsConnected { get; set; }
        public bool IsPaired { get; set; }
        public bool IsDry { get; set; }
        public bool HaveAce { get; set; }
        public bool HaveKing { get; set; }
        public bool NoOverCards { get; set; }
        public bool HaveDrawingHand { get; set; }
        public bool HaveFlushDraw { get; set; }
        public bool HaveStraightDraw { get; set; }
        public int GetHighestRank { get; set; }
        public int GetLowestRank { get; set; }
        public HeroHand Hand { get; set; }


        public bool StrongHand()
        {
            return HasTopPair || HasOverPair || HasTwoPair || Hand >= HeroHand.DoblePareja;
        }

        public bool LowHand()
        {
            return Hand <= HeroHand.CartaAlta;
        }
    }
}
