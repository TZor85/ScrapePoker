using Emgu.CV.CvEnum;
using OpenScrape.App.Enums;

namespace OpenScrape.App.Tables
{
    public static class VsOpenRaise
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
            var hands = new List<Hands>();
            var porcen = new List<int>();
            var response = new Dictionary<string, List<int>>();

            
            int numeroAleatorio = GenerarNumeroAleatorio();

            int acumulado = 0;

            hands.Add(new Hands { Hand = "AA", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "AKs", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "AQs", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "AJs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "ATs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "A9s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "A9s", Action = "Call", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "A8s", Action = "4Bet", Porcentajes = 15 });
            hands.Add(new Hands { Hand = "A8s", Action = "Call", Porcentajes = 85 });
            hands.Add(new Hands { Hand = "A7s", Action = "4Bet", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "A7s", Action = "Call", Porcentajes = 90 });
            hands.Add(new Hands { Hand = "A6s", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "A6s", Action = "Call", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "A5s", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "A5s", Action = "Call", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "A4s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "A4s", Action = "Call", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "A3s", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "A3s", Action = "Call", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "A2s", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "A2s", Action = "Fold", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "AKo", Action = "All-In", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "AKo", Action = "4Bet", Porcentajes = 90 });
            hands.Add(new Hands { Hand = "KK", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "KQs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "KJs", Action = "All-In", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "KJs", Action = "Call", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "KTs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "K9s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "K9s", Action = "Call", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "K8s", Action = "Fold", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "K8s", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "K8s", Action = "Marginal Call", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "K7s", Action = "Marginal Call", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "K7s", Action = "Fold", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "K5s", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "K5s", Action = "Fold", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "AQo", Action = "Call", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "AQo", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "AQo", Action = "All-In", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "KQo", Action = "Call", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "KQo", Action = "4Bet", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "QQ", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "QJs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "QTs", Action = "4Bet", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "QTs", Action = "Call", Porcentajes = 90 });
            hands.Add(new Hands { Hand = "Q9s", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "Q9s", Action = "Call", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "AJo", Action = "Call", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "AJo", Action = "4Bet", Porcentajes = 95 });
            hands.Add(new Hands { Hand = "KJo", Action = "Marginal Call", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "KJo", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "KJo", Action = "Fold", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "QJo", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "QJo", Action = "Fold", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "JJ", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "JTs", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "JTs", Action = "Call", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "J9s", Action = "Call", Porcentajes = 45 });
            hands.Add(new Hands { Hand = "J9s", Action = "4Bet", Porcentajes = 55 });
            hands.Add(new Hands { Hand = "J8s", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "J8s", Action = "Fold", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "ATo", Action = "Fold", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "ATo", Action = "4Bet", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "KTo", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "KTo", Action = "Fold", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "QTo", Action = "4Bet", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "QTo", Action = "Fold", Porcentajes = 90 });
            hands.Add(new Hands { Hand = "TT", Action = "All-In", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "TT", Action = "4Bet", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "T9s", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "T9s", Action = "Call", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "T8s", Action = "Marginal Call", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "T8s", Action = "Fold", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "T8s", Action = "4Bet", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "99", Action = "All-In", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "99", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "99", Action = "Call", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "98s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "98s", Action = "Fold", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "97s", Action = "Fold", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "97s", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "88", Action = "All-In", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "88", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "88", Action = "Call", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "87s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "87s", Action = "Fold", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "86s", Action = "Fold", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "86s", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "77", Action = "All-In", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "77", Action = "4Bet", Porcentajes = 39 });
            hands.Add(new Hands { Hand = "77", Action = "Call", Porcentajes = 41 });
            hands.Add(new Hands { Hand = "76s", Action = "Fold", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "76s", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "66", Action = "All-In", Porcentajes = 29 });
            hands.Add(new Hands { Hand = "66", Action = "Call", Porcentajes = 31 });
            hands.Add(new Hands { Hand = "66", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "65s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "65s", Action = "Fold", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "55", Action = "All-In", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "55", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "55", Action = "Call", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "54s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "54s", Action = "Fold", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "44", Action = "All-In", Porcentajes = 29 });
            hands.Add(new Hands { Hand = "44", Action = "Call", Porcentajes = 31 });
            hands.Add(new Hands { Hand = "44", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "43s", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "43s", Action = "Fold", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "33", Action = "4Bet", Porcentajes = 29 });
            hands.Add(new Hands { Hand = "33", Action = "Call", Porcentajes = 31 });
            hands.Add(new Hands { Hand = "33", Action = "All-In", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "22", Action = "4Bet", Porcentajes = 29 });
            hands.Add(new Hands { Hand = "22", Action = "Call", Porcentajes = 31 });
            hands.Add(new Hands { Hand = "22", Action = "All-In", Porcentajes = 40 });
            
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
                    return hands.FirstOrDefault(x => x.Porcentajes == kvp).Action;
                }
            }


            return "Fold";
        }


        private static string OpenRaiseBTNvs3BetBB(string hand)
        {

            var hands = new List<Hands>();
            var porcen = new List<int>();
            var response = new Dictionary<string, List<int>>();


            hands.Add(new Hands { Hand = "AA", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "AKs", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "AQs", Action = "4Bet", Porcentajes = 49 });
            hands.Add(new Hands { Hand = "AQs", Action = "Call", Porcentajes = 51 });
            hands.Add(new Hands { Hand = "AJs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "ATs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "A9s", Action = "4Bet", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "A9s", Action = "Call", Porcentajes = 90 });
            hands.Add(new Hands { Hand = "A8s", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "A8s", Action = "Call", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "A7s", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "A7s", Action = "Call", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "A6s", Action = "4Bet", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "A6s", Action = "Fold", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "A5s", Action = "All-In", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "A5s", Action = "4Bet", Porcentajes = 25 });
            hands.Add(new Hands { Hand = "A5s", Action = "Call", Porcentajes = 55 });
            hands.Add(new Hands { Hand = "A4s", Action = "4Bet", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "A4s", Action = "Marginal Call", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "A4s", Action = "Fold", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "AKo", Action = "All-In", Porcentajes = 25 });
            hands.Add(new Hands { Hand = "AKo", Action = "4Bet", Porcentajes = 75 });
            hands.Add(new Hands { Hand = "KK", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "KQs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "KJs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "KTs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "K9s", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "K9s", Action = "Call", Porcentajes = 80 });
            hands.Add(new Hands { Hand = "K8s", Action = "4Bet", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "K8s", Action = "Marginal Call", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "K8s", Action = "Fold", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "K5s", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "K5s", Action = "Fold", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "AQo", Action = "All-In", Porcentajes = 50 });
            hands.Add(new Hands { Hand = "AQo", Action = "4Bet", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "AQo", Action = "Call", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "KQo", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "KQo", Action = "Marginal Call", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "KQo", Action = "Fold", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "QQ", Action = "4Bet", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "QJs", Action = "Call", Porcentajes = 100 });
            hands.Add(new Hands { Hand = "QTs", Action = "4Bet", Porcentajes = 35 });
            hands.Add(new Hands { Hand = "QTs", Action = "Call", Porcentajes = 65 });
            hands.Add(new Hands { Hand = "Q9s", Action = "4Bet", Porcentajes = 25 });
            hands.Add(new Hands { Hand = "Q9s", Action = "Fold", Porcentajes = 75 });
            hands.Add(new Hands { Hand = "Q8s", Action = "4Bet", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "Q8s", Action = "Fold", Porcentajes = 95 });
            hands.Add(new Hands { Hand = "AJo", Action = "4Bet", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "AJo", Action = "Marginal Call", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "AJo", Action = "Fold", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "JJ", Action = "All-In", Porcentajes = 20 });
            hands.Add(new Hands { Hand = "JJ", Action = "4Bet", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "JJ", Action = "Call", Porcentajes = 10 });
            hands.Add(new Hands { Hand = "JTs", Action = "4Bet", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "JTs", Action = "Call", Porcentajes = 95 });
            hands.Add(new Hands { Hand = "ATo", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "ATo", Action = "Fold", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "TT", Action = "All-In", Porcentajes = 15 });
            hands.Add(new Hands { Hand = "TT", Action = "4Bet", Porcentajes = 25 });
            hands.Add(new Hands { Hand = "TT", Action = "Call", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "T9s", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "T9s", Action = "Marginal Call", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "T9s", Action = "Fold", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "99", Action = "4Bet", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "99", Action = "Call", Porcentajes = 95 });
            hands.Add(new Hands { Hand = "88", Action = "4Bet", Porcentajes = 30 });
            hands.Add(new Hands { Hand = "88", Action = "Call", Porcentajes = 70 });
            hands.Add(new Hands { Hand = "87s", Action = "Marginal Call", Porcentajes = 35 });
            hands.Add(new Hands { Hand = "87s", Action = "Fold", Porcentajes = 65 });
            hands.Add(new Hands { Hand = "77", Action = "4Bet", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "77", Action = "Marginal Call", Porcentajes = 55 });
            hands.Add(new Hands { Hand = "77", Action = "Fold", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "76s", Action = "Marginal Call", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "76s", Action = "Fold", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "66", Action = "Marginal Call", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "66", Action = "Fold", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "65s", Action = "Marginal Call", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "65s", Action = "Fold", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "55", Action = "4Bet", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "55", Action = "Marginal Call", Porcentajes = 35 });
            hands.Add(new Hands { Hand = "55", Action = "Fold", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "54s", Action = "Marginal Call", Porcentajes = 40 });
            hands.Add(new Hands { Hand = "54s", Action = "Fold", Porcentajes = 60 });
            hands.Add(new Hands { Hand = "44", Action = "Marginal Call", Porcentajes = 25 });
            hands.Add(new Hands { Hand = "44", Action = "Fold", Porcentajes = 75 });
            hands.Add(new Hands { Hand = "33", Action = "4Bet", Porcentajes = 5 });
            hands.Add(new Hands { Hand = "33", Action = "Fold", Porcentajes = 95 });


            return "Fold";
        }

        #endregion

        private static int GenerarNumeroAleatorio()
        {
            Random random = new Random();
            return random.Next(101);
        }

        public static string GetOpenRaiseSBvsBB(string hand)
        {            
            return OpenRaiseSBvsBB(hand);
        }

    }
}
