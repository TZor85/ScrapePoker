using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class MonteCarloSimulator
    {
        private const int DeckSize = 52;
        private const int DefaultIterations = 10000;
        private const int HandRankCount = 11; // HandRank values 1-10, índice 0 no usado

        // Deck preconstruido una sola vez — se copia con Array.Copy por iteración
        private static readonly CardDataOuts[] DeckTemplate = CreateDeckTemplate();

        // HandEvaluator es stateless (solo constantes) → instancia única compartida
        private static readonly HandEvaluator SharedEvaluator = new();

        // Cada thread reutiliza su propio array de deck, evitando allocations
        private static readonly ThreadLocal<CardDataOuts[]> ThreadDeck =
            new(() => new CardDataOuts[DeckSize]);

        // Buffer reutilizable por thread para construir manos de 7 cartas
        private static readonly ThreadLocal<List<CardDataOuts>> ThreadHandBuffer =
            new(() => new List<CardDataOuts>(7));

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
            int simulationCount = iterations ?? DefaultIterations;

            // Contadores compartidos — se agregan con Interlocked desde el estado local de cada thread
            int totalWins = 0;
            int totalTies = 0;
            var handDistribution = new int[HandRankCount];

            // Parallel.For con estado local por thread para minimizar contención
            Parallel.For(0, simulationCount,
                // Inicializar estado local: [0]=wins, [1]=ties, [2..12]=distribución por HandRank
                () => new int[2 + HandRankCount],
                (i, state, local) =>
                {
                    var result = RunSingleSimulation(myCards, communityCards, numOpponents);
                    local[0] += result.wins;
                    local[1] += result.ties;
                    local[2 + (int)result.bestRank]++;
                    return local;
                },
                local =>
                {
                    // Agregar resultados locales a los contadores globales
                    Interlocked.Add(ref totalWins, local[0]);
                    Interlocked.Add(ref totalTies, local[1]);
                    for (int r = 0; r < HandRankCount; r++)
                    {
                        Interlocked.Add(ref handDistribution[r], local[2 + r]);
                    }
                });

            // Construir resultado final
            var distribution = new Dictionary<HandRank, int>();
            foreach (HandRank rank in Enum.GetValues(typeof(HandRank)))
            {
                distribution[rank] = handDistribution[(int)rank];
            }

            return new EquityResult
            {
                WinProbability = (double)totalWins / simulationCount,
                TieProbability = (double)totalTies / simulationCount,
                LoseProbability = (double)(simulationCount - totalWins - totalTies) / simulationCount,
                Equity = (double)(totalWins + totalTies * 0.5) / simulationCount,
                Simulations = simulationCount,
                HandDistribution = distribution
            };
        }

        private (int wins, int ties, HandRank bestRank) RunSingleSimulation(
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards, int numOpponents)
        {
            // Copiar deck template al array ThreadLocal (evita crear lista nueva)
            var deck = ThreadDeck.Value!;
            Array.Copy(DeckTemplate, deck, DeckSize);
            int available = DeckSize;

            // Remover cartas conocidas con swap-to-end (O(1) por carta encontrada)
            available = RemoveKnownCards(deck, available, myCards);
            available = RemoveKnownCards(deck, available, communityCards);

            // Completar cartas comunitarias
            var handBuffer = ThreadHandBuffer.Value!;

            // Construir mano de 7 cartas del hero: hole cards + community + simuladas
            handBuffer.Clear();
            handBuffer.AddRange(myCards);
            handBuffer.AddRange(communityCards);
            int communityNeeded = 5 - communityCards.Count;
            for (int c = 0; c < communityNeeded; c++)
            {
                handBuffer.Add(DrawRandomCard(deck, ref available));
            }

            // Evaluar mano del hero
            var myBestHand = SharedEvaluator.EvaluateBestHand(handBuffer);

            // Las cartas comunitarias simuladas para usar con oponentes
            // (son las últimas 5 cartas del handBuffer: desde index 2 hasta 6)
            var simulatedCommunity = handBuffer.GetRange(2, 5);

            // Evaluar manos de oponentes
            int wins = 0;
            int ties = 0;

            for (int i = 0; i < numOpponents; i++)
            {
                // Robar 2 cartas para el oponente
                var card1 = DrawRandomCard(deck, ref available);
                var card2 = DrawRandomCard(deck, ref available);

                // Construir mano de 7 cartas del oponente
                var opponentFullHand = new List<CardDataOuts>(7);
                opponentFullHand.Add(card1);
                opponentFullHand.Add(card2);
                opponentFullHand.AddRange(simulatedCommunity);

                var opponentBestHand = SharedEvaluator.EvaluateBestHand(opponentFullHand);

                var comparison = CompareHands(myBestHand, opponentBestHand);
                if (comparison > 0) wins++;
                else if (comparison == 0) ties++;
            }

            return (wins, ties, myBestHand.Rank);
        }

        private static CardDataOuts[] CreateDeckTemplate()
        {
            var deck = new CardDataOuts[DeckSize];
            int index = 0;
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    deck[index++] = new CardDataOuts(suit, rank);
                }
            }
            return deck;
        }

        /// <summary>
        /// Remueve cartas conocidas del deck intercambiándolas con el final del rango disponible.
        /// O(n) por carta en vez de O(n²) con RemoveAll.
        /// </summary>
        private static int RemoveKnownCards(CardDataOuts[] deck, int available, List<CardDataOuts> cardsToRemove)
        {
            foreach (var card in cardsToRemove)
            {
                for (int i = 0; i < available; i++)
                {
                    if (deck[i].Suit == card.Suit && deck[i].Rank == card.Rank)
                    {
                        available--;
                        deck[i] = deck[available];
                        break;
                    }
                }
            }
            return available;
        }

        /// <summary>
        /// Roba una carta aleatoria intercambiándola con la última posición disponible.
        /// Elimina la necesidad de List.RemoveAt (que desplaza elementos).
        /// </summary>
        private static CardDataOuts DrawRandomCard(CardDataOuts[] deck, ref int available)
        {
            int index = Random.Shared.Next(available);
            var card = deck[index];
            available--;
            deck[index] = deck[available];
            return card;
        }

        private static int CompareHands(HandEvaluation hand1, HandEvaluation hand2)
        {
            if (hand1.Score > hand2.Score) return 1;
            if (hand1.Score < hand2.Score) return -1;

            // Comparar kickers si los scores son iguales
            for (int i = 0; i < Math.Min(hand1.Kickers.Count, hand2.Kickers.Count); i++)
            {
                if (hand1.Kickers[i] > hand2.Kickers[i]) return 1;
                if (hand1.Kickers[i] < hand2.Kickers[i]) return -1;
            }

            return 0; // Empate
        }
    }
}
