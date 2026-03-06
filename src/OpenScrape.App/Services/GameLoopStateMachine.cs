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

    public void Reset()
    {
        var previousState = CurrentState;
        CurrentState = GameState.WaitingForHand;

        _logger.LogInformation(
            "Estado reseteado: {PreviousState} -> WaitingForHand",
            previousState);
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
}
