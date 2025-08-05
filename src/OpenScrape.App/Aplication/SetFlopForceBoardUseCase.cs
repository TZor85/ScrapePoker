using Emgu.CV.Dai;
using OpenScrape.App.Entities;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Aplication;

internal record CardGroup<TKey>(TKey Key, int Count);

/// <summary>
/// Implementa la lógica para analizar el estado del juego en el flop,
/// determinando características del tablero y la mano del jugador.
/// </summary>
public class SetFlopForceBoardUseCase : ISetFlopForceBoardUseCase
{
    private const int AceForce = 14;
    private const int KingForce = 13;
    private const int QueenForce = 12;
    private const int JackForce = 11;
    private const int TenForce = 10;
    private const int AceLowForce = 1;
    private const int MinCardForce = 2;
    private const int MaxCardForce = 14;


    private static readonly int[] RoyalFlushCards = { 10, 11, 12, 13, 14 };
    private static readonly int[] WheelCards = { 1, 2, 3, 4, 5 };


    /// <summary>
    /// Ejecuta el caso de uso para analizar el flop y determinar las características del tablero y la mano.
    /// </summary>
    public SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request)
    {
        ValidateRequest(request);

        var boardCards = GetFlopCards(request.PlayerState.BoardCards);
        var flopResult = request.TableScrapeFlopResult;
        var playerState = request.PlayerState;

        // Análisis del board
        var cardsBySuitOnBoard = GroupCardsBySuit(boardCards);
        var cardsByForceOnBoard = GroupCardsByForce(boardCards);
        var flopForcesOrdered = GetOrderedForces(boardCards);

        var (maxBoardForce, middleBoardForce, bottomBoardForce) = GetBoardForceRanking(flopForcesOrdered);
        var (maxHandCardForce, minHandCardForce) = GetHandForceRanking(playerState);

        // Poblar todas las propiedades
        PopulateBoardTexture(flopResult, boardCards, cardsBySuitOnBoard, cardsByForceOnBoard, flopForcesOrdered);
        PopulateHeroStrength(flopResult, playerState, boardCards, maxBoardForce, middleBoardForce, bottomBoardForce, maxHandCardForce, cardsByForceOnBoard);
        PopulateDrawingOpportunities(flopResult, playerState, boardCards, cardsBySuitOnBoard);
        DetermineOptimalHeroHand(flopResult, boardCards, playerState);

        // Análisis avanzado
        PerformAdvancedBoardAnalysis(flopResult, boardCards, playerState);
        CalculateEquityFactors(flopResult, playerState, boardCards);

        return new SetFlopForceBoardUseCaseResponse
        {
            PlayerState = playerState,
            TableScrapeFlopResult = flopResult
        };
    }

    #region [Validation Methods]

    private static void ValidateRequest(SetFlopForceBoardUseCaseRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (request.PlayerState == null)
            throw new ArgumentNullException(nameof(request.PlayerState));
        if (request.TableScrapeFlopResult == null)
            throw new ArgumentNullException(nameof(request.TableScrapeFlopResult));
        if (request.PlayerState.BoardCards == null)
            throw new ArgumentNullException(nameof(request.PlayerState.BoardCards));

        ValidateHoleCards(request.PlayerState);
    }

    private static void ValidateHoleCards(PlayerGameState playerState)
    {
        if (!IsValidCardForce(playerState.HoleCard1Rank) || !IsValidCardForce(playerState.HoleCard2Rank))
            throw new ArgumentException("Las cartas de mano deben tener valores válidos (2-14)");

        if (!IsValidSuit(playerState.HoleCard1Suit) || !IsValidSuit(playerState.HoleCard2Suit))
            throw new ArgumentException("Los palos de las cartas deben ser válidos (0-3)");
    }

    private static bool IsValidCardForce(int force) => force >= MinCardForce && force <= MaxCardForce;
    private static bool IsValidSuit(int suit) => suit >= 1 && suit <= 4;

    private static IReadOnlyList<BoardData> GetFlopCards(List<BoardData> boardCards)
    {
        var flopCards = boardCards.Where(w => w.Position == BoardPosition.Flop).ToList();

        if (flopCards.Count != 3)
            throw new ArgumentException("El flop debe contener exactamente 3 cartas.", nameof(boardCards));

        return flopCards;
    }

    #endregion

    #region [Grouping and Ordering Methods]

    private static List<CardGroup<int>> GroupCardsBySuit(IReadOnlyList<BoardData> boardCards) =>
        boardCards.GroupBy(card => card.Suit)
                  .Select(group => new CardGroup<int>(group.Key, group.Count()))
                  .ToList();

    private static List<CardGroup<int>> GroupCardsByForce(IReadOnlyList<BoardData> boardCards) =>
        boardCards.GroupBy(card => card.Force)
                  .Select(group => new CardGroup<int>(group.Key, group.Count()))
                  .ToList();

    private static List<int> GetOrderedForces(IReadOnlyList<BoardData> boardCards) =>
        boardCards.Select(card => card.Force).OrderBy(force => force).ToList();

    private static (int max, int middle, int bottom) GetBoardForceRanking(List<int> orderedForces) =>
        (orderedForces[2], orderedForces[1], orderedForces[0]);

    private static (int max, int min) GetHandForceRanking(PlayerGameState playerState) =>
        (Math.Max(playerState.HoleCard1Rank, playerState.HoleCard2Rank),
         Math.Min(playerState.HoleCard1Rank, playerState.HoleCard2Rank));

    #endregion

    #region [Board Texture Analysis]

    private void PopulateBoardTexture(
        TableScrapeFlopResult flopResult,
        IReadOnlyList<BoardData> boardCards,
        List<CardGroup<int>> cardsBySuitOnBoard,
        List<CardGroup<int>> cardsByForceOnBoard,
        List<int> flopForcesOrdered)
    {
        var texture = flopResult.BoardTexture;

        // Características básicas del board
        texture.IsRainbow = cardsBySuitOnBoard.Count == 3;
        texture.IsPaired = cardsByForceOnBoard.Any(g => g.Count >= 2);
        texture.IsConnected = IsConnectedBoard(flopForcesOrdered);
        texture.IsDry = texture.IsRainbow && !texture.IsConnected && !texture.IsPaired;
        texture.IsCoordinated = !texture.IsDry;

        // Rankings del board
        texture.HighestRank = flopForcesOrdered[2];
        texture.LowestRank = flopForcesOrdered[0];
        texture.HasAce = boardCards.Any(card => card.Force == AceForce);
        texture.HasKing = boardCards.Any(card => card.Force == KingForce);
    }

    private static bool IsConnectedBoard(List<int> orderedForces)
    {
        var gap1 = orderedForces[1] - orderedForces[0];
        var gap2 = orderedForces[2] - orderedForces[1];
        var totalSpread = orderedForces[2] - orderedForces[0];

        // Conectado si hay cartas consecutivas o dentro de un rango de 4
        var hasConsecutiveCards = (gap1 > 0 && gap1 <= 2) || (gap2 > 0 && gap2 <= 2);
        var isWithinStraightRange = totalSpread <= 4 && orderedForces.Distinct().Count() == 3;

        // Casos especiales para As
        var hasAceConnector = CheckAceConnector(orderedForces);

        return hasConsecutiveCards || isWithinStraightRange || hasAceConnector;
    }

    private static bool CheckAceConnector(List<int> orderedForces)
    {
        if (!orderedForces.Contains(AceForce)) return false;

        // A-2-3, A-2-4, A-2-5, A-3-4, A-3-5, A-4-5 (wheel connectors)
        var lowCards = orderedForces.Where(f => f <= 5).ToList();
        if (lowCards.Count >= 2) return true;

        // A-K-Q, A-K-J, A-Q-J (broadway connectors)
        var highCards = orderedForces.Where(f => f >= JackForce).ToList();
        return highCards.Count >= 2;
    }


    #endregion

    #region [Hero Strength Analysis]

    private void PopulateHeroStrength(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards,
        int maxBoardForce, int middleBoardForce, int bottomBoardForce,
        int maxHandCardForce,
        List<CardGroup<int>> cardsByForceOnBoard)
    {
        var strength = flopResult.HeroStrength;
        var handForce0 = playerState.HoleCard1Rank;
        var handForce1 = playerState.HoleCard2Rank;

        // Características básicas de la mano
        strength.HandIsConnected = IsHandConnected(handForce0, handForce1);
        strength.HasHighCard = maxHandCardForce >= KingForce || boardCards.Any(c => c.Force >= KingForce);
        strength.HasOverCards = maxHandCardForce > maxBoardForce;
        strength.HasNoOverCards = boardCards.All(card => card.Force <= maxHandCardForce);

        if (playerState.HavePocketPair)
        {
            AnalyzePocketPair(strength, playerState, boardCards, maxBoardForce, cardsByForceOnBoard);
        }
        else
        {
            AnalyzeUnpairedHand(strength, playerState, boardCards, maxBoardForce, middleBoardForce, bottomBoardForce);
        }
    }

    private static bool IsHandConnected(int force0, int force1)
    {
        var diff = Math.Abs(force0 - force1);

        // Conectores directos
        if (diff == 1) return true;

        // Gappers (hasta 4 gaps)
        if (diff <= 4) return true;

        // Casos especiales con As
        if ((force0 == AceForce && force1 <= 5) || (force1 == AceForce && force0 <= 5)) return true;
        if ((force0 == AceForce && force1 >= TenForce) || (force1 == AceForce && force0 >= TenForce)) return true;

        return false;
    }


    private void AnalyzePocketPair(
        HeroHandStrength strength,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards,
        int maxBoardForce,
        List<CardGroup<int>> cardsByForceOnBoard)
    {
        var pocketRank = playerState.HoleCard1Rank;
        var boardPairs = cardsByForceOnBoard.Where(g => g.Count >= 2).ToList();
        var boardTrips = cardsByForceOnBoard.Where(g => g.Count == 3).FirstOrDefault();

        strength.HasOverPair = pocketRank > maxBoardForce;
        strength.HasSet = boardCards.Any(c => c.Force == pocketRank);

        if (strength.HasSet)
        {
            strength.HasFullHouse = boardPairs.Any(p => p.Key != pocketRank);
        }
        else if (boardPairs.Any())
        {
            var boardPairRank = boardPairs.First().Key;
            strength.HasTwoPair = pocketRank != boardPairRank;
            strength.HasFullHouse = boardTrips != null;
        }
    }

    private void AnalyzeUnpairedHand(
        HeroHandStrength strength,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards,
        int maxBoardForce, int middleBoardForce, int bottomBoardForce)
    {
        var handForce0 = playerState.HoleCard1Rank;
        var handForce1 = playerState.HoleCard2Rank;

        var card0MakesPair = boardCards.Any(c => c.Force == handForce0);
        var card1MakesPair = boardCards.Any(c => c.Force == handForce1);

        if (card0MakesPair && card1MakesPair)
        {
            strength.HasTwoPair = true;
        }
        else if (card0MakesPair || card1MakesPair)
        {
            var pairedCardForce = card0MakesPair ? handForce0 : handForce1;
            playerState.Kicker = card0MakesPair ? handForce1 : handForce0;

            strength.HasTopPair = pairedCardForce == maxBoardForce;
            strength.HasMiddlePair = pairedCardForce == middleBoardForce;
            strength.HasBottomPair = pairedCardForce == bottomBoardForce;
        }
        else
        {
            // Sin par - verificar si ambas cartas son overcards
            strength.HasHighCard = handForce0 > maxBoardForce && handForce1 > maxBoardForce;
        }
    }

    #endregion

    #region Drawing Opportunities Analysis

    private void PopulateDrawingOpportunities(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards,
        List<CardGroup<int>> cardsBySuitOnBoard)
    {
        var draws = flopResult.Draws;

        draws.HasFlushDraw = CheckFlushDrawOptimized(boardCards, playerState.HoleCard1Suit, playerState.HoleCard2Suit);
        draws.HasStraightDraw = CheckStraightDrawOptimized(boardCards, playerState.HoleCard1Rank, playerState.HoleCard2Rank);
        draws.HasBackdoorFlushDraw = CheckBackdoorFlushDraw(cardsBySuitOnBoard, playerState);
        draws.HasDrawingHand = draws.HasFlushDraw || draws.HasStraightDraw;
    }

    private static bool CheckFlushDrawOptimized(IReadOnlyList<BoardData> boardCards, int handSuit0, int handSuit1)
    {
        var allSuits = new List<int> { handSuit0, handSuit1 };
        allSuits.AddRange(boardCards.Select(card => card.Suit));

        return allSuits.GroupBy(suit => suit).Any(group => group.Count() == 4);
    }

    private static bool CheckStraightDrawOptimized(IReadOnlyList<BoardData> boardCards, int handForce0, int handForce1)
    {
        var allForces = new HashSet<int> { handForce0, handForce1 };

        foreach (var card in boardCards)
        {
            allForces.Add(card.Force);
            if (card.Force == AceForce) allForces.Add(AceLowForce);
        }

        if (handForce0 == AceForce) allForces.Add(AceLowForce);
        if (handForce1 == AceForce) allForces.Add(AceLowForce);

        var sortedForces = allForces.OrderBy(f => f).ToList();

        return HasOpenEndedStraightDraw(sortedForces) || HasGutshotStraightDraw(sortedForces);
    }

    private static bool HasOpenEndedStraightDraw(List<int> sortedForces)
    {
        for (int i = 0; i <= sortedForces.Count - 4; i++)
        {
            if (sortedForces[i + 3] - sortedForces[i] == 3)
            {
                // Verificar que son exactamente 4 cartas consecutivas
                var consecutiveCount = 1;
                for (int j = i; j < i + 3; j++)
                {
                    if (sortedForces[j + 1] - sortedForces[j] == 1)
                        consecutiveCount++;
                }
                if (consecutiveCount == 4) return true;
            }
        }
        return false;
    }

    private static bool HasGutshotStraightDraw(List<int> sortedForces)
    {
        for (int i = 0; i <= sortedForces.Count - 4; i++)
        {
            var span = sortedForces[i + 3] - sortedForces[i];
            if (span == 4)
            {
                // Verificar que hay exactamente un gap
                var gaps = 0;
                for (int j = i; j < i + 3; j++)
                {
                    var diff = sortedForces[j + 1] - sortedForces[j];
                    if (diff > 1) gaps += diff - 1;
                }
                if (gaps == 1) return true;
            }
        }
        return false;
    }

    private static bool CheckBackdoorFlushDraw(List<CardGroup<int>> cardsBySuitOnBoard, PlayerGameState playerState)
    {
        if (!playerState.IsSuited) return false;

        var handSuit = playerState.HoleCard1Suit;
        var suitGroup = cardsBySuitOnBoard.FirstOrDefault(g => g.Key == handSuit);

        return suitGroup?.Count == 1; // Una carta del mismo palo en el board
    }

    #endregion

    #region [Hand Evaluation]

    private void DetermineOptimalHeroHand(
        TableScrapeFlopResult flopResult,
        IReadOnlyList<BoardData> boardCards,
        PlayerGameState playerState)
    {
        var allCards = CreateAllCardsList(boardCards, playerState);
        var handRanking = EvaluatePokerHand(allCards);

        flopResult.HeroStrength.Hand = handRanking;

        // Si no hay mano hecha, verificar proyectos
        if (handRanking <= HeroHand.CartaAlta)
        {
            flopResult.HeroStrength.Hand = DetermineDrawHand(flopResult.Draws);
        }
    }

    private static List<BoardData> CreateAllCardsList(IReadOnlyList<BoardData> boardCards, PlayerGameState playerState)
    {
        var allCards = new List<BoardData>(boardCards)
        {
            new() { Force = playerState.HoleCard1Rank, Suit = playerState.HoleCard1Suit, Name = "Hole1" },
            new() { Force = playerState.HoleCard2Rank, Suit = playerState.HoleCard2Suit, Name = "Hole2" }
        };
        return allCards;
    }

    private static HeroHand EvaluatePokerHand(List<BoardData> allCards)
    {
        var forces = allCards.Select(c => c.Force).ToList();
        var suits = allCards.Select(c => c.Suit).ToList();

        // Evaluar de mayor a menor fuerza
        if (IsRoyalFlush(forces, suits)) return HeroHand.EscaleraReal;
        if (IsStraightFlush(forces, suits)) return HeroHand.EscaleraDeColor;
        if (IsFourOfAKind(forces)) return HeroHand.Poker;
        if (IsFullHouse(forces)) return HeroHand.Full;
        if (IsFlush(suits)) return HeroHand.Color;
        if (IsStraight(forces)) return HeroHand.Escalera;
        if (IsThreeOfAKind(forces)) return HeroHand.Trio;
        if (IsTwoPair(forces)) return HeroHand.DoblePareja;
        if (IsOnePair(forces)) return HeroHand.Pareja;

        return HeroHand.CartaAlta;
    }

    private static bool IsRoyalFlush(List<int> forces, List<int> suits)
    {
        return IsFlush(suits) && RoyalFlushCards.All(forces.Contains);
    }

    private static bool IsStraightFlush(List<int> forces, List<int> suits)
    {
        return IsFlush(suits) && IsStraight(forces);
    }

    private static bool IsFourOfAKind(List<int> forces)
    {
        return forces.GroupBy(f => f).Any(g => g.Count() >= 4);
    }

    private static bool IsFullHouse(List<int> forces)
    {
        var groups = forces.GroupBy(f => f).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        return groups.Count >= 2 && groups[0] >= 3 && groups[1] >= 2;
    }

    private static bool IsFlush(List<int> suits)
    {
        return suits.GroupBy(s => s).Any(g => g.Count() >= 5);
    }

    private static bool IsStraight(List<int> forces)
    {
        var uniqueForces = new HashSet<int>(forces);

        // Agregar As bajo si hay As alto
        if (uniqueForces.Contains(AceForce))
            uniqueForces.Add(AceLowForce);

        var sortedForces = uniqueForces.OrderBy(f => f).ToList();

        // Buscar 5 cartas consecutivas
        for (int i = 0; i <= sortedForces.Count - 5; i++)
        {
            if (sortedForces[i + 4] - sortedForces[i] == 4)
            {
                // Verificar que son realmente consecutivas
                bool isConsecutive = true;
                for (int j = i; j < i + 4; j++)
                {
                    if (sortedForces[j + 1] - sortedForces[j] != 1)
                    {
                        isConsecutive = false;
                        break;
                    }
                }
                if (isConsecutive) return true;
            }
        }

        return false;
    }

    private static bool IsThreeOfAKind(List<int> forces)
    {
        return forces.GroupBy(f => f).Any(g => g.Count() >= 3);
    }

    private static bool IsTwoPair(List<int> forces)
    {
        return forces.GroupBy(f => f).Count(g => g.Count() >= 2) >= 2;
    }

    private static bool IsOnePair(List<int> forces)
    {
        return forces.GroupBy(f => f).Any(g => g.Count() >= 2);
    }

    private static HeroHand DetermineDrawHand(DrawingOpportunities draws)
    {
        if (draws.HasFlushDraw && draws.HasStraightDraw)
            return HeroHand.ProyectoEscaleraColor;
        if (draws.HasFlushDraw)
            return HeroHand.ProyectoColor;
        if (draws.HasStraightDraw)
            return HeroHand.ProyectoEscalera;

        return HeroHand.Nada;
    }

    #endregion

    #region [Advanced Analysis]

    private void PerformAdvancedBoardAnalysis(
        TableScrapeFlopResult flopResult,
        IReadOnlyList<BoardData> boardCards,
        PlayerGameState playerState)
    {
        AnalyzeBoardDangerLevel(flopResult, boardCards);
        AnalyzePositionalAdvantage(flopResult, playerState);
        AnalyzeBluffingOpportunities(flopResult, boardCards, playerState);
    }

    private static void AnalyzeBoardDangerLevel(TableScrapeFlopResult flopResult, IReadOnlyList<BoardData> boardCards)
    {
        var texture = flopResult.BoardTexture;
        var dangerFactors = 0;

        if (!texture.IsRainbow) dangerFactors++; // Flush possible
        if (texture.IsConnected) dangerFactors++; // Straight possible
        if (texture.IsPaired) dangerFactors++; // Trips/Full House possible
        if (boardCards.Any(c => c.Force >= JackForce)) dangerFactors++; // High cards

        // Esta información podría agregarse a una nueva propiedad en BoardTexture
        // texture.DangerLevel = dangerFactors switch
        // {
        //     0 => DangerLevel.Low,
        //     1 => DangerLevel.Medium,
        //     2 => DangerLevel.High,
        //     _ => DangerLevel.Extreme
        // };
    }

    private static void AnalyzePositionalAdvantage(TableScrapeFlopResult flopResult, PlayerGameState playerState)
    {
        // Análisis basado en posición para determinar agresividad recomendada
        var inPosition = playerState.IsInPosition;
        var hasStrongHand = flopResult.HasStrongHand;
        var hasDraws = flopResult.Draws.HasDrawingHand;

        // Esta lógica podría expandirse para incluir recomendaciones de acción
        // basadas en posición, fuerza de mano y textura del board
    }

    private static void AnalyzeBluffingOpportunities(
        TableScrapeFlopResult flopResult,
        IReadOnlyList<BoardData> boardCards,
        PlayerGameState playerState)
    {
        // Análisis de oportunidades de bluff basado en:
        // - Board texture (boards secos favorecen bluffs)
        // - Posición del jugador
        // - Imagen en la mesa
        // - Stack sizes relativos

        var texture = flopResult.BoardTexture;
        var canBluffEffectively = texture.IsDry && playerState.IsInPosition;

        // Esta información podría agregarse como una nueva propiedad
        // flopResult.BluffingOpportunity = canBluffEffectively;
    }

    private static void CalculateEquityFactors(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerState,
        IReadOnlyList<BoardData> boardCards)
    {
        // Cálculo aproximado de equity basado en:
        // - Fuerza actual de la mano
        // - Outs disponibles
        // - Posición
        // - Número de oponentes activos

        var baseEquity = CalculateBaseEquity(flopResult.HeroStrength.Hand);
        var drawEquity = CalculateDrawEquity(flopResult.Draws);
        var positionalBonus = playerState.IsInPosition ? 0.05 : 0.0;

        // var estimatedEquity = baseEquity + drawEquity + positionalBonus;
        // flopResult.EstimatedEquity = Math.Min(1.0, estimatedEquity);
    }

    private static double CalculateBaseEquity(HeroHand hand)
    {
        return hand switch
        {
            HeroHand.EscaleraReal => 1.0,
            HeroHand.EscaleraDeColor => 0.95,
            HeroHand.Poker => 0.90,
            HeroHand.Full => 0.85,
            HeroHand.Color => 0.70,
            HeroHand.Escalera => 0.65,
            HeroHand.Trio => 0.60,
            HeroHand.DoblePareja => 0.45,
            HeroHand.Pareja => 0.25,
            _ => 0.15
        };
    }

    private static double CalculateDrawEquity(DrawingOpportunities draws)
    {
        var equity = 0.0;

        if (draws.HasFlushDraw) equity += 0.35; // ~9 outs
        if (draws.HasStraightDraw) equity += 0.32; // ~8 outs
        if (draws.HasBackdoorFlushDraw) equity += 0.04; // ~1.5 outs

        return Math.Min(0.50, equity); // Cap para evitar sobreestimación
    }

    #endregion

    #region [Legacy Methods (Mantenidos para compatibilidad)]

    [Obsolete("Use CheckStraightDrawOptimized instead")]
    private bool ProyectoEscalera(int card1, int card2, int card3, int card4, int card5)
    {
        var forces = new List<int> { card1, card2, card3, card4, card5 }.OrderBy(f => f).ToList();
        var differences = new int[forces.Count - 1];

        for (int i = 0; i < differences.Length; i++)
        {
            differences[i] = forces[i + 1] - forces[i];
        }

        return !differences.Any(d => d > 2);
    }

    [Obsolete("Use IsStraight instead")]
    private bool ExisteEscalera(List<int> cards)
    {
        cards.Sort();
        var differences = new int[cards.Count - 1];

        for (int i = 0; i < differences.Length; i++)
        {
            differences[i] = cards[i + 1] - cards[i];
        }

        return differences.All(d => d == 1);
    }

    [Obsolete("Use CheckFlushDrawOptimized instead")]
    private bool HasflushDraw(List<BoardData> boardData, int cardSuit0, int cardSuit1)
    {
        var allCards = new List<int> { cardSuit0, cardSuit1 };
        allCards.AddRange(boardData.Select(s => s.Suit));

        return allCards.GroupBy(g => g).Any(a => a.Count() >= 4);
    }

    [Obsolete("Use CheckStraightDrawOptimized instead")]
    private bool HasStraightDraw(List<BoardData> boardData, int cardForce0, int cardForce1)
    {
        var allCards = new List<int> { cardForce0, cardForce1 };
        allCards.AddRange(boardData.Select(s => s.Force));

        var distinctRanks = allCards.Distinct().OrderBy(o => o).ToList();

        for (int i = 0; i < distinctRanks.Count - 3; i++)
        {
            if (distinctRanks[i + 3] - distinctRanks[i] <= 4)
                return true;
        }

        return false;
    }

    #endregion

}




