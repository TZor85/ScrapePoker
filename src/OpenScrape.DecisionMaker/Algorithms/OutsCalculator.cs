using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class OutsCalculator
    {
        public class OutsResult
        {
            public int TotalOuts { get; set; }
            public bool HasFlushDraw { get; set; }
            public bool HasOpenEndedStraightDraw { get; set; }
            public bool HasGutshotStraightDraw { get; set; }
            public bool HasStraightFlushDraw { get; set; }
            public double OutsToEquity { get; set; }
            public List<string> DrawTypes { get; set; }
        }

        public OutsResult CalculateOuts(List<CardDataOuts> myCards, List<CardDataOuts> communityCards)
        {
            var result = new OutsResult { DrawTypes = new List<string>() };
            var allCards = myCards.Concat(communityCards).ToList();
            var deck = CreateDeck();
            RemoveCards(deck, allCards);

            // Calculate outs for each type of draw
            var flushOuts = CalculateFlushOuts(allCards, deck);
            var straightOuts = CalculateStraightOuts(allCards, deck);
            var straightFlushOuts = CalculateStraightFlushOuts(allCards, deck);

            // Determine the best draw types
            if (flushOuts >= 9)
            {
                result.HasFlushDraw = true;
                result.DrawTypes.Add("Flush Draw");
            }

            if (straightOuts >= 8)
            {
                result.HasOpenEndedStraightDraw = true;
                result.DrawTypes.Add("Open-Ended Straight Draw");
            }
            else if (straightOuts >= 4)
            {
                result.HasGutshotStraightDraw = true;
                result.DrawTypes.Add("Gutshot Straight Draw");
            }

            if (straightFlushOuts >= 1)
            {
                result.HasStraightFlushDraw = true;
                result.DrawTypes.Add("Straight Flush Draw");
            }

            // Calculate total outs (avoiding double counting)
            result.TotalOuts = CalculateTotalOuts(flushOuts, straightOuts, straightFlushOuts);

            // Convert outs to equity (Rule of 2 and 4)
            int cardsToCome = 5 - communityCards.Count;
            result.OutsToEquity = result.TotalOuts * cardsToCome * 2.0;

            return result;
        }

        private int CalculateFlushOuts(List<CardDataOuts> allCards, List<CardDataOuts> deck)
        {
            var suitCounts = new Dictionary<Suit, int>();

            foreach (var card in allCards)
            {
                suitCounts[card.Suit] = suitCounts.GetValueOrDefault(card.Suit) + 1;
            }

            // Find the suit with the most cards
            var maxSuit = suitCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

            if (maxSuit.Value >= 4) // Flush draw
            {
                return deck.Count(c => c.Suit == maxSuit.Key);
            }

            return 0;
        }

        private int CalculateStraightOuts(List<CardDataOuts> allCards, List<CardDataOuts> deck)
        {
            var ranks = allCards.Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();
            var outs = 0;

            // Check for open-ended straight draw
            for (int i = 0; i <= ranks.Count - 4; i++)
            {
                if (ranks[i + 3] - ranks[i] == 4)
                {
                    // Open-ended: need one card on either end
                    var neededRanks = new List<int> { ranks[i] - 1, ranks[i + 3] + 1 };
                    outs += deck.Count(c => neededRanks.Contains((int)c.Rank));
                }
            }

            // Check for gutshot straight draw
            for (int i = 0; i <= ranks.Count - 3; i++)
            {
                if (ranks[i + 2] - ranks[i] == 3)
                {
                    // Gutshot: need one card in the middle
                    var neededRank = ranks[i] + 1;
                    outs += deck.Count(c => (int)c.Rank == neededRank);
                }
            }

            return outs;
        }

        private int CalculateStraightFlushOuts(List<CardDataOuts> allCards, List<CardDataOuts> deck)
        {
            var suitGroups = allCards.GroupBy(c => c.Suit).ToList();

            foreach (var group in suitGroups)
            {
                if (group.Count() >= 3) // Potential straight flush draw
                {
                    var ranks = group.Select(c => (int)c.Rank).Distinct().OrderBy(r => r).ToList();

                    // Check for straight flush possibilities
                    for (int i = 0; i <= ranks.Count - 3; i++)
                    {
                        if (ranks[i + 2] - ranks[i] <= 4)
                        {
                            return deck.Count(c => c.Suit == group.Key &&
                                ranks.Contains((int)c.Rank) == false);
                        }
                    }
                }
            }

            return 0;
        }

        private int CalculateTotalOuts(int flushOuts, int straightOuts, int straightFlushOuts)
        {
            // Avoid double counting by taking the maximum of overlapping draws
            return Math.Max(flushOuts, Math.Max(straightOuts, straightFlushOuts));
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
    }
}
