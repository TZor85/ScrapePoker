# OpenScrape.App — Design Técnico

> Cómo está construido el composition root, el ciclo de captura y los servicios que orquestan el motor de decisión. Toda la lógica WinForms, OCR Tesseract, P-Invoke y configuración `IConfiguration` vive aquí; las demás capas se consumen mediante interfaces inyectadas.

---

## Interface

`OpenScrape.App` no expone una API pública para consumidores externos — es el ejecutable. Sin embargo, **internamente** se organiza alrededor de 11 interfaces inyectables (servicios extraídos del antiguo `FrmMain` god-class) y 18 use cases que componen la fachada `IPokerCalculator`. Esta sección documenta esos contratos como si fueran "API interna del módulo App".

### 1. Interfaces de servicio inyectables (11)

| Interfaz | Implementación | Símbolos clave | Lifetime |
|----------|----------------|----------------|----------|
| `IPokerCalculator` | `UnifiedPokerCalculator` | `Calculate(playerHand, communityCards, pot, betToCall, numOpp, mcIters?, isInPos, heroStack, villainStack, situation?, villainPos, profile?) → PokerCalculationResult` 🟢 | Singleton |
| `IPokerDecisionFacade` | `PokerDecisionFacade` | Pipeline 5 fases medidas (equity → texture → profile → decision → sizing). **No usado en producción** (cutover pendiente) 🟡 | Scoped |
| `IGameCoordinator` | `GameCoordinator` | `DetermineFlopAction/Turn/River`, `GetOpponentBetSize`, `GetActiveVillainId`, `GetVillainType`, `GetActiveVillainProfile`, `GetVillainStack`, `HeroBlocksTopBoardCard`, `FormatCardsForLog`, `DetectDonkBet`, `AdjustBetSize`, `AnalyzeBoardChange/Turn/River`, `DetectNewHand`, `ResetContext`, `TrackVillainPostflopAction`, `SetFlop/Turn/RiverResult` 🟢 | Scoped |
| `IScreenReaderService` | `ScreenReaderService` | `ReadPlayerName`, `ReadBet`, `ReadStack`, `ReadHandNumber`, `ReadGenericText` (con consenso 3-reads + normalización) 🟢 | Singleton |
| `ITableLayoutService` | `TableLayoutService` | `SetDealerPlayer`, `SetEmptyPlayer`, `SetSitOutPlayer`, `SetActivePlayer`, `InitializePlayers`, `RefreshPlayerStates`, `ValidatePlayerStates`, `RetryEmptyAliases`, `SetVillainPosition`, `ResetDealerState`, `DealerValuePosition` (prop) 🟢 | Scoped |
| `ICoordinateScaler` | `CoordinateScaler` | `Initialize(refW, refH)` (one-shot) + `ScaleRegion(x, y, w, h, currW, currH) → (x, y, w, h)` 🟢 | Singleton |
| `IFrmOverlay` | `FrmOverlay` | `UpdateAction`, `UpdateEquityPercentage`, `UpdatePotOddsPercentage`, `UpdateStreetPhase`, `UpdateTableName`, `UpdateBoardTexture`, `UpdateStreetIndicator`, `ClearAll` 🟢 | Scoped (instanciado por `FrmMain`) |
| `IGameLoopCoordinator` | `GameLoopCoordinator` (`IAsyncDisposable`) | `StartAsync(token)`, `StopAsync()`, `ResultReady` (event), `IsRunning` (prop). **Inactivo bajo `FeatureFlags.UseGameLoopCoordinator=false`** 🟡 | Scoped |
| `IUiSyncService` | `UiSyncService` / `NullUiSyncService` (tests) | `Attach(form)`, `Detach()`, `Post(GameLoopResult)` 🟢 | Scoped |
| `IPostflopContextHolder` | `PostflopContextHolder` | `Current` (`Volatile.Read`), `Update(Func<T,T>)` (`lock`), `StartNewHand()` 🟢 | Scoped |
| `IActionFormatter` | `ActionFormatter` | `EnrichActionWithBBAmount(action, villains, bigBlind) → string` 🟢 | Singleton |
| `IOverlayPositioner` | `OverlayPositioner` | `Calculate(winLeft, winTop, winW, winH, ovrW, ovrH, hOff, vOff) → (x, y)` 🟢 | Singleton |
| `IMetricsCollector` | `MetricsCollector` | `Measure(cat) → ScopedMeasurement`, `MeasureSessionOnly(cat)`, `Record(cat, ts)`, `StartHand(id)`, `EndHand() → TelemetryAggregate?`, `SnapshotSession() → MetricsSnapshot`, `ResetSession()` 🟢 | Singleton |

### 2. Use cases públicos del namespace `Aplication`

| Símbolo | Asignatura | Retorno | Observación |
|---------|-----------|---------|-------------|
| `IPokerCalculator.Calculate` | `(List<CardDataOuts> hand, List<CardDataOuts> board, decimal pot, decimal betToCall, int numOpp=1, int? mcIters=null, bool isInPos=false, decimal heroStack=0, decimal villainStack=0, string? situation=null, TablePosition villainPos=None, OpponentProfile? profile=null)` | `PokerCalculationResult` | **Facade real del motor** — 8 pasos pipeline 🟢 (`Aplication/UseCases/UnifiedPokerCalculator.cs:80-83`) |
| `ISetPreflopActionUseCase.Execute` | `(PlayerGameState, ActionScenarioRequest)` | `Task<ResponseAction>` | Cascada 11 ramas según `HandSituation`. Compone 10 sub-cases `GetActionXxx` 🟡 |
| `ISetFlopForceBoardUseCase.Execute` | `(TableScrapeFlopResult, BoardData[])` | `void` | Pobla `BoardTexture`/`HeroHandStrength`/`Draws` 🟢 |
| `IGetCardsFlopUseCase.Execute` | `(Image, ICoordinateScaler, regionMap)` | `Task<BoardData[]>` | OCR + dHash compare contra 52 cartas 🟢 |
| `IGetCardsTurnUseCase.Execute` | `(Image, ...)` | `Task<BoardData?>` | Carta 4 (Turn) 🟢 |
| `IGetCardsRiverUseCase.Execute` | `(Image, ...)` | `Task<BoardData?>` | Carta 5 (River) 🟢 |
| `IOutsCalculatorUseCase.Execute` | `(List<CardDataOuts>, List<CardDataOuts>)` | `HandStrength` con 12 tipos draw | Wrapper sobre `OutsCalculator` con probabilidad combinatoria 🟢 |
| `IPotOddsCalculator.Execute` | `(HandStrength, decimal pot, decimal call)` | `decimal potOddsPct` | Wrapper legacy con regla 4-2 🟢 |
| `IGetHashImageUseCase.Execute` | `(Bitmap)` | `ulong dHash` | Compute perceptual hash 64-bit 🟢 |
| `IGetCropImageUseCase.Execute` | `(Image, Region)` | `string base64` | Crop + base64 + cache `WeakReference<Image>` 🟢 |
| `ILoadTableMapUseCase.Execute` | `(string mapName)` | `Task<List<RegionTableMap>>` | Wrapper sobre `RegionTableMapUseCases.LoadAsync` 🟢 |
| `ISaveTableMapUseCase.Execute` | `(List<RegionTableMap>)` | `Task` | Persistir tableMap con Marten 🟢 |
| `ISetMovementRegionUseCase.Execute` | `(string regionName, int dx, int dy)` | `Region` | Mover una región interactivamente 🟢 |

### 3. Forms — superficie pública (consumida por el composition root)

| Símbolo | Asignatura | Observación |
|---------|-----------|-------------|
| `FrmMain` (ctor) | 44 dependencias inyectadas | `[STAThread]` resuelto desde `CreateAsyncScope` 🟢 (`Program.cs:213-222`) |
| `FrmMain.btnCapture_Click` | `(sender, EventArgs)` | Pipeline ~270 LOC: capture → OCR → layout → preflop/postflop → overlay → telemetry 🟢 (`Forms/FrmMain.cs:642-821`) |
| `FrmMain.BackgroundWorker1_DoWork` | `(sender, DoWorkEventArgs)` | Loop principal: invoca `btnCapture_Click` cada `CaptureIntervalMs=100` ms 🟢 (`Forms/FrmMain.cs:2737-2897`) |
| `FrmOverlay.UpdateAction(string)` / `UpdateEquityPercentage(string)` / etc. | `IFrmOverlay` | API consumida desde `FrmMain` y `UiSyncService` 🟢 |
| `FrmHandDetail.LoadHand(HandRecord)` | `(HandRecord)` | Renderiza Hand History con `RichTextBox.AppendText` color-coded 🟢 |
| `FrmDetectionDebug.Show(Region)` | `(Region)` | Calibración OCR zoomed con `ZOOM_FACTOR=10` 🟢 |
| `FormImage.IAddImage.Execute(IntPtr handle)` | `(IntPtr)` | Visor PNG con navegación ←→ 🟡 |
| `FormListApps` | `(IAddImage)` | Lista ventanas filtrando por `"NL H"` 🟡 |

---

## Fluxo Principal

> El ciclo de captura es el corazón del módulo. Se ejecuta desde `BackgroundWorker1_DoWork` cada 100 ms (configurable) e invoca a `btnCapture_Click` que es el orquestador de ~270 LOC.

```
BackgroundWorker1_DoWork (loop cada CaptureIntervalMs=100ms)
    └─> btnCapture_Click(sender, e)
          ├─ using _cycleTimer = _metrics.Measure("Cycle.Total")
          ├─ Limpiar UI (lbAction, FrmOverlay)
          ├─ if (!cbTest.Checked):
          │     using _captureTimer = _metrics.Measure("Capture.Screenshot")
          │     await GetImageWhilePlaying()  ──> _formImage.pbImage.Image
          │
          ├─ var handBefore = _tableHand
          ├─ await SetTableHand()  ──> OCR hand number, set _tableHand y _newHand
          │
          ├─ if (_newHand && !isTestPostflop):
          │     ├─ Snapshot: prevPot, prevHoleCards, prevPosition
          │     ├─ prevHeroStack = _heroStackPreRebuy ?? _playerGameState.HeroStack  // R5 auto-rebuy
          │     ├─ if (sameHand && IsPostflop):
          │     │     savedPostflopState = currentState; savedBoardCards = ...
          │     ├─ _frmOverlay.ClearAll()
          │     ├─ _playerGameState = new(); _heroStackPreRebuy = 0
          │     ├─ _gameLoopStateMachine.Reset() → TryTransition(HandDetected)
          │     ├─ await HandleNewHandAsync(...)  ──> GameLoggerService.StartNewHandAsync
          │     ├─ _contextHolder.StartNewHand()
          │     └─ if (savedPostflopState) ForceState + restore boardCards
          │
          ├─ if (cbTest.Checked) ForceState(FlopDetected/TurnDetected/RiverDetected)
          │
          ├─ // Detección de jugadores
          ├─ if (needsInitialization):
          │     SetEmptyPlayer + SetSitOutPlayer + SetActivePlayer + InitializePlayers
          │     (asigna posiciones, alias, dealer)
          ├─ else:
          │     SetEmptyPlayer + SetActivePlayer + RefreshPlayerStates
          │     if (Position==None || playerCountChanged):
          │         SetDealerPlayer + SetVillainPosition  // recalcular layout
          │
          ├─ SetBetPlayer()        // OCR de bets activas
          ├─ SetHeroStack()        // OCR del stack hero (con auto-rebuy detection)
          ├─ RetryEmptyAliases()   // reintento OCR para nombres no leídos
          ├─ ValidatePlayerStates()
          │
          ├─ await ProcessTableInfoAsync(potOddsResult)
          │       ├─ if (IsPreflop) await ProcessPreflopAsync()
          │       │   └─ SetPreflopActionUseCase.Execute → ResponseAction
          │       └─ else await ProcessPostFlopAsync(potOddsResult)
          │           ├─ if (IsBoardCardVisible("Card4")) TryTransition(TurnDetected)
          │           ├─ if (IsBoardCardVisible("Card5")) TryTransition(RiverDetected)
          │           ├─ GetCardsFlop/Turn/RiverUseCase  ──> BoardData[]
          │           ├─ using _equityTimer = _metrics.Measure("Decision.Equity")
          │           ├─ _pokerCalculator.Calculate(...) ──> PokerCalculationResult
          │           ├─ using _decisionTimer = _metrics.Measure("Decision.DecisionService")
          │           ├─ _coordinator.DetermineFlopAction/Turn/River(...) ──> action
          │           ├─ action = _coordinator.AdjustBetSize(action, stack, pot, ...)
          │           └─ action = _actionFormatter.EnrichActionWithBBAmount(action, ...)
          │
          ├─ UpdateUIWithResults(potOddsResult)
          │       ├─ UpdatePlayerIndicators()
          │       ├─ UpdateOverlayWithPotOdds(...)
          │       ├─ UpdateResumeTextForPreflop/Flop()
          │       └─ _frmOverlay.UpdateAction(action) + UpdateBoardTexture(...)
          │
          ├─ using _overlayTimer = _metrics.Measure("Overlay.Render")
          ├─ telemetry = _metrics.EndHand()
          └─ if (telemetry != null) _gameLoggerService.SetTelemetry(telemetry)
```

🟢 Pipeline trazado en `src/OpenScrape.App/Forms/FrmMain.cs:642-821,1104-1198,1198-1763`.

---

## Fluxos Alternativos

- **Modo Test (cbTest.Checked):** salta la captura real, usa `_formImage.pbImage.Image` ya cargado en `FormImage` y fuerza el state machine a `FlopDetected`/`TurnDetected`/`RiverDetected` según el `RadioButton` activo (`rbFlop`/`rbTurn`/`rbRiver`). Sigue ejecutando `ProcessTableInfoAsync` para validar la decisión sin captura. 🟢 (`Forms/FrmMain.cs:669-756`)
- **Misma mano con cambio de calle (sameHand && IsPostflop):** preserva el `GameState` y los `BoardCards` antes del reset y usa `_gameLoopStateMachine.ForceState(savedState)` para restaurar — porque `HandDetected → FlopDetected/Turn/River` no es una transición válida en `_validTransitions`. 🟢 (`Forms/FrmMain.cs:696-740`)
- **Detección de nueva mano por consenso (`DetectNewHand`):** combina 7 indicadores (handNumberChanged, hole cards presentes, pot < 10, board vacío, dealer cambió, SB cambió, BB cambió). Si `handNumberChanged` requiere ≥1 indicador secundario; sin él requiere ≥3. Persiste `previousDealer/SB/BB` por referencia. 🟢 (`Services/GameCoordinator.cs:297-?`)
- **Auto-rebuy (R5/R11):** durante la mano activa `_heroStackPreRebuy` se actualiza con el stack actual; cuando la siguiente mano arranca y el stack subió bruscamente (≈100 BB), `EndHand` usa `_heroStackPreRebuy` en lugar del valor actual para calcular `Profit = stackPreRebuy - stackStart`. 🟢 (`Forms/FrmMain.cs:99-105,690`)
- **Reintento de transición en game loop:** si `TryTransition(state, visibleBoardCards)` retorna false (cartas insuficientes), el ciclo siguiente reintenta. La cuenta `MaxOcrRetries=2` aplica al OCR previo, no al state machine. 🟢 (`Services/GameLoopStateMachine.cs:73-110,165`)
- **Postflop reentry mid-street (villain raise):** si `IsBoardCardVisible("Card4"/"Card5")` retorna false en `ProcessPostFlopAsync` y ya estamos en `FlopAction`/`TurnAction`, se reprocesa la calle actual con `VillainBetSize` actualizado (BF3 — `MEMORY.md`). 🟢 (`Forms/FrmMain.cs:1198-1500`)
- **Cambio de mesa (StartSessionAsync):** si llega un `sessionId` distinto, persiste la sesión anterior con `await SaveSessionAsync()` antes de reiniciar contadores. Si la persistencia falla, captura excepción y continúa la nueva sesión sin perder el flujo (la sesión anterior queda parcialmente guardada en disk). 🟡 (`Services/GameLoggerService.cs:50-55`)
- **Fail-fast del `StrategyProfile`:** si `Validate` lanza `StrategyProfileValidationException`, se muestra `MessageBox` con el listado completo de errores y se llama a `Environment.Exit(1)`. La aplicación no muestra `FrmMain`. 🟢 (`Program.cs:187-197`)
- **`FrmMain_FormClosing`:** Si `_gameLoopCoordinator.IsRunning`, llama a `_uiSyncService.Detach()` + `_gameLoopCts?.Cancel()` + `_gameLoopCoordinator.StopAsync()` antes de cerrar. Persiste la mano y sesión activas con `await`. 🟢 (`Forms/FrmMain.cs:304-?`)
- **Cutover futuro al `GameLoopCoordinator`:** controlado por `FeatureFlags.UseGameLoopCoordinator`. Activación leerá `IGameLoopCoordinator.ResultReady` desde `UiSyncService.Attach(this)` que hará `BeginInvoke` para actualizar overlay sin tocar el `BackgroundWorker`. **Inactivo en producción.** 🟡 (`Services/GameLoopCoordinator.cs`, `Services/UiSyncService.cs`, `Configuration/FeatureFlags.cs`)
- **`FormImage` con path hardcoded:** Visor PNG navegable con flechas ←→ que apunta a `C:\Code\Poker\ScrapePoker\resources\Games`. Anomalía conocida — comentario `//portatil` indica deuda técnica y dependencia de la máquina de desarrollo. 🔴 (`Forms/FormImage.cs`)

---

## Dependências

### Externas (NuGet) — solo las que usa OpenScrape.App directamente

- **`Microsoft.Extensions.Hosting 10.0.3`** — `Host.CreateDefaultBuilder` para DI + `IConfiguration` + `ILogger`. (`Program.cs:43`)
- **`Microsoft.Extensions.{Configuration,Logging,Options} 10.0.3`** — bindings de `appsettings.json`, scopes de logging, `IOptions<T>` para `StrategyProfile`/`OverlayConfig`/`FeatureFlags`/`GameLoopOptions`. (`Program.cs:5,62`)
- **`Marten 8.24.0`** — único punto de acceso al document store de PostgreSQL. Consumido por `GameLoggerService`, `BankrollTrackerService` (vía DI factory en `Program.cs:107-113`), `CardCacheService`, `LoadTableMapUseCase`, `SaveTableMapUseCase`. (`OpenScrape.App.csproj`)
- **`Tesseract 5.x`** — OCR engine. Encapsulado por `OcrService` con `lock` global porque es single-threaded. (`Services/OcrService.cs:33`)
- **`OpenCvSharp4 4.x`** — preprocesamiento de imágenes (deskew, contraste, binarización). Usado por `ImagePreprocessorHelper`. (`Helpers/ImagePreprocessorHelper.cs`)
- **`SkiaSharp 2.x`** — manipulación de bitmaps (alternativa a `System.Drawing` en algunos paths). Usado por `OcrService` (cache `LruCache<string, SKBitmap>`) y `ColorDetectionService`. (`Services/OcrService.cs:1`, `Services/ColorDetectionService.cs`)

### Internas (proyectos del solution)

- **`OpenScrape.Domain`** — DTOs, value objects, enums, mappers. Cero dependencias upstream. Consumido por todos los servicios.
- **`OpenScrape.DecisionMaker`** — algoritmos puros (`MonteCarloSimulator`, `BitHandEvaluator`, `OutsCalculator`, `BoardTextureAnalyzer`, `PreflopEquityCalculator`) y servicios (`PostflopDecisionService`, `OpponentTracker`, `BetSizingService`, etc.). Inyectados como singletons + forwarding `interfaz → concreta`.
- **`OpenScrape.Features`** — use cases scoped: `Table/`, `Cards/`, `ActionScenario/`, `RegionsTableMap/`, `GameRound/`. Composición vía `services.AddUseCases()` (`Program.cs:53`).
- **`OpenScrape.Infrastructure`** — `services.AddDataBase(config, true)` configura el `IDocumentStore` de Marten (índices, esquema, plugins). (`Program.cs:52`)

### Plataforma (P-Invoke)

- **`User32.dll`** — `EnumWindows`, `GetWindowText`, `GetWindow`, `GetWindowRect`, `IsWindowVisible`, `SetProcessDPIAware`, `PrintWindow`, `WM_NCLBUTTONDOWN`. Encapsulado en `Helpers/CaptureWindowsHelper.cs:User32` y `WindowsInformationHelper`.
- **`GDI32.dll`** — `BitBlt`, `CreateCompatibleDC`, `CreateCompatibleBitmap`, `SelectObject`, `DeleteDC`, `DeleteObject`. Fallback de captura cuando `PrintWindow` falla.

---

## Decisões de Design Identificadas

| Decisión | Evidencia en el código | Confianza |
|----------|-----------------------|-----------|
| **`FrmMain` resuelto desde `CreateAsyncScope`, no del root.** Sus dependencias scoped (`GameCoordinator`, `TableLayoutService`, `GameLoggerService`, `PostflopContextHolder`, `SetPreflopActionUseCase`, `GameLoopCoordinator`, `UiSyncService`, `PokerDecisionFacade`) viven todo el ciclo de la ventana | `Program.cs:213-222`, ADR-0014 | 🟢 |
| **Forwarding pattern para algoritmos del DecisionMaker.** Cada algoritmo registra concreta + interfaz como singleton compartiendo instancia (`AddSingleton<IFace>(sp => sp.GetRequiredService<Concrete>())`) | `Program.cs:71-113`, ADR-0006 | 🟢 |
| **Fail-fast del `StrategyProfile` antes de mostrar UI.** `StrategyProfileValidator.Validate` aborta con `Environment.Exit(1)` si los thresholds son inválidos | `Program.cs:187-197`, ADR-0008 | 🟢 |
| **`UnifiedPokerCalculator` ES el facade real.** A pesar del `PokerDecisionFacade` planeado para cutover, el código de producción inyecta `IPokerCalculator` (que apunta a `UnifiedPokerCalculator`). El facade está scoped pero no consumido | `Program.cs:116`, `Aplication/UseCases/UnifiedPokerCalculator.cs:46-78`, ADR-0006 | 🟢 |
| **Cache equity con `ConcurrentDictionary` y FIFO de inserción.** Clave: `"hand|board|numOpp|situation"`. Tope `EquityCacheMaxSize=2048` — al llegar al límite se descarta el primero insertado (no LRU real) | `Aplication/UseCases/UnifiedPokerCalculator.cs:59-60` | 🟢 |
| **Cache OCR bicapa con `dHash` 64-bit.** Bitmap (200) + texto (500). Hamming ≤15 para considerar match. `LruCache` thread-safe con `lock` | `Services/OcrService.cs:13-18`, `Services/ImageCropperService.cs` | 🟢 |
| **Tesseract single-threaded protegido con `lock` global.** Tesseract no soporta acceso concurrente al engine | `Services/OcrService.cs:19,33`, ADR-0010 § Tesseract | 🟢 |
| **`PostflopGameContext` inmutable + `Holder` thread-safe scoped.** El `record` no muta — `Update(Func<T,T>)` produce nueva instancia bajo `lock(_gate)`. `Volatile.Read` para lecturas sin contención | `Services/PostflopContextHolder.cs`, ADR-0007 | 🟢 |
| **State machine con 10 estados y `_validTransitions` declarativo.** `lock(_stateLock)` thread-safe, validación cruzada con `visibleBoardCards`, `ForceState` solo para test/debug | `Services/GameLoopStateMachine.cs:5-110,138-160`, ADR-0012 | 🟢 |
| **`RegionLookupCache` con dual dictionary O(1).** `Dictionary<mapId, Dictionary<regionName, Region>>` + `Dictionary<mapId, List<Region>>`. `OrdinalIgnoreCase` para tolerar typos de configuración | `Services/RegionLookupCache.cs`, ADR-0016 | 🟢 |
| **`CardCacheService` singleton lazy con `SemaphoreSlim` double-check.** Una sola query Marten por vida de la app — 52 cartas. `await using LightweightSession` | `Services/CardCacheService.cs`, ADR-0016 | 🟢 |
| **Auto-rebuy detection con `_heroStackPreRebuy` snapshot.** Stack se snapshotea durante la mano; subida brusca al inicio de mano (≈100 BB) NO sobrescribe el valor previo | `Forms/FrmMain.cs:99-105,690`, ADR-0013 | 🟢 |
| **`GameLoggerService` scoped con `SemaphoreSlim _dbWriteLock`.** Una mano por vez al persistir; las anteriores ya están en disk | `Services/GameLoggerService.cs:20`, ADR-0017 | 🟢 |
| **Truncado de manos en memoria a `MaxHandsInMemory=20`.** Acumuladores `_sessionTotalHands`/`_sessionTotalProfit` con `Interlocked` son la fuente de verdad para cálculos derivados | `Services/GameLoggerService.cs:15,27-28`, ADR-0017 | 🟢 |
| **Telemetría con histograma de 30 buckets logarítmicos.** `bound[i] = 1e-5 × 10^(i × 0.2)` segundos = 10µs → 6.3s. Sobrestima ~37 % nunca subestima | `Telemetry/Histogram.cs:1-88`, ADR-0017 | 🟢 |
| **17 categorías de telemetría como contrato estable.** `TelemetryCategories.DisplayOrder` y `SessionOnly` definen orden UI y exclusión del snapshot de mano | `Telemetry/TelemetryCategories.cs:8-65`, ADR-0017 | 🟢 |
| **`TextBoxLoggerProvider` one-shot con buffer FIFO.** Bufferiza pendientes hasta `SetTextBoxTarget(tb)`, registra el target permanentemente, cross-thread con `BeginInvoke` y rotación FIFO | `Services/Logging/TextBoxLoggerProvider.cs`, ADR-0009 | 🟢 |
| **`FrmOverlay` con `Color.Magenta TransparencyKey`.** Cualquier región pintada de magenta es transparente al cliente OS. P-Invoke `WM_NCLBUTTONDOWN` para drag manual | `Forms/FrmOverlay.cs`, `Forms/FormAction.cs` | 🟢 |
| **`OverlayPositioner` extraído de `FrmMain.CalculateOverlayPosition`.** Aritmética 1:1: `centerX = winLeft + winWidth/2; x = centerX - overlayW/2 - hOff; y = winBottom - vOff` | `Services/OverlayPositioner.cs` | 🟢 |
| **`ActionFormatter` extraído para parseo BB.** Lógica que transforma `"Bet 2.5x"` en `"Bet 2.5x (5.0BB)"` con `pot * 2.5 / bigBlind` por villano | `Services/ActionFormatter.cs` | 🟢 |
| **`CoordinateScaler` con factor promedio.** `scale = (currW/refW + currH/refH)/2.0`. Si nunca `Initialize`, devuelve coords sin escalar (no falla) | `Helpers/CoordinateScaler.cs:?`, ADR-0016 | 🟢 |
| **`PrintWindow PW_RENDERFULLCONTENT` con fallback `BitBlt`.** Permite captura de ventanas Chromium/DirectX en background sin requerir foco | `Helpers/CaptureWindowsHelper.cs:?`, ADR-0004 | 🟢 |
| **DPI awareness activado al inicio de cada captura.** `User32.SetProcessDPIAware` antes de `PrintWindow` para evitar escalado del SO | `Helpers/CaptureWindowsHelper.cs` | 🟢 |
| **`ImagePreprocessorHelper` con `Parallel.For` en grayscale.** Paleta + `Format8bppIndexed` + `Parallel.For` para resize ×2 + grayscale en imágenes <1000 px | `Helpers/ImagePreprocessorHelper.cs:?` | 🟢 |
| **`PokerHandEvaluator` legacy coexiste con `BitHandEvaluator`.** Brute-force C(n,5) sin justificación documentada — candidato a eliminación | `Services/PokerHandEvaluator.cs:388 LOC`, anomalía Scout | 🔴 |
| **`PokerDecisionFacade` planeado pero NO usado en producción.** `FrmMain` consume directamente `IPokerCalculator` y `IPostflopDecisionService`. Cutover Fase 2 pendiente | `Services/PokerDecisionFacade.cs`, ADR-0006 | 🟡 |
| **`GameLoopCoordinator` esqueleto bajo feature flag.** Retorna `GameLoopResult { Empty = true }` por tick. Migración Fase 4 pendiente | `Services/GameLoopCoordinator.cs`, `Configuration/FeatureFlags.cs` | 🟡 |
| **`appsettings.json` versionado contiene credenciales reales.** Anomalía crítica documentada por el Scout. Contradice ADR-0018 que mandata `appsettings.Development.json` para secrets | `appsettings.json`, ADR-0018, Scout § Anomalías | 🔴 |
| **`EncrypterHelper` AES-CBC con IV fija de 16 ceros.** Compromete confidencialidad si hay 2+ ciphertexts del mismo plaintext prefix | `Helpers/EncrypterHelper.cs`, anomalía documentada | 🔴 |
| **Dual ubicación de `eng.traineddata`.** Embebida (`Resources/tessdata/`) + copia output (`tessdata/` con `PreserveNewest`). Auto-extracción al primer arranque sin la copia output | `Services/OcrService.cs:35-54`, anomalía Scout | 🟡 |

---

## Estado Interno

### Estado por instancia de `FrmMain` (mutable, no thread-safe — accedido solo desde UI thread)

| Campo | Tipo | Inicialización | Reset |
|-------|------|----------------|-------|
| `_playerGameState` | `PlayerGameState` (DTO mutable) | `new()` en ctor | `new PlayerGameState()` al detectar nueva mano (`btnCapture_Click:711`) |
| `_scrapeFlopResult` | `TableScrapeFlopResult` | `new()` en ctor | Sobrescrito por `SetFlopForceBoardUseCase` cada captura |
| `_responseAction` | `ResponseAction` | `new()` | `new ResponseAction()` al detectar nueva mano |
| `_heroStackPreRebuy` | `decimal` | `0` | `0` al detectar nueva mano (`btnCapture_Click:713`) |
| `_previousSBPlayerName`, `_previousBBPlayerName` | `string` | `""` | Actualizados por `DetectNewHand` por referencia |
| `_lastActivePlayerCount` | `int` | `0` | Actualizado en cada iteración del loop |
| `_executeCapture`, `_backgroundExecute` | `volatile bool` | `false` | Toggle por UI buttons |
| `_tableHand` | `string` | `""` | Sobrescrito por `SetTableHand()` cada captura |
| `_newHand` | `bool` | `false` | `false` tras procesar nueva mano |
| `_handle` | `IntPtr` | `IntPtr.Zero` | Set por `FormListApps` al elegir cliente |
| `_locWindowRect` | `User32.RECT` | `new()` | Sobrescrito por `GetWindowRect` cada captura |
| `_cycleCounter` | `long` (interlocked) | `0` | Incrementado en cada `btnCapture_Click` |
| `_isClosing` | `bool` | `false` | `true` al cerrar (evita doble FormClosing) |

### Estado del state machine (`GameLoopStateMachine`, singleton)

- `CurrentState: GameState` (default `WaitingForHand`) — protegido por `lock(_stateLock)`.
- `_validTransitions: Dictionary<GameState, HashSet<GameState>>` — readonly static.
- Reset incondicional al detectar nueva mano: `Reset() → TryTransition(HandDetected)`.

### Estado del context holder (`PostflopContextHolder`, scoped)

- `Current: PostflopGameContext` (`record`) — `Volatile.Read`.
- Mutaciones vía `Update(Func<PostflopGameContext, PostflopGameContext>)` bajo `lock(_gate)`.
- `StartNewHand()` produce nueva instancia vacía bajo lock.

### Estado de la sesión persistida (`GameLoggerService`, scoped)

- `_currentSession: GameSession?` (Marten document) — null hasta `StartSessionAsync`.
- `_currentHand: HandRecord?` — null entre manos.
- `_sessionScope`, `_handScope: IDisposable?` — correlation scopes de logging.
- `_sessionTotalHands: int`, `_sessionTotalProfit: decimal` — `Interlocked` updates. Fuente de verdad porque `GameSession.Hands` está truncada a `MaxHandsInMemory=20`.
- `_dbWriteLock: SemaphoreSlim(1, 1)` — serialización de writes.

### Caches singleton (compartidas entre scopes)

- `OcrService._bitmapCache: LruCache<string, SKBitmap>` (200 entradas).
- `OcrService._ocrCache: LruCache<ulong, string>` (500 entradas, key = `dHash`).
- `UnifiedPokerCalculator._equityCache: ConcurrentDictionary<string, double>` (FIFO insertion, max 2048).
- `RegionLookupCache._regionsByName: Dictionary<mapId, Dictionary<regionName, Region>>`.
- `CardCacheService._cards: List<CardDTO>?` (lazy, una sola query Marten).
- `MetricsCollector._categories: ConcurrentDictionary<string, CategoryState>` (per-category con `Histogram LastHand` + `Histogram Session`).

### Estado del overlay (`FrmOverlay`, scoped)

- `_animationTimer: System.Windows.Forms.Timer` (fade-in).
- 9 filas en `TableLayoutPanel` + action panel — repintado bajo `BeginInvoke` cross-thread.
- `Color.Magenta TransparencyKey` — pintar magenta = transparente al SO.
- P-Invoke `WM_NCLBUTTONDOWN` para drag (no hay título).

---

## Observabilidade

### Telemetría (`MetricsCollector` + `TelemetryCategories`)

17 categorías nombradas, medidas con `using ScopedMeasurement` zero-allocation. Cada categoría mantiene dos histogramas (LastHand + Session) de 30 buckets logarítmicos.

| Categoría | Punto de medición | Fase |
|-----------|------------------|------|
| `Cycle.Total` | `btnCapture_Click` completo | Top-level |
| `Capture.Screenshot` | `GetImageWhilePlaying` | Capture |
| `OCR.Cards` | `GetCardsFlop/Turn/RiverUseCase` | OCR |
| `OCR.Bets` | `SetBetPlayer` y `ScreenReaderService.ReadBet` | OCR |
| `OCR.Stacks` | `SetHeroStack` y `ScreenReaderService.ReadStack` | OCR |
| `OCR.HandNumber` | `SetTableHand` | OCR |
| `OCR.PlayerNames` | `TableLayoutService.RetryEmptyAliases` | OCR |
| `Layout.Dealer` | `TableLayoutService.SetDealerPlayer` | Layout |
| `Layout.Positions` | `TableLayoutService.SetVillainPosition` y `RefreshPlayerStates` | Layout |
| `Decision.Total` | `ProcessTableInfoAsync` postflop completo | Decision |
| `Decision.Equity` | `_pokerCalculator.Calculate` | Decision |
| `Decision.Texture` | `_coordinator.AnalyzeBoardChange/Turn/River` | Decision |
| `Decision.Profile` | `OpponentTracker.GetProfile` | Decision |
| `Decision.DecisionService` | `_postflopDecisionService.DetermineAction` | Decision |
| `Decision.Sizing` | `_betSizingService.CalculateDynamicBetSize` | Decision |
| `Overlay.Render` | `_frmOverlay.UpdateAction` y `UpdateBoardTexture` | Render |
| `Persistence.SaveHand` (SessionOnly) | `GameLoggerService.FinalizeAndPersistHandAsync` | Persistence |

### Logs estructurados (`ILogger`)

- **Sink primario:** `TextBoxLogger` → `tbResume` (pestaña Logs). Formato `[HH:mm:ss LVL CategoryShort] {Scopes} message`.
- **Sink secundario:** consola estándar (visible al lanzar desde Visual Studio).
- **Correlation scopes:** `BeginScope({ SessionId, TableName })` (sesión) y `BeginScope({ HandNumber })` (mano). Se propagan a todos los logs del scope.
- **Niveles:** `Trace` (TRC) → `Critical` (CRT). Configurable por `TextBoxLoggerOptions.MinimumLevel`.
- **Buffer one-shot:** mensajes pre-`SetTextBoxTarget` se almacenan en cola y se flushean al primer `Set`.

### Bloques de Hand History en logs

`tbResume` muestra estructura por calle:

```
═══ [FLOP] ═══
Hero: [As Kh]  Board: [Qd 7c 2s]
Equity: 67.3%, PotOdds: 25.0%, EV: +1.2BB
HandRank: OnePair (TopPair, TPTK, KickerStrength=Strong)
BoardTexture: SemiDry (wetness=22, FlushDrawAppeared=false)
Draws: BackdoorFlush(1.5), BackdoorStraight(0)
Decision: Bet 1/2 [VALUE]
═══ [TURN] ═══
...
```

Tags de decisión: `[VALUE]`, `[CHECK-RAISE]`, `[BLUFF]`, `[BARREL]`, `[FLOAT-EXIT]`, `[PROBE]`, `[DELAYED-VALUE]`, `[POT-CONTROL]`. Útil para auditar decisiones individuales.

### `DetectionLoggerService` (sink JSON file-based)

Logger separado para detección de turnos (color, dealer, OCR de cartas). Archivo JSON 1/día en `resources/logs/`. Métodos: `LogColorDetection`, `LogTurnDetected`, `LogConfigurationChange`, `LogDetectionStatistics`, `SaveDebugScreenshot` (con cruz roja en punto detectado), `CleanupOldLogs` (>30 días). 🟢 (`Services/DetectionLoggerService.cs`)

---

## Riscos y Lacunas

### Críticos (🔴)

- **Credenciales reales en `appsettings.json` versionado.** Anomalía Scout #1 — connection string PostgreSQL completa y `EncryptionKey` están comprometidas. Cualquiera con acceso al repo puede consultar la BD de producción del usuario. Mitigación: rotar credenciales, mover a `appsettings.Development.json` o `User Secrets`. Ver ADR-0018.
- **`EncrypterHelper` con IV fija de 16 ceros.** Aunque el uso actual es limitado (cifrado de datos persistidos pre-Marten), el patrón AES-CBC con IV fija es vulnerable a ataques de prefijo conocido si se reusa la clave. Validar si `EncrypterHelper` está en el camino crítico actual o es código muerto.
- **Anomalía dual de `eng.traineddata`.** La traineddata vive como recurso embebido (~30 MB) Y como archivo `PreserveNewest` en output. Aumenta el tamaño del binario y permite que las dos copias divergan en versiones de Tesseract. Decidir cuál es la fuente de verdad y eliminar la otra. Ver legacy-mapping § anomalías.
- **`FormImage` con path hardcoded.** `C:\Code\Poker\ScrapePoker\resources\Games` con comentario `//portatil` indica una dependencia de la máquina de desarrollo. Bloquea redistribución y portabilidad.

### Importantes (🟡)

- **`FrmMain` 4 502 LOC viola SRP.** Refactor Fase 7 pendiente (decision pipeline, OCR helpers, calibration UI deberían separarse en partial classes o forms hijos). El refactor previo logró 5 977 → 4 502 LOC pero queda mucho por cortar.
- **`PokerDecisionFacade` muerto en producción.** Está registrado como `Scoped` y mide 5 fases con telemetría, pero **ningún consumidor lo inyecta**. `FrmMain` consume directo `IPokerCalculator` y `IPostflopDecisionService`. Riesgo: la telemetría que el equipo cree que mide desde el facade en realidad se mide en otro lado. Ver ADR-0006.
- **`GameLoopCoordinator` inactivo bajo feature flag.** El esqueleto está completo pero retorna `Empty=true` por tick. Migrar el `BackgroundWorker` a `PeriodicTimer` requiere cuidar el cleanup de `_gameLoopCts` y los `BeginInvoke` cross-thread.
- **`PokerHandEvaluator` legacy duplicado.** 388 LOC de brute-force C(n,5) sin justificación. `BitHandEvaluator` del módulo DecisionMaker hace lo mismo zero-alloc. Eliminación pendiente.
- **`ScreenReaderService.NormalizeBetValue` con artefacto "8".** Heurística regex que detecta el carácter "8" insertado por OCR cuando hay ruido visual. Frágil — si Tesseract cambia el patrón de error, la heurística falla silenciosa. Recomendado: añadir test de regresión con casos reales del log.
- **Dependencia opaca en `FrmMain` con 44 inyecciones.** El constructor consume 44 servicios — dificulta tests unitarios y aumenta superficie de cambios. Refactor Fase 7 pretende reducirlo a <15 vía mediator pattern.
- **`appsettings.json` con ~150 parámetros del `StrategyProfile`.** El fail-fast valida la mayoría, pero la sintaxis de keys (`"Flop_OpenRaise"` etc.) es propensa a typo. Documentar en `_reversa_sdd/configuration.md` (pendiente).
- **`TableLayoutService.RefreshPlayerStates` infiere folded por desaparición de bets.** No lee texto "FOLD" — si la sala redibuja la mesa con timing distinto, podría confundirse. Validar con OCR adicional del estado del seat.

### Lacunas de validación humana (🔴)

- **¿Cuál es el SLO real de `Cycle.Total`?** El histograma sobrestima ~37 %; necesitamos un benchmark contra un cliente real para fijar p50/p95 objetivo.
- **¿Cuál es el OCR cache hit ratio en sesiones largas?** Inferimos >60 % por la naturaleza repetitiva de las cartas, pero falta medición.
- **¿`PrintWindow PW_RENDERFULLCONTENT` funciona en TODOS los clientes soportados?** Validado en el cliente actual, pero salas con tecnologías nuevas (Chromium 120+, WebView2) podrían requerir `BitBlt` fallback más frecuentemente.
- **¿Cuántos jugadores soporta el dealer detection con color RGB 200/140/80 dorado?** Asumimos 9-max, pero salas con dealer button distinto fallan silenciosamente.
- **¿Cuál es el threshold real para detectar auto-rebuy?** El código asume "subida brusca" sin un `delta` explícito. Documentar el valor (Sprint reciente fijó ≈50 BB, validar contra log).

### Lacunas técnicas (preguntas para el Revisor)

- **`PokerDecisionFacade` vs `UnifiedPokerCalculator`:** ¿se mantienen ambos por compatibilidad o se elimina uno?
- **Estrategia de cutover a `GameLoopCoordinator`:** ¿con qué tests de regresión se valida la migración?
- **Persistencia de `appsettings.json` con credenciales reales:** ¿se migra el repositorio a un fork limpio o se mantiene el histórico?
- **Filtrado `"NL H"` global en `FormListApps`:** ¿se vuelve configurable o solo soportamos NL Hold'em?
