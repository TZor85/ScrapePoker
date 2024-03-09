using OpenScrape.App.Entities;

namespace OpenScrape.App.Helpers
{
    public static class UserHandHelper
    {
        public static string SetHandValue(TableScrapeResult scrapeResult)
        {
            string hand = string.Empty;

            if (scrapeResult.U0CardForce0 >= scrapeResult.U0CardForce1)
                hand = $"{scrapeResult.U0CardFace0[0]}{scrapeResult.U0CardFace1[0]}";
            else
                hand = $"{scrapeResult.U0CardFace1[0]}{scrapeResult.U0CardFace0[0]}";

            if (scrapeResult.U0CardForce0 != scrapeResult.U0CardForce1)
            {
                if (scrapeResult.U0CardSuit0 == scrapeResult.U0CardSuit1)
                    hand += "s";
                else
                    hand += "o";
            }

            return hand;
        }

        public static bool Exist4Bet(TableScrapeResult scrapeResult)
        {
            var apuesta = 0m;
            var cont = 0;

            foreach (var item in scrapeResult.DataPlayer.Where(w => w.Bet > 1))
            {
                if (item.Bet > apuesta)
                {
                    cont++; // Si cont > 1, hay mas de un jugador que ha subido (hay 4bet)
                    apuesta = item.Bet;
                }
            }

            return cont > 1 ? false : true;
        }

    }
}
