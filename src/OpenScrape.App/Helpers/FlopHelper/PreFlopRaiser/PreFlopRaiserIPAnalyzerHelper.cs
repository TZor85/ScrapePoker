using OpenScrape.App.Entities;

namespace OpenScrape.App.Helpers.FlopHelper.PreFlopRaiser;

/// <summary>
/// Analizador especializado para decisiones IP (In Position) del pre-flop raiser en BUTTON
/// Implementa estrategia de betting sizing basada en fuerza de mano y textura del board
/// </summary>
public static class PreFlopRaiserIPAnalyzerHelper
{
    private const int AceForce = 14;
    private const int KingForce = 13;
    private const int QueenForce = 12;
    private const int JackForce = 11;

    /// <summary>
    /// Analiza la situación del preflop raiser en posición y determina el sizing óptimo
    /// </summary>
    /// <param name="playerState">Estado actual del jugador</param>
    /// <param name="flopResult">Resultado del análisis del flop</param>
    /// <returns>Recomendación de acción: "3/4", "1/3", "Check"</returns>
    public static string AnalyzeBettingAction(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        if (playerState == null || flopResult == null)
            return "Check";

        // Verificar si está en posición (Button o equivalente)
        if (!playerState.IsInPosition)
            return "Check";

        // === CATEGORÍA 3/4 BOTE: Manos muy fuertes o nutted ===
        if (ShouldBet34Pot(playerState, flopResult))
            return "Bet 3/4";

        // === CATEGORÍA CHECK: Spots muy específicos ===
        if (ShouldCheck(playerState, flopResult))
            return "Check";

        // === CATEGORÍA 1/3 BOTE: Todo lo demás ===
        return "Bet 1/3";
    }

    private static bool ShouldBet34Pot(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        // AA siempre (sin importar el flop)
        if (playerState.HavePocketPair &&
            playerState.HoleCard1Rank == AceForce)
            return true;

        // Sets/trips
        if (flopResult.HeroStrength.HasSet || flopResult.HeroStrength.HasThreeOfAKind)
            return true;

        // Top pair con nuts kicker en boards favorables
        if (flopResult.HeroStrength.HasTopPair &&
            HasNutsKicker(playerState, flopResult) &&
            IsFavorableBoard(flopResult))
            return true;

        // Dos pares fuertes
        if (flopResult.HeroStrength.HasTwoPair && IsStrongTwoPair(playerState, flopResult))
            return true;

        // Straight draws muy fuertes (incluye gutshots con pocos outs y flush draws a 1 carta)
        if (IsVeryStrongDraw(playerState, flopResult))
            return true;

        return false;
    }

    private static bool ShouldCheck(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        // Draws muy débiles en boards peligrosos
        if (HasWeakDrawOnDangerousBoard(playerState, flopResult))
            return true;

        // Manos que prefieren pot control absoluto
        if (NeedsPotControl(playerState, flopResult))
            return true;

        return false;
    }

    private static bool HasNutsKicker(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        // Si tiene top pair, verificar si el kicker es nuts (A o K dependiendo del board)
        if (!flopResult.HeroStrength.HasTopPair)
            return false;

        int kicker = playerState.Kicker;

        // Si el kicker es As, siempre es nuts
        if (kicker == AceForce)
            return true;

        // Si el kicker es K y no hay A en el board, es nuts
        if (kicker == KingForce && !flopResult.BoardTexture.HasAce)
            return true;

        return false;
    }

    private static bool IsFavorableBoard(TableScrapeFlopResult flopResult)
    {
        // Board seco o con muy pocas outs para los proyectos
        if (flopResult.BoardTexture.IsDry)
            return true;

        // Board rainbow sin muchos draws disponibles
        if (flopResult.BoardTexture.IsRainbow &&
            !flopResult.BoardTexture.IsConnected &&
            !flopResult.Draws.HasFlushDraw)
            return true;

        // Boards con pocas cartas altas (menos amenazantes)
        if (!flopResult.BoardTexture.HasAce &&
            !flopResult.BoardTexture.HasKing &&
            flopResult.BoardTexture.HighestRank <= QueenForce)
            return true;

        return false;
    }

    private static bool IsStrongTwoPair(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        // Dos pares con cartas altas (A, K, Q, J)
        int card1 = playerState.HoleCard1Rank;
        int card2 = playerState.HoleCard2Rank;

        return (card1 >= JackForce || card2 >= JackForce);
    }

    private static bool IsVeryStrongDraw(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        // OESD en boards favorables
        if (flopResult.Draws.HasStraightDraw && IsFavorableBoard(flopResult))
            return true;

        // Flush draw (9 outs) en boards no muy coordinados
        if (flopResult.Draws.HasFlushDraw &&
            !flopResult.BoardTexture.IsCoordinated)
            return true;

        // Combo draws (flush + straight)
        if (flopResult.Draws.HasFlushDraw && flopResult.Draws.HasStraightDraw)
            return true;

        return false;
    }

    private static bool HasWeakDrawOnDangerousBoard(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        bool isDangerousBoard = IsDangerousBoard(flopResult);

        // Solo backdoor draws en boards peligrosos
        if (flopResult.Draws.HasBackdoorFlushDraw &&
            !flopResult.Draws.HasStrongDraw &&
            isDangerousBoard)
            return true;

        // Gutshots débiles en boards con muchos proyectos
        if (flopResult.Draws.HasStraightDraw &&
            flopResult.Draws.HasFlushDraw &&
            isDangerousBoard)
            return true;

        return false;
    }

    private static bool IsDangerousBoard(TableScrapeFlopResult flopResult)
    {
        // Boards con proyectos o muchas cartas altas
        bool hasDraws = flopResult.Draws.HasFlushDraw ||
                       flopResult.BoardTexture.IsConnected;

        bool hasManyHighCards = (flopResult.BoardTexture.HasAce ? 1 : 0) +
                               (flopResult.BoardTexture.HasKing ? 1 : 0) +
                               (flopResult.BoardTexture.HighestRank >= QueenForce ? 1 : 0) >= 2;

        return hasDraws || hasManyHighCards || flopResult.BoardTexture.IsCoordinated;
    }

    private static bool NeedsPotControl(PlayerGameState playerState, TableScrapeFlopResult flopResult)
    {
        // Manos marginales como middle pair débil en boards coordinados
        if (flopResult.HeroStrength.HasMiddlePair &&
            flopResult.BoardTexture.IsCoordinated)
            return true;

        // Bottom pair en boards peligrosos
        if (flopResult.HeroStrength.HasBottomPair &&
            IsDangerousBoard(flopResult))
            return true;

        // Ace high en boards muy peligrosos
        if (flopResult.HeroStrength.HasHighCard &&
            flopResult.BoardTexture.HasAce &&
            IsDangerousBoard(flopResult))
            return true;

        // Pocket pairs bajas en boards muy coordinados
        if (playerState.HavePocketPair &&
            playerState.HoleCard1Rank < JackForce &&
            !flopResult.HeroStrength.HasOverPair &&
            flopResult.BoardTexture.IsCoordinated)
            return true;

        return false;
    }


}




