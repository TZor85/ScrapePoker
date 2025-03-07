using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Aplication.UseCases;

public class OutsCalculatorUseCase : IOutsCalculatorUseCase
{
    public HandStrength CalculateAllOuts(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards)
    {
        var allKnownCards = playerHand.Concat(communityCards).ToList();
        var strength = new HandStrength();
        int unknownCardsCount = 52 - allKnownCards.Count; // Cartas restantes

        // Determinar fase del juego (Flop, Turn, River)
        int communityCardsPhase = communityCards.Count;
        int cardsToCome = communityCardsPhase switch
        {
            3 => 2, // Flop → 2 cartas por venir (Turn y River)
            4 => 1, // Turn → 1 carta por venir (River)
            _ => 0  // River o Pre-Flop (no aplica)
        };

        // 1. Proyecto de color (Flush)
        strength.FlushDraw = CalculateDraw(
            GetFlushOuts(allKnownCards),
            unknownCardsCount,
            cardsToCome
        );

        // 2. Proyecto de escalera (Straight)
        strength.StraightDraw = CalculateDraw(
            GetStraightOuts(allKnownCards),
            unknownCardsCount,
            cardsToCome
        );

        // 3. Gutshot (escalera interna)
        strength.GutshotDraw = CalculateDraw(
            GetGutshotOuts(allKnownCards),
            unknownCardsCount,
            cardsToCome);

        // 4. Sets y Full Houses
        strength.SetDraw = CalculateDraw(
            GetSetOuts(allKnownCards, playerHand),
            unknownCardsCount,
            cardsToCome);

        strength.FullHouseDraw = CalculateDraw(
            GetFullHouseOuts(allKnownCards), 
            unknownCardsCount, 
            cardsToCome);

        // 5. Overcards (cartas altas)
        strength.OvercardDraw = CalculateDraw(
            GetOvercardOuts(allKnownCards, playerHand), 
            unknownCardsCount, 
            cardsToCome);

        // 6. Dos parejas
        strength.TwoPairDraw = CalculateDraw(
            GetTwoPairOuts(allKnownCards, playerHand),
            unknownCardsCount,
            cardsToCome);

        // 7. Doble Gut
        strength.DoubleGutshotDraw = CalculateDraw(
            GetDoubleGutshotOuts(allKnownCards), 
            unknownCardsCount, 
            cardsToCome);

        // 8. Proyecto de escalera de color (Straight Flush)
        strength.StraightFlushDraw = CalculateDraw(
            GetStraightFlushOuts(allKnownCards),
            unknownCardsCount,
            cardsToCome);

        // 9. Four of a Kind
        strength.FourOfAKindDraw = CalculateDraw(
            GetFourOfAKindOuts(allKnownCards),
            unknownCardsCount,
            cardsToCome
        );

        // 10. Three of a Kind desde pareja
        strength.ThreeOfAKindDraw = CalculateDraw(
            GetThreeOfAKindOuts(allKnownCards, playerHand),
            unknownCardsCount,
            cardsToCome
        );

        // 11. One Pair desde carta alta
        strength.OnePairDraw = CalculateDraw(
            GetOnePairOuts(allKnownCards, playerHand),
            unknownCardsCount,
            cardsToCome
        );

        // 12. Mejorar carta alta
        strength.HighCardDraw = CalculateDraw(
            GetHighCardOuts(allKnownCards, playerHand),
            unknownCardsCount,
            cardsToCome
        );

        return strength;
    }

    // Método auxiliar para calcular probabilidades
    private DrawProbability CalculateDraw(List<CardDataOuts> outs, int unknownCards, int cardsToCome)
    {
        var draw = new DrawProbability { Outs = outs };

        if (cardsToCome == 0 || unknownCards == 0)
        {
            draw.Probability = 0;
            return draw;
        }

        // Usar regla del 4 y 2 (aproximación rápida)
        if (cardsToCome == 2)
            draw.Probability = outs.Count * 4;
        else if (cardsToCome == 1)
            draw.Probability = outs.Count * 2;

        // Opción: Cálculo exacto usando combinatoria
        // draw.Probability = CalculateExactProbability(outs.Count, unknownCards, cardsToCome);

        return draw;
    }

    // Método para cálculo exacto (combinatoria)
    private double CalculateExactProbability(int outs, int unknownCards, int cardsToCome)
    {
        if (cardsToCome == 1)
            return (outs / (double)unknownCards) * 100;
        else if (cardsToCome == 2)
        {
            double probNoOutsFirst = (unknownCards - outs) / (double)unknownCards;
            double probNoOutsSecond = (unknownCards - outs - 1) / (double)(unknownCards - 1);
            return (1 - (probNoOutsFirst * probNoOutsSecond)) * 100;
        }
        return 0;
    }

    // ----------------------
    // Lógica para Flush
    // ----------------------
    private List<CardDataOuts> GetFlushOuts(List<CardDataOuts> knownCards)
    {
        var flushOuts = new List<CardDataOuts>();
        var suitGroups = knownCards
            .GroupBy(c => c.Suit)
            .Where(g => g.Count() >= 4);

        foreach (var group in suitGroups)
        {
            var suit = group.Key;
            var existingRanks = group.Select(c => c.Rank).ToList();

            // Generar todas las cartas faltantes del palo
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                if (!existingRanks.Contains(rank))
                {
                    var card = new CardDataOuts { Suit = suit, Rank = rank };
                    if (!IsCardKnown(card, knownCards))
                        flushOuts.Add(card);
                }
            }
        }
        return flushOuts;
    }

    // ----------------------
    // Lógica para Escalera (Straight)
    // ----------------------
    private List<CardDataOuts> GetStraightOuts(List<CardDataOuts> knownCards)
    {
        var allRanks = knownCards.Select(c => (int)c.Rank).Distinct().ToList();
        allRanks.AddRange(new[] { 1 }); // Añadir As como 1 para detectar A-2-3-4-5
        allRanks = allRanks.Distinct().OrderBy(r => r).ToList();

        var straightOuts = new List<CardDataOuts>();
        var possibleStraights = new List<List<int>>();

        // Detectar secuencias de 4 cartas consecutivas (open-ended)
        for (int i = 0; i < allRanks.Count - 3; i++)
        {
            var sequence = allRanks.Skip(i).Take(4).ToList();
            if (sequence.Last() - sequence.First() == 3)
            {
                possibleStraights.Add(sequence);
            }
        }

        // Añadir outs para completar la escalera
        foreach (var seq in possibleStraights)
        {
            int lower = seq.First() - 1;
            int upper = seq.Last() + 1;

            // Escalera abierta por arriba/abajo
            if (upper <= 14) // 14 = As (Ace)
                straightOuts.AddRange(GenerateCardsForRank(upper, knownCards));

            if (lower >= 1)
                straightOuts.AddRange(GenerateCardsForRank(lower, knownCards));
        }

        // Detectar escalera Ace-low (A-2-3-4-5)
        if (allRanks.Contains(14) && allRanks.Contains(2) && allRanks.Contains(3) && allRanks.Contains(4))
            straightOuts.AddRange(GenerateCardsForRank(5, knownCards));

        return straightOuts.DistinctBy(c => c.Id).ToList();
    }

    // ----------------------
    // Lógica para Gutshot (escalera interna)
    // ----------------------
    private List<CardDataOuts> GetGutshotOuts(List<CardDataOuts> knownCards)
    {
        var allRanks = knownCards.Select(c => (int)c.Rank).Distinct().ToList();
        allRanks.Add(1); // As como 1
        allRanks = allRanks.Distinct().OrderBy(r => r).ToList();

        var gutshotOuts = new List<CardDataOuts>();

        for (int i = 0; i < allRanks.Count - 3; i++)
        {
            var window = allRanks.Skip(i).Take(4).ToList();
            if (window.Last() - window.First() == 4) // Hay un hueco
            {
                var missing = Enumerable.Range(window.First(), 5).Except(window).ToList();
                if (missing.Count == 1)
                    gutshotOuts.AddRange(GenerateCardsForRank(missing[0], knownCards));
            }
        }

        return gutshotOuts.DistinctBy(c => c.Id).ToList();
    }

    // ----------------------
    // Lógica para Sets
    // ----------------------
    private List<CardDataOuts> GetSetOuts(List<CardDataOuts> knownCards, List<CardDataOuts> playerHand)
    {
        var setOuts = new List<CardDataOuts>();
        var playerPairs = playerHand
            .GroupBy(c => c.Rank)
            .Where(g => g.Count() == 2)
            .Select(g => g.Key);

        foreach (var rank in playerPairs)
        {
            var remaining = Enum.GetValues(typeof(Suit))
                .Cast<Suit>()
                .Select(s => new CardDataOuts { Suit = s, Rank = rank })
                .Where(c => !IsCardKnown(c, knownCards));

            setOuts.AddRange(remaining);
        }
        return setOuts;
    }

    // ----------------------
    // Lógica para Full House
    // ----------------------
    private List<CardDataOuts> GetFullHouseOuts(List<CardDataOuts> knownCards)
    {
        var fullHouseOuts = new List<CardDataOuts>();
        var rankGroups = knownCards.GroupBy(c => c.Rank);

        var trips = rankGroups.Where(g => g.Count() == 3).ToList();
        var pairs = rankGroups.Where(g => g.Count() == 2).ToList();

        // Si hay un trío, buscar pares para convertirlo en full house
        foreach (var trip in trips)
        {
            foreach (var pair in pairs)
            {
                fullHouseOuts.AddRange(
                    GenerateCardsForRank(pair.Key, knownCards)
                );
            }
        }

        return fullHouseOuts.DistinctBy(c => c.Id).ToList();
    }

    // ----------------------
    // Lógica para Overcards
    // ----------------------
    private List<CardDataOuts> GetOvercardOuts(List<CardDataOuts> knownCards, List<CardDataOuts> playerHand)
    {
        var overcardOuts = new List<CardDataOuts>();
        var maxBoardRank = knownCards.Any() ? knownCards.Max(c => (int)c.Rank) : 0;

        foreach (var card in playerHand)
        {
            if ((int)card.Rank > maxBoardRank)
            {
                // Añadir todas las cartas restantes de este rango
                overcardOuts.AddRange(
                    GenerateCardsForRank(card.Rank, knownCards)
                );
            }
        }
        return overcardOuts.DistinctBy(c => c.Id).ToList();
    }

    // ----------------------
    // Lógica para Dos parejas
    // ----------------------
    private List<CardDataOuts> GetTwoPairOuts(List<CardDataOuts> knownCards, List<CardDataOuts> playerHand)
    {
        var twoPairOuts = new List<CardDataOuts>();
        var playerRanks = playerHand.Select(c => c.Rank).ToList();
        var boardRanks = knownCards.Except(playerHand).Select(c => c.Rank).ToList();

        // Caso 1: Jugador tiene una pareja, busca otra pareja en el board
        if (playerHand.GroupBy(c => c.Rank).Any(g => g.Count() == 2))
        {
            var pairedBoardRanks = boardRanks
                .GroupBy(r => r)
                .Where(g => g.Count() == 1) // Rangos no pareados en el board
                .Select(g => g.Key);

            foreach (var rank in pairedBoardRanks)
            {
                twoPairOuts.AddRange(GenerateCardsForRank(rank, knownCards));
            }
        }
        // Caso 2: Jugador no tiene pareja, busca dos pares posibles
        else
        {
            var allPossiblePairs = playerRanks
                .Concat(boardRanks)
                .GroupBy(r => r)
                .Where(g => g.Count() == 1)
                .Select(g => g.Key);

            foreach (var rank in allPossiblePairs)
            {
                twoPairOuts.AddRange(GenerateCardsForRank(rank, knownCards));
            }
        }

        return twoPairOuts.DistinctBy(c => c.Id).ToList();
    }

    // ----------------------
    // Lógica para Doble Gutshot
    // ----------------------
    private List<CardDataOuts> GetDoubleGutshotOuts(List<CardDataOuts> knownCards)
    {
        var allRanks = knownCards.Select(c => (int)c.Rank).Distinct().ToList();
        allRanks.Add(1); // As como 1
        allRanks = allRanks.OrderBy(r => r).ToList();

        var gutshots = new List<int>();
        for (int i = 0; i < allRanks.Count - 3; i++)
        {
            var window = allRanks.Skip(i).Take(4).ToList();
            if (window.Last() - window.First() == 4) // Hueco de 1 carta
            {
                var missing = Enumerable.Range(window.First(), 5).Except(window).ToList();
                if (missing.Count == 1)
                    gutshots.Add(missing[0]);
            }
        }

        // Si hay al menos 2 gutshots diferentes
        var doubleGutshotOuts = gutshots
            .Distinct()
            .Take(2)
            .SelectMany(r => GenerateCardsForRank(r, knownCards))
            .ToList();

        return doubleGutshotOuts;
    }

    // ----------------------
    // Lógica para Straight Flush
    // ----------------------
    private List<CardDataOuts> GetStraightFlushOuts(List<CardDataOuts> knownCards)
    {
        var straightFlushOuts = new List<CardDataOuts>();
        var flushOuts = GetFlushOuts(knownCards);

        foreach (var card in flushOuts)
        {
            // Simular añadir esta carta al board y ver si forma una escalera
            var simulatedCards = knownCards.Concat(new[] { card }).ToList();
            var straightOuts = GetStraightOuts(simulatedCards);

            if (straightOuts.Any(c => c.Suit == card.Suit))
                straightFlushOuts.Add(card);
        }

        return straightFlushOuts;
    }

    // ----------------------
    // Lógica para Four of a Kind (poker)
    // ----------------------
    private List<CardDataOuts> GetFourOfAKindOuts(List<CardDataOuts> knownCards)
    {
        var fourOfAKindOuts = new List<CardDataOuts>();
        var trioGroups = knownCards
            .GroupBy(c => c.Rank)
            .Where(g => g.Count() == 3); // Buscar tríos en las cartas conocidas

        foreach (var group in trioGroups)
        {
            Rank targetRank = group.Key;
            // La cuarta carta del mismo valor que el trío
            fourOfAKindOuts.AddRange(
                GenerateCardsForRank(targetRank, knownCards)
            );
        }
        return fourOfAKindOuts;
    }


    // ----------------------
    // Lógica para Three of a Kind (Trío)
    // ----------------------
    private List<CardDataOuts> GetThreeOfAKindOuts(List<CardDataOuts> knownCards, List<CardDataOuts> playerHand)
    {
        var threeOfAKindOuts = new List<CardDataOuts>();
        var playerPairs = playerHand
            .GroupBy(c => c.Rank)
            .Where(g => g.Count() == 1) // Cartas únicas en la mano
            .Select(g => g.Key);

        foreach (var rank in playerPairs)
        {
            // Si hay una carta de este rango en el board, puede formar una pareja
            if (knownCards.Any(c => c.Rank == rank))
            {
                threeOfAKindOuts.AddRange(
                    GenerateCardsForRank(rank, knownCards)
                );
            }
        }
        return threeOfAKindOuts;
    }

    // ----------------------
    // Lógica para una pareja
    // ----------------------
    private List<CardDataOuts> GetOnePairOuts(List<CardDataOuts> knownCards, List<CardDataOuts> playerHand)
    {
        var onePairOuts = new List<CardDataOuts>();
        var playerHighCards = playerHand
            .Where(c => !knownCards.Any(kc => kc.Rank == c.Rank)) // Cartas no pareadas en el board
            .Select(c => c.Rank);

        foreach (var rank in playerHighCards)
        {
            onePairOuts.AddRange(
                GenerateCardsForRank(rank, knownCards)
            );
        }
        return onePairOuts;
    }

    // ----------------------
    // Lógica para mejorar high card
    // ----------------------
    private List<CardDataOuts> GetHighCardOuts(List<CardDataOuts> knownCards, List<CardDataOuts> playerHand)
    {
        var highCardOuts = new List<CardDataOuts>();
        var boardMaxRank = knownCards.Any() ? knownCards.Max(c => c.Rank) : Rank.Two;
        var playerHighRanks = playerHand
            .Select(c => c.Rank)
            .Where(r => r > boardMaxRank);

        foreach (var rank in playerHighRanks)
        {
            highCardOuts.AddRange(
                GenerateCardsForRank(rank, knownCards)
            );
        }
        return highCardOuts;
    }
       

    // ----------------------
    // Métodos Auxiliares
    // ----------------------
    private List<CardDataOuts> GenerateCardsForRank(Rank rank, List<CardDataOuts> knownCards)
    {
        return Enum.GetValues(typeof(Suit))
            .Cast<Suit>()
            .Select(s => new CardDataOuts { Suit = s, Rank = rank })
            .Where(c => !IsCardKnown(c, knownCards))
            .ToList();
    }

    private List<CardDataOuts> GenerateCardsForRank(int rankValue, List<CardDataOuts> knownCards)
    {
        var rank = (Rank)rankValue;
        return GenerateCardsForRank(rank, knownCards);
    }


    private bool IsCardKnown(CardDataOuts card, List<CardDataOuts> knownCards)
    {
        return knownCards.Any(c => c.Suit == card.Suit && c.Rank == card.Rank);
    }
}
