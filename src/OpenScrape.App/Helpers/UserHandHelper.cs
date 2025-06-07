using OpenScrape.App.Entities;

namespace OpenScrape.App.Helpers
{
    public static class UserHandHelper
    {
        public static string SetHandValue(PlayerGameState scrapeResult)
        {
            string hand = string.Empty;

            if (scrapeResult.HoleCard1Rank != 0)
            {
                if (scrapeResult.HoleCard1Rank >= scrapeResult.HoleCard2Rank)
                    hand = $"{scrapeResult.HoleCard1Face[0]}{scrapeResult.HoleCard2Face[0]}";
                else
                    hand = $"{scrapeResult.HoleCard2Face[0]}{scrapeResult.HoleCard1Face[0]}";

                if (scrapeResult.HoleCard1Rank != scrapeResult.HoleCard2Rank)
                {
                    if (scrapeResult.HoleCard1Suit == scrapeResult.HoleCard2Suit)
                        hand += "s";
                    else
                        hand += "o";
                }
            }

            return hand;
        }

        public static bool Exist4Bet(PlayerGameState scrapeResult)
        {
            var apuesta = 0m;
            var cont = 0;

            foreach (var item in scrapeResult.Players.Where(w => w.Bet > 1))
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
