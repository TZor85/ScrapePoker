## 1. Preparación e inventario

- [x] 1.1 Añadir sección `"GameLoop": { "CaptureIntervalMs": 100 }` y `"Features": { "UseGameLoopCoordinator": false }` en `appsettings.json`
- [x] 1.2 Añadir clase `GameLoopOptions` en `OpenScrape.App/Configuration/` con `CaptureIntervalMs` (default 100) y `StopTimeoutMs` (default 2000)
- [x] 1.3 Añadir clase `FeatureFlags` con propiedad `UseGameLoopCoordinator` y registrarla con `IOptions<FeatureFlags>`
- [x] 1.4 Inventariar en comentario temporal al final de `FrmMain.cs` todos los campos mutables relacionados con el game loop (lista explícita: `_executeCapture`, `_backgroundExecute`, `_heroStackPreRebuy`, contadores de reintentos OCR, flags debug) y a qué servicio migra cada uno
- [x] 1.5 Verificar que `dotnet build OpenScrape.sln` y `dotnet test OpenScrape.sln` siguen verdes

## 2. PokerDecisionFacade

- [x] 2.1 Crear `src/OpenScrape.DecisionMaker/DTOs/DecisionRequest.cs` (record) con todos los campos necesarios para reproducir una decisión postflop
- [x] 2.2 Crear `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs` (record) con `RecommendedAction`, `EquityPercent`, `Reason` (non-null), `BoardTexture`, `BetSize`, `PhaseTimings` (Dictionary<string, TimeSpan>)
- [x] 2.3 Crear `IPokerDecisionFacade.cs` con `Task<DecisionResult> EvaluateAsync(DecisionRequest, CancellationToken)` — **ubicación final: `src/OpenScrape.App/Services/`** (desvío de design.md: `IPokerCalculator` vive en App, y `OpenScrape.DecisionMaker` no puede referenciar `OpenScrape.App`)
- [x] 2.4 Implementar `PokerDecisionFacade.cs` delegando en `IPokerCalculator`, `IPostflopDecisionService`, `IBetSizingService`, `IBoardTextureAnalyzer`, `IOpponentTracker`; medir timings con `Stopwatch` (ubicación: `src/OpenScrape.App/Services/`)
- [x] 2.5 Registrar `AddScoped<IPokerDecisionFacade, PokerDecisionFacade>()` en `Program.cs`
- [x] 2.6 Añadir tests unitarios `OpenScrape.App.Tests/PokerDecisionFacadeTests.cs`: evaluación flop/turn/river, propagación de `Reason`, `PhaseTimings` poblado, cancelación con `OperationCanceledException`, perfil explícito vs lookup, extracción de bet size
- [x] 2.7 Verificar build + tests verdes (827/827 verdes)

## 3. GameLoopCoordinator — esqueleto sin lógica

- [x] 3.1 Crear `src/OpenScrape.App/Services/IGameLoopCoordinator.cs` con `event EventHandler<GameLoopResult> ResultReady`, `bool IsRunning`, `Task StartAsync(CancellationToken)`, `Task StopAsync()`, `IAsyncDisposable`. También creado `GameLoopResult` (record inmutable con `Empty`, `Error`, `Street`, `RecommendedAction`, `DecisionResult`, `LogText`, `HandCompleted`, `NewHandDetected`, `Timestamp`)
- [x] 3.2 Crear `src/OpenScrape.App/Services/GameLoopCoordinator.cs`. **Desvío del design**: el esqueleto acepta solo `ILogger<GameLoopCoordinator>` + `IOptions<GameLoopOptions>`. Las demás dependencias (`IScreenReaderService`, `ITableLayoutService`, `GameLoopStateMachine`, `IPokerDecisionFacade`, `GameLoggerService`, `PostflopGameContext`, `IOptions<StrategyProfile>`, `CardCacheService`, `IGameCoordinator`) se añaden en Fase 4 cuando se migra la lógica real. Racional: permitir tests de lifecycle aislados sin arrastrar un ctor de 10+ dependencias que nunca se invocan en el esqueleto
- [x] 3.3 Implementar `StartAsync` con `PeriodicTimer` que dispara `TickAsync` cada `CaptureIntervalMs`; guard para `IsRunning == true` (idempotente); `SemaphoreSlim` protege la transición start/stop
- [x] 3.4 Implementar `StopAsync` que cancela el `CancellationTokenSource` interno (linked al token externo), espera a que el loop termine con timeout `StopTimeoutMs`, logue warning si timeout, dispone el timer vía `using` en `RunLoopAsync`
- [x] 3.5 Implementar `TickAsync` como stub que emite `GameLoopResult { Empty = true }` y captura excepciones emitiendo `GameLoopResult { Error = ex }`
- [x] 3.6 Registrar `AddScoped<IGameLoopCoordinator, GameLoopCoordinator>()` en `Program.cs`
- [x] 3.7 Tests de integración `OpenScrape.App.Tests/GameLoopCoordinatorLifecycleTests.cs` (9 tests): arranque + emisión, idempotencia de `StartAsync`, idempotencia de `StopAsync`, cancelación cooperativa sin error, `StopAsync` sin `Start`, `DisposeAsync` detiene loop, `StartAsync` tras dispose lanza `ObjectDisposedException`, suscriptor que lanza no derriba el loop ni a otros suscriptores (iteración sobre `GetInvocationList`), `GameLoopResult` típico es `Empty=true`
- [x] 3.8 Verificar build + tests verdes (836/836 tests verdes, 0 errores, 0 advertencias)

## 4. GameLoopCoordinator — migración de lógica

- [ ] 4.1 Portar lógica de `FrmMain.btnCapture_Click` → `TickAsync` delegando en `IGameCoordinator.ProcessCurrentStateAsync`. **DIFERIDO**: requiere rewrite de ~500 LOC acoplados a estado mutable de `FrmMain` (`_handle`, `_useCase`, `_coordinateScaler`, `_playerGameState`, contadores OCR) y validación contra sesiones reales de casino. No se puede cerrar sin screenshots de referencia. El feature flag queda en `false` → no hay regresión.
- [ ] 4.2 Portar detección de transición `FlopAction → TurnDetected` / `TurnAction → RiverDetected`. **DIFERIDO** por la misma razón que 4.1 (está entrelazado dentro de `ProcessPostFlopAsync`).
- [x] 4.3 Mover `_heroStackPreRebuy` y lógica de auto-rebuy a `PostflopGameContext` (método `TrackHeroStackForRebuy(decimal stack)` que retorna stack efectivo). Implementado en `PostflopGameContext.cs` con umbral de 50 (decimal). `FrmMain._heroStackPreRebuy` permanece hasta el cutover definitivo (7.3).
- [x] 4.4 `PostflopGameContext.Reset()` ya limpiaba los campos cross-street; se amplió para resetear también `HeroStackPreRebuy` a 0.
- [x] 4.5 `TickAsync` en `GameLoopCoordinator` ya captura excepciones y emite `GameLoopResult { Error = ex }` sin derribar el loop (implementado en Fase 3.5).
- [x] 4.6 Tests unitarios `PostflopGameContextRebuyTests.cs` (6 tests: primera lectura, descenso normal, incremento pequeño, detección de rebuy, umbral exacto, reset). **Desvío**: los 15 tests de integración contra screenshots planificados originalmente NO se escriben porque 4.1/4.2 están diferidos — tests sin lógica que validar serían ruido.
- [x] 4.7 Build + tests verdes (849/849)

## 5. UiSyncService

- [x] 5.1 `IUiSyncService.cs` con `Attach(IGameLoopCoordinator, ISynchronizeInvoke, IFrmOverlay, object? logsBox)`, `Detach()`, `IDisposable`
- [x] 5.2 `UiSyncService.cs` se suscribe a `ResultReady` en `Attach`, marshalla con `BeginInvoke`, auto-scrolla el `TextBox` pasado como `logsBox`
- [x] 5.3 `NullUiSyncService.cs` (Null Object) para tests y futuros builds sin UI
- [x] 5.4 Registrado `AddScoped<IUiSyncService, UiSyncService>()` en `Program.cs` (el null object no se registra por DI: los tests lo instancian directamente)
- [x] 5.5 `IFrmOverlay` creado con 14 métodos consumidos (`UpdateAction`, `UpdateEquityPercentage`, `UpdatePotOddsPercentage`, `UpdateStreetPhase`, `UpdateSituacion`, `UpdateHandStrength`, `UpdateBoardTexture`, `UpdateStreetIndicator`, `UpdateFoldEquity`, `UpdateEVWithFoldEquity`, `UpdateSuggestedBetSize`, `UpdateTableName`, `UpdateWithCalculationResult`, `ClearAll`). `FrmOverlay : Form, IFrmOverlay`
- [x] 5.6 `UiSyncServiceTests.cs` con 7 tests: fake `ISynchronizeInvoke` + fake `IFrmOverlay`, decision update, resultado empty, Attach doble lanza, Detach + reattach, Detach sin attach no lanza, Dispose idempotente, NullUiSyncService

## 6. Integración FrmMain — bajo feature flag

- [x] 6.1 Inyectados `IGameLoopCoordinator`, `IUiSyncService`, `IOptions<FeatureFlags>` en ctor de `FrmMain` (suma temporal, deps=30 hasta 7.5)
- [x] 6.2 En `btnWindow_Click` (punto de arranque de la sesión), bloque condicional: `if (_featureFlags.UseGameLoopCoordinator && !_gameLoopCoordinator.IsRunning)` arranca `CancellationTokenSource`, `_uiSyncService.Attach(coord, this, _frmOverlay, tbResume)` y `_gameLoopCoordinator.StartAsync(cts.Token)`. Flag `false` por defecto → código viejo sigue ejecutándose intacto.
- [x] 6.3 En `FrmMain_FormClosing`, si `_gameLoopCoordinator.IsRunning`: `_uiSyncService.Detach()`, `_gameLoopCts?.Cancel()`, `await _gameLoopCoordinator.StopAsync()`. Comportamiento viejo del closing intacto.
- [ ] 6.4 **PENDIENTE TÚ**: Verificación manual con flag `false` de que no hay regresión (no tengo acceso a casino_barcelona).
- [ ] 6.5 **PENDIENTE TÚ**: Verificación manual con flag `true` en sesión real.
- [x] 6.6 Build + tests verdes (849/849)

## 7. Cutover y limpieza

- [ ] 7.1 **BLOQUEADA por 6.4/6.5**: flip de `UseGameLoopCoordinator` a `true` solo procede tras validación manual exitosa. Flag permanece `false`.
- [ ] 7.2 **BLOQUEADA por 7.1**: marcar `btnCapture_Click`/`ProcessPostFlopAsync` obsoletos solo cuando el reemplazo esté validado. Diferido hasta 4.1/4.2.
- [ ] 7.3 **BLOQUEADA por 7.2**: eliminar código obsoleto tras 1 semana de validación. Diferido.
- [ ] 7.4 **BLOQUEADA por 7.3**: eliminar `FeatureFlags` solo cuando el cutover sea definitivo. Diferido.
- [ ] 7.5 **BLOQUEADA por 4.1/4.2**: consolidar ctor de `FrmMain` a ≤10 deps requiere primero migrar la lógica al coordinator. Diferido.
- [ ] 7.6 **BLOQUEADA por 7.5**: `FrmMain.cs` ≤1,500 LOC. LOC actual: 4,452 (creció +68 por inyección temporal + wiring condicional). Diferido al cutover.
- [x] 7.7 `dotnet format --verify-no-changes OpenScrape.sln` → sin cambios pendientes. Formato limpio.
- [x] 7.8 `dotnet test OpenScrape.sln` → **849/849 tests verdes** (816 pre-existentes + 33 nuevos: 10 facade + 9 coordinator lifecycle + 7 UI sync + 6 rebuy + 1 extra).
- [ ] 7.9 **PENDIENTE TÚ**: validación manual final en sesión real cuando se complete 4.1/4.2 y se active el flag.
