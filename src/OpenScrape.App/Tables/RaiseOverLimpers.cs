namespace OpenScrape.App.Tables
{
    public class RaiseOverLimpers
    {

        #region Hands

        private static readonly HashSet<string> bigBlindVsSmallBlindHands = new HashSet<string>
        {
            "AA", "AKs", "AQs", "AJs", "ATs", "A9s", "A8s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s", "K7s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s",
            "AJo", "KJo", "QJo", "JJ", "JTs", "J9s",
            "ATo", "KTo", "QTo", "JTo", "TT", "T9s", "T8s",
            "A9o", "K9o", "T9o", "99", "98s", "97s",
            "A8o", "98o", "88", "87s", "86s",
            "87o", "77", "76s",
            "66", "65s",
            "A5o", "55", "54s",
            "A4o", "44",
            "33",
            "22"
        };

        private static readonly HashSet<string> bigBlindHands = new HashSet<string>
        {
            "AA", "AKs", "AQs", "AJs", "ATs", "A9s", "A8s", "A5s", "A4s", "A3s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s",
            "AQo", "KQo", "QQ", "QJs", "QTs",
            "AJo", "JJ", "JTs", "J9s",
            "TT", "T9s",
            "99", "98s",
            "88", "87s",
            "77",
            "66"
        };

        private static readonly HashSet<string> smallBlindHands = new HashSet<string>
        {
            "AA", "AKs", "AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s",
            "AJo", "JJ", "JTs", "J9s",
            "TT", "T9s", "T8s",
            "99", "98s",
            "88", "87s",
            "77",
            "66"
        };

        private static readonly HashSet<string> buttonHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s",
            "AQo", "KQo", "QQ", "QJs", "QTs",
            "AJo", "KJo", "QJo", "JJ", "JTs", "J9s",
            "ATo", "JTo", "TT", "T9s", "T8s",
            "99", "98s",
            "88", "87s",
            "77", "76s",
            "66", "65s",
            "55", "54s"
        };


        private static readonly HashSet<string> cutOffHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A8s", "A5s", "A4s", "A3s",
            "AKo", "KK", "KQs", "KJs", "KTs",
            "AQo", "KQo", "QQ", "QJs", "QTs",
            "AJo", "JJ", "JTs", "J9s",
            "TT", "T9s",
            "99", "98s",
            "88",
            "77",
            "66"
        };


        private static readonly HashSet<string> middleHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A5s", "A4s",
            "AKo", "KK", "KQs", "KJs",
            "AQo", "KQo", "QQ", "QJs",
            "AJo", "JJ", "JTs",
            "TT",
            "99",
            "88",
            "77"
        };

        #endregion

        public static string GetBigBlindVsSmallBlindHands(string hand)
        {
            return bigBlindVsSmallBlindHands.Contains(hand) ? "Raise Over Limper x4" : "Fold";
        }

        public static string GetBigBlindAction(string hand)
        {
            return bigBlindHands.Contains(hand) ? "Raise Over Limper x5" : "Fold";
        }

        public static string GetSmallBlindAction(string hand)
        {
            return smallBlindHands.Contains(hand) ? "Raise Over Limper x5" : "Fold";
        }

        public static string GetButtonAction(string hand)
        {
            return buttonHands.Contains(hand) ? "Raise Over Limper x5" : "Fold";
        }

        public static string GetCutOffAction(string hand)
        {
            return cutOffHands.Contains(hand) ? "Raise Over Limper x5" : "Fold";
        }

        public static string GetMiddleAction(string hand)
        {
            return middleHands.Contains(hand) ? "Raise Over Limper x5" : "Fold";
        }

    }
}
