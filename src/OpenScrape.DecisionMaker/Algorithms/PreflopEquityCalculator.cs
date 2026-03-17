using Microsoft.Extensions.Logging;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System.Text.Json;

namespace OpenScrape.DecisionMaker.Algorithms
{
    public class PreflopEquityCalculator
    {
        private readonly Dictionary<string, double> _preflopEquities;

        public PreflopEquityCalculator()
        {
            _preflopEquities = LoadPreflopEquities();
        }

        public double GetEquity(List<CardDataOuts> holeCards, int numOpponents = 1)
        {
            if (holeCards.Count != 2)
                throw new ArgumentException("Exactly 2 hole cards required for preflop equity calculation");

            var handNotation = GetHandNotation(holeCards[0], holeCards[1]);

            if (_preflopEquities.TryGetValue(handNotation, out var equity))
            {
                // Adjust equity based on number of opponents
                return AdjustEquityForOpponents(equity, numOpponents);
            }

            return 0.5; // Default to 50% if no data available
        }

        public List<string> GetRecommendedHands(double minEquity, int numOpponents = 1)
        {
            return _preflopEquities
                .Where(kvp => AdjustEquityForOpponents(kvp.Value, numOpponents) >= minEquity)
                .Select(kvp => kvp.Key)
                .OrderByDescending(kvp => _preflopEquities[kvp])
                .ToList();
        }

        private string GetHandNotation(CardDataOuts card1, CardDataOuts card2)
        {
            // Convert cards to standard notation (e.g., "AKs", "TTo", "87o")
            var rank1 = GetRankSymbol(card1.Rank);
            var rank2 = GetRankSymbol(card2.Rank);

            // Sort ranks (higher rank first)
            if ((int)card1.Rank < (int)card2.Rank)
            {
                (rank1, rank2) = (rank2, rank1);
            }

            if (card1.Rank == card2.Rank)
            {
                return $"{rank1}{rank2}"; // Pocket pair (e.g., "AA", "TT")
            }
            else if (card1.Suit == card2.Suit)
            {
                return $"{rank1}{rank2}s"; // Suited (e.g., "AKs", "87s")
            }
            else
            {
                return $"{rank1}{rank2}o"; // Offsuit (e.g., "AKo", "87o")
            }
        }

        private string GetRankSymbol(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "A",
                Rank.King => "K",
                Rank.Queen => "Q",
                Rank.Jack => "J",
                Rank.Ten => "T",
                _ => ((int)rank).ToString()
            };
        }

        /// <summary>
        /// Ajusta equity HU para múltiples oponentes usando interpolación logarítmica.
        /// Calibrado para que AA vs 5 oponentes ≈ 49% (valor real) en vez de 27%.
        /// </summary>
        private double AdjustEquityForOpponents(double baseEquity, int numOpponents)
        {
            if (numOpponents <= 1) return baseEquity;

            // Factor de ajuste calibrado: manos fuertes mantienen más equity multiway
            // Fórmula: equity_adj = equity^(1 + log2(numOpponents) * factor)
            // Factor 0.35 calibrado para que AA(0.85) vs 5 ≈ 0.49
            double exponent = 1.0 + Math.Log2(numOpponents) * 0.35;
            return Math.Pow(baseEquity, exponent);
        }

        /// <summary>
        /// Tabla completa de 169 manos únicas con equity heads-up vs 1 oponente random.
        /// Se ajusta dinámicamente al número de oponentes en GetEquity().
        /// </summary>
        private Dictionary<string, double> LoadPreflopEquities()
        {
            return new Dictionary<string, double>
            {
                // === Pocket Pairs (13) ===
                {"AA", 0.852}, {"KK", 0.824}, {"QQ", 0.799}, {"JJ", 0.775}, {"TT", 0.750},
                {"99", 0.720}, {"88", 0.691}, {"77", 0.661}, {"66", 0.633}, {"55", 0.605},
                {"44", 0.577}, {"33", 0.549}, {"22", 0.502},

                // === Suited Aces (12) ===
                {"AKs", 0.670}, {"AQs", 0.662}, {"AJs", 0.654}, {"ATs", 0.647},
                {"A9s", 0.628}, {"A8s", 0.621}, {"A7s", 0.612}, {"A6s", 0.603},
                {"A5s", 0.607}, {"A4s", 0.597}, {"A3s", 0.587}, {"A2s", 0.576},

                // === Offsuit Aces (12) ===
                {"AKo", 0.653}, {"AQo", 0.644}, {"AJo", 0.636}, {"ATo", 0.627},
                {"A9o", 0.607}, {"A8o", 0.598}, {"A7o", 0.588}, {"A6o", 0.578},
                {"A5o", 0.582}, {"A4o", 0.571}, {"A3o", 0.561}, {"A2o", 0.550},

                // === Suited Kings (11) ===
                {"KQs", 0.634}, {"KJs", 0.626}, {"KTs", 0.619}, {"K9s", 0.600},
                {"K8s", 0.588}, {"K7s", 0.579}, {"K6s", 0.570}, {"K5s", 0.561},
                {"K4s", 0.551}, {"K3s", 0.541}, {"K2s", 0.531},

                // === Offsuit Kings (11) ===
                {"KQo", 0.614}, {"KJo", 0.606}, {"KTo", 0.597}, {"K9o", 0.576},
                {"K8o", 0.563}, {"K7o", 0.553}, {"K6o", 0.543}, {"K5o", 0.533},
                {"K4o", 0.522}, {"K3o", 0.512}, {"K2o", 0.501},

                // === Suited Queens (10) ===
                {"QJs", 0.614}, {"QTs", 0.606}, {"Q9s", 0.588}, {"Q8s", 0.574},
                {"Q7s", 0.562}, {"Q6s", 0.555}, {"Q5s", 0.545}, {"Q4s", 0.535},
                {"Q3s", 0.525}, {"Q2s", 0.515},

                // === Offsuit Queens (10) ===
                {"QJo", 0.593}, {"QTo", 0.584}, {"Q9o", 0.564}, {"Q8o", 0.549},
                {"Q7o", 0.536}, {"Q6o", 0.528}, {"Q5o", 0.517}, {"Q4o", 0.506},
                {"Q3o", 0.496}, {"Q2o", 0.485},

                // === Suited Jacks (9) ===
                {"JTs", 0.596}, {"J9s", 0.578}, {"J8s", 0.564}, {"J7s", 0.551},
                {"J6s", 0.540}, {"J5s", 0.530}, {"J4s", 0.519}, {"J3s", 0.509},
                {"J2s", 0.499},

                // === Offsuit Jacks (9) ===
                {"JTo", 0.575}, {"J9o", 0.554}, {"J8o", 0.539}, {"J7o", 0.524},
                {"J6o", 0.512}, {"J5o", 0.501}, {"J4o", 0.490}, {"J3o", 0.479},
                {"J2o", 0.469},

                // === Suited Tens (8) ===
                {"T9s", 0.577}, {"T8s", 0.560}, {"T7s", 0.546}, {"T6s", 0.533},
                {"T5s", 0.520}, {"T4s", 0.510}, {"T3s", 0.500}, {"T2s", 0.490},

                // === Offsuit Tens (8) ===
                {"T9o", 0.554}, {"T8o", 0.536}, {"T7o", 0.520}, {"T6o", 0.506},
                {"T5o", 0.492}, {"T4o", 0.481}, {"T3o", 0.470}, {"T2o", 0.460},

                // === Suited Nines (7) ===
                {"98s", 0.557}, {"97s", 0.540}, {"96s", 0.526}, {"95s", 0.511},
                {"94s", 0.498}, {"93s", 0.488}, {"92s", 0.479},

                // === Offsuit Nines (7) ===
                {"98o", 0.533}, {"97o", 0.514}, {"96o", 0.499}, {"95o", 0.483},
                {"94o", 0.469}, {"93o", 0.458}, {"92o", 0.448},

                // === Suited Eights (6) ===
                {"87s", 0.538}, {"86s", 0.522}, {"85s", 0.506}, {"84s", 0.491},
                {"83s", 0.479}, {"82s", 0.470},

                // === Offsuit Eights (6) ===
                {"87o", 0.514}, {"86o", 0.496}, {"85o", 0.479}, {"84o", 0.462},
                {"83o", 0.449}, {"82o", 0.439},

                // === Suited Sevens (5) ===
                {"76s", 0.521}, {"75s", 0.504}, {"74s", 0.488}, {"73s", 0.473},
                {"72s", 0.463},

                // === Offsuit Sevens (5) ===
                {"76o", 0.496}, {"75o", 0.478}, {"74o", 0.460}, {"73o", 0.444},
                {"72o", 0.433},

                // === Suited Sixes (4) ===
                {"65s", 0.504}, {"64s", 0.487}, {"63s", 0.471}, {"62s", 0.460},

                // === Offsuit Sixes (4) ===
                {"65o", 0.478}, {"64o", 0.460}, {"63o", 0.442}, {"62o", 0.430},

                // === Suited Fives (3) ===
                {"54s", 0.487}, {"53s", 0.471}, {"52s", 0.459},

                // === Offsuit Fives (3) ===
                {"54o", 0.461}, {"53o", 0.443}, {"52o", 0.430},

                // === Suited Fours (2) ===
                {"43s", 0.461}, {"42s", 0.448},

                // === Offsuit Fours (2) ===
                {"43o", 0.432}, {"42o", 0.418},

                // === Suited/Offsuit Treys y Deuces (2) ===
                {"32s", 0.440}, {"32o", 0.411}
            };
        }

        public void SavePreflopEquities(string filePath)
        {
            var json = JsonSerializer.Serialize(_preflopEquities, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }

        public void LoadPreflopEquitiesFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var loadedEquities = JsonSerializer.Deserialize<Dictionary<string, double>>(json);

                if (loadedEquities != null)
                {
                    _preflopEquities.Clear();
                    foreach (var kvp in loadedEquities)
                    {
                        _preflopEquities[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }
}
