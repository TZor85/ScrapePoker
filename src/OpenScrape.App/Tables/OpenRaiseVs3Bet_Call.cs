using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tables
{
    public class OpenRaiseVs3Bet_Call
    {
        public static string GetOpenRaiseBTNvs3BetSBAndBBCall(string hand)
        { 
            return OpenRaiseBTNvs3BetSBAndBBCall(hand);
        }

        public static string GetOpenRaiseCOvs3BetSBAndBBCall(string hand)
        {
            return OpenRaiseCOvs3BetSBAndBBCall(hand);
        }

        public static string GetOpenRaiseCOvs3BetBTNAndBBCall(string hand)
        {
            return OpenRaiseCOvs3BetBTNAndBBCall(hand);
        }

        public static string GetOpenRaiseCOvs3BetBTNAndSBCall(string hand)
        {
            return OpenRaiseCOvs3BetBTNAndSBCall(hand);
        }

        public static string GetOpenRaiseMPvs3BetSBAndBBCall(string hand)
        {
            return OpenRaiseMPvs3BetSBAndBBCall(hand);
        }

        public static string GetOpenRaiseMPvs3BetBTNAndBBCall(string hand)
        {
            return OpenRaiseMPvs3BetBTNAndBBCall(hand);
        }

        public static string GetOpenRaiseMPvs3BetBTNAndSBCall(string hand)
        {
            return OpenRaiseMPvs3BetBTNAndSBCall(hand);
        }

        public static string GetOpenRaiseMPvs3BetCOAndBBCall(string hand)
        {
            return OpenRaiseMPvs3BetCOAndBBCall(hand);
        }

        public static string GetOpenRaiseMPvs3BetCOAndSBCall(string hand)
        {
            return OpenRaiseMPvs3BetCOAndSBCall(hand);
        }

        public static string GetOpenRaiseMPvs3BetCOAndBTNCall(string hand)
        {
            return OpenRaiseMPvs3BetCOAndBTNCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetSBAndBBCall(string hand)
        {
            return OpenRaiseEPvs3BetSBAndBBCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetBTNAndSBCall(string hand)
        {
            return OpenRaiseEPvs3BetBTNAndSBCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetBTNAndBBCall(string hand)
        {
            return OpenRaiseEPvs3BetBTNAndBBCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetCOAndBBCall(string hand)
        {
            return OpenRaiseEPvs3BetCOAndBBCall(hand);
        }
                
        public static string GetOpenRaiseEPvs3BetCOAndSBCall(string hand)
        {
            return OpenRaiseEPvs3BetCOAndSBCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetCOAndBTNCall(string hand)
        {
            return OpenRaiseEPvs3BetCOAndBTNCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetMPAndBBCall(string hand)
        {
            return OpenRaiseEPvs3BetMPAndBBCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetMPAndSBCall(string hand)
        {
            return OpenRaiseEPvs3BetMPAndSBCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetMPAndBTNCall(string hand)
        {
            return OpenRaiseEPvs3BetMPAndBTNCall(hand);
        }

        public static string GetOpenRaiseEPvs3BetMPAndCOCall(string hand)
        {
            return OpenRaiseEPvs3BetMPAndCOCall(hand);
        }

        #region Hands

        private static string OpenRaiseBTNvs3BetSBAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x24.5", Porcentajes = 98 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes = 2 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes = 94 },
                new Hands{ Hand = "AKs", Action = "4Bet x24.5", Porcentajes = 6 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "AJs", Action = "All-In", Porcentajes = 47 },
                new Hands{ Hand = "AJs", Action = "4Bet x24.5", Porcentajes = 5 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes = 48 },
                new Hands{ Hand = "ATs", Action = "4Bet x24.5", Porcentajes = 21 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes = 79 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes = 97 },
                new Hands{ Hand = "KK", Action = "4Bet x24.5", Porcentajes = 3 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes = 44 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes = 56 },
                new Hands{ Hand = "KJs", Action = "All-In", Porcentajes = 69 },
                new Hands{ Hand = "KJs", Action = "4Bet x24.5", Porcentajes = 21 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes = 10 },
                new Hands{ Hand = "KTs", Action = "4Bet x24.5", Porcentajes = 19 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes = 81 },
                new Hands{ Hand = "AQo", Action = "4Bet x24.5", Porcentajes = 23 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes = 47 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes = 30 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes = 98 },
                new Hands{ Hand = "QQ", Action = "4Bet x24.5", Porcentajes = 2 },
                new Hands{ Hand = "QJs", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "JJ", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "JTs", Action = "All-In", Porcentajes = 69 },
                new Hands{ Hand = "JTs", Action = "4Bet x24.5", Porcentajes = 23 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes = 8 },
                new Hands{ Hand = "TT", Action = "All-In", Porcentajes = 95 },
                new Hands{ Hand = "TT", Action = "4Bet x24.5", Porcentajes = 5 },
                new Hands{ Hand = "99", Action = "4Bet x24.5", Porcentajes = 16 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes = 84 },
                new Hands{ Hand = "88", Action = "4Bet x24.5", Porcentajes = 6 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes = 48 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes = 46 },
                new Hands{ Hand = "77", Action = "4Bet x24.5", Porcentajes = 2 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes = 48 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes = 50 },
                new Hands{ Hand = "66", Action = "4Bet x24.5", Porcentajes = 6 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes = 52 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes = 42 },
                new Hands{ Hand = "55", Action = "All-In", Porcentajes = 58 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes = 37 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes = 5 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes = 52 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes = 48 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes = 48 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes = 52 },
                new Hands{ Hand = "22", Action = "Marginal Call", Porcentajes = 89 },
                new Hands{ Hand = "22", Action = "Fold", Porcentajes = 11 },
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseCOvs3BetSBAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x24.5", Porcentajes = 100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes = 85 },
                new Hands{ Hand = "AKs", Action = "4Bet x24.5", Porcentajes = 15 },
                new Hands{ Hand = "AQs", Action = "4Bet x24.5", Porcentajes = 21 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes = 79 },
                new Hands{ Hand = "A5s", Action = "4Bet x24.5", Porcentajes = 19 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes = 81 },
                new Hands{ Hand = "A4s", Action = "4Bet x24.5", Porcentajes = 10 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes = 90 },
                new Hands{ Hand = "A3s", Action = "4Bet x24.5", Porcentajes = 11 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes = 89 },
                new Hands{ Hand = "A2s", Action = "4Bet x24.5", Porcentajes = 2 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes = 98 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes = 53 },
                new Hands{ Hand = "KK", Action = "4Bet x24.5", Porcentajes = 47 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes = 24 },
                new Hands{ Hand = "KQs", Action = "4Bet x24.5", Porcentajes = 39 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes = 37 },
                new Hands{ Hand = "KJs", Action = "4Bet x24.5", Porcentajes = 42 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes = 58 },
                new Hands{ Hand = "AQo", Action = "4Bet x24.5", Porcentajes = 15 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes = 85 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "JJ", Action = "All-In", Porcentajes = 35 },                
                new Hands{ Hand = "JJ", Action = "4Bet x24.5", Porcentajes = 37 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes = 28 },
                new Hands{ Hand = "TT", Action = "All-In", Porcentajes = 8 },
                new Hands{ Hand = "TT", Action = "4Bet x24.5", Porcentajes = 5 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes = 35 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes = 52 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes = 26 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes = 74 },
                new Hands{ Hand = "88", Action = "4Bet x24.5", Porcentajes = 8 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes = 35 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes = 57 },
                new Hands{ Hand = "77", Action = "4Bet x24.5", Porcentajes = 6 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes = 32 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes = 62 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes = 18 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes = 82 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes = 35 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes = 65 },
                new Hands{ Hand = "54s", Action = "4Bet x24.5", Porcentajes = 2 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes = 98 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes = 39 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes = 61 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes = 100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes = 100 },
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseCOvs3BetBTNAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x21", Porcentajes = 100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes = 48 },
                new Hands{ Hand = "AKs", Action = "4Bet x21", Porcentajes = 52 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes = 100 },
                new Hands{ Hand = "AJs", Action = "4Bet x21", Porcentajes = 35 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes = 29 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes = 36 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes = 31 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes = 69 },
                new Hands{ Hand = "A5s", Action = "4Bet x21", Porcentajes = 26 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes = 74 },
                new Hands{ Hand = "A4s", Action = "4Bet x21", Porcentajes = 6 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes = 94 },
                new Hands{ Hand = "A3s", Action = "4Bet x21", Porcentajes = 5 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes = 95 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes = 100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes = 8 },
                new Hands{ Hand = "KK", Action = "4Bet x21", Porcentajes = 92 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes = 29 },
                new Hands{ Hand = "KQs", Action = "4Bet x21", Porcentajes = 37 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes = 34 },
                new Hands{ Hand = "KJs", Action = "4Bet x21", Porcentajes = 56 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes = 35 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes = 9 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes = 11 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes = 89 },
                new Hands{ Hand = "K8s", Action = "4Bet x21", Porcentajes = 15 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes = 85 },
                new Hands{ Hand = "K6s", Action = "4Bet x21", Porcentajes = 8 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes = 92 },
                new Hands{ Hand = "K5s", Action = "4Bet x21", Porcentajes = 11 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes = 89 },
                new Hands{ Hand = "K4s", Action = "4Bet x21", Porcentajes = 2 },
                new Hands{ Hand = "K4s", Action = "Fold", Porcentajes = 98 },
                new Hands{ Hand = "AQo", Action = "4Bet x21", Porcentajes = 47 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes = 2 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes = 51 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes = 84 },
                new Hands{ Hand = "QQ", Action = "4Bet x21", Porcentajes = 16 },
                new Hands{ Hand = "QJs", Action = "4Bet x21", Porcentajes = 15 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes = 85 },
                new Hands{ Hand = "JJ", Action = "All-In", Porcentajes = 15 },
                new Hands{ Hand = "JJ", Action = "4Bet x21", Porcentajes = 69 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes = 16 },
                new Hands{ Hand = "TT", Action = "4Bet x21", Porcentajes = 15 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes = 42 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes = 43 },
                new Hands{ Hand = "99", Action = "4Bet x21", Porcentajes = 2 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes = 48 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes = 50 },
                new Hands{ Hand = "88", Action = "4Bet x21", Porcentajes = 6 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes = 55 },
                new Hands{ Hand = "87s", Action = "4Bet x21", Porcentajes = 13 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes = 87 },
                new Hands{ Hand = "77", Action = "4Bet x21", Porcentajes = 6 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes = 61 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes = 34 },
                new Hands{ Hand = "76s", Action = "4Bet x21", Porcentajes = 6 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes = 31 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes = 63 },
                new Hands{ Hand = "66", Action = "4Bet x21", Porcentajes = 5 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes = 68 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes = 27 },
                new Hands{ Hand = "65s", Action = "4Bet x21", Porcentajes = 15 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes = 29 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes = 56 },
                new Hands{ Hand = "55", Action = "4Bet x21", Porcentajes = 8 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes = 66 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes = 26 },
                new Hands{ Hand = "54s", Action = "4Bet x21", Porcentajes = 23 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes = 77 },
                new Hands{ Hand = "44", Action = "4Bet x21", Porcentajes = 2 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes = 85 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes = 13 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes = 100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes = 100 },
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseCOvs3BetBTNAndSBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x20", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  78 },
                new Hands{ Hand = "AKs", Action = "4Bet x20", Porcentajes =  22 },
                new Hands{ Hand = "AQs", Action = "4Bet x20", Porcentajes =  14 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  86 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  49 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A9s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "A7s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "A6s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "4Bet x20", Porcentajes =  26 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "A4s", Action = "4Bet x20", Porcentajes =  9 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  68 },
                new Hands{ Hand = "A3s", Action = "4Bet x20", Porcentajes =  10 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "A2s", Action = "4Bet x20", Porcentajes =  9 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  39 },
                new Hands{ Hand = "KK", Action = "4Bet x20", Porcentajes =  61 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes =  65 },
                new Hands{ Hand = "KQs", Action = "4Bet x20", Porcentajes =  16 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  19 },
                new Hands{ Hand = "KJs", Action = "4Bet x20", Porcentajes =  17 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "KTs", Action = "4Bet x20", Porcentajes =  9 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  64 },
                new Hands{ Hand = "K9s", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "K9s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "K8s", Action = "4Bet x20", Porcentajes =  16 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "K7s", Action = "4Bet x20", Porcentajes =  5 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "K6s", Action = "4Bet x20", Porcentajes =  25 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "K5s", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "K4s", Action = "4Bet x20", Porcentajes =  6 },
                new Hands{ Hand = "K4s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "K3s", Action = "4Bet x20", Porcentajes =  5 },
                new Hands{ Hand = "K3s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "AQo", Action = "4Bet x20", Porcentajes =  39 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "KQo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  77 },
                new Hands{ Hand = "QQ", Action = "4Bet x20", Porcentajes =  23 },
                new Hands{ Hand = "QJs", Action = "4Bet x20", Porcentajes =  4 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  53 },
                new Hands{ Hand = "QTs", Action = "4Bet x20", Porcentajes =  6 },
                new Hands{ Hand = "QTs", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "Q9s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "Q8s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "Q7s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "Q6s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "Q5s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "AJo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "KJo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "QJo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "4Bet x20", Porcentajes =  51 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  49 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "J9s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "J8s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "J7s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "ATo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "KTo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "QTo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "JTo", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All-In", Porcentajes =  10 },
                new Hands{ Hand = "TT", Action = "4Bet x20", Porcentajes =  6 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  61 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  23 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "T8s", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "T8s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "T7s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "A9o", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  55 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "98s", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "98s", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "98s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "97s", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "97s", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "A8o", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "4Bet x20", Porcentajes =  9 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  57 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  34 },
                new Hands{ Hand = "87s", Action = "4Bet x20", Porcentajes =  4 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "86s", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "86s", Action = "Marginal Call", Porcentajes =  66 },
                new Hands{ Hand = "86s", Action = "Fold", Porcentajes =  32 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  88 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  12 },
                new Hands{ Hand = "76s", Action = "4Bet x20", Porcentajes =  11 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  39 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "66", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  98 },
                new Hands{ Hand = "65s", Action = "4Bet x20", Porcentajes =  32 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  68 },
                new Hands{ Hand = "A5o", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "4Bet x20", Porcentajes =  30 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  70 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseMPvs3BetSBAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x25", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  74 },
                new Hands{ Hand = "AKs", Action = "4Bet x25", Porcentajes =  26 },
                new Hands{ Hand = "AQs", Action = "4Bet x25", Porcentajes =  55 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  32 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  13 },
                new Hands{ Hand = "A5s", Action = "4Bet x25", Porcentajes =  10 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "A4s", Action = "4Bet x25", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A3s", Action = "4Bet x25", Porcentajes =  1 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  96 },
                new Hands{ Hand = "AKo", Action = "4Bet x25", Porcentajes =  4 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "4Bet x25", Porcentajes =  54 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  46 },
                new Hands{ Hand = "KTs", Action = "4Bet x25", Porcentajes =  3 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "K6s", Action = "4Bet x25", Porcentajes =  13 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  44 },
                new Hands{ Hand = "QQ", Action = "4Bet x25", Porcentajes =  38 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  18 },
                new Hands{ Hand = "JJ", Action = "4Bet x25", Porcentajes =  6 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "TT", Action = "4Bet x25", Porcentajes =  10 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "99", Action = "4Bet x25", Porcentajes =  4 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "88", Action = "4Bet x25", Porcentajes =  7 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "66", Action = "4Bet x25", Porcentajes =  4 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  40 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseMPvs3BetBTNAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x21.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  67 },
                new Hands{ Hand = "AKs", Action = "4Bet x21.5", Porcentajes =  33 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  35 },
                new Hands{ Hand = "AQs", Action = "4Bet x21.5", Porcentajes =  42 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  23 },
                new Hands{ Hand = "A5s", Action = "4Bet x21.5", Porcentajes =  14 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "A4s", Action = "4Bet x21.5", Porcentajes =  14 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "A3s", Action = "4Bet x21.5", Porcentajes =  6 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "A2s", Action = "4Bet x21.5", Porcentajes =  2 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  82 },
                new Hands{ Hand = "KK", Action = "4Bet x21.5", Porcentajes =  18 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes =  9 },
                new Hands{ Hand = "KQs", Action = "4Bet x21.5", Porcentajes =  90 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "KTs", Action = "4Bet x21.5", Porcentajes =  5 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "K7s", Action = "4Bet x21.5", Porcentajes =  1 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "K6s", Action = "4Bet x21.5", Porcentajes =  18 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "K5s", Action = "4Bet x21.5", Porcentajes =  3 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "K4s", Action = "4Bet x21.5", Porcentajes =  41 },
                new Hands{ Hand = "K4s", Action = "Fold", Porcentajes =  59 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  37 },
                new Hands{ Hand = "QQ", Action = "4Bet x21.5", Porcentajes =  63 },
                new Hands{ Hand = "QTs", Action = "4Bet x21.5", Porcentajes =  7 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "TT", Action = "4Bet x21.5", Porcentajes =  11 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "99", Action = "4Bet x21.5", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "88", Action = "4Bet x21.5", Porcentajes =  5 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  68 },
                new Hands{ Hand = "77", Action = "4Bet x21.5", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  98 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  2 },
                new Hands{ Hand = "66", Action = "4Bet x21.5", Porcentajes =  6 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  41 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  53 },
                new Hands{ Hand = "65s", Action = "4Bet x21.5", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  39 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "55", Action = "4Bet x21.5", Porcentajes =  19 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "54s", Action = "4Bet x21.5", Porcentajes =  26 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  65 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  9 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseMPvs3BetBTNAndSBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  66 },
                new Hands{ Hand = "AKs", Action = "4Bet x19.5", Porcentajes =  34 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  13 },
                new Hands{ Hand = "AQs", Action = "4Bet x19.5", Porcentajes =  60 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  27 },
                new Hands{ Hand = "A5s", Action = "4Bet x19.5", Porcentajes =  9 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "A4s", Action = "4Bet x19.5", Porcentajes =  27 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "A3s", Action = "4Bet x19.5", Porcentajes =  5 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  96 },
                new Hands{ Hand = "KK", Action = "4Bet x19.5", Porcentajes =  4 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes =  22 },
                new Hands{ Hand = "KQs", Action = "4Bet x19.5", Porcentajes =  69 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  9 },
                new Hands{ Hand = "KTs", Action = "4Bet x19.5", Porcentajes =  8 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "K6s", Action = "4Bet x19.5", Porcentajes =  2 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  37 },
                new Hands{ Hand = "QQ", Action = "4Bet x19.5", Porcentajes =  57 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  6 },
                new Hands{ Hand = "JJ", Action = "4Bet x19.5", Porcentajes =  8 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  21 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  71 },
                new Hands{ Hand = "TT", Action = "4Bet x19.5", Porcentajes =  4 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "99", Action = "4Bet x19.5", Porcentajes =  6 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "88", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "87s", Action = "4Bet x19.5", Porcentajes =  6 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "76s", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  95 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  4 },
                new Hands{ Hand = "66", Action = "4Bet x19.5", Porcentajes =  4 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  53 },
                new Hands{ Hand = "65s", Action = "4Bet x19.5", Porcentajes =  42 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  58 },
                new Hands{ Hand = "55", Action = "4Bet x19.5", Porcentajes =  7 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  57 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  36 },
                new Hands{ Hand = "54s", Action = "4Bet x19.5", Porcentajes =  6 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  94 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseMPvs3BetCOAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x20", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  60 },
                new Hands{ Hand = "AKs", Action = "4Bet x20", Porcentajes =  40 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  42 },
                new Hands{ Hand = "AQs", Action = "4Bet x20", Porcentajes =  51 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  7 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "A5s", Action = "4Bet x20", Porcentajes =  35 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "A4s", Action = "4Bet x20", Porcentajes =  5 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "A3s", Action = "4Bet x20", Porcentajes =  28 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "A2s", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  94 },
                new Hands{ Hand = "AKo", Action = "4Bet x20", Porcentajes =  6 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  85 },
                new Hands{ Hand = "KK", Action = "4Bet x20", Porcentajes =  15 },
                new Hands{ Hand = "KQs", Action = "4Bet x20", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "KTs", Action = "4Bet x20", Porcentajes =  16 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  55 },
                new Hands{ Hand = "K7s", Action = "4Bet x20", Porcentajes =  6 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "K6s", Action = "4Bet x20", Porcentajes =  7 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "K5s", Action = "4Bet x20", Porcentajes =  12 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "K4s", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "K4s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  19 },
                new Hands{ Hand = "QQ", Action = "4Bet x20", Porcentajes =  81 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  26 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "QTs", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  42 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  58 },
                new Hands{ Hand = "TT", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "T8s", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "T8s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "99", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  41 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "88", Action = "4Bet x20", Porcentajes =  8 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  48 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  40 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "77", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  48 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "4Bet x20", Porcentajes =  15 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  68 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  17 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  89 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  11 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseMPvs3BetCOAndSBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x20", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  87 },
                new Hands{ Hand = "AKs", Action = "4Bet x20", Porcentajes =  13 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  23 },
                new Hands{ Hand = "AQs", Action = "4Bet x20", Porcentajes =  47 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  30 },
                new Hands{ Hand = "A5s", Action = "4Bet x20", Porcentajes =  27 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "A4s", Action = "4Bet x20", Porcentajes =  21 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "A3s", Action = "4Bet x20", Porcentajes =  14 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "A2s", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  93 },
                new Hands{ Hand = "KK", Action = "4Bet x20", Porcentajes =  7 },
                new Hands{ Hand = "KQs", Action = "4Bet x20", Porcentajes =  88 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  12 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "KTs", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "K7s", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "K6s", Action = "4Bet x20", Porcentajes =  9 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "K5s", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "K4s", Action = "4Bet x20", Porcentajes =  11 },
                new Hands{ Hand = "K4s", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  34 },
                new Hands{ Hand = "QQ", Action = "4Bet x20", Porcentajes =  56 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  9 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  71 },
                new Hands{ Hand = "TT", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "99", Action = "4Bet x20", Porcentajes =  5 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "98s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "4Bet x20", Porcentajes =  14 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  58 },
                new Hands{ Hand = "87s", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "77", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  38 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "76s", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  97 },
                new Hands{ Hand = "66", Action = "4Bet x20", Porcentajes =  13 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  54 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  33 },
                new Hands{ Hand = "65s", Action = "4Bet x20", Porcentajes =  4 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  96 },
                new Hands{ Hand = "55", Action = "4Bet x20", Porcentajes =  4 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  72 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  24 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseMPvs3BetCOAndBTNCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  77 },
                new Hands{ Hand = "AKs", Action = "4Bet x21", Porcentajes =  23 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  48 },
                new Hands{ Hand = "AQs", Action = "4Bet x21", Porcentajes =  34 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  18 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "A5s", Action = "4Bet x21", Porcentajes =  32 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  68 },
                new Hands{ Hand = "A4s", Action = "4Bet x21", Porcentajes =  5 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  30 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "A3s", Action = "4Bet x21", Porcentajes =  5 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "All-In", Porcentajes =  32 },
                new Hands{ Hand = "KQs", Action = "4Bet x21", Porcentajes =  68 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  36 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  64 },
                new Hands{ Hand = "KTs", Action = "4Bet x21", Porcentajes =  16 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "K6s", Action = "4Bet x21", Porcentajes =  5 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  16 },
                new Hands{ Hand = "QQ", Action = "4Bet x21", Porcentajes =  63 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  21 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "QTs", Action = "4Bet x21", Porcentajes =  19 },
                new Hands{ Hand = "QTs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  61 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "TT", Action = "4Bet x21", Porcentajes =  11 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  57 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  32 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "99", Action = "4Bet x21", Porcentajes =  7 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  74 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  19 },
                new Hands{ Hand = "88", Action = "4Bet x21", Porcentajes =  4 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  85 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  11 },
                new Hands{ Hand = "87s", Action = "4Bet x21", Porcentajes =  36 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  64 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  96 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  4 },
                new Hands{ Hand = "76s", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "66", Action = "4Bet x21", Porcentajes =  2 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  98 },
                new Hands{ Hand = "65s", Action = "4Bet x21", Porcentajes =  13 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  87 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetSBAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All-In", Porcentajes =  9 },
                new Hands{ Hand = "AA", Action = "4Bet x24", Porcentajes =  91 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "4Bet x24", Porcentajes =  1 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  19 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "A5s", Action = "4Bet x24", Porcentajes =  34 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  66 },
                new Hands{ Hand = "A4s", Action = "4Bet x24", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  69 },
                new Hands{ Hand = "AKo", Action = "4Bet x24", Porcentajes =  31 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "4Bet x24", Porcentajes =  44 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  43 },
                new Hands{ Hand = "QQ", Action = "4Bet x24", Porcentajes =  41 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  16 },
                new Hands{ Hand = "JJ", Action = "4Bet x24", Porcentajes =  5 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "99", Action = "4Bet x24", Porcentajes =  4 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "88", Action = "4Bet x24", Porcentajes =  3 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetBTNAndSBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "4Bet x19.5", Porcentajes =  25 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  15 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "A5s", Action = "4Bet x19.5", Porcentajes =  15 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "A4s", Action = "4Bet x19.5", Porcentajes =  49 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "A2s", Action = "4Bet x19.5", Porcentajes =  99 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  84 },
                new Hands{ Hand = "AKo", Action = "4Bet x19.5", Porcentajes =  16 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  77 },
                new Hands{ Hand = "KK", Action = "4Bet x19.5", Porcentajes =  23 },
                new Hands{ Hand = "KQs", Action = "4Bet x19.5", Porcentajes =  48 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "KJs", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "KTs", Action = "4Bet x19.5", Porcentajes =  8 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "K7s", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "K6s", Action = "4Bet x19.5", Porcentajes =  34 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  66 },
                new Hands{ Hand = "K5s", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  16 },
                new Hands{ Hand = "QQ", Action = "4Bet x19.5", Porcentajes =  74 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  10 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  30 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "99", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "88", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "87s", Action = "4Bet x19.5", Porcentajes =  83 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  17 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "66", Action = "4Bet x19.5", Porcentajes =  19 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  52 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  29 },
                new Hands{ Hand = "65s", Action = "4Bet x19.5", Porcentajes =  13 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  52 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  35 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "4Bet x19.5", Porcentajes =  4 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  19 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetBTNAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x20.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  14 },
                new Hands{ Hand = "AQs", Action = "4Bet x20.5", Porcentajes =  38 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  21 },
                new Hands{ Hand = "A5s", Action = "4Bet x20.5", Porcentajes =  28 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "A4s", Action = "4Bet x20.5", Porcentajes =  15 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "A3s", Action = "4Bet x20.5", Porcentajes =  43 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "A2s", Action = "4Bet x20.5", Porcentajes =  11 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  84 },
                new Hands{ Hand = "AKo", Action = "4Bet x20.5", Porcentajes =  16 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  84 },
                new Hands{ Hand = "KK", Action = "4Bet x20.5", Porcentajes =  16 },
                new Hands{ Hand = "KQs", Action = "4Bet x20.5", Porcentajes =  41 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  59 },
                new Hands{ Hand = "KTs", Action = "4Bet x20.5", Porcentajes =  33 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "K8s", Action = "4Bet x20.5", Porcentajes =  26 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "K7s", Action = "4Bet x20.5", Porcentajes =  22 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "K6s", Action = "4Bet x20.5", Porcentajes =  9 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "K5s", Action = "4Bet x20.5", Porcentajes =  14 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  3 },
                new Hands{ Hand = "QQ", Action = "4Bet x20.5", Porcentajes =  76 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  21 },
                new Hands{ Hand = "JJ", Action = "4Bet x20.5", Porcentajes =  5 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "4Bet x20.5", Porcentajes =  8 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  63 },
                new Hands{ Hand = "66", Action = "4Bet x20.5", Porcentajes =  12 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  53 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  35 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  77 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  23 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetCOAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  5 },
                new Hands{ Hand = "AQs", Action = "4Bet x19.5", Porcentajes =  17 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  54 },
                new Hands{ Hand = "A5s", Action = "4Bet x19.5", Porcentajes =  27 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "A4s", Action = "4Bet x19.5", Porcentajes =  31 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  33 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  36 },
                new Hands{ Hand = "A3s", Action = "4Bet x19.5", Porcentajes =  27 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "A2s", Action = "4Bet x19.5", Porcentajes =  41 },
                new Hands{ Hand = "A2s", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  54 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  80 },
                new Hands{ Hand = "AKo", Action = "4Bet x19.5", Porcentajes =  20 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  72 },
                new Hands{ Hand = "KK", Action = "4Bet x19.5", Porcentajes =  28 },
                new Hands{ Hand = "KQs", Action = "4Bet x19.5", Porcentajes =  77 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  23 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "KTs", Action = "4Bet x19.5", Porcentajes =  21 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "K8s", Action = "4Bet x19.5", Porcentajes =  2 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "K7s", Action = "4Bet x19.5", Porcentajes =  31 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "K6s", Action = "4Bet x19.5", Porcentajes =  23 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  17 },
                new Hands{ Hand = "QQ", Action = "4Bet x19.5", Porcentajes =  70 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  13 },
                new Hands{ Hand = "JJ", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "TT", Action = "4Bet x19.5", Porcentajes =  6 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "99", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "88", Action = "4Bet x19.5", Porcentajes =  9 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  37 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  54 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "4Bet x19.5", Porcentajes =  13 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  56 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  31 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetCOAndSBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x20", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  44 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  33 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  23 },
                new Hands{ Hand = "A7s", Action = "4Bet x20", Porcentajes =  8 },
                new Hands{ Hand = "A7s", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "A5s", Action = "4Bet x20", Porcentajes =  5 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "A4s", Action = "4Bet x20", Porcentajes =  29 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "A3s", Action = "4Bet x20", Porcentajes =  21 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "A2s", Action = "4Bet x20", Porcentajes =  15 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  78 },
                new Hands{ Hand = "AKo", Action = "4Bet x20", Porcentajes =  22 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  80 },
                new Hands{ Hand = "KK", Action = "4Bet x20", Porcentajes =  20 },
                new Hands{ Hand = "KQs", Action = "4Bet x20", Porcentajes =  11 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "KTs", Action = "4Bet x20", Porcentajes =  21 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "K8s", Action = "4Bet x20", Porcentajes =  6 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "K6s", Action = "4Bet x20", Porcentajes =  11 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "K5s", Action = "4Bet x20", Porcentajes =  18 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  3 },
                new Hands{ Hand = "QQ", Action = "4Bet x20", Porcentajes =  74 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  23 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  21 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "TT", Action = "4Bet x20", Porcentajes =  4 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "99", Action = "4Bet x20", Porcentajes =  12 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "88", Action = "4Bet x20", Porcentajes =  4 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "87s", Action = "4Bet x20", Porcentajes =  55 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "77", Action = "4Bet x20", Porcentajes =  13 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "4Bet x20", Porcentajes =  16 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  77 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  7 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetCOAndBTNCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  60 },
                new Hands{ Hand = "AKs", Action = "4Bet x21", Porcentajes =  40 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  25 },
                new Hands{ Hand = "AQs", Action = "4Bet x21", Porcentajes =  44 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  31 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "A8s", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A5s", Action = "4Bet x21", Porcentajes =  18 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "A4s", Action = "4Bet x21", Porcentajes =  24 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "A3s", Action = "4Bet x21", Porcentajes =  20 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  99 },
                new Hands{ Hand = "AKo", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  83 },
                new Hands{ Hand = "KK", Action = "4Bet x21", Porcentajes =  17 },
                new Hands{ Hand = "KQs", Action = "4Bet x21", Porcentajes =  55 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  32 },
                new Hands{ Hand = "KJs", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "KTs", Action = "4Bet x21", Porcentajes =  26 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "K8s", Action = "4Bet x21", Porcentajes =  6 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "K7s", Action = "4Bet x21", Porcentajes =  10 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "K6s", Action = "4Bet x21", Porcentajes =  39 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  61 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  13 },
                new Hands{ Hand = "QQ", Action = "4Bet x21", Porcentajes =  59 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  28 },
                new Hands{ Hand = "JJ", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  48 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  41 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  59 },
                new Hands{ Hand = "99", Action = "4Bet x21", Porcentajes =  14 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  58 },
                new Hands{ Hand = "88", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  57 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  42 },
                new Hands{ Hand = "87s", Action = "4Bet x21", Porcentajes =  6 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  69 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  25 },
                new Hands{ Hand = "77", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "65s", Action = "4Bet x21", Porcentajes =  18 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  82 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetMPAndBBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "4Bet x20", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All-In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All-In", Porcentajes =  14 },
                new Hands{ Hand = "AQs", Action = "4Bet x20", Porcentajes =  28 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  46 },
                new Hands{ Hand = "A7s", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "A7s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "A5s", Action = "4Bet x20", Porcentajes =  35 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "A4s", Action = "4Bet x20", Porcentajes =  26 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  54 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  20 },
                new Hands{ Hand = "A3s", Action = "4Bet x20", Porcentajes =  30 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  66 },
                new Hands{ Hand = "A2s", Action = "4Bet x20", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All-In", Porcentajes =  71 },
                new Hands{ Hand = "AKo", Action = "4Bet x20", Porcentajes =  29 },
                new Hands{ Hand = "KK", Action = "All-In", Porcentajes =  68 },
                new Hands{ Hand = "KK", Action = "4Bet x20", Porcentajes =  32 },
                new Hands{ Hand = "KQs", Action = "4Bet x20", Porcentajes =  53 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  47 },
                new Hands{ Hand = "KTs", Action = "4Bet x20", Porcentajes =  14 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "K9s", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "K9s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "K8s", Action = "4Bet x20", Porcentajes =  7 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "K6s", Action = "4Bet x20", Porcentajes =  31 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "K5s", Action = "4Bet x20", Porcentajes =  15 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "QQ", Action = "All-In", Porcentajes =  3 },
                new Hands{ Hand = "QQ", Action = "4Bet x20", Porcentajes =  86 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  11 },
                new Hands{ Hand = "JJ", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "TT", Action = "4Bet x20", Porcentajes =  5 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "88", Action = "4Bet x20", Porcentajes =  2 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  34 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  64 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "4Bet x20", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  37 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "4Bet x20", Porcentajes =  3 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  70 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  27 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetMPAndSBCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "AQs", Action = "4Bet x19", Porcentajes =  5 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  72 },
                new Hands{ Hand = "AKo", Action = "4Bet x19", Porcentajes =  28 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  69 },
                new Hands{ Hand = "KK", Action = "4Bet x19", Porcentajes =  31 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  2 },
                new Hands{ Hand = "QQ", Action = "4Bet x19", Porcentajes =  71 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  27 },
                new Hands{ Hand = "AA", Action = "4Bet x19", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "4Bet x19", Porcentajes =  24 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "A4s", Action = "4Bet x19", Porcentajes =  34 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  46 },
                new Hands{ Hand = "A3s", Action = "4Bet x19", Porcentajes =  20 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "A2s", Action = "4Bet x19", Porcentajes =  40 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "KQs", Action = "4Bet x19", Porcentajes =  20 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "KTs", Action = "4Bet x19", Porcentajes =  6 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "K8s", Action = "4Bet x19", Porcentajes =  2 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "K6s", Action = "4Bet x19", Porcentajes =  40 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "K5s", Action = "4Bet x19", Porcentajes =  10 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "JJ", Action = "4Bet x19", Porcentajes =  3 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "TT", Action = "4Bet x19", Porcentajes =  2 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "99", Action = "4Bet x19", Porcentajes =  17 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "88", Action = "4Bet x19", Porcentajes =  5 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "87s", Action = "4Bet x19", Porcentajes =  49 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "77", Action = "4Bet x19", Porcentajes =  5 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "76s", Action = "4Bet x19", Porcentajes =  3 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  97 },
                new Hands{ Hand = "66", Action = "4Bet x19", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  65 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  34 },
                new Hands{ Hand = "54s", Action = "4Bet x19", Porcentajes =  1 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetMPAndBTNCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  63 },
                new Hands{ Hand = "AKs", Action = "4Bet x20.5", Porcentajes =  37 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "AQs", Action = "4Bet x20.5", Porcentajes =  60 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  30 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  85 },
                new Hands{ Hand = "KK", Action = "4Bet x20.5", Porcentajes =  15 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "QQ", Action = "4Bet x20.5", Porcentajes =  54 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  36 },
                new Hands{ Hand = "AA", Action = "4Bet x20.5", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "4Bet x20.5", Porcentajes =  4 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "A4s", Action = "4Bet x20.5", Porcentajes =  34 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "A3s", Action = "4Bet x20.5", Porcentajes =  15 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "KQs", Action = "4Bet x20.5", Porcentajes =  48 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "KJs", Action = "4Bet x20.5", Porcentajes =  5 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "KTs", Action = "4Bet x20.5", Porcentajes =  21 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "K8s", Action = "4Bet x20.5", Porcentajes =  2 },
                new Hands{ Hand = "K8s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "K7s", Action = "4Bet x20.5", Porcentajes =  5 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "K6s", Action = "4Bet x20.5", Porcentajes =  31 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "K5s", Action = "4Bet x20.5", Porcentajes =  4 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "JJ", Action = "4Bet x20.5", Porcentajes =  4 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  52 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "99", Action = "4Bet x20.5", Porcentajes =  6 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  45 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "88", Action = "4Bet x20.5", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  56 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  43 },
                new Hands{ Hand = "87s", Action = "4Bet x20.5", Porcentajes =  59 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  41 },
                new Hands{ Hand = "77", Action = "4Bet x20.5", Porcentajes =  3 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  49 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  48 },
                new Hands{ Hand = "66", Action = "4Bet x20.5", Porcentajes =  2 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  98 },
                new Hands{ Hand = "65s", Action = "4Bet x20.5", Porcentajes =  24 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  76 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  41 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  59 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string OpenRaiseEPvs3BetMPAndCOCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  17 },
                new Hands{ Hand = "AQs", Action = "4Bet x19.5", Porcentajes =  42 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  14 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  92 },
                new Hands{ Hand = "AKo", Action = "4Bet x19.5", Porcentajes =  8 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  89 },
                new Hands{ Hand = "KK", Action = "4Bet x19.5", Porcentajes =  11 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  8 },
                new Hands{ Hand = "QQ", Action = "4Bet x19.5", Porcentajes =  65 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  27 },
                new Hands{ Hand = "AA", Action = "4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "4Bet x19.5", Porcentajes =  22 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "A4s", Action = "4Bet x19.5", Porcentajes =  36 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  36 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  28 },
                new Hands{ Hand = "A3s", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A2s", Action = "4Bet x19.5", Porcentajes =  13 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "KQs", Action = "4Bet x19.5", Porcentajes =  41 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "KTs", Action = "4Bet x19.5", Porcentajes =  14 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "K7s", Action = "4Bet x19.5", Porcentajes =  5 },
                new Hands{ Hand = "K7s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "K6s", Action = "4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "K5s", Action = "4Bet x19.5", Porcentajes =  5 },
                new Hands{ Hand = "K5s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "JJ", Action = "4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  42 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  48 },
                new Hands{ Hand = "TT", Action = "4Bet x19.5", Porcentajes =  5 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  38 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "99", Action = "4Bet x19.5", Porcentajes =  8 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  35 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "88", Action = "4Bet x19.5", Porcentajes =  4 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  48 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  48 },
                new Hands{ Hand = "87s", Action = "4Bet x19.5", Porcentajes =  40 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  60 },
                new Hands{ Hand = "77", Action = "4Bet x19.5", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  35 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  64 },
                new Hands{ Hand = "76s", Action = "4Bet x19.5", Porcentajes =  6 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  94 },
                new Hands{ Hand = "65s", Action = "4Bet x19.5", Porcentajes =  36 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  64 },
                new Hands{ Hand = "54s", Action = "4Bet x19.5", Porcentajes =  16 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  84 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  84 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  16 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        #endregion
    }
}
