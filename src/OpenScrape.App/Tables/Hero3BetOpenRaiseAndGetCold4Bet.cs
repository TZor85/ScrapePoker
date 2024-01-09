using OpenScrape.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Tables
{
    public static class Hero3BetOpenRaiseAndGetCold4Bet
    {

        #region HeroSB
        public static string GetBTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNFold_Minus22_5BB(string hand)
        {
            return BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNFold_Minus22_5BB(hand);
        }

        public static string GetBTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNFold_Plus22_5BB(string hand)
        {
            return BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNFold_Plus22_5BB(hand);
        }

        public static string GetBTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Minus22_5BB(string hand)
        {
            return BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Minus22_5BB(hand);
        }

        public static string GetBTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Plus22_5BB(string hand)
        {
            return BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Plus22_5BB(hand);
        }

        public static string GetCOOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Minus21BB(string hand)
        {
            return COOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Minus21BB(hand);
        }

        public static string GetCOOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Plus21BB(string hand)
        {
            return COOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Plus21BB(hand);
        }

        public static string GetCOOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Minus21BB(string hand)
        {
            return COOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Minus21BB(hand);
        }

        public static string GetCOOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Plus21BB(string hand)
        {
            return COOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Plus21BB(hand);
        }

        public static string GetMPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Minus21BB(string hand)
        {
            return MPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Minus21BB(hand);
        }

        public static string GetMPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Plus21BB(string hand)
        {
            return MPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Plus21BB(hand);
        }

        public static string GetMPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Minus21BB(string hand)
        {
            return MPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Minus21BB(hand);
        }

        public static string GetMPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Plus21BB(string hand)
        {
            return MPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Plus21BB(hand);
        }

        public static string GetEPOpenRaiseHero3BetSBAndBBCold4BetAndEPFold_Minus21BB(string hand)
        {
            return EPOpenRaiseHero3BetSBAndBBCold4BetAndEPFold_Minus21BB(hand);
        }

        public static string GetEPOpenRaiseHero3BetSBAndBBCold4BetAndEPFold_Plus21BB(string hand)
        {
            return EPOpenRaiseHero3BetSBAndBBCold4BetAndEPFold_Plus21BB(hand);
        }

        public static string GetEPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Minus21BB(string hand)
        {
            return EPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Minus21BB(hand);
        }

        public static string GetEPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Plus21BB(string hand)
        {
            return EPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Plus21BB(hand);
        }

        #endregion




        #region Hands

        #region SB
        private static string BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNFold_Minus22_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  56 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  44 },
                new Hands{ Hand = "AJs", Action = "All In", Porcentajes =  78 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  22 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  81 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  19 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  9 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  34 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "All In", Porcentajes =  80 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  20 },
                new Hands{ Hand = "KJs", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  88 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  38 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  62 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  65 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  35 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  71 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  29 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  8 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  92 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  67 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  33 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  21 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "A2s", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  67 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  33 },
                new Hands{ Hand = "K6s", Action = "Marginal Call", Porcentajes =  98 },
                new Hands{ Hand = "K6s", Action = "Fold", Porcentajes =  2 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  34 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  66 },
                new Hands{ Hand = "J9s", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "J9s", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  32 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  68 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNFold_Plus22_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Marginal All In", Porcentajes =  39 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  61 },
                new Hands{ Hand = "77", Action = "Marginal All In", Porcentajes =  72 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  28 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Minus22_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  96 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  93 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  7 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  90 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  10 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  95 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  41 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  59 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  68 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  32 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  42 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  58 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  98 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  2 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  97 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  3 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Plus22_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  77 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  23 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  37 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  40 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  92 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  8 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  65 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  35 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  51 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  49 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A9s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A9s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A8s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  71 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  29 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Marginal All In", Porcentajes =  80 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  20 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  92 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  8 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  3 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  58 },
                new Hands{ Hand = "AKo", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "AKo", Action = "Fold", Porcentajes =  31 },
                new Hands{ Hand = "QQ", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "QQ", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  26 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  39 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  61 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  89 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  11 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  5 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  95 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  48 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  25 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  50 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  42 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  98 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  36 },
                new Hands{ Hand = "QQ", Action = "Marginal Call", Porcentajes =  63 },
                new Hands{ Hand = "QQ", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A9s", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "A9s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "A8s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  74 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  26 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  64 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  36 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  38 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  78 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  22 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal All In", Porcentajes =  71 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  29 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  42 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  58 },
                new Hands{ Hand = "AJs", Action = "All In", Porcentajes =  78 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  22 },
                new Hands{ Hand = "A6s", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "A6s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  4 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "A2s", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  3 },
                new Hands{ Hand = "QQ", Action = "Marginal Call", Porcentajes =  26 },
                new Hands{ Hand = "QQ", Action = "Fold", Porcentajes =  71 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  9 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  26 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  96 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  4 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHero3BetSBAndBBCold4BetAndEPFold_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  95 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  84 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  16 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  63 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  15 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  22 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  90 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  10 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "QTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHero3BetSBAndBBCold4BetAndEPFold_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Marginal All In", Porcentajes =  99 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal All In", Porcentajes =  99 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal All In", Porcentajes =  99 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "66", Action = "Marginal All In", Porcentajes =  98 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  2 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  92 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  8 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  35 },
                new Hands{ Hand = "AKs", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  85 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  15 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  38 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  17 },
                new Hands{ Hand = "QQ", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "QQ", Action = "Fold", Porcentajes =  63 },
                new Hands{ Hand = "KK", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "KK", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  57 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  43 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  33 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  98 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        #endregion

        private static string EPOpenRaiseHero3BetSBAndBBCold4BetAndEPCall_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }



        #endregion

    }
}
