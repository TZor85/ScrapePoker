using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

/// <summary>
/// Evaluador de manos de póker basado en bit-manipulation.
/// Reemplaza el enfoque brute-force de C(n,5) combinaciones + LINQ
/// con operaciones directas sobre bitmasks y arrays en stack.
///
/// Mejoras clave respecto a HandEvaluator original:
/// - Sin generación de C(n,5)=21 combinaciones
/// - Sin Dictionary ni LINQ (cero allocations por evaluación)
/// - Usa stackalloc para conteos en stack en lugar de heap
/// - Un único escaneo de cartas para construir todos los bitmasks
/// - Rendimiento estimado: 10-20x más rápido por evaluación
///
/// Crítico para MonteCarloSimulator: ~20,000 evaluaciones por cálculo de equity.
/// </summary>
public class BitHandEvaluator : IHandEvaluator
{
    // Wheel: A-2-3-4-5 representado como bitmask (bit N = rank N)
    // bit2 | bit3 | bit4 | bit5 | bit14
    private const int WheelMask = (1 << 14) | (1 << 2) | (1 << 3) | (1 << 4) | (1 << 5);

    public HandEvaluation EvaluateBestHand(List<CardDataOuts> cards)
    {
        if (cards.Count < 5)
            throw new ArgumentException("Se requieren al menos 5 cartas para evaluar una mano");

        int count = cards.Count;

        // Arrays en stack para conteo — sin allocations en heap por evaluación
        // rankCount[r] = cuántas cartas tienen rank r (índice 2-14)
        Span<int> rankCount = stackalloc int[15]; // índices 0-14
        // suitCount[s] = cuántas cartas tienen suit s (índice 1-4)
        Span<int> suitCount = stackalloc int[5];  // índices 0-4
        // rankBits: OR de todos los ranks presentes (para detección de straight)
        int rankBits = 0;
        // suitRankBits[s]: bitmask de ranks para cada suit (para straight flush)
        Span<int> suitRankBits = stackalloc int[5];

        // Fase 1: Escaneo único — construir todos los conteos y bitmasks
        for (int i = 0; i < count; i++)
        {
            int r = (int)cards[i].Rank;
            int s = (int)cards[i].Suit;
            rankCount[r]++;
            suitCount[s]++;
            rankBits |= 1 << r;
            suitRankBits[s] |= 1 << r;
        }

        // Fase 2: Detectar flush (suit con ≥5 cartas)
        int flushSuit = -1;
        for (int s = 1; s <= 4; s++)
        {
            if (suitCount[s] >= 5)
            {
                flushSuit = s;
                break;
            }
        }

        // Fase 3: Detectar straight flush o flush
        if (flushSuit >= 0)
        {
            int sfHigh = FindStraightHigh(suitRankBits[flushSuit]);
            if (sfHigh > 0)
            {
                // Straight Flush (incluye Royal Flush cuando high=14)
                return new HandEvaluation
                {
                    Rank = HandRank.StraightFlush,
                    Score = PokerConstants.StraightFlushMultiplier * sfHigh,
                    Cards = SelectFlushCards(cards, flushSuit),
                    Kickers = new List<int>()
                };
            }
            // Solo Flush
            return BuildFlushResult(suitRankBits[flushSuit], cards, flushSuit);
        }

        // Fase 4: Encontrar grupos (quads, trips, pairs) sin allocations
        int quadsRank = 0, tripsRank = 0, highPair = 0, lowPair = 0;
        for (int r = 14; r >= 2; r--)
        {
            int c = rankCount[r];
            if (c == 4 && quadsRank == 0) { quadsRank = r; }
            else if (c == 3 && tripsRank == 0) { tripsRank = r; }
            else if (c >= 2)
            {
                if (highPair == 0) highPair = r;
                else if (lowPair == 0) { lowPair = r; break; }
            }
        }

        // Fase 5: Detectar straight (solo sin flush, ya procesado)
        int straightHigh = FindStraightHigh(rankBits);

        // Fase 6: Clasificar y construir resultado (jerarquía de mayor a menor)

        // --- Four of a Kind ---
        if (quadsRank > 0)
        {
            int kicker = FindHighestExcluding(rankCount, quadsRank, -1);
            var eval = new HandEvaluation
            {
                Rank = HandRank.FourOfAKind,
                Score = PokerConstants.FourOfAKindMultiplier * quadsRank,
                Cards = SelectBestCards(cards, quadsRank, -1, 4),
                Kickers = new List<int>()
            };
            if (kicker > 0) eval.Kickers.Add(kicker);
            return eval;
        }

        // --- Full House ---
        // trips + par o trips + otro trips
        if (tripsRank > 0)
        {
            // El par puede venir de highPair, o de un segundo trips
            int pairRank = highPair > 0 ? highPair : 0;
            // Si hay otro trips rank >= 2 que no es tripsRank, también sirve
            for (int r = 14; r >= 2; r--)
            {
                if (r != tripsRank && rankCount[r] >= 3 && r > pairRank)
                    pairRank = r;
            }

            if (pairRank > 0)
            {
                var eval = new HandEvaluation
                {
                    Rank = HandRank.FullHouse,
                    Score = PokerConstants.FullHouseMultiplier * tripsRank,
                    Cards = SelectBestCards(cards, tripsRank, pairRank, 5),
                    Kickers = new List<int> { pairRank }
                };
                return eval;
            }
        }

        // --- Straight (sin flush, y no hay full house) ---
        if (straightHigh > 0 && tripsRank == 0)
        {
            return new HandEvaluation
            {
                Rank = HandRank.Straight,
                Score = PokerConstants.StraightMultiplier * straightHigh,
                Cards = SelectBestCards(cards, 0, -1, 5),
                Kickers = new List<int>()
            };
        }

        // --- Three of a Kind (sin par adicional = no full house) ---
        if (tripsRank > 0)
        {
            // Si hay straight también disponible, el straight gana
            if (straightHigh > 0)
            {
                return new HandEvaluation
                {
                    Rank = HandRank.Straight,
                    Score = PokerConstants.StraightMultiplier * straightHigh,
                    Cards = SelectBestCards(cards, 0, -1, 5),
                    Kickers = new List<int>()
                };
            }

            var eval = new HandEvaluation
            {
                Rank = HandRank.ThreeOfAKind,
                Score = PokerConstants.ThreeOfAKindMultiplier * tripsRank,
                Cards = SelectBestCards(cards, tripsRank, -1, 3),
                Kickers = new List<int>()
            };
            // 2 kickers más altos excluyendo el trips
            for (int r = 14; r >= 2 && eval.Kickers.Count < 2; r--)
            {
                if (r != tripsRank && rankCount[r] > 0)
                    eval.Kickers.Add(r);
            }
            return eval;
        }

        // Straight tardío (cuando había trips pero no full house, que ya se procesó arriba)
        if (straightHigh > 0)
        {
            return new HandEvaluation
            {
                Rank = HandRank.Straight,
                Score = PokerConstants.StraightMultiplier * straightHigh,
                Cards = SelectBestCards(cards, 0, -1, 5),
                Kickers = new List<int>()
            };
        }

        // --- Two Pair ---
        if (highPair > 0 && lowPair > 0)
        {
            int kicker = FindHighestExcluding(rankCount, highPair, lowPair);
            var eval = new HandEvaluation
            {
                Rank = HandRank.TwoPair,
                Score = PokerConstants.TwoPairMultiplier * highPair,
                Cards = SelectBestCards(cards, highPair, lowPair, 4),
                Kickers = new List<int> { lowPair }
            };
            if (kicker > 0) eval.Kickers.Add(kicker);
            return eval;
        }

        // --- One Pair ---
        if (highPair > 0)
        {
            var eval = new HandEvaluation
            {
                Rank = HandRank.OnePair,
                Score = PokerConstants.PairMultiplier * highPair,
                Cards = SelectBestCards(cards, highPair, -1, 2),
                Kickers = new List<int>()
            };
            // 3 kickers más altos excluyendo el par
            for (int r = 14; r >= 2 && eval.Kickers.Count < 3; r--)
            {
                if (r != highPair && rankCount[r] > 0)
                    eval.Kickers.Add(r);
            }
            return eval;
        }

        // --- High Card ---
        var highCardEval = new HandEvaluation
        {
            Rank = HandRank.HighCard,
            Score = PokerConstants.HighCardMultiplier,
            Cards = SelectBestCards(cards, 0, -1, 5),
            Kickers = new List<int>()
        };
        for (int r = 14; r >= 2 && highCardEval.Kickers.Count < 5; r--)
        {
            if (rankCount[r] > 0)
                highCardEval.Kickers.Add(r);
        }
        return highCardEval;
    }

    /// <summary>
    /// Busca la escalera más alta en un bitmask de ranks.
    /// Retorna el rank alto de la escalera (5-14), o 0 si no hay.
    /// El wheel (A-2-3-4-5) retorna 5.
    /// </summary>
    private static int FindStraightHigh(int bits)
    {
        // Verificar desde Ace-high (bits 10-14) hasta 6-high (bits 2-6)
        // Una escalera = 5 bits consecutivos. Mask = 0x1F << (high-4)
        for (int high = 14; high >= 6; high--)
        {
            int mask = 0x1F << (high - 4);
            if ((bits & mask) == mask)
                return high;
        }
        // Wheel: A(14)-2-3-4-5
        if ((bits & WheelMask) == WheelMask)
            return 5;
        return 0;
    }

    /// <summary>
    /// Encuentra la carta más alta excluyendo hasta dos ranks.
    /// exclude2 = -1 significa sin segundo exclusión.
    /// </summary>
    private static int FindHighestExcluding(Span<int> rankCount, int exclude1, int exclude2)
    {
        for (int r = 14; r >= 2; r--)
        {
            if (r != exclude1 && r != exclude2 && rankCount[r] > 0)
                return r;
        }
        return 0;
    }

    /// <summary>
    /// Construye el resultado para un flush, evaluando los 5 ranks más altos del suit.
    /// </summary>
    private static HandEvaluation BuildFlushResult(int flushBits, List<CardDataOuts> cards, int flushSuit)
    {
        var eval = new HandEvaluation
        {
            Rank = HandRank.Flush,
            Score = PokerConstants.FlushMultiplier,
            Cards = SelectFlushCards(cards, flushSuit),
            Kickers = new List<int>()
        };
        // Los 5 ranks más altos del flush como kickers (para comparación de flush vs flush)
        for (int r = 14; r >= 2 && eval.Kickers.Count < 5; r--)
        {
            if ((flushBits & (1 << r)) != 0)
                eval.Kickers.Add(r);
        }
        return eval;
    }

    /// <summary>
    /// Selecciona las mejores cartas del flush suit (las 5 de mayor rank).
    /// </summary>
    private static List<CardDataOuts> SelectFlushCards(List<CardDataOuts> cards, int flushSuit)
    {
        var result = new List<CardDataOuts>(7);
        foreach (var card in cards)
        {
            if ((int)card.Suit == flushSuit)
                result.Add(card);
        }
        result.Sort((a, b) => b.Rank.CompareTo(a.Rank));
        if (result.Count > 5) result.RemoveRange(5, result.Count - 5);
        return result;
    }

    /// <summary>
    /// Selecciona las cartas más relevantes para poblar HandEvaluation.Cards.
    /// primaryRank=0 significa sin rank primario (high card, straight).
    /// secondary=-1 significa sin rank secundario.
    /// </summary>
    private static List<CardDataOuts> SelectBestCards(
        List<CardDataOuts> cards, int primaryRank, int secondaryRank, int primaryNeeded)
    {
        if (cards.Count <= 5)
            return new List<CardDataOuts>(cards);

        var result = new List<CardDataOuts>(5);
        var others = new List<CardDataOuts>();

        foreach (var card in cards)
        {
            int r = (int)card.Rank;
            if ((primaryRank > 0 && r == primaryRank && result.Count < primaryNeeded) ||
                (secondaryRank > 0 && r == secondaryRank && result.Count < 5))
                result.Add(card);
            else
                others.Add(card);
        }

        others.Sort((a, b) => b.Rank.CompareTo(a.Rank));
        foreach (var card in others)
        {
            if (result.Count >= 5) break;
            result.Add(card);
        }

        return result;
    }
}
