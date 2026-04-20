## ADDED Requirements

### Requirement: GameLoopCoordinator orquesta el ciclo captura-decisión sin dependencias UI

El sistema SHALL extraer la orquestación del loop de captura y procesado de `FrmMain` a un servicio `GameLoopCoordinator : IGameLoopCoordinator` (scoped) que NO dependa de `Control`, `Form` ni `PictureBox`. El coordinator MUST exponer ciclo de vida mediante `StartAsync(CancellationToken)` / `StopAsync()` y emitir resultados por eventos o `IObservable<GameLoopResult>`.

#### Scenario: Arranque y parada limpios

- **WHEN** se llama `StartAsync` con un `CancellationToken` válido
- **THEN** el coordinator inicia un `PeriodicTimer` que dispara una iteración de captura cada `CaptureIntervalMs` configurable
- **AND** al llamar `StopAsync` el timer se detiene y el método espera a que la iteración en curso finalice antes de retornar
- **AND** no queda ningún thread activo tras `StopAsync`

#### Scenario: Cancelación cooperativa durante una iteración

- **WHEN** se cancela el `CancellationToken` mientras una iteración está procesando OCR
- **THEN** la iteración actual aborta en el siguiente `ThrowIfCancellationRequested` sin lanzar excepción no capturada
- **AND** `StartAsync` retorna normalmente
- **AND** no se emite `GameLoopResult` parcial

### Requirement: GameLoopCoordinator publica resultados por evento tipado

El coordinator SHALL exponer un evento `event EventHandler<GameLoopResult> ResultReady` (o equivalente `IObservable<GameLoopResult>`). Cada iteración exitosa MUST publicar exactamente un `GameLoopResult`. Iteraciones con error MUST publicar un `GameLoopResult` con `Error` poblado en lugar de lanzar excepción al thread del timer.

#### Scenario: Iteración exitosa emite resultado

- **WHEN** el coordinator completa una iteración con screenshot válido y decisión calculada
- **THEN** se emite un `GameLoopResult` con `RecommendedAction`, `DecisionResult`, `CalculationResult` y `HandCompleted` poblados
- **AND** los suscriptores reciben el evento en el thread del pool (no el thread UI)

#### Scenario: Error de OCR no derriba el loop

- **WHEN** el `IScreenReaderService` lanza una excepción durante una iteración
- **THEN** el coordinator captura la excepción, emite `GameLoopResult { Error = ... }` y continúa con la siguiente iteración
- **AND** no se propaga la excepción al `PeriodicTimer`

### Requirement: GameLoopCoordinator gestiona transiciones de estado vía GameLoopStateMachine

El coordinator SHALL consultar `IGameLoopStateMachine.CurrentState` antes de cada iteración y delegar en el método de procesado apropiado (`ProcessPreflopAsync`, `ProcessFlopAsync`, etc.). La transición entre estados MUST pasar por `TryTransition(state, visibleBoardCards)` con validación del número de cartas visibles.

#### Scenario: Transición flop → turn cuando aparece la cuarta carta

- **GIVEN** el estado es `FlopAction` y el screenshot contiene 4 cartas de board visibles
- **WHEN** el coordinator llama a la capa de detección y `visibleBoardCards == 4`
- **THEN** se llama `TryTransition(TurnDetected, 4)` y retorna `true`
- **AND** la siguiente iteración procesa turn

#### Scenario: Rechazo de transición con cartas insuficientes

- **GIVEN** el estado es `FlopAction`
- **WHEN** el detector reporta `visibleBoardCards == 3` (no hay cuarta carta)
- **THEN** no se invoca `TryTransition(TurnDetected, ...)`
- **AND** la iteración re-procesa el flop con posible actualización de bet info

### Requirement: GameLoopCoordinator reemplaza volatile flags por CancellationToken

El coordinator MUST NO utilizar `volatile bool` para señalizar parada del loop. El control de ejecución SHALL basarse exclusivamente en `CancellationTokenSource` + `PeriodicTimer`. Los campos `_executeCapture` y `_backgroundExecute` de `FrmMain` deben ser eliminados tras la migración.

#### Scenario: Botón "Parar captura" cancela el token

- **WHEN** el usuario pulsa el botón Parar en la UI
- **THEN** `FrmMain` invoca `_coordinator.StopAsync()` que internamente cancela el `CancellationTokenSource`
- **AND** el `PeriodicTimer` se dispone y el loop termina

### Requirement: GameLoopCoordinator es testeable sin instanciar WinForms

El coordinator SHALL aceptar en su constructor únicamente servicios mockeables (interfaces `IScreenReaderService`, `ITableLayoutService`, `IGameLoopStateMachine`, `IPokerDecisionFacade`, `IGameLoggerService`, `PostflopGameContext`, `IOptions<StrategyProfile>`). MUST NO aceptar `Form`, `Control`, `PictureBox` ni referencias a ensamblados WinForms.

#### Scenario: Tests de integración sin UI

- **GIVEN** un test de NUnit que instancia `GameLoopCoordinator` con fakes de `IScreenReaderService` e `ITableLayoutService`
- **WHEN** se invoca `StartAsync` con un token que se cancela tras 3 iteraciones
- **THEN** el test recibe exactamente 3 `GameLoopResult` sin arrancar ningún `Form`
- **AND** el test finaliza en menos de 500ms
