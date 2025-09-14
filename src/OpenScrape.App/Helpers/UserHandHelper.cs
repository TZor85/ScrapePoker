using OpenScrape.App.Entities;

namespace OpenScrape.App.Helpers
{
    public static class UserHandHelper
    {
        public static string SetHandValue(PlayerGameState playerState)
        {
            string hand = string.Empty;

            if (playerState.HoleCard1Rank != 0)
            {
                if (playerState.HoleCard1Rank >= playerState.HoleCard2Rank)
                    hand = $"{playerState.HoleCard1Face[0]}{playerState.HoleCard2Face[0]}";
                else
                    hand = $"{playerState.HoleCard2Face[0]}{playerState.HoleCard1Face[0]}";

                if (playerState.HoleCard1Rank != playerState.HoleCard2Rank)
                {
                    if (playerState.HoleCard1Suit == playerState.HoleCard2Suit)
                        hand += "s";
                    else
                        hand += "o";
                }
            }

            return hand;
        }

        public static bool Exist4Bet(PlayerGameState playerState)
        {
            var apuesta = 0m;
            var cont = 0;

            foreach (var item in playerState.Players.Where(w => w.Bet > 1).OrderBy(o => o.Position))
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
