# Legacy Mapping — `OpenScrape.App`

> Mapeo de archivos del módulo legado a las specs generadas.
> Granularidad: módulo (per `[specs] granularity = "module"` en `.reversa/config.toml`).
> Generado por el Arqueólogo del Reversa.

## Estructura de archivos del legado

### Composition root y configuración

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Program.cs` | clase static | 224 | Composition root: STA Main, Host builder, ~50 registros DI, fail-fast `StrategyProfileValidator`, `CreateAsyncScope` con FrmMain, init `CoordinateScaler` desde config |
| `src/OpenScrape.App/Configuration/FeatureFlags.cs` | sealed clase | 9 | `UseGameLoopCoordinator: bool = false` (cutover Fase 6) |
| `src/OpenScrape.App/Configuration/GameLoopOptions.cs` | sealed clase | 11 | `CaptureIntervalMs=100`, `StopTimeoutMs=2000` |
| `src/OpenScrape.App/appsettings.json` | JSON | 31485 bytes | Strategy profile (~150 params) + secciones. **Anomalía:** credenciales reales comprometidas (Scout) |
| `src/OpenScrape.App/appsettings.Development.json` | JSON | 310 bytes | Override development (gitignored) |
| `src/OpenScrape.App/Properties/launchSettings.json` | JSON | — | `DOTNET_ENVIRONMENT=Development` para Visual Studio |
| `src/OpenScrape.App/OpenScrape.App.csproj` | XML | — | net10.0-windows, paquetes NuGet |

### `Forms/` (7 forms + 6 designers + 4 .resx)

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Forms/FrmMain.cs` | partial Form + IDisposable | **4502** | God class: 5 tabs (Juego/Config/Tablas/Logs/Historial+Bankroll+Métricas), pipeline `btnCapture_Click` ~270 LOC, `BackgroundWorker1_DoWork` polling, 95+ métodos, 44 dependencias inyectadas. **Anomalía crítica:** SRP violado, refactor pendiente Fase 7 |
| `src/OpenScrape.App/Forms/FrmMain.Designer.cs` | partial Form designer | — | Generado WinForms |
| `src/OpenScrape.App/Forms/FrmOverlay.cs` | partial Form + `IFrmOverlay` | 561 | Overlay flotante con `TableLayoutPanel` 9 filas + action panel + animation timer fade-in. Color `Magenta TransparencyKey`. Bordes redondeados con `GraphicsPath`. P-Invoke `WM_NCLBUTTONDOWN` para drag |
| `src/OpenScrape.App/Forms/FrmOverlay.Designer.cs` | partial designer | — | Generado |
| `src/OpenScrape.App/Forms/FrmHandDetail.cs` | Form sin .Designer | 135 | Popup con `RichTextBox` coloreado para Hand History. Formatea con `Color`-encoded `Append`, separadores `═══`, situación, decisiones por street, resultado |
| `src/OpenScrape.App/Forms/FrmDetectionDebug.cs` | partial Form | 366 | Calibración OCR: `Timer 100ms`, captura zoomed `ZOOM_FACTOR=10`, controles numéricos `numX/numY` para ajustar coords, save/load colors |
| `src/OpenScrape.App/Forms/FrmDetectionDebug.Designer.cs` | partial designer | — | Generado |
| `src/OpenScrape.App/Forms/FormImage.cs` | partial Form + `IAddImage` | — | Visor PNG navegable (←→). **Anomalía:** path hardcoded `C:\Code\Poker\ScrapePoker\resources\Games` y comentario `//portatil` |
| `src/OpenScrape.App/Forms/FormImage.Designer.cs` | partial designer | — | Generado |
| `src/OpenScrape.App/Forms/FormAction.cs` | partial Form | 41 | Popup simple con `lbAction.Text = DatoRecibido`. Usa `Color.Magenta TransparencyKey` |
| `src/OpenScrape.App/Forms/FormAction.Designer.cs` | partial designer | — | Generado |
| `src/OpenScrape.App/Forms/FormListApps.cs` | partial Form | 62 | Lista ventanas filtrando "NL H" (clientes poker NL Hold'em) via `WindowsInformationHelper.FindWindows`. Devuelve `IntPtr handle` al `IAddImage.Execute` |
| `src/OpenScrape.App/Forms/FormListApps.Designer.cs` | partial designer | — | Generado |

### `Services/` (38 archivos C# + 4 Logging)

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Services/GameCoordinator.cs` | clase + `IGameCoordinator` | **793** | Centro de decisiones postflop scoped: `DetermineFlopAction/Turn/River` (canónicos, ~150 LOC c/u), helpers `GetOpponentBetSize`/`GetActiveVillainId`/`GetVillainType`/`GetVillainStack`/`HeroBlocksTopBoardCard`/`FormatCardsForLog`/`DetectDonkBet`/`AdjustBetSize`, `AnalyzeBoardChange`/`Turn`/`River`, `DetectNewHand`. Construye `PostflopDecisionInput` (30+ campos) y delega al motor `OpenScrape.DecisionMaker` |
| `src/OpenScrape.App/Services/IGameCoordinator.cs` | interface | 64 | Contrato del coordinator |
| `src/OpenScrape.App/Services/TableLayoutService.cs` | clase + `ITableLayoutService` | **664** | Detección dealer (color RGB 200/140/80 dorado en radio 3 px), posiciones (delegado a `PositionCalculator`), aliases (`ReadPlayerName`+regex validación), estados Empty/SitOut/Active/Folded/RefreshPlayerStates. `_metrics.Measure("LayoutDealer"/"LayoutPositions")` |
| `src/OpenScrape.App/Services/ITableLayoutService.cs` | interface | 63 | Contrato layout |
| `src/OpenScrape.App/Services/ScreenReaderService.cs` | clase + `IScreenReaderService` | **521** | OCR multi-lectura por consenso (3 reads + cleanup) para player names, bets, stacks, hand numbers, text genérico. Normalización post-OCR para artefactos "8" y separadores decimales perdidos. `_metrics.Measure("Ocr*")` |
| `src/OpenScrape.App/Services/IScreenReaderService.cs` | interface | 50 | Contrato OCR |
| `src/OpenScrape.App/Services/OcrService.cs` | clase + `IDisposable` + `OcrResult` | **462** | Tesseract wrapper: dual LRU cache (bitmap 200, ocrText 500), 4 attempts (default/lower/higher/contrast), dHash 64-bit perceptual, `lock(_lock)` global (Tesseract single-threaded). Auto-extrae `eng.traineddata` embebida si no existe en `tessdata/` |
| `src/OpenScrape.App/Services/ImageCropperService.cs` | clase + `FastBitmap` | **395** | Crop+base64+compare. Pre-filtro `dHash` Hamming distance ≤ 15 antes de pixel-comparison. `CalculateSimilarity` con terminación temprana cada 32 px. `WeakReference<Image>` cache para `Base64ToImage` |
| `src/OpenScrape.App/Services/PokerHandEvaluator.cs` | sealed clase | **388** | Evaluador legacy brute-force C(n,5). Detecta Royal/Straight/4ofKind/FullHouse/Flush/Straight/3ofKind/2Pair/Pair en orden de fuerza. **Anomalía:** convive con `BitHandEvaluator` del módulo DecisionMaker sin justificación |
| `src/OpenScrape.App/Services/DetectionLoggerService.cs` | clase | **362** | Logger JSON file-based (1 archivo/día) para detección de turnos. `LogColorDetection`, `LogTurnDetected`, `LogConfigurationChange`, `LogDetectionStatistics`, `SaveDebugScreenshot` (con cruz roja en punto detectado), `CleanupOldLogs` (>30 días) |
| `src/OpenScrape.App/Services/GameLoggerService.cs` | clase | **374** | Lifecycle de sesiones/manos en Marten: `StartSessionAsync` (correlation scope `SessionId`+`TableName`), `StartNewHandAsync` (`HandNumber` scope), `LogStreetDecision`, `UpdateBoard`/`PotSize`/`Situation`, `EndHand` (calcula Won/Lost/Push), `FinalizeAndPersistHandAsync` con `SemaphoreSlim _dbWriteLock`, `MaxHandsInMemory=20` truncado, `_sessionTotalHands`/`_sessionTotalProfit` con `Interlocked` |
| `src/OpenScrape.App/Services/PokerDecisionFacade.cs` | sealed clase + `IPokerDecisionFacade` | **231** | Facade 5 fases medidas: equity → texture → profile → decision → sizing. Planeada como cutover, **no usado en producción** (FrmMain consume directamente `IPokerCalculator` y `IPostflopDecisionService`) |
| `src/OpenScrape.App/Services/IPokerDecisionFacade.cs` | interface | 24 | Contrato facade |
| `src/OpenScrape.App/Services/GameLoopCoordinator.cs` | sealed clase + `IGameLoopCoordinator` + `IAsyncDisposable` | **213** | Esqueleto del game loop: `SemaphoreSlim` start/stop, `PeriodicTimer` configurable, evento `ResultReady`, `SafeEmit` con catch por suscriptor. **Status:** feature flag OFF, retorna `GameLoopResult { Empty = true }` por tick. Migración Fase 4 pendiente |
| `src/OpenScrape.App/Services/IGameLoopCoordinator.cs` | interface + `IAsyncDisposable` | 31 | Contrato loop |
| `src/OpenScrape.App/Services/GameLoopStateMachine.cs` | clase + enum `GameState` | **166** | 10 estados (`WaitingForHand`→`HandDetected`→`PreflopAction`→`Flop/Turn/River*Detected/Action`→`HandComplete`). Diccionario `_validTransitions`. `lock(_stateLock)` thread-safe. `TryTransition` con sobrecarga validando `visibleBoardCards` (≥3 flop, ≥4 turn, ≥5 river). `ForceState` para test/debug. `MaxOcrRetries=2` |
| `src/OpenScrape.App/Services/UiSyncService.cs` | sealed clase + `IUiSyncService` | **130** | `BeginInvoke` cross-thread router: `GameLoopResult` → overlay updates. `Attach`/`Detach` lock-protected |
| `src/OpenScrape.App/Services/IUiSyncService.cs` | interface | 30 | Contrato sync |
| `src/OpenScrape.App/Services/NullUiSyncService.cs` | clase | 18 | No-op para tests |
| `src/OpenScrape.App/Services/PostflopContextHolder.cs` | sealed clase + `IPostflopContextHolder` | **35** | Wrapper thread-safe sobre `PostflopGameContext`: `Volatile.Read` para reads, `lock(_gate)` para `Update(Func<T,T>)` y `StartNewHand` |
| `src/OpenScrape.App/Services/IPostflopContextHolder.cs` | interface | 20 | Contrato holder |
| `src/OpenScrape.App/Services/StrategyProfileValidator.cs` | static clase | **97** | Fail-fast en arranque: valida ~30 thresholds requeridos (Flop/Turn/River × 12 situaciones), rangos `[0,100]`, orden tiers `FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove`. Acumula errores en `StrategyProfileValidationException` |
| `src/OpenScrape.App/Services/StrategyProfileService.cs` | clase | **33** | Wrapper sobre `IOptions<StrategyProfile>`. `GetBluffFrequency(BoardPosition)` switch para Flop/Turn/River |
| `src/OpenScrape.App/Services/RegionLookupCache.cs` | clase | **64** | O(1) lookup: `Dictionary<mapId, Dictionary<regionName, Region>>` + `Dictionary<mapId, List<Region>>`. `OrdinalIgnoreCase`. `Initialize(maps)` reconstruye |
| `src/OpenScrape.App/Services/CardCacheService.cs` | primary ctor clase | **44** | Singleton lazy: `SemaphoreSlim` double-check, `await using session = LightweightSession`, query 52 `Card` → `List<CardDTO>`. Una sola query por vida de la app |
| `src/OpenScrape.App/Services/ColorDetectionService.cs` | clase + `Dispose` | **63** | LockBits 32bppArgb con `GCHandle.Pinned`, cache last-image-only. `Marshal.Copy` Scan0 a `_pixelData`. `GetPixelColor(image, x, y) → SKColor` BGRA |
| `src/OpenScrape.App/Services/PositionCalculator.cs` | static clase | **114** | `AssignAllPositions(dealerPos, players)` core: heads-up SB/BB, moving blinds (saltan SitOut consecutivos a la izquierda), labels Early/Middle/CutOff por `effectiveCount` |
| `src/OpenScrape.App/Services/CoordinateScaler.cs` (en Helpers/) | clase + `ICoordinateScaler` | 45 | `Initialize(refW, refH)` solo una vez. `ScaleRegion(posX, posY, w, h, currentW, currentH)`: `scale = (currentW/refW + currentH/refH)/2.0`. Si no init, devuelve coords sin escalar |
| `src/OpenScrape.App/Services/ICoordinateScaler.cs` | interface | 13 | Contrato escalador |
| `src/OpenScrape.App/Services/IFrmOverlay.cs` | interface | 28 | Contrato overlay (UpdateAction/Equity/PotOdds/Phase/Indicator/Texture/...) |
| `src/OpenScrape.App/Services/OverlayPositioner.cs` | sealed clase + `IOverlayPositioner` | **31** | Calcula posición overlay: `centerX = winLeft + (winWidth/2)`, `x = centerX - (overlayW/2) - horizontalOffset`, `y = winBottom - verticalOffset`. Aritmética 1:1 de `FrmMain.CalculateOverlayPosition` extraída |
| `src/OpenScrape.App/Services/IOverlayPositioner.cs` | interface | 22 | Contrato positioner |
| `src/OpenScrape.App/Services/ActionFormatter.cs` | sealed clase + `IActionFormatter` | **49** | `EnrichActionWithBBAmount(action, villains, bigBlind)`: parsea `Bet 2.5x` → calcula totalBB y agrega `(N.NBB)`. Lógica 1:1 extraída de FrmMain |
| `src/OpenScrape.App/Services/IActionFormatter.cs` | interface | 22 | Contrato formatter |
| `src/OpenScrape.App/Services/LruCache.cs` | internal sealed clase generic | **111** | `LruCache<TKey, TValue>` thread-safe (`lock`). `Dictionary` + `LinkedList`. `TryGet/Set/GetOrAdd`. Eviction `LRU` por capacidad. `Values` retorna snapshot |
| `src/OpenScrape.App/Services/Logging/TextBoxLogger.cs` | sealed clase + `ILogger` | **120** | Render `[HH:mm:ss LVL CategoryShort] {Scopes} message | ExceptionType: msg`. Scopes via `IExternalScopeProvider`. `LevelToken` map (TRC/DBG/INF/WRN/ERR/CRT) |
| `src/OpenScrape.App/Services/Logging/TextBoxLoggerProvider.cs` | sealed clase + `ILoggerProvider` + `ISupportExternalScope` | **138** | `[ProviderAlias("TextBox")]`. Buffer pendientes hasta `SetTextBoxTarget` (one-shot). `BeginInvoke` cross-thread + `AppendWithRotation` por `MaxLines` (FIFO) |
| `src/OpenScrape.App/Services/Logging/TextBoxLoggerOptions.cs` | clase | — | `MinimumLevel`, `MaxLines`, `BufferUntilTargetReady`, `BufferCapacity` |
| `src/OpenScrape.App/Services/Logging/TextBoxLoggerExtensions.cs` | static clase | — | `AddTextBoxLogger(this ILoggingBuilder)` extension |
| `src/OpenScrape.App/Services/GameLoopResult.cs` | record | **39** | DTO emitido por `GameLoopCoordinator.ResultReady`: Empty/Error/Street/RecommendedAction/DecisionResult/LogText |

### `Telemetry/`

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Telemetry/IMetricsCollector.cs` | interface | **58** | Contrato: `Measure`, `Record`, `MeasureSessionOnly`, `RecordSessionOnly`, `StartHand`, `EndHand`, `SnapshotSession`, `ResetSession` |
| `src/OpenScrape.App/Telemetry/MetricsCollector.cs` | sealed clase | **149** | Thread-safe per-category. `ConcurrentDictionary<string, CategoryState>`. `CategoryState` con `Lock`+`Histogram LastHand`+`Histogram Session`. `EndHand` excluye `TelemetryCategories.SessionOnly`. `StartHand` con mano pendiente loggea warning |
| `src/OpenScrape.App/Telemetry/Histogram.cs` | sealed clase | **88** | 30 buckets logarítmicos `bound[i] = 1e-5 × 10^(i × 0.2)` segundos (10μs → 6.3s). `Add` O(30) lineal, `GetPercentile` O(30) acumulado. **Sobrestima ~37% nunca subestima**. NO thread-safe (sync externo) |
| `src/OpenScrape.App/Telemetry/MetricsSnapshot.cs` | sealed record | **11** | Vista inmutable `(CurrentHandId, LastHand: IReadOnlyDictionary, Session: IReadOnlyDictionary)` |
| `src/OpenScrape.App/Telemetry/ScopedMeasurement.cs` | readonly struct + `IDisposable` | **36** | Zero-allocation. `Stopwatch.GetTimestamp` start/end. Distingue `sessionOnly` para `Persistence.SaveHand` |
| `src/OpenScrape.App/Telemetry/TelemetryCategories.cs` | static clase | **65** | Contrato estable (no renombrar sin migración): 16 const + `DisplayOrder` (orden UI) + `SessionOnly` HashSet |

### `Aplication/UseCases/` y `Aplication/`

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` | clase + `IPokerCalculator` + `PokerCalculationResult` | **476** | **Fachada principal del motor de decisión usada hoy en producción**. 8 pasos: pot odds → equity (cache `ConcurrentDictionary` 2048) → outs → hand eval → board texture → fold equity → EV → bet sizing recomendado. `ClassifyPair` distingue Overpair/TopPair/MiddlePair/BottomPair/PocketPairUnder/BoardPaired. **Anomalía:** dualidad con `PokerDecisionFacade` |
| `src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs` | clase + `ISetPreflopActionUseCase` | ~250 | Cascada 11 ramas según `HandSituation` y `IsSecondAction`. Compone 10 sub-cases (`GetActionXxx`) que delegan a `ActionScenarioUseCases` (módulo Features) |
| `src/OpenScrape.App/Aplication/SetFlopForceBoardUseCase.cs` | clase + `ISetFlopForceBoardUseCase` | — | Pobla `BoardTexture`/`HeroHandStrength`/`Draws` de `TableScrapeFlopResult` |
| `src/OpenScrape.App/Aplication/GetCropImageUseCase.cs` | clase + `IGetCropImageUseCase` | — | Wrapper sobre `ImageCropperService.CropImageToBase64` |
| `src/OpenScrape.App/Aplication/GetHashImageUseCase.cs` | clase + `IGetHashImageUseCase` | — | Compute hash de imagen |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs` | clase + `IGetCardsFlopUseCase` | **115** | OCR de 3 cartas Flop por dHash compare contra `CardCacheService` (52 cartas). Retorna `BoardData[]` |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsTurnUseCase.cs` | clase + `IGetCardsTurnUseCase` | — | Análogo para Turn (carta 4) |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsRiverUseCase.cs` | clase + `IGetCardsRiverUseCase` | — | Análogo para River (carta 5) |
| `src/OpenScrape.App/Aplication/UseCases/OutsCalculatorUseCase.cs` | clase + `IOutsCalculatorUseCase` | **514** | Calcula 12 tipos de draws: Flush/Straight/Gutshot/Sets/FullHouse/Overcards/2Pair/DoubleGutshot/StraightFlush/4ofKind/3ofKind/OnePair/HighCard. Probabilidad exacta vía combinatoria (no regla 4-2). Devuelve `HandStrength` con `DrawProbability` por tipo |
| `src/OpenScrape.App/Aplication/UseCases/PotOddsCalculator.cs` | clase + `IPotOddsCalculator` | **84** | Suma outs únicos (Flush+Straight+Sets) con `DistinctBy(Id)` y aplica regla 4-2. Wrapper legacy |
| `src/OpenScrape.App/Aplication/UseCases/SaveTableMapUseCase.cs` | clase + `ISaveTableMapUseCase` | — | Persistir tableMap |
| `src/OpenScrape.App/Aplication/UseCases/LoadTableMapUseCase.cs` | clase + `ILoadTableMapUseCase` | — | Cargar tableMap |
| `src/OpenScrape.App/Aplication/UseCases/SetMovementRegionUseCase.cs` | clase + `ISetMovementRegionUseCase` | — | Actualizar coords región |
| `src/OpenScrape.App/Aplication/UseCases/GetWindowsScreenUseCase.cs` | clase | — | Captura ventana con P-Invoke `GetWindow`/`Execute` |
| `src/OpenScrape.App/Aplication/UseCases/BaseRequest.cs` / `BaseResponse.cs` | clases | — | Bases comunes |
| `src/OpenScrape.App/Aplication/UseCases/Actions/Get*UseCase.cs` × 10 | clases + interfaces | — | OpenRaise, 3Bet, Cold4Bet, Hero3BetAndOpenRaiser4Bet, HeroCallOpenRaiseAndGetSqueeze, RaiseOverLimper, RaiseVsSBLimp, Squeeze, Vs3Bet, Vs3BetAndCall. Cada uno consume `ActionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation, ActionScenarioRequest)` |

### `Helpers/`

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Helpers/CaptureWindowsHelper.cs` | static clase + `User32` + `GDI32` | **245** | P-Invoke screenshot: `SetProcessDPIAware` → `PrintWindow PW_RENDERFULLCONTENT` (fallback `BitBlt`) → escalar con `NearestNeighbor` → DPI 600. Helper `BinaryImage` con LockBits unsafe pointers para grayscale + threshold |
| `src/OpenScrape.App/Helpers/ImagePreprocessorHelper.cs` | clase | **407** | Pipeline OCR: resize ×2 si <1000px → `FastGrayscale` (Format8bppIndexed + paleta + `Parallel.For`) → median noise → `FastContrast 1.5` → `Deskew` (rotation matrix) → `FastBinarize` |
| `src/OpenScrape.App/Helpers/EncrypterHelper.cs` | static clase | **145** | AES-CBC + SHA256 hash de secret como key. **Anomalía:** IV fija de 16 ceros |
| `src/OpenScrape.App/Helpers/WindowsInformationHelper.cs` | static clase | **82** | `EnumWindows` filtrando `.NET`/`GDI+`/`Hidden`/`DDE`/`System`/`Opera`. Devuelve `IEnumerable<KeyValuePair<string, IntPtr>>` |
| `src/OpenScrape.App/Helpers/HandHelper.cs` | static clase | **44** | `GetSuitHand(text)` y `GetForceHand(text)` switch char→int |
| `src/OpenScrape.App/Helpers/UserHandHelper.cs` | static clase | **48** | `SetHandValue(state)` formatea `"AKs"`/`"AKo"`. `Exist4Bet(state)` cuenta raises >1BB ordenados por position |
| `src/OpenScrape.App/Helpers/ColorHelper.cs` | static clase + 2 DTOs | **44** | `GetRGBColor(GetRGBColorRequest)` extrae RGB de píxel |
| `src/OpenScrape.App/Helpers/AppThemeHelper.cs` | static clase | **23** | Paleta 3 colores: `PrimaryDark`, `PrimaryLight`, `Accent` + estados `Success`/`Warning`/`Danger` + fondos |
| `src/OpenScrape.App/Helpers/ObtainActionHelper.cs` | static clase + `Hands` | **50** | Random weighted selection de acciones por porcentaje acumulado |
| `src/OpenScrape.App/Helpers/PlayerRegionParser.cs` | static clase | **25** | `GetPlayerNumber(regionName, extraText)` regex `p(\d+){extraText}`. Extraído de `FrmMain.GetPlayerNumber` |
| `src/OpenScrape.App/Helpers/CoordinateScaler.cs` | clase + `ICoordinateScaler` | **45** | (ver tabla Services arriba) |
| `src/OpenScrape.App/Helpers/FlopHelper/FlopAnalyzerHelperReqest.cs` | clase | — | Request DTOs analizadores legacy |
| `src/OpenScrape.App/Helpers/FlopHelper/PreFlopRaiser/PreFlopRaiserIPAnalyzerHelper.cs` | clase | — | Analizador legacy IP |
| `src/OpenScrape.App/Helpers/FlopHelper/RaiseOverLimper/RaiseOverLimperIPAnalyzerHelper.cs` | clase | — | Analizador legacy ROL IP |
| `src/OpenScrape.App/Helpers/FlopHelper/RaiseOverLimper/RaiseOverLimperOOPAnalyzerHelper.cs` | clase | — | Analizador legacy ROL OOP |

### `Entities/` (DTOs operacionales)

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Entities/PlayerGameState.cs` | clase + `ResponseAction` + `BoardData` | **64** | Estado mutable principal: hero (cards/rank/suit/kicker), pot, position, bet, stack, situation, players, boardCards. Computed `HavePocketPair`, `IsSuited` |
| `src/OpenScrape.App/Entities/Player.cs` | clase | **22** | Name/Alias/Dealer/Bet/Stack/Active/SitOut/Empty/HasFolded/BigBlind/SmallBlind/Position/ValuePosition/`WasPreflopAggressor` |
| `src/OpenScrape.App/Entities/BoardTextures.cs` | 2 enums | **5** | `TurnBoardTexture { Dry, Coordinated, Paired }` y `RiverBoardTexture { Dry, Coordinated, Paired }` |
| `src/OpenScrape.App/Entities/TableScrapeFlopResult.cs` | 4 clases con `[DataAnnotations]` | **104** | `BoardTexture` (IsCoordinated/Rainbow/Connected/Paired/Dry/HighestRank/LowestRank/HasAce/HasKing) + `HeroHandStrength` (HasTopPair/HasOverPair/HasTwoPair/HasSet/...) + `DrawingOpportunities` (HasFlushDraw/HasStraightDraw/HasBackdoorFlushDraw). Computed `HasStrongHand`/`HasWeakHand`/`ShouldContinue` |

### `Models/` (DTOs internos)

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Models/BestHandResult.cs` | internal sealed record | **17** | `(HandRank Ranking, List<NormalizedCard> Cards, List<NormalizedCard> Kickers)` |
| `src/OpenScrape.App/Models/HandEvaluationResult.cs` | clase | — | `HandRanking`/`BestFiveCards`/`HandDescription`/`HandStrength`/`Kickers`/`AllCards` |
| `src/OpenScrape.App/Models/NormalizedCard.cs` | clase | — | `OriginalCard`/`Rank`/`Suit` |
| `src/OpenScrape.App/Models/Region.cs` | clase | — | DTO Region local (legacy) |

### `Interfaces/`

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Interfaces/IAddImage.cs` | interface | — | `Execute(IntPtr window)` consumido por `FormImage`/`FormListApps` |

### `Data/` (15 JSON de estrategia)

| Archivo | Propósito |
|---------|-----------|
| `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json` | Strategy preflop por situación |
| `Cold4Bet.json`, `FourBet.json`, `RaiseOverLimpers.json`, `RaiseVsSbLimp.json` | Strategy preflop adicional |
| `VsSqueeze.json`, `VsThreeBetAndCall.json` | Defensa preflop |
| `Cartas2.json` | Cartas embebidas |
| `Regiones.json`, `Regiones3.json`, `RegionToTest.json` | Coordenadas regiones |
| `tableMap.json` | Mapa completo de la mesa |

### `Properties/`

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.App/Properties/Resources.Designer.cs` | static class generada | — | Recursos embebidos |
| `src/OpenScrape.App/Properties/Settings.Designer.cs` | sealed class generada | — | Settings WinForms |
| `src/OpenScrape.App/Properties/launchSettings.json` | JSON | — | Profiles de Visual Studio |

### `Resources/tessdata/` y `tessdata/`

| Archivo | Propósito |
|---------|-----------|
| `Resources/tessdata/eng.traineddata` | Tesseract data — embebida (EmbeddedResource) |
| `tessdata/eng.traineddata` | Tesseract data — copia output (PreserveNewest). **Anomalía Scout:** dual ubicación |

## Composition root → registros DI (extracto)

> Detalle completo en `_reversa_sdd/code-analysis.md` y `flowcharts/OpenScrape.App.md`.

```
Singletons (compartidos):
  Algoritmos DM: MonteCarloSimulator, BitHandEvaluator, OutsCalculator,
                  BoardTextureAnalyzer, PreflopEquityCalculator + 5 IFace forwarding
  Servicios DM: BetSizingService, RangePolarizer, PostflopDecisionService,
                OpponentTracker, StrategyAnalyzerService, ExploitabilityCalculator,
                AutoCalibrationService, DangerPenaltyCalculator, ImpliedOddsCalculator,
                PreflopAnalyzer, EquityCalculatorService, StrategyBacktester,
                BankrollTrackerService (lambda con IDocumentStore + StrategyProfile)
                + 12 IFace forwarding
  Calculator: IPokerCalculator → UnifiedPokerCalculator
  App: OcrService, ColorDetectionService, ImageCropperService, DetectionLoggerService,
       ScreenReaderService, GameLoopStateMachine, RegionLookupCache, CardCacheService,
       CoordinateScaler, MetricsCollector, ActionFormatter, OverlayPositioner,
       StrategyProfileService, ThresholdsRegistry
  Use cases: SetFlopForceBoardUseCase, GetHashImageUseCase, GetCropImageUseCase,
             OutsCalculatorUseCase, GetWindowsScreenUseCase,
             GetCardsFlop/Turn/RiverUseCase

Scoped (1 por scope, comparten contexto en una mano):
  GameLoggerService, PokerDecisionFacade, GameLoopCoordinator, UiSyncService,
  TableLayoutService, PostflopContextHolder, GameCoordinator,
  SetPreflopActionUseCase

Transient:
  FrmMain (resuelto en CreateAsyncScope)
```
