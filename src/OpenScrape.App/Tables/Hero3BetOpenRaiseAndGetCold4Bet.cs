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


        #region Hands

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

        private static string BTNOpenRaiseHero3BetSBAndBBCold4BetAndBTNCall_Plus22_5BB(string hand)
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

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {
                
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOFold_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string COOpenRaiseHero3BetSBAndBBCold4BetAndCOCall_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPFold_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Minus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        private static string MPOpenRaiseHero3BetSBAndBBCold4BetAndMPCall_Plus21BB(string hand)
        {
            var hands = new List<Hands>
            {

            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }

        #endregion

    }
}
