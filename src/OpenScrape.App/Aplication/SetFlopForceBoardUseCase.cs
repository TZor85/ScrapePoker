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

    
    /// <summary>
    /// Ejecuta el caso de uso para analizar el flop y determinar las características del tablero y la mano.
    /// </summary>
    /// <param name="request">Los datos de entrada con la información de las cartas en mesa y en mano.</param>
    /// <returns>Una respuesta con los resultados del análisis del flop. 
    /// El objeto TableScrapeFlopResult dentro del request es actualizado.</returns>
    /// <exception cref="ArgumentNullException">Si <paramref name="request"/>, <paramref name="request.TableScrapeResult"/>,
    /// o <paramref name="request.TableScrapeFlopResult"/> son nulos.</exception>
    /// <exception cref="ArgumentException">Si <paramref name="request.TableScrapeResult.DataBoard"/> no contiene exactamente 3 cartas para el flop.</exception>
    public SetFlopForceBoardUseCaseResponse Execute(SetFlopForceBoardUseCaseRequest request)
    {

        if (request == null) throw new ArgumentNullException(nameof(request));
        if (request.TableScrapeResult == null) throw new ArgumentNullException(nameof(request.TableScrapeResult));
        if (request.TableScrapeFlopResult == null) throw new ArgumentNullException(nameof(request.TableScrapeFlopResult));
        if (request.TableScrapeResult.BoardCards == null) throw new ArgumentNullException(nameof(request.TableScrapeResult.BoardCards));

        IReadOnlyList<BoardData> boardCards = request.TableScrapeResult.BoardCards;

        if (boardCards.Count != 3)
        {
            throw new ArgumentException("El flop (DataBoard) debe contener exactamente 3 cartas.", nameof(request.TableScrapeResult.BoardCards));
        }

        var flopResult = request.TableScrapeFlopResult;
        var tableResult = request.TableScrapeResult;   

        // --- 1. Análisis del Flop (Board) ---
        var cardsBySuitOnBoard = boardCards
            .GroupBy(card => card.Suit)
            .Select(group => new CardGroup<int>(group.Key, group.Count()))
            .ToList();

        var cardsByForceOnBoard = boardCards
            .GroupBy(card => card.Force)
            .Select(group => new CardGroup<int>(group.Key, group.Count()))
            .ToList();

        var flopForcesOrdered = boardCards.Select(card => card.Force).OrderBy(force => force).ToList();
        int maxBoardForce = flopForcesOrdered[2];
        int middleBoardForce = flopForcesOrdered[1];
        int bottomBoardForce = flopForcesOrdered[0];

        // --- 2. Análisis de la Mano del Jugador (Hole Cards) ---
        int handCardForce0 = tableResult.HoleCard1Rank;
        int handCardForce1 = tableResult.HoleCard2Rank;
        int handCardSuit0 = tableResult.HoleCard1Suit;
        int handCardSuit1 = tableResult.HoleCard2Suit;
        int maxHandCardForce = Math.Max(handCardForce0, handCardForce1);

        // --- 3. Poblar Propiedades del Flop y Mano ---
        PopulateFlopProperties(flopResult, boardCards, cardsBySuitOnBoard, cardsByForceOnBoard, flopForcesOrdered, maxHandCardForce);
        PopulateHandProperties(flopResult, tableResult, boardCards, maxBoardForce, middleBoardForce, bottomBoardForce, cardsBySuitOnBoard);

        // --- 4. Determinar la Mano del Héroe ---
        DetermineHeroHand(flopResult, boardCards, handCardForce0, handCardForce1, handCardSuit0, handCardSuit1);

        // Un flop es coordinado si no es "seco" (es decir, tiene potencial de proyecto o está emparejado).
        flopResult.IsCoordinated = !flopResult.IsDry || flopResult.Hand >= HeroHand.ProyectoEscalera;


        return new SetFlopForceBoardUseCaseResponse
        {
            TableScrapeResult = tableResult,
            TableScrapeFlopResult = flopResult
        };

    }

    /// <summary>
    /// Calcula y establece las propiedades generales del flop.
    /// </summary>
    private void PopulateFlopProperties(
        TableScrapeFlopResult flopResult,
        IReadOnlyList<BoardData> boardCards,
        List<CardGroup<int>> cardsBySuitOnBoard,
        List<CardGroup<int>> cardsByForceOnBoard,
        List<int> flopForcesOrdered,
        int maxHandCardForce)
    {
        flopResult.HasHighCard = boardCards.Any(card => card.Force == AceForce || card.Force == KingForce);
        flopResult.IsRainbow = cardsBySuitOnBoard.Count == 3; // Tres palos diferentes.

        // Un flop está conectado si dos cartas cualesquiera tienen una diferencia de 1 o 2,
        // o si las tres cartas forman un gutshot/OESD (ej. 5,7,9 -> gap de 2, gap de 2; 5,6,8 -> gap de 1, gap de 2).
        // La lógica original era (ranks[1] - ranks[0] <= 2) || (ranks[2] - ranks[1] <= 2).
        // Una definición más robusta podría ser más compleja, pero mantenemos la simplicidad.
        int gap1 = flopForcesOrdered[1] - flopForcesOrdered[0];
        int gap2 = flopForcesOrdered[2] - flopForcesOrdered[1];
        int outerGap = flopForcesOrdered[2] - flopForcesOrdered[0]; // Gap entre la más alta y la más baja

        // Conectado si hay dos cartas consecutivas con gap <= 2 (y >0 para evitar pares)
        // O si las tres cartas están dentro de un rango de 4 (ej. 5,7,9 -> 9-5=4; 2,3,4 -> 4-2=2)
        // Esto cubre secuencias como A23, 567, TJQ, QKA, y también conectores con gaps como 578, 8TJ.
        bool twoConsecutiveConnected = (gap1 > 0 && gap1 <= 2) || (gap2 > 0 && gap2 <= 2);
        bool threeCardSpreadConnected = outerGap > 0 && outerGap <= 4 && flopForcesOrdered.Distinct().Count() == 3; // Asegura 3 cartas distintas en el rango

        flopResult.IsConnected = twoConsecutiveConnected || threeCardSpreadConnected;

        flopResult.IsPaired = cardsByForceOnBoard.Any(g => g.Count == 2); // Hay un par en el flop.
                                                                              // cardsByForceOnBoard.Any(g => g.Count == 3) para trío en flop.

        flopResult.IsDry = flopResult.IsRainbow && !flopResult.IsConnected && !flopResult.IsPaired && !cardsByForceOnBoard.Any(g => g.Count == 3);

        // Original: !boardCards.All(a => a.Force > maxHandCardForce)
        // Esto significa: "No es verdad que TODAS las cartas del flop sean MÁS ALTAS que la carta más alta de la mano".
        // O, "Al menos una carta del flop es MENOR O IGUAL que la carta más alta de la mano".
        // Esto parece correcto si la intención es "El flop NO presenta solo overcards a mi mano".
        flopResult.NoOverCards = boardCards.Any(card => card.Force <= maxHandCardForce);

        // La lógica original `cartasMismoPalo.Count() != 3` es equivalente a `cardsBySuitOnBoard.Count < 3`.
        // Esto es cierto si hay dos o una carta del mismo palo.
        // Un "FlushDrawInFlop" más preciso sería si exactamente dos cartas son del mismo palo.
        flopResult.FlushDrawInFlop = cardsBySuitOnBoard.Any(s => s.Count == 2);
    }

    /// <summary>
    /// Calcula y establece las propiedades de la mano del jugador en relación con el flop.
    /// </summary>
    private void PopulateHandProperties(
        TableScrapeFlopResult flopResult,
        PlayerGameState playerGameState,
        IReadOnlyList<BoardData> boardCards, 
        int maxBoardForce, int middleBoardForce, int bottomBoardForce,
        List<CardGroup<int>> cardsBySuitOnBoard)
    {
        int handCardForce0 = playerGameState.HoleCard1Rank;
        int handCardForce1 = playerGameState.HoleCard2Rank;
        int handCardSuit0 = playerGameState.HoleCard1Suit;
        int handCardSuit1 = playerGameState.HoleCard2Suit;

        // La definición de "conectado" puede variar. Math.Abs == 1 es para conectores directos.
        int forceDiff = Math.Abs(handCardForce0 - handCardForce1);
        flopResult.HandIsConnected = forceDiff == 1 || (forceDiff > 1 && forceDiff <= 4); // Incluye conectores y gappers

        flopResult.HaveAce = handCardForce0 == AceForce || handCardForce1 == AceForce;
        flopResult.HaveKing = handCardForce0 == KingForce || handCardForce1 == KingForce;
        flopResult.GetHighestRank = Math.Max(handCardForce0, handCardForce1);
        flopResult.GetLowestRank = Math.Min(handCardForce0, handCardForce1);

        flopResult.HasOverCards = flopResult.GetHighestRank > maxBoardForce;

        flopResult.HaveFlushDraw = CheckFlushDraw(boardCards, handCardSuit0, handCardSuit1);
        flopResult.HaveStraightDraw = CheckStraightDraw(boardCards, handCardForce0, handCardForce1);
        flopResult.HaveDrawingHand = flopResult.HaveFlushDraw || flopResult.HaveStraightDraw;

        if (playerGameState.HavePocketPair)
        {
            flopResult.HasOverPair = handCardForce0 > maxBoardForce;
        }
        else // No tiene par en mano
        {
            // Dos Pares: una carta de la mano hace par con el flop, y la otra carta de la mano hace otro par con el flop.
            // O una carta de la mano hace par con una carta del flop, y la otra carta de la mano hace par con OTRA carta del flop.
            bool card0MakesPairWithBoard = boardCards.Any(c => c.Force == handCardForce0);
            bool card1MakesPairWithBoard = boardCards.Any(c => c.Force == handCardForce1);

            if (card0MakesPairWithBoard && card1MakesPairWithBoard)
            {
                // Asegurarse de que los pares son con cartas diferentes del board o que las cartas de mano son diferentes
                // Esta lógica puede ser compleja. Simplificando: si ambas cartas de mano encuentran un par en el board
                // y las cartas de mano son diferentes, es two pair.
                // Si las cartas de mano son iguales, sería un Set (Trio).
                flopResult.HasTwoPair = true;
            }
            else if (card0MakesPairWithBoard || card1MakesPairWithBoard) // Solo una carta de mano hace par
            {
                int pairedHandCardForce = card0MakesPairWithBoard ? handCardForce0 : handCardForce1;
                if (pairedHandCardForce == maxBoardForce) flopResult.HasTopPair = true;
                else if (pairedHandCardForce == middleBoardForce) flopResult.HasMiddlePair = true;
                else if (pairedHandCardForce == bottomBoardForce) flopResult.HasBottomPair = true;
                // Si no es top, middle, o bottom, sigue siendo un par, pero estas flags son específicas.
            }
        }

        if (playerGameState.IsSuited)
        {
            // Necesita dos cartas más del mismo palo en turn y river.
            flopResult.HaveBackdoorFlushDraw = cardsBySuitOnBoard.Any(sbg => sbg.Key == handCardSuit0 && sbg.Count == 1);
        }

        // Esto es diferente de HasOverCards, que solo considera la carta más alta de la mano.
        // Y diferente de OverPair, que requiere un par en mano.
        // Esto es para manos tipo AK en un flop J-7-2.
        if (handCardForce0 > maxBoardForce && handCardForce1 > maxBoardForce)
        {
            flopResult.HaveHighCards = true;
        }
    }

    /// <summary>
    /// Determina la mano del héroe (mejor combinación de 5 cartas) usando las cartas de mano y el flop.
    /// También establece proyectos si no hay una mano hecha fuerte.
    /// </summary>
    private void DetermineHeroHand(
        TableScrapeFlopResult flopResult,
        IReadOnlyList<BoardData> boardCards,
        int handCardForce0, int handCardForce1,
        int handCardSuit0, int handCardSuit1)
    {
        var allFiveCards = new List<BoardData>(boardCards);
        allFiveCards.Add(new BoardData { Force = handCardForce0, Suit = handCardSuit0, Name = "Hand0" }); // Name opcional para debug
        allFiveCards.Add(new BoardData { Force = handCardForce1, Suit = handCardSuit1, Name = "Hand1" });

        var allForces = allFiveCards.Select(c => c.Force).ToList();
        var allSuits = allFiveCards.Select(c => c.Suit).ToList();

        // --- Evaluación de Manos (de más fuerte a más débil) ---
        // Esta es una evaluación simplificada para 5 cartas. Un evaluador completo de Hold'em consideraría las 7 cartas (board + mano).
        // Aquí nos centramos en la mejor mano de 5 cartas posible con el flop.

        // Check para Escalera de Color y Escalera Real
        // (Lógica compleja, omitida para brevedad, pero un evaluador completo la tendría)
        // Para simplificar, si hay Color y Escalera con las 5 cartas, lo marcamos como EscaleraColor.
        bool isFlushPossible = allSuits.GroupBy(s => s).Any(g => g.Count() >= 5); // Con 5 cartas, esto significa todas del mismo palo.
        bool isStraightPossible = CheckStraight(allForces.Distinct().OrderBy(f => f).ToList());

        if (isFlushPossible && isStraightPossible)
        {
            // Aquí se necesitaría una lógica más detallada para confirmar que las mismas 5 cartas forman la escalera y el color.
            // Por ahora, una simplificación:
            flopResult.Hand = HeroHand.EscaleraDeColor; // Podría ser EscaleraReal
            return;
        }

        var forcesGrouped = allForces.GroupBy(f => f)
                                     .Select(g => new { Force = g.Key, Count = g.Count() })
                                     .OrderByDescending(x => x.Count)
                                     .ThenByDescending(x => x.Force)
                                     .ToList();

        if (forcesGrouped.Any(g => g.Count == 4))
        {
            flopResult.Hand = HeroHand.Poker;
            return;
        }

        bool hasTrio = forcesGrouped.Any(g => g.Count == 3);
        int pairCount = forcesGrouped.Count(g => g.Count == 2);

        if (hasTrio && pairCount >= 1)
        {
            flopResult.Hand = HeroHand.Full;
            return;
        }

        if (isFlushPossible) // Ya verificado arriba, pero si no es EscaleraColor.
        {
            flopResult.Hand = HeroHand.Color;
            return;
        }

        if (isStraightPossible) // Ya verificado arriba, pero si no es EscaleraColor.
        {
            flopResult.Hand = HeroHand.Escalera;
            return;
        }

        if (hasTrio)
        {
            flopResult.Hand = HeroHand.Trio;
            return;
        }

        if (pairCount >= 2)
        {
            flopResult.Hand = HeroHand.DoblePareja;
            return;
        }

        if (pairCount == 1)
        {
            flopResult.Hand = HeroHand.Pareja;
            return;
        }

        // --- Si no hay mano hecha, comprobar proyectos ---
        // HasFlushDraw y HasStraightDraw ya se calcularon en PopulateHandProperties
        // y se refieren a proyectos de 4 cartas hacia un color/escalera.
        if (flopResult.HaveFlushDraw && flopResult.HaveStraightDraw)
        {
            flopResult.Hand = HeroHand.ProyectoEscaleraColor;
        }
        else if (flopResult.HaveFlushDraw)
        {
            flopResult.Hand = HeroHand.ProyectoColor;
        }
        else if (flopResult.HaveStraightDraw)
        {
            flopResult.Hand = HeroHand.ProyectoEscalera;
        }
        else
        {
            flopResult.Hand = HeroHand.Nada; // O CartaAlta, dependiendo de la definición de HeroHand.
        }
    }

    /// <summary>
    /// Verifica si hay un proyecto de color (4 cartas del mismo palo) entre las cartas del flop y la mano.
    /// </summary>
    private bool CheckFlushDraw(IReadOnlyList<BoardData> boardCards, int handCardSuit0, int handCardSuit1)
    {
        var allSuits = new List<int> { handCardSuit0, handCardSuit1 };
        allSuits.AddRange(boardCards.Select(card => card.Suit));

        return allSuits.GroupBy(suit => suit).Any(group => group.Count() == 4);
    }

    /// <summary>
    /// Verifica si hay un proyecto de escalera (OESD o Gutshot) entre las cartas del flop y la mano.
    /// Un proyecto de escalera necesita 4 cartas para formar una secuencia.
    /// </summary>
    private bool CheckStraightDraw(IReadOnlyList<BoardData> boardCards, int handCardForce0, int handCardForce1)
    {
        var allForces = new List<int> { handCardForce0, handCardForce1 };
        allForces.AddRange(boardCards.Select(card => card.Force));

        // Considerar el As tanto como 1 (para A-2-3-4-5) como 14.
        // Si hay un As (14), también añadimos un 1 para la evaluación de escaleras bajas.
        var forcesForStraight = new HashSet<int>(); // Usar HashSet para manejar duplicados y As bajo.
        foreach (var force in allForces)
        {
            forcesForStraight.Add(force);
            if (force == AceForce) // As
            {
                forcesForStraight.Add(1); // As bajo
            }
        }

        var distinctOrderedForces = forcesForStraight.OrderBy(f => f).ToList();

        if (distinctOrderedForces.Count < 4) return false; // No hay suficientes cartas distintas para un proyecto de 4 cartas.

        // Verificar OESD (Open-Ended Straight Draw): 4 cartas consecutivas. Ej: 5-6-7-8
        for (int i = 0; i <= distinctOrderedForces.Count - 4; i++)
        {
            if (distinctOrderedForces[i + 3] - distinctOrderedForces[i] == 3) // Ej: 8-5 = 3
            {
                // Chequear que no sea ya una escalera de 5 cartas
                if (distinctOrderedForces.Count == 4 || // Si solo hay 4 cartas distintas, es OESD
                    (distinctOrderedForces.Count > 4 && // Si hay 5 o más, asegurar que no es ya escalera
                     !(distinctOrderedForces[i + 4] - distinctOrderedForces[i] == 4 && distinctOrderedForces.Count >= 5)))
                {
                    return true; // OESD
                }
            }
        }

        // Verificar Gutshot: 4 cartas donde falta una intermedia para la escalera. Ej: 5-6-8-9 (falta el 7)
        // O una secuencia de 3 con una carta a un extremo y otra al otro. Ej: 5-7-8-9 (falta el 6)
        // O una secuencia de 2 con dos cartas a los extremos. Ej: 5-6-9-T (faltan 7 u 8)
        // Esta lógica puede ser compleja. Una forma común es verificar si 4 de 5 cartas están en un rango de 4.
        // Ejemplo: 5,6,7,9 (rango 9-5=4). 5,6,8,9 (rango 9-5=4). 5,7,8,9 (rango 9-5=4).
        for (int i = 0; i < distinctOrderedForces.Count - 3; i++) // Necesitamos al menos 4 cartas
        {
            // Si 4 cartas están dentro de un span de 4 y no son consecutivas (ya cubierto por OESD si fueran 4 consecutivas)
            // Ej: 5,6,7,9 (span 4, 4 cartas) -> Gutshot
            // Ej: 2,3,5,A(14) -> no es gutshot directo con esta lógica simple.
            // Ej: T,J,Q,A (span 4, 4 cartas) -> Gutshot (necesita K)
            if (distinctOrderedForces[i + 3] - distinctOrderedForces[i] == 4)
            {
                // Para evitar contar una escalera hecha de 5 cartas como gutshot si solo tomamos 4 de ellas.
                // Si tenemos 5,6,7,8,9, entonces 5,6,7,9 NO es un gutshot, es parte de una escalera.
                // Esta condición es suficiente para un proyecto de 4 cartas.
                return true; // Gutshot
            }
        }
        return false;
    }

    /// <summary>
    /// Verifica si una lista de 5 fuerzas de cartas forma una escalera.
    /// </summary>
    private bool CheckStraight(List<int> distinctSortedForces)
    {
        if (distinctSortedForces.Count < 5) return false;

        // Check normal
        for (int i = 0; i <= distinctSortedForces.Count - 5; i++)
        {
            if (distinctSortedForces[i + 4] - distinctSortedForces[i] == 4) return true;
        }

        // Check para escalera A-5 (Wheel: A,2,3,4,5)
        // Si As (14) está presente, y también 2,3,4,5.
        // El distinctSortedForces ya contendría 1 si el As (14) estaba presente y se añadió.
        // Entonces, solo necesitamos buscar la secuencia 1,2,3,4,5.
        if (distinctSortedForces.Contains(1) && distinctSortedForces.Contains(2) && distinctSortedForces.Contains(3) &&
            distinctSortedForces.Contains(4) && distinctSortedForces.Contains(5))
        {
            return true;
        }
        return false;
    }






    private bool ProyectoEscalera(int card1, int card2, int card3, int card4, int card5)
    {
        var listaForceCards = new List<int> { card1, card2, card3, card4, card5 };

        listaForceCards.Sort();

        int[] diferencias = new int[listaForceCards.ToArray().Length - 1];
        for (int i = 0; i < diferencias.Length; i++)
        {
            diferencias[i] = listaForceCards[i + 1] - listaForceCards[i];
        }

        if (!diferencias.Any(d => d > 2))
            return true;

        return false;
    }

    private bool ExisteEscalera(List<int> cards)
    {
        cards.Sort();

        int[] diferencias = new int[cards.ToArray().Length - 1];
        for (int i = 0; i < diferencias.Length; i++)
        {
            diferencias[i] = cards[i + 1] - cards[i];
        }

        // Verificar si todas las diferencias son iguales a 1 (correlativos)
        if (diferencias.All(d => d == 1))
            return true;

        return false;
    }

    private bool HasflushDraw(List<BoardData> boardData, int cardSuit0, int cardSuit1)
    {
        var allCards = new List<int> { cardSuit0, cardSuit1 };
        allCards.AddRange(boardData.Select(s => s.Suit));

        return allCards
            .GroupBy(g => g)
            .Any(a => a.Count() >= 4);
    }

    private bool HasStraightDraw(List<BoardData> boardData, int cardForce0, int cardForce1)
    {
        var allCards = new List<int> { cardForce0, cardForce1 };
        allCards.AddRange(boardData.Select(s => s.Force));

        var distinctRanks = allCards.Select(s => s).Distinct().OrderBy(o => o).ToList();

        for (int i = 0; i < distinctRanks.Count - 3; i++)
        {
            if (distinctRanks[i + 3] - distinctRanks[i] <= 4)
                return true;
        }

        return false;
    }
}
