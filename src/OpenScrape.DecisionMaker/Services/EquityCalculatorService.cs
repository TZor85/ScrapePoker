using Microsoft.Extensions.Logging;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenScrape.DecisionMaker.Services
{
    public class EquityCalculatorService
    {
        private readonly MonteCarloSimulator _monteCarloSimulator;
        private readonly OutsCalculator _outsCalculator;
        private readonly PreflopEquityCalculator _preflopEquityCalculator;

        public EquityCalculatorService(
            MonteCarloSimulator monteCarloSimulator,
            OutsCalculator outsCalculator,
            PreflopEquityCalculator preflopEquityCalculator)
        {
            _monteCarloSimulator = monteCarloSimulator;
            _outsCalculator = outsCalculator;
            _preflopEquityCalculator = preflopEquityCalculator;
        }

        public class FullEquityAnalysis
        {
            public double OverallEquity { get; set; }
            public double WinProbability { get; set; }
            public double TieProbability { get; set; }
            public int Outs { get; set; }
            public double OutsToEquity { get; set; }
            public List<string> DrawTypes { get; set; }
            public Dictionary<HandRank, int> HandDistribution { get; set; }
            public string RecommendedAction { get; set; }
            public double PotOdds { get; set; }
            public double ExpectedValue { get; set; }
        }

        public FullEquityAnalysis CalculateFullEquity(
            List<CardDataOuts> myCards,
            List<CardDataOuts> communityCards,
            int numOpponents,
            double potSize,
            double callAmount,
            int? monteCarloIterations = null)
        {
            var analysis = new FullEquityAnalysis
            {
                DrawTypes = new List<string>(),
                HandDistribution = new Dictionary<HandRank, int>()
            };

            try
            {
                // Calculate pot odds
                analysis.PotOdds = callAmount / (potSize + callAmount);

                // Determine calculation method based on game stage
                if (communityCards.Count == 0) // Preflop
                {
                    analysis.OverallEquity = _preflopEquityCalculator.GetEquity(myCards, numOpponents);
                    analysis.WinProbability = analysis.OverallEquity;
                    analysis.TieProbability = 0.0;
                }
                else // Postflop
                {
                    // Run Monte Carlo simulation
                    var monteCarloResult = _monteCarloSimulator.CalculateEquity(
                        myCards, communityCards, numOpponents, monteCarloIterations);

                    analysis.OverallEquity = monteCarloResult.Equity;
                    analysis.WinProbability = monteCarloResult.WinProbability;
                    analysis.TieProbability = monteCarloResult.TieProbability;
                    analysis.HandDistribution = monteCarloResult.HandDistribution;

                    // Calculate outs
                    var outsResult = _outsCalculator.CalculateOuts(myCards, communityCards);
                    analysis.Outs = outsResult.TotalOuts;
                    analysis.OutsToEquity = outsResult.OutsToEquity;
                    analysis.DrawTypes = outsResult.DrawTypes;
                }

                // Calculate expected value
                analysis.ExpectedValue = CalculateExpectedValue(analysis.OverallEquity, potSize, callAmount);

                // Generate recommendation
                analysis.RecommendedAction = GenerateRecommendation(analysis);

                return analysis;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private double CalculateExpectedValue(double equity, double potSize, double callAmount)
        {
            double winAmount = potSize;
            double loseAmount = -callAmount;

            return equity * winAmount + (1 - equity) * loseAmount;
        }

        private string GenerateRecommendation(FullEquityAnalysis analysis)
        {
            double equity = analysis.OverallEquity;
            double potOdds = analysis.PotOdds;
            double expectedValue = analysis.ExpectedValue;

            // Decision thresholds (these can be adjusted based on strategy)
            const double raiseThreshold = 0.6;
            const double callThreshold = 0.3;
            const double bluffThreshold = 0.25;

            if (expectedValue > 0)
            {
                if (equity > raiseThreshold)
                {
                    return "RAISE/BET";
                }
                else if (equity > callThreshold)
                {
                    return "CALL";
                }
                else
                {
                    return "CALL (Pot Odds)";
                }
            }
            else
            {
                if (equity > bluffThreshold && analysis.Outs > 8)
                {
                    return "SEMI-BLUFF";
                }
                else
                {
                    return "FOLD";
                }
            }
        }

        public string GetEquityAnalysisSummary(FullEquityAnalysis analysis)
        {
            var summary = new StringBuilder();
            summary.AppendLine("=== EQUITY ANALYSIS ===");
            summary.AppendLine($"Overall Equity: {analysis.OverallEquity:P2}");
            summary.AppendLine($"Win Probability: {analysis.WinProbability:P2}");
            summary.AppendLine($"Tie Probability: {analysis.TieProbability:P2}");
            summary.AppendLine($"Pot Odds: {analysis.PotOdds:P2}");
            summary.AppendLine($"Expected Value: {analysis.ExpectedValue:C2}");

            if (analysis.Outs > 0)
            {
                summary.AppendLine($"Outs: {analysis.Outs}");
                summary.AppendLine($"Outs to Equity: {analysis.OutsToEquity:P2}");
                summary.AppendLine($"Draw Types: {string.Join(", ", analysis.DrawTypes)}");
            }

            summary.AppendLine($"Recommended Action: {analysis.RecommendedAction}");

            if (analysis.HandDistribution.Any())
            {
                summary.AppendLine("\n=== HAND DISTRIBUTION ===");
                foreach (var kvp in analysis.HandDistribution.Where(kvp => kvp.Value > 0))
                {
                    summary.AppendLine($"{kvp.Key}: {kvp.Value} hands");
                }
            }

            return summary.ToString();
        }
    }
}
