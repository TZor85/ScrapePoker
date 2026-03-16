using Microsoft.Extensions.Logging;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class MonteCarloSimulator
    {
        private readonly int _defaultIterations = 10000;

        public MonteCarloSimulator()
        {
        }

        public class EquityResult
        {
            public double WinProbability { get; set; }
            public double TieProbability { get; set; }
            public double LoseProbability { get; set; }
            public double Equity { get; set; }
            public int Simulations { get; set; }
            public Dictionary<HandRank, int> HandDistribution { get; set; } = new();
        }

        public EquityResult CalculateEquity(List<CardDataOuts> myCards, List<CardDataOuts> communityCards,
            int numOpponents, int? iterations = null)
        {
            int simulationCount = iterations ?? _defaultIterations;

            var wins = 0;
            var ties = 0;
            var handDistribution = new ConcurrentDictionary<HandRank, int>();

            // Initialize hand distribution
            foreach (HandRank rank in Enum.GetValues(typeof(HandRank)))
            {
                handDistribution[rank] = 0;
            }

            // Parallel processing for performance
            var results = new ConcurrentBag<(int wins, int ties, HandRank bestRank)>();

            Parallel.For(0, simulationCount, i =>
            {
                var result = RunSingleSimulation(myCards, communityCards, numOpponents);
                results.Add(result);
            });

            // Aggregate results
            foreach (var result in results)
            {
                wins += result.wins;
                ties += result.ties;
                handDistribution[result.bestRank]++;
            }

            var equityResult = new EquityResult
            {
                WinProbability = (double)wins / simulationCount,
                TieProbability = (double)ties / simulationCount,
                LoseProbability = (double)(simulationCount - wins - ties) / simulationCount,
                Equity = (double)(wins + ties * 0.5) / simulationCount,
                Simulations = simulationCount,
                HandDistribution = handDistribution.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return equityResult;
        }

        private (int wins, int ties, HandRank bestRank) RunSingleSimulation(
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards, int numOpponents)
        {
            // Create deck and remove known cards
            var deck = CreateDeck();
            RemoveCards(deck, myCards);
            RemoveCards(deck, communityCards);

            // Complete the community cards
            var simulatedCommunityCards = new List<CardDataOuts>(communityCards);
            while (simulatedCommunityCards.Count < 5)
            {
                simulatedCommunityCards.Add(DrawRandomCard(deck));
            }

            // Create opponent hands
            var opponentHands = new List<List<CardDataOuts>>();
            for (int i = 0; i < numOpponents; i++)
            {
                var opponentHand = new List<CardDataOuts>();
                for (int j = 0; j < 2; j++)
                {
                    opponentHand.Add(DrawRandomCard(deck));
                }
                opponentHands.Add(opponentHand);
            }

            // Evaluate all hands
            var myBestHand = EvaluateBestHand(myCards.Concat(simulatedCommunityCards).ToList());
            var opponentBestHands = opponentHands.Select(hand =>
                EvaluateBestHand(hand.Concat(simulatedCommunityCards).ToList())).ToList();

            // Compare hands
            var wins = 0;
            var ties = 0;

            foreach (var opponentHand in opponentBestHands)
            {
                var comparison = CompareHands(myBestHand, opponentHand);
                if (comparison > 0) wins++;
                else if (comparison == 0) ties++;
            }

            return (wins, ties, myBestHand.Rank);
        }

        private List<CardDataOuts> CreateDeck()
        {
            var deck = new List<CardDataOuts>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    deck.Add(new CardDataOuts(suit, rank));
                }
            }
            return deck;
        }

        private void RemoveCards(List<CardDataOuts> deck, List<CardDataOuts> cardsToRemove)
        {
            foreach (var card in cardsToRemove)
            {
                deck.RemoveAll(c => c.Suit == card.Suit && c.Rank == card.Rank);
            }
        }

        private CardDataOuts DrawRandomCard(List<CardDataOuts> deck)
        {
            int index = Random.Shared.Next(deck.Count);
            var card = deck[index];
            deck.RemoveAt(index);
            return card;
        }

        private HandEvaluation EvaluateBestHand(List<CardDataOuts> cards)
        {
            var evaluator = new HandEvaluator();
            return evaluator.EvaluateBestHand(cards);
        }

        private int CompareHands(HandEvaluation hand1, HandEvaluation hand2)
        {
            if (hand1.Score > hand2.Score) return 1;
            if (hand1.Score < hand2.Score) return -1;

            // Compare kickers if scores are equal
            for (int i = 0; i < Math.Min(hand1.Kickers.Count, hand2.Kickers.Count); i++)
            {
                if (hand1.Kickers[i] > hand2.Kickers[i]) return 1;
                if (hand1.Kickers[i] < hand2.Kickers[i]) return -1;
            }

            return 0; // Tie
        }
    }
}
