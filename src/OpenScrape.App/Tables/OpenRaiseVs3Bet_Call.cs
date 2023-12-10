using OpenScrape.App.Helpers;

namespace OpenScrape.App.Tables
{
    public class OpenRaiseVs3Bet_Call
    {
        public static string GetOpenRaiseBTNvs3BetSBAndCall(string hand)
        { 
            return OpenRaiseBTNvs3BetSBAndBBCall(hand);
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

        #endregion
    }
}
