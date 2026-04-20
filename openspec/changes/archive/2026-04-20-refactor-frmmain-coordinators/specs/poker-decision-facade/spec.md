## ADDED Requirements

### Requirement: PokerDecisionFacade agrega cálculo de equity y decisión postflop en una sola superficie

El sistema SHALL proveer un servicio `PokerDecisionFacade : IPokerDecisionFacade` (scoped) que exponga un único método `EvaluateAsync(DecisionRequest) : Task<DecisionResult>` agregando internamente: cálculo de equity (`IPokerCalculator`), decisión postflop (`IPostflopDecisionService`), dimensionamiento de apuesta (`IBetSizingService`), textura de board (`IBoardTextureAnalyzer`) y perfil de oponente (`IOpponentTracker`). Los consumidores —principalmente `GameLoopCoordinator`— MUST NO inyectar los 5 servicios por separado.

#### Scenario: Evaluar mano postflop completa

- **GIVEN** un `DecisionRequest` con street=Flop, cartas hero, board de 3 cartas, stack, bote, posición y bet del villain
- **WHEN** se llama `EvaluateAsync(request)`
- **THEN** el facade calcula equity con `IPokerCalculator`, deriva textura con `IBoardTextureAnalyzer`, consulta perfil de oponente con `IOpponentTracker`, obtiene la decisión con `IPostflopDecisionService` y el sizing con `IBetSizingService`
- **AND** retorna un `DecisionResult` con `RecommendedAction`, `EquityPercent`, `Reason`, `BoardTexture`, `BetSize` poblados

### Requirement: DecisionRequest y DecisionResult son records inmutables

La entrada y salida del facade SHALL ser records inmutables (`DecisionRequest`, `DecisionResult`) con todos los campos necesarios para reproducir la decisión en backtest sin acceso al estado global. `DecisionResult.Reason` MUST ser no-null y describir el path de decisión tomado.

#### Scenario: Reproducibilidad en backtest

- **GIVEN** un `DecisionRequest` serializable almacenado en histórico
- **WHEN** se re-invoca `EvaluateAsync` con ese request
- **THEN** el `DecisionResult` retornado es equivalente al original (misma `RecommendedAction` y `EquityPercent` dentro de ±1% por varianza MC)
- **AND** `DecisionResult.Reason` contiene el path (`"FacingBet → Raise (TwoPair+)"`)

### Requirement: PokerDecisionFacade no contiene lógica de decisión propia

El facade SHALL delegar en los servicios subyacentes sin implementar heurísticas de poker adicionales. Su responsabilidad es únicamente orquestación y agregación. Cualquier lógica nueva (p.ej. penalizaciones por textura) MUST añadirse en el servicio correspondiente, no en el facade.

#### Scenario: Nueva regla de poker añadida en PostflopDecisionService

- **GIVEN** se añade una nueva regla de check-raise en `PostflopDecisionService`
- **WHEN** el facade ejecuta `EvaluateAsync`
- **THEN** la nueva regla aplica automáticamente sin modificar `PokerDecisionFacade`
- **AND** el diff del PR que añade la regla NO toca `PokerDecisionFacade.cs`

### Requirement: PokerDecisionFacade soporta cancelación

`EvaluateAsync` SHALL aceptar un `CancellationToken` y propagarlo a las operaciones asíncronas internas (notablemente el cálculo Monte Carlo). Si el token se cancela durante la simulación, el método MUST lanzar `OperationCanceledException` en lugar de retornar un resultado parcial.

#### Scenario: Cancelación durante Monte Carlo

- **GIVEN** una invocación a `EvaluateAsync` con un token que se cancela a los 50ms
- **WHEN** el cálculo MC de 50K iteraciones está en curso
- **THEN** la operación aborta y lanza `OperationCanceledException`
- **AND** no se retorna `DecisionResult` parcial

### Requirement: PokerDecisionFacade emite telemetría de latencia por fase

El facade SHALL medir el tiempo de cada fase (equity, textura, decisión, sizing) y exponer los valores en `DecisionResult.PhaseTimings` (diccionario `string → TimeSpan`). Esto habilita debugging de regresiones de rendimiento sin instrumentación manual en cada servicio.

#### Scenario: Latencia de fases expuesta

- **WHEN** se completa `EvaluateAsync`
- **THEN** `DecisionResult.PhaseTimings` contiene al menos las claves `"equity"`, `"texture"`, `"decision"`, `"sizing"`
- **AND** cada valor es un `TimeSpan` positivo
