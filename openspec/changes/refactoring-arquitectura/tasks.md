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

## Fase 6 — Extraer ScreenReaderService (PENDIENTE)

### 6A. Definir contrato

- [ ] 6.1 Crear `src/OpenScrape.App/Services/IScreenReaderService.cs`:
  ```csharp
  public interface IScreenReaderService
  {
      Bitmap CaptureScreen(IntPtr windowHandle);
      string ReadPlayerName(Bitmap screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral);
      decimal ReadBetValue(Bitmap screenshot, int posX, int posY, int width, int height, decimal potSize);
      decimal ReadStackValue(Bitmap screenshot, int posX, int posY, int width, int height, double? umbral, double? inactiveUmbral, bool? isOnlyNumber);
      string ReadHandNumber(Bitmap screenshot, int x, int y, int w, int h);
      string ReadText(Bitmap screenshot, int x, int y, int w, int h, double? umbral);
      bool DetectColor(Bitmap screenshot, int x, int y, int w, int h, List<string> targetColors);
      OcrResultWithConfidence ReadWithConfidence(Bitmap screenshot, int x, int y, int w, int h);
  }
  ```

### 6B. Extraer métodos

- [ ] 6.2 Crear `src/OpenScrape.App/Services/ScreenReaderService.cs`
- [ ] 6.3 Mover `ReadPlayerNameOCR()` → `ScreenReaderService.ReadPlayerName()`
- [ ] 6.4 Mover `SetBetValue()` → `ScreenReaderService.ReadBetValue()` (incluye consensus 3-read, PreprocessImageForOCR, CleanOcrNumericText)
- [ ] 6.5 Mover `SetStackValue()` → `ScreenReaderService.ReadStackValue()`
- [ ] 6.6 Mover `SetHandNumberOCR()` → `ScreenReaderService.ReadHandNumber()`
- [ ] 6.7 Mover `SetTextOCR()` → `ScreenReaderService.ReadText()`
- [ ] 6.8 Mover `PreprocessImageForOCR()` → método privado de ScreenReaderService
- [ ] 6.9 Mover `TryMultipleOCRThresholds()` → método privado
- [ ] 6.10 Mover `NormalizeBetValue()` → método privado
- [ ] 6.11 Mover `NormalizeStackValue()` → método privado

### 6C. Actualizar

- [ ] 6.12 Inyectar `IScreenReaderService` en FrmMain
- [ ] 6.13 Reemplazar llamadas OCR en FrmMain por `_screenReader.ReadXxx()`
- [ ] 6.14 Registrar `ScreenReaderService` en `Program.cs` como Singleton
- [ ] 6.15 Verificar: `dotnet build && dotnet test`

### Notas para implementación
- Todos los métodos OCR reciben `Bitmap` de `_formImage.pbImage.Image` — el servicio recibirá bitmap como parámetro
- `OcrService`, `ColorDetectionService`, `ImageCropperService` ya son DI (Fase 3) — ScreenReaderService los inyecta
- `NormalizeBetValue` depende de `_playerGameState.PotSize` — pasar como parámetro
- `SetBetValue` tiene lógica de 3-read consensus — mover completa

---

## Fase 7 — Extraer TableLayoutService (PENDIENTE)

### 7A. Definir contrato

- [ ] 7.1 Crear `src/OpenScrape.App/Services/ITableLayoutService.cs`

### 7B. Extraer métodos

- [ ] 7.2 Crear `src/OpenScrape.App/Services/TableLayoutService.cs`
- [ ] 7.3 Mover `InitializePlayersAsync()`
- [ ] 7.4 Mover detección de dealer: `SetDealerPlayer()`, `IsDealerButtonColor()`, `SetDealerForPlayer()`, `DetermineP0Position()`
- [ ] 7.5 Mover detección de jugadores: `SetActivePlayer()`, `SetEmptyPlayer()`, `SetSitOutPlayer()`, `DetectFoldedPlayers()`, `ValidatePlayerStates()`, `RefreshPlayerStates()`
- [ ] 7.6 Mover posiciones y alias: `SetVillainPosition()`, `SetVillainPositionExtension()`, `SetIsInPosition()`, `SetAliasVillain()`, `RetryEmptyAliases()`, `ValidatePositionAssignments()`

### 7C. Actualizar

- [ ] 7.7 Inyectar `ITableLayoutService` en FrmMain (y GameCoordinator si necesario)
- [ ] 7.8 Reemplazar llamadas en FrmMain por `_tableLayout.DetectXxx()`
- [ ] 7.9 Registrar `TableLayoutService` en `Program.cs` como Scoped
- [ ] 7.10 Verificar: `dotnet build && dotnet test`

### Notas para implementación
- Dependen de `_regionLookupCache`, `ICoordinateScaler`, `OcrService`, `ColorDetectionService` — todos ya inyectables
- `_formImage.pbImage.Image` es la dependencia principal — bitmap se pasa como parámetro
- `PlayerGameState[]` se pasa por referencia (FrmMain lo posee)

---

## Verificación Final (tras completar Fases 6-7)

- [ ] V1. `dotnet clean && dotnet build` — 0 errores, 0 warnings nuevos
- [ ] V2. `dotnet test` — 592+ tests pasan
- [ ] V3. `dotnet format --verify-no-changes` — formato correcto
- [ ] V4. FrmMain.cs < 3,000 LOC (objetivo post fases 6-7)
- [ ] V5. `grep -r "new ColorDetectionService\|new OcrService\|new ImageCropperService" src/OpenScrape.App/Forms/` — 0 resultados
- [ ] V6. `grep -r "static.*_reference\|static.*_isInitialized" src/` — 0 resultados
- [ ] V7. Ejecutar app manualmente — game loop funciona
- [ ] V8. Jugar 1 sesión de prueba completa
