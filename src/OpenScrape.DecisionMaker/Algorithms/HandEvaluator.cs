using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class HandEvaluator : IHandEvaluator
    {
        /// <summary>
        /// Delegación a BitHandEvaluator para HandScore (este evaluador legacy no lo implementa directamente).
        /// </summary>
        public HandScore EvaluateHandScore(List<CardDataOuts> cards)
        {
            var bitEval = new BitHandEvaluator();
            return bitEval.EvaluateHandScore(cards);
        }

        public HandEvaluation EvaluateBestHand(List<CardDataOuts> cards)
        {
            if (cards.Count < 5)
                throw new ArgumentException("At least 5 cards are required to evaluate a hand");

            // Generate all possible 5-card combinations
            var combinations = GenerateCombinations(cards, 5);
            HandEvaluation? bestHand = null;

            foreach (var combination in combinations)
            {
                var evaluation = EvaluateFiveCardHand(combination);
                if (bestHand == null || evaluation.Score > bestHand.Score)
                {
                    bestHand = evaluation;
                }
            }

            return bestHand ?? throw new InvalidOperationException("No se pudo evaluar la mano: sin combinaciones válidas");
        }

        private List<List<CardDataOuts>> GenerateCombinations(List<CardDataOuts> cards, int k)
        {
            var result = new List<List<CardDataOuts>>();
            var current = new List<CardDataOuts>();

            GenerateCombinationsRecursive(cards, k, 0, current, result);
            return result;
        }

        private void GenerateCombinationsRecursive(List<CardDataOuts> cards, int k, int start,
            List<CardDataOuts> current, List<List<CardDataOuts>> result)
        {
            if (current.Count == k)
            {
                result.Add(new List<CardDataOuts>(current));
                return;
            }

            for (int i = start; i < cards.Count; i++)
            {
                current.Add(cards[i]);
                GenerateCombinationsRecursive(cards, k, i + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }

        private HandEvaluation EvaluateFiveCardHand(List<CardDataOuts> cards)
        {
            var evaluation = new HandEvaluation { Cards = cards };

            // Count ranks and suits
            var rankCounts = new Dictionary<Rank, int>();
            var suitCounts = new Dictionary<Suit, int>();

            foreach (var card in cards)
            {
                rankCounts[card.Rank] = rankCounts.GetValueOrDefault(card.Rank) + 1;
                suitCounts[card.Suit] = suitCounts.GetValueOrDefault(card.Suit) + 1;
            }

            // Sort cards by rank for easier straight detection
            var sortedCards = cards.OrderByDescending(c => c.Rank).ToList();
            var ranks = sortedCards.Select(c => (int)c.Rank).ToList();

            // Check for flush
            bool isFlush = suitCounts.Values.Any(count => count >= 5);

            // Check for straight
            bool isStraight = IsStraight(ranks);

            // Get rank counts sorted by frequency
            var sortedRankCounts = rankCounts.OrderByDescending(kvp => kvp.Value)
                .ThenByDescending(kvp => kvp.Key).ToList();

            // Determine hand rank and calculate score
            if (isStraight && isFlush)
            {
                evaluation.Rank = HandRank.StraightFlush;
                evaluation.Score = PokerConstants.StraightFlushMultiplier * GetStraightHighCard(sortedCards);
            }
            else if (sortedRankCounts[0].Value == 4)
            {
                evaluation.Rank = HandRank.FourOfAKind;
                evaluation.Score = PokerConstants.FourOfAKindMultiplier * (int)sortedRankCounts[0].Key;
                evaluation.Kickers.Add((int)sortedRankCounts[1].Key);
            }
            else if (sortedRankCounts[0].Value == 3 && sortedRankCounts[1].Value == 2)
            {
                evaluation.Rank = HandRank.FullHouse;
                evaluation.Score = PokerConstants.FullHouseMultiplier * (int)sortedRankCounts[0].Key;
                evaluation.Kickers.Add((int)sortedRankCounts[1].Key);
            }
            else if (isFlush)
            {
                evaluation.Rank = HandRank.Flush;
                evaluation.Score = PokerConstants.FlushMultiplier;
                foreach (var card in sortedCards)
                {
                    evaluation.Kickers.Add((int)card.Rank);
                }
            }
            else if (isStraight)
            {
                evaluation.Rank = HandRank.Straight;
                evaluation.Score = PokerConstants.StraightMultiplier * GetStraightHighCard(sortedCards);
            }
            else if (sortedRankCounts[0].Value == 3)
            {
                evaluation.Rank = HandRank.ThreeOfAKind;
                evaluation.Score = PokerConstants.ThreeOfAKindMultiplier * (int)sortedRankCounts[0].Key;
                foreach (var kvp in sortedRankCounts.Where(kvp => kvp.Value == 1))
                {
                    evaluation.Kickers.Add((int)kvp.Key);
                }
            }
            else if (sortedRankCounts[0].Value == 2 && sortedRankCounts[1].Value == 2)
            {
                evaluation.Rank = HandRank.TwoPair;
                evaluation.Score = PokerConstants.TwoPairMultiplier * (int)sortedRankCounts[0].Key;
                evaluation.Kickers.Add((int)sortedRankCounts[1].Key);
                foreach (var kvp in sortedRankCounts.Where(kvp => kvp.Value == 1))
                {
                    evaluation.Kickers.Add((int)kvp.Key);
                }
            }
            else if (sortedRankCounts[0].Value == 2)
            {
                evaluation.Rank = HandRank.OnePair;
                evaluation.Score = PokerConstants.PairMultiplier * (int)sortedRankCounts[0].Key;
                foreach (var kvp in sortedRankCounts.Where(kvp => kvp.Value == 1))
                {
                    evaluation.Kickers.Add((int)kvp.Key);
                }
            }
            else
            {
                evaluation.Rank = HandRank.HighCard;
                evaluation.Score = PokerConstants.HighCardMultiplier;
                foreach (var card in sortedCards)
                {
                    evaluation.Kickers.Add((int)card.Rank);
                }
            }

            return evaluation;
        }

        private bool IsStraight(List<int> ranks)
        {
            var uniqueRanks = ranks.Distinct().OrderByDescending(r => r).ToList();

            // Verificar escalera regular
            for (int i = 0; i <= uniqueRanks.Count - 5; i++)
            {
                if (uniqueRanks[i] - uniqueRanks[i + 4] == 4)
                    return true;
            }

            // Verificar wheel (A-2-3-4-5)
            if (IsWheel(uniqueRanks))
                return true;

            return false;
        }

        /// <summary>
        /// Detecta si los rangos forman un wheel (A-2-3-4-5).
        /// </summary>
        private static bool IsWheel(List<int> uniqueRanks)
        {
            return uniqueRanks.Contains(14) && uniqueRanks.Contains(2) &&
                uniqueRanks.Contains(3) && uniqueRanks.Contains(4) && uniqueRanks.Contains(5);
        }

        /// <summary>
        /// Devuelve la carta alta de la escalera. Para el wheel (A-2-3-4-5) es 5, no el As.
        /// </summary>
        private int GetStraightHighCard(List<CardDataOuts> sortedCards)
        {
            var uniqueRanks = sortedCards.Select(c => (int)c.Rank).Distinct()
                .OrderByDescending(r => r).ToList();

            if (IsWheel(uniqueRanks))
                return 5;

            return (int)sortedCards[0].Rank;
        }
    }
}
