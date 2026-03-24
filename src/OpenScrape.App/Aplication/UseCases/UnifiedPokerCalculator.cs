using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenScrape.DecisionMaker;
using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;
using System;
using System.Collections.Concurrent;
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
        public HandRank HeroHandRank { get; set; }        // Ranking de la mano actual de hero
        public bool HasComboDraw { get; set; }             // Flush draw + straight draw
        public KickerStrength HeroKickerStrength { get; set; } // Fuerza del kicker con top pair
        public BoardTextureCategory? BoardTexture { get; set; } // Textura del board (Dry/SemiDry/SemiWet/Wet/Paired)
        public double BoardWetnessScore { get; set; }           // Puntuación de humedad del board (0-100)
        public PairClassification PairType { get; set; }        // Sub-tipo de par (solo relevante cuando HeroHandRank == OnePair)
    }

    public class UnifiedPokerCalculator : IPokerCalculator
    {
        private readonly MonteCarloSimulator _monteCarloSimulator;
        private readonly OutsCalculator _outsCalculator;
        private readonly PreflopEquityCalculator _preflopEquityCalculator;
        private readonly IHandEvaluator _handEvaluator;
        private readonly BoardTextureAnalyzer _boardTextureAnalyzer;
        private readonly StrategyProfile _profile;
        private readonly ILogger<UnifiedPokerCalculator> _logger;

        // Cache de equity postflop: evita re-ejecutar Monte Carlo cuando las cartas no cambian.
        // Clave: "carta1,carta2|comm1,comm2,comm3|numOpp|situacion"
        // Valor: equity en porcentaje (0-100)
        private readonly ConcurrentDictionary<string, double> _equityCache = new();
        private const int EquityCacheMaxSize = 512;

        public UnifiedPokerCalculator(
            MonteCarloSimulator monteCarloSimulator,
            OutsCalculator outsCalculator,
            PreflopEquityCalculator preflopEquityCalculator,
            IHandEvaluator handEvaluator,
            BoardTextureAnalyzer boardTextureAnalyzer,
            IOptions<StrategyProfile> profileOptions,
            ILogger<UnifiedPokerCalculator> logger)
        {
            _monteCarloSimulator = monteCarloSimulator;
            _outsCalculator = outsCalculator;
            _preflopEquityCalculator = preflopEquityCalculator;
            _handEvaluator = handEvaluator;
            _boardTextureAnalyzer = boardTextureAnalyzer;
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

                // 2. Calcular equity (con rango del villano si hay situación definida)
                result.EquityPercentage = CalculateEquity(playerHand, communityCards, numOpponents, monteCarloIterations, handSituation);

                // 3. Calcular outs y draws
                var outsResult = _outsCalculator.CalculateOuts(playerHand, communityCards);
                result.TotalOuts = outsResult.TotalOuts;
                result.DrawTypes = outsResult.DrawTypes;
                result.HasComboDraw = outsResult.HasComboDraw;

                // 3b. Evaluar la mano actual de hero (postflop con 5+ cartas)
                var allCards = playerHand.Concat(communityCards).ToList();
                if (allCards.Count >= 5)
                {
                    var handEval = _handEvaluator.EvaluateBestHand(allCards);
                    result.HeroHandRank = handEval.Rank;

                    // Kicker strength: solo para OnePair que sea top pair
                    if (handEval.Rank == HandRank.OnePair && communityCards.Count >= 3)
                    {
                        var pairRank = handEval.Cards
                            .GroupBy(c => c.Rank).FirstOrDefault(g => g.Count() == 2)?.Key;
                        int topBoardRank = communityCards.Max(c => (int)c.Rank);
                        bool isTopPair = pairRank.HasValue && (int)pairRank.Value == topBoardRank;

                        if (isTopPair && handEval.Kickers.Count > 0)
                        {
                            int bestKicker = handEval.Kickers[0];
                            result.HeroKickerStrength = bestKicker >= 13 ? KickerStrength.Strong
                                : bestKicker >= 10 ? KickerStrength.Medium
                                : KickerStrength.Weak;
                        }

                        // Clasificar sub-tipo de par para decisiones turn/river diferenciadas
                        result.PairType = ClassifyPair(handEval, playerHand, communityCards);
                    }
                }

                // 3c. Analizar textura del board (postflop con 3+ community cards)
                if (communityCards.Count >= 3)
                {
                    var textureResult = _boardTextureAnalyzer.Analyze(communityCards);
                    result.BoardTexture = textureResult.Category;
                    result.BoardWetnessScore = textureResult.WetnessScore;
                }

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
                    communityCards.Count, result.TotalOuts > 0, isInPosition, heroStack, currentPotSize, handSituation);

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
            int numOpponents, int? monteCarloIterations, string? handSituation = null)
        {
            if (communityCards.Count == 0) // Preflop
            {
                // En situaciones 3Bet+, usar Monte Carlo contra VillainRange filtrado (con cache)
                if (handSituation != null && Enum.TryParse<HandSituation>(handSituation, out var preflopSituation))
                {
                    var preflopRange = VillainRange.GetForSituation(preflopSituation);
                    if (preflopRange != null)
                    {
                        string preflopCacheKey = $"preflop|{BuildHandKey(playerHand)}|{handSituation}|{numOpponents}";
                        if (_equityCache.TryGetValue(preflopCacheKey, out double cachedPreflop))
                            return cachedPreflop;

                        int adaptiveIters = GetAdaptiveIterations(playerHand, numOpponents);
                        var mcResult = _monteCarloSimulator.CalculateEquity(
                            playerHand, new List<CardDataOuts>(), numOpponents,
                            adaptiveIters, preflopRange);
                        double preflopEquity = mcResult.Equity * 100;

                        CacheEquity(preflopCacheKey, preflopEquity);
                        return preflopEquity;
                    }
                }

                return _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
            }
            else // Postflop — usar cache para evitar re-ejecutar Monte Carlo
            {
                string cacheKey = BuildEquityCacheKey(playerHand, communityCards, numOpponents, handSituation);
                if (_equityCache.TryGetValue(cacheKey, out double cached))
                    return cached;

                // Obtener rango del villano según la situación de la mano
                VillainRange? villainRange = null;
                if (handSituation != null && Enum.TryParse<HandSituation>(handSituation, out var situation))
                {
                    villainRange = VillainRange.GetForSituation(situation);
                }

                var monteCarloResult = _monteCarloSimulator.CalculateEquity(
                    playerHand, communityCards, numOpponents, monteCarloIterations, villainRange);
                double equity = monteCarloResult.Equity * 100;

                CacheEquity(cacheKey, equity);
                return equity;
            }
        }

        private static string BuildEquityCacheKey(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
            int numOpponents, string? handSituation)
        {
            // Ordenar cartas para que el orden no afecte la clave
            var hand = string.Join(",", playerHand.Select(c => c.Id).OrderBy(x => x));
            var comm = string.Join(",", communityCards.Select(c => c.Id).OrderBy(x => x));
            return $"{hand}|{comm}|{numOpponents}|{handSituation ?? ""}";
        }

        /// <summary>
        /// Iteraciones adaptativas: decisiones claras (equity >75% o <25%) usan menos iteraciones.
        /// </summary>
        private int GetAdaptiveIterations(List<CardDataOuts> playerHand, int numOpponents)
        {
            // Usar lookup table preflop como estimación rápida
            double roughEquity = _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
            if (roughEquity > 75 || roughEquity < 25)
                return 500;
            if (roughEquity > 65 || roughEquity < 35)
                return 750;
            return PokerConstants.DefaultMonteCarloIterations;
        }

        private static string BuildHandKey(List<CardDataOuts> playerHand) =>
            string.Join(",", playerHand.Select(c => c.Id).OrderBy(x => x));

        private void CacheEquity(string key, double equity)
        {
            if (_equityCache.Count >= EquityCacheMaxSize)
                _equityCache.Clear();
            _equityCache[key] = equity;
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
            bool isInPosition, decimal heroStack, decimal potSize, string handSituation)
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

            if (heroStack > 0 && potSize > 0)
            {
                double spr = (double)(heroStack / potSize);
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

        /// <summary>
        /// Clasifica el sub-tipo de par cuando hero tiene OnePair.
        /// Distingue Overpair, TopPair, MiddlePair, BottomPair, PocketPairUnder y BoardPaired.
        /// </summary>
        public static PairClassification ClassifyPair(
            HandEvaluation handEval,
            List<CardDataOuts> playerHand,
            List<CardDataOuts> communityCards)
        {
            if (communityCards.Count == 0)
                return PairClassification.None;

            // Rank que forma el par en la evaluación
            var pairGroup = handEval.Cards
                .GroupBy(c => c.Rank)
                .FirstOrDefault(g => g.Count() == 2);
            if (pairGroup == null)
                return PairClassification.None;

            int pairRankValue = (int)pairGroup.Key;

            // Ranks del board (sin las hole cards)
            var boardRanks = communityCards.Select(c => (int)c.Rank).ToList();
            int maxBoard = boardRanks.Max();
            int minBoard = boardRanks.Min();

            // Ranks de las hole cards de hero
            var holeRanks = playerHand.Select(c => (int)c.Rank).ToList();
            bool holeCard1ContributesPair = holeRanks.Count > 0 && holeRanks[0] == pairRankValue;
            bool holeCard2ContributesPair = holeRanks.Count > 1 && holeRanks[1] == pairRankValue;
            bool heroContributesPair = holeCard1ContributesPair || holeCard2ContributesPair;

            // El par está solo en el board — hero no aporta ninguna hole card al par
            if (!heroContributesPair)
                return PairClassification.BoardPaired;

            // Pocket pair: ambas hole cards tienen el mismo rank y ese rank forma el par
            bool isPocketPair = holeCard1ContributesPair && holeCard2ContributesPair;
            if (isPocketPair)
            {
                // Overpair: pocket pair superior a todas las cartas del board
                return pairRankValue > maxBoard
                    ? PairClassification.Overpair
                    : PairClassification.PocketPairUnder;
            }

            // Par formado por una hole card que empareja una carta del board
            if (pairRankValue == maxBoard)
                return PairClassification.TopPair;

            if (pairRankValue == minBoard)
                return PairClassification.BottomPair;

            return PairClassification.MiddlePair;
        }
    }
}