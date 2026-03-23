using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.DecisionMaker.Algorithms;

public enum BoardTextureCategory { Dry, SemiDry, SemiWet, Wet, Paired }

public record BoardTextureResult(
    BoardTextureCategory Category,
    double WetnessScore,
    bool IsMonotone,
    bool IsTwoTone,
    bool IsRainbow,
    bool IsPaired,
    bool IsConnected,
    bool IsBroadwayHeavy,
    bool IsLowBoard,
    bool HasFlushPossibility,
    bool HasStraightPossibility)
{
    /// <summary>
    /// Mapeo retrocompatible a las categorías simples Dry/Coordinated/Paired.
    /// </summary>
    public string SimplifiedTexture => Category switch
    {
        BoardTextureCategory.Paired => "Paired",
        BoardTextureCategory.Wet or BoardTextureCategory.SemiWet => "Coordinated",
        _ => "Dry"
    };
}

/// <summary>
/// Resultado del análisis de cambio de board al caer una nueva carta.
/// Detecta si la nueva carta completa draws, empareja el board, etc.
/// </summary>
public record BoardChangeResult(
    bool FlushCompleted,
    bool FlushDrawAppeared,
    bool StraightCompleted,
    bool BoardPaired,
    bool OvercardAppeared,
    int CompletedFlushSuit,
    int DangerLevel)
{
    public static BoardChangeResult Safe => new(false, false, false, false, false, -1, 0);
}

public class BoardTextureAnalyzer : IBoardTextureAnalyzer
{
    /// <summary>
    /// Analiza la textura del board basándose en las cartas comunitarias.
    /// Acepta ranks (Force) y suits como listas separadas.
    /// </summary>
    public BoardTextureResult Analyze(List<int> ranks, List<int> suits)
    {
        if (ranks.Count < 3)
            return new BoardTextureResult(BoardTextureCategory.Dry, 0, false, false, true, false, false, false, false, false, false);

        var sortedRanks = ranks.OrderBy(r => r).ToList();
        var suitGroups = suits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        var rankGroups = sortedRanks.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());

        // Suit analysis
        int maxSameSuit = suitGroups.Values.Max();
        bool isMonotone = maxSameSuit >= ranks.Count;
        bool isTwoTone = !isMonotone && maxSameSuit >= 2;
        bool isRainbow = maxSameSuit == 1;

        // Pair analysis
        bool isPaired = rankGroups.Values.Any(c => c >= 2);
        bool hasTrips = rankGroups.Values.Any(c => c >= 3);

        // Connectivity analysis
        int maxGap = 0;
        int connectedCount = 0;
        for (int i = 1; i < sortedRanks.Count; i++)
        {
            int gap = sortedRanks[i] - sortedRanks[i - 1];
            if (gap <= 2) connectedCount++;
            maxGap = Math.Max(maxGap, gap);
        }
        bool isConnected = connectedCount >= sortedRanks.Count - 1 && maxGap <= 3;

        // Broadway (10+) analysis — rank 10=Ten, 11=Jack, 12=Queen, 13=King, 14=Ace
        int broadwayCount = sortedRanks.Count(r => r >= 10);
        bool isBroadwayHeavy = broadwayCount >= (ranks.Count <= 3 ? 2 : 3);

        // Low board (todas < 9)
        bool isLowBoard = sortedRanks.All(r => r < 9);

        // Flush possibility (3+ del mismo suit en board)
        bool hasFlushPossibility = maxSameSuit >= 3;

        // Straight possibility (3+ cartas consecutivas o con gap <= 2)
        bool hasStraightPossibility = HasStraightDraw(sortedRanks);

        // Wetness score (0-100)
        double wetnessScore = CalculateWetnessScore(
            isMonotone, isTwoTone, isRainbow,
            isPaired, hasTrips,
            isConnected, connectedCount,
            isBroadwayHeavy, hasFlushPossibility, hasStraightPossibility,
            ranks.Count);

        // Categorizar
        var category = CategorizeBoard(wetnessScore, isPaired, hasTrips);

        return new BoardTextureResult(
            category, wetnessScore,
            isMonotone, isTwoTone, isRainbow,
            isPaired, isConnected,
            isBroadwayHeavy, isLowBoard,
            hasFlushPossibility, hasStraightPossibility);
    }

    /// <summary>
    /// Versión que acepta CardDataOuts directamente.
    /// </summary>
    public BoardTextureResult Analyze(List<CardDataOuts> communityCards)
    {
        var ranks = communityCards.Select(c => (int)c.Rank).ToList();
        var suits = communityCards.Select(c => (int)c.Suit).ToList();
        return Analyze(ranks, suits);
    }

    /// <summary>
    /// Analiza cómo cambia el board al caer una nueva carta (turn o river).
    /// Detecta flush completado, straight completado, board paired, overcard, etc.
    /// </summary>
    public BoardChangeResult AnalyzeBoardChange(
        List<int> previousRanks, List<int> previousSuits,
        int newCardRank, int newCardSuit)
    {
        if (previousRanks.Count < 3)
            return BoardChangeResult.Safe;

        var allRanks = previousRanks.Concat(new[] { newCardRank }).ToList();
        var allSuits = previousSuits.Concat(new[] { newCardSuit }).ToList();

        // --- Flush analysis ---
        var suitGroups = allSuits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
        var prevSuitGroups = previousSuits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());

        // Flush completado: 4+ del mismo suit en board (villano solo necesita 1 carta de ese palo)
        bool flushCompleted = suitGroups.Values.Any(c => c >= 4) && !prevSuitGroups.Values.Any(c => c >= 4);

        int completedFlushSuit = flushCompleted
            ? suitGroups.Where(g => g.Value >= 4).OrderByDescending(g => g.Value).First().Key
            : -1;

        // Flush draw en board: 3 del mismo suit (villano necesita 1 carta suited para flush)
        // Se activa cuando la nueva carta crea el tercer palo igual (2→3)
        bool flushDrawAppeared = !flushCompleted &&
            suitGroups.Values.Any(c => c >= 3) &&
            !prevSuitGroups.Values.Any(c => c >= 3);

        // --- Straight analysis ---
        var prevUnique = previousRanks.Distinct().OrderBy(r => r).ToList();
        var allUnique = allRanks.Distinct().OrderBy(r => r).ToList();

        bool prevHadStraightDraw = HasStraightDraw(prevUnique);
        bool nowHasStraight = HasCompletedStraight(allUnique);
        bool straightCompleted = nowHasStraight && prevHadStraightDraw;

        // --- Board paired ---
        var prevRankGroups = previousRanks.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());
        bool boardPaired = previousRanks.Contains(newCardRank) && !prevRankGroups.Values.Any(c => c >= 2);

        // --- Overcard ---
        int prevMaxRank = previousRanks.Max();
        bool overcardAppeared = newCardRank > prevMaxRank;

        // --- Danger level (0-10) ---
        int dangerLevel = 0;
        if (flushCompleted) dangerLevel += 4;
        else if (flushDrawAppeared) dangerLevel += 2;
        if (straightCompleted) dangerLevel += 3;
        if (boardPaired) dangerLevel += 2;
        if (overcardAppeared) dangerLevel += 1;

        return new BoardChangeResult(
            flushCompleted, flushDrawAppeared, straightCompleted,
            boardPaired, overcardAppeared, completedFlushSuit,
            Math.Min(10, dangerLevel));
    }

    /// <summary>
    /// Versión que acepta CardDataOuts para board anterior + nueva carta.
    /// </summary>
    public BoardChangeResult AnalyzeBoardChange(
        List<CardDataOuts> previousBoard, CardDataOuts newCard)
    {
        var ranks = previousBoard.Select(c => (int)c.Rank).ToList();
        var suits = previousBoard.Select(c => (int)c.Suit).ToList();
        return AnalyzeBoardChange(ranks, suits, (int)newCard.Rank, (int)newCard.Suit);
    }

    private static bool HasCompletedStraight(List<int> sortedUnique)
    {
        if (sortedUnique.Count < 4) return false;

        // Check 4+ consecutivas con gap <= 1
        for (int i = 0; i <= sortedUnique.Count - 4; i++)
        {
            if (sortedUnique[i + 3] - sortedUnique[i] <= 4)
            {
                // Verificar que hay al menos 4 cartas en ese rango
                int count = sortedUnique.Count(r => r >= sortedUnique[i] && r <= sortedUnique[i] + 4);
                if (count >= 4)
                    return true;
            }
        }

        // Wheel check: A-2-3-4 o A-2-3-4-5
        if (sortedUnique.Contains(14))
        {
            var lowCards = sortedUnique.Where(r => r <= 5).ToList();
            if (lowCards.Count >= 3 && lowCards.Max() - lowCards.Min() <= 4)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Detecta si el board tiene potencial de straight draw.
    /// Condición: 3+ cartas únicas que encajen en una ventana de 5 ranks consecutivos.
    /// Ventana de 5 = máximo gap de 4 entre la menor y la mayor de 3 cartas.
    /// Ejemplo: [9, 11, 13] (K-J-9) gap=4 → Q-T completa escalera → true.
    /// </summary>
    private static bool HasStraightDraw(List<int> sortedRanks)
    {
        if (sortedRanks.Count < 3) return false;

        var unique = sortedRanks.Distinct().OrderBy(r => r).ToList();

        // Verificar ventanas de 3+ cartas con gap total <= 4
        for (int i = 0; i <= unique.Count - 3; i++)
        {
            if (unique[i + 2] - unique[i] <= 4)
                return true;
        }

        // Wheel check (A-2-3-4-5): Ace actúa como 1
        if (unique.Contains(14))
        {
            var lowCards = unique.Where(r => r <= 5).ToList();
            if (lowCards.Count >= 2)
                return true;
        }

        return false;
    }

    private static double CalculateWetnessScore(
        bool isMonotone, bool isTwoTone, bool isRainbow,
        bool isPaired, bool hasTrips,
        bool isConnected, int connectedCount,
        bool isBroadwayHeavy, bool hasFlushPossibility, bool hasStraightPossibility,
        int cardCount)
    {
        double score = 0;

        // Suit-based wetness
        if (isMonotone) score += PokerConstants.WetnessMonotoneScore;
        else if (isTwoTone) score += PokerConstants.WetnessTwoToneScore;

        // Connectivity
        if (isConnected) score += PokerConstants.WetnessConnectedScore;
        else score += connectedCount * PokerConstants.WetnessConnectedPerCount;

        // Draws
        if (hasFlushPossibility) score += PokerConstants.WetnessFlushPossibilityScore;
        if (hasStraightPossibility) score += PokerConstants.WetnessStraightPossibilityScore;

        // Broadway heavy boards son más dinámicos
        if (isBroadwayHeavy) score += PokerConstants.WetnessBroadwayScore;

        // Paired reduce wetness (menos combinaciones de draws)
        if (isPaired) score += PokerConstants.WetnessPairedReduction;
        if (hasTrips) score += PokerConstants.WetnessTripsReduction;

        // Más cartas = más posibilidades
        if (cardCount >= 5) score += PokerConstants.WetnessExtraCardsBonus;

        return Math.Max(0, Math.Min(100, score));
    }

    private static BoardTextureCategory CategorizeBoard(double wetnessScore, bool isPaired, bool hasTrips)
    {
        if (isPaired || hasTrips)
            return BoardTextureCategory.Paired;

        return wetnessScore switch
        {
            >= PokerConstants.WetnessSemiWetMax => BoardTextureCategory.Wet,
            >= PokerConstants.WetnessSemiDryMax => BoardTextureCategory.SemiWet,
            >= PokerConstants.WetnessDryMax => BoardTextureCategory.SemiDry,
            _ => BoardTextureCategory.Dry
        };
    }
}
