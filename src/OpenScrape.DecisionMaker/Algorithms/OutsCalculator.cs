using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

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
            public bool HasOvercards { get; set; }
            public int OvercardCount { get; set; }
            public double OutsToEquity { get; set; }
            public List<string> DrawTypes { get; set; } = [];
        }

        public OutsResult CalculateOuts(List<CardDataOuts> myCards, List<CardDataOuts> communityCards)
        {
            var result = new OutsResult { DrawTypes = new List<string>() };
            var allCards = myCards.Concat(communityCards).ToList();
            var deck = CreateDeck();
            RemoveCards(deck, allCards);

            // 1. Identificar flush draw suit (4+ cartas del mismo palo)
            Suit? flushDrawSuit = GetFlushDrawSuit(allCards);

            // 2. Cartas que completan flush
            var flushOutCards = flushDrawSuit.HasValue
                ? deck.Where(c => c.Suit == flushDrawSuit.Value).ToHashSet(CardComparer.Instance)
                : new HashSet<CardDataOuts>(CardComparer.Instance);

            // 3. Ranks que completan una escalera
            var straightCompletingRanks = GetStraightCompletingRanks(allCards);

            // 4. Cartas que completan escalera (todos los palos)
            var straightOutCards = deck
                .Where(c => straightCompletingRanks.Contains((int)c.Rank))
                .ToHashSet(CardComparer.Instance);

            // 5. Overlap: cartas que completan AMBOS (inclusión-exclusión)
            var overlapCards = new HashSet<CardDataOuts>(
                straightOutCards.Where(c => flushOutCards.Contains(c)),
                CardComparer.Instance);

            int flushOuts = flushOutCards.Count;
            int straightOuts = straightOutCards.Count;
            int overlapOuts = overlapCards.Count;

            // 6. Overcards: cartas de hero más altas que todas las del board
            // Solo se cuentan cuando NO hay flush draw ni OESD (draws principales ya dominan)
            // y NO tienes ya una mano hecha (flush o straight completados)
            int overcardOuts = 0;
            bool hasMainDraw = flushOuts > 0 || straightCompletingRanks.Count >= 2;
            bool hasMadeHand = HasMadeFlush(allCards) || HasFiveCardStraight(
                allCards.Select(c => (int)c.Rank).Distinct().ToHashSet());
            if (communityCards.Count >= 3 && !hasMainDraw && !hasMadeHand)
            {
                var boardMaxRank = communityCards.Max(c => (int)c.Rank);
                var overcards = myCards
                    .Where(c => (int)c.Rank > boardMaxRank)
                    .Select(c => c.Rank)
                    .Distinct()
                    .ToList();

                if (overcards.Count > 0)
                {
                    result.HasOvercards = true;
                    result.OvercardCount = overcards.Count;

                    foreach (var rank in overcards)
                    {
                        // 3 outs por overcard (3 cartas del mismo rank en el deck)
                        // Descontar las que ya son straight outs (gutshot)
                        var overcardCards = deck
                            .Where(c => c.Rank == rank)
                            .ToList();

                        foreach (var oc in overcardCards)
                        {
                            if (!straightOutCards.Contains(oc))
                                overcardOuts++;
                        }
                    }

                    if (overcardOuts > 0)
                        result.DrawTypes.Add($"Overcards ({result.OvercardCount})");
                }
            }

            // Total = flush + straight - overlap + overcards (sin doble conteo)
            result.TotalOuts = flushOuts + straightOuts - overlapOuts + overcardOuts;

            // Clasificar tipos de draw
            if (flushOuts >= 9)
            {
                result.HasFlushDraw = true;
                result.DrawTypes.Add("Flush Draw");
            }

            if (straightCompletingRanks.Count >= 2)
            {
                result.HasOpenEndedStraightDraw = true;
                result.DrawTypes.Add("Open-Ended Straight Draw");
            }
            else if (straightCompletingRanks.Count == 1)
            {
                result.HasGutshotStraightDraw = true;
                result.DrawTypes.Add("Gutshot Straight Draw");
            }

            if (overlapOuts >= 1)
            {
                result.HasStraightFlushDraw = true;
                result.DrawTypes.Add("Straight Flush Draw");
            }

            // Convertir outs a equity (Regla del 2 y 4)
            int cardsToCome = 5 - communityCards.Count;
            result.OutsToEquity = result.TotalOuts * cardsToCome * 2.0;

            return result;
        }

        /// <summary>
        /// Devuelve el palo con flush draw (4+ cartas), o null si no hay.
        /// Si ya hay flush completo (5+), no necesitamos flush outs.
        /// </summary>
        private Suit? GetFlushDrawSuit(List<CardDataOuts> allCards)
        {
            var suitCounts = new Dictionary<Suit, int>();
            foreach (var card in allCards)
            {
                suitCounts[card.Suit] = suitCounts.GetValueOrDefault(card.Suit) + 1;
            }

            var maxSuit = suitCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

            // 4 cartas = flush draw; 5+ = ya tenemos flush, no necesitamos outs
            return maxSuit.Value == 4 ? maxSuit.Key : null;
        }

        /// <summary>
        /// Calcula qué ranks completarían una escalera de 5 cartas.
        /// Para cada rank posible (2-14), prueba si añadirlo crea una escalera nueva.
        /// Incluye detección de rueda (A-2-3-4-5).
        /// </summary>
        private HashSet<int> GetStraightCompletingRanks(List<CardDataOuts> allCards)
        {
            var ranks = allCards.Select(c => (int)c.Rank).Distinct().ToHashSet();

            // Si ya tenemos escalera, no necesitamos outs de escalera
            if (HasFiveCardStraight(ranks))
                return new HashSet<int>();

            var completingRanks = new HashSet<int>();

            for (int testRank = 2; testRank <= 14; testRank++)
            {
                if (ranks.Contains(testRank)) continue;

                ranks.Add(testRank);

                if (HasFiveCardStraight(ranks))
                {
                    completingRanks.Add(testRank);
                }

                ranks.Remove(testRank);
            }

            return completingRanks;
        }

        /// <summary>
        /// Verifica si un conjunto de ranks contiene 5 consecutivos.
        /// Incluye la rueda (A-2-3-4-5) donde Ace=14 actúa como 1.
        /// </summary>
        private bool HasFiveCardStraight(HashSet<int> ranks)
        {
            // Escaleras regulares: 2-3-4-5-6 hasta 10-J-Q-K-A
            for (int low = 2; low <= 10; low++)
            {
                if (ranks.Contains(low) && ranks.Contains(low + 1) &&
                    ranks.Contains(low + 2) && ranks.Contains(low + 3) &&
                    ranks.Contains(low + 4))
                    return true;
            }

            // Rueda: A(14)-2-3-4-5
            if (ranks.Contains(14) && ranks.Contains(2) &&
                ranks.Contains(3) && ranks.Contains(4) && ranks.Contains(5))
                return true;

            return false;
        }

        /// <summary>
        /// Verifica si ya hay flush completo (5+ cartas del mismo palo).
        /// </summary>
        private bool HasMadeFlush(List<CardDataOuts> allCards)
        {
            var suitCounts = new Dictionary<Suit, int>();
            foreach (var card in allCards)
            {
                suitCounts[card.Suit] = suitCounts.GetValueOrDefault(card.Suit) + 1;
            }
            return suitCounts.Values.Any(count => count >= 5);
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

        /// <summary>
        /// Comparador para CardDataOuts basado en Suit+Rank (evita duplicados en HashSet)
        /// </summary>
        private class CardComparer : IEqualityComparer<CardDataOuts>
        {
            public static readonly CardComparer Instance = new();

            public bool Equals(CardDataOuts? x, CardDataOuts? y)
            {
                if (x is null || y is null) return x is null && y is null;
                return x.Suit == y.Suit && x.Rank == y.Rank;
            }

            public int GetHashCode(CardDataOuts obj) =>
                HashCode.Combine(obj.Suit, obj.Rank);
        }
    }
}
