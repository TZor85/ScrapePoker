using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tables
{
    public class Cold4Bet
    {
        #region RaiserEP
        public static string GetCold4BetBBvsOpenRaiseEPand3BetMP(string hand)
        {
            return Cold4BetBBvsOpenRaiseEPand3BetMP(hand);
        }

        public static string GetCold4BetSBvsOpenRaiseEPand3BetMP(string hand)
        {
            return Cold4BetSBvsOpenRaiseEPand3BetMP(hand);
        }

        public static string GetCold4BetBTNvsOpenRaiseEPand3BetMP(string hand)
        {
            return Cold4BetBTNvsOpenRaiseEPand3BetMP(hand);
        }

        public static string GetCold4BetCOvsOpenRaiseEPand3BetMP(string hand)
        {
            return Cold4BetCOvsOpenRaiseEPand3BetMP(hand);
        }

        public static string GetCold4BetBBvsOpenRaiseEPand3BetCO(string hand)
        {
            return Cold4BetBBvsOpenRaiseEPand3BetCO(hand);
        }

        public static string GetCold4BetSBvsOpenRaiseEPand3BetCO(string hand)
        {
            return Cold4BetSBvsOpenRaiseEPand3BetCO(hand);
        }

        public static string GetCold4BetBTNvsOpenRaiseEPand3BetCO(string hand)
        {
            return Cold4BetBTNvsOpenRaiseEPand3BetCO(hand);
        }

        public static string GetCold4BetBBvsOpenRaiseEPand3BetBTN(string hand)
        {
            return Cold4BetBBvsOpenRaiseEPand3BetBTN(hand);
        }

        public static string GetCold4BetSBvsOpenRaiseEPand3BetBTN(string hand)
        {
            return Cold4BetSBvsOpenRaiseEPand3BetBTN(hand);
        }

        public static string GetCold4BetBBvsOpenRaiseEPand3BetSB(string hand)
        {
            return Cold4BetBBvsOpenRaiseEPand3BetSB(hand);
        }
        #endregion

        #region RaiserMP

        public static string GetCold4BetBBvsOpenRaiseMPand3BetCO(string hand)
        {
            return Cold4BetBBvsOpenRaiseMPand3BetCO(hand);
        }

        public static string GetCold4BetSBvsOpenRaiseMPand3BetCO(string hand)
        {
            return Cold4BetSBvsOpenRaiseMPand3BetCO(hand);
        }

        public static string GetCold4BetBTNvsOpenRaiseMPand3BetCO(string hand)
        {
            return Cold4BetBTNvsOpenRaiseMPand3BetCO(hand);
        }

        public static string GetCold4BetBBvsOpenRaiseMPand3BetBTN(string hand)
        {
            return Cold4BetBBvsOpenRaiseMPand3BetBTN(hand);
        }

        public static string GetCold4BetSBvsOpenRaiseMPand3BetBTN(string hand)
        {
            return Cold4BetSBvsOpenRaiseMPand3BetBTN(hand);
        }

        public static string GetCold4BetBBvsOpenRaiseMPand3BetSB(string hand)
        {
            return Cold4BetBBvsOpenRaiseMPand3BetSB(hand);
        }



        #endregion

        #region RaiserCO

        public static string GetCold4BetBBvsOpenRaiseCOand3BetBTN(string hand)
        {
            return Cold4BetBBvsOpenRaiseCOand3BetBTN(hand);
        }

        public static string GetCold4BetSBvsOpenRaiseCOand3BetBTN(string hand)
        {
            return Cold4BetSBvsOpenRaiseCOand3BetBTN(hand);
        }

        public static string GetCold4BetBBvsOpenRaiseCOand3BetSB(string hand)
        {
            return Cold4BetBBvsOpenRaiseCOand3BetSB(hand);
        }

        #endregion

        #region RaiserBTN

        public static string GetCold4BetBBvsOpenRaiseBTNand3BetSB(string hand)
        {
            return Cold4BetBBvsOpenRaiseBTNand3BetSB(hand);
        }

        #endregion

        #region Hands

        private static string Cold4BetBBvsOpenRaiseEPand3BetMP(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x18", Porcentajes =  37 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x18", Porcentajes =  19 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x18", Porcentajes =  46 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x18", Porcentajes =  28 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x18", Porcentajes =  37 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x18", Porcentajes =  4 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x18", Porcentajes =  29 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x18", Porcentajes =  17 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x18", Porcentajes =  20 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x18", Porcentajes =  65 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x18", Porcentajes =  9 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x18", Porcentajes =  99 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetSBvsOpenRaiseEPand3BetMP(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x18", Porcentajes =  3 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x18", Porcentajes =  18 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x18", Porcentajes =  44 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x18", Porcentajes =  25 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x18", Porcentajes =  28 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x18", Porcentajes =  7 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x18", Porcentajes =  22 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x18", Porcentajes =  8 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x18", Porcentajes =  14 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x18", Porcentajes =  56 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x18", Porcentajes =  2 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x18", Porcentajes =  12 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x18", Porcentajes =  99 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBTNvsOpenRaiseEPand3BetMP(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x13.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x13.5", Porcentajes =  95 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x13.5", Porcentajes =  54 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x13.5", Porcentajes =  12 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x13.5", Porcentajes =  7 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x13.5", Porcentajes =  18 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x13.5", Porcentajes =  19 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x13.5", Porcentajes =  50 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x13.5", Porcentajes =  43 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x13.5", Porcentajes =  99 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x13.5", Porcentajes =  10 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x13.5", Porcentajes =  21 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x13.5", Porcentajes =  20 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x13.5", Porcentajes =  13 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x13.5", Porcentajes =  42 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x13.5", Porcentajes =  96 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x13.5", Porcentajes =  7 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x13.5", Porcentajes =  15 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x13.5", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetCOvsOpenRaiseEPand3BetMP(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x13.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x13.5", Porcentajes =  94 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  6 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x13.5", Porcentajes =  45 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x13.5", Porcentajes =  7 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x13.5", Porcentajes =  10 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x13.5", Porcentajes =  8 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x13.5", Porcentajes =  24 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x13.5", Porcentajes =  3 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x13.5", Porcentajes =  52 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x13.5", Porcentajes =  45 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x13.5", Porcentajes =  99 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x13.5", Porcentajes =  16 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x13.5", Porcentajes =  14 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x13.5", Porcentajes =  24 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x13.5", Porcentajes =  46 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x13.5", Porcentajes =  95 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x13.5", Porcentajes =  4 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x13.5", Porcentajes =  14 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x13.5", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseEPand3BetCO(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x17.5", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x17.5", Porcentajes =  67 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x17.5", Porcentajes =  22 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x17.5", Porcentajes =  44 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x17.5", Porcentajes =  30 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x17.5", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x17.5", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x17.5", Porcentajes =  30 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x17.5", Porcentajes =  9 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x17.5", Porcentajes =  43 },
                new Hands{ Hand = "K7s", Action = "Cold4Bet x17.5", Porcentajes =  9 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x17.5", Porcentajes =  17 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x17.5", Porcentajes =  73 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x17.5", Porcentajes =  14 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x17.5", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetSBvsOpenRaiseEPand3BetCO(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x18", Porcentajes =  3 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x18", Porcentajes =  17 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x18", Porcentajes =  52 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x18", Porcentajes =  19 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x18", Porcentajes =  28 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x18", Porcentajes =  13 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x18", Porcentajes =  37 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x18", Porcentajes =  14 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x18", Porcentajes =  52 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x18", Porcentajes =  10 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x18", Porcentajes =  99 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBTNvsOpenRaiseEPand3BetCO(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x13.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x13.5", Porcentajes =  96 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x13.5", Porcentajes =  47 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x13.5", Porcentajes =  19 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x13.5", Porcentajes =  6 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x13.5", Porcentajes =  35 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x13.5", Porcentajes =  8 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x13.5", Porcentajes =  45 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x13.5", Porcentajes =  47 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x13.5", Porcentajes =  17 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x13.5", Porcentajes =  34 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x13.5", Porcentajes =  18 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x13.5", Porcentajes =  46 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x13.5", Porcentajes =  96 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x13.5", Porcentajes =  6 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x13.5", Porcentajes =  16 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x13.5", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x13.5", Porcentajes =  99 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  1 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseEPand3BetBTN(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x17.5", Porcentajes =  94 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  2 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x17.5", Porcentajes =  83 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x17.5", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x17.5", Porcentajes =  66 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x17.5", Porcentajes =  5 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x17.5", Porcentajes =  13 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x17.5", Porcentajes =  42 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x17.5", Porcentajes =  40 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x17.5", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x17.5", Porcentajes =  20 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x17.5", Porcentajes =  58 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x17.5", Porcentajes =  10 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x17.5", Porcentajes =  8 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x17.5", Porcentajes =  14 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  57 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x17.5", Porcentajes =  43 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetSBvsOpenRaiseEPand3BetBTN(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  9 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x18", Porcentajes =  91 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x18", Porcentajes =  15 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x18", Porcentajes =  6 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x18", Porcentajes =  9 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x18", Porcentajes =  41 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x18", Porcentajes =  29 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x18", Porcentajes =  7 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x18", Porcentajes =  13 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x18", Porcentajes =  55 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x18", Porcentajes =  7 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x18", Porcentajes =  59 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x18", Porcentajes =  14 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  57 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x18", Porcentajes =  43 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseEPand3BetSB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  2 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x21", Porcentajes =  98 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x21", Porcentajes =  30 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x21", Porcentajes =  10 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x21", Porcentajes =  5 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x21", Porcentajes =  18 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x21", Porcentajes =  12 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x21", Porcentajes =  34 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x21", Porcentajes =  25 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x21", Porcentajes =  19 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x21", Porcentajes =  4 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  13 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x21", Porcentajes =  87 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseMPand3BetCO(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x18.5", Porcentajes =  94 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  4 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x18.5", Porcentajes =  95 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x18.5", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x18.5", Porcentajes =  99 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x18.5", Porcentajes =  50 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x18.5", Porcentajes =  39 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x18.5", Porcentajes =  24 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x18.5", Porcentajes =  65 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x18.5", Porcentajes =  37 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x18.5", Porcentajes =  19 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x18.5", Porcentajes =  24 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x18.5", Porcentajes =  8 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x18.5", Porcentajes =  11 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x18.5", Porcentajes =  31 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x18.5", Porcentajes =  15 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  16 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x18.5", Porcentajes =  84 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x18.5", Porcentajes =  99 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x18.5", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetSBvsOpenRaiseMPand3BetCO(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  7 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x19", Porcentajes =  93 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x19", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x19", Porcentajes =  99 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x19", Porcentajes =  17 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x19", Porcentajes =  4 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x19", Porcentajes =  35 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x19", Porcentajes =  27 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x19", Porcentajes =  52 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x19", Porcentajes =  44 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x19", Porcentajes =  97 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x19", Porcentajes =  4 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x19", Porcentajes =  29 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x19", Porcentajes =  21 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x19", Porcentajes =  8 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x19", Porcentajes =  14 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  87 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x19", Porcentajes =  13 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x19", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  7 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x19", Porcentajes =  93 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBTNvsOpenRaiseMPand3BetCO(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "Cold4Bet x13.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x13.5", Porcentajes =  97 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  3 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x13.5", Porcentajes =  93 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  7 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x13.5", Porcentajes =  90 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x13.5", Porcentajes =  39 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x13.5", Porcentajes =  16 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x13.5", Porcentajes =  25 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x13.5", Porcentajes =  7 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x13.5", Porcentajes =  79 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x13.5", Porcentajes =  26 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x13.5", Porcentajes =  95 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x13.5", Porcentajes =  20 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x13.5", Porcentajes =  14 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x13.5", Porcentajes =  92 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  8 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x13.5", Porcentajes =  95 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x13.5", Porcentajes =  24 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x13.5", Porcentajes =  99 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x13.5", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseMPand3BetBTN(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  25 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x18", Porcentajes =  75 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x18", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x18", Porcentajes =  99 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x18", Porcentajes =  99 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x18", Porcentajes =  63 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x18", Porcentajes =  27 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x18", Porcentajes =  5 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x18", Porcentajes =  52 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x18", Porcentajes =  12 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x18", Porcentajes =  47 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x18", Porcentajes =  30 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x18", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x18", Porcentajes =  20 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x18", Porcentajes =  29 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x18", Porcentajes =  7 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x18", Porcentajes =  8 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x18", Porcentajes =  21 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x18", Porcentajes =  31 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x18", Porcentajes =  18 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  59 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x18", Porcentajes =  41 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  13 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x18", Porcentajes =  87 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetSBvsOpenRaiseMPand3BetBTN(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  15 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x19.5", Porcentajes =  85 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x19.5", Porcentajes =  93 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x19.5", Porcentajes =  99 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x19.5", Porcentajes =  19 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x19.5", Porcentajes =  14 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x19.5", Porcentajes =  13 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x19.5", Porcentajes =  34 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x19.5", Porcentajes =  44 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x19.5", Porcentajes =  41 },
                new Hands{ Hand = "A3s", Action = "Cold4Bet x19.5", Porcentajes =  3 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x19.5", Porcentajes =  3 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x19.5", Porcentajes =  33 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x19.5", Porcentajes =  2 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x19.5", Porcentajes =  13 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x19.5", Porcentajes =  99 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  34 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x19.5", Porcentajes =  66 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseMPand3BetSB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  66 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x21", Porcentajes =  32 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x21", Porcentajes =  97 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  3 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x21", Porcentajes =  16 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x21", Porcentajes =  1 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x21", Porcentajes =  6 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x21", Porcentajes =  47 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x21", Porcentajes =  21 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x21", Porcentajes =  26 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x21", Porcentajes =  36 },
                new Hands{ Hand = "A3s", Action = "Cold4Bet x21", Porcentajes =  34 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x21", Porcentajes =  20 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x21", Porcentajes =  44 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x21", Porcentajes =  36 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x21", Porcentajes =  15 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x21", Porcentajes =  26 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x21", Porcentajes =  12 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x21", Porcentajes =  94 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x21", Porcentajes =  88 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseCOand3BetBTN(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x19.5", Porcentajes =  84 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  6 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  71 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x19.5", Porcentajes =  28 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  41 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x19.5", Porcentajes =  58 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x19.5", Porcentajes =  94 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x19.5", Porcentajes =  17 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x19.5", Porcentajes =  35 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x19.5", Porcentajes =  10 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x19.5", Porcentajes =  40 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x19.5", Porcentajes =  28 },
                new Hands{ Hand = "A3s", Action = "Cold4Bet x19.5", Porcentajes =  6 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x19.5", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x19.5", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x19.5", Porcentajes =  50 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x19.5", Porcentajes =  39 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x19.5", Porcentajes =  22 },
                new Hands{ Hand = "K7s", Action = "Cold4Bet x19.5", Porcentajes =  2 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x19.5", Porcentajes =  11 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x19.5", Porcentajes =  31 },
                new Hands{ Hand = "AQo", Action = "Cold4Bet x19.5", Porcentajes =  59 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x19.5", Porcentajes =  96 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x19.5", Porcentajes =  22 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "99", Action = "Cold4Bet x19.5", Porcentajes =  7 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "88", Action = "Cold4Bet x19.5", Porcentajes =  3 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Cold4Bet x19.5", Porcentajes =  4 },
                new Hands{ Hand = "54s", Action = "Cold4Bet x19.5", Porcentajes =  3 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  1 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetSBvsOpenRaiseCOand3BetBTN(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x20.5", Porcentajes =  85 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  3 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  77 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x20.5", Porcentajes =  22 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x20.5", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x20.5", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x20.5", Porcentajes =  73 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x20.5", Porcentajes =  33 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x20.5", Porcentajes =  12 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x20.5", Porcentajes =  36 },
                new Hands{ Hand = "A4s", Action = "Cold4Bet x20.5", Porcentajes =  30 },
                new Hands{ Hand = "A3s", Action = "Cold4Bet x20.5", Porcentajes =  3 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x20.5", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x20.5", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x20.5", Porcentajes =  48 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x20.5", Porcentajes =  24 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x20.5", Porcentajes =  23 },
                new Hands{ Hand = "K8s", Action = "Cold4Bet x20.5", Porcentajes =  1 },
                new Hands{ Hand = "K7s", Action = "Cold4Bet x20.5", Porcentajes =  5 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x20.5", Porcentajes =  14 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x20.5", Porcentajes =  37 },
                new Hands{ Hand = "AQo", Action = "Cold4Bet x20.5", Porcentajes =  48 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x20.5", Porcentajes =  66 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x20.5", Porcentajes =  3 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Cold4Bet x20.5", Porcentajes =  1 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Cold4Bet x20.5", Porcentajes =  2 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  51 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x20.5", Porcentajes =  49 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }


        private static string Cold4BetBBvsOpenRaiseCOand3BetSB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  49 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x21", Porcentajes =  45 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  6 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  36 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x21", Porcentajes =  59 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x21", Porcentajes =  83 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  17 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x21", Porcentajes =  26 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x21", Porcentajes =  2 },
                new Hands{ Hand = "A9s", Action = "Cold4Bet x21", Porcentajes =  2 },
                new Hands{ Hand = "A8s", Action = "Cold4Bet x21", Porcentajes =  51 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x21", Porcentajes =  14 },
                new Hands{ Hand = "A5s", Action = "Cold4Bet x21", Porcentajes =  31 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x21", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x21", Porcentajes =  95 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x21", Porcentajes =  45 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x21", Porcentajes =  16 },
                new Hands{ Hand = "K9s", Action = "Cold4Bet x21", Porcentajes =  25 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x21", Porcentajes =  3 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x21", Porcentajes =  20 },
                new Hands{ Hand = "AQo", Action = "Cold4Bet x21", Porcentajes =  52 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x21", Porcentajes =  71 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x21", Porcentajes =  15 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  2 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string Cold4BetBBvsOpenRaiseBTNand3BetSB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  8 },
                new Hands{ Hand = "AQs", Action = "Cold4Bet x22.5", Porcentajes =  75 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  17 },
                new Hands{ Hand = "KQs", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "KQs", Action = "Cold4Bet x22.5", Porcentajes =  90 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  9 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  5 },
                new Hands{ Hand = "JJ", Action = "Cold4Bet x22.5", Porcentajes =  94 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  5 },
                new Hands{ Hand = "TT", Action = "Cold4Bet x22.5", Porcentajes =  91 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "AA", Action = "Cold4Bet x22.5", Porcentajes =  98 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "AKs", Action = "Cold4Bet x22.5", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Cold4Bet x22.5", Porcentajes =  87 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  13 },
                new Hands{ Hand = "ATs", Action = "Cold4Bet x22.5", Porcentajes =  99 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "A7s", Action = "Cold4Bet x22.5", Porcentajes =  23 },
                new Hands{ Hand = "A6s", Action = "Cold4Bet x22.5", Porcentajes =  23 },
                new Hands{ Hand = "KK", Action = "Cold4Bet x22.5", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Cold4Bet x22.5", Porcentajes =  96 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "KTs", Action = "Cold4Bet x22.5", Porcentajes =  96 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "K6s", Action = "Cold4Bet x22.5", Porcentajes =  1 },
                new Hands{ Hand = "K5s", Action = "Cold4Bet x22.5", Porcentajes =  6 },
                new Hands{ Hand = "AQo", Action = "Cold4Bet x22.5", Porcentajes =  100 },
                new Hands{ Hand = "KQo", Action = "Cold4Bet x22.5", Porcentajes =  5 },
                new Hands{ Hand = "QQ", Action = "Cold4Bet x22.5", Porcentajes =  100 },
                new Hands{ Hand = "AJo", Action = "Cold4Bet x22.5", Porcentajes =  2 },
                new Hands{ Hand = "KJo", Action = "Cold4Bet x22.5", Porcentajes =  16 },
                new Hands{ Hand = "99", Action = "Cold4Bet x22.5", Porcentajes =  18 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "88", Action = "Cold4Bet x22.5", Porcentajes =  11 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "AKo", Action = "Cold4Bet x22.5", Porcentajes =  88 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }


        #endregion

    }
}
