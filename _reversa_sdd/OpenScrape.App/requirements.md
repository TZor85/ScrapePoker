# OpenScrape.App — Requisitos

> Capa de **composition root, interfaz de usuario y captura en vivo** del bot. Es la única capa autorizada a depender de WinForms, Tesseract, GDI/User32 y la `IConfiguration` del Host. Todas las demás capas (`Domain`, `Features`, `Infrastructure`, `DecisionMaker`) son consumidas desde aquí, nunca al revés. 🟢 (`src/OpenScrape.App/OpenScrape.App.csproj`, `src/OpenScrape.App/Program.cs:43-179`)

---

## Visión General

`OpenScrape.App` agrupa **~110 archivos C# / ~13 800 LOC** organizados en nueve carpetas funcionales:

| Carpeta | Tipos representativos | Rol |
|---------|----------------------|-----|
| Raíz | `Program.cs` (224 LOC) | Composition root: `Host.CreateDefaultBuilder` con `STAThread`, ~50 registros DI, fail-fast `StrategyProfileValidator`, `CreateAsyncScope` con `FrmMain` 🟢 (`Program.cs:43-179,187-197,213-222`) |
| `Configuration/` | `FeatureFlags`, `GameLoopOptions` | Toggles de cutover y opciones del game loop. `FeatureFlags.UseGameLoopCoordinator=false` por defecto 🟢 |
| `Forms/` | `FrmMain` (4 502 LOC), `FrmOverlay` (561), `FrmHandDetail`, `FrmDetectionDebug`, `FormImage`, `FormAction`, `FormListApps` | UI WinForms — incluye 5 pestañas (Juego/Config/Tablas/Logs/Historial+Bankroll+Métricas), overlay flotante con magenta-key, popup de hand history y formulario de calibración OCR 🟢 |
| `Services/` | `OcrService` (462), `ScreenReaderService` (521), `TableLayoutService` (664), `GameCoordinator` (793), `GameLoggerService` (374), `GameLoopStateMachine` (166), `OcrService`, `ImageCropperService`, `ColorDetectionService`, `DetectionLoggerService`, `RegionLookupCache`, `CardCacheService`, `PostflopContextHolder`, `StrategyProfileValidator`, `CoordinateScaler`, `OverlayPositioner`, `ActionFormatter`, `LruCache<TK,TV>`, `Logging/TextBoxLogger*` (38 archivos) | Servicios de soporte de captura, OCR, decisión, persistencia y logging 🟢 |
| `Telemetry/` | `MetricsCollector` (149), `Histogram` (88), `MetricsSnapshot`, `ScopedMeasurement`, `TelemetryCategories`, `IMetricsCollector` | Telemetría per-cycle/per-hand con 17 categorías nombradas y orden de UI estable 🟢 (`Telemetry/TelemetryCategories.cs`) |
| `Aplication/UseCases/` | `UnifiedPokerCalculator` (476), `OutsCalculatorUseCase` (514), `GetCardsFlop/Turn/RiverUseCase`, `PotOddsCalculator`, `Save/LoadTableMapUseCase`, `GetWindowsScreenUseCase`, `Actions/Get*UseCase` ×10 | Use cases scoped que componen los building blocks de `Features` y `DecisionMaker`. **`UnifiedPokerCalculator` es el facade público real del motor de decisión usado en producción** (no `PokerDecisionFacade`) 🟢 (`Aplication/UseCases/UnifiedPokerCalculator.cs:46`) |
| `Helpers/` | `CaptureWindowsHelper` (245), `ImagePreprocessorHelper` (407), `EncrypterHelper` (145), `WindowsInformationHelper`, `HandHelper`, `UserHandHelper`, `ColorHelper`, `AppThemeHelper`, `ObtainActionHelper`, `PlayerRegionParser`, `CoordinateScaler`, `FlopHelper/*` | Helpers static y P-Invoke (User32, GDI32). `CaptureWindowsHelper` y `ImagePreprocessorHelper` concentran la captura DPI-aware y el pipeline OCR; `EncrypterHelper` concentra la criptografía (con anomalía conocida de IV fija) 🟡 (`Helpers/EncrypterHelper.cs:?`) |
| `Entities/`, `Models/`, `Interfaces/` | `PlayerGameState`, `Player`, `BoardTextures` enums, `TableScrapeFlopResult`, `BestHandResult`, `HandEvaluationResult`, `NormalizedCard`, `Region`, `IAddImage` | DTOs operacionales y mutables que viajan entre la UI, los servicios y los use cases 🟢 |
| `Data/`, `Resources/`, `Properties/` | 15 JSON de estrategia/regiones, `eng.traineddata` embebida + copia output, `appsettings.json` (~31 KB con ~150 parámetros del `StrategyProfile` + secciones), `launchSettings.json` con `DOTNET_ENVIRONMENT=Development` | Configuración y assets. **Anomalía Scout:** `appsettings.json` tiene credenciales reales hardcodeadas y la traineddata vive duplicada (`Resources/tessdata/` embebida + `tessdata/` copia output) 🔴 (`appsettings.json`, `_reversa_sdd/inventory.md` § Anomalías) |

🟢 El módulo declara `<TargetFramework>net10.0-windows</TargetFramework>`, `<UseWindowsForms>true</UseWindowsForms>` y `<OutputType>WinExe</OutputType>`. Es el único proyecto con dependencia a WinForms, Tesseract y P-Invoke. (`src/OpenScrape.App/OpenScrape.App.csproj`)

> ⚠️ **Importante:** El facade `IPokerCalculator → UnifiedPokerCalculator` vive **aquí**, no en `OpenScrape.DecisionMaker`. Es quien orquesta los building blocks expuestos por la capa de motor (Equity → Outs → HandEval → Texture → FoldEquity → EV → Sizing). 🟢 (`Aplication/UseCases/UnifiedPokerCalculator.cs:46-78`)

---

## Responsabilidades

### R1 — Composition root y configuración

- **Construir el grafo de dependencias completo en arranque.** `Program.Main` construye un `Host.CreateDefaultBuilder` con `DOTNET_ENVIRONMENT` (default `Development`), registra ~50 servicios y resuelve `FrmMain` desde un `CreateAsyncScope` (no del root, porque sus dependencias scoped exigen un scope vivo durante toda la sesión). 🟢 (`Program.cs:34-43,213-222`)
- **Registrar algoritmos del `DecisionMaker` con patrón forwarding (clase concreta + interfaz comparten instancia).** Garantiza que cualquier consumidor — incluido un test que resuelva por interfaz — vea el mismo singleton. Aplica a `MonteCarloSimulator`, `BitHandEvaluator`, `OutsCalculator`, `BoardTextureAnalyzer`, `BetSizingService`, `RangePolarizer`, `PostflopDecisionService`, `OpponentTracker`, `StrategyAnalyzerService`, `ExploitabilityCalculator`, `AutoCalibrationService`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `PreflopAnalyzer`, `EquityCalculatorService`, `StrategyBacktester`, `BankrollTrackerService`. 🟢 (`Program.cs:71-113`)
- **Ejecutar fail-fast del `StrategyProfile` antes de mostrar UI.** Si faltan thresholds, hay claves inválidas o tiers incoherentes (`FoldBelow ≥ ThinValueAbove`, etc.), `StrategyProfileValidator.Validate` lanza `StrategyProfileValidationException`, se muestra `MessageBox` y se llama a `Environment.Exit(1)`. La aplicación nunca arranca con un perfil inválido. 🟢 (`Program.cs:187-197`, `Services/StrategyProfileValidator.cs`)
- **Inicializar `CoordinateScaler` desde `appsettings.json`.** Lee `CaptureSettings:IsReferenceSet`, `ReferenceImageWidth`, `ReferenceImageHeight`. Si la sección está completa, llama a `Initialize(refW, refH)`; si no, las coordenadas se usan sin escalar. 🟢 (`Program.cs:199-208`, `Helpers/CoordinateScaler.cs`)
- **Cargar `appsettings.{Environment}.json` con override de credenciales reales.** `appsettings.json` contiene placeholders y entra al control de versiones; `appsettings.Development.json` contiene cadenas de conexión y claves AES y está gitignored. 🟢 (`Program.cs:41,49-50`, ADR-0018 `_reversa_sdd/adrs/0018-config-environment-development-secrets-gitignored.md`)

### R2 — Captura de pantalla y preprocesamiento de imagen

- **Capturar la ventana del cliente de poker en alta resolución sin requerir foco.** Usa `User32.SetProcessDPIAware`, prefiere `User32.PrintWindow` con flag `PW_RENDERFULLCONTENT` (captura ventanas Chromium/DirectX en background) y cae a `GDI32.BitBlt` cuando `PrintWindow` falla. Escala con `InterpolationMode.NearestNeighbor` a `DPI=600` para mejorar OCR. 🟢 (`Helpers/CaptureWindowsHelper.cs`)
- **Preprocesar imágenes para OCR con un pipeline determinista.** `ImagePreprocessorHelper` aplica resize ×2 (si <1000 px) → grayscale 8 bpp paralelo → mediana de ruido → contraste 1.5 → deskew con matriz de rotación → binarización fast. Cada paso es opcional y configurable por la región leída. 🟢 (`Helpers/ImagePreprocessorHelper.cs`)
- **Listar y filtrar las ventanas activas para identificar clientes de poker.** `WindowsInformationHelper.EnumWindows` excluye ventanas `.NET`, `GDI+`, `Hidden`, `DDE`, `System`, `Opera`. `FormListApps` filtra adicionalmente por substring `"NL H"` (clientes de NL Hold'em). 🟢 (`Helpers/WindowsInformationHelper.cs`, `Forms/FormListApps.cs`)

### R3 — OCR multi-lectura con consenso y caché

- **Extraer texto de regiones de la mesa con tasa de error <1 % en condiciones nominales.** `OcrService` envuelve `Tesseract 5.x` con un `lock` global (Tesseract es single-threaded), un `LruCache<string, SKBitmap>` (200 entradas) y un `LruCache<ulong, string>` (500 entradas) indexado por `dHash` de 64 bits. El motor se inicializa con `eng.traineddata`, auto-extrayéndola del recurso embebido si falta en disco. 🟢 (`Services/OcrService.cs:13-60`, ADR-0010 § Tesseract)
- **Multiplicar la fiabilidad mediante consenso de 3 lecturas.** `ScreenReaderService` realiza tres lecturas independientes con preprocesamiento variado, normaliza cada salida (decimales perdidos, artefacto "8", caracteres confusos `O`/`0`, `l`/`1`, `S`/`5`) y devuelve el valor con mayoría simple. La normalización de bets y stacks valida contra el `pot` para descartar ruido. 🟢 (`Services/ScreenReaderService.cs`, OCR-1/OCR-2 en `MEMORY.md`)
- **Detectar cartas con `dHash` perceptual + comparación pixelada.** `ImageCropperService` calcula un `dHash` 64-bit de cada crop y lo compara contra la colección embebida (`Data/Cartas2.json`, 52 cartas). Hamming distance ≤ 15 → candidato; refinamiento con `CalculateSimilarity` con terminación temprana cada 32 px. 🟢 (`Services/ImageCropperService.cs`)
- **Auto-extraer `eng.traineddata` del recurso embebido si falta en disco.** En el constructor de `OcrService`, si `tessdata/eng.traineddata` no existe, se copia desde `OpenScrape.App.Resources.tessdata.eng.traineddata`. Permite que la app funcione sin redistribuir la traineddata. 🟢 (`Services/OcrService.cs:35-54`)

### R4 — Detección del layout de la mesa y posiciones

- **Identificar al jugador con el botón de dealer por color.** `TableLayoutService` detecta el RGB dorado (200/140/80) en un radio de 3 px alrededor del centro nominal de cada asiento. Reintenta en cada iteración del loop mientras `Position == None` para tolerar capturas iniciales que pierden el botón. 🟢 (`Services/TableLayoutService.cs`, ADR-0019 § Posiciones)
- **Asignar posiciones a los jugadores activos respetando moving blinds.** Delegada a `PositionCalculator.AssignAllPositions(dealerPos, players)`: heads-up SB/BB, moving blinds saltan SitOut consecutivos a la izquierda, labels Early/Middle/CutOff por `effectiveCount`. 🟢 (`Services/PositionCalculator.cs:?`, ADR-0019)
- **Reconocer alias de jugador por OCR + regex de validación.** `ReadPlayerName` aplica preprocesamiento + lectura + filtro regex que descarta nombres con caracteres no imprimibles o longitud no plausible. Persiste el alias junto al `seat` en `OpponentTracker` para tracking estadístico estable entre manos. 🟢 (`Services/TableLayoutService.cs`)
- **Clasificar el estado de cada asiento.** Cinco estados: `Empty`, `SitOut`, `Active`, `Folded`, `RefreshPlayerStates`. La transición `Active → Folded` se infiere por desaparición de cartas y bets, no por OCR del texto "FOLD". 🟡 (`Services/TableLayoutService.cs`)

### R5 — Pipeline de decisión postflop y construcción del input

- **Coordinar el cálculo de equity, outs, textura y decisión postflop.** `GameCoordinator.DetermineFlopAction/Turn/River` (canónicos, ~150 LOC c/u) construye un `PostflopDecisionInput` (record con 6 campos requeridos + 36 con defaults) consumiendo helpers (`GetOpponentBetSize`, `GetActiveVillainId`, `GetVillainType`, `GetVillainStack`, `HeroBlocksTopBoardCard`, `DetectDonkBet`, `AdjustBetSize`, `AnalyzeBoardChange/Turn/River`) y delega al motor `IPostflopDecisionService.DetermineAction`. 🟢 (`Services/GameCoordinator.cs:30-298`)
- **Exponer un facade público de cálculo en producción.** `IPokerCalculator → UnifiedPokerCalculator.Calculate(...)` realiza 8 pasos: pot odds → equity (cache `ConcurrentDictionary` 2048 entradas) → outs → hand evaluation → board texture → fold equity → EV → bet sizing recomendado. **Es el punto de entrada efectivo** del motor de decisión, no el `PokerDecisionFacade` planeado para cutover. 🟢 (`Aplication/UseCases/UnifiedPokerCalculator.cs:46-237`, ADR-0006)
- **Mantener el contexto cross-street en un holder thread-safe.** `PostflopContextHolder` envuelve `PostflopGameContext` con `Volatile.Read` para lecturas y `lock(_gate)` para `Update(Func<T,T>)` y `StartNewHand`. Garantiza que la transición `WaitingForHand → HandDetected` siempre arranca con un contexto vacío. 🟢 (`Services/PostflopContextHolder.cs:?`, ADR-0007)
- **Detectar nuevas manos sin confiar en el `HandNumber` del cliente.** `GameCoordinator.DetectNewHand` combina cambios en `HandNumber` (cuando OCR es fiable) con cambios en hero hole cards y reset del board. Resetea incondicionalmente el `PostflopContextHolder` al confirmar una mano nueva. 🟢 (`Services/GameCoordinator.cs:DetectNewHand`)
- **Detectar auto-rebuy y excluirlo del cálculo de profit.** `_heroStackPreRebuy` se actualiza durante la mano activa; cuando el stack sube bruscamente al inicio (≈100 BB) se interpreta como rebuy automático y NO se sobrescribe el valor previo, preservando el profit real al cierre de la mano. 🟢 (`Forms/FrmMain.cs:99-105`, ADR-0013)

### R6 — Game loop y máquina de estados

- **Avanzar el estado de la mano respetando 10 transiciones válidas.** `GameLoopStateMachine` implementa `WaitingForHand → HandDetected → PreflopAction → FlopDetected → FlopAction → TurnDetected → TurnAction → RiverDetected → RiverAction → HandComplete → (loop)`. El diccionario `_validTransitions` rechaza transiciones fuera de tabla y el método `TryTransition(state, visibleBoardCards)` exige cartas mínimas (≥3 flop, ≥4 turn, ≥5 river) antes de avanzar. 🟢 (`Services/GameLoopStateMachine.cs:5-110`, ADR-0012)
- **Ejecutar el ciclo de captura periódico desde un `BackgroundWorker`.** `FrmMain.BackgroundWorker1_DoWork` invoca `btnCapture_Click` con un intervalo configurable (`GameLoopOptions.CaptureIntervalMs=100` por defecto) y un timeout de parada de `2000 ms`. La parada limpia se hace vía `CancellationTokenSource`. 🟢 (`Forms/FrmMain.cs:2737-2897`, `Configuration/GameLoopOptions.cs`)
- **Permitir cutover futuro al `GameLoopCoordinator` independiente.** `GameLoopCoordinator` (sealed, `IAsyncDisposable`) implementa `SemaphoreSlim` start/stop, `PeriodicTimer` y evento `ResultReady` con `SafeEmit` (catch por suscriptor). Hoy retorna `GameLoopResult { Empty = true }` por tick — está detrás del feature flag `FeatureFlags.UseGameLoopCoordinator=false`. La migración Fase 4 está pendiente. 🟡 (`Services/GameLoopCoordinator.cs`, `Configuration/FeatureFlags.cs`)
- **Forzar transiciones para test/debug con validación.** `ForceState(state)` valida que el estado destino exista en `_validTransitions` y emite `LogWarning`. Permite restauración postflop o tests de regresión sin abrir el contrato del estado normal. 🟢 (`Services/GameLoopStateMachine.cs:138-160`)

### R7 — Persistencia de sesiones y manos

- **Iniciar sesión por mesa y reutilizarla mientras dure.** `GameLoggerService.StartSessionAsync` reusa la sesión si `SessionId` coincide; si difiere, persiste la anterior con `await SaveSessionAsync()` y abre una nueva. Resetea acumuladores `_sessionTotalHands` y `_sessionTotalProfit`. 🟢 (`Services/GameLoggerService.cs:45-79`)
- **Iniciar mano y propagar el correlation scope.** `StartNewHandAsync` finaliza la mano anterior si existe (`FinalizeAndPersistHandAsync` con `SemaphoreSlim _dbWriteLock`), abre un `BeginScope({ HandNumber, ... })` que enriquece todos los logs hasta el cierre. 🟢 (`Services/GameLoggerService.cs:82-130`)
- **Registrar la decisión de cada calle como un `StreetDecision` inmutable.** `LogStreetDecision(street, equity, potOdds, ev, recommendedAction, actionTaken, potSize, betSize, situation, isInPosition, reason?, boardTexture?, totalOuts?, spr?)` añade un value object al `HandRecord` activo. 🟢 (`Services/GameLoggerService.cs`)
- **Cerrar la mano calculando profit con stack pre-rebuy.** `EndHand(prevHeroStack, finalStack)` clasifica resultado (`Won`/`Lost`/`Push`/`Unknown`), calcula `Profit = finalStack − prevHeroStack`, actualiza `_sessionTotalHands` y `_sessionTotalProfit` con `Interlocked` y trunca la lista en memoria a `MaxHandsInMemory=20`. Las manos truncadas ya están persistidas como `HandRecord` independientes (idempotencia). 🟢 (`Services/GameLoggerService.cs:?`)
- **Consultar histórico para la pestaña Historial y para backtest.** `GetRecentSessionsWithStatsAsync` y `GetHandsForSessionAsync` son consumidos por `dgvSessions`/`dgvSessionHands`/`FrmHandDetail` y por `StrategyBacktester`. 🟢 (`Services/GameLoggerService.cs`, `Forms/FrmMain.cs:?`)

### R8 — Telemetría de rendimiento y calidad

- **Medir 17 categorías por mano y por sesión sin afectar el ciclo de captura.** `MetricsCollector` (singleton) expone `Measure(category)` y `MeasureSessionOnly(category)` que retornan un `ScopedMeasurement` (struct) con `using` zero-allocation. Cada categoría tiene dos `Histogram` de 30 buckets logarítmicos (`bound[i] = 1e-5 × 10^(i × 0.2)` segundos = 10 µs → 6.3 s). Sobrestima ~37 % nunca subestima. 🟢 (`Telemetry/MetricsCollector.cs:11-149`, `Telemetry/Histogram.cs:1-88`)
- **Mantener el contrato de categorías estable y ordenado.** `TelemetryCategories` define 17 const (`Cycle.Total`, `Capture.Screenshot`, `OCR.{Cards,Bets,Stacks,HandNumber,PlayerNames}`, `Layout.{Dealer,Positions}`, `Decision.{Total,Equity,Texture,Profile,DecisionService,Sizing}`, `Overlay.Render`, `Persistence.SaveHand`) + `DisplayOrder` (orden UI) + `SessionOnly` (HashSet con `Persistence.SaveHand`). **No se renombran sin migración** — son parte del esquema persistido en `HandRecord.Telemetry`. 🟢 (`Telemetry/TelemetryCategories.cs:8-65`)
- **Excluir categorías `SessionOnly` del snapshot de mano.** `Persistence.SaveHand` se mide después del snapshot, así que solo se acumula en sesión. `EndHand` elimina explícitamente las categorías presentes en `SessionOnly` antes de devolver el `TelemetryAggregate`. 🟢 (`Telemetry/MetricsCollector.cs:74-101`)
- **Renderizar la pestaña Métricas con orden y latencias por mano.** `FrmMain.InitializeMetricsTab()` y `RenderMetricsGrid()` consumen `MetricsCollector.SnapshotSession()` para mostrar tabla con `LastHand` y `Session` por categoría, respetando `DisplayOrder`. 🟢 (ADR-0017)

### R9 — Overlay flotante y feedback al usuario en vivo

- **Mostrar la decisión recomendada sin robar foco al cliente.** `FrmOverlay` es un `Form` topmost sin bordes con `Color.Magenta TransparencyKey`, `TableLayoutPanel` 9 filas + action panel + animation timer fade-in y bordes redondeados con `GraphicsPath`. Usa P-Invoke `WM_NCLBUTTONDOWN` para drag manual. 🟢 (`Forms/FrmOverlay.cs`)
- **Posicionar el overlay relativo a la ventana del cliente.** `OverlayPositioner.Calculate(winLeft, winTop, winWidth, winHeight, overlayW, overlayH, horizOffset, vertOffset) → (x, y)` aritmética 1:1 extraída de `FrmMain.CalculateOverlayPosition`. `centerX = winLeft + winWidth/2; x = centerX − overlayW/2 − horizOffset; y = winBottom − vertOffset`. 🟢 (`Services/OverlayPositioner.cs`)
- **Enriquecer la acción mostrada con BB.** `ActionFormatter.EnrichActionWithBBAmount(action, villains, bigBlind)` parsea `Bet 2.5x` → calcula `totalBB` y agrega `(N.NBB)`. Lógica 1:1 extraída de `FrmMain`. 🟢 (`Services/ActionFormatter.cs`)
- **Mostrar el detalle completo de cada mano histórica con highlights.** `FrmHandDetail` carga el `HandRecord`, separa los `StreetDecision` por calle, formatea cada bloque con `Color`-encoded `RichTextBox.AppendText` (verde = jugada óptima, rojo = divergencia, separadores `═══`) y muestra resultado y situación. 🟢 (`Forms/FrmHandDetail.cs`)

### R10 — Calibración interactiva de regiones

- **Permitir al usuario ajustar coordenadas de regiones en vivo.** Pestaña "Tablas" de `FrmMain` con grid de 8 botones (`btnUp`, `btnDown`, `btnLeft`, `btnRight`, `btnUpLeft`, `btnUpRight`, `btnDownLeft`, `btnDownRight`) + 4 botones de redimensión (`btnPlusWidth`, `btnMinusWidth`, `btnPlusHeight`, `btnMinusHeight`). Persiste vía `SaveTableMapUseCase`. 🟢 (`Forms/FrmMain.cs:3071-3232,3534-3686`, `Aplication/UseCases/SaveTableMapUseCase.cs`)
- **Calibrar OCR con visualizador zoomed en tiempo real.** `FrmDetectionDebug` ejecuta un `Timer 100 ms`, captura cada región con `ZOOM_FACTOR=10`, expone controles `numX`/`numY`/`numWidth`/`numHeight` y permite save/load de colores objetivo. 🟢 (`Forms/FrmDetectionDebug.cs`)
- **Cargar/guardar mapas de mesa por sala.** `LoadTableMapUseCase` y `SaveTableMapUseCase` son wrappers thin sobre `RegionTableMapUseCases` del módulo `Features`. La selección activa vive en `RegionLookupCache` (singleton, `Dictionary<mapId, Dictionary<regionName, Region>>` con `OrdinalIgnoreCase`). 🟢 (`Services/RegionLookupCache.cs`)
- **Escalar coordenadas si la captura difiere de la imagen de referencia.** `CoordinateScaler.Initialize(refW, refH)` se llama una sola vez en `Program.cs`. `ScaleRegion(posX, posY, w, h, currentW, currentH)` aplica `scale = (currentW/refW + currentH/refH)/2.0` o devuelve coords sin escalar si nunca se inicializó. 🟢 (`Helpers/CoordinateScaler.cs`)

### R11 — Logging configurable a TextBox + consola

- **Renderizar logs en la pestaña Logs con tokens cortos por nivel.** `TextBoxLogger` formatea `[HH:mm:ss LVL CategoryShort] {Scopes} message | ExceptionType: msg` con `LevelToken` map (TRC/DBG/INF/WRN/ERR/CRT). Resuelve scopes vía `IExternalScopeProvider`. 🟢 (`Services/Logging/TextBoxLogger.cs`)
- **Bufferizar logs hasta que el `TextBox` esté disponible.** `TextBoxLoggerProvider` (`[ProviderAlias("TextBox")]`) bufferiza pendientes hasta `SetTextBoxTarget(textBox)` (one-shot). Cross-thread con `BeginInvoke` y rotación FIFO por `MaxLines`. 🟢 (`Services/Logging/TextBoxLoggerProvider.cs`, ADR-0009)
- **Configurar el sink desde `appsettings.json`.** `TextBoxLoggerOptions` expone `MinimumLevel`, `MaxLines`, `BufferUntilTargetReady`, `BufferCapacity`. Registrado vía `services.AddTextBoxLogger()` extension. 🟢 (`Services/Logging/TextBoxLoggerOptions.cs`, `TextBoxLoggerExtensions.cs`)

### R12 — Tracking de rivales y mapeo seat ↔ alias

- **Resolver el villano activo de cada mano.** `GameCoordinator.GetActiveVillainId(state)` selecciona al jugador con mayor `Bet` activo y mapea su `Name` a `Alias` vía `OpponentTracker.ResolveName`. Si no se puede resolver, retorna `"Unknown"`. 🟢 (`Services/GameCoordinator.cs:81-106`)
- **Devolver el `OpponentType` corregido por posición.** `GetVillainType(state, heroIsInPosition)` consulta `OpponentTracker.GetProfile(villainId).GetTypeForPosition(!heroIsInPosition)`. Si la fiabilidad preflop es insuficiente, retorna `OpponentType.Unknown`. 🟢 (`Services/GameCoordinator.cs:95-114`)
- **Trackear acciones postflop por posición IP/OOP.** `TrackVillainPostflopAction(state, maxBet, isPreflopAggressor, heroIsInPosition)` alimenta `OpponentTracker` con cuatro contadores separados (IP/OOP × call/fold). 🟢 (`Services/GameCoordinator.cs:116-?`)

---

## Reglas de Negocio

### Reglas de arranque

- **La aplicación NO arranca si el `StrategyProfile` es inválido.** `StrategyProfileValidator.Validate` acumula errores y aborta con `Environment.Exit(1)` antes de mostrar `FrmMain`. 🟢 (`Program.cs:187-197`, ADR-0008)
- **`DOTNET_ENVIRONMENT` controla qué `appsettings.{Environment}.json` se sobrepone.** Default `Development` para esta app desktop. `appsettings.json` contiene placeholders `CHANGE_ME` y entra al control de versiones. 🟢 (`Program.cs:41`, ADR-0018)
- **`FrmMain` SIEMPRE se resuelve desde un `CreateAsyncScope`, nunca del root.** Sus dependencias (`GameCoordinator`, `TableLayoutService`, `GameLoggerService`, `PostflopContextHolder`) son scoped y vivirán todo el ciclo de vida de la ventana. 🟢 (`Program.cs:213-222`, ADR-0014)

### Reglas de captura y OCR

- **Tesseract es single-threaded.** `OcrService` protege todas las llamadas con `lock(_lock)` global. 🟢 (`Services/OcrService.cs:19`)
- **Cache OCR es bicapa: bitmap (200) + ocrText (500).** Indexada por `dHash` 64-bit, no por hash de píxeles raw. 🟢 (`Services/OcrService.cs:13-18`)
- **Cuatro intentos de OCR con preprocesamiento variado.** Default → lower threshold → higher threshold → contrast boost. Si los cuatro retornan distinto, devuelve la moda; si la confianza media (`GetMeanConfidence`) cae <0.70, marca el resultado como `IsHighConfidence=false` y permite reintento. 🟢 (Sprint 15 § OCR confidence scoring, `Services/OcrService.cs`)
- **Captura usa `PrintWindow PW_RENDERFULLCONTENT` con fallback a `BitBlt`.** Permite leer ventanas Chromium/DirectX en background. 🟢 (`Helpers/CaptureWindowsHelper.cs`)
- **DPI awareness se activa al inicio de cada captura.** `User32.SetProcessDPIAware` se invoca antes de `PrintWindow` para evitar escalado del SO. 🟢 (`Helpers/CaptureWindowsHelper.cs`)

### Reglas del game loop

- **Toda transición de estado pasa por `_validTransitions`.** Transiciones fuera del diccionario emiten `LogWarning` y retornan `false`. 🟢 (`Services/GameLoopStateMachine.cs:24-66`)
- **El estado se valida contra cartas visibles del board.** `TryTransition(state, visibleBoardCards)` exige `≥3` para flop, `≥4` para turn, `≥5` para river; bloquea si la cuenta es menor. 🟢 (`Services/GameLoopStateMachine.cs:73-110`, ADR-0012)
- **`MaxOcrRetries=2` por iteración.** Si OCR falla más de 2 veces consecutivas en una transición, la captura se aborta y el state machine permanece en el estado actual. 🟢 (`Services/GameLoopStateMachine.cs:165`)
- **El detection de dealer se reintenta en cada iteración mientras `Position == None`.** Tolera capturas iniciales que pierden el botón. 🟢 (`Services/TableLayoutService.cs`)

### Reglas de persistencia

- **Una sesión por mesa, reutilizada mientras `SessionId` coincida.** Si cambia, la anterior se persiste con `await` antes de cambiar. 🟢 (`Services/GameLoggerService.cs:45-79`)
- **`MaxHandsInMemory=20` manos en `GameSession.Hands`.** Las anteriores ya están persistidas como `HandRecord` independientes. 🟢 (`Services/GameLoggerService.cs:15`)
- **`_sessionTotalHands` y `_sessionTotalProfit` se actualizan con `Interlocked`.** Los acumuladores son la fuente de verdad para cálculos derivados (`BBPer100`), porque la lista en memoria está truncada. 🟢 (`Services/GameLoggerService.cs:27-28`)
- **`HandRecord.HeroStackEnd` se calcula con `prevHeroStack`, NO con el stack actual.** El stack actual puede haber sido auto-rebuy a 100 BB. 🟢 (ADR-0013)
- **Auto-rebuy se detecta por subida brusca de stack al inicio de mano.** Threshold conservador (≈50 BB). El valor pre-rebuy se preserva en `_heroStackPreRebuy`. 🟢 (`Forms/FrmMain.cs:99-105`)

### Reglas de telemetría

- **Las 17 categorías de `TelemetryCategories` son contrato estable.** Cualquier renombrado requiere migración del esquema persistido en `HandRecord.Telemetry`. 🟢 (ADR-0017)
- **`SessionOnly` excluye categorías del snapshot de mano.** `Persistence.SaveHand` solo aparece en sesión, no en `LastHand`. 🟢 (`Telemetry/TelemetryCategories.cs:61-64`)
- **Histograma sobrestima ~37 %, nunca subestima.** Buckets logarítmicos `1e-5 × 10^(i × 0.2)` segundos. Adecuado para detectar regresiones, no para latencias absolutas precisas. 🟢 (`Telemetry/Histogram.cs`)
- **`Histogram` no es thread-safe — sincronización externa.** `MetricsCollector.CategoryState` envuelve cada `Histogram` con `lock`. 🟢 (`Telemetry/Histogram.cs`, `MetricsCollector.cs:143-149`)

### Reglas de UI

- **`FrmOverlay` usa `Color.Magenta` como `TransparencyKey`.** Cualquier región pintada de magenta se vuelve transparente al cliente OS. 🟢 (`Forms/FrmOverlay.cs`, `FormAction.cs`)
- **`FrmHandDetail` está sin `.Designer.cs`.** Layout 100 % programático con `RichTextBox.AppendText` color-coded. 🟡 (`Forms/FrmHandDetail.cs:135 LOC`)
- **`FormImage` tiene path hardcoded `C:\Code\Poker\ScrapePoker\resources\Games`.** Anomalía Scout: comentario `//portatil` indica deuda técnica. 🔴 (`Forms/FormImage.cs`)
- **`FormListApps` filtra por substring `"NL H"`.** Solo lista clientes de NL Hold'em — el filtro es global, no configurable. 🟡 (`Forms/FormListApps.cs`)

### Reglas de criptografía y secretos

- **`appsettings.json` contiene credenciales reales en el repo.** Anomalía crítica del Scout: la connection string completa y el `EncryptionKey` están en el archivo versionado, no en `appsettings.Development.json`. 🔴 (Scout § Anomalías, `appsettings.json`)
- **`EncrypterHelper` usa AES-CBC con IV fija de 16 bytes a cero.** Compromete confidencialidad si el atacante tiene 2+ ciphertexts del mismo plaintext prefix. 🔴 (`Helpers/EncrypterHelper.cs`, anomalía documentada en legacy-mapping)

---

## Requisitos Funcionales

| ID | Requisito | Prioridad | Critério de Aceite |
|----|-----------|-----------|-------------------|
| RF-01 | Capturar la ventana del cliente y producir un `Image` listo para OCR | Must | `CaptureWindowsHelper.GetWindowsScreenAsync(handle)` retorna bitmap a DPI 600 sin pérdida de cliente Chromium |
| RF-02 | Extraer `BoardData` de las 5 cartas del board con confianza ≥0.70 | Must | `GetCardsFlop/Turn/RiverUseCase` retorna `BoardData[]` con `IsHighConfidence=true` en condiciones nominales |
| RF-03 | Asignar `TablePosition` a cada jugador activo respetando moving blinds | Must | `TableLayoutService.RefreshPlayerStates` produce `Player.Position ∈ {Early, Middle, CutOff, Button, SmallBlind, BigBlind}` para 9-max |
| RF-04 | Coordinar la decisión postflop con motor `IPostflopDecisionService` | Must | `GameCoordinator.DetermineFlopAction(state, ...)` retorna `(string action, decimal? size)` no nulo |
| RF-05 | Persistir cada `StreetDecision` y cerrar la mano con resultado | Must | `GameLoggerService.LogStreetDecision` y `EndHand` producen `HandRecord` con `Result ∈ {Won, Lost, Push, Unknown}` |
| RF-06 | Respetar 10 transiciones válidas del state machine | Must | `GameLoopStateMachine.TryTransition` rechaza transiciones no válidas con `LogWarning` |
| RF-07 | Mostrar acción recomendada en `FrmOverlay` con magenta-key transparency | Should | `FrmOverlay.UpdateAction(text)` propaga al `lbAction` y respeta `_overlayConfig.HorizontalOffset` |
| RF-08 | Renderizar pestaña Métricas con `MetricsCollector.SnapshotSession()` | Should | Grid muestra 17 categorías ordenadas por `DisplayOrder` con `LastHand` y `Session` p50/p95 |
| RF-09 | Permitir calibración interactiva de regiones por mesa | Should | Botones de mover/redimensionar persisten cambios vía `SaveTableMapUseCase` |
| RF-10 | Bufferizar logs hasta que el `TextBox` esté disponible (one-shot) | Should | `TextBoxLoggerProvider.SetTextBoxTarget(tb)` flushea pendientes y registra el target permanentemente |
| RF-11 | Detectar auto-rebuy y excluirlo del cálculo de profit | Should | Subida brusca de stack al inicio de mano no sobrescribe `_heroStackPreRebuy` |
| RF-12 | Escalar coordenadas si la captura difiere de la imagen de referencia | Should | `CoordinateScaler.ScaleRegion` aplica factor promedio si `Initialize` se ejecutó en arranque |
| RF-13 | Trackear villain por alias estable (no por seat) | Should | `OpponentTracker.ResolveName(name) → alias` mantiene contadores entre sesiones |
| RF-14 | Auto-extraer `eng.traineddata` si falta en disco | Could | Primera ejecución sin `tessdata/` copia desde recurso embebido sin error de usuario |
| RF-15 | Permitir cutover futuro a `GameLoopCoordinator` por feature flag | Could | `FeatureFlags.UseGameLoopCoordinator=true` activa el coordinator independiente de `BackgroundWorker` |
| RF-16 | Backtest A/B de estrategias contra histórico | Could | Botón "Backtest A/B" en pestaña Historial invoca `StrategyBacktester.RunBacktest` y muestra divergencias |

---

## Requisitos No Funcionales

| Tipo | Requisito inferido | Evidencia en código | Confianza |
|------|--------------------|---------------------|-----------|
| **Rendimiento** | Cycle.Total < 600 ms p95 (capture+OCR+decision+overlay) | `MetricsCollector` mide `Cycle.Total` con buckets hasta 6.3 s; Sprint 16 fijó objetivos | 🟡 |
| **Rendimiento** | OCR cache hit ratio > 60 % en sesiones largas | `OcrService` con dual cache `dHash` 200/500 | 🟡 |
| **Rendimiento** | Equity cache `ConcurrentDictionary` con `MaxSize=2048` (FIFO de inserción) | `Aplication/UseCases/UnifiedPokerCalculator.cs:59-60` | 🟢 |
| **Rendimiento** | Captura DPI-aware sin escalado del SO | `User32.SetProcessDPIAware` | 🟢 |
| **Concurrencia** | Tesseract single-threaded protegido con `lock` global | `Services/OcrService.cs:19` | 🟢 |
| **Concurrencia** | `PostflopContextHolder` thread-safe (`Volatile.Read` + `lock(_gate)`) | `Services/PostflopContextHolder.cs` | 🟢 |
| **Concurrencia** | `GameLoopStateMachine` thread-safe (`lock(_stateLock)`) | `Services/GameLoopStateMachine.cs:22,47` | 🟢 |
| **Concurrencia** | `GameLoggerService` con `SemaphoreSlim _dbWriteLock` para Marten | `Services/GameLoggerService.cs:20` | 🟢 |
| **Disponibilidad** | Fail-fast si `StrategyProfile` inválido | `Program.cs:187-197` | 🟢 |
| **Disponibilidad** | Auto-extracción de `eng.traineddata` ante ausencia | `Services/OcrService.cs:35-54` | 🟢 |
| **Disponibilidad** | `MaxOcrRetries=2` antes de abortar transición | `Services/GameLoopStateMachine.cs:165` | 🟢 |
| **Persistencia** | Sesión persistida con `await` al cambiar de mesa | `Services/GameLoggerService.cs:50-55` | 🟢 |
| **Observabilidad** | 17 categorías de telemetría con buckets log | `Telemetry/TelemetryCategories.cs` | 🟢 |
| **Observabilidad** | `BeginScope` con `SessionId`, `TableName`, `HandNumber` | `Services/GameLoggerService.cs:69-78,118-124` | 🟢 |
| **Observabilidad** | Logs renderizados con tokens cortos en TextBox + consola | `Services/Logging/TextBoxLogger.cs` | 🟢 |
| **Seguridad** | Credenciales reales en `appsettings.Development.json` (gitignored) | `appsettings.Development.json`, ADR-0018 | 🟡 |
| **Seguridad** | `EncrypterHelper` usa AES-CBC con IV fija (anomalía conocida) | `Helpers/EncrypterHelper.cs` | 🔴 |
| **Seguridad** | `appsettings.json` versionado contiene credenciales reales (anomalía Scout) | `appsettings.json`, Scout § Anomalías | 🔴 |
| **Portabilidad** | net10.0-windows, exclusivamente x64. Inviable en Linux/macOS | `OpenScrape.App.csproj`, ADR-0002 | 🟢 |
| **Mantenibilidad** | `FrmMain` 4 502 LOC viola SRP — refactor pendiente Fase 7 | `Forms/FrmMain.cs`, `MEMORY.md § Refactoring Arquitectónico` | 🟡 |

> Inferido a partir del código y los ADRs. Validar con equipo de operaciones los SLOs de p95 y los umbrales de cache hit ratio.

---

## Critérios de Aceitación

```gherkin
# RF-01 + RF-02 + RF-04: ciclo de captura → decisión postflop
Dado que el cliente de poker está abierto y el flop está visible
  Y la sesión ha sido iniciada con StartSessionAsync(sessionId, tableName, bigBlind)
  Y el StrategyProfile fue validado en arranque sin errores
Cuando el BackgroundWorker invoca btnCapture_Click en una iteración nueva
Entonces CaptureWindowsHelper produce un bitmap DPI=600 con PrintWindow
  Y GetCardsFlopUseCase devuelve 3 BoardData con IsHighConfidence=true
  Y TableLayoutService asigna TablePosition a todos los jugadores activos
  Y GameCoordinator.DetermineFlopAction retorna (action, size) en <600 ms p95
  Y FrmOverlay.UpdateAction muestra el texto enriquecido con BB amount

# RF-06: validación de cartas en transición de estado
Dado que el state machine está en FlopAction
  Y el OCR del board reporta solo 3 cartas visibles
Cuando se invoca TryTransition(TurnDetected, visibleBoardCards: 3)
Entonces la transición se rechaza con LogWarning
  Y CurrentState permanece FlopAction
  Y el ciclo siguiente reintenta la lectura

# RF-05 + RF-11: cierre de mano con auto-rebuy
Dado que la mano comenzó con heroStackStart=87BB
  Y durante la mano el stack subió bruscamente de 12BB a 100BB (auto-rebuy)
Cuando GameCoordinator.DetectNewHand confirma que la mano terminó
Entonces _heroStackPreRebuy preserva el valor previo al rebuy
  Y EndHand calcula Profit = stackPreRebuy - heroStackStart
  Y HandRecord.Result se clasifica como Won/Lost/Push según el profit real
  Y la sesión persiste con SaveSessionAsync sin perder la mano

# RF-08: snapshot de telemetría sin SessionOnly en LastHand
Dado que la mano N completó 17 mediciones
Cuando MetricsCollector.EndHand() retorna el TelemetryAggregate
Entonces el aggregate excluye Persistence.SaveHand de LastHand
  Y SnapshotSession() lo incluye en Session pero no en CurrentHandId

# RF-10: buffer de logs con target one-shot
Dado que la app arrancó y FrmMain aún no completó InitializeComponent
  Y el logger ya emitió 5 mensajes durante DI
Cuando FrmMain ejecuta _textBoxLoggerProvider.SetTextBoxTarget(tbResume)
Entonces los 5 mensajes se flushean en orden FIFO al TextBox
  Y los siguientes mensajes se renderizan en directo
  Y el target NO puede ser redefinido (one-shot)

# Caso de error: arranque con StrategyProfile inválido
Dado que appsettings.json tiene FoldBelow > ThinValueAbove en alguna combinación
Cuando Program.Main invoca StrategyProfileValidator.Validate
Entonces se lanza StrategyProfileValidationException con todos los errores acumulados
  Y se muestra MessageBox con el listado
  Y Environment.Exit(1) aborta antes de mostrar FrmMain
```

---

## Prioridade (MoSCoW)

| Requisito | MoSCoW | Justificación |
|-----------|--------|---------------|
| Captura DPI-aware con `PrintWindow PW_RENDERFULLCONTENT` | Must | Único punto de obtención de input — sin él no hay producto |
| OCR multi-lectura con consenso y caché bicapa | Must | Toda la fiabilidad del bot depende de ello |
| `GameCoordinator.DetermineXxxAction` y `UnifiedPokerCalculator` | Must | Punto de entrada efectivo del motor de decisión |
| State machine con transiciones validadas | Must | Sin ella las decisiones se aplican en street equivocada |
| Persistencia de `GameSession`/`HandRecord` con auto-rebuy detection | Must | Auditabilidad y backtest dependen de ello |
| Fail-fast del `StrategyProfile` | Must | Garantía operacional — la app no debe correr con perfil inválido |
| Telemetría 17 categorías + UI Métricas | Should | Diagnóstico y detección de regresiones, no funcionalidad core |
| `FrmOverlay` flotante con magenta-key | Should | Feedback visual al usuario; el bot funciona sin overlay (modo headless de pruebas) |
| Calibración interactiva de regiones | Should | Necesaria al instalar en una mesa nueva, no en runtime |
| Backtest A/B de estrategias | Could | Herramienta de validación, no del game loop |
| Cutover a `GameLoopCoordinator` independiente | Could | Refactor pendiente, valor estructural pero no funcional |
| `FormImage` con path hardcoded | Won't | Deuda técnica — se elimina en migración futura |

---

## Rastreabilidad de Código

> Mapeo a los archivos legados con su rol funcional dominante. Detalle exhaustivo en `_reversa_sdd/OpenScrape.App/legacy-mapping.md`.

| Archivo | Función / Clase | Cobertura |
|---------|-----------------|-----------|
| `src/OpenScrape.App/Program.cs` | `Program.Main` (composition root) | 🟢 |
| `src/OpenScrape.App/Forms/FrmMain.cs` | `FrmMain` (god class — 5 tabs, pipeline, BackgroundWorker) | 🟢 |
| `src/OpenScrape.App/Forms/FrmOverlay.cs` | `FrmOverlay`, `IFrmOverlay` | 🟢 |
| `src/OpenScrape.App/Forms/FrmHandDetail.cs` | `FrmHandDetail` (Hand history popup) | 🟢 |
| `src/OpenScrape.App/Forms/FrmDetectionDebug.cs` | `FrmDetectionDebug` (calibración OCR zoomed) | 🟢 |
| `src/OpenScrape.App/Services/GameCoordinator.cs` | `GameCoordinator`, `IGameCoordinator` | 🟢 |
| `src/OpenScrape.App/Services/TableLayoutService.cs` | `TableLayoutService`, `ITableLayoutService` | 🟢 |
| `src/OpenScrape.App/Services/ScreenReaderService.cs` | `ScreenReaderService`, `IScreenReaderService` | 🟢 |
| `src/OpenScrape.App/Services/OcrService.cs` | `OcrService`, `OcrResult`, dual `LruCache` | 🟢 |
| `src/OpenScrape.App/Services/ImageCropperService.cs` | `ImageCropperService`, `FastBitmap`, dHash | 🟢 |
| `src/OpenScrape.App/Services/GameLoggerService.cs` | `GameLoggerService` (sesiones/manos en Marten) | 🟢 |
| `src/OpenScrape.App/Services/GameLoopStateMachine.cs` | `GameLoopStateMachine`, `GameState` enum, `_validTransitions` | 🟢 |
| `src/OpenScrape.App/Services/GameLoopCoordinator.cs` | `GameLoopCoordinator` (cutover futuro) | 🟡 |
| `src/OpenScrape.App/Services/PostflopContextHolder.cs` | `PostflopContextHolder`, thread-safe wrapper | 🟢 |
| `src/OpenScrape.App/Services/StrategyProfileValidator.cs` | `StrategyProfileValidator.Validate` (fail-fast) | 🟢 |
| `src/OpenScrape.App/Services/RegionLookupCache.cs` | `RegionLookupCache` (singleton dictionary O(1)) | 🟢 |
| `src/OpenScrape.App/Services/CardCacheService.cs` | `CardCacheService` (singleton lazy) | 🟢 |
| `src/OpenScrape.App/Services/ColorDetectionService.cs` | `ColorDetectionService` (LockBits 32bppArgb) | 🟢 |
| `src/OpenScrape.App/Services/CoordinateScaler.cs` | `CoordinateScaler`, `ICoordinateScaler` | 🟢 |
| `src/OpenScrape.App/Services/OverlayPositioner.cs` | `OverlayPositioner` (aritmética 1:1 extraída) | 🟢 |
| `src/OpenScrape.App/Services/ActionFormatter.cs` | `ActionFormatter` (enrich con BB amount) | 🟢 |
| `src/OpenScrape.App/Services/PositionCalculator.cs` | `PositionCalculator.AssignAllPositions` | 🟢 |
| `src/OpenScrape.App/Services/LruCache.cs` | `LruCache<TKey,TValue>` (thread-safe) | 🟢 |
| `src/OpenScrape.App/Services/Logging/TextBoxLogger*.cs` | `TextBoxLogger`, `TextBoxLoggerProvider`, options | 🟢 |
| `src/OpenScrape.App/Telemetry/MetricsCollector.cs` | `MetricsCollector`, `IMetricsCollector` | 🟢 |
| `src/OpenScrape.App/Telemetry/Histogram.cs` | `Histogram` (30 buckets log) | 🟢 |
| `src/OpenScrape.App/Telemetry/TelemetryCategories.cs` | `TelemetryCategories` (17 const + DisplayOrder + SessionOnly) | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` | `IPokerCalculator → UnifiedPokerCalculator` (facade real) | 🟢 |
| `src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs` | `SetPreflopActionUseCase` (cascada 11 ramas) | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/OutsCalculatorUseCase.cs` | `OutsCalculatorUseCase` (12 tipos draw) | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/GetCardsFlop/Turn/RiverUseCase.cs` | OCR de cartas via `dHash` compare | 🟢 |
| `src/OpenScrape.App/Aplication/UseCases/PotOddsCalculator.cs` | `PotOddsCalculator` (regla 4-2 legacy) | 🟢 |
| `src/OpenScrape.App/Helpers/CaptureWindowsHelper.cs` | P-Invoke screenshot DPI-aware | 🟢 |
| `src/OpenScrape.App/Helpers/ImagePreprocessorHelper.cs` | Pipeline OCR (resize/grayscale/contraste/deskew/binarize) | 🟢 |
| `src/OpenScrape.App/Helpers/EncrypterHelper.cs` | AES-CBC + SHA256 (IV fija — anomalía 🔴) | 🟡 |
| `src/OpenScrape.App/Helpers/CoordinateScaler.cs` | (ver Services/CoordinateScaler.cs) | 🟢 |
| `src/OpenScrape.App/Entities/PlayerGameState.cs` | DTO mutable hero+pot+players+board | 🟢 |
| `src/OpenScrape.App/Entities/Player.cs` | DTO jugador con Position, Bet, Stack, SitOut | 🟢 |
| `src/OpenScrape.App/Entities/TableScrapeFlopResult.cs` | DTOs `BoardTexture`/`HeroHandStrength`/`DrawingOpportunities` | 🟢 |
| `src/OpenScrape.App/Configuration/FeatureFlags.cs` | `UseGameLoopCoordinator=false` (cutover Fase 6) | 🟢 |
| `src/OpenScrape.App/Configuration/GameLoopOptions.cs` | `CaptureIntervalMs=100`, `StopTimeoutMs=2000` | 🟢 |
| `src/OpenScrape.App/Data/*.json` | Strategy preflop por situación, regions, tableMap | 🟢 |
| `src/OpenScrape.App/Resources/tessdata/eng.traineddata` | Tesseract data — embebida (`EmbeddedResource`) | 🟢 |
| `src/OpenScrape.App/tessdata/eng.traineddata` | Copia output (`PreserveNewest` — anomalía Scout) | 🟡 |
| `src/OpenScrape.App/appsettings.json` | StrategyProfile + secciones (con anomalía 🔴 de credenciales) | 🟡 |
| `src/OpenScrape.App/appsettings.Development.json` | Override credentials (gitignored) | 🟢 |
