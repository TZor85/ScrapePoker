# Spec: GameCoordinator

## Requisito

El sistema DEBE extraer la orquestación del game loop de FrmMain a un servicio `GameCoordinator : IGameCoordinator` que coordine el flujo completo de una mano de poker sin depender de controles UI.

## Conceptos

- **GameCoordinator**: Servicio Scoped que recibe screenshot + estado de jugadores y devuelve `GameLoopResult` con la acción recomendada y datos para actualizar la UI.
- **GameLoopResult**: Record inmutable con el resultado de procesar un estado del game loop.
- **Delegación pura**: FrmMain llama a GameCoordinator y usa el resultado para actualizar UI. No hay lógica de poker en FrmMain.

## Interfaz Pública

```csharp
public interface IGameCoordinator
{
    Task<GameLoopResult> ProcessCurrentStateAsync(Bitmap screenshot, PlayerGameState[] players);
    Task<GameLoopResult> ProcessPreflopAsync(Bitmap screenshot, PlayerGameState[] players);
    Task<GameLoopResult> ProcessFlopAsync(Bitmap screenshot, PlayerGameState[] players);
    Task<GameLoopResult> ProcessTurnAsync(Bitmap screenshot, PlayerGameState[] players);
    Task<GameLoopResult> ProcessRiverAsync(Bitmap screenshot, PlayerGameState[] players);
    Task HandleNewHandAsync(PlayerGameState[] players);
    void ResetState();
}

public record GameLoopResult
{
    public string? RecommendedAction { get; init; }
    public PostflopDecisionResult? DecisionResult { get; init; }
    public PokerCalculationResult? CalculationResult { get; init; }
    public string? LogText { get; init; }
    public bool HandCompleted { get; init; }
    public bool NewHandDetected { get; init; }
    public Card[]? DetectedCards { get; init; }
    public string? BoardTexture { get; init; }
}
```

## Dependencias del Constructor

```csharp
public GameCoordinator(
    IGameLoopStateMachine stateMachine,
    IPokerCalculator calculator,
    IPostflopDecisionService decisionService,
    IScreenReaderService screenReader,
    ITableLayoutService tableLayout,
    IBetSizingService betSizing,
    IOpponentTracker opponentTracker,
    IBoardTextureAnalyzer boardTextureAnalyzer,
    GameLoggerService logger,
    PostflopGameContext context,
    IOptions<StrategyProfile> strategyProfile,
    CardCacheService cardCache)
```

## Escenarios

### Escenario 1: Procesar flop con equity alta

DADO que el GameLoopStateMachine está en FlopDetected
Y el screenshot contiene 3 cartas de board visibles
Y la equity calculada es 72%
CUANDO se llama ProcessFlopAsync
ENTONCES el resultado contiene RecommendedAction = "Bet"
Y DecisionResult.Action != Fold
Y LogText contiene "FLOP"
Y el estado transiciona a FlopAction

### Escenario 2: Procesar river con equity baja

DADO que el estado es RiverDetected
Y la equity es 15%
Y hay una apuesta del villain
CUANDO se llama ProcessRiverAsync
ENTONCES el resultado contiene RecommendedAction que incluye "Fold"
Y DecisionResult.IsFold == true

### Escenario 3: Detectar nueva mano

DADO que el estado es HandComplete o WaitingForHand
Y el hand number en el screenshot cambió
CUANDO se llama ProcessCurrentStateAsync
ENTONCES NewHandDetected == true
Y se invoca HandleNewHandAsync internamente
Y PostflopGameContext se resetea

### Escenario 4: Transición turn sin carta visible

DADO que el estado es FlopAction
Y la carta 4 del board NO es visible
CUANDO se llama ProcessCurrentStateAsync
ENTONCES el estado permanece en FlopAction
Y se re-procesa el flop con bet info actualizada (reprocess scenario)

### Escenario 5: ResetState limpia todo el contexto

DADO que hay una mano activa con PostflopGameContext poblado
CUANDO se llama ResetState
ENTONCES PostflopGameContext.Reset() es invocado
Y GameLoopStateMachine.Reset() es invocado

### Escenario 6: GameCoordinator no accede a controles UI

DADO cualquier estado del game loop
CUANDO se procesa cualquier calle
ENTONCES GameCoordinator NO invoca Invoke/BeginInvoke
Y NO accede a ningún Control de WinForms
Y toda la información UI va en GameLoopResult
