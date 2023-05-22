using OpenScrape.App.Enums;

namespace OpenScrape.App.Tables
{
    public static class Vs3Bet
    {
        #region Hands

        private static readonly Dictionary<string, Dictionary<HeroActions3Bet, int>> SBvs3BetBB = new Dictionary<string, Dictionary<HeroActions3Bet, int>>
        {
            { "AA",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "AKs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "AQs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "AJs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "ATs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "A9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.Call, 50 } } },
            { "A8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 15 }, { HeroActions3Bet.Call, 85 } } },
            { "A7s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.Call, 90 } } },
            { "A6s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 60 }, { HeroActions3Bet.Call, 40 } } },
            { "A5s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 80 } } },
            { "A4s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.Call, 50 } } },
            { "A3s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 60 }, { HeroActions3Bet.Call, 40 } } },
            { "A2s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "AKo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 10 }, { HeroActions3Bet.FourBet, 90 } } },
            { "KK",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "KQs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "KJs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 40 }, { HeroActions3Bet.Call, 60 } } },
            { "KTs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "K9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.Call, 50 } } },
            { "K8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.MarginalCall, 50 }, { HeroActions3Bet.Fold, 20 } } },
            { "K7s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "K5s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 40 }, { HeroActions3Bet.Fold, 60 } } },
            { "AQo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 70 }, { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 10 } } },
            { "KQo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 70 }, { HeroActions3Bet.Call, 30 } } },
            { "QQ",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "QJs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "QTs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 90 }, { HeroActions3Bet.FourBet, 10 } } },
            { "Q9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 60 }, { HeroActions3Bet.FourBet, 40 } } },
            { "AJo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 95 }, { HeroActions3Bet.Call, 5 } } },
            { "KJo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 40 }, { HeroActions3Bet.MarginalCall, 10 }, { HeroActions3Bet.Fold, 50 } } },
            { "QJo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "JJ",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "JTs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 80 } } },
            { "J9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 55 }, { HeroActions3Bet.Call, 45 } } },
            { "J8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "ATo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 80 }, { HeroActions3Bet.Fold, 20 } } },
            { "KTo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "KTo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "QTo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.Fold, 90 } } },
            { "TT",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 30 }, { HeroActions3Bet.FourBet, 70 } } },
            { "T9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 40 }, { HeroActions3Bet.Call, 60 } } },
            { "T8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.MarginalCall, 20 }, { HeroActions3Bet.Fold, 30 } } },
            { "99",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 10 }, { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Call, 60 } } },
            { "98s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 35 }, { HeroActions3Bet.MarginalCall, 45 }, { HeroActions3Bet.FourBet, 20 } } },
            { "88",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 25 }, { HeroActions3Bet.Call, 75 } } },
            { "77",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 80 } } },
            { "76s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "66",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 10 }, { HeroActions3Bet.FourBet, 5}, { HeroActions3Bet.MarginalCall, 80 }, { HeroActions3Bet.Fold, 5} } },
            { "65s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "55",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.MarginalCall, 40 }, { HeroActions3Bet.Fold, 30} } },
            { "54s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 25 }, { HeroActions3Bet.Fold, 75 } } },
            { "44",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.MarginalCall, 40 }, { HeroActions3Bet.Fold, 50} } },
            { "33",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "22",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.Fold, 90 } } }
        };


        private static readonly Dictionary<string, Dictionary<HeroActions3Bet, int>> BTNvs3BetBB = new Dictionary<string, Dictionary<HeroActions3Bet, int>>
        {
            { "AA",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "AKs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "AQs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.Call, 50 } } },
            { "AJs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "ATs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "A9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.Call, 90 } } },
            { "A8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Call, 70 } } },
            { "A7s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 60 }, { HeroActions3Bet.Call, 40 } } },
            { "A6s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 40 }, { HeroActions3Bet.Fold, 60 } } },
            { "A5s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 20 }, { HeroActions3Bet.FourBet, 25 }, { HeroActions3Bet.Call, 55 } } },
            { "A4s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.MarginalCall, 30 }, { HeroActions3Bet.Fold, 20} } },
            { "AKo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 25 }, { HeroActions3Bet.FourBet, 75 } } },
            { "KK",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "KQs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "KJs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "KTs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "K9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 80 } } },
            { "K8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.MarginalCall, 20 }, { HeroActions3Bet.Fold, 70 } } },
            { "K5s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "AQo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 50 }, { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 30 } } },
            { "KQo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 60 }, { HeroActions3Bet.MarginalCall, 30 }, { HeroActions3Bet.Fold, 10} } },
            { "QQ",  new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            
            { "QJs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 100 } } },
            { "QTs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 90 }, { HeroActions3Bet.FourBet, 10 } } },
            { "Q9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.Call, 60 }, { HeroActions3Bet.FourBet, 40 } } },
            { "AJo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 95 }, { HeroActions3Bet.Call, 5 } } },
            { "KJo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 40 }, { HeroActions3Bet.MarginalCall, 10 }, { HeroActions3Bet.Fold, 50 } } },
            { "QJo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "JJ", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 100 } } },
            { "JTs", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 80 } } },
            { "J9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 55 }, { HeroActions3Bet.Call, 45 } } },
            { "J8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "ATo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 80 }, { HeroActions3Bet.Fold, 20 } } },
            { "KTo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "KTo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "QTo", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.Fold, 90 } } },
            { "TT", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 30 }, { HeroActions3Bet.FourBet, 70 } } },
            { "T9s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 40 }, { HeroActions3Bet.Call, 60 } } },
            { "T8s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 50 }, { HeroActions3Bet.MarginalCall, 20 }, { HeroActions3Bet.Fold, 30 } } },
            { "99", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 10 }, { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Call, 60 } } },
            { "98s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 35 }, { HeroActions3Bet.MarginalCall, 45 }, { HeroActions3Bet.FourBet, 20 } } },
            { "88", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 25 }, { HeroActions3Bet.Call, 75 } } },
            { "77", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 20 }, { HeroActions3Bet.Call, 80 } } },
            { "76s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "66", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.AllIn, 10 }, { HeroActions3Bet.FourBet, 5}, { HeroActions3Bet.MarginalCall, 80 }, { HeroActions3Bet.Fold, 5} } },
            { "65s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 20 }, { HeroActions3Bet.Fold, 80 } } },
            { "55", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.MarginalCall, 40 }, { HeroActions3Bet.Fold, 30} } },
            { "54s", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.MarginalCall, 25 }, { HeroActions3Bet.Fold, 75 } } },
            { "44", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.MarginalCall, 40 }, { HeroActions3Bet.Fold, 50} } },
            { "33", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 30 }, { HeroActions3Bet.Fold, 70 } } },
            { "22", new Dictionary<HeroActions3Bet, int> { { HeroActions3Bet.FourBet, 10 }, { HeroActions3Bet.Fold, 90 } } }
        };

        #endregion




    }
}
