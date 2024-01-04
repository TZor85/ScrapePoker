using OpenScrape.App.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Tables
{
    public class Hero3BetAndOpenRaiser4Bet
    {

        public string GetSBOpenRaiseHeroInBB3BetAndSB4Bet_Minus20BB(string hand)
        {
            return SBOpenRaiseHeroInBB3BetAndSB4Bet_Minus20BB(hand);
        }

        public string GetSBOpenRaiseHeroInBB3BetAndSB4Bet_plus20BB(string hand)
        {
            return SBOpenRaiseHeroInBB3BetAndSB4Bet_plus20BB(hand);
        }

        public string GetBTNOpenRaiseHeroInBB3BetAndBTN4Bet_Minus23_5BB(string hand)
        {
            return BTNOpenRaiseHeroInBB3BetAndBTN4Bet_Minus23_5BB(hand);
        }

        public string GetBTNOpenRaiseHeroInBB3BetAndBTN4Bet_Plus23_5BB(string hand)
        {
            return BTNOpenRaiseHeroInBB3BetAndBTN4Bet_Plus23_5BB(hand);
        }

        public string GetCOOpenRaiseHeroInBB3BetAndCO4Bet_Minus22_5BB(string hand)
        {
            return COOpenRaiseHeroInBB3BetAndCO4Bet_Minus22_5BB(hand);
        }

        public string GetCOOpenRaiseHeroInBB3BetAndCO4Bet_Plus22_5BB(string hand)
        {
            return COOpenRaiseHeroInBB3BetAndCO4Bet_Plus22_5BB(hand);
        }

        public string GetMPOpenRaiseHeroInBB3BetAndMP4Bet_Minus22BB(string hand)
        {
            return MPOpenRaiseHeroInBB3BetAndMP4Bet_Minus22BB(hand);
        }

        public string GetMPOpenRaiseHeroInBB3BetAndMP4Bet_Plus22BB(string hand)
        {
            return MPOpenRaiseHeroInBB3BetAndMP4Bet_Plus22BB(hand);
        }

        public string GetEPOpenRaiseHeroInBB3BetAndEP4Bet(string hand)
        {
            return EPOpenRaiseHeroInBB3BetAndEP4Bet(hand);
        }

        public string GetBTNOpenRaiseHeroInSB3BetAndBTN4Bet_Minus23_5BB(string hand)
        {
            return BTNOpenRaiseHeroInSB3BetAndBTN4Bet_Minus23_5BB(hand);
        }

        public string GetBTNOpenRaiseHeroInSB3BetAndBTN4Bet_Plus23_5BB(string hand)
        {
            return BTNOpenRaiseHeroInSB3BetAndBTN4Bet_Plus23_5BB(hand);
        }

        public string GetCOOpenRaiseHeroInSB3BetAndCO4Bet(string hand)
        {
            return COOpenRaiseHeroInSB3BetAndCO4Bet(hand);
        }

        public string GetMPOpenRaiseHeroInSB3BetAndMP4Bet_Minus20_5BB(string hand)
        {
            return MPOpenRaiseHeroInSB3BetAndMP4Bet_Minus20_5BB(hand);
        }

        public string GetMPOpenRaiseHeroInSB3BetAndMP4Bet_Plus20_5BB(string hand)
        {
            return MPOpenRaiseHeroInSB3BetAndMP4Bet_Plus20_5BB(hand);
        }

        public string GetEPOpenRaiseHeroInSB3BetAndEP4Bet_Minus20_5BB(string hand)
        {
            return EPOpenRaiseHeroInSB3BetAndEP4Bet_Minus20_5BB(hand);
        }

        public string GetEPOpenRaiseHeroInSB3BetAndEP4Bet_Plus20_5BB(string hand)
        {
            return EPOpenRaiseHeroInSB3BetAndEP4Bet_Plus20_5BB(hand);
        }


        #region Hands

        private static string SBOpenRaiseHeroInBB3BetAndSB4Bet_Minus20BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A3s", Action = "All In", Porcentajes =  48 },
                new Hands{ Hand = "A3s", Action = "Call", Porcentajes =  52 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  93 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  7 },
                new Hands{ Hand = "KQs", Action = "All In", Porcentajes =  73 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  27 },
                new Hands{ Hand = "KJs", Action = "All In", Porcentajes =  34 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  66 },
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  59 },
                new Hands{ Hand = "AQo", Action = "Call", Porcentajes =  41 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "QJs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  88 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  12 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  90 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  10 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  48 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  52 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  29 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  71 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  31 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  69 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A8s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A7s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A2s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K8s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K7s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K6s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K4s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K3s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K2s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KQo", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "Q9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJo", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "J9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T8s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "97s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "86s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "64s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "53s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "43s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "32s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  1 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string SBOpenRaiseHeroInBB3BetAndSB4Bet_plus20BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQo", Action = "Marginal All In", Porcentajes =  51 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Marginal All In", Porcentajes =  46 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  54 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHeroInBB3BetAndBTN4Bet_Minus23_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  79 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  21 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  64 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  36 },
                new Hands{ Hand = "AJs", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A3s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A2s", Action = "All In", Porcentajes =  78 },
                new Hands{ Hand = "A2s", Action = "Call", Porcentajes =  22 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "All In", Porcentajes =  64 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  36 },
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  32 },
                new Hands{ Hand = "AQo", Action = "Call", Porcentajes =  68 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  71 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  29 },
                new Hands{ Hand = "33", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K6s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K5s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K4s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "K3s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KQo", Action = "Marginal Call", Porcentajes =  63 },
                new Hands{ Hand = "KQo", Action = "Fold", Porcentajes =  37 },
                new Hands{ Hand = "QJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "J9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T8s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "97s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "86s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "75s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "64s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "53s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "43s", Action = "Call", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHeroInBB3BetAndBTN4Bet_Plus23_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Marginal All In", Porcentajes =  93 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  7 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "All In", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHeroInBB3BetAndCO4Bet_Minus22_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  98 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  84 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  16 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  59 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  41 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  4 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  96 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  37 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  63 },
                new Hands{ Hand = "44", Action = "All In", Porcentajes =  61 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  39 },
                new Hands{ Hand = "33", Action = "All In", Porcentajes =  66 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  34 },
                new Hands{ Hand = "22", Action = "All In", Porcentajes =  2 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  98 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A3s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A2s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "J9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "64s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "53s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "43s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "43s", Action = "Fold", Porcentajes =  1 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHeroInBB3BetAndCO4Bet_Plus22_5BB(string hand)
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
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "All In", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHeroInBB3BetAndMP4Bet_Minus22BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  97 },
                new Hands{ Hand = "A4s", Action = "Call", Porcentajes =  3 },
                new Hands{ Hand = "A3s", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "A3s", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  93 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  7 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  91 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  9 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  26 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  74 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A2s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A2s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  87 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  13 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  55 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "98s", Action = "Marginal Call", Porcentajes =  33 },
                new Hands{ Hand = "98s", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  95 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  5 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  1 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHeroInBB3BetAndMP4Bet_Plus22BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "Marginal All In", Porcentajes =  81 },
                new Hands{ Hand = "AKo", Action = "Fold", Porcentajes =  19 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHeroInBB3BetAndEP4Bet(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  97 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  3 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  85 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  15 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  96 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  4 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  66 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  34 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A3s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QTs", Action = "Marginal Call", Porcentajes =  37 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  63 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  54 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  46 },
                new Hands{ Hand = "98s", Action = "Marginal Call", Porcentajes =  95 },
                new Hands{ Hand = "98s", Action = "Fold", Porcentajes =  5 },
                new Hands{ Hand = "87s", Action = "Marginal Call", Porcentajes =  58 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  42 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  89 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  11 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  87 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  13 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  87 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  13 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHeroInSB3BetAndBTN4Bet_Minus23_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  95 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "AJs", Action = "All In", Porcentajes =  40 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  60 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  98 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  59 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  34 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "All In", Porcentajes =  64 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  36 },
                new Hands{ Hand = "KJs", Action = "All In", Porcentajes =  95 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  41 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  16 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QJs", Action = "All In", Porcentajes =  46 },
                new Hands{ Hand = "QJs", Action = "Call", Porcentajes =  54 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  38 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  62 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  18 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  82 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  44 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  56 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  66 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  34 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  88 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A8s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QTs", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string BTNOpenRaiseHeroInSB3BetAndBTN4Bet_Plus23_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQo", Action = "Marginal All In", Porcentajes =  30 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Marginal All In", Porcentajes =  49 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  51 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHeroInSB3BetAndCO4Bet(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "All In", Porcentajes =  41 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  59 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  98 },
                new Hands{ Hand = "A4s", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  45 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  55 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  76 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  24 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "44", Action = "All In", Porcentajes =  2 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  98 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A8s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHeroInSB3BetAndMP4Bet_Minus20_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  92 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  8 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  85 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  3 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  70 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  30 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  9 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  91 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  26 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  74 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  51 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  1 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHeroInSB3BetAndMP4Bet_Plus20_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "Marginal All In", Porcentajes =  86 },
                new Hands{ Hand = "AKo", Action = "Fold", Porcentajes =  14 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Marginal All In", Porcentajes =  95 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  5 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHeroInSB3BetAndEP4Bet_Minus20_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  76 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  24 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  34 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  60 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  95 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  81 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  19 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  65 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  35 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  85 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  15 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  56 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  56 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "87s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  86 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  14 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  80 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  20 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  78 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHeroInSB3BetAndEP4Bet_Plus20_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Marginal All In", Porcentajes =  69 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  31 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHeroInBTN3BetAndCO4Bet(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  47 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  53 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  73 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  27 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  85 },
                new Hands{ Hand = "A4s", Action = "Call", Porcentajes =  15 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "All In", Porcentajes =  32 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  68 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  60 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  40 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  69 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  31 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  33 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "JTs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "T9s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 }

            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHeroInBTN3BetAndMP4Bet_Minus19BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  56 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  44 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  76 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  24 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  87 },
                new Hands{ Hand = "A4s", Action = "Call", Porcentajes =  13 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  91 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  9 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  78 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  22 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  11 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  89 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  78 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  22 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  89 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  11 },
                new Hands{ Hand = "T9s", Action = "Marginal Call", Porcentajes =  97 },
                new Hands{ Hand = "T9s", Action = "Fold", Porcentajes =  3 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  51 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  46 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  54 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  91 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  9 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHeroInBTN3BetAndMP4Bet_Plus19BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "Marginal All In", Porcentajes =  7 },
                new Hands{ Hand = "AKo", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHeroInBTN3BetAndEP4Bet_Minus19_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  70 },
                new Hands{ Hand = "AA", Action = "Call", Porcentajes =  30 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  88 },
                new Hands{ Hand = "AKs", Action = "Call", Porcentajes =  12 },
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  55 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  39 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  6 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  34 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  84 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  16 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  86 },
                new Hands{ Hand = "KK", Action = "Call", Porcentajes =  14 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  18 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  82 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  72 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  28 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  33 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "87s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  46 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  54 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  35 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  65 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string EPOpenRaiseHeroInBTN3BetAndEP4Bet_Plus19_5BB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AA", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "AKs", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "KK", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal All In", Porcentajes =  44 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 }
            };


            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        #endregion

    }
}
