using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            decimal currentPotSize, decimal betToCall, int numOpponents = 1, int? monteCarloIterations = null,
            bool isInPosition = false, decimal heroStack = 0, decimal villainStack = 0, string? handSituation = null);
    }

    public class PokerCalculationResult
    {
        public double PotOddsPercentage { get; set; }    // Pot odds en porcentaje (ej: 25.0)
        public double EquityPercentage { get; set; }     // Equity en porcentaje (ej: 22.5)
        public bool ShouldCall { get; set; }             // Recomendación básica
        public double ExpectedValue { get; set; }        // EV de la acción
        public double FoldEquity { get; set; }           // Probabilidad de que el oponente se retire
        public double EVWithFoldEquity { get; set; }     // EV considerando fold equity
        public List<string> DrawTypes { get; set; } = [];  // Tipos de draws
        public int TotalOuts { get; set; }               // Outs totales
        public string Street { get; set; } = string.Empty; // Calle actual
        public string RecommendedAction { get; set; } = string.Empty; // Acción recomendada
        public double? SuggestedBetSize { get; set; }    // Tamaño de apuesta sugerido (como porcentaje del pote)
    }

    public class UnifiedPokerCalculator : IPokerCalculator
    {
        private readonly MonteCarloSimulator _monteCarloSimulator;
        private readonly OutsCalculator _outsCalculator;
        private readonly PreflopEquityCalculator _preflopEquityCalculator;
        private readonly StrategyProfile _profile;
        private readonly ILogger<UnifiedPokerCalculator> _logger;

        public UnifiedPokerCalculator(
            MonteCarloSimulator monteCarloSimulator,
            OutsCalculator outsCalculator,
            PreflopEquityCalculator preflopEquityCalculator,
            IOptions<StrategyProfile> profileOptions,
            ILogger<UnifiedPokerCalculator> logger)
        {
            _monteCarloSimulator = monteCarloSimulator;
            _outsCalculator = outsCalculator;
            _preflopEquityCalculator = preflopEquityCalculator;
            _profile = profileOptions.Value;
            _logger = logger;
        }

        public PokerCalculationResult Calculate(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
            decimal currentPotSize, decimal betToCall, int numOpponents = 1, int? monteCarloIterations = null,
            bool isInPosition = false, decimal heroStack = 0, decimal villainStack = 0, string? handSituation = null)
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

                // 4. Calcular fold equity basado en posición y situación
                result.FoldEquity = CalculateFoldEquity(result.PotOddsPercentage, isInPosition, handSituation, communityCards.Count);

                // 5. Calcular Expected Value mejorado
                result.ExpectedValue = CalculateExpectedValue(result.EquityPercentage / 100.0,
                    (double)currentPotSize, (double)betToCall);

                // 6. Calcular EV con fold equity (para apuestas)
                result.EVWithFoldEquity = CalculateEVWithFoldEquity(result.EquityPercentage / 100.0,
                    result.FoldEquity / 100.0, (double)currentPotSize, (double)betToCall);

                // 7. Determinar si debe pagar (con factores adicionales para cash games)
                result.ShouldCall = CalculateShouldCall(result.EquityPercentage, result.PotOddsPercentage,
                    communityCards.Count, result.TotalOuts > 0, isInPosition, heroStack, villainStack, handSituation);

                // 8. Generar acción recomendada con bet sizing
                var actionResult = GenerateRecommendedAction(result, isInPosition, heroStack, villainStack, currentPotSize);
                result.RecommendedAction = actionResult.Action;
                result.SuggestedBetSize = actionResult.BetSize;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en cálculo de equity. Hand: {Hand}, Community: {Community}",
                    string.Join(",", playerHand.Select(c => c.Id)),
                    string.Join(",", communityCards.Select(c => c.Id)));
                return new PokerCalculationResult
                {
                    PotOddsPercentage = 0,
                    EquityPercentage = 0,
                    ShouldCall = false,
                    ExpectedValue = -(double)betToCall,
                    FoldEquity = 0,
                    EVWithFoldEquity = -(double)betToCall,
                    DrawTypes = new List<string>(),
                    TotalOuts = 0,
                    Street = "Unknown",
                    RecommendedAction = "Fold",
                    SuggestedBetSize = null
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

        private double CalculateFoldEquity(double potOddsPercentage, bool isInPosition, string handSituation, int communityCardsCount)
        {
            double baseFoldEquity = _profile.FoldEquityBase;

            if (communityCardsCount >= 4) baseFoldEquity += _profile.FoldEquityRiverPenalty;
            else if (communityCardsCount >= 3) baseFoldEquity += _profile.FoldEquityFlopBonus;

            if (isInPosition) baseFoldEquity += _profile.FoldEquityIPBonus;

            if (handSituation?.Contains("ThreeBet") == true) baseFoldEquity += _profile.FoldEquityThreeBetPenalty;

            return Math.Max(_profile.FoldEquityMin, Math.Min(_profile.FoldEquityMax, baseFoldEquity));
        }

        private double CalculateEVWithFoldEquity(double equity, double foldEquity, double potSize, double betAmount)
        {
            // EV de una apuesta: (equity × pote_final) + (fold_equity × pote_actual) - ((1-fold_equity) × bet_amount)
            double finalPot = potSize + betAmount;
            double foldEV = foldEquity * potSize;
            double callEV = (1 - foldEquity) * ((equity * finalPot) - betAmount);
            return foldEV + callEV;
        }

        private bool CalculateShouldCall(double equity, double potOdds, int communityCardsCount, bool hasDraws,
            bool isInPosition, decimal heroStack, decimal villainStack, string handSituation)
        {
            // Lógica básica
            bool basicDecision = equity >= potOdds;

            // Factores adicionales para cash games
            double adjustedEquity = equity;

            if (hasDraws && communityCardsCount < 5)
            {
                adjustedEquity += _profile.DrawEquityBonus;
            }

            if (communityCardsCount >= 4)
            {
                adjustedEquity += _profile.RiverEquityPenalty;
            }

            if (isInPosition)
            {
                adjustedEquity += _profile.IPEquityBonus;
            }

            if (heroStack > 0 && villainStack > 0)
            {
                decimal currentPot = heroStack + villainStack;
                double spr = (double)(heroStack / currentPot);
                if (spr > _profile.BetSizingSPRDeepThreshold)
                {
                    adjustedEquity += _profile.DeepStackEquityBonus;
                }
            }

            return adjustedEquity >= potOdds;
        }

        private double CalculateExpectedValue(double equity, double potSize, double callAmount)
        {
            return (equity * potSize) - ((1 - equity) * callAmount);
        }

        private (string Action, double? BetSize) GenerateRecommendedAction(PokerCalculationResult result,
            bool isInPosition, decimal heroStack, decimal villainStack, decimal currentPotSize)
        {
            // Lógica de decisión mejorada para cash games
            if (!result.ShouldCall)
                return ("Fold", null);

            // Considerar EV con fold equity para decisiones de apuesta
            bool shouldBet = result.EVWithFoldEquity > result.ExpectedValue + _profile.BetEVThreshold;

            if (shouldBet)
            {
                // Calcular tamaño de apuesta sugerido
                double betSizePercentage = CalculateBetSize(result, isInPosition, heroStack, villainStack, currentPotSize);
                return ($"Bet {betSizePercentage:F1}x pot", betSizePercentage);
            }

            if (result.EquityPercentage > result.PotOddsPercentage + 15)
                return ("Call", null); // Equity claramente mejor

            if (result.TotalOuts >= 8 && result.Street != "River")
                return ("Call", null); // Buen draw

            return ("Call", null); // Default
        }

        private double CalculateBetSize(PokerCalculationResult result, bool isInPosition,
            decimal heroStack, decimal villainStack, decimal currentPotSize)
        {
            // Lógica básica de bet sizing para cash games
            double baseSize = 0.5; // 0.5x pot

            // Ajustes por equity
            if (result.EquityPercentage > 70) baseSize = 0.75; // Value bet
            else if (result.EquityPercentage < 40) baseSize = 0.33; // Thin value o bluff

            // Ajustes por posición
            if (isInPosition) baseSize += 0.1;

            // Ajustes por stacks (SPR)
            if (heroStack > 0 && villainStack > 0)
            {
                double spr = (double)(Math.Min(heroStack, villainStack) / currentPotSize);
                if (spr > 2.0) baseSize += 0.1; // Deep stacks permiten bets más grandes
            }

            // Limitar tamaño razonable
            return Math.Min(1.5, Math.Max(0.25, baseSize));
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