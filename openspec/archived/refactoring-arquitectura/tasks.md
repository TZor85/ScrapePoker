# Tasks: Refactoring Arquitectónico

## Fase 1 — Interfaces para DecisionMaker ✅ (2026-04-07)

- [x] 13 interfaces creadas en `src/OpenScrape.DecisionMaker/Interfaces/`
- [x] 13 clases implementan su interfaz
- [x] Program.cs con forwarding DI (concreto + interfaz → misma instancia)
- [x] FrmMain recibe interfaces en constructor
- [x] StrategyBacktester recibe IPostflopDecisionService
- [x] AutoCalibrationService acepta IExploitabilityCalculator
- [x] Build 0 errores, 592 tests pasan

## Fase 2 — PostflopDecisionInput ✅ (2026-04-07)

- [x] Record `PostflopDecisionInput` creado en `DTOs/` con 6 required + 30 defaults
- [x] Overload `DetermineAction(PostflopDecisionInput)` en PostflopDecisionService
- [x] Overload en IPostflopDecisionService
- [x] Legacy de 36 params marcado `[Obsolete]`
- [x] 3 call sites en FrmMain migrados (Flop, Turn, River)
- [x] Build 0 errores, 592 tests pasan

## Fase 3 — Migrar Servicios Manuales a DI ✅ (2026-04-07)

- [x] 13 servicios/UseCases migrados de `new` a DI
- [x] OcrService, ColorDetectionService, ImageCropperService como Singleton
- [x] SetFlopForceBoardUseCase, GetHashImageUseCase, GetCropImageUseCase por interfaz
- [x] OutsCalculatorUseCase, GetWindowsScreenUseCase, DetectionLoggerService
- [x] SetPreflopActionUseCase, GetCardsFlop/Turn/RiverUseCase
- [x] 0 instancias `new ServiceX()` en FrmMain
- [x] Build 0 errores, 592 tests pasan

## Fase 4 — CoordinateScaler a Servicio Inyectable ✅ (2026-04-07)

- [x] ICoordinateScaler creado en App/Services/
- [x] CoordinateScaler: `static class` → `class : ICoordinateScaler` Singleton
- [x] Program.cs: inicialización via instancia DI
- [x] FrmMain: 2 call sites migrados a _coordinateScaler
- [x] 3 GetCardsXxxUseCase: reciben ICoordinateScaler + ImageCropperService por DI
- [x] 0 referencias estáticas a CoordinateScaler en src/
- [x] Build 0 errores, 592 tests pasan

## Fase 5 — Extraer GameCoordinator ✅ (2026-04-07)

- [x] IGameCoordinator + GameCoordinator creados (739 LOC)
- [x] GameDecisionResult record creado
- [x] TurnBoardTexture/RiverBoardTexture enums movidos a Entities/BoardTextures.cs
- [x] 3 DetermineXxxAction movidos a GameCoordinator (pipeline completa)
- [x] 11 helpers delegados (GetOpponentBetSize, GetVillainType, etc.)
- [x] FormatCardsForLog delegado
- [x] FrmMain: 5,977 → 5,449 LOC (-528)
- [x] Build 0 errores, 592 tests pasan

## Fase 8 — Thread Safety ✅ (2026-04-07)

- [x] OpponentTracker: Dictionary → ConcurrentDictionary (ambos)
- [x] OpponentTracker: GetOrAdd, TryAdd, TryRemove
- [x] AutoCalibrationService: Interlocked.Increment/Exchange
- [x] FrmMain: _executeCapture, _backgroundExecute → volatile
- [x] GameLoggerService: Interlocked.Increment para _sessionTotalHands
- [x] GameLoggerService: lock para _sessionTotalProfit
- [x] GameLoggerService: SemaphoreSlim(1,1) para DB writes
- [x] Build 0 errores, 592 tests pasan

---

## Fase 6 — Extraer ScreenReaderService ✅ (2026-04-07)

### 6A. Definir contrato

- [x] 6.1 Crear `src/OpenScrape.App/Services/IScreenReaderService.cs` (8 métodos: ReadPlayerName, ReadBetValue, ReadStackValue, ReadHandNumber, ReadText, ReadTextWithMultipleThresholds, NormalizeBetValue, NormalizeStackValue)

### 6B. Extraer métodos

- [x] 6.2 Crear `src/OpenScrape.App/Services/ScreenReaderService.cs`
- [x] 6.3 Mover `ReadPlayerNameOCR()` → `ScreenReaderService.ReadPlayerName()`
- [x] 6.4 Mover `SetBetValue()` → `ScreenReaderService.ReadBetValue()` (incluye consensus 3-read, PreprocessImageForOCR, CleanOcrNumericText)
- [x] 6.5 Mover `SetStackValue()` → `ScreenReaderService.ReadStackValue()`
- [x] 6.6 Mover `SetHandNumberOCR()` → `ScreenReaderService.ReadHandNumber()`
- [x] 6.7 Mover `SetTextOCR()` → `ScreenReaderService.ReadText()`
- [x] 6.8 Mover `PreprocessImageForOCR()` → método privado de ScreenReaderService
- [x] 6.9 Mover `TryMultipleOCRThresholds()` → `ReadTextWithMultipleThresholds()` público
- [x] 6.10 Mover `NormalizeBetValue()` → método público de ScreenReaderService
- [x] 6.11 Mover `NormalizeStackValue()` → método público de ScreenReaderService
- [x] 6.extra Mover `CleanOcrPlayerName()`, `CleanOcrNumericText()`, `CleanOcrHandNumber()` → privados

### 6C. Actualizar

- [x] 6.12 Inyectar `IScreenReaderService` en FrmMain
- [x] 6.13 Reemplazar llamadas OCR en FrmMain por `_screenReader.ReadXxx()`
- [x] 6.14 Registrar `ScreenReaderService` en `Program.cs` como Singleton (con forwarding)
- [x] 6.15 Verificar: `dotnet build && dotnet test` — 0 errores, 592 tests pasan

---

## Fase 7 — Extraer TableLayoutService ✅ (2026-04-07)

### 7A. Definir contrato

- [x] 7.1 Crear `src/OpenScrape.App/Services/ITableLayoutService.cs` (15 métodos + 3 propiedades dealer state)

### 7B. Extraer métodos

- [x] 7.2 Crear `src/OpenScrape.App/Services/TableLayoutService.cs` (624 LOC)
- [x] 7.3 Mover `InitializePlayersAsync()` → `InitializePlayers(Image, PlayerGameState)`
- [x] 7.4 Mover detección de dealer: `SetDealerPlayer()`, `IsDealerButtonColor()`, `SetDealerForPlayer()`, `DetermineP0Position()`
- [x] 7.5 Mover detección de jugadores: `SetActivePlayer()`, `SetEmptyPlayer()`, `SetSitOutPlayer()`, `DetectFoldedPlayers()`, `ValidatePlayerStates()`, `RefreshPlayerStates()`
- [x] 7.6 Mover posiciones y alias: `SetVillainPosition()`, `SetVillainPositionExtension()`, `SetIsInPosition()`, `SetAliasVillain()`, `RetryEmptyAliases()`, `ValidatePositionAssignments()`
- [x] 7.extra Mover helpers: `CreatePlayerData()`, `IsColorMatch()`, constantes `_colorEmpty`/`_colorPlaying`/`_colorDealer`
- [x] 7.extra Estado dealer: `DealerValuePosition`, `DealerPosition`, `PreviousDealerPlayerName` como propiedades

### 7C. Actualizar

- [x] 7.7 Inyectar `ITableLayoutService` en FrmMain
- [x] 7.8 Reemplazar llamadas en FrmMain por `_tableLayout.XxxMethod()`
- [x] 7.9 Registrar `TableLayoutService` en `Program.cs` como Scoped (forwarding)
- [x] 7.10 Verificar: `dotnet build && dotnet test` — 0 errores, 592 tests pasan

---

## Verificación Final (tras completar Fases 6-7)

- [x] V1. `dotnet clean && dotnet build` — 0 errores
- [x] V2. `dotnet test` — 592 tests pasan
- [ ] V3. `dotnet format --verify-no-changes` — formato correcto
- [x] V4. FrmMain.cs = 4,244 LOC (era 5,977, -29% reducción)
- [ ] V5. `grep -r "new ColorDetectionService\|new OcrService\|new ImageCropperService" src/OpenScrape.App/Forms/` — 0 resultados
- [ ] V6. `grep -r "static.*_reference\|static.*_isInitialized" src/` — 0 resultados
- [x] V7. Ejecutar app manualmente — game loop funciona ✅ (2026-04-07)
- [ ] V8. Jugar 1 sesión de prueba completa
