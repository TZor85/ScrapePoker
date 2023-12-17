namespace OpenScrape.App.Tables
{
    public static class OpenRaises
    {
        #region Hands
        private static readonly HashSet<string> smallBlindHands = new HashSet<string>
        {
            "AA", "AKs", "AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s", "K7s", "K6s", "K5s", "K4s", "K3s", "K2s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s", "Q8s", "Q7s", "Q6s", "Q5s", "Q4s", "Q3s", "Q2s",
            "AJo", "KJo", "QJo", "JJ", "JTs", "J9s", "J8s", "J7s", "J6s", "J5s", "J4s",
            "ATo", "KTo", "QTo", "JTo", "TT", "T9s", "T8s", "T7s", "T6s",
            "A9o", "K9o", "Q9o", "J9o", "T9o", "99", "98s", "97s", "96s",
            "A8o", "K8o", "98o", "88", "87s", "86s", "85s",
            "A7o", "77", "76s", "75s",
            "A6o", "66", "65s", "64s",
            "A5o", "55", "54s", "53s",
            "A4o", "44",
            "33",
            "22"
        };

        private static readonly HashSet<string> buttonHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s", "K7s", "K6s", "K5s", "K4s", "K3s", "K2s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s", "Q8s", "Q7s", "Q6s", "Q5s", "Q4s", "Q3s",
            "AJo", "KJo", "QJo", "JJ", "JTs", "J9s", "J8s", "J7s", "J6s", "J5s", "J4s",
            "ATo", "KTo", "QTo", "JTo", "TT", "T9s", "T8s", "T7s", "T6s",
            "A9o", "K9o", "Q9o", "J9o", "T9o", "99", "98s", "97s", "96s",
            "A8o", "88", "87s", "86s",
            "A7o", "77", "76s", "75s",
            "A6o", "66", "65s", "64s",
            "A5o", "55", "54s",
            "A4o", "44",
            "33",
            "22"
        };

        private static readonly HashSet<string> cutOffHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s", "K7s", "K6s", "K5s", "K4s", "K3s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s", "Q8s", "Q7s", "Q6s",
            "AJo", "KJo", "QJo", "JJ", "JTs", "J9s", "J8s", "J7s",
            "ATo", "KTo", "QTo", "JTo", "TT", "T9s", "T8s",
            "A9o", "99", "98s", "97s",
            "A8o", "88", "87s",
            "77", "76s",
            "66",
            "55",
            "44"            
        };

        private static readonly HashSet<string> middleHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s", "K7s", "K6s", "K5s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s", "Q8s",
            "AJo", "KJo", "QJo", "JJ", "JTs", "J9s",
            "ATo", "TT", "T9s",
            "A9o", "99", "98s",
            "88",
            "77",
            "66"
        };

        private static readonly HashSet<string> earlyHands = new HashSet<string>
        {
            "AA", "AKs","AQs", "AJs", "ATs", "A9s", "A8s", "A7s", "A6s", "A5s", "A4s", "A3s", "A2s",
            "AKo", "KK", "KQs", "KJs", "KTs", "K9s", "K8s",
            "AQo", "KQo", "QQ", "QJs", "QTs", "Q9s",
            "AJo", "KJo", "JJ", "JTs",
            "ATo", "TT", "T9s",
            "99",
            "88",
            "77"
        };

        #endregion

        public static string GetSmallBlindAction(string hand)
        {
            return smallBlindHands.Contains(hand) ? "Open Raise x2.6" : "Fold";
        }

        public static string GetButtonAction(string hand)
        {
            return buttonHands.Contains(hand) ? "Open Raise x2.4" : "Fold";
        }

        public static string GetCutOffAction(string hand)
        {
            return cutOffHands.Contains(hand) ? "Open Raise x2.2" : "Fold";
        }

        public static string GetMiddleAction(string hand)
        {
            return middleHands.Contains(hand) ? "Open Raise x2.1" : "Fold";
        }

        public static string GetEarlyAction(string hand)
        {
            return earlyHands.Contains(hand) ? "Open Raise x2" : "Fold";
        }
    }
}
