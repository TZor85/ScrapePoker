using OpenScrape.App.Entities;
using OpenScrape.App.Models;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.App.Services
{
    /// <summary>
    /// Evaluador completo de manos de poker para Texas Hold'em
    /// Soporta evaluación con 5, 6 y 7 cartas
    /// </summary>
    public sealed class PokerHandEvaluator
    {
        /// <summary>
        /// Evalúa la mejor mano posible con las cartas disponibles
        /// </summary>
        /// <param name="cards">Lista de cartas (5-7 cartas)</param>
        /// <returns>Resultado de la evaluación</returns>
        public HandEvaluationResult EvaluateHand(IReadOnlyList<BoardData> cards)
        {
            ValidateInput(cards);

            var normalizedCards = NormalizeCards(cards);
            var bestHand = FindBestFiveCardHand(normalizedCards);

            return new HandEvaluationResult
            {
                HandRanking = bestHand.Ranking,
                BestFiveCards = bestHand.Cards,
                HandDescription = GetHandDescription(bestHand),
                HandStrength = CalculateHandStrength(bestHand.Ranking),
                Kickers = bestHand.Kickers,
                AllCards = normalizedCards
            };
        }

        /// <summary>
        /// Encuentra la mejor mano de 5 cartas posible
        /// </summary>
        private BestHandResult FindBestFiveCardHand(List<NormalizedCard> cards)
        {
            var allCombinations = GetAllFiveCardCombinations(cards);
            BestHandResult bestHand = null;

            foreach (var combination in allCombinations)
            {
                var evaluation = EvaluateFiveCards(combination);

                if (bestHand == null || IsStrongerHand(evaluation, bestHand))
                {
                    bestHand = evaluation;
                }
            }

            return bestHand ?? throw new InvalidOperationException("No se pudo evaluar la mano");
        }

        /// <summary>
        /// Evalúa exactamente 5 cartas
        /// </summary>
        private BestHandResult EvaluateFiveCards(List<NormalizedCard> cards)
        {
            var sortedCards = cards.OrderByDescending(c => c.Rank).ToList();

            // Verificar en orden de fuerza (de mayor a menor)
            if (IsRoyalFlush(sortedCards, out var royalFlushCards))
                return new BestHandResult(HandRanking.RoyalFlush, royalFlushCards, []);

            if (IsStraightFlush(sortedCards, out var straightFlushCards))
                return new BestHandResult(HandRanking.StraightFlush, straightFlushCards, []);

            if (IsFourOfAKind(sortedCards, out var fourKindCards, out var fourKindKickers))
                return new BestHandResult(HandRanking.FourOfAKind, fourKindCards, fourKindKickers);

            if (IsFullHouse(sortedCards, out var fullHouseCards))
                return new BestHandResult(HandRanking.FullHouse, fullHouseCards, []);

            if (IsFlush(sortedCards, out var flushCards))
                return new BestHandResult(HandRanking.Flush, flushCards, []);

            if (IsStraight(sortedCards, out var straightCards))
                return new BestHandResult(HandRanking.Straight, straightCards, []);

            if (IsThreeOfAKind(sortedCards, out var threeKindCards, out var threeKindKickers))
                return new BestHandResult(HandRanking.ThreeOfAKind, threeKindCards, threeKindKickers);

            if (IsTwoPair(sortedCards, out var twoPairCards, out var twoPairKickers))
                return new BestHandResult(HandRanking.TwoPair, twoPairCards, twoPairKickers);

            if (IsOnePair(sortedCards, out var onePairCards, out var onePairKickers))
                return new BestHandResult(HandRanking.OnePair, onePairCards, onePairKickers);

            return new BestHandResult(HandRanking.HighCard, sortedCards, []);
        }

        #region Validadores de Jugadas

        private bool IsRoyalFlush(List<NormalizedCard> cards, out List<NormalizedCard> result)
        {
            result = [];
            if (!IsFlush(cards, out var flushCards)) return false;

            var ranks = flushCards.Select(c => c.Rank).OrderDescending().ToList();
            var royalRanks = new[] { Rank.Ace, Rank.King, Rank.Queen, Rank.Jack, Rank.Ten };

            if (ranks.SequenceEqual(royalRanks))
            {
                result = flushCards;
                return true;
            }
            return false;
        }

        private bool IsStraightFlush(List<NormalizedCard> cards, out List<NormalizedCard> result)
        {
            result = [];
            return IsFlush(cards, out var flushCards) &&
                   IsStraight(flushCards, out result);
        }

        private bool IsFourOfAKind(List<NormalizedCard> cards, out List<NormalizedCard> fourCards, out List<NormalizedCard> kickers)
        {
            fourCards = [];
            kickers = [];

            var groups = cards.GroupBy(c => c.Rank).ToList();
            var fourGroup = groups.FirstOrDefault(g => g.Count() == 4);

            if (fourGroup != null)
            {
                fourCards = fourGroup.ToList();
                kickers = cards.Except(fourCards).Take(1).ToList();
                return true;
            }
            return false;
        }

        private bool IsFullHouse(List<NormalizedCard> cards, out List<NormalizedCard> result)
        {
            result = [];
            var groups = cards.GroupBy(c => c.Rank).ToList();

            var threeGroup = groups.FirstOrDefault(g => g.Count() == 3);
            var pairGroup = groups.FirstOrDefault(g => g.Count() == 2);

            if (threeGroup != null && pairGroup != null)
            {
                result = threeGroup.Concat(pairGroup).ToList();
                return true;
            }
            return false;
        }

        private bool IsFlush(List<NormalizedCard> cards, out List<NormalizedCard> result)
        {
            result = [];
            var suitGroups = cards.GroupBy(c => c.Suit).ToList();
            var flushGroup = suitGroups.FirstOrDefault(g => g.Count() >= 5);

            if (flushGroup != null)
            {
                result = flushGroup.OrderByDescending(c => c.Rank).Take(5).ToList();
                return true;
            }
            return false;
        }

        private bool IsStraight(List<NormalizedCard> cards, out List<NormalizedCard> result)
        {
            result = [];
            var distinctRanks = cards.Select(c => c.Rank).Distinct().OrderDescending().ToArray();

            // Verificar escalera normal
            for (int i = 0; i <= distinctRanks.Length - 5; i++)
            {
                if (IsConsecutiveSequence(distinctRanks.Skip(i).Take(5)))
                {
                    result = GetCardsForStraight(cards, distinctRanks.Skip(i).Take(5));
                    return true;
                }
            }

            // Verificar escalera baja (A-2-3-4-5)
            if (distinctRanks.Contains(Rank.Ace) &&
                distinctRanks.Contains(Rank.Five) &&
                distinctRanks.Contains(Rank.Four) &&
                distinctRanks.Contains(Rank.Three) &&
                distinctRanks.Contains(Rank.Two))
            {
                var lowStraightRanks = new[] { Rank.Five, Rank.Four, Rank.Three, Rank.Two, Rank.Ace };
                result = GetCardsForStraight(cards, lowStraightRanks);
                return true;
            }

            return false;
        }

        private bool IsThreeOfAKind(List<NormalizedCard> cards, out List<NormalizedCard> threeCards, out List<NormalizedCard> kickers)
        {
            threeCards = [];
            kickers = [];

            var groups = cards.GroupBy(c => c.Rank).ToList();
            var threeGroup = groups.FirstOrDefault(g => g.Count() == 3);

            if (threeGroup != null)
            {
                threeCards = threeGroup.ToList();
                kickers = cards.Except(threeCards)
                              .OrderByDescending(c => c.Rank)
                              .Take(2)
                              .ToList();
                return true;
            }
            return false;
        }

        private bool IsTwoPair(List<NormalizedCard> cards, out List<NormalizedCard> pairCards, out List<NormalizedCard> kickers)
        {
            pairCards = [];
            kickers = [];

            var groups = cards.GroupBy(c => c.Rank).ToList();
            var pairs = groups.Where(g => g.Count() == 2)
                             .OrderByDescending(g => g.Key)
                             .Take(2)
                             .ToList();

            if (pairs.Count == 2)
            {
                pairCards = pairs.SelectMany(p => p).ToList();
                kickers = cards.Except(pairCards)
                              .OrderByDescending(c => c.Rank)
                              .Take(1)
                              .ToList();
                return true;
            }
            return false;
        }

        private bool IsOnePair(List<NormalizedCard> cards, out List<NormalizedCard> pairCards, out List<NormalizedCard> kickers)
        {
            pairCards = [];
            kickers = [];

            var groups = cards.GroupBy(c => c.Rank).ToList();
            var pairGroup = groups.FirstOrDefault(g => g.Count() == 2);

            if (pairGroup != null)
            {
                pairCards = pairGroup.ToList();
                kickers = cards.Except(pairCards)
                              .OrderByDescending(c => c.Rank)
                              .Take(3)
                              .ToList();
                return true;
            }
            return false;
        }

        #endregion


        #region Métodos Auxiliares

        private void ValidateInput(IReadOnlyList<BoardData> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (cards.Count < 5 || cards.Count > 7)
                throw new ArgumentException("Se requieren entre 5 y 7 cartas", nameof(cards));
        }

        private List<NormalizedCard> NormalizeCards(IReadOnlyList<BoardData> cards)
        {

            return cards.Select(c => new NormalizedCard
            {
                OriginalCard = c,
                Rank = (Rank)c.Force,
                Suit = (Suit)c.Suit
            }).ToList();
        }

        private IEnumerable<List<NormalizedCard>> GetAllFiveCardCombinations(List<NormalizedCard> cards)
        {
            if (cards.Count == 5)
            {
                yield return cards;
                yield break;
            }

            // Generar todas las combinaciones de 5 cartas
            for (int i = 0; i < cards.Count; i++)
            {
                for (int j = i + 1; j < cards.Count; j++)
                {
                    for (int k = j + 1; k < cards.Count; k++)
                    {
                        for (int l = k + 1; l < cards.Count; l++)
                        {
                            for (int m = l + 1; m < cards.Count; m++)
                            {
                                yield return [cards[i], cards[j], cards[k], cards[l], cards[m]];
                            }
                        }
                    }
                }
            }
        }

        private bool IsStrongerHand(BestHandResult hand1, BestHandResult hand2)
        {
            if (hand1.Ranking != hand2.Ranking)
                return hand1.Ranking > hand2.Ranking;
                
            // Comparar kickers si es la misma jugada
            return CompareKickers(hand1, hand2);
        }

        private bool CompareKickers(BestHandResult hand1, BestHandResult hand2)
        {
            var kickers1 = hand1.Cards.Concat(hand1.Kickers).OrderByDescending(c => c.Rank);
            var kickers2 = hand2.Cards.Concat(hand2.Kickers).OrderByDescending(c => c.Rank);
            
            return kickers1.Zip(kickers2, (k1, k2) => k1.Rank.CompareTo(k2.Rank))
                          .FirstOrDefault(comparison => comparison != 0) > 0;
        }

        private bool IsConsecutiveSequence(IEnumerable<Rank> ranks)
        {
            var rankArray = ranks.ToArray();
            for (int i = 0; i < rankArray.Length - 1; i++)
            {
                if ((int)rankArray[i] - (int)rankArray[i + 1] != 1)
                    return false;
            }
            return true;
        }

        private List<NormalizedCard> GetCardsForStraight(List<NormalizedCard> cards, IEnumerable<Rank> ranks)
        {
            return ranks.Select(rank => cards.First(c => c.Rank == rank)).ToList();
        }

        private string GetHandDescription(BestHandResult hand)
        {
            return hand.Ranking switch
            {
                HandRanking.RoyalFlush => "Escalera Real",
                HandRanking.StraightFlush => "Escalera de Color",
                HandRanking.FourOfAKind => "Poker",
                HandRanking.FullHouse => "Full House",
                HandRanking.Flush => "Color",
                HandRanking.Straight => "Escalera",
                HandRanking.ThreeOfAKind => "Trío",
                HandRanking.TwoPair => "Doble Pareja",
                HandRanking.OnePair => "Pareja",
                HandRanking.HighCard => "Carta Alta",
                _ => "Desconocido"
            };
        }

        private double CalculateHandStrength(HandRanking ranking)
        {
            return ranking switch
            {
                HandRanking.RoyalFlush => 1.0,
                HandRanking.StraightFlush => 0.95,
                HandRanking.FourOfAKind => 0.90,
                HandRanking.FullHouse => 0.85,
                HandRanking.Flush => 0.75,
                HandRanking.Straight => 0.65,
                HandRanking.ThreeOfAKind => 0.55,
                HandRanking.TwoPair => 0.45,
                HandRanking.OnePair => 0.25,
                HandRanking.HighCard => 0.10,
                _ => 0.0
            };
        }

        #endregion
    }
}
