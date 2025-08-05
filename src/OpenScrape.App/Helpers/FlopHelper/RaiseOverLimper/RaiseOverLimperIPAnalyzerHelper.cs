using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Helpers.FlopHelper.RaiseOverLimper;


/// <summary>
/// Analizador para determinar el sizing de raise sobre limpers en posición durante el flop
/// Implementa la estrategia de 1/2 pot para manos fuertes y 1/3 pot para manos marginales
/// </summary>
public static class RaiseOverLimperIPAnalyzerHelper
{
    private const int AceForce = 14;
    private const int KingForce = 13;
    private const int QueenForce = 12;
    private const int JackForce = 11;
    private const int TenForce = 10;
    private const int NineForce = 9;

    // Umbrales para determinar calidad del kicker
    private const int StrongKickerThreshold = JackForce; // J o mejor
    private const int WeakKickerThreshold = 6; // 6 o peor

    /// <summary>
    /// Determina el sizing óptimo de apuesta de continuación en el flop.
    /// </summary>
    /// <param name="flopResult">Resultado del análisis del flop</param>
    /// <param name="playerState">Estado actual del jugador</param>
    /// <returns>El sizing recomendado: "1/2", "1/3" o "Check"</returns>
    public static string DetermineContinuationBetSizing(TableScrapeFlopResult flopResult, PlayerGameState playerState)
    {
        if (flopResult == null || playerState == null)
            throw new ArgumentNullException("Los parámetros no pueden ser nulos");

        // 1. Verificar manos que siempre apuestan 1/2 bote
        if (ShouldBetHalfPot(flopResult, playerState))
            return "1/2";

        // 2. Verificar manos que apuestan 1/3 bote
        if (ShouldBetThirdPot(flopResult, playerState))
            return "1/3";

        // 3. Todo lo demás es check
        return "Check";
    }

    /// <summary>
    /// Determina si debemos apostar 1/2 bote (manos fuertes).
    /// </summary>
    private static bool ShouldBetHalfPot(TableScrapeFlopResult flopResult, PlayerGameState playerState)
    {
        var strength = flopResult.HeroStrength;
        var texture = flopResult.BoardTexture;

        // Manos premium que siempre apuestan 1/2 bote
        if (HasPremiumMadeHand(strength))
            return true;

        // Top pair con buen kicker en boards favorables
        if (strength.HasTopPair && HasStrongTopPair(playerState, texture))
            return true;

        // Dos pares o mejor
        if (strength.HasTwoPair || strength.HasSet || strength.HasFullHouse)
            return true;

        // Top pair en boards muy secos
        if (strength.HasTopPair && IsVeryDryBoard(texture))
            return true;

        return false;
    }

    /// <summary>
    /// Determina si debemos apostar 1/3 bote (manos marginales).
    /// </summary>
    private static bool ShouldBetThirdPot(TableScrapeFlopResult flopResult, PlayerGameState playerState)
    {
        var strength = flopResult.HeroStrength;
        var texture = flopResult.BoardTexture;

        // Top pair con kicker débil
        if (strength.HasTopPair && HasWeakTopPair(playerState, texture))
            return true;

        // Top pair con kicker medio en boards coordinados/peligrosos
        if (strength.HasTopPair && HasMediumTopPairOnDangerousBoard(playerState, texture))
            return true;

        // Middle pair fuerte en boards secos
        if (strength.HasMiddlePair && IsVeryDryBoard(texture) && HasDecentKicker(playerState))
            return true;

        // Overpair débil en boards muy coordinados
        if (strength.HasOverPair && IsHighlyCoordinatedBoard(texture) && HasLowOverpair(playerState, texture))
            return true;

        return false;
    }

    #region [Hand Strength Analysis]

    /// <summary>
    /// Verifica si tenemos una mano premium hecha.
    /// </summary>
    private static bool HasPremiumMadeHand(HeroHandStrength strength)
    {
        return strength.Hand >= HeroHand.Trio || // Set o mejor
               strength.HasFullHouse ||
               strength.HasSet;
    }

    /// <summary>
    /// Determina si tenemos top pair fuerte.
    /// </summary>
    private static bool HasStrongTopPair(PlayerGameState playerState, BoardTexture texture)
    {
        var kicker = playerState.Kicker;

        // Kicker fuerte (J o mejor)
        if (kicker >= StrongKickerThreshold)
            return true;

        // Top pair de As o Rey siempre se considera fuerte
        var topPairRank = Math.Max(playerState.HoleCard1Rank, playerState.HoleCard2Rank);
        if (topPairRank >= KingForce && topPairRank == texture.HighestRank)
            return true;

        return false;
    }

    /// <summary>
    /// Determina si tenemos top pair débil.
    /// </summary>
    private static bool HasWeakTopPair(PlayerGameState playerState, BoardTexture texture)
    {
        var kicker = playerState.Kicker;

        // Kicker muy débil (6 o peor)
        if (kicker <= WeakKickerThreshold)
            return true;

        // Top pair bajo con kicker medio en boards peligrosos
        var topPairRank = GetTopPairRank(playerState, texture);
        if (topPairRank <= NineForce && kicker <= TenForce && !texture.IsDry)
            return true;

        return false;
    }

    /// <summary>
    /// Determina si tenemos top pair medio en board peligroso.
    /// </summary>
    private static bool HasMediumTopPairOnDangerousBoard(PlayerGameState playerState, BoardTexture texture)
    {
        var kicker = playerState.Kicker;

        // Kicker medio (7-10) en boards coordinados
        if (kicker >= 7 && kicker <= TenForce && IsHighlyCoordinatedBoard(texture))
            return true;

        // Top pair medio en boards con muchos draws
        var topPairRank = GetTopPairRank(playerState, texture);
        if (topPairRank >= TenForce && topPairRank <= QueenForce &&
            (texture.IsConnected && !texture.IsRainbow))
            return true;

        return false;
    }

    /// <summary>
    /// Verifica si tenemos un kicker decente.
    /// </summary>
    private static bool HasDecentKicker(PlayerGameState playerState)
    {
        return playerState.Kicker >= 8; // 8 o mejor se considera decente
    }

    /// <summary>
    /// Determina si tenemos overpair bajo.
    /// </summary>
    private static bool HasLowOverpair(PlayerGameState playerState, BoardTexture texture)
    {
        if (!playerState.HavePocketPair) return false;

        var pocketRank = playerState.HoleCard1Rank;
        var boardHigh = texture.HighestRank;

        // Overpair bajo: pocket pair que es overpair pero no muy alto
        return pocketRank > boardHigh && pocketRank <= TenForce;
    }

    /// <summary>
    /// Obtiene el rank de la carta que forma top pair.
    /// </summary>
    private static int GetTopPairRank(PlayerGameState playerState, BoardTexture texture)
    {
        var card1 = playerState.HoleCard1Rank;
        var card2 = playerState.HoleCard2Rank;
        var boardHigh = texture.HighestRank;

        if (card1 == boardHigh) return card1;
        if (card2 == boardHigh) return card2;

        return Math.Max(card1, card2); // Fallback
    }

    #endregion

    #region [Board Texture Analysis]

    /// <summary>
    /// Determina si el board es muy seco (favorable para apostar).
    /// </summary>
    private static bool IsVeryDryBoard(BoardTexture texture)
    {
        return texture.IsDry &&
               texture.IsRainbow &&
               !texture.IsConnected &&
               !texture.IsPaired &&
               texture.HighestRank <= QueenForce; // Sin cartas muy altas
    }

    /// <summary>
    /// Determina si el board es altamente coordinado (peligroso).
    /// </summary>
    private static bool IsHighlyCoordinatedBoard(BoardTexture texture)
    {
        var dangerFactors = 0;

        if (!texture.IsRainbow) dangerFactors++; // Flush draw possible
        if (texture.IsConnected) dangerFactors++; // Straight draws possible
        if (texture.IsPaired) dangerFactors++; // Trips possible
        if (HasMultipleHighCards(texture)) dangerFactors++; // Multiple overcards

        return dangerFactors >= 2; // 2 o más factores de peligro
    }

    /// <summary>
    /// Verifica si el board tiene múltiples cartas altas.
    /// </summary>
    private static bool HasMultipleHighCards(BoardTexture texture)
    {
        // Esto requeriría acceso a todas las cartas del board
        // Por ahora, usamos una aproximación basada en las propiedades disponibles
        return texture.HasAce && texture.HasKing;
    }

    #endregion

    #region [Specific Scenario Analysis]

    /// <summary>
    /// Analiza escenarios específicos mencionados en las reglas.
    /// </summary>
    public static string AnalyzeSpecificScenario(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards)
    {
        // Ejemplo: AdKh en Ks7hAs → Dos pares (1/2 bote)
        if (HasTwoPairOrBetter(flopResult))
            return "1/2";

        // Ejemplo: AhJs en Qc7dAs → Top pair con J kicker en board seco (1/2 bote)
        if (HasTopPairStrongKickerDryBoard(flopResult, playerState, boardCards))
            return "1/2";

        // Ejemplo: 4dAd en 4h9s3c → Top pair pero kicker muy débil (1/3 bote)
        if (HasTopPairWeakKicker(flopResult, playerState))
            return "1/3";

        // Ejemplo: KcTh en Jh3cKd → Top pair pero kicker medio con straight draws (1/3 bote)
        if (HasTopPairMediumKickerWithDraws(flopResult, playerState, boardCards))
            return "1/3";

        return DetermineContinuationBetSizing(flopResult, playerState);
    }

    private static bool HasTwoPairOrBetter(TableScrapeFlopResult flopResult)
    {
        return flopResult.HeroStrength.HasTwoPair ||
               flopResult.HeroStrength.HasSet ||
               flopResult.HeroStrength.HasFullHouse ||
               flopResult.HeroStrength.Hand >= HeroHand.DoblePareja;
    }

    private static bool HasTopPairStrongKickerDryBoard(TableScrapeFlopResult flopResult, PlayerGameState playerState, IReadOnlyList<BoardData> boardCards)
    {
        return flopResult.HeroStrength.HasTopPair &&
               playerState.Kicker >= JackForce &&
               flopResult.BoardTexture.IsDry;
    }

    private static bool HasTopPairWeakKicker(TableScrapeFlopResult flopResult, PlayerGameState playerState)
    {
        return flopResult.HeroStrength.HasTopPair &&
               playerState.Kicker <= WeakKickerThreshold;
    }

    private static bool HasTopPairMediumKickerWithDraws(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards)
    {
        return flopResult.HeroStrength.HasTopPair &&
               playerState.Kicker >= 7 && playerState.Kicker <= TenForce &&
               (flopResult.BoardTexture.IsConnected || flopResult.Draws.HasStraightDraw);
    }

    #endregion

    #region [Utility Methods]

    /// <summary>
    /// Proporciona información detallada sobre la decisión tomada.
    /// </summary>
    public static string GetDecisionReasoning(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerState)
    {
        var sizing = DetermineContinuationBetSizing(flopResult, playerState);
        var strength = flopResult.HeroStrength;
        var texture = flopResult.BoardTexture;

        return sizing switch
        {
            "1/2" => GetHalfPotReasoning(strength, texture, playerState),
            "1/3" => GetThirdPotReasoning(strength, texture, playerState),
            "Check" => GetCheckReasoning(strength, texture, playerState),
            _ => "Decisión no determinada"
        };
    }

    private static string GetHalfPotReasoning(HeroHandStrength strength, BoardTexture texture, PlayerGameState playerState)
    {
        if (strength.HasSet) return "Set - mano premium que requiere value betting agresivo";
        if (strength.HasTwoPair) return "Dos pares - mano fuerte que debe apostar por value";
        if (strength.HasTopPair && playerState.Kicker >= StrongKickerThreshold)
            return $"Top pair con kicker fuerte ({GetCardName(playerState.Kicker)}) - apuesta por value";
        if (strength.HasTopPair && texture.IsDry)
            return "Top pair en board seco - proteger contra draws";

        return "Mano fuerte que justifica apuesta de 1/2 bote";
    }

    private static string GetThirdPotReasoning(HeroHandStrength strength, BoardTexture texture, PlayerGameState playerState)
    {
        if (strength.HasTopPair && playerState.Kicker <= WeakKickerThreshold)
            return $"Top pair con kicker débil ({GetCardName(playerState.Kicker)}) - value bet conservador";
        if (strength.HasTopPair && IsHighlyCoordinatedBoard(texture))
            return "Top pair en board peligroso - bet sizing reducido por protección";

        return "Mano marginal que justifica apuesta conservadora de 1/3 bote";
    }

    private static string GetCheckReasoning(HeroHandStrength strength, BoardTexture texture, PlayerGameState playerState)
    {
        if (strength.Hand <= HeroHand.CartaAlta)
            return "Sin mano hecha - check para pot control";
        if (strength.HasBottomPair)
            return "Bottom pair - demasiado débil para apostar";

        return "Mano no justifica apuesta - check es la opción más segura";
    }

    private static string GetCardName(int cardRank)
    {
        return cardRank switch
        {
            14 => "A",
            13 => "K",
            12 => "Q",
            11 => "J",
            10 => "T",
            _ => cardRank.ToString()
        };
    }

    #endregion

}
