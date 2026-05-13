using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

using VillainCombo = (OpenScrape.Domain.ValueObjects.CardDataOuts Card1, OpenScrape.Domain.ValueObjects.CardDataOuts Card2, double Weight);

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class MonteCarloSimulator : IMonteCarloSimulator
    {
        private const int HandRankCount = 11; // HandRank values 1-10, índice 0 no usado

        // Deck preconstruido una sola vez — se copia con Array.Copy por iteración
        private static readonly CardDataOuts[] DeckTemplate = CreateDeckTemplate();
        private const int DeckSize = PokerConstants.DeckSize;

        // BitHandEvaluator es stateless (solo constantes) → instancia única compartida
        private static readonly BitHandEvaluator SharedEvaluator = new();

        // Cada thread reutiliza su propio array de deck, evitando allocations
        private static readonly ThreadLocal<CardDataOuts[]> ThreadDeck =
            new(() => new CardDataOuts[DeckSize]);

        // Buffer reutilizable por thread para construir manos de 7 cartas
        private static readonly ThreadLocal<List<CardDataOuts>> ThreadHandBuffer =
            new(() => new List<CardDataOuts>(7));

        // Buffer reutilizable por thread para mano del oponente
        private static readonly ThreadLocal<List<CardDataOuts>> ThreadOpponentBuffer =
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
            public int SkippedSimulations { get; set; }
            public bool IsReliable { get; set; } = true;
            public double BlockedComboPercentage { get; set; }
            public Dictionary<HandRank, int> HandDistribution { get; set; } = new();
        }

        public const double UnreliableThreshold = 0.20; // 20% de combos bloqueados

        public EquityResult CalculateEquity(List<CardDataOuts> myCards, List<CardDataOuts> communityCards,
            int numOpponents, int? iterations = null, VillainRange? villainRange = null)
        {
            // Pre-expandir combos del villano si hay rango definido
            var villainCombos = villainRange != null
                ? BuildVillainCombos(villainRange, myCards, communityCards)
                : null;

            double precomputedTotalWeight = villainCombos != null ? ComputeTotalWeight(villainCombos) : 0;

            // Calcular porcentaje de combos bloqueados para evaluar fiabilidad
            double blockedPercentage = 0;
            if (villainCombos != null && villainRange != null)
            {
                blockedPercentage = CalculateBlockedComboPercentage(villainRange, myCards, communityCards);
            }

            int communityCount = communityCards.Count;

            // River (5 community cards) → enumeración exacta
            if (communityCount == 5)
            {
                var result = ExactEnumerationRiver(myCards, communityCards, numOpponents, villainCombos, precomputedTotalWeight);
                result.BlockedComboPercentage = blockedPercentage;
                result.IsReliable = blockedPercentage <= UnreliableThreshold;
                return result;
            }

            // Turn (4 community cards) → enumeración exacta
            if (communityCount == 4)
            {
                var result = ExactEnumerationTurn(myCards, communityCards, numOpponents, villainCombos, precomputedTotalWeight);
                result.BlockedComboPercentage = blockedPercentage;
                result.IsReliable = blockedPercentage <= UnreliableThreshold;
                return result;
            }

            // Flop (3 cards) o preflop (0 cards) → Monte Carlo con iteraciones altas
            int simulationCount = iterations ?? GetAdaptiveIterations(communityCount);

            var mcResult = RunMonteCarloSimulation(myCards, communityCards, numOpponents,
                simulationCount, villainCombos, precomputedTotalWeight);
            mcResult.BlockedComboPercentage = blockedPercentage;
            mcResult.IsReliable = mcResult.IsReliable && blockedPercentage <= UnreliableThreshold;
            return mcResult;
        }

        private double CalculateBlockedComboPercentage(VillainRange range, List<CardDataOuts> myCards, List<CardDataOuts> communityCards)
        {
            var blocked = new HashSet<(Suit, Rank)>();
            foreach (var c in myCards) blocked.Add((c.Suit, c.Rank));
            foreach (var c in communityCards) blocked.Add((c.Suit, c.Rank));

            int totalCombosInRange = 0;
            int blockedCombosInRange = 0;

            foreach (var (notation, weight) in range.Hands)
            {
                if (weight <= 0) continue;

                var expanded = VillainRange.ExpandHandNotation(notation);
                foreach (var (c1, c2) in expanded)
                {
                    totalCombosInRange++;
                    if (blocked.Contains((c1.Suit, c1.Rank)) || blocked.Contains((c2.Suit, c2.Rank)))
                    {
                        blockedCombosInRange++;
                    }
                }
            }

            return totalCombosInRange > 0 ? (double)blockedCombosInRange / totalCombosInRange : 0;
        }

        /// <summary>
        /// Iteraciones adaptativas por street: más cartas desconocidas = más varianza = más iteraciones.
        /// </summary>
        private static int GetAdaptiveIterations(int communityCount)
        {
            return communityCount switch
            {
                0 => 30_000,  // Preflop: 5 community desconocidas, alta varianza
                3 => 50_000,  // Flop: 2 community desconocidas, varianza media
                _ => PokerConstants.DefaultMonteCarloIterations
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ENUMERACIÓN EXACTA — River (5 community cards conocidas)
        // C(remaining, 2) manos posibles del oponente ≈ 990 evaluaciones
        // ═══════════════════════════════════════════════════════════════════════

        private EquityResult ExactEnumerationRiver(
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards,
            int numOpponents, List<VillainCombo>? villainCombos, double totalWeight)
        {
            // Construir mano del hero (7 cartas)
            var heroHand = new List<CardDataOuts>(7);
            heroHand.AddRange(myCards);
            heroHand.AddRange(communityCards);
            var heroScore = SharedEvaluator.EvaluateHandScore(heroHand);

            // Deck residual: cartas que no son del hero ni community
            var blocked = new HashSet<(Suit, Rank)>();
            foreach (var c in myCards) blocked.Add((c.Suit, c.Rank));
            foreach (var c in communityCards) blocked.Add((c.Suit, c.Rank));

            var remaining = new List<CardDataOuts>();
            foreach (var c in DeckTemplate)
            {
                if (!blocked.Contains((c.Suit, c.Rank)))
                    remaining.Add(c);
            }

            long totalWins = 0, totalTies = 0, totalHands = 0;
            var handDistribution = new int[HandRankCount];
            handDistribution[(int)heroScore.Rank] = 1; // Hero siempre tiene esta mano

            if (villainCombos != null && villainCombos.Count > 0)
            {
                // Enumerar sobre el rango del villano (ponderado)
                double weightedWins = 0, weightedTies = 0, weightedTotal = 0;

                foreach (var combo in villainCombos)
                {
                    // Verificar que ambas cartas están en remaining
                    if (blocked.Contains((combo.Card1.Suit, combo.Card1.Rank)) ||
                        blocked.Contains((combo.Card2.Suit, combo.Card2.Rank)))
                        continue;

                    var oppHand = new List<CardDataOuts>(7)
                    {
                        combo.Card1, combo.Card2
                    };
                    oppHand.AddRange(communityCards);

                    var oppScore = SharedEvaluator.EvaluateHandScore(oppHand);
                    int cmp = heroScore.CompareTo(oppScore);

                    if (cmp > 0) weightedWins += combo.Weight;
                    else if (cmp == 0) weightedTies += combo.Weight;
                    weightedTotal += combo.Weight;
                }

                if (weightedTotal > 0)
                {
                    return new EquityResult
                    {
                        WinProbability = weightedWins / weightedTotal,
                        TieProbability = weightedTies / weightedTotal,
                        LoseProbability = (weightedTotal - weightedWins - weightedTies) / weightedTotal,
                        Equity = (weightedWins + weightedTies * 0.5) / weightedTotal,
                        Simulations = villainCombos.Count,
                        HandDistribution = BuildDistribution(handDistribution)
                    };
                }
            }

            // Sin rango: enumerar C(remaining, 2)
            for (int i = 0; i < remaining.Count; i++)
            {
                for (int j = i + 1; j < remaining.Count; j++)
                {
                    var oppHand = new List<CardDataOuts>(7)
                    {
                        remaining[i], remaining[j]
                    };
                    oppHand.AddRange(communityCards);

                    var oppScore = SharedEvaluator.EvaluateHandScore(oppHand);
                    int cmp = heroScore.CompareTo(oppScore);

                    if (cmp > 0) totalWins++;
                    else if (cmp == 0) totalTies++;
                    totalHands++;
                }
            }

            return new EquityResult
            {
                WinProbability = (double)totalWins / totalHands,
                TieProbability = (double)totalTies / totalHands,
                LoseProbability = (double)(totalHands - totalWins - totalTies) / totalHands,
                Equity = (double)(totalWins + totalTies * 0.5) / totalHands,
                Simulations = (int)totalHands,
                HandDistribution = BuildDistribution(handDistribution)
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // ENUMERACIÓN EXACTA — Turn (4 community cards conocidas)
        // 45 posibles river cards × C(remaining-1, 2) opponent hands ≈ 42K evaluaciones
        // ═══════════════════════════════════════════════════════════════════════

        private EquityResult ExactEnumerationTurn(
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards,
            int numOpponents, List<VillainCombo>? villainCombos, double totalWeight)
        {
            var blocked = new HashSet<(Suit, Rank)>();
            foreach (var c in myCards) blocked.Add((c.Suit, c.Rank));
            foreach (var c in communityCards) blocked.Add((c.Suit, c.Rank));

            var remaining = new List<CardDataOuts>();
            foreach (var c in DeckTemplate)
            {
                if (!blocked.Contains((c.Suit, c.Rank)))
                    remaining.Add(c);
            }

            var handDistribution = new int[HandRankCount];

            if (villainCombos != null && villainCombos.Count > 0)
            {
                // Turn + rango: enumerar river cards × combos del rango
                double weightedWins = 0, weightedTies = 0, weightedTotal = 0;

                // Para cada posible river card
                for (int r = 0; r < remaining.Count; r++)
                {
                    var riverCard = remaining[r];

                    // Hero hand con 5 community
                    var heroHand = new List<CardDataOuts>(7);
                    heroHand.AddRange(myCards);
                    heroHand.AddRange(communityCards);
                    heroHand.Add(riverCard);
                    var heroScore = SharedEvaluator.EvaluateHandScore(heroHand);
                    handDistribution[(int)heroScore.Rank]++;

                    foreach (var combo in villainCombos)
                    {
                        // Skip si carta bloqueada por river card o hero/community
                        if (combo.Card1.Suit == riverCard.Suit && combo.Card1.Rank == riverCard.Rank) continue;
                        if (combo.Card2.Suit == riverCard.Suit && combo.Card2.Rank == riverCard.Rank) continue;

                        var oppHand = new List<CardDataOuts>(7)
                        {
                            combo.Card1, combo.Card2
                        };
                        oppHand.AddRange(communityCards);
                        oppHand.Add(riverCard);

                        var oppScore = SharedEvaluator.EvaluateHandScore(oppHand);
                        int cmp = heroScore.CompareTo(oppScore);

                        if (cmp > 0) weightedWins += combo.Weight;
                        else if (cmp == 0) weightedTies += combo.Weight;
                        weightedTotal += combo.Weight;
                    }
                }

                if (weightedTotal > 0)
                {
                    return new EquityResult
                    {
                        WinProbability = weightedWins / weightedTotal,
                        TieProbability = weightedTies / weightedTotal,
                        LoseProbability = (weightedTotal - weightedWins - weightedTies) / weightedTotal,
                        Equity = (weightedWins + weightedTies * 0.5) / weightedTotal,
                        Simulations = remaining.Count * villainCombos.Count,
                        HandDistribution = BuildDistribution(handDistribution)
                    };
                }
            }

            // Sin rango: enumerar river × C(remaining-1, 2)
            long totalWins = 0, totalTies = 0, totalHands = 0;

            for (int r = 0; r < remaining.Count; r++)
            {
                var riverCard = remaining[r];

                var heroHand = new List<CardDataOuts>(7);
                heroHand.AddRange(myCards);
                heroHand.AddRange(communityCards);
                heroHand.Add(riverCard);
                var heroScore = SharedEvaluator.EvaluateHandScore(heroHand);
                handDistribution[(int)heroScore.Rank]++;

                for (int i = 0; i < remaining.Count; i++)
                {
                    if (i == r) continue;
                    for (int j = i + 1; j < remaining.Count; j++)
                    {
                        if (j == r) continue;

                        var oppHand = new List<CardDataOuts>(7)
                        {
                            remaining[i], remaining[j]
                        };
                        oppHand.AddRange(communityCards);
                        oppHand.Add(riverCard);

                        var oppScore = SharedEvaluator.EvaluateHandScore(oppHand);
                        int cmp = heroScore.CompareTo(oppScore);

                        if (cmp > 0) totalWins++;
                        else if (cmp == 0) totalTies++;
                        totalHands++;
                    }
                }
            }

            return new EquityResult
            {
                WinProbability = (double)totalWins / totalHands,
                TieProbability = (double)totalTies / totalHands,
                LoseProbability = (double)(totalHands - totalWins - totalTies) / totalHands,
                Equity = (double)(totalWins + totalTies * 0.5) / totalHands,
                Simulations = (int)totalHands,
                HandDistribution = BuildDistribution(handDistribution)
            };
        }

        // ═══════════════════════════════════════════════════════════════════════
        // MONTE CARLO — Flop/Preflop (alta varianza, muchas iteraciones)
        // Usa HandScore struct → zero heap allocations por iteración
        // ═══════════════════════════════════════════════════════════════════════

        private EquityResult RunMonteCarloSimulation(
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards,
            int numOpponents, int simulationCount,
            List<VillainCombo>? villainCombos, double precomputedTotalWeight)
        {
            int totalWins = 0;
            int totalTies = 0;
            int totalSkipped = 0;
            var handDistribution = new int[HandRankCount];

            Parallel.For(0, simulationCount,
                () => new int[3 + HandRankCount], // [0]=wins, [1]=ties, [2]=skipped, [3..]=distribution
                (i, state, local) =>
                {
                    var result = RunSingleSimulation(myCards, communityCards, numOpponents,
                        villainCombos, precomputedTotalWeight);

                    if (result.skipped)
                    {
                        local[2]++;
                    }
                    else
                    {
                        local[0] += result.wins;
                        local[1] += result.ties;
                        local[3 + (int)result.bestRank]++;
                    }
                    return local;
                },
                local =>
                {
                    Interlocked.Add(ref totalWins, local[0]);
                    Interlocked.Add(ref totalTies, local[1]);
                    Interlocked.Add(ref totalSkipped, local[2]);
                    for (int r = 0; r < HandRankCount; r++)
                    {
                        Interlocked.Add(ref handDistribution[r], local[3 + r]);
                    }
                });

            int effectiveCount = simulationCount - totalSkipped;
            if (effectiveCount <= 0) effectiveCount = 1;

            var distribution = BuildDistribution(handDistribution);

            return new EquityResult
            {
                WinProbability = (double)totalWins / effectiveCount,
                TieProbability = (double)totalTies / effectiveCount,
                LoseProbability = (double)(effectiveCount - totalWins - totalTies) / effectiveCount,
                Equity = (double)(totalWins + totalTies * 0.5) / effectiveCount,
                Simulations = effectiveCount,
                SkippedSimulations = totalSkipped,
                HandDistribution = distribution
            };
        }

        private (int wins, int ties, HandRank bestRank, bool skipped) RunSingleSimulation(
            List<CardDataOuts> myCards, List<CardDataOuts> communityCards, int numOpponents,
            List<VillainCombo>? villainCombos, double precomputedTotalWeight)
        {
            var deck = ThreadDeck.Value!;
            Array.Copy(DeckTemplate, deck, DeckSize);
            int available = DeckSize;

            available = RemoveKnownCards(deck, available, myCards);
            available = RemoveKnownCards(deck, available, communityCards);

            // Completar cartas comunitarias
            var handBuffer = ThreadHandBuffer.Value!;
            handBuffer.Clear();
            handBuffer.AddRange(myCards);
            handBuffer.AddRange(communityCards);
            int communityNeeded = 5 - communityCards.Count;
            for (int c = 0; c < communityNeeded; c++)
            {
                handBuffer.Add(DrawRandomCard(deck, ref available));
            }

            // Evaluar mano del hero (HandScore struct — zero alloc)
            var heroScore = SharedEvaluator.EvaluateHandScore(handBuffer);

            // Evaluar oponentes
            bool heroLost = false;
            bool heroTied = false;
            var oppBuffer = ThreadOpponentBuffer.Value!;

            for (int i = 0; i < numOpponents; i++)
            {
                CardDataOuts card1, card2;

                if (villainCombos != null && villainCombos.Count > 0)
                {
                    if (!TryDrawFromRange(villainCombos, deck, available, precomputedTotalWeight, out card1, out card2))
                    {
                        // Skip: no contaminar con random cuando el rango está totalmente bloqueado
                        return (0, 0, heroScore.Rank, skipped: true);
                    }
                    available = RemoveCard(deck, available, card1);
                    available = RemoveCard(deck, available, card2);
                }
                else
                {
                    card1 = DrawRandomCard(deck, ref available);
                    card2 = DrawRandomCard(deck, ref available);
                }

                // Construir mano oponente reutilizando buffer ThreadLocal
                oppBuffer.Clear();
                oppBuffer.Add(card1);
                oppBuffer.Add(card2);
                // Community = handBuffer[2..6] (las 5 community cards)
                for (int ci = 2; ci < 7; ci++)
                    oppBuffer.Add(handBuffer[ci]);

                var oppScore = SharedEvaluator.EvaluateHandScore(oppBuffer);
                int comparison = heroScore.CompareTo(oppScore);

                if (comparison < 0) { heroLost = true; break; }
                else if (comparison == 0) heroTied = true;
            }

            int wins = (!heroLost && !heroTied) ? 1 : 0;
            int ties = (!heroLost && heroTied) ? 1 : 0;

            return (wins, ties, heroScore.Rank, skipped: false);
        }

        // ═══════════════════════════════════════════════════════════════════════
        // UTILIDADES
        // ═══════════════════════════════════════════════════════════════════════

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

        private static CardDataOuts DrawRandomCard(CardDataOuts[] deck, ref int available)
        {
            int index = Random.Shared.Next(available);
            var card = deck[index];
            available--;
            deck[index] = deck[available];
            return card;
        }

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
                    if (blocked.Contains((c1.Suit, c1.Rank)) || blocked.Contains((c2.Suit, c2.Rank)))
                        continue;

                    combos.Add((c1, c2, weight));
                }
            }

            return combos;
        }

        /// <summary>
        /// Pre-computa el peso total de los combos del villano (una sola vez, no por iteración).
        /// </summary>
        private static double ComputeTotalWeight(List<VillainCombo> combos)
        {
            double total = 0;
            foreach (var combo in combos)
                total += combo.Weight;
            return total;
        }

        /// <summary>
        /// Selecciona una mano del villano ponderada por frecuencia.
        /// Usa totalWeight pre-computado en vez de recalcularlo.
        /// Si falla después de 10 intentos, retorna false (la iteración se descarta).
        /// </summary>
        private static bool TryDrawFromRange(
            List<VillainCombo> combos, CardDataOuts[] deck, int available,
            double totalWeight, out CardDataOuts card1, out CardDataOuts card2)
        {
            if (totalWeight <= 0)
            {
                card1 = default!;
                card2 = default!;
                return false;
            }

            for (int attempt = 0; attempt < 20; attempt++)
            {
                double roll = Random.Shared.NextDouble() * totalWeight;
                double cumulative = 0;

                foreach (var combo in combos)
                {
                    cumulative += combo.Weight;
                    if (roll <= cumulative)
                    {
                        if (IsCardAvailable(deck, available, combo.Card1) &&
                            IsCardAvailable(deck, available, combo.Card2))
                        {
                            card1 = combo.Card1;
                            card2 = combo.Card2;
                            return true;
                        }
                        break;
                    }
                }
            }

            card1 = default!;
            card2 = default!;
            return false;
        }

        private static bool IsCardAvailable(CardDataOuts[] deck, int available, CardDataOuts target)
        {
            for (int i = 0; i < available; i++)
            {
                if (deck[i].Suit == target.Suit && deck[i].Rank == target.Rank)
                    return true;
            }
            return false;
        }

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

        private static Dictionary<HandRank, int> BuildDistribution(int[] handDistribution)
        {
            var distribution = new Dictionary<HandRank, int>();
            foreach (HandRank rank in Enum.GetValues(typeof(HandRank)))
            {
                distribution[rank] = handDistribution[(int)rank];
            }
            return distribution;
        }
    }
}
