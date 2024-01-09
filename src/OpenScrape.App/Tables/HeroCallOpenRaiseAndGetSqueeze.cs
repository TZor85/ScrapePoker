using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tables
{
    public static class HeroCallOpenRaiseAndGetSqueeze
    {

        #region Hands

        public static string GetBTNOpenRaiseHeroCallSBAndBBSqueezeAndBTNFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "KJs", Action = "All In", Porcentajes =  29 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  71 },
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  88 },
                new Hands{ Hand = "AQo", Action = "Call", Porcentajes =  12 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  81 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  19 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  12 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  17 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  20 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "44", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "A9s", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "A9s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "KQo", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "KQo", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "QTs", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  55 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "22", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "22", Action = "Fold", Porcentajes =  92 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetBTNOpenRaiseHeroCallSBAndBBSqueezeAndBTNCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  80 },
                new Hands{ Hand = "AQo", Action = "Call", Porcentajes =  20 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "99", Action = "All In", Porcentajes =  55 },
                new Hands{ Hand = "99", Action = "Call", Porcentajes =  45 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  55 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "KQo", Action = "Marginal Call", Porcentajes =  51 },
                new Hands{ Hand = "KQo", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "QJs", Action = "Marginal Call", Porcentajes =  30 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "AJo", Action = "Marginal Call", Porcentajes =  18 },
                new Hands{ Hand = "AJo", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "JTs", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  68 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  32 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  53 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  47 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  79 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  21 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetCOOpenRaiseHeroCallBTNAndSBSqueezeAndCOFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  33 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  67 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  53 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  40 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  7 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  67 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  33 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "98s", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "98s", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "22", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "22", Action = "Fold", Porcentajes =  86 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetCOOpenRaiseHeroCallBTNAndSBSqueezeAndCOCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  67 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  17 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  83 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  11 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  72 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  17 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  57 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  43 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  72 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  28 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  21 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  71 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  71 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  29 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetCOOpenRaiseHeroCallBTNAndBBSqueezeAndCOFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  4 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  34 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  54 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  46 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  4 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "AJs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "KQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  38 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  19 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  81 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  50 },
                new Hands{ Hand = "TT", Action = "Call", Porcentajes =  50 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetCOOpenRaiseHeroCallBTNAndBBSqueezeAndCOCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQo", Action = "All In", Porcentajes =  15 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  41 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  23 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  59 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  18 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  51 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  55 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  63 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  37 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetMPOpenRaiseHeroCallCOAndBTNSqueezeAndMPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  95 },
                new Hands{ Hand = "A5s", Action = "Call", Porcentajes =  5 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  69 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  30 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  97 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  3 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  61 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  19 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  19 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  7 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  65 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A3s", Action = "Marginal Call", Porcentajes =  66 },
                new Hands{ Hand = "A3s", Action = "Fold", Porcentajes =  34 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  19 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  81 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "98s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  81 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  19 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  79 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  21 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  59 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  41 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  84 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  16 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A9s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "A8s", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "QJs", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "QTs", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "JTs", Action = "Fold", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetMPOpenRaiseHeroCallCOAndBTNSqueezeAndMPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  23 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  65 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  12 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  69 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  30 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  18 },
                new Hands{ Hand = "JJ", Action = "Call", Porcentajes =  82 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  78 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  12 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  11 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  89 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  50 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  15 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "AQo", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "AQo", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  53 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  47 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  47 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  53 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  79 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  21 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetMPOpenRaiseHeroCallCOAndSBSqueezeAndMPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  49 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  44 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  58 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  42 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  87 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  13 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  56 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  31 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  4 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  6 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  17 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  1 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  99 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  98 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  96 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  71 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "22", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "22", Action = "Fold", Porcentajes =  72 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetMPOpenRaiseHeroCallCOAndSBSqueezeAndMPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  98 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  2 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  76 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  24 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  7 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  30 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  70 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  18 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  32 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  68 },
                new Hands{ Hand = "44", Action = "Call", Porcentajes =  99 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "33", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetMPOpenRaiseHeroCallCOAndBBSqueezeAndMPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  38 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  70 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  30 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  81 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  19 },
                new Hands{ Hand = "JJ", Action = "All In", Porcentajes =  50 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  38 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  14 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  4 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "88", Action = "All In", Porcentajes =  5 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "77", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "66", Action = "All In", Porcentajes =  2 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "55", Action = "All In", Porcentajes =  1 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  2 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  86 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetMPOpenRaiseHeroCallCOAndBBSqueezeAndMPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  99 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  1 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  86 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  14 },
                new Hands{ Hand = "TT", Action = "All In", Porcentajes =  9 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  7 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  93 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  11 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  89 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "44", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "44", Action = "Fold", Porcentajes =  1 },
                new Hands{ Hand = "33", Action = "Marginal Call", Porcentajes =  99 },
                new Hands{ Hand = "33", Action = "Fold", Porcentajes =  1 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndCOSqueezeAndEPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  35 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  50 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  15 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  10 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  67 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  23 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  92 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  8 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  42 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  58 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  43 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  57 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  18 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  18 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  82 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  27 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  73 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  77 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  23 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  48 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  24 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  76 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  61 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  39 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndCOSqueezeAndEPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  41 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  59 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  39 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  61 },
                new Hands{ Hand = "A4s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  67 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  33 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  45 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  55 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  49 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  79 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  21 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  63 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  37 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndBTNSqueezeAndEPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  36 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  56 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  8 },
                new Hands{ Hand = "A4s", Action = "All In", Porcentajes =  14 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  65 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  21 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  46 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  54 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AJs", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "AJs", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  3 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  97 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  28 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  72 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  51 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  49 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Marginal Call", Porcentajes =  49 },
                new Hands{ Hand = "55", Action = "Fold", Porcentajes =  51 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  55 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  45 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndBTNSqueezeAndEPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  50 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  50 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  26 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  74 },
                new Hands{ Hand = "A4s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  80 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  20 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  39 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  61 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  62 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  38 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  76 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  24 },
                new Hands{ Hand = "77", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  70 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  30 },
                new Hands{ Hand = "66", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "55", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndSBSqueezeAndEPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  18 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  82 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  7 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  93 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  92 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  8 },
                new Hands{ Hand = "A4s", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "A4s", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  8 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  92 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  54 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  46 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  13 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  87 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  17 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  83 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  22 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  78 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  29 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  71 },
                new Hands{ Hand = "76s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  41 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  59 },
                new Hands{ Hand = "65s", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  48 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndSBSqueezeAndEPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  25 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  75 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  20 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  80 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  23 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  77 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  38 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  21 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  42 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  58 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndBBSqueezeAndEPFold(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "A5s", Action = "All In", Porcentajes =  7 },
                new Hands{ Hand = "A5s", Action = "Marginal Call", Porcentajes =  85 },
                new Hands{ Hand = "A5s", Action = "Fold", Porcentajes =  8 },
                new Hands{ Hand = "AKo", Action = "All In", Porcentajes =  13 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  87 },
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  39 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  61 },
                new Hands{ Hand = "AQs", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "ATs", Action = "Marginal Call", Porcentajes =  31 },
                new Hands{ Hand = "ATs", Action = "Fold", Porcentajes =  69 },
                new Hands{ Hand = "KQs", Action = "Marginal Call", Porcentajes =  10 },
                new Hands{ Hand = "KQs", Action = "Fold", Porcentajes =  90 },
                new Hands{ Hand = "KJs", Action = "Marginal Call", Porcentajes =  34 },
                new Hands{ Hand = "KJs", Action = "Fold", Porcentajes =  66 },
                new Hands{ Hand = "KTs", Action = "Marginal Call", Porcentajes =  21 },
                new Hands{ Hand = "KTs", Action = "Fold", Porcentajes =  79 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  12 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  88 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  15 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  85 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  16 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  84 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  14 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  86 },
                new Hands{ Hand = "76s", Action = "Marginal Call", Porcentajes =  64 },
                new Hands{ Hand = "76s", Action = "Fold", Porcentajes =  36 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  48 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  52 },
                new Hands{ Hand = "65s", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "65s", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "54s", Action = "Marginal Call", Porcentajes =  18 },
                new Hands{ Hand = "54s", Action = "Fold", Porcentajes =  82 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        public static string GetEPOpenRaiseHeroCallMPAndBBSqueezeAndEPCall(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands{ Hand = "QQ", Action = "All In", Porcentajes =  24 },
                new Hands{ Hand = "QQ", Action = "Call", Porcentajes =  76 },
                new Hands{ Hand = "AQs", Action = "Marginal Call", Porcentajes =  38 },
                new Hands{ Hand = "AQs", Action = "Fold", Porcentajes =  62 },
                new Hands{ Hand = "AKo", Action = "Call", Porcentajes =  100 },
                new Hands{ Hand = "JJ", Action = "Marginal Call", Porcentajes =  9 },
                new Hands{ Hand = "JJ", Action = "Fold", Porcentajes =  91 },
                new Hands{ Hand = "TT", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "TT", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "99", Action = "Marginal Call", Porcentajes =  5 },
                new Hands{ Hand = "99", Action = "Fold", Porcentajes =  95 },
                new Hands{ Hand = "88", Action = "Marginal Call", Porcentajes =  44 },
                new Hands{ Hand = "88", Action = "Fold", Porcentajes =  56 },
                new Hands{ Hand = "77", Action = "Marginal Call", Porcentajes =  6 },
                new Hands{ Hand = "77", Action = "Fold", Porcentajes =  94 },
                new Hands{ Hand = "66", Action = "Marginal Call", Porcentajes =  71 },
                new Hands{ Hand = "66", Action = "Fold", Porcentajes =  29 },
                new Hands{ Hand = "22", Action = "Call", Porcentajes =  100 }
            };

            return ObtainActionHelper.ObtainAction(hands, hand);
        }
        #endregion

    }
}
