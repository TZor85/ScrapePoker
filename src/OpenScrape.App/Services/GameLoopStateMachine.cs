using Microsoft.Extensions.Logging;

namespace OpenScrape.App.Services;

public enum GameState
{
    WaitingForHand,
    HandDetected,
    PreflopAction,
    FlopDetected,
    FlopAction,
    TurnDetected,
    TurnAction,
    RiverDetected,
    RiverAction,
    HandComplete
}

public class GameLoopStateMachine
{
    private readonly ILogger<GameLoopStateMachine> _logger;
    private readonly object _stateLock = new();

    private static readonly Dictionary<GameState, HashSet<GameState>> _validTransitions = new()
    {
        [GameState.WaitingForHand] = new() { GameState.HandDetected },
        [GameState.HandDetected] = new() { GameState.PreflopAction, GameState.WaitingForHand },
        [GameState.PreflopAction] = new() { GameState.FlopDetected, GameState.HandComplete, GameState.WaitingForHand },
        [GameState.FlopDetected] = new() { GameState.FlopAction, GameState.WaitingForHand },
        [GameState.FlopAction] = new() { GameState.TurnDetected, GameState.HandComplete, GameState.WaitingForHand },
        [GameState.TurnDetected] = new() { GameState.TurnAction, GameState.WaitingForHand },
        [GameState.TurnAction] = new() { GameState.RiverDetected, GameState.HandComplete, GameState.WaitingForHand },
        [GameState.RiverDetected] = new() { GameState.RiverAction, GameState.WaitingForHand },
        [GameState.RiverAction] = new() { GameState.HandComplete, GameState.WaitingForHand },
        [GameState.HandComplete] = new() { GameState.WaitingForHand }
    };

    public GameState CurrentState { get; private set; } = GameState.WaitingForHand;

    public GameLoopStateMachine(ILogger<GameLoopStateMachine> logger)
    {
        _logger = logger;
    }

    public bool TryTransition(GameState newState)
    {
        lock (_stateLock)
        {
            if (!_validTransitions.TryGetValue(CurrentState, out var validTargets) ||
                !validTargets.Contains(newState))
            {
                _logger.LogWarning(
                    "Transición inválida: {CurrentState} -> {NewState}",
                    CurrentState, newState);
                return false;
            }

            var previousState = CurrentState;
            CurrentState = newState;

            _logger.LogInformation(
                "Transición de estado: {PreviousState} -> {NewState}",
                previousState, newState);

            return true;
        }
    }

    /// <summary>
    /// Transición con validación de board cards visibles.
    /// Previene decidir en street equivocada (ej: FlopDetected con 4 cartas visibles).
    /// </summary>
    public bool TryTransition(GameState newState, int visibleBoardCards)
    {
        // Validar coherencia entre estado destino y cartas visibles
        int expectedMinCards = newState switch
        {
            GameState.FlopDetected or GameState.FlopAction => 3,
            GameState.TurnDetected or GameState.TurnAction => 4,
            GameState.RiverDetected or GameState.RiverAction => 5,
            _ => 0 // WaitingForHand, HandDetected, PreflopAction, HandComplete: no requiere cartas
        };

        if (visibleBoardCards > 0 && expectedMinCards > 0 && visibleBoardCards < expectedMinCards)
        {
            _logger.LogWarning(
                "Transición bloqueada por board cards: {CurrentState} -> {NewState}, " +
                "cartas visibles={VisibleCards}, mínimo requerido={ExpectedMin}",
                CurrentState, newState, visibleBoardCards, expectedMinCards);
            return false;
        }

        // Warn si hay más cartas de las esperadas (posible desfase de state)
        int expectedMaxCards = newState switch
        {
            GameState.FlopDetected or GameState.FlopAction => 3,
            GameState.TurnDetected or GameState.TurnAction => 4,
            GameState.RiverDetected or GameState.RiverAction => 5,
            _ => 99
        };

        if (visibleBoardCards > expectedMaxCards)
        {
            _logger.LogWarning(
                "Posible desfase de street: {CurrentState} -> {NewState}, " +
                "cartas visibles={VisibleCards} > esperado={ExpectedMax}",
                CurrentState, newState, visibleBoardCards, expectedMaxCards);
        }

        return TryTransition(newState);
    }

    public void Reset()
    {
        lock (_stateLock)
        {
            var previousState = CurrentState;
            CurrentState = GameState.WaitingForHand;

            _logger.LogInformation(
                "Estado reseteado: {PreviousState} -> WaitingForHand",
                previousState);
        }
    }

    public bool IsInStreet(GameState streetDetected, GameState streetAction)
    {
        return CurrentState == streetDetected || CurrentState == streetAction;
    }

    public bool IsPreflop => CurrentState is GameState.HandDetected or GameState.PreflopAction;
    public bool IsFlop => CurrentState is GameState.FlopDetected or GameState.FlopAction;
    public bool IsTurn => CurrentState is GameState.TurnDetected or GameState.TurnAction;
    public bool IsRiver => CurrentState is GameState.RiverDetected or GameState.RiverAction;
    public bool IsWaiting => CurrentState == GameState.WaitingForHand;
    public bool IsHandComplete => CurrentState == GameState.HandComplete;

    /// <summary>
    /// Fuerza el estado directamente (solo para modo test/debug o restauración postflop).
    /// Valida que el estado destino es un GameState válido (no arbitrario).
    /// </summary>
    public void ForceState(GameState state)
    {
        lock (_stateLock)
        {
            if (!_validTransitions.ContainsKey(state))
            {
                _logger.LogError(
                    "ForceState rechazado: estado inválido {State}", state);
                return;
            }

            var previousState = CurrentState;
            CurrentState = state;

            _logger.LogWarning(
                "Estado forzado: {PreviousState} -> {NewState}",
                previousState, state);
        }
    }

    /// <summary>
    /// Número máximo de reintentos para OCR fallido.
    /// </summary>
    public const int MaxOcrRetries = 2;
}
