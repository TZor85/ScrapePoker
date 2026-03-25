using OpenScrape.DecisionMaker.Algorithms;
using OpenScrape.Domain.Enums;

namespace OpenScrape.DecisionMaker.Services;

/// <summary>
/// Contexto de estado cross-street para decisiones postflop.
/// Mantiene el estado que persiste entre flop → turn → river dentro de una mano.
/// </summary>
public class PostflopGameContext
{
    // === Estado cross-street: quién apostó en qué calle ===
    public bool VillainBetFlop { get; set; }
    public bool VillainBetTurn { get; set; }
    public bool HeroBetFlop { get; set; }
    public bool HeroBetTurn { get; set; }
    public bool PreviousStreetWasBet { get; set; }

    // === Tamaño de apuesta del villano por street (sizing tells) ===
    public BetSizeCategory VillainBetSizeFlop { get; set; } = BetSizeCategory.NoBet;
    public BetSizeCategory VillainBetSizeTurn { get; set; } = BetSizeCategory.NoBet;

    /// <summary>
    /// El agresor preflop checkeó en el flop → señal de debilidad para probe bet.
    /// </summary>
    public bool VillainAggressorCheckedFlop { get; set; }

    /// <summary>
    /// El villano apostó en flop pero checkeó en turn → patrón bet-check-bet si apuesta en river.
    /// Indica debilidad (draw fallido que reintenta) vs barrel real (rango fuerte).
    /// </summary>
    public bool VillainCheckedMiddleStreet { get; set; }

    /// <summary>
    /// Hero floateó en flop (call con aire + posición) → en turn debe apostar si villano chequea.
    /// </summary>
    public bool HeroFloatedFlop { get; set; }

    /// <summary>
    /// Hero calleó turn con flush draw peligroso en board (3+ same suit).
    /// Si river completa el flush → check automático.
    /// </summary>
    public bool TurnCalledWithFlushDanger { get; set; }

    /// <summary>
    /// Estado base de peligro del flop (flush draw presence, paired, connected).
    /// Se combina con boardChange del turn via CombineBoardChanges().
    /// </summary>
    public BoardChangeResult InitialBoardDanger { get; set; } = BoardChangeResult.Safe;

    /// <summary>
    /// Resultado del análisis de cambio de board (peligro de turn/river card).
    /// Se propaga entre streets para acumular peligro.
    /// </summary>
    public BoardChangeResult LastBoardChange { get; set; } = BoardChangeResult.Safe;

    /// <summary>
    /// Detecta si el villano está barreling (apostó en 2+ calles consecutivas).
    /// </summary>
    public bool IsVillainBarreling => VillainBetFlop && VillainBetTurn;

    /// <summary>
    /// Reinicia el contexto para una nueva mano.
    /// </summary>
    public void Reset()
    {
        VillainBetFlop = false;
        VillainBetTurn = false;
        HeroBetFlop = false;
        HeroBetTurn = false;
        PreviousStreetWasBet = false;
        VillainAggressorCheckedFlop = false;
        VillainCheckedMiddleStreet = false;
        HeroFloatedFlop = false;
        TurnCalledWithFlushDanger = false;
        VillainBetSizeFlop = BetSizeCategory.NoBet;
        VillainBetSizeTurn = BetSizeCategory.NoBet;
        InitialBoardDanger = BoardChangeResult.Safe;
        LastBoardChange = BoardChangeResult.Safe;
    }

    /// <summary>
    /// Actualiza el estado al finalizar una calle del flop.
    /// </summary>
    public void UpdateFlopState(bool heroBet, bool villainBet, bool isPreflopAggressor)
    {
        HeroBetFlop = heroBet;
        VillainBetFlop = villainBet;
        PreviousStreetWasBet = heroBet;
        VillainAggressorCheckedFlop = !isPreflopAggressor && !villainBet;
    }

    /// <summary>
    /// Actualiza el estado al finalizar el turn.
    /// </summary>
    public void UpdateTurnState(bool heroBet, bool villainBet)
    {
        // Detectar patrón bet-check: villain apostó en flop pero no en turn
        VillainCheckedMiddleStreet = VillainBetFlop && !villainBet;
        HeroBetTurn = heroBet;
        VillainBetTurn = villainBet;
        PreviousStreetWasBet = heroBet;
    }

    /// <summary>
    /// Combina el peligro del turn con el nuevo peligro del river.
    /// </summary>
    public static BoardChangeResult CombineBoardChanges(
        BoardChangeResult previous, BoardChangeResult current)
    {
        if (previous.DangerLevel == 0)
            return current;
        if (current.DangerLevel == 0)
            return previous;

        return new BoardChangeResult(
            FlushCompleted: previous.FlushCompleted || current.FlushCompleted,
            FlushDrawAppeared: previous.FlushDrawAppeared || current.FlushDrawAppeared,
            StraightCompleted: previous.StraightCompleted || current.StraightCompleted,
            BoardPaired: previous.BoardPaired || current.BoardPaired,
            OvercardAppeared: previous.OvercardAppeared || current.OvercardAppeared,
            CompletedFlushSuit: current.CompletedFlushSuit >= 0
                ? current.CompletedFlushSuit
                : previous.CompletedFlushSuit,
            DangerLevel: Math.Min(10, previous.DangerLevel + current.DangerLevel));
    }
}
