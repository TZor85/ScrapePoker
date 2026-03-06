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

public class BoardTextureAnalyzer
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

    private static bool HasStraightDraw(List<int> sortedRanks)
    {
        if (sortedRanks.Count < 3) return false;

        var unique = sortedRanks.Distinct().ToList();
        for (int i = 0; i <= unique.Count - 3; i++)
        {
            if (unique[i + 2] - unique[i] <= 4)
                return true;
        }

        // Wheel check (A-2-3-4-5): si hay Ace (14) y cartas bajas
        if (unique.Contains(14) && unique.Any(r => r <= 5))
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
        if (isMonotone) score += 35;
        else if (isTwoTone) score += 15;
        else if (isRainbow) score += 0;

        // Connectivity
        if (isConnected) score += 20;
        else score += connectedCount * 8;

        // Draws
        if (hasFlushPossibility) score += 15;
        if (hasStraightPossibility) score += 15;

        // Broadway heavy boards son más dinámicos
        if (isBroadwayHeavy) score += 10;

        // Paired reduce wetness (menos combinaciones de draws)
        if (isPaired) score -= 10;
        if (hasTrips) score -= 15;

        // Más cartas = más posibilidades
        if (cardCount >= 5) score += 5;

        return Math.Max(0, Math.Min(100, score));
    }

    private static BoardTextureCategory CategorizeBoard(double wetnessScore, bool isPaired, bool hasTrips)
    {
        if (isPaired || hasTrips)
            return BoardTextureCategory.Paired;

        return wetnessScore switch
        {
            >= 60 => BoardTextureCategory.Wet,
            >= 35 => BoardTextureCategory.SemiWet,
            >= 15 => BoardTextureCategory.SemiDry,
            _ => BoardTextureCategory.Dry
        };
    }
}
