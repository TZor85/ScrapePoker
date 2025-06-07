using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Helpers.FlopHelper.RaiseOverLimper
{
    public static class FlopRaiseOverLimperOOPAnalyzerHelper
    {

        #region CheckCall

        public static bool IsActionToCheckCall(FlopAnalyzerHelperReqest request)
        {
            if (IsMediumLowPairVsOvercards(request) && IsSuitedWithDraws(request) && IsHighAceVsLowMediumBoard(request) && IsConnectedWithPairVsDangerousBoard(request))
                return true;

            return false;
        }

        private static bool IsMediumLowPairVsOvercards(FlopAnalyzerHelperReqest request)
        {
            if (!request.PlayerState.HavePocketPair) return false;

            int pairRank = request.PlayerState.HoleCard1Rank;
            if (pairRank >= 10) return false; // No es par medio/bajo

            // Verifica si hay sobrecartas en el flop
            return request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop).Any(card => card.Force > pairRank);
        }

        private static bool IsSuitedWithDraws(FlopAnalyzerHelperReqest request)
        {
            if (!request.PlayerState.IsSuited) return false;

            var flop = request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop);

            // Verifica si hay potencial de color o escalera
            bool hasFlushDraw = flop.Count(c => c.Suit == request.PlayerState.HoleCard1Suit) >= 2;
            bool hasConnectedCards = request.TableScrapeFlopResult.HandIsConnected;

            return hasFlushDraw || hasConnectedCards;
        }

        private static bool IsHighAceVsLowMediumBoard(FlopAnalyzerHelperReqest request)
        {
            if (!request.TableScrapeFlopResult.HaveAce) return false;

            // Verifica si el flop es bajo/medio (todas las cartas menores a Q)
            return request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop).All(card => card.Force < 12);
        }

        private static bool IsConnectedWithPairVsDangerousBoard(FlopAnalyzerHelperReqest request)
        {
            if (!request.TableScrapeFlopResult.HandIsConnected) return false;

            var flop = request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop);
            var pairRank0 = request.PlayerState.HoleCard1Rank;
            var pairRank1 = request.PlayerState.HoleCard2Rank;

            // Verifica si tenemos par y hay cartas peligrosas (A, K, Q)
            bool hasPair = flop.Any(c =>
                c.Force == pairRank0 || c.Force == pairRank1);
            bool hasDangerousCards = flop.Any(c =>
                c.Force == 14 || c.Force == 13 || c.Force == 12);

            return hasPair && hasDangerousCards;
        }

        #endregion

        #region CheckFold

        public static bool IsActionToCheckFold(FlopAnalyzerHelperReqest request)
        {
            if (IsHighCardsVsLowFlop(request) && IsAceWeakKickerVsDangerousBoard(request) && IsKingVsDifficultBoard(request) && IsMediumPairVsMultipleOvercards(request))
                return true;

            return false;
        }

        private static bool IsHighCardsVsLowFlop(FlopAnalyzerHelperReqest request)
        {
            // Verifica si tenemos cartas altas (QJ+) y el flop es bajo (menor que 10)
            bool hasHighCards = request.TableScrapeFlopResult.GetLowestRank >= 11; // J o mayor

            var flop = request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop).ToList();

            bool isLowFlop = flop.All(c => c.Force < 10);
            bool isUnconnectedFlop = IsUnconnectedBoard(flop);

            return hasHighCards && isLowFlop && isUnconnectedFlop;
        }

        private static bool IsAceWeakKickerVsDangerousBoard(FlopAnalyzerHelperReqest request)
        {
            if (!request.TableScrapeFlopResult.HaveAce) return false;

            // Verifica si el kicker es débil (menor que T)
            bool hasWeakKicker = request.TableScrapeFlopResult.GetLowestRank < 10;

            var flop = request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop).ToList();

            // Verifica si el flop es peligroso (coordinado o con cartas altas)
            bool isDangerousFlop = IsCoordinatedBoard(flop) ||
                                  flop.Any(c => c.Force >= 10);

            return hasWeakKicker && isDangerousFlop;
        }

        private static bool IsKingVsDifficultBoard(FlopAnalyzerHelperReqest request)
        {
            if (!request.TableScrapeFlopResult.HaveKing) return false;

            var flop = request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop).ToList();

            // Verifica si hay una J o Q en el flop y el board está descoordinado
            bool hasJackOrQueen = flop.Any(c => c.Force == 11 || c.Force == 12);
            bool isUncoordinated = !IsCoordinatedBoard(flop);

            return hasJackOrQueen && isUncoordinated;
        }

        public static bool IsMediumPairVsMultipleOvercards(FlopAnalyzerHelperReqest request)
        {
            if (!request.PlayerState.HavePocketPair) return false;

            int pairRank = request.PlayerState.HoleCard1Rank;
            if (pairRank >= 10) return false; // No es par medio

            var flop = request.PlayerState.BoardCards.Where(w => w.Position == BoardPosition.Flop).ToList();

            // Cuenta las sobrecartas en el flop
            int overcardsCount = flop.Count(c => c.Force > pairRank);

            return overcardsCount >= 2;
        }

        private static bool IsCoordinatedBoard(List<BoardData> flop)
        {
            var ranks = flop.Select(c => c.Force).OrderBy(r => r).ToList();
            // Verifica si hay 3 cartas consecutivas o potencial de escalera
            return ranks[2] - ranks[0] <= 4 ||
                   ranks[1] - ranks[0] <= 3 && ranks[2] - ranks[1] <= 3;
        }

        private static bool IsUnconnectedBoard(List<BoardData> flop)
        {
            var ranks = flop.Select(c => c.Force).OrderBy(r => r).ToList();
            // Verifica si las cartas están desconectadas (gap > 3)
            return ranks[1] - ranks[0] > 3 || ranks[2] - ranks[1] > 3;
        }

        #endregion
    }

}
