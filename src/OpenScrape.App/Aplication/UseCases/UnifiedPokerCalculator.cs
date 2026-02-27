using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenScrape.App.Aplication.UseCases
{
    public interface IPokerCalculator
    {
        PokerCalculationResult Calculate(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
            decimal currentPotSize, decimal betToCall, int numOpponents = 1, int? monteCarloIterations = null);
    }

    public class PokerCalculationResult
    {
        public double PotOddsPercentage { get; set; }    // Pot odds en porcentaje (ej: 25.0)
        public double EquityPercentage { get; set; }     // Equity en porcentaje (ej: 22.5)
        public bool ShouldCall { get; set; }             // Recomendación básica
        public double ExpectedValue { get; set; }        // EV de la acción
        public List<string> DrawTypes { get; set; }      // Tipos de draws
        public int TotalOuts { get; set; }               // Outs totales
        public string Street { get; set; }               // Calle actual
        public string RecommendedAction { get; set; }    // Acción recomendada
    }

    public class UnifiedPokerCalculator : IPokerCalculator
    {
        private readonly MonteCarloSimulator _monteCarloSimulator;
        private readonly OutsCalculator _outsCalculator;
        private readonly PreflopEquityCalculator _preflopEquityCalculator;

        public UnifiedPokerCalculator(
            MonteCarloSimulator monteCarloSimulator,
            OutsCalculator outsCalculator,
            PreflopEquityCalculator preflopEquityCalculator)
        {
            _monteCarloSimulator = monteCarloSimulator;
            _outsCalculator = outsCalculator;
            _preflopEquityCalculator = preflopEquityCalculator;
        }

        public PokerCalculationResult Calculate(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
            decimal currentPotSize, decimal betToCall, int numOpponents = 1, int? monteCarloIterations = null)
        {
            var result = new PokerCalculationResult
            {
                DrawTypes = new List<string>(),
                Street = GetStreetName(communityCards.Count)
            };

            try
            {
                // 1. Calcular pot odds
                result.PotOddsPercentage = CalculatePotOddsPercentage(currentPotSize, betToCall);

                // 2. Calcular equity
                result.EquityPercentage = CalculateEquity(playerHand, communityCards, numOpponents, monteCarloIterations);

                // 3. Calcular outs y draws
                var outsResult = _outsCalculator.CalculateOuts(playerHand, communityCards);
                result.TotalOuts = outsResult.TotalOuts;
                result.DrawTypes = outsResult.DrawTypes;

                // 4. Determinar si debe pagar (con factores adicionales)
                result.ShouldCall = CalculateShouldCall(result.EquityPercentage, result.PotOddsPercentage,
                    communityCards.Count, result.TotalOuts > 0);

                // 5. Calcular Expected Value
                result.ExpectedValue = CalculateExpectedValue(result.EquityPercentage / 100.0,
                    (double)currentPotSize, (double)betToCall);

                // 6. Generar acción recomendada
                result.RecommendedAction = GenerateRecommendedAction(result);

                return result;
            }
            catch (Exception)
            {
                // En caso de error, devolver valores seguros
                return new PokerCalculationResult
                {
                    PotOddsPercentage = 0,
                    EquityPercentage = 0,
                    ShouldCall = false,
                    ExpectedValue = -(double)betToCall,
                    DrawTypes = new List<string>(),
                    TotalOuts = 0,
                    Street = "Unknown",
                    RecommendedAction = "Fold"
                };
            }
        }

        private double CalculatePotOddsPercentage(decimal currentPotSize, decimal betToCall)
        {
            if (betToCall <= 0) return 0;

            decimal totalPotAfterCall = currentPotSize + betToCall;
            return (double)((betToCall / totalPotAfterCall) * 100);
        }

        private double CalculateEquity(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
            int numOpponents, int? monteCarloIterations)
        {
            if (communityCards.Count == 0) // Preflop
            {
                return _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
            }
            else // Postflop
            {
                var monteCarloResult = _monteCarloSimulator.CalculateEquity(
                    playerHand, communityCards, numOpponents, monteCarloIterations);
                return monteCarloResult.Equity * 100;
            }
        }

        private bool CalculateShouldCall(double equity, double potOdds, int communityCardsCount, bool hasDraws)
        {
            // Lógica básica
            bool basicDecision = equity >= potOdds;

            // Factores adicionales sin complejidad excesiva
            double adjustedEquity = equity;

            // Bonus por draws (outs disponibles)
            if (hasDraws && communityCardsCount < 5)
            {
                adjustedEquity += 2.0; // Pequeño bonus por tener draws
            }

            // Ajuste por calle (más conservador en streets posteriores)
            if (communityCardsCount >= 4) // River
            {
                adjustedEquity -= 1.0; // Más conservador en river
            }

            return adjustedEquity >= potOdds;
        }

        private double CalculateExpectedValue(double equity, double potSize, double callAmount)
        {
            return (equity * potSize) - ((1 - equity) * callAmount);
        }

        private string GenerateRecommendedAction(PokerCalculationResult result)
        {
            if (!result.ShouldCall)
                return "Fold";

            // Lógica simple para recomendaciones
            if (result.EquityPercentage > result.PotOddsPercentage + 10)
                return "Call"; // Equity claramente mejor

            if (result.TotalOuts >= 8 && result.Street != "River")
                return "Call"; // Buen draw

            return "Call"; // Default
        }

        private string GetStreetName(int communityCardsCount)
        {
            return communityCardsCount switch
            {
                0 => "Pre-Flop",
                3 => "Flop",
                4 => "Turn",
                5 => "River",
                _ => "Unknown"
            };
        }
    }
}