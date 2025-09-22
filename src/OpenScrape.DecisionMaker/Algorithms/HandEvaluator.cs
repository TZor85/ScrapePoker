using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class HandEvaluator
    {
        // Multipliers for different hand ranks
        private const long HIGH_CARD_MULTIPLIER = 1;
        private const long PAIR_MULTIPLIER = 1000000;
        private const long TWO_PAIR_MULTIPLIER = 10000000;
        private const long THREE_OF_A_KIND_MULTIPLIER = 100000000;
        private const long STRAIGHT_MULTIPLIER = 1000000000;
        private const long FLUSH_MULTIPLIER = 10000000000;
        private const long FULL_HOUSE_MULTIPLIER = 100000000000;
        private const long FOUR_OF_A_KIND_MULTIPLIER = 1000000000000;
        private const long STRAIGHT_FLUSH_MULTIPLIER = 10000000000000;

        public HandEvaluation EvaluateBestHand(List<CardDataOuts> cards)
        {
            if (cards.Count < 5)
                throw new ArgumentException("At least 5 cards are required to evaluate a hand");

            // Generate all possible 5-card combinations
            var combinations = GenerateCombinations(cards, 5);
            HandEvaluation bestHand = null;

            foreach (var combination in combinations)
            {
                var evaluation = EvaluateFiveCardHand(combination);
                if (bestHand == null || evaluation.Score > bestHand.Score)
                {
                    bestHand = evaluation;
                }
            }

            return bestHand;
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
                evaluation.Score = STRAIGHT_FLUSH_MULTIPLIER * (int)sortedCards[0].Rank;
            }
            else if (sortedRankCounts[0].Value == 4)
            {
                evaluation.Rank = HandRank.FourOfAKind;
                evaluation.Score = FOUR_OF_A_KIND_MULTIPLIER * (int)sortedRankCounts[0].Key;
                evaluation.Kickers.Add((int)sortedRankCounts[1].Key);
            }
            else if (sortedRankCounts[0].Value == 3 && sortedRankCounts[1].Value == 2)
            {
                evaluation.Rank = HandRank.FullHouse;
                evaluation.Score = FULL_HOUSE_MULTIPLIER * (int)sortedRankCounts[0].Key;
                evaluation.Kickers.Add((int)sortedRankCounts[1].Key);
            }
            else if (isFlush)
            {
                evaluation.Rank = HandRank.Flush;
                evaluation.Score = FLUSH_MULTIPLIER;
                foreach (var card in sortedCards)
                {
                    evaluation.Kickers.Add((int)card.Rank);
                }
            }
            else if (isStraight)
            {
                evaluation.Rank = HandRank.Straight;
                evaluation.Score = STRAIGHT_MULTIPLIER * (int)sortedCards[0].Rank;
            }
            else if (sortedRankCounts[0].Value == 3)
            {
                evaluation.Rank = HandRank.ThreeOfAKind;
                evaluation.Score = THREE_OF_A_KIND_MULTIPLIER * (int)sortedRankCounts[0].Key;
                foreach (var kvp in sortedRankCounts.Where(kvp => kvp.Value == 1))
                {
                    evaluation.Kickers.Add((int)kvp.Key);
                }
            }
            else if (sortedRankCounts[0].Value == 2 && sortedRankCounts[1].Value == 2)
            {
                evaluation.Rank = HandRank.TwoPair;
                evaluation.Score = TWO_PAIR_MULTIPLIER * (int)sortedRankCounts[0].Key;
                evaluation.Kickers.Add((int)sortedRankCounts[1].Key);
                foreach (var kvp in sortedRankCounts.Where(kvp => kvp.Value == 1))
                {
                    evaluation.Kickers.Add((int)kvp.Key);
                }
            }
            else if (sortedRankCounts[0].Value == 2)
            {
                evaluation.Rank = HandRank.OnePair;
                evaluation.Score = PAIR_MULTIPLIER * (int)sortedRankCounts[0].Key;
                foreach (var kvp in sortedRankCounts.Where(kvp => kvp.Value == 1))
                {
                    evaluation.Kickers.Add((int)kvp.Key);
                }
            }
            else
            {
                evaluation.Rank = HandRank.HighCard;
                evaluation.Score = HIGH_CARD_MULTIPLIER;
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

            // Check for regular straight
            for (int i = 0; i <= uniqueRanks.Count - 5; i++)
            {
                if (uniqueRanks[i] - uniqueRanks[i + 4] == 4)
                    return true;
            }

            // Check for wheel straight (A-2-3-4-5)
            if (uniqueRanks.Contains(14) && uniqueRanks.Contains(2) &&
                uniqueRanks.Contains(3) && uniqueRanks.Contains(4) && uniqueRanks.Contains(5))
                return true;

            return false;
        }
    }
}
