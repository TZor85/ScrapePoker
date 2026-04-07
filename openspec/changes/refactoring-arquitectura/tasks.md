# Tasks: Refactoring Arquitectónico

## Fase 1 — Interfaces para DecisionMaker (13 servicios)

- [ ] 1.1 Crear `src/OpenScrape.DecisionMaker/Interfaces/IPostflopDecisionService.cs`
  - Métodos: `DetermineAction(PostflopDecisionInput)`, `DetermineAction(double equity, ...)` (legacy)
  - Return: `PostflopDecisionResult`

- [ ] 1.2 Crear `src/OpenScrape.DecisionMaker/Interfaces/IBetSizingService.cs`
  - Métodos públicos actuales de `BetSizingService`

- [ ] 1.3 Crear `src/OpenScrape.DecisionMaker/Interfaces/IOpponentTracker.cs`
  - Métodos: `GetProfile()`, `RecordHandPlayed()`, `RecordVPIP()`, `RecordPFR()`, `RecordPostflopAction()`, `RecordCBetOpportunity()`, `RecordFacedCBet()`, `RegisterSeatAlias()`, `GetFoldToBetPct()`

- [ ] 1.4 Crear `src/OpenScrape.DecisionMaker/Interfaces/IDangerPenaltyCalculator.cs`
  - Métodos: `CalculateDangerPenalty()`, `AnalyzeDangerCards()`

- [ ] 1.5 Crear `src/OpenScrape.DecisionMaker/Interfaces/IImpliedOddsCalculator.cs`
  - Métodos: `CalculateImpliedOdds()`, `CalculateReverseImpliedOdds()`

- [ ] 1.6 Crear `src/OpenScrape.DecisionMaker/Interfaces/IPreflopAnalyzer.cs`
  - Métodos: `AnalyzePreflop()`, `GetPreflopEquity()`

- [ ] 1.7 Crear `src/OpenScrape.DecisionMaker/Interfaces/IRangePolarizer.cs`
  - Métodos públicos actuales de `RangePolarizer`

- [ ] 1.8 Crear `src/OpenScrape.DecisionMaker/Interfaces/IStrategyAnalyzerService.cs`
  - Métodos públicos de análisis de estrategia

- [ ] 1.9 Crear `src/OpenScrape.DecisionMaker/Interfaces/IExploitabilityCalculator.cs`
  - Métodos: `CalculateExploitability()`, `FindLeaks()`

- [ ] 1.10 Crear `src/OpenScrape.DecisionMaker/Interfaces/IAutoCalibrationService.cs`
  - Métodos: `Calibrate()`, `PreviewCalibration()`

- [ ] 1.11 Crear `src/OpenScrape.DecisionMaker/Interfaces/IBankrollTrackerService.cs`
  - Métodos: `GetBankrollStats()`, `InitializeBankroll()`

- [ ] 1.12 Crear `src/OpenScrape.DecisionMaker/Interfaces/IEquityCalculatorService.cs`
  - Métodos públicos de cálculo de equity

- [ ] 1.13 Crear `src/OpenScrape.DecisionMaker/Interfaces/IStrategyBacktester.cs`
  - Métodos: `RunBacktest()`, `CompareStrategies()`

- [ ] 1.14 Hacer que cada clase implemente su interfaz (`: IXxxService`)

- [ ] 1.15 Actualizar `Program.cs` — registrar por interfaz:
  ```csharp
  services.AddSingleton<PostflopDecisionService>();
  services.AddSingleton<IPostflopDecisionService>(sp => sp.GetRequiredService<PostflopDecisionService>());
  ```

- [ ] 1.16 Actualizar `FrmMain` constructor — recibir interfaces en vez de clases concretas

- [ ] 1.17 Verificar: `dotnet build OpenScrape.sln` sin errores

- [ ] 1.18 Verificar: `dotnet test OpenScrape.sln` — 519 tests pasan

---

## Fase 2 — PostflopDecisionInput (Objeto Parámetro)

- [ ] 2.1 Crear `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs`:
  ```csharp
  public record PostflopDecisionInput
  {
      public required double Equity { get; init; }
      public required BoardPosition Street { get; init; }
      public required HandSituation Situation { get; init; }
      public required string BoardTexture { get; init; }
      public required bool IsInPosition { get; init; }
      public required BetSizeCategory VillainBetSize { get; init; }
      public double PotOdds { get; init; }
      public int TotalOuts { get; init; }
      public bool PreviousStreetBet { get; init; }
      public bool VillainShowedAggression { get; init; }
      public BoardChangeResult? BoardChange { get; init; }
      public bool HeroBlocksDangerSuit { get; init; }
      public decimal HeroStack { get; init; }
      public decimal PotSize { get; init; }
      public bool HasFlushDraw { get; init; }
      public int NumOpponents { get; init; } = 1;
      public bool HeroIsAggressor { get; init; }
      public HandRank HeroHandRank { get; init; }
      public bool HasComboDraw { get; init; }
      public bool VillainAggressorCheckedPreviousStreet { get; init; }
      public bool VillainBarreling { get; init; }
      public OpponentType VillainType { get; init; }
      public PairClassification PairClassification { get; init; }
      public double FoldEquity { get; init; }
      public BetSizeCategory VillainBetSizeFlop { get; init; }
      public BetSizeCategory VillainBetSizeTurn { get; init; }
      public bool VillainCheckedMiddleStreet { get; init; }
      public bool HeroHasNutBlocker { get; init; }
      public bool HeroFloatedFlop { get; init; }
      public double VillainFoldToBetPct { get; init; } = -1;
      public KickerStrength HeroKickerStrength { get; init; }
      public bool TurnCalledWithFlushDanger { get; init; }
      public bool HeroBlocksTopCard { get; init; }
      public bool HeroCheckedAllStreets { get; init; }
      public bool IsAnyoneAllIn { get; init; }
  }
  ```

- [ ] 2.2 Agregar método `DetermineAction(PostflopDecisionInput input)` en `PostflopDecisionService`:
  - Extraer campos del record y delegar al método existente de 36 params
  - Este será el método principal; el de 36 params se marca `[Obsolete]`

- [ ] 2.3 Actualizar `IPostflopDecisionService` (de Fase 1) con el nuevo overload

- [ ] 2.4 Actualizar call sites en FrmMain (`DetermineFlopActionUnified`, `DetermineTurnAction`, `DetermineRiverAction`):
  - Construir `PostflopDecisionInput` con object initializer
  - Llamar al nuevo overload

- [ ] 2.5 Verificar: `dotnet test` — todos los tests pasan (tests usan overload legacy, que sigue activo)

---

## Fase 3 — Migrar Servicios Manuales a DI

- [ ] 3.1 Registrar `OcrService` en `Program.cs` como Singleton:
  - Verificar que el constructor no requiere parámetros de FrmMain
  - Si requiere ruta de tessdata, inyectar via `IOptions<OcrConfig>`

- [ ] 3.2 Registrar `ColorDetectionService` en `Program.cs` como Singleton

- [ ] 3.3 Registrar `ImageCropperService` en `Program.cs` como Singleton

- [ ] 3.4 Crear interfaces `IOcrService`, `IColorDetectionService`, `IImageCropperService` en `src/OpenScrape.App/Services/`

- [ ] 3.5 Registrar `GetHashImageUseCase`, `GetCropImageUseCase` en DI (o verificar que ya están via `CardUseCases`)

- [ ] 3.6 Registrar `SetFlopForceBoardUseCase` en DI

- [ ] 3.7 Eliminar `new` manuales en FrmMain — reemplazar por campos inyectados

- [ ] 3.8 Eliminar `DetectionLoggerService` creado manualmente — registrar en DI

- [ ] 3.9 Verificar: `dotnet build && dotnet test` — todo pasa

---

## Fase 4 — CoordinateScaler a Servicio Inyectable

- [ ] 4.1 Crear `src/OpenScrape.App/Services/ICoordinateScaler.cs`:
  ```csharp
  public interface ICoordinateScaler
  {
      bool IsInitialized { get; }
      int ReferenceWidth { get; }
      int ReferenceHeight { get; }
      void Initialize(int referenceWidth, int referenceHeight);
      (int X, int Y, int Width, int Height) ScaleRegion(int x, int y, int w, int h, int currentWidth, int currentHeight);
  }
  ```

- [ ] 4.2 Convertir `CoordinateScaler` de `static class` a `class CoordinateScaler : ICoordinateScaler`:
  - Eliminar `static` de clase y miembros
  - Mantener misma lógica interna
  - Eliminar `Reset()` (ya no necesario — los tests usan nueva instancia)

- [ ] 4.3 Registrar en `Program.cs` como Singleton

- [ ] 4.4 Actualizar `FrmMain` — inyectar `ICoordinateScaler` en constructor:
  - Reemplazar todas las llamadas `CoordinateScaler.ScaleRegion(...)` por `_coordinateScaler.ScaleRegion(...)`
  - Reemplazar `CoordinateScaler.Initialize(...)` por `_coordinateScaler.Initialize(...)`

- [ ] 4.5 Actualizar `Program.cs` línea 104 — inicialización via servicio resuelto, no estático

- [ ] 4.6 Actualizar cualquier otro archivo que use `CoordinateScaler` directamente (buscar con grep)

- [ ] 4.7 Verificar: `dotnet build && dotnet test`

---

## Fase 5 — Extraer GameCoordinator

### 5A. Definir contrato

- [ ] 5.1 Crear `src/OpenScrape.App/Services/IGameCoordinator.cs`:
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
  ```

- [ ] 5.2 Crear `src/OpenScrape.App/DTOs/GameLoopResult.cs`:
  ```csharp
  public record GameLoopResult
  {
      public string? RecommendedAction { get; init; }
      public PostflopDecisionResult? DecisionResult { get; init; }
      public PokerCalculationResult? CalculationResult { get; init; }
      public string? LogText { get; init; }
      public bool HandCompleted { get; init; }
      public bool NewHandDetected { get; init; }
  }
  ```

### 5B. Extraer métodos (mover sin modificar lógica)

- [ ] 5.3 Crear `src/OpenScrape.App/Services/GameCoordinator.cs`

- [ ] 5.4 Mover `ProcessPreflopAsync()` (FrmMain:1364) → `GameCoordinator.ProcessPreflopAsync()`:
  - Extraer dependencias necesarias como parámetros de constructor
  - Reemplazar acceso directo a controles UI por return values en `GameLoopResult`

- [ ] 5.5 Mover `ProcessPostFlopAsync()` (FrmMain:1423) → `GameCoordinator.ProcessCurrentStateAsync()`:
  - Incluye lógica de transición Flop→Turn→River

- [ ] 5.6 Mover `ProcessFlopAsync()` (FrmMain:1781) → `GameCoordinator.ProcessFlopAsync()`

- [ ] 5.7 Mover `ProcessTurnAsync()` (FrmMain:2197) → `GameCoordinator.ProcessTurnAsync()`

- [ ] 5.8 Mover `ProcessRiverAsync()` (FrmMain:2278) → `GameCoordinator.ProcessRiverAsync()`

- [ ] 5.9 Mover `HandleNewHandAsync()` (FrmMain:2362) → `GameCoordinator.HandleNewHandAsync()`

- [ ] 5.10 Mover métodos de decisión:
  - `DetermineFlopActionUnified()` (FrmMain:1890)
  - `DetermineTurnAction()` (FrmMain:2010)
  - `DetermineRiverAction()` (FrmMain:1524)
  - `AdjustBetSize()` (FrmMain:5711)

- [ ] 5.11 Mover métodos de board analysis:
  - `AnalyzeBoardChange()` (FrmMain:1664)
  - `AnalyzeTurnBoardTexture()` (FrmMain:1677)
  - `AnalyzeRiverBoardTexture()` (FrmMain:1703)
  - `IsBoardCardVisible()` (FrmMain:1492)

- [ ] 5.12 Mover métodos de equity:
  - `GetPotOddsCalculator()` (FrmMain:2676)
  - `GetOpponentBetSize()` (FrmMain:109)
  - `GetVillainType()` (FrmMain:360)
  - `TrackVillainPostflopAction()` (FrmMain:377)

### 5C. Actualizar FrmMain

- [ ] 5.13 Inyectar `IGameCoordinator` en constructor de FrmMain

- [ ] 5.14 Reemplazar llamadas directas por delegación:
  ```csharp
  // Antes (en BackgroundWorker/game loop):
  await ProcessFlopAsync(potOddsResult);
  
  // Después:
  var result = await _coordinator.ProcessFlopAsync(screenshot, _players);
  UpdateUIWithResults(result);
  ```

- [ ] 5.15 Crear método `UpdateUIFromResult(GameLoopResult result)` en FrmMain que actualice:
  - Overlay
  - tbResume (log)
  - Card images
  - Player indicators

- [ ] 5.16 Registrar `GameCoordinator` en `Program.cs` como Scoped

- [ ] 5.17 Verificar: `dotnet build && dotnet test`

### 5D. Tests

- [ ] 5.18 Crear `OpenScrape.App.Tests/Services/GameCoordinatorTests.cs`:
  - Test: ProcessFlopAsync con equity alta → result contiene Bet
  - Test: ProcessRiverAsync con equity baja → result contiene Fold
  - Test: HandleNewHandAsync resetea PostflopGameContext
  - Test: Transiciones de estado correctas
  - Usar mocks de interfaces (Fase 1) para aislar

---

## Fase 6 — Extraer ScreenReaderService

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
      bool DetectColor(Bitmap screenshot, int x, int y, List<string> targetColors);
      OcrResult ReadWithConfidence(Bitmap screenshot, int x, int y, int w, int h);
  }
  ```

### 6B. Extraer métodos

- [ ] 6.2 Crear `src/OpenScrape.App/Services/ScreenReaderService.cs`

- [ ] 6.3 Mover `ReadPlayerNameOCR()` (FrmMain:283) → `ScreenReaderService.ReadPlayerName()`

- [ ] 6.4 Mover `SetBetValue()` (FrmMain:3795) → `ScreenReaderService.ReadBetValue()`:
  - Incluye consensus 3-read, PreprocessImageForOCR, CleanOcrNumericText

- [ ] 6.5 Mover `SetStackValue()` (FrmMain:3919) → `ScreenReaderService.ReadStackValue()`

- [ ] 6.6 Mover `SetHandNumberOCR()` (FrmMain:3997) → `ScreenReaderService.ReadHandNumber()`

- [ ] 6.7 Mover `SetTextOCR()` (FrmMain:4068) → `ScreenReaderService.ReadText()`

- [ ] 6.8 Mover `PreprocessImageForOCR()` (FrmMain:3871) → método privado de ScreenReaderService

- [ ] 6.9 Mover `TryMultipleOCRThresholds()` (FrmMain:3419) → método privado

- [ ] 6.10 Mover `NormalizeBetValue()` (FrmMain:2863) → método privado

- [ ] 6.11 Mover `NormalizeStackValue()` (FrmMain:2813) → método privado

- [ ] 6.12 Mover `GetImageWhilePlaying()` (FrmMain:4101) → `ScreenReaderService.CaptureScreen()`

### 6C. Actualizar

- [ ] 6.13 Inyectar `IScreenReaderService` en `GameCoordinator` y `FrmMain`

- [ ] 6.14 Reemplazar llamadas OCR en FrmMain y GameCoordinator por `_screenReader.ReadXxx()`

- [ ] 6.15 Registrar `ScreenReaderService` en `Program.cs` como Singleton

- [ ] 6.16 Verificar: `dotnet build && dotnet test`

### 6D. Tests

- [ ] 6.17 Crear `OpenScrape.App.Tests/Services/ScreenReaderServiceTests.cs`:
  - Test: NormalizeBetValue — "593" con pot 5.93 → 5.93
  - Test: NormalizeStackValue — formatos decimales
  - Test: ReadBetValue consensus — 3 lecturas consistentes

---

## Fase 7 — Extraer TableLayoutService

### 7A. Definir contrato

- [ ] 7.1 Crear `src/OpenScrape.App/Services/ITableLayoutService.cs`:
  ```csharp
  public interface ITableLayoutService
  {
      Task InitializePlayersAsync(Bitmap screenshot, PlayerGameState[] players);
      void DetectDealerPosition(Bitmap screenshot, PlayerGameState[] players);
      void DetectActivePlayers(Bitmap screenshot, PlayerGameState[] players);
      void DetectEmptySeats(Bitmap screenshot, PlayerGameState[] players);
      void DetectFoldedPlayers(Bitmap screenshot, PlayerGameState[] players);
      void DetectSitOutPlayers(Bitmap screenshot, PlayerGameState[] players);
      TablePosition DetermineHeroPosition(PlayerGameState[] players);
      void SetVillainPositions(PlayerGameState[] players);
      bool ValidatePositionAssignments(PlayerGameState[] players);
      void SetIsInPosition(PlayerGameState[] players);
      string GetActiveVillainId(PlayerGameState[] players);
      void RetryEmptyAliases(Bitmap screenshot, PlayerGameState[] players);
  }
  ```

### 7B. Extraer métodos

- [ ] 7.2 Crear `src/OpenScrape.App/Services/TableLayoutService.cs`

- [ ] 7.3 Mover `InitializePlayersAsync()` (FrmMain:1267)

- [ ] 7.4 Mover detección de dealer:
  - `SetDealerPlayer()` (FrmMain:3250)
  - `IsDealerButtonColor()` (FrmMain:3305)
  - `SetDealerForPlayer()` (FrmMain:3334)
  - `DetermineP0Position()` (FrmMain:3402)

- [ ] 7.5 Mover detección de jugadores:
  - `SetActivePlayer()` (FrmMain:2998)
  - `SetEmptyPlayer()` (FrmMain:2958)
  - `SetSitOutPlayer()` (FrmMain:3440)
  - `DetectFoldedPlayers()` (FrmMain:173)
  - `ValidatePlayerStates()` (FrmMain:241)
  - `RefreshPlayerStates()` (FrmMain:207)

- [ ] 7.6 Mover posiciones y alias:
  - `SetVillainPosition()` (FrmMain:3548)
  - `SetVillainPositionExtension()` (FrmMain:3593)
  - `SetIsInPosition()` (FrmMain:3508)
  - `SetAliasVillain()` (FrmMain:3040)
  - `GetActiveVillainId()` (FrmMain:264)
  - `RetryEmptyAliases()` (FrmMain:332)
  - `ValidatePositionAssignments()` (FrmMain:2152)

- [ ] 7.7 Mover `HeroBlocksTopBoardCard()` (FrmMain:3496) — depende de cartas, evaluar si va aquí o en GameCoordinator

- [ ] 7.8 Mover `CreatePlayerData()` (FrmMain:3069)

### 7C. Actualizar

- [ ] 7.9 Inyectar `ITableLayoutService` en `GameCoordinator` y `FrmMain`

- [ ] 7.10 Reemplazar llamadas de detección en FrmMain por `_tableLayout.DetectXxx()`

- [ ] 7.11 Registrar `TableLayoutService` en `Program.cs` como Scoped

- [ ] 7.12 Verificar: `dotnet build && dotnet test`

### 7D. Tests

- [ ] 7.13 Crear `OpenScrape.App.Tests/Services/TableLayoutServiceTests.cs`:
  - Test: DetermineHeroPosition con dealer en diferentes seats
  - Test: DetectFoldedPlayers marca correctamente
  - Test: ValidatePositionAssignments detecta inconsistencias
  - Test: SetIsInPosition — hero BTN vs BB

---

## Fase 8 — Thread Safety

- [ ] 8.1 `OpponentTracker` — reemplazar `Dictionary<string, OpponentProfile>` por `ConcurrentDictionary`:
  ```csharp
  private readonly ConcurrentDictionary<string, OpponentProfile> _profiles = new();
  ```
  - Reemplazar `_profiles[key] = value` por `_profiles.AddOrUpdate()`
  - Reemplazar `_profiles.ContainsKey()` + `_profiles[key]` por `_profiles.GetOrAdd()`

- [ ] 8.2 `OpponentTracker._seatAliasCache` → `ConcurrentDictionary<string, string>`

- [ ] 8.3 `FrmMain._executeCapture` → marcar `volatile`:
  ```csharp
  private volatile bool _executeCapture;
  ```

- [ ] 8.4 `FrmMain._newHand` → marcar `volatile`

- [ ] 8.5 `FrmMain._newTableHand` → marcar `volatile`

- [ ] 8.6 `AutoCalibrationService._decisionsSinceLastCalibration` → usar `Interlocked.Increment()`:
  ```csharp
  Interlocked.Increment(ref _decisionsSinceLastCalibration);
  ```

- [ ] 8.7 `GameLoggerService` — agregar `SemaphoreSlim` para serializar writes a BD:
  ```csharp
  private readonly SemaphoreSlim _dbWriteLock = new(1, 1);
  
  public async Task SaveSessionAsync(...)
  {
      await _dbWriteLock.WaitAsync();
      try { /* ... */ }
      finally { _dbWriteLock.Release(); }
  }
  ```

- [ ] 8.8 Verificar: `dotnet build && dotnet test`

- [ ] 8.9 Verificar con análisis estático que no quedan campos mutables compartidos sin protección

---

## Verificación Final

- [ ] V1. `dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln` — 0 errores, 0 warnings nuevos
- [ ] V2. `dotnet test OpenScrape.sln` — 519+ tests pasan
- [ ] V3. `dotnet format --verify-no-changes OpenScrape.sln` — formato correcto
- [ ] V4. FrmMain.cs < 1,500 LOC (contar con `wc -l`)
- [ ] V5. `grep -r "new ColorDetectionService\|new OcrService\|new ImageCropperService\|new SetFlopForceBoard\|new GetHashImage\|new GetCropImage" src/OpenScrape.App/Forms/` — 0 resultados
- [ ] V6. `grep -r "static.*_reference\|static.*_isInitialized" src/` — 0 resultados (CoordinateScaler migrado)
- [ ] V7. Ejecutar app manualmente — game loop funciona igual que antes
- [ ] V8. Jugar 1 sesión de prueba completa — decisiones correctas, overlay funciona, historial se guarda
