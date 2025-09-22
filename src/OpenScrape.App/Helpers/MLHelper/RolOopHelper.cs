using Microsoft.ML;
using Microsoft.ML.Data;

namespace OpenScrape.App.Helpers.MLHelper
{
    public class PokerHandData
    {
        public string? Mano { get; set; }
        public string? Flop { get; set; }
        public string? Apuesta { get; set; }
        [VectorType(20)] // Increased from 6 to 20 features
        public float[] Features { get; set; }
    }

    public class PokerPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedAction { get; set; }
        public float[] Score { get; set; }
    }

    public enum HandStrength
    {
        HighCard = 1,
        Pair = 2,
        TwoPair = 3,
        ThreeOfAKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
        StraightFlush = 9,
        RoyalFlush = 10
    }

    public class RolOopHelper
    {
        private MLContext _mlContext;
        private ITransformer _model;
        private PredictionEngine<PokerHandData, PokerPrediction> _predEngine;
        private const string ModelPath = "rolOop.zip";

        public RolOopHelper()
        {
            _mlContext = new MLContext(seed: 0);
        }

        public void TrainModel(string csvPath, bool saveModel = true)
        {
            var lines = File.ReadAllLines(csvPath).Skip(1);
            var data = lines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split(','))
                .Where(parts => parts.Length >= 3)
                .Select(parts => new PokerHandData
                {
                    Mano = parts[0].Trim(),
                    Flop = parts[1].Trim(),
                    Apuesta = parts[2].Trim(),
                    Features = ExtractAdvancedFeatures(parts[0].Trim(), parts[1].Trim())
                                .Select(v => (float)v).ToArray()
                });

            var trainData = _mlContext.Data.LoadFromEnumerable(data);

            // Enhanced pipeline with feature selection and normalization
            var pipeline = _mlContext.Transforms.Conversion
                .MapValueToKey("Label", nameof(PokerHandData.Apuesta))
                .Append(_mlContext.Transforms.Concatenate("Features", nameof(PokerHandData.Features)))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    maximumNumberOfIterations: 100))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _model = pipeline.Fit(trainData);
            _predEngine = _mlContext.Model.CreatePredictionEngine<PokerHandData, PokerPrediction>(_model);

            if (saveModel)
                _mlContext.Model.Save(_model, trainData.Schema, ModelPath);
        }

        public void LoadModel()
        {
            if (!File.Exists(ModelPath))
                throw new FileNotFoundException($"No se encontró el modelo entrenado en {ModelPath}");

            _model = _mlContext.Model.Load(ModelPath, out var schema);
            _predEngine = _mlContext.Model.CreatePredictionEngine<PokerHandData, PokerPrediction>(_model);
        }

        public string PredictAction(string mano, string flop, out float[] probs)
        {
            if (_predEngine == null)
                throw new InvalidOperationException("El modelo no está cargado ni entrenado.");

            var sample = new PokerHandData
            {
                Mano = mano,
                Flop = flop,
                Features = ExtractAdvancedFeatures(mano, flop).Select(v => (float)v).ToArray()
            };

            var prediction = _predEngine.Predict(sample);
            probs = prediction.Score;
            return prediction.PredictedAction;
        }

        // ----------------- Advanced Feature Extraction -----------------
        private double[] ExtractAdvancedFeatures(string mano, string flop)
        {
            var handCards = ParseCards(mano);
            var flopCards = ParseCards(flop);
            var allCards = handCards.Concat(flopCards).ToList();

            double[] features = new double[20];

            // Basic features (0-5)
            features[0] = GetHighCard(allCards) / 14.0; // Normalized high card
            features[1] = IsSuited(handCards) ? 1.0 : 0.0; // Suited hole cards
            features[2] = IsConnected(handCards) ? 1.0 : 0.0; // Connected cards
            features[3] = handCards.Count > 0 ? GetCardValue(handCards[0].Value) / 14.0 : 0; // Normalized card 1
            features[4] = handCards.Count > 1 ? GetCardValue(handCards[1].Value) / 14.0 : 0; // Normalized card 2
            features[5] = GetPairStrength(allCards); // Pair strength (0-1)

            // Advanced hand strength features (6-10)
            var handStrength = EvaluateHandStrength(allCards);
            features[6] = (int)handStrength / 10.0; // Normalized hand strength
            features[7] = CountPairs(allCards) / 3.0; // Multiple pairs
            features[8] = HasThreeOfAKind(allCards) ? 1.0 : 0.0;
            features[9] = HasStraightDraw(allCards) ? 1.0 : 0.0;
            features[10] = HasFlushDraw(allCards) ? 1.0 : 0.0;

            // Draw potential features (11-15)
            features[11] = CountOuts(allCards, DrawType.Straight) / 8.0; // Straight outs
            features[12] = CountOuts(allCards, DrawType.Flush) / 9.0; // Flush outs
            features[13] = CountOuts(allCards, DrawType.Pair) / 6.0; // Pair improvement outs
            features[14] = GetDrawStrength(allCards); // Overall draw strength
            features[15] = GetPositionValue(handCards); // Hand position value

            // Board texture features (16-19)
            features[16] = GetBoardTexture(flopCards); // Wet/dry board
            features[17] = CountHighCards(allCards) / 5.0; // High card count
            features[18] = GetSuitDistribution(allCards); // Suit distribution
            features[19] = GetRankDistribution(allCards); // Rank distribution

            return features;
        }

        // ----------------- Enhanced Hand Evaluation -----------------
        private HandStrength EvaluateHandStrength(List<CardPrediction> cards)
        {
            if (cards.Count < 5) return HandStrength.HighCard;

            var ranks = cards.Select(c => GetCardValue(c.Value)).OrderByDescending(x => x).ToList();
            var suits = cards.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.Count());
            var rankGroups = ranks.GroupBy(r => r).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();

            bool hasFlush = suits.Values.Any(count => count >= 5);
            bool hasStraight = HasStraight(ranks);

            if (hasFlush && hasStraight && ranks.Contains(14)) return HandStrength.RoyalFlush;
            if (hasFlush && hasStraight) return HandStrength.StraightFlush;
            if (rankGroups[0].Count() == 4) return HandStrength.FourOfAKind;
            if (rankGroups[0].Count() == 3 && rankGroups[1].Count() >= 2) return HandStrength.FullHouse;
            if (hasFlush) return HandStrength.Flush;
            if (hasStraight) return HandStrength.Straight;
            if (rankGroups[0].Count() == 3) return HandStrength.ThreeOfAKind;
            if (rankGroups[0].Count() == 2 && rankGroups[1].Count() == 2) return HandStrength.TwoPair;
            if (rankGroups[0].Count() == 2) return HandStrength.Pair;

            return HandStrength.HighCard;
        }

        private bool HasStraight(List<int> ranks)
        {
            var distinctRanks = ranks.Distinct().OrderByDescending(x => x).ToList();
            if (distinctRanks.Count < 5) return false;

            for (int i = 0; i <= distinctRanks.Count - 5; i++)
            {
                bool isStraight = true;
                for (int j = 1; j < 5; j++)
                {
                    if (distinctRanks[i] - j != distinctRanks[i + j])
                    {
                        isStraight = false;
                        break;
                    }
                }
                if (isStraight) return true;
            }

            // Check for A-2-3-4-5 straight (wheel)
            if (distinctRanks.Contains(14) && distinctRanks.Contains(2) &&
                distinctRanks.Contains(3) && distinctRanks.Contains(4) && distinctRanks.Contains(5))
                return true;

            return false;
        }

        // ----------------- Draw Analysis -----------------
        private enum DrawType { Straight, Flush, Pair }

        private int CountOuts(List<CardPrediction> cards, DrawType drawType)
        {
            switch (drawType)
            {
                case DrawType.Straight:
                    return CountStraightOuts(cards);
                case DrawType.Flush:
                    return CountFlushOuts(cards);
                case DrawType.Pair:
                    return CountPairOuts(cards);
                default:
                    return 0;
            }
        }

        private int CountStraightOuts(List<CardPrediction> cards)
        {
            var ranks = cards.Select(c => GetCardValue(c.Value)).Distinct().OrderBy(x => x).ToList();
            if (ranks.Count < 4) return 0;

            int outs = 0;
            // Check for open-ended straight draws
            for (int i = 0; i < ranks.Count - 3; i++)
            {
                var sequence = ranks.Skip(i).Take(4).ToList();
                if (sequence[3] - sequence[0] == 3) // 4 cards in sequence
                {
                    outs += 8; // Open-ended straight draw
                    break;
                }
            }

            return Math.Min(outs, 8);
        }

        private int CountFlushOuts(List<CardPrediction> cards)
        {
            var suitCounts = cards.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.Count());
            var maxSuitCount = suitCounts.Values.Max();

            if (maxSuitCount == 4) return 9; // Flush draw
            if (maxSuitCount == 3) return 0; // Too early for flush consideration

            return 0;
        }

        private int CountPairOuts(List<CardPrediction> cards)
        {
            if (cards.Count < 2) return 0;

            var handCards = cards.Take(2).ToList();
            var unpaired = handCards.Where(c => !cards.Skip(2).Any(fc => GetCardValue(fc.Value) == GetCardValue(c.Value)))
                                   .ToList();

            return unpaired.Count * 3; // 3 outs per unpaired hole card
        }

        // ----------------- Helper Functions -----------------
        private bool IsSuited(List<CardPrediction> handCards) =>
            handCards.Count == 2 && handCards[0].Suit == handCards[1].Suit;

        private bool IsConnected(List<CardPrediction> handCards)
        {
            if (handCards.Count != 2) return false;
            var values = handCards.Select(c => GetCardValue(c.Value)).OrderBy(v => v).ToList();
            return Math.Abs(values[1] - values[0]) <= 4;
        }

        private double GetPairStrength(List<CardPrediction> cards)
        {
            var pairs = cards.GroupBy(c => GetCardValue(c.Value))
                           .Where(g => g.Count() >= 2)
                           .OrderByDescending(g => g.Key)
                           .ToList();

            if (!pairs.Any()) return 0.0;
            return pairs.First().Key / 14.0; // Return strength of highest pair
        }

        private int CountPairs(List<CardPrediction> cards) =>
            cards.GroupBy(c => GetCardValue(c.Value)).Count(g => g.Count() >= 2);

        private bool HasThreeOfAKind(List<CardPrediction> cards) =>
            cards.GroupBy(c => GetCardValue(c.Value)).Any(g => g.Count() >= 3);

        private bool HasStraightDraw(List<CardPrediction> cards) => CountStraightOuts(cards) > 0;

        private bool HasFlushDraw(List<CardPrediction> cards) => CountFlushOuts(cards) > 0;

        private double GetDrawStrength(List<CardPrediction> cards)
        {
            var straightOuts = CountStraightOuts(cards);
            var flushOuts = CountFlushOuts(cards);
            var pairOuts = CountPairOuts(cards);

            return (straightOuts + flushOuts + pairOuts) / 20.0; // Normalized
        }

        private double GetPositionValue(List<CardPrediction> handCards)
        {
            if (handCards.Count != 2) return 0.0;

            var values = handCards.Select(c => GetCardValue(c.Value)).ToList();
            var avgValue = values.Average();

            // Higher cards have better position value
            return avgValue / 14.0;
        }

        private double GetBoardTexture(List<CardPrediction> flopCards)
        {
            if (flopCards.Count < 3) return 0.5; // Neutral

            var ranks = flopCards.Select(c => GetCardValue(c.Value)).OrderBy(v => v).ToList();
            var suits = flopCards.GroupBy(c => c.Suit).Count();

            // Wet board indicators
            double wetness = 0.0;
            if (suits == 1) wetness += 0.3; // Same suit (flush draw possible)
            if (ranks[2] - ranks[0] <= 4) wetness += 0.3; // Connected ranks
            if (ranks.Distinct().Count() == 3) wetness += 0.2; // All different ranks

            return Math.Min(wetness, 1.0);
        }

        private int CountHighCards(List<CardPrediction> cards) =>
            cards.Count(c => GetCardValue(c.Value) >= 10);

        private double GetSuitDistribution(List<CardPrediction> cards)
        {
            if (cards.Count == 0) return 0.0;
            var suitCounts = cards.GroupBy(c => c.Suit).Select(g => g.Count()).ToList();
            var maxSuitCount = suitCounts.Max();
            return maxSuitCount / (double)cards.Count;
        }

        private double GetRankDistribution(List<CardPrediction> cards)
        {
            if (cards.Count == 0) return 0.0;
            var rankCounts = cards.GroupBy(c => GetCardValue(c.Value)).Select(g => g.Count()).ToList();
            var maxRankCount = rankCounts.Max();
            return maxRankCount / (double)cards.Count;
        }

        // ----------------- Original Helper Methods (Enhanced) -----------------
        private List<CardPrediction> ParseCards(string cards)
        {
            var result = new List<CardPrediction>();
            for (int i = 0; i < cards.Length; i += 2)
                if (i + 1 < cards.Length)
                    result.Add(new CardPrediction { Value = cards[i], Suit = cards[i + 1] });
            return result;
        }

        private int GetCardValue(char card) => card switch
        {
            'A' => 14,
            'K' => 13,
            'Q' => 12,
            'J' => 11,
            'T' => 10,
            _ when char.IsDigit(card) => int.Parse(card.ToString()),
            _ => 0
        };

        private int GetHighCard(List<CardPrediction> cards) =>
            cards.Any() ? cards.Max(c => GetCardValue(c.Value)) : 0;

        private bool HasPair(List<CardPrediction> cards) =>
            cards.GroupBy(c => c.Value).Any(g => g.Count() >= 2);
    }

    public class CardPrediction
    {
        public char Value { get; set; }
        public char Suit { get; set; }

        public override string ToString() => $"{Value}{Suit}";
    }
}
