using Emgu.CV.CvEnum;
using OpenScrape.App.Enums;
using static OpenScrape.App.Tables.OpenRaiseVs3Bet;

namespace OpenScrape.App.Tables
{
    public static class OpenRaiseVs3Bet
    {
        public class Hands
        {
            public string Hand { get; set; } = default!;
            public string Action { get; set; } = default!;
            public int Porcentajes { get; set; }
        }

        #region Hands

        private static string OpenRaiseSBvs3BetBB(string hand)
        {
            var hands = new List<Hands>
            {
                new Hands { Hand = "AA", Action = "4Bet x20", Porcentajes = 100 },
                new Hands { Hand = "AKs", Action = "4Bet x20", Porcentajes = 100 },
                new Hands { Hand = "AQs", Action = "4Bet x20", Porcentajes = 100 },
                new Hands { Hand = "AJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "ATs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "A9s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "A9s", Action = "Call", Porcentajes = 51 },
                new Hands { Hand = "A8s", Action = "4Bet x20", Porcentajes = 15 },
                new Hands { Hand = "A8s", Action = "Call", Porcentajes = 85 },
                new Hands { Hand = "A7s", Action = "4Bet x20", Porcentajes = 10 },
                new Hands { Hand = "A7s", Action = "Call", Porcentajes = 90 },
                new Hands { Hand = "A6s", Action = "4Bet x20", Porcentajes = 60 },
                new Hands { Hand = "A6s", Action = "Call", Porcentajes = 40 },
                new Hands { Hand = "A5s", Action = "4Bet x20", Porcentajes = 20 },
                new Hands { Hand = "A5s", Action = "Call", Porcentajes = 80 },
                new Hands { Hand = "A4s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "A4s", Action = "Call", Porcentajes = 51 },
                new Hands { Hand = "A3s", Action = "4Bet x20", Porcentajes = 60 },
                new Hands { Hand = "A3s", Action = "Call", Porcentajes = 40 },
                new Hands { Hand = "A2s", Action = "4Bet x20", Porcentajes = 30 },
                new Hands { Hand = "A2s", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "AKo", Action = "All-In", Porcentajes = 10 },
                new Hands { Hand = "AKo", Action = "4Bet x20", Porcentajes = 90 },
                new Hands { Hand = "KK", Action = "4Bet x20", Porcentajes = 100 },
                new Hands { Hand = "KQs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "KJs", Action = "All-In", Porcentajes = 40 },
                new Hands { Hand = "KJs", Action = "Call", Porcentajes = 60 },
                new Hands { Hand = "KTs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "K9s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "K9s", Action = "Call", Porcentajes = 51 },
                new Hands { Hand = "K8s", Action = "Fold", Porcentajes = 20 },
                new Hands { Hand = "K8s", Action = "4Bet x20", Porcentajes = 30 },
                new Hands { Hand = "K8s", Action = "Marginal Call", Porcentajes = 50 },
                new Hands { Hand = "K7s", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "K7s", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "K5s", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "K5s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "AQo", Action = "Call", Porcentajes = 10 },
                new Hands { Hand = "AQo", Action = "4Bet x20", Porcentajes = 20 },
                new Hands { Hand = "AQo", Action = "All-In", Porcentajes = 70 },
                new Hands { Hand = "KQo", Action = "Call", Porcentajes = 30 },
                new Hands { Hand = "KQo", Action = "4Bet x20", Porcentajes = 70 },
                new Hands { Hand = "QQ", Action = "4Bet x20", Porcentajes = 100 },
                new Hands { Hand = "QJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "QTs", Action = "4Bet x20", Porcentajes = 10 },
                new Hands { Hand = "QTs", Action = "Call", Porcentajes = 90 },
                new Hands { Hand = "Q9s", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "Q9s", Action = "Call", Porcentajes = 60 },
                new Hands { Hand = "AJo", Action = "Call", Porcentajes = 5 },
                new Hands { Hand = "AJo", Action = "4Bet x20", Porcentajes = 95 },
                new Hands { Hand = "KJo", Action = "Marginal Call", Porcentajes = 10 },
                new Hands { Hand = "KJo", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "KJo", Action = "Fold", Porcentajes = 50 },
                new Hands { Hand = "QJo", Action = "4Bet x20", Porcentajes = 20 },
                new Hands { Hand = "QJo", Action = "Fold", Porcentajes = 80 },
                new Hands { Hand = "JJ", Action = "4Bet x20", Porcentajes = 100 },
                new Hands { Hand = "JTs", Action = "4Bet x20", Porcentajes = 20 },
                new Hands { Hand = "JTs", Action = "Call", Porcentajes = 80 },
                new Hands { Hand = "J9s", Action = "Call", Porcentajes = 45 },
                new Hands { Hand = "J9s", Action = "4Bet x20", Porcentajes = 55 },
                new Hands { Hand = "J8s", Action = "4Bet x20", Porcentajes = 20 },
                new Hands { Hand = "J8s", Action = "Fold", Porcentajes = 80 },
                new Hands { Hand = "ATo", Action = "Fold", Porcentajes = 20 },
                new Hands { Hand = "ATo", Action = "4Bet x20", Porcentajes = 80 },
                new Hands { Hand = "KTo", Action = "4Bet x20", Porcentajes = 20 },
                new Hands { Hand = "KTo", Action = "Fold", Porcentajes = 80 },
                new Hands { Hand = "QTo", Action = "4Bet x20", Porcentajes = 10 },
                new Hands { Hand = "QTo", Action = "Fold", Porcentajes = 90 },
                new Hands { Hand = "TT", Action = "All-In", Porcentajes = 30 },
                new Hands { Hand = "TT", Action = "4Bet x20", Porcentajes = 70 },
                new Hands { Hand = "T9s", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "T9s", Action = "Call", Porcentajes = 60 },
                new Hands { Hand = "T8s", Action = "Marginal Call", Porcentajes = 20 },
                new Hands { Hand = "T8s", Action = "Fold", Porcentajes = 30 },
                new Hands { Hand = "T8s", Action = "4Bet x20", Porcentajes = 50 },
                new Hands { Hand = "99", Action = "All-In", Porcentajes = 10 },
                new Hands { Hand = "99", Action = "4Bet x20", Porcentajes = 30 },
                new Hands { Hand = "99", Action = "Call", Porcentajes = 60 },
                new Hands { Hand = "98s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "98s", Action = "Fold", Porcentajes = 51 },
                new Hands { Hand = "97s", Action = "Fold", Porcentajes = 40 },
                new Hands { Hand = "97s", Action = "4Bet x20", Porcentajes = 60 },
                new Hands { Hand = "88", Action = "All-In", Porcentajes = 10 },
                new Hands { Hand = "88", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "88", Action = "Call", Porcentajes = 50 },
                new Hands { Hand = "87s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "87s", Action = "Fold", Porcentajes = 51 },
                new Hands { Hand = "86s", Action = "Fold", Porcentajes = 40 },
                new Hands { Hand = "86s", Action = "4Bet x20", Porcentajes = 60 },
                new Hands { Hand = "77", Action = "All-In", Porcentajes = 20 },
                new Hands { Hand = "77", Action = "4Bet x20", Porcentajes = 39 },
                new Hands { Hand = "77", Action = "Call", Porcentajes = 41 },
                new Hands { Hand = "76s", Action = "Fold", Porcentajes = 40 },
                new Hands { Hand = "76s", Action = "4Bet x20", Porcentajes = 60 },
                new Hands { Hand = "66", Action = "All-In", Porcentajes = 29 },
                new Hands { Hand = "66", Action = "Call", Porcentajes = 31 },
                new Hands { Hand = "66", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "65s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "65s", Action = "Fold", Porcentajes = 51 },
                new Hands { Hand = "55", Action = "All-In", Porcentajes = 20 },
                new Hands { Hand = "55", Action = "4Bet x20", Porcentajes = 30 },
                new Hands { Hand = "55", Action = "Call", Porcentajes = 50 },
                new Hands { Hand = "54s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "54s", Action = "Fold", Porcentajes = 51 },
                new Hands { Hand = "44", Action = "All-In", Porcentajes = 29 },
                new Hands { Hand = "44", Action = "Call", Porcentajes = 31 },
                new Hands { Hand = "44", Action = "4Bet x20", Porcentajes = 40 },
                new Hands { Hand = "43s", Action = "4Bet x20", Porcentajes = 49 },
                new Hands { Hand = "43s", Action = "Fold", Porcentajes = 51 },
                new Hands { Hand = "33", Action = "4Bet x20", Porcentajes = 29 },
                new Hands { Hand = "33", Action = "Call", Porcentajes = 31 },
                new Hands { Hand = "33", Action = "All-In", Porcentajes = 40 },
                new Hands { Hand = "22", Action = "4Bet x20", Porcentajes = 29 },
                new Hands { Hand = "22", Action = "Call", Porcentajes = 31 },
                new Hands { Hand = "22", Action = "All-In", Porcentajes = 40 }
            };


            return ObtainAction(hands, hand);
        }


        private static string OpenRaiseBTNvs3BetBB(string hand)
        {

            var hands = new List<Hands>
            {
                new Hands { Hand = "AA", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "AKs", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "AQs", Action = "4Bet", Porcentajes = 49 },
                new Hands { Hand = "AQs", Action = "Call", Porcentajes = 51 },
                new Hands { Hand = "AJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "ATs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "A9s", Action = "4Bet", Porcentajes = 10 },
                new Hands { Hand = "A9s", Action = "Call", Porcentajes = 90 },
                new Hands { Hand = "A8s", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "A8s", Action = "Call", Porcentajes = 70 },
                new Hands { Hand = "A7s", Action = "4Bet", Porcentajes = 60 },
                new Hands { Hand = "A7s", Action = "Call", Porcentajes = 40 },
                new Hands { Hand = "A6s", Action = "4Bet", Porcentajes = 40 },
                new Hands { Hand = "A6s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "A5s", Action = "All-In", Porcentajes = 20 },
                new Hands { Hand = "A5s", Action = "4Bet", Porcentajes = 25 },
                new Hands { Hand = "A5s", Action = "Call", Porcentajes = 55 },
                new Hands { Hand = "A4s", Action = "4Bet", Porcentajes = 50 },
                new Hands { Hand = "A4s", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "A4s", Action = "Fold", Porcentajes = 20 },
                new Hands { Hand = "AKo", Action = "All-In", Porcentajes = 25 },
                new Hands { Hand = "AKo", Action = "4Bet", Porcentajes = 75 },
                new Hands { Hand = "KK", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "KQs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "KJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "KTs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "K9s", Action = "4Bet", Porcentajes = 20 },
                new Hands { Hand = "K9s", Action = "Call", Porcentajes = 80 },
                new Hands { Hand = "K8s", Action = "4Bet", Porcentajes = 10 },
                new Hands { Hand = "K8s", Action = "Marginal Call", Porcentajes = 20 },
                new Hands { Hand = "K8s", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "K5s", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "K5s", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "AQo", Action = "All-In", Porcentajes = 50 },
                new Hands { Hand = "AQo", Action = "4Bet", Porcentajes = 20 },
                new Hands { Hand = "AQo", Action = "Call", Porcentajes = 30 },
                new Hands { Hand = "KQo", Action = "4Bet", Porcentajes = 60 },
                new Hands { Hand = "KQo", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "KQo", Action = "Fold", Porcentajes = 10 },
                new Hands { Hand = "QQ", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "QJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "QTs", Action = "4Bet", Porcentajes = 35 },
                new Hands { Hand = "QTs", Action = "Call", Porcentajes = 65 },
                new Hands { Hand = "Q9s", Action = "4Bet", Porcentajes = 25 },
                new Hands { Hand = "Q9s", Action = "Fold", Porcentajes = 75 },
                new Hands { Hand = "Q8s", Action = "4Bet", Porcentajes = 5 },
                new Hands { Hand = "Q8s", Action = "Fold", Porcentajes = 95 },
                new Hands { Hand = "AJo", Action = "4Bet", Porcentajes = 60 },
                new Hands { Hand = "AJo", Action = "Marginal Call", Porcentajes = 20 },
                new Hands { Hand = "AJo", Action = "Fold", Porcentajes = 20 },
                new Hands { Hand = "JJ", Action = "All-In", Porcentajes = 20 },
                new Hands { Hand = "JJ", Action = "4Bet", Porcentajes = 70 },
                new Hands { Hand = "JJ", Action = "Call", Porcentajes = 10 },
                new Hands { Hand = "JTs", Action = "4Bet", Porcentajes = 5 },
                new Hands { Hand = "JTs", Action = "Call", Porcentajes = 95 },
                new Hands { Hand = "ATo", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "ATo", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "TT", Action = "All-In", Porcentajes = 15 },
                new Hands { Hand = "TT", Action = "4Bet", Porcentajes = 25 },
                new Hands { Hand = "TT", Action = "Call", Porcentajes = 60 },
                new Hands { Hand = "T9s", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "T9s", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "T9s", Action = "Fold", Porcentajes = 40 },
                new Hands { Hand = "99", Action = "4Bet", Porcentajes = 5 },
                new Hands { Hand = "99", Action = "Call", Porcentajes = 95 },
                new Hands { Hand = "88", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "88", Action = "Call", Porcentajes = 70 },
                new Hands { Hand = "87s", Action = "Marginal Call", Porcentajes = 35 },
                new Hands { Hand = "87s", Action = "Fold", Porcentajes = 65 },
                new Hands { Hand = "77", Action = "4Bet", Porcentajes = 5 },
                new Hands { Hand = "77", Action = "Marginal Call", Porcentajes = 55 },
                new Hands { Hand = "77", Action = "Fold", Porcentajes = 40 },
                new Hands { Hand = "76s", Action = "Marginal Call", Porcentajes = 40 },
                new Hands { Hand = "76s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "66", Action = "Marginal Call", Porcentajes = 60 },
                new Hands { Hand = "66", Action = "Fold", Porcentajes = 40 },
                new Hands { Hand = "65s", Action = "Marginal Call", Porcentajes = 40 },
                new Hands { Hand = "65s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "55", Action = "4Bet", Porcentajes = 5 },
                new Hands { Hand = "55", Action = "Marginal Call", Porcentajes = 35 },
                new Hands { Hand = "55", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "54s", Action = "Marginal Call", Porcentajes = 40 },
                new Hands { Hand = "54s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "44", Action = "Marginal Call", Porcentajes = 25 },
                new Hands { Hand = "44", Action = "Fold", Porcentajes = 75 },
                new Hands { Hand = "33", Action = "4Bet", Porcentajes = 5 },
                new Hands { Hand = "33", Action = "Fold", Porcentajes = 95 }
            };


            return ObtainAction(hands, hand);
        }

        private static string OpenRaiseBTNvs3BetSB(string hand)
        {

            var hands = new List<Hands>
            {
                new Hands { Hand = "AA", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "AKs", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "AQs", Action = "4Bet", Porcentajes = 48 },
                new Hands { Hand = "AQs", Action = "Call", Porcentajes = 52 },
                new Hands { Hand = "AJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "ATs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "A9s", Action = "4Bet", Porcentajes = 56 },
                new Hands { Hand = "A9s", Action = "Call", Porcentajes = 44 },                
                new Hands { Hand = "A8s", Action = "4Bet", Porcentajes = 56 },
                new Hands { Hand = "A8s", Action = "Fold", Porcentajes = 44 },
                new Hands { Hand = "A7s", Action = "4Bet", Porcentajes = 45 },
                new Hands { Hand = "A7s", Action = "Fold", Porcentajes = 55 },
                new Hands { Hand = "A6s", Action = "4Bet", Porcentajes = 74 },
                new Hands { Hand = "A6s", Action = "Fold", Porcentajes = 26 },
                new Hands { Hand = "A5s", Action = "4Bet", Porcentajes = 15 },
                new Hands { Hand = "A5s", Action = "Marginal Call", Porcentajes = 31 },
                new Hands { Hand = "A5s", Action = "Fold", Porcentajes = 54 },
                new Hands { Hand = "A4s", Action = "Marginal Call", Porcentajes = 3 },
                new Hands { Hand = "A4s", Action = "Fold", Porcentajes = 97 },
                new Hands { Hand = "AKo", Action = "All-In", Porcentajes = 2 },
                new Hands { Hand = "AKo", Action = "4Bet", Porcentajes = 98 },
                new Hands { Hand = "KK", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "KQs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "KJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "KTs", Action = "4Bet", Porcentajes = 9 },
                new Hands { Hand = "KTs", Action = "Call", Porcentajes = 91 },
                new Hands { Hand = "K9s", Action = "4Bet", Porcentajes = 11 },
                new Hands { Hand = "K9s", Action = "Marginal Call", Porcentajes = 15 },
                new Hands { Hand = "K9s", Action = "Fold", Porcentajes = 74 },
                new Hands { Hand = "AQo", Action = "All-In", Porcentajes = 43 },
                new Hands { Hand = "AQo", Action = "Call", Porcentajes = 57 },
                new Hands { Hand = "KQo", Action = "4Bet", Porcentajes = 77 },
                new Hands { Hand = "KQo", Action = "Marginal Call", Porcentajes = 13 },
                new Hands { Hand = "KQo", Action = "Fold", Porcentajes = 10 },
                new Hands { Hand = "QQ", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "QJs", Action = "4Bet", Porcentajes = 37 },
                new Hands { Hand = "QJs", Action = "Call", Porcentajes = 63 },
                new Hands { Hand = "QTs", Action = "4Bet", Porcentajes = 83 },
                new Hands { Hand = "QTs", Action = "Call", Porcentajes = 17 },
                new Hands { Hand = "Q9s", Action = "4Bet", Porcentajes = 31 },
                new Hands { Hand = "Q9s", Action = "Fold", Porcentajes = 69 },
                new Hands { Hand = "AJo", Action = "4Bet", Porcentajes = 63 },
                new Hands { Hand = "AJo", Action = "Marginal Call", Porcentajes = 17 },
                new Hands { Hand = "AJo", Action = "Fold", Porcentajes = 20 },
                new Hands { Hand = "JJ", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "JTs", Action = "Marginal Call", Porcentajes = 52 },
                new Hands { Hand = "JTs", Action = "Fold", Porcentajes = 48 },
                new Hands { Hand = "ATo", Action = "4Bet", Porcentajes = 13 },
                new Hands { Hand = "ATo", Action = "Fold", Porcentajes = 87 },
                new Hands { Hand = "TT", Action = "4Bet", Porcentajes = 79 },
                new Hands { Hand = "TT", Action = "Call", Porcentajes = 21 },
                new Hands { Hand = "T9s", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "T9s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "99", Action = "All-In", Porcentajes = 30 },
                new Hands { Hand = "99", Action = "4Bet", Porcentajes = 7 },
                new Hands { Hand = "99", Action = "Call", Porcentajes = 63 },
                new Hands { Hand = "98s", Action = "Marginal Call", Porcentajes = 39 },
                new Hands { Hand = "98s", Action = "Fold", Porcentajes = 61 },
                new Hands { Hand = "88", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "88", Action = "Call", Porcentajes = 70 },
                new Hands { Hand = "87s", Action = "Marginal Call", Porcentajes = 22 },
                new Hands { Hand = "87s", Action = "Fold", Porcentajes = 78 },
                new Hands { Hand = "77", Action = "4Bet", Porcentajes = 15 },
                new Hands { Hand = "77", Action = "Marginal Call", Porcentajes = 54 },
                new Hands { Hand = "77", Action = "Fold", Porcentajes = 31 },
                new Hands { Hand = "76s", Action = "Marginal Call", Porcentajes = 35 },
                new Hands { Hand = "76s", Action = "Fold", Porcentajes = 65 },
                new Hands { Hand = "66", Action = "4Bet", Porcentajes = 4 },
                new Hands { Hand = "66", Action = "Marginal Call", Porcentajes = 37 },
                new Hands { Hand = "66", Action = "Fold", Porcentajes = 59 },
                new Hands { Hand = "65s", Action = "Marginal Call", Porcentajes = 33 },
                new Hands { Hand = "65s", Action = "Fold", Porcentajes = 67 },
                new Hands { Hand = "55", Action = "4Bet", Porcentajes = 10 },
                new Hands { Hand = "55", Action = "Marginal Call", Porcentajes = 17 },
                new Hands { Hand = "55", Action = "Fold", Porcentajes = 73 },
                new Hands { Hand = "54s", Action = "Marginal Call", Porcentajes = 40 },
                new Hands { Hand = "54s", Action = "Fold", Porcentajes = 60 },
                new Hands { Hand = "44", Action = "Marginal Call", Porcentajes = 17 },
                new Hands { Hand = "44", Action = "Fold", Porcentajes = 83 },
                new Hands { Hand = "33", Action = "Marginal Call", Porcentajes = 15 },
                new Hands { Hand = "33", Action = "Fold", Porcentajes = 85 },
                new Hands { Hand = "22", Action = "Marginal Call", Porcentajes = 15 },
                new Hands { Hand = "22", Action = "Fold", Porcentajes = 85 }
            };


            return ObtainAction(hands, hand);
        }

        private static string OpenRaiseCOvs3BetBB(string hand)
        {

            var hands = new List<Hands>
            {
                new Hands { Hand = "AA", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "AKs", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "AQs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "AJs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "ATs", Action = "4Bet", Porcentajes = 32 },
                new Hands { Hand = "ATs", Action = "Call", Porcentajes = 68 },
                new Hands { Hand = "A9s", Action = "4Bet", Porcentajes = 20 },
                new Hands { Hand = "A9s", Action = "Marginal Call", Porcentajes = 70 },
                new Hands { Hand = "A9s", Action = "Fold", Porcentajes = 10 },
                new Hands { Hand = "A8s", Action = "4Bet", Porcentajes = 75 },
                new Hands { Hand = "A8s", Action = "Call", Porcentajes = 25 },
                new Hands { Hand = "A7s", Action = "4Bet", Porcentajes = 46 },
                new Hands { Hand = "A7s", Action = "Marginal Call", Porcentajes = 6 },
                new Hands { Hand = "A7s", Action = "Fold", Porcentajes = 48 },
                new Hands { Hand = "A6s", Action = "4Bet", Porcentajes = 8 },
                new Hands { Hand = "A6s", Action = "Fold", Porcentajes = 92 },
                new Hands { Hand = "A5s", Action = "4Bet", Porcentajes = 10 },
                new Hands { Hand = "A5s", Action = "Marginal Call", Porcentajes = 73 },
                new Hands { Hand = "A5s", Action = "Fold", Porcentajes = 17 },
                new Hands { Hand = "A4s", Action = "Marginal Call", Porcentajes = 17 },
                new Hands { Hand = "A4s", Action = "Fold", Porcentajes = 83 },
                new Hands { Hand = "AKo", Action = "All-In", Porcentajes = 10 },
                new Hands { Hand = "AKo", Action = "4Bet", Porcentajes = 50 },
                new Hands { Hand = "AKo", Action = "Call", Porcentajes = 40 },
                new Hands { Hand = "KK", Action = "4Bet", Porcentajes = 100 },
                new Hands { Hand = "KQs", Action = "Call", Porcentajes = 100 },
                new Hands { Hand = "KJs", Action = "4Bet", Porcentajes = 15 },
                new Hands { Hand = "KJs", Action = "Call", Porcentajes = 85 },
                new Hands { Hand = "KTs", Action = "4Bet", Porcentajes = 42 },
                new Hands { Hand = "KTs", Action = "Call", Porcentajes = 58 },
                new Hands { Hand = "K9s", Action = "4Bet", Porcentajes = 30 },
                new Hands { Hand = "K9s", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "K8s", Action = "4Bet", Porcentajes = 15 },
                new Hands { Hand = "K8s", Action = "Fold", Porcentajes = 85 },
                new Hands { Hand = "K5s", Action = "4Bet", Porcentajes = 20 },
                new Hands { Hand = "K5s", Action = "Fold", Porcentajes = 80 },
                new Hands { Hand = "K4s", Action = "4Bet", Porcentajes = 8 },
                new Hands { Hand = "K4s", Action = "Fold", Porcentajes = 92 },
                new Hands { Hand = "AQo", Action = "4Bet", Porcentajes = 77 },
                new Hands { Hand = "AQo", Action = "Marginal Call", Porcentajes = 8 },
                new Hands { Hand = "AQo", Action = "Fold", Porcentajes = 15 },
                new Hands { Hand = "QQ", Action = "4Bet", Porcentajes = 59 },
                new Hands { Hand = "QQ", Action = "Call", Porcentajes = 41 },
                new Hands { Hand = "QJs", Action = "Marginal Call", Porcentajes = 70 },
                new Hands { Hand = "QJs", Action = "Fold", Porcentajes = 30 },
                new Hands { Hand = "QTs", Action = "Marginal Call", Porcentajes = 47 },
                new Hands { Hand = "QTs", Action = "Fold", Porcentajes = 53 },
                new Hands { Hand = "AJo", Action = "4Bet", Porcentajes = 4 },
                new Hands { Hand = "AJo", Action = "Fold", Porcentajes = 96 },
                new Hands { Hand = "KJo", Action = "4Bet", Porcentajes = 12 },
                new Hands { Hand = "KJo", Action = "Fold", Porcentajes = 88 },
                new Hands { Hand = "JJ", Action = "4Bet", Porcentajes = 20 },
                new Hands { Hand = "JJ", Action = "Call", Porcentajes = 80 },
                new Hands { Hand = "JTs", Action = "Marginal Call", Porcentajes = 4 },
                new Hands { Hand = "JTs", Action = "Fold", Porcentajes = 96 },
                new Hands { Hand = "TT", Action = "4Bet", Porcentajes = 2 },
                new Hands { Hand = "TT", Action = "Marginal Call", Porcentajes = 82 },
                new Hands { Hand = "TT", Action = "Fold", Porcentajes = 16 },
                new Hands { Hand = "99", Action = "Marginal Call", Porcentajes = 45 },
                new Hands { Hand = "99", Action = "Fold", Porcentajes = 55 },
                new Hands { Hand = "98s", Action = "Marginal Call", Porcentajes = 2 },
                new Hands { Hand = "98s", Action = "Fold", Porcentajes = 98 },
                new Hands { Hand = "88", Action = "Marginal Call", Porcentajes = 42 },
                new Hands { Hand = "88", Action = "Fold", Porcentajes = 58 },
                new Hands { Hand = "87s", Action = "Marginal Call", Porcentajes = 26 },
                new Hands { Hand = "87s", Action = "Fold", Porcentajes = 74 },
                new Hands { Hand = "77", Action = "Marginal Call", Porcentajes = 37 },
                new Hands { Hand = "77", Action = "Fold", Porcentajes = 63 },
                new Hands { Hand = "76s", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "76s", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "66", Action = "Marginal Call", Porcentajes = 30 },
                new Hands { Hand = "66", Action = "Fold", Porcentajes = 70 },
                new Hands { Hand = "65s", Action = "Marginal Call", Porcentajes = 54 },
                new Hands { Hand = "65s", Action = "Fold", Porcentajes = 46 },
                new Hands { Hand = "55", Action = "Marginal Call", Porcentajes = 15 },
                new Hands { Hand = "55", Action = "Fold", Porcentajes = 85 },
                new Hands { Hand = "54s", Action = "Marginal Call", Porcentajes = 85 },
                new Hands { Hand = "54s", Action = "Fold", Porcentajes = 15 },
                new Hands { Hand = "44", Action = "Marginal Call", Porcentajes = 4 },
                new Hands { Hand = "44", Action = "Fold", Porcentajes = 96 }
            };


            return ObtainAction(hands, hand);
        }

        #endregion

        private static string ObtainAction(List<Hands> hands, string hand)
        {
            Random random = new Random();

            var porcen = new List<int>();
            var numeroAleatorio = random.Next(101);
            int acumulado = 0;
            int sumaTotal = 0;

            foreach (var item in hands)
            {
                if (item.Hand == hand)
                {
                    sumaTotal += item.Porcentajes;
                    porcen.Add(item.Porcentajes);
                }
            }

            foreach (var kvp in porcen)
            {
                acumulado += kvp;
                if (numeroAleatorio <= acumulado)
                {
                    return hands.FirstOrDefault(x => x.Porcentajes == kvp)?.Action ?? "Fold";
                }
            }

            return "Fold";


        }

        public static string GetOpenRaiseSBvsBB(string hand)
        {            
            return OpenRaiseSBvsBB(hand);
        }

    }
}
