using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VillainCombo = (OpenScrape.Domain.ValueObjects.CardDataOuts Card1, OpenScrape.Domain.ValueObjects.CardDataOuts Card2, double Weight);

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
            int numOpponents, int? iterations = null, VillainRange? villainRange = null)
        {
            int simulationCount = iterations ?? DefaultIterations;

            // Pre-expandir combos del villano si hay rango definido
            var villainCombos = villainRange != null
                ? BuildVillainCombos(villainRange, myCards, communityCards)
                : null;

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
                    var result = RunSingleSimulation(myCards, communityCards, numOpponents, villainCombos);
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
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards, int numOpponents,
            List<VillainCombo>? villainCombos = null)
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

            // Evaluar manos de oponentes — hero necesita ganar a TODOS para ganar el bote
            bool heroLost = false;
            bool heroTied = false;

            for (int i = 0; i < numOpponents; i++)
            {
                CardDataOuts card1, card2;

                if (villainCombos != null && villainCombos.Count > 0)
                {
                    // Seleccionar mano ponderada del rango del villano
                    if (!TryDrawFromRange(villainCombos, deck, available, out card1, out card2))
                    {
                        // Fallback: si todas las manos del rango están bloqueadas, aleatorio
                        card1 = DrawRandomCard(deck, ref available);
                        card2 = DrawRandomCard(deck, ref available);
                    }
                    else
                    {
                        // Remover las cartas seleccionadas del deck disponible
                        available = RemoveCard(deck, available, card1);
                        available = RemoveCard(deck, available, card2);
                    }
                }
                else
                {
                    // Sin rango: aleatorio puro (comportamiento original)
                    card1 = DrawRandomCard(deck, ref available);
                    card2 = DrawRandomCard(deck, ref available);
                }

                // Construir mano de 7 cartas del oponente
                var opponentFullHand = new List<CardDataOuts>(7);
                opponentFullHand.Add(card1);
                opponentFullHand.Add(card2);
                opponentFullHand.AddRange(simulatedCommunity);

                var opponentBestHand = SharedEvaluator.EvaluateBestHand(opponentFullHand);

                var comparison = CompareHands(myBestHand, opponentBestHand);
                if (comparison < 0) { heroLost = true; break; }
                else if (comparison == 0) heroTied = true;
            }

            // Win = ganó a todos, Tie = empató con al menos uno sin perder, Loss = perdió contra alguno
            int wins = (!heroLost && !heroTied) ? 1 : 0;
            int ties = (!heroLost && heroTied) ? 1 : 0;

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

        /// <summary>
        /// Pre-expande todas las combinaciones del rango del villano con sus pesos,
        /// filtrando las que colisionan con cartas del hero o community.
        /// Se calcula una sola vez antes del loop de simulación.
        /// </summary>
        private static List<VillainCombo> BuildVillainCombos(
            VillainRange range, List<CardDataOuts> myCards, List<CardDataOuts> communityCards)
        {
            var blocked = new HashSet<(Suit, Rank)>();
            foreach (var c in myCards) blocked.Add((c.Suit, c.Rank));
            foreach (var c in communityCards) blocked.Add((c.Suit, c.Rank));

            var combos = new List<VillainCombo>();
            foreach (var (notation, weight) in range.Hands)
            {
                if (weight <= 0) continue;

                var expanded = VillainRange.ExpandHandNotation(notation);
                foreach (var (c1, c2) in expanded)
                {
                    // Descartar combos que colisionen con cartas conocidas
                    if (blocked.Contains((c1.Suit, c1.Rank)) || blocked.Contains((c2.Suit, c2.Rank)))
                        continue;

                    combos.Add((c1, c2, weight));
                }
            }

            return combos;
        }

        /// <summary>
        /// Selecciona una mano del villano ponderada por frecuencia.
        /// Verifica que ambas cartas sigan disponibles en el deck.
        /// </summary>
        private static bool TryDrawFromRange(
            List<VillainCombo> combos, CardDataOuts[] deck, int available,
            out CardDataOuts card1, out CardDataOuts card2)
        {
            // Calcular peso total
            double totalWeight = 0;
            foreach (var combo in combos)
                totalWeight += combo.Weight;

            if (totalWeight <= 0)
            {
                card1 = default;
                card2 = default;
                return false;
            }

            // Intentar hasta 10 veces (por si la mano elegida está bloqueada por community simulada)
            for (int attempt = 0; attempt < 10; attempt++)
            {
                double roll = Random.Shared.NextDouble() * totalWeight;
                double cumulative = 0;

                foreach (var combo in combos)
                {
                    cumulative += combo.Weight;
                    if (roll <= cumulative)
                    {
                        // Verificar que ambas cartas están en el deck disponible
                        if (IsCardAvailable(deck, available, combo.Card1) &&
                            IsCardAvailable(deck, available, combo.Card2))
                        {
                            card1 = combo.Card1;
                            card2 = combo.Card2;
                            return true;
                        }
                        break; // Carta bloqueada, reintentar
                    }
                }
            }

            card1 = default;
            card2 = default;
            return false;
        }

        /// <summary>
        /// Verifica si una carta específica está disponible en el deck.
        /// </summary>
        private static bool IsCardAvailable(CardDataOuts[] deck, int available, CardDataOuts target)
        {
            for (int i = 0; i < available; i++)
            {
                if (deck[i].Suit == target.Suit && deck[i].Rank == target.Rank)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Remueve una carta específica del deck (swap-to-end).
        /// </summary>
        private static int RemoveCard(CardDataOuts[] deck, int available, CardDataOuts card)
        {
            for (int i = 0; i < available; i++)
            {
                if (deck[i].Suit == card.Suit && deck[i].Rank == card.Rank)
                {
                    available--;
                    deck[i] = deck[available];
                    return available;
                }
            }
            return available;
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
