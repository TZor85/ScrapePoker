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

        private double AdjustEquityForOpponents(double baseEquity, int numOpponents)
        {
            // Simple adjustment: equity decreases with more opponents
            // This is a simplified model - in reality, the relationship is more complex
            return baseEquity / Math.Pow(numOpponents, 0.7);
        }

        private Dictionary<string, double> LoadPreflopEquities()
        {
            // In a real implementation, this would load from a JSON file
            // For now, we'll include some sample data
            return new Dictionary<string, double>
            {
                // Pocket pairs
                {"AA", 0.85}, {"KK", 0.82}, {"QQ", 0.80}, {"JJ", 0.77}, {"TT", 0.75},
                {"99", 0.72}, {"88", 0.69}, {"77", 0.66}, {"66", 0.63}, {"55", 0.60},
                
                // Suited aces
                {"AKs", 0.67}, {"AQs", 0.66}, {"AJs", 0.65}, {"ATs", 0.64},
                {"A9s", 0.63}, {"A8s", 0.62}, {"A7s", 0.61}, {"A6s", 0.60},
                
                // Offsuit aces
                {"AKo", 0.65}, {"AQo", 0.64}, {"AJo", 0.63}, {"ATo", 0.62},
                
                // Suited connectors
                {"KQs", 0.63}, {"KJs", 0.62}, {"KTs", 0.61}, {"QJs", 0.61},
                {"QTs", 0.60}, {"JTs", 0.59}, {"T9s", 0.58}, {"98s", 0.57},
                {"87s", 0.56}, {"76s", 0.55}, {"65s", 0.54}, {"54s", 0.53},
                
                // Other premium hands
                {"KQo", 0.60}, {"KJo", 0.59}, {"QJo", 0.58}
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
