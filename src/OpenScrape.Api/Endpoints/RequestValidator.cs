using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.Api.Endpoints;

/// <summary>
/// Rechaza pedidos que el motor no puede evaluar aunque el JSON sea válido:
/// board incompleto para la calle y cartas repetidas.
/// </summary>
public static class RequestValidator
{
    public static string? Validate(DecisionRequest request)
    {
        var expectedBoard = request.Street switch
        {
            DecisionStreet.Preflop => 0,
            DecisionStreet.Flop => 3,
            DecisionStreet.Turn => 4,
            DecisionStreet.River => 5,
            _ => -1
        };

        if (expectedBoard < 0)
            return $"La calle '{request.Street}' no es válida.";

        if (request.Board.Count != expectedBoard)
            return $"La calle {request.Street} espera {expectedBoard} cartas de board y llegaron {request.Board.Count}.";

        // Postflop los thresholds se eligen por situación preflop. Sin ella el motor
        // no puede decidir: mejor un 422 explícito que un 500 por clave inválida.
        if (request.Street != DecisionStreet.Preflop && request.HandSituation == HandSituation.None)
            return $"La calle {request.Street} requiere una situación preflop válida (HandSituation).";

        if (request.PotSize < 0)
            return "El pozo no puede ser negativo.";

        if (request.HeroStack < 0)
            return "El stack del héroe no puede ser negativo.";

        var cards = new List<CardRef>(request.Board) { request.HeroCard1, request.HeroCard2 };
        var distinct = cards.Select(card => (card.Rank, card.Suit)).Distinct().Count();
        if (distinct != cards.Count)
            return "Hay cartas repetidas entre la mano y el board.";

        return null;
    }
}
