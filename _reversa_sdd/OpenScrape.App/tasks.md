# OpenScrape.App — Tareas de Implementación

> Secuencia de tareas para reimplementar la capa de UI, captura, OCR, telemetría y composition root a partir del legado, con rastreabilidad línea a línea. **Módulo grande** (~110 archivos `.cs` / ~13 800 LOC) organizado en 11 bloques funcionales paralelizables. La columna vertebral es `FrmMain` (4 502 LOC, god-class), `GameCoordinator` (793), `TableLayoutService` (664) y `ScreenReaderService` (521). Cada bloque puede ser asignado a un developer distinto sin colisión de archivos.

---

## Pré-requisitos

- [ ] .NET 10 SDK instalado (`net10.0-windows` con `<UseWindowsForms>true</UseWindowsForms>`).
- [ ] Windows 11 x64 — única plataforma soportada (`OutputType=WinExe`).
- [ ] PostgreSQL local accesible para `Marten 8.24.0` (consumido por `GameLoggerService`, `BankrollTrackerService`, `CardCacheService`).
- [ ] `OpenScrape.Domain`, `OpenScrape.Features`, `OpenScrape.Infrastructure` y `OpenScrape.DecisionMaker` compilables. La App no compila sin las cuatro capas inferiores.
- [ ] `eng.traineddata` (Tesseract eng) disponible: como recurso embebido (`Resources/tessdata/eng.traineddata`, build action `EmbeddedResource`) y/o como archivo output (`tessdata/eng.traineddata`, `CopyToOutputDirectory=PreserveNewest`).
- [ ] Variables de entorno y configuración:
  - [ ] `DOTNET_ENVIRONMENT=Development` para que se sobrepongan los secretos de `appsettings.Development.json`.
  - [ ] `appsettings.Development.json` con connection string PostgreSQL real y `EncryptionKey` (gitignored).
- [ ] NuGet packages: `Microsoft.Extensions.Hosting 10.0.3`, `Microsoft.Extensions.Logging 10.0.3`, `Microsoft.Extensions.Configuration.Json 10.0.3`, `Microsoft.Extensions.Options 10.0.3`, `Marten 8.24.0`, `Tesseract 5.x`, `OpenCvSharp4 4.x` + runtime, `SkiaSharp 2.x`.
- [ ] Decisiones humanas pendientes (ver `questions.md`):
  - [ ] ¿`PokerHandEvaluator` legacy se elimina o queda como referencia? 🟡
  - [ ] ¿`PokerDecisionFacade` se vuelve el camino real o se elimina? 🟡
  - [ ] ¿`GameLoopCoordinator` se activa (cutover Fase 4) o se elimina? 🟡
  - [ ] ¿Credenciales reales de `appsettings.json` se rotan y mueven a User Secrets? 🔴
  - [ ] ¿`EncrypterHelper` se reescribe con IV aleatoria por cifrado? 🔴
  - [ ] ¿`FormImage` con path hardcoded `C:\Code\Poker\...\Games` se hace configurable? 🔴
  - [ ] ¿La traineddata vive duplicada (embebida + output) o se elimina una? 🔴
  - [ ] ¿`FrmMain` se refactoriza Fase 7 (split por pestañas en partial classes)? 🟡
  - [ ] ¿Filtro `"NL H"` global de `FormListApps` se vuelve configurable? 🟡

---

## Tareas

> Cada tarea referencia el archivo legado de origen y su número de línea cuando aplica.

### Bloque A — Estructura del proyecto

- [ ] **T-01** Crear `OpenScrape.App.csproj` con `<OutputType>WinExe</OutputType>`, `<TargetFramework>net10.0-windows</TargetFramework>`, `<UseWindowsForms>true</UseWindowsForms>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<ApplicationIcon>...</ApplicationIcon>`.
  - Origen en el legado: `src/OpenScrape.App/OpenScrape.App.csproj`
  - Crit. de pronto: `dotnet build` produce un `.exe` ejecutable.
  - Confianza: 🟢

- [ ] **T-02** Declarar `<ProjectReference>` a `OpenScrape.Domain`, `OpenScrape.Features`, `OpenScrape.Infrastructure` y `OpenScrape.DecisionMaker`.
  - Origen en el legado: `OpenScrape.App.csproj` § `ItemGroup` con ProjectReference
  - Crit. de pronto: `dotnet list reference` muestra las cuatro referencias.
  - Confianza: 🟢

- [ ] **T-03** Declarar `<EmbeddedResource>` para `Resources/tessdata/eng.traineddata` y `<None Update="tessdata/eng.traineddata"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` para los assets de Tesseract.
  - Origen en el legado: `OpenScrape.App.csproj` § Resources
  - Crit. de pronto: el build coloca `eng.traineddata` en el directorio output Y la traineddata es accesible vía `Assembly.GetManifestResourceStream("OpenScrape.App.Resources.tessdata.eng.traineddata")`.
  - Confianza: 🟢

- [ ] **T-04** Configurar `Properties/launchSettings.json` con perfil `OpenScrape.App` y `DOTNET_ENVIRONMENT=Development`.
  - Origen en el legado: `src/OpenScrape.App/Properties/launchSettings.json`
  - Crit. de pronto: lanzar desde Visual Studio levanta la app con env var `Development`.
  - Confianza: 🟢

### Bloque B — Helpers (P-Invoke, captura, preprocesamiento, criptografía)

- [ ] **T-05** Implementar `Helpers/CaptureWindowsHelper.cs` con `User32` y `GDI32` static classes anidadas. Métodos: `SetProcessDPIAware`, `PrintWindow`, `BitBlt`, `GetWindowRect`, `EnumWindows`, `GetWindowText`, `IsWindowVisible`. Pipeline `GetWindowsScreenAsync(IntPtr handle)`: `SetProcessDPIAware → PrintWindow PW_RENDERFULLCONTENT (fallback BitBlt) → escalar con NearestNeighbor → DPI 600`. Helper `BinaryImage` con `LockBits` unsafe pointers para grayscale + threshold.
  - Origen en el legado: `src/OpenScrape.App/Helpers/CaptureWindowsHelper.cs:1-245`
  - Crit. de pronto: capturar una ventana con `WS_VISIBLE` produce un `Bitmap` no-nulo a DPI 600. Test con `notepad.exe` abierto.
  - Confianza: 🟢

- [ ] **T-06** Implementar `Helpers/ImagePreprocessorHelper.cs` con pipeline OCR: `resize(×2 si <1000 px) → FastGrayscale (Format8bppIndexed + paleta + Parallel.For) → mediana 3×3 → FastContrast(1.5) → Deskew con matriz de rotación → FastBinarize`. Cada paso es opcional vía parámetros booleanos.
  - Origen en el legado: `src/OpenScrape.App/Helpers/ImagePreprocessorHelper.cs:1-407`
  - Crit. de pronto: input 800×600 RGB → output `Format8bppIndexed` deskewed binarizado en <30 ms. Test contra dataset legado de 10 cartas.
  - Confianza: 🟢

- [ ] **T-07** Implementar `Helpers/CoordinateScaler.cs` (clase + `ICoordinateScaler` interface). Método `Initialize(refW, refH)` one-shot (lock interno para idempotencia); `ScaleRegion(posX, posY, w, h, currentW, currentH) → (x, y, w, h)` con `scale = (currentW/refW + currentH/refH)/2.0`. Si nunca se invocó `Initialize`, devuelve coords sin escalar.
  - Origen en el legado: `src/OpenScrape.App/Helpers/CoordinateScaler.cs:1-45`
  - Crit. de pronto: tests cubriendo (a) sin init → coords identidad, (b) con init y captura igual → coords identidad, (c) con captura distinta → factor promedio aplicado.
  - Confianza: 🟢

- [ ] **T-08** Implementar `Helpers/WindowsInformationHelper.cs` con `EnumWindows` callback que filtra `.NET`, `GDI+`, `Hidden`, `DDE`, `System`, `Opera`, retorna `IEnumerable<KeyValuePair<string title, IntPtr handle>>`.
  - Origen en el legado: `src/OpenScrape.App/Helpers/WindowsInformationHelper.cs:1-82`
  - Crit. de pronto: con Visual Studio + cliente abierto, retorna ≥1 entrada con el título del cliente. Excluye títulos vacíos.
  - Confianza: 🟢

- [ ] **T-09** Implementar `Helpers/EncrypterHelper.cs` con AES-CBC + SHA256 hash de secret como key. **Nota:** la implementación legacy tiene IV fija de 16 ceros — anomalía documentada 🔴. Decidir en `questions.md` si se mantiene 1:1 o se reescribe con `RandomNumberGenerator.GetBytes(16)` y prefijo IV en ciphertext.
  - Origen en el legado: `src/OpenScrape.App/Helpers/EncrypterHelper.cs:1-145`
  - Crit. de pronto: `Encrypt("foo", "bar") + Decrypt(...)` round-trip retorna `"foo"`. Si se reescribe con IV aleatoria, dos `Encrypt("foo", "bar")` consecutivos producen ciphertexts distintos.
  - Confianza: 🟡 (depende de decisión humana sobre el bug)

- [ ] **T-10** Implementar `Helpers/HandHelper.cs` (`GetSuitHand(text) → int`, `GetForceHand(text) → int`) con `switch` char→int y `Helpers/UserHandHelper.cs` (`SetHandValue(state)` formatea `"AKs"`/`"AKo"`, `Exist4Bet(state)` cuenta raises >1BB ordenados por position).
  - Origen en el legado: `src/OpenScrape.App/Helpers/HandHelper.cs:1-44`, `Helpers/UserHandHelper.cs:1-48`
  - Crit. de pronto: `SetHandValue({As, Kh})` → `"AKo"`; `Exist4Bet` con 4 raises → `true`.
  - Confianza: 🟢

- [ ] **T-11** Implementar `Helpers/ColorHelper.cs` (`GetRGBColor(GetRGBColorRequest) → (R, G, B)`), `Helpers/AppThemeHelper.cs` (paleta `PrimaryDark`/`PrimaryLight`/`Accent` + estados `Success`/`Warning`/`Danger`), `Helpers/PlayerRegionParser.cs` (regex `p(\d+){extraText}` para `GetPlayerNumber`), `Helpers/ObtainActionHelper.cs` (random weighted selection).
  - Origen en el legado: `Helpers/ColorHelper.cs:1-44`, `Helpers/AppThemeHelper.cs:1-23`, `Helpers/PlayerRegionParser.cs:1-25`, `Helpers/ObtainActionHelper.cs:1-50`
  - Crit. de pronto: `GetPlayerNumber("p3card1", "card1") → 3`. `RandomWeightedSelect([(80, "Bet"), (20, "Check")])` produce ~80/20.
  - Confianza: 🟢

### Bloque C — Entities y DTOs operacionales

- [ ] **T-12** Implementar `Entities/PlayerGameState.cs` (clase mutable con campos hero, pot, position, bet, stack, situation, players, boardCards + computed `HavePocketPair`, `IsSuited`).
  - Origen en el legado: `src/OpenScrape.App/Entities/PlayerGameState.cs:1-64`
  - Crit. de pronto: `new PlayerGameState()` produce instancia con campos default.
  - Confianza: 🟢

- [ ] **T-13** Implementar `Entities/Player.cs` (DTO con Name/Alias/Dealer/Bet/Stack/Active/SitOut/Empty/HasFolded/BigBlind/SmallBlind/Position/ValuePosition/`WasPreflopAggressor`).
  - Origen en el legado: `src/OpenScrape.App/Entities/Player.cs:1-22`
  - Crit. de pronto: tests de mutación de cada campo + `Empty` y `SitOut` son mutuamente excluyentes.
  - Confianza: 🟢

- [ ] **T-14** Implementar `Entities/BoardTextures.cs` con dos enums: `TurnBoardTexture { Dry, Coordinated, Paired }` y `RiverBoardTexture { Dry, Coordinated, Paired }`.
  - Origen en el legado: `src/OpenScrape.App/Entities/BoardTextures.cs:1-5`
  - Crit. de pronto: enums declarados; los valores 0/1/2 estables (no se renombran).
  - Confianza: 🟢

- [ ] **T-15** Implementar `Entities/TableScrapeFlopResult.cs` con 4 clases anidadas: `BoardTexture` (IsCoordinated/Rainbow/Connected/Paired/Dry/HighestRank/LowestRank/HasAce/HasKing) + `HeroHandStrength` (HasTopPair/HasOverPair/HasTwoPair/HasSet/...) + `DrawingOpportunities` (HasFlushDraw/HasStraightDraw/HasBackdoorFlushDraw) + computed `HasStrongHand`/`HasWeakHand`/`ShouldContinue`.
  - Origen en el legado: `src/OpenScrape.App/Entities/TableScrapeFlopResult.cs:1-104`
  - Crit. de pronto: tests de las 9 propiedades computadas con casos representativos.
  - Confianza: 🟢

- [ ] **T-16** Implementar `Models/BestHandResult.cs` (`internal sealed record (HandRank Ranking, List<NormalizedCard> Cards, List<NormalizedCard> Kickers)`), `Models/HandEvaluationResult.cs`, `Models/NormalizedCard.cs`, `Models/Region.cs`.
  - Origen en el legado: `src/OpenScrape.App/Models/`
  - Crit. de pronto: records públicos compilables; serializables a JSON para debug logs.
  - Confianza: 🟢

### Bloque D — Configuración y feature flags

- [ ] **T-17** Implementar `Configuration/FeatureFlags.cs` (sealed, `UseGameLoopCoordinator: bool = false`).
  - Origen en el legado: `src/OpenScrape.App/Configuration/FeatureFlags.cs:1-9`
  - Crit. de pronto: `IOptions<FeatureFlags>` se hidrata desde `appsettings.json` § `FeatureFlags`. Default `false`.
  - Confianza: 🟢

- [ ] **T-18** Implementar `Configuration/GameLoopOptions.cs` (sealed, `CaptureIntervalMs=100`, `StopTimeoutMs=2000`, `SectionName="GameLoop"`).
  - Origen en el legado: `src/OpenScrape.App/Configuration/GameLoopOptions.cs:1-11`
  - Crit. de pronto: `IOptions<GameLoopOptions>` configurable desde `appsettings.json` § `GameLoop`.
  - Confianza: 🟢

- [ ] **T-19** Generar `appsettings.json` template con secciones: `StrategyProfile` (~150 parámetros), `OverlayConfig`, `FeatureFlags`, `GameLoop`, `CaptureSettings`, `TextBoxLogger`, `Marten`. **Importante:** los placeholders de credenciales son `CHANGE_ME` (no valores reales). Anomalía Scout 🔴: la versión legacy contiene credenciales reales.
  - Origen en el legado: `src/OpenScrape.App/appsettings.json` (~31 KB)
  - Crit. de pronto: `appsettings.json` cargable sin errores; `StrategyProfileValidator.Validate` pasa con los thresholds de demo.
  - Confianza: 🟡

- [ ] **T-20** Generar `appsettings.Development.json` con connection string real + `EncryptionKey` real, **gitignored**.
  - Origen en el legado: `src/OpenScrape.App/appsettings.Development.json` (310 bytes)
  - Crit. de pronto: archivo en `.gitignore`. `git status` no lo muestra.
  - Confianza: 🟢

- [ ] **T-21** Copiar 15 JSON de estrategia + cartas + regiones a `Data/`: `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json`, `Cold4Bet.json`, `FourBet.json`, `RaiseOverLimpers.json`, `RaiseVsSbLimp.json`, `VsSqueeze.json`, `VsThreeBetAndCall.json`, `Cartas2.json`, `Regiones.json`, `Regiones3.json`, `RegionToTest.json`, `tableMap.json`. Build action `Content` con `CopyToOutputDirectory=PreserveNewest`.
  - Origen en el legado: `src/OpenScrape.App/Data/*.json`
  - Crit. de pronto: tras build, el directorio output contiene los 16 JSON.
  - Confianza: 🟢

### Bloque E — Telemetría

- [ ] **T-22** Implementar `Telemetry/Histogram.cs` (sealed) con 30 buckets logarítmicos `bound[i] = 1e-5 × 10^(i × 0.2)` segundos. Métodos `Add(TimeSpan)` O(30) lineal, `GetPercentile(p)` O(30) acumulado. Sobrestima ~37 % nunca subestima. **NO thread-safe** — sincronización externa.
  - Origen en el legado: `src/OpenScrape.App/Telemetry/Histogram.cs:1-88`
  - Crit. de pronto: tests cubren (a) `Add(10µs) → bucket 0`, (b) `GetPercentile(50)` con 1000 muestras gaussianas dentro del rango sobrestimado, (c) overflow → último bucket.
  - Confianza: 🟢

- [ ] **T-23** Implementar `Telemetry/MetricsSnapshot.cs` (sealed record `CurrentHandId, IReadOnlyDictionary<string, HistogramSnapshot> LastHand, Session`).
  - Origen en el legado: `src/OpenScrape.App/Telemetry/MetricsSnapshot.cs:1-11`
  - Crit. de pronto: serializable, inmutable.
  - Confianza: 🟢

- [ ] **T-24** Implementar `Telemetry/ScopedMeasurement.cs` (`readonly struct` + `IDisposable`). `Stopwatch.GetTimestamp` start/end, distingue `sessionOnly` para `Persistence.SaveHand`. Zero-allocation.
  - Origen en el legado: `src/OpenScrape.App/Telemetry/ScopedMeasurement.cs:1-36`
  - Crit. de pronto: `using var m = new ScopedMeasurement(collector, "X")` no genera allocations en heap (verificar con BenchmarkDotNet).
  - Confianza: 🟢

- [ ] **T-25** Implementar `Telemetry/TelemetryCategories.cs` (static) con 17 const + `DisplayOrder` (orden UI) + `SessionOnly` (HashSet). **Contrato estable — no renombrar sin migración.**
  - Origen en el legado: `src/OpenScrape.App/Telemetry/TelemetryCategories.cs:1-65`
  - Crit. de pronto: las 17 const existen exactamente con esos nombres. `SessionOnly` contiene `Persistence.SaveHand`.
  - Confianza: 🟢

- [ ] **T-26** Implementar `Telemetry/IMetricsCollector.cs` (interfaz: `Measure`, `Record`, `MeasureSessionOnly`, `RecordSessionOnly`, `StartHand`, `EndHand`, `SnapshotSession`, `ResetSession`).
  - Origen en el legado: `src/OpenScrape.App/Telemetry/IMetricsCollector.cs:1-58`
  - Crit. de pronto: contrato compilable; mock para tests `NullMetricsCollector` opcional.
  - Confianza: 🟢

- [ ] **T-27** Implementar `Telemetry/MetricsCollector.cs` (sealed, `IMetricsCollector`). Thread-safe per-category vía `ConcurrentDictionary<string, CategoryState>`. `CategoryState` con `object Lock + Histogram LastHand + Histogram Session`. `EndHand` excluye categorías presentes en `TelemetryCategories.SessionOnly`. `StartHand` con mano pendiente loggea warning.
  - Origen en el legado: `src/OpenScrape.App/Telemetry/MetricsCollector.cs:1-149`
  - Crit. de pronto: tests cubren (a) concurrencia con 4 threads × 10K records, (b) `StartHand` + `EndHand` retorna aggregate sin SessionOnly, (c) `SnapshotSession` no muta el estado.
  - Confianza: 🟢

### Bloque F — Servicios singleton de soporte

- [ ] **T-28** Implementar `Services/LruCache.cs` (`internal sealed class LruCache<TKey, TValue>` thread-safe con `lock`). `Dictionary` + `LinkedList`. Métodos: `TryGet`, `Set`, `GetOrAdd`, `Clear`, `Values` (snapshot). Eviction LRU por capacidad.
  - Origen en el legado: `src/OpenScrape.App/Services/LruCache.cs:1-111`
  - Crit. de pronto: tests cubren (a) eviction al exceder capacidad, (b) `GetOrAdd` único bajo concurrencia, (c) thread-safety con 4 threads.
  - Confianza: 🟢

- [ ] **T-29** Implementar `Services/OcrService.cs` (`IDisposable`). Tesseract 5.x wrapper con `lock` global (Tesseract single-threaded), dual `LruCache`: `LruCache<string, SKBitmap>` (200) + `LruCache<ulong, string>` (500, key = `dHash` 64-bit). 4 attempts (default/lower/higher/contrast). Auto-extracción de `eng.traineddata` desde recurso embebido si falta en disco. Method `ExtractTextFromRegionAsync(imagePath, x, y, w, h, mode)`. `OnDebugImageGenerated` event para `FrmDetectionDebug`. `OcrResult` con `Text`, `Confidence` (`GetMeanConfidence`), `IsHighConfidence` (≥0.70).
  - Origen en el legado: `src/OpenScrape.App/Services/OcrService.cs:1-462`
  - Crit. de pronto: (a) primera ejecución sin `tessdata/` extrae `eng.traineddata` del resource y arranca; (b) 4 reads del mismo crop retornan misma string en cache hit; (c) crop con texto "AB12" → text="AB12" + confidence ≥0.70; (d) verificar lock con 4 threads concurrentes.
  - Confianza: 🟢

- [ ] **T-30** Implementar `Services/ImageCropperService.cs` con `FastBitmap` interno. `CropImageToBase64(image, region) → string`. Pre-filtro `dHash` Hamming distance ≤15 antes de pixel-comparison. `CalculateSimilarity` con terminación temprana cada 32 px. `WeakReference<Image>` cache para `Base64ToImage`.
  - Origen en el legado: `src/OpenScrape.App/Services/ImageCropperService.cs:1-395`
  - Crit. de pronto: (a) crop equivalente vía dHash en <5 ms; (b) cache hit en segundo `Base64ToImage` con misma string.
  - Confianza: 🟢

- [ ] **T-31** Implementar `Services/ColorDetectionService.cs` (`IDisposable`). LockBits 32bppArgb con `GCHandle.Pinned`, cache last-image-only. `Marshal.Copy` Scan0 a `_pixelData`. `GetPixelColor(image, x, y) → SKColor` BGRA.
  - Origen en el legado: `src/OpenScrape.App/Services/ColorDetectionService.cs:1-63`
  - Crit. de pronto: (a) lectura de un píxel en <1 µs; (b) misma `image` con dos llamadas no re-locks (cache hit).
  - Confianza: 🟢

- [ ] **T-32** Implementar `Services/RegionLookupCache.cs` (singleton). `Dictionary<mapId, Dictionary<regionName, Region>>` + `Dictionary<mapId, List<Region>>`. `OrdinalIgnoreCase`. `Initialize(maps)` reconstruye los diccionarios. `GetRegion(mapId, regionName)` y `GetRegions(mapId)`.
  - Origen en el legado: `src/OpenScrape.App/Services/RegionLookupCache.cs:1-64`
  - Crit. de pronto: (a) lookup O(1) verificable con BenchmarkDotNet vs `FirstOrDefault`; (b) `Initialize` idempotente.
  - Confianza: 🟢

- [ ] **T-33** Implementar `Services/CardCacheService.cs` (singleton lazy). `SemaphoreSlim` double-check. `await using session = LightweightSession`, query 52 `Card` → `List<CardDTO>`. Una sola query Marten por vida de la app.
  - Origen en el legado: `src/OpenScrape.App/Services/CardCacheService.cs:1-44`
  - Crit. de pronto: (a) primera llamada hace 1 query; (b) llamadas 2..N reusan el cache; (c) test concurrente con 10 hilos solo dispara 1 query.
  - Confianza: 🟢

- [ ] **T-34** Implementar `Services/ActionFormatter.cs` (sealed, `IActionFormatter`). `EnrichActionWithBBAmount(action, villains, bigBlind) → string` parsea `"Bet 2.5x"` → calcula `totalBB = pot × 2.5 / bigBlind` y agrega `(N.NBB)`.
  - Origen en el legado: `src/OpenScrape.App/Services/ActionFormatter.cs:1-49`
  - Crit. de pronto: tests de (a) `Bet 2.5x` con pot 10, BB 0.50 → `"Bet 2.5x (50.0BB)"`; (b) action sin `x` retorna sin modificar.
  - Confianza: 🟢

- [ ] **T-35** Implementar `Services/OverlayPositioner.cs` (sealed, `IOverlayPositioner`). `Calculate(winLeft, winTop, winW, winH, ovrW, ovrH, hOff, vOff) → (x, y)` con `centerX = winLeft + winW/2; x = centerX - ovrW/2 - hOff; y = winBottom - vOff`.
  - Origen en el legado: `src/OpenScrape.App/Services/OverlayPositioner.cs:1-31`
  - Crit. de pronto: tests con 5 ventanas de tamaños distintos, comparar con valor legacy.
  - Confianza: 🟢

- [ ] **T-36** Implementar `Services/PositionCalculator.cs` (static). `AssignAllPositions(dealerPos, players)`: heads-up SB/BB, moving blinds (saltan SitOut consecutivos a la izquierda), labels Early/Middle/CutOff por `effectiveCount`.
  - Origen en el legado: `src/OpenScrape.App/Services/PositionCalculator.cs:1-114`
  - Crit. de pronto: tests cubren (a) heads-up con dealer en seat 1 → SB=1, BB=2; (b) 9-max con SitOut en seat 4 → BB salta al siguiente; (c) `effectiveCount=6` → labels coherentes con BBzar.
  - Confianza: 🟢

- [ ] **T-37** Implementar `Services/StrategyProfileService.cs` (clase). Wrapper sobre `IOptions<StrategyProfile>`. `GetBluffFrequency(BoardPosition)` switch para Flop/Turn/River.
  - Origen en el legado: `src/OpenScrape.App/Services/StrategyProfileService.cs:1-33`
  - Crit. de pronto: lecturas de profile devuelven valores de `appsettings.json`.
  - Confianza: 🟢

- [ ] **T-38** Implementar `Services/StrategyProfileValidator.cs` (static). Fail-fast: valida ~30 thresholds requeridos (Flop/Turn/River × 12 situaciones), rangos `[0,100]`, orden tiers `FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove`. Acumula errores en `StrategyProfileValidationException`.
  - Origen en el legado: `src/OpenScrape.App/Services/StrategyProfileValidator.cs:1-97`
  - Crit. de pronto: (a) profile válido pasa sin excepción; (b) profile con `FoldBelow=80, ThinValueAbove=70` lanza excepción que lista la combinación inválida.
  - Confianza: 🟢

### Bloque G — Servicios scoped (state machine, layout, screen reader, coordinator, persistencia)

- [ ] **T-39** Implementar `Services/GameLoopStateMachine.cs` (singleton, `lock(_stateLock)`). 10 estados (`WaitingForHand → HandDetected → PreflopAction → Flop/Turn/River*Detected/Action → HandComplete`). Diccionario `_validTransitions` declarativo. Métodos: `TryTransition(state)`, `TryTransition(state, visibleBoardCards)` con validación cruzada (≥3 flop, ≥4 turn, ≥5 river), `ForceState(state)` (test/debug, valida estado destino existe), `Reset()`. Properties `IsPreflop`, `IsFlop`, `IsTurn`, `IsRiver`, `IsWaiting`, `IsHandComplete`, `IsInStreet(detected, action)`. Const `MaxOcrRetries=2`.
  - Origen en el legado: `src/OpenScrape.App/Services/GameLoopStateMachine.cs:1-166`
  - Crit. de pronto: (a) `TryTransition(invalid)` retorna `false` con LogWarning; (b) `TryTransition(TurnDetected, 3)` bloquea con LogWarning; (c) `ForceState(invalidEnum)` rechaza con LogError; (d) tests concurrentes con 4 threads no corrompen estado.
  - Confianza: 🟢

- [ ] **T-40** Implementar `Services/IPostflopContextHolder.cs` + `Services/PostflopContextHolder.cs` (sealed, scoped). Wrapper thread-safe sobre `PostflopGameContext`. `Volatile.Read` para `Current`. `lock(_gate)` para `Update(Func<T,T>)` y `StartNewHand()`.
  - Origen en el legado: `src/OpenScrape.App/Services/PostflopContextHolder.cs:1-35`
  - Crit. de pronto: (a) `Update` produce nueva instancia inmutable; (b) `StartNewHand` resetea a contexto vacío; (c) tests concurrentes con 4 threads no corrompen el record.
  - Confianza: 🟢

- [ ] **T-41** Implementar `Services/IScreenReaderService.cs` + `Services/ScreenReaderService.cs` (singleton). OCR multi-lectura con consenso (3 reads + cleanup) para player names, bets, stacks, hand numbers, text genérico. Normalización post-OCR para artefactos `"8"` y separadores decimales perdidos. Métodos: `ReadPlayerName(image, region)`, `ReadBet(image, region, potHint)`, `ReadStack(image, region)`, `ReadHandNumber(image, region)`, `ReadGenericText(image, region)`. Métricas via `_metrics.Measure("OCR.*")`.
  - Origen en el legado: `src/OpenScrape.App/Services/ScreenReaderService.cs:1-521`
  - Crit. de pronto: (a) 3 reads con OCR ruidoso → mayoría wins; (b) "593" con decimal perdido normalizado a "5,93" si pot context valida; (c) artefacto "8" insertado por Tesseract removido.
  - Confianza: 🟢

- [ ] **T-42** Implementar `Services/ITableLayoutService.cs` + `Services/TableLayoutService.cs` (scoped). Detección dealer (color RGB 200/140/80 dorado en radio 3 px, reintenta cada loop si `Position == None`), posiciones (delegado a `PositionCalculator`), aliases (`ReadPlayerName` + regex validación), estados Empty/SitOut/Active/Folded. Métodos: `SetDealerPlayer`, `SetEmptyPlayer`, `SetSitOutPlayer`, `SetActivePlayer`, `InitializePlayers`, `RefreshPlayerStates`, `ValidatePlayerStates`, `RetryEmptyAliases`, `SetVillainPosition`, `ResetDealerState`. Property `DealerValuePosition`. Métricas via `_metrics.Measure("Layout.*")`.
  - Origen en el legado: `src/OpenScrape.App/Services/TableLayoutService.cs:1-664`
  - Crit. de pronto: (a) imagen con dealer button en seat 3 → `DealerValuePosition=3`; (b) seat con OCR fallido → estado `Empty`; (c) `RefreshPlayerStates` infiere folded por desaparición de bets sin leer texto; (d) `RetryEmptyAliases` reintenta hasta 3 ciclos.
  - Confianza: 🟢

- [ ] **T-43** Implementar `Services/GameLoggerService.cs` (scoped, sin interfaz). Lifecycle de sesiones/manos en Marten. `_currentSession: GameSession?`, `_currentHand: HandRecord?`, `_dbWriteLock: SemaphoreSlim(1,1)`. `_sessionTotalHands`, `_sessionTotalProfit` con `Interlocked` (acumuladores fuente de verdad porque `MaxHandsInMemory=20` trunca). Métodos: `StartSessionAsync(sessionId, tableName, bigBlind=0.5)`, `StartNewHandAsync(handNumber, hero1, hero2, position, stack, numOpp, blindPosted=0)`, `LogStreetDecision(...)`, `UpdateBoard`, `UpdatePotSize`, `UpdateSituation`, `EndHand(prevStack, finalStack)`, `FinalizeAndPersistHandAsync`, `SetTelemetry(aggregate)`, `GetRecentSessionsWithStatsAsync`, `GetHandsForSessionAsync`. Correlation scopes via `BeginScope({ SessionId, TableName, HandNumber })`.
  - Origen en el legado: `src/OpenScrape.App/Services/GameLoggerService.cs:1-374`
  - Crit. de pronto: (a) `StartSessionAsync` con mismo `sessionId` no resetea acumuladores; (b) `EndHand(prevStack=87, finalStack=92)` produce `HandRecord.Profit=+5BB`, `Result=Won`; (c) tras 25 manos, `GameSession.Hands.Count=20` pero `_sessionTotalHands=25`; (d) `BeginScope` propaga `HandNumber` a todos los logs hasta `FinalizeAndPersistHandAsync`.
  - Confianza: 🟢

- [ ] **T-44** Implementar `Services/IGameCoordinator.cs` + `Services/GameCoordinator.cs` (scoped, ~793 LOC). Centralizar decisiones postflop. Construir `PostflopDecisionInput` (record con 30+ campos) y delegar a `IPostflopDecisionService.DetermineAction`. Métodos: `DetermineFlopAction`, `DetermineTurnAction`, `DetermineRiverAction` (canónicos, ~150 LOC c/u). Helpers: `GetOpponentBetSize`, `GetActiveVillainId`, `GetVillainType(state, heroIsInPosition)`, `GetActiveVillainProfile`, `GetVillainStack` (fallback a heroStack si OCR=0), `HeroBlocksTopBoardCard`, `FormatCardsForLog`, `DetectDonkBet` (con cross-street check de HeroBetFlop/HeroBetTurn), `AdjustBetSize` (parsea fraction → `BetSizingService.CalculateDynamicBetSize`). Board analysis: `AnalyzeBoardChange(boardCards, previousCardCount)`, `AnalyzeTurnBoardTexture(boardCards)`, `AnalyzeRiverBoardTexture(boardCards)`. `DetectNewHand(state, handNumberChanged, currentHand, ref previousDealer/SB/BB)` con 7 indicadores. Tracking: `TrackVillainPostflopAction(state, maxBet, isPreflopAggressor, heroIsInPosition)`. Properties: `FlopResult`, `TurnResult`, `RiverResult`, `TurnBoardTexture`, `RiverBoardTexture`. `ResetContext()`.
  - Origen en el legado: `src/OpenScrape.App/Services/GameCoordinator.cs:1-793`
  - Crit. de pronto: (a) `DetermineFlopAction` con TPTK + dry board retorna `"Bet 1/2"`; (b) `DetectNewHand` con `handNumberChanged=true + 1 indicador` retorna `true`; (c) `DetectDonkBet` con `villainAggressor + heroNotPreviousAggressor` retorna `IsDonkBet=true`; (d) `AdjustBetSize` con `"Bet Pot"` aplica multiplicador SPR y multiway.
  - Confianza: 🟢

- [ ] **T-45** Implementar `Services/IUiSyncService.cs` + `Services/UiSyncService.cs` (sealed, scoped) + `Services/NullUiSyncService.cs` (no-op para tests). `BeginInvoke` cross-thread router. `Attach(form)`/`Detach()` lock-protected. `Post(GameLoopResult)` despacha a UI thread.
  - Origen en el legado: `src/OpenScrape.App/Services/UiSyncService.cs:1-130`, `NullUiSyncService.cs:1-18`
  - Crit. de pronto: (a) `Attach` registra el form; (b) `Post` desde thread no-UI llega a UI thread vía `BeginInvoke`; (c) `Detach` después de `Attach` no produce excepciones.
  - Confianza: 🟢

- [ ] **T-46** Implementar `Services/IGameLoopCoordinator.cs` + `Services/GameLoopCoordinator.cs` (sealed, scoped, `IAsyncDisposable`). Esqueleto del game loop independiente del `BackgroundWorker`. `SemaphoreSlim` start/stop. `PeriodicTimer` configurable (`GameLoopOptions.CaptureIntervalMs`). Evento `ResultReady`. `SafeEmit` con catch por suscriptor. **Bajo feature flag `UseGameLoopCoordinator=false` retorna `GameLoopResult { Empty = true }` por tick** — migración Fase 4 pendiente.
  - Origen en el legado: `src/OpenScrape.App/Services/GameLoopCoordinator.cs:1-213`
  - Crit. de pronto: (a) `StartAsync` arranca el `PeriodicTimer`; (b) `StopAsync` cancela y dispone limpiamente; (c) `IsRunning` consistente; (d) excepción en suscriptor no rompe loop ni mata otros suscriptores.
  - Confianza: 🟡

- [ ] **T-47** Implementar `Services/PokerDecisionFacade.cs` (sealed, scoped, `IPokerDecisionFacade`). 5 fases medidas: equity → texture → profile → decision → sizing. **Planeado como cutover, NO usado en producción** (FrmMain consume directamente `IPokerCalculator` y `IPostflopDecisionService`). Decidir en `questions.md` si se mantiene o elimina.
  - Origen en el legado: `src/OpenScrape.App/Services/PokerDecisionFacade.cs:1-231`
  - Crit. de pronto: si se mantiene, las 5 fases se miden con `MetricsCollector` y producen mismo resultado que el camino directo.
  - Confianza: 🟡 (depende de decisión humana)

### Bloque H — Logging (TextBox sink + provider)

- [ ] **T-48** Implementar `Services/Logging/TextBoxLoggerOptions.cs` (clase) con `MinimumLevel`, `MaxLines`, `BufferUntilTargetReady`, `BufferCapacity`.
  - Origen en el legado: `src/OpenScrape.App/Services/Logging/TextBoxLoggerOptions.cs`
  - Crit. de pronto: hidratable desde `appsettings.json` § `TextBoxLogger`.
  - Confianza: 🟢

- [ ] **T-49** Implementar `Services/Logging/TextBoxLogger.cs` (sealed, `ILogger`). Render `[HH:mm:ss LVL CategoryShort] {Scopes} message | ExceptionType: msg`. Scopes via `IExternalScopeProvider`. `LevelToken` map (TRC/DBG/INF/WRN/ERR/CRT).
  - Origen en el legado: `src/OpenScrape.App/Services/Logging/TextBoxLogger.cs:1-120`
  - Crit. de pronto: (a) log con scope `{ HandNumber: 42 }` produce `... [HandNumber=42] message`; (b) log con exception incluye `| ExceptionType: msg`.
  - Confianza: 🟢

- [ ] **T-50** Implementar `Services/Logging/TextBoxLoggerProvider.cs` (sealed, `ILoggerProvider`, `ISupportExternalScope`, `[ProviderAlias("TextBox")]`). Buffer pendientes hasta `SetTextBoxTarget(textBox)` (one-shot). `BeginInvoke` cross-thread + `AppendWithRotation` por `MaxLines` (FIFO).
  - Origen en el legado: `src/OpenScrape.App/Services/Logging/TextBoxLoggerProvider.cs:1-138`
  - Crit. de pronto: (a) 5 logs antes de `SetTextBoxTarget` se bufferizan en orden; (b) tras `SetTextBoxTarget`, los 5 se flushean al TextBox; (c) un segundo `SetTextBoxTarget` se ignora (one-shot).
  - Confianza: 🟢

- [ ] **T-51** Implementar `Services/Logging/TextBoxLoggerExtensions.cs` (static) con `AddTextBoxLogger(this ILoggingBuilder)` extension.
  - Origen en el legado: `src/OpenScrape.App/Services/Logging/TextBoxLoggerExtensions.cs`
  - Crit. de pronto: `lb.AddTextBoxLogger()` registra `TextBoxLoggerProvider` como singleton.
  - Confianza: 🟢

- [ ] **T-52** Implementar `Services/DetectionLoggerService.cs` (sin interfaz, singleton). Logger JSON file-based (1 archivo/día) para detección de turnos. Métodos: `LogColorDetection`, `LogTurnDetected`, `LogConfigurationChange`, `LogDetectionStatistics`, `LogDetectionError`, `SaveDebugScreenshot` (con cruz roja en punto detectado), `CleanupOldLogs` (>30 días).
  - Origen en el legado: `src/OpenScrape.App/Services/DetectionLoggerService.cs:1-362`
  - Crit. de pronto: (a) cada llamada genera entrada JSON en `resources/logs/{yyyy-MM-dd}.jsonl`; (b) `SaveDebugScreenshot` con coords (50, 100) dibuja cruz roja en (50, 100); (c) `CleanupOldLogs` borra archivos >30 días.
  - Confianza: 🟢

### Bloque I — Use cases (Aplication/)

- [ ] **T-53** Implementar `Aplication/UseCases/UnifiedPokerCalculator.cs` (`IPokerCalculator`). 8 pasos pipeline: pot odds → equity (cache `ConcurrentDictionary` con `EquityCacheMaxSize=2048`) → outs → hand evaluation → board texture → fold equity → EV → bet sizing recomendado. `ClassifyPair` distingue Overpair/TopPair/MiddlePair/BottomPair/PocketPairUnder/BoardPaired. `Calculate(playerHand, communityCards, pot, betToCall, numOpp, mcIters?, isInPos, heroStack, villainStack, situation?, villainPos, profile?) → PokerCalculationResult` con 17 campos.
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs:1-476`
  - Crit. de pronto: (a) preflop AKs → equity ~45% heads-up; (b) flop con TPTK + dry → `RecommendedAction="Bet 1/2"`; (c) cache hit en segunda llamada con misma key.
  - Confianza: 🟢

- [ ] **T-54** Implementar `Aplication/SetPreflopActionUseCase.cs` (`ISetPreflopActionUseCase`, scoped). Cascada 11 ramas según `HandSituation` (`OpenRaise`, `RaiseOverLimper`, `BBvsSB`, `ThreeBet`, `Vs3Bet`, `FourBet`, `Cold4Bet`, `Squeeze`, `VsSqueeze`, `Hero3BetAndOpenRaiser4Bet`, `HeroCallOpenRaiseAndGetSqueeze`). Compone 10 sub-cases `GetActionXxxUseCase` que delegan a `ActionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation, ActionScenarioRequest)`. Detección Squeeze → FourBet (OCR-4 fix).
  - Origen en el legado: `src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs:1-250`
  - Crit. de pronto: (a) HU con OpenRaise → `GetActionOpenRaiseUseCase`; (b) Squeeze que se reabre como 4Bet → `GetActionCold4BetUseCase`; (c) tests con cada situación retornan `ResponseAction` no nulo.
  - Confianza: 🟢

- [ ] **T-55** Implementar `Aplication/SetFlopForceBoardUseCase.cs` (`ISetFlopForceBoardUseCase`, singleton). Pobla `BoardTexture`/`HeroHandStrength`/`Draws` de `TableScrapeFlopResult` desde `BoardData[]`.
  - Origen en el legado: `src/OpenScrape.App/Aplication/SetFlopForceBoardUseCase.cs`
  - Crit. de pronto: input `[As Kd 7c]` → `IsRainbow=false, IsCoordinated=false, HasAce=true`.
  - Confianza: 🟢

- [ ] **T-56** Implementar `Aplication/UseCases/GetCardsFlopUseCase.cs` (115 LOC), `GetCardsTurnUseCase.cs`, `GetCardsRiverUseCase.cs`. OCR de cartas via `dHash` compare contra `CardCacheService` (52 cartas). Retorna `BoardData[]`. Usa `ICoordinateScaler` para regiones escaladas.
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs:1-115`, equivalente Turn/River
  - Crit. de pronto: (a) flop visible con 3 cartas → array de 3 elementos; (b) flop sin cartas → array vacío.
  - Confianza: 🟢

- [ ] **T-57** Implementar `Aplication/UseCases/OutsCalculatorUseCase.cs` (`IOutsCalculatorUseCase`, singleton). Calcula 12 tipos de draws (Flush/Straight/Gutshot/Sets/FullHouse/Overcards/2Pair/DoubleGutshot/StraightFlush/4ofKind/3ofKind/OnePair/HighCard) con probabilidad combinatoria exacta (no regla 4-2). Retorna `HandStrength` con `DrawProbability` por tipo.
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/OutsCalculatorUseCase.cs:1-514`
  - Crit. de pronto: tests cubren flush draw (9 outs, ~36% turn+river) y open-ended straight (8 outs, ~31.5%).
  - Confianza: 🟢

- [ ] **T-58** Implementar `Aplication/UseCases/PotOddsCalculator.cs` (`IPotOddsCalculator`). Suma outs únicos (Flush+Straight+Sets) con `DistinctBy(Id)` y aplica regla 4-2. Wrapper legacy.
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/PotOddsCalculator.cs:1-84`
  - Crit. de pronto: input flush+straight overlap → outs únicos (no duplicados).
  - Confianza: 🟢

- [ ] **T-59** Implementar `Aplication/UseCases/SaveTableMapUseCase.cs`, `LoadTableMapUseCase.cs`, `SetMovementRegionUseCase.cs`, `GetWindowsScreenUseCase.cs`. Wrappers thin sobre `RegionTableMapUseCases` (Features) y `CaptureWindowsHelper`.
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/SaveTableMapUseCase.cs`, `LoadTableMapUseCase.cs`, `SetMovementRegionUseCase.cs`, `GetWindowsScreenUseCase.cs`
  - Crit. de pronto: round-trip Save+Load retorna mismas regiones.
  - Confianza: 🟢

- [ ] **T-60** Implementar `Aplication/UseCases/Actions/Get*UseCase.cs` ×10 (`OpenRaise`, `3Bet`, `Cold4Bet`, `Hero3BetAndOpenRaiser4Bet`, `HeroCallOpenRaiseAndGetSqueeze`, `RaiseOverLimper`, `RaiseVsSBLimp`, `Squeeze`, `Vs3Bet`, `Vs3BetAndCall`). Cada uno consume `ActionScenarioUseCases.GetActionScenario.ExecuteAsync(GameSituation, ActionScenarioRequest)` y retorna `ResponseAction`.
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/Actions/`
  - Crit. de pronto: cada use case tiene tests unitarios con request/response esperados.
  - Confianza: 🟢

- [ ] **T-61** Implementar `Aplication/GetCropImageUseCase.cs` y `GetHashImageUseCase.cs`. Wrappers sobre `ImageCropperService.CropImageToBase64` y `ImageCropperService.ComputeDHash`.
  - Origen en el legado: `src/OpenScrape.App/Aplication/GetCropImageUseCase.cs`, `GetHashImageUseCase.cs`
  - Crit. de pronto: round-trip crop+hash determinista.
  - Confianza: 🟢

- [ ] **T-62** Implementar `Aplication/UseCases/BaseRequest.cs`, `BaseResponse.cs`, `Helpers/FlopHelper/*` (analizadores legacy `PreFlopRaiserIPAnalyzerHelper`, `RaiseOverLimperIPAnalyzerHelper`, `RaiseOverLimperOOPAnalyzerHelper` + DTOs).
  - Origen en el legado: `src/OpenScrape.App/Aplication/UseCases/Base*.cs`, `Helpers/FlopHelper/`
  - Crit. de pronto: clases compilables; usadas como templates por use cases.
  - Confianza: 🟢

### Bloque J — Forms (UI WinForms)

- [ ] **T-63** Implementar `Forms/FrmOverlay.cs` + `FrmOverlay.Designer.cs` + `IFrmOverlay`. Form topmost sin bordes con `Color.Magenta TransparencyKey`. `TableLayoutPanel` 9 filas + action panel + `Timer` fade-in. Bordes redondeados con `GraphicsPath`. P-Invoke `WM_NCLBUTTONDOWN` para drag manual. Métodos: `UpdateAction`, `UpdateEquityPercentage`, `UpdatePotOddsPercentage`, `UpdateStreetPhase`, `UpdateTableName`, `UpdateBoardTexture`, `UpdateStreetIndicator`, `ClearAll`.
  - Origen en el legado: `src/OpenScrape.App/Forms/FrmOverlay.cs:1-561`
  - Crit. de pronto: (a) form muestra solo el contenido no-magenta; (b) drag manual mueve el form; (c) `UpdateAction("Bet 1/2")` repinta el `lbAction` cross-thread.
  - Confianza: 🟢

- [ ] **T-64** Implementar `Forms/FrmHandDetail.cs` (sin `.Designer.cs`, layout 100% programático). Popup con `RichTextBox` coloreado para Hand History. Formatea con `Color`-encoded `AppendText`, separadores `═══`, situación, decisiones por street, resultado.
  - Origen en el legado: `src/OpenScrape.App/Forms/FrmHandDetail.cs:1-135`
  - Crit. de pronto: cargar un `HandRecord` con 3 calles muestra 3 bloques `═══` con colores correctos.
  - Confianza: 🟢

- [ ] **T-65** Implementar `Forms/FrmDetectionDebug.cs` + `.Designer.cs`. Calibración OCR: `Timer 100ms`, captura zoomed `ZOOM_FACTOR=10`, controles numéricos `numX/numY/numWidth/numHeight` para ajustar coords, save/load colors.
  - Origen en el legado: `src/OpenScrape.App/Forms/FrmDetectionDebug.cs:1-366`
  - Crit. de pronto: abrir el form con una región genera un loop de capturas zoomed cada 100 ms.
  - Confianza: 🟢

- [ ] **T-66** Implementar `Forms/FormImage.cs` + `.Designer.cs` (`IAddImage`). Visor PNG navegable (←→). **Anomalía:** path hardcoded `C:\Code\Poker\ScrapePoker\resources\Games` con comentario `//portatil`. Decidir en `questions.md` si se hace configurable.
  - Origen en el legado: `src/OpenScrape.App/Forms/FormImage.cs`
  - Crit. de pronto: si se hace configurable, lee path desde `appsettings.json` § `ImageGalleryPath`.
  - Confianza: 🔴 (depende de decisión humana sobre el hardcode)

- [ ] **T-67** Implementar `Forms/FormAction.cs` + `.Designer.cs`. Popup simple con `lbAction.Text = DatoRecibido`. Usa `Color.Magenta TransparencyKey`.
  - Origen en el legado: `src/OpenScrape.App/Forms/FormAction.cs:1-41`
  - Crit. de pronto: form acepta string en ctor y muestra en label.
  - Confianza: 🟢

- [ ] **T-68** Implementar `Forms/FormListApps.cs` + `.Designer.cs`. Lista ventanas filtrando "NL H" (clientes poker NL Hold'em) via `WindowsInformationHelper.FindWindows`. Devuelve `IntPtr handle` al `IAddImage.Execute`.
  - Origen en el legado: `src/OpenScrape.App/Forms/FormListApps.cs:1-62`
  - Crit. de pronto: con cliente abierto, lista ≥1 entrada; doble clic devuelve handle.
  - Confianza: 🟡 (filtro hardcoded a "NL H" — decisión pendiente)

- [ ] **T-69** Implementar `Forms/FrmMain.cs` + `FrmMain.Designer.cs` (~4 502 LOC). 5 pestañas: **Juego** (captura, overlay, equity), **Config** (regions calibration), **Tablas** (mapa de mesa), **Logs** (`tbResume` con `TextBoxLogger` sink + Hand History blocks), **Historial** (`dgvSessions` + `dgvSessionHands` + botón Backtest A/B + Bankroll + Métricas). Constructor con 44 dependencias inyectadas. Pipeline `btnCapture_Click` ~270 LOC: captura → OCR → layout → preflop/postflop → overlay → telemetry. `BackgroundWorker1_DoWork` polling con `CaptureIntervalMs=100`. **Refactor pendiente Fase 7:** split por pestañas en partial classes.
  - Origen en el legado: `src/OpenScrape.App/Forms/FrmMain.cs:1-4502`
  - Crit. de pronto: (a) form arranca con 5 tabs; (b) `btnCapture_Click` ejecuta el pipeline completo en <600 ms; (c) `FormClosing` persiste última mano y sesión activas con `await`; (d) cierre limpio del `BackgroundWorker` y `_gameLoopCts`.
  - Confianza: 🟡 (god-class — refactor explícito pendiente)

### Bloque K — Composition root

- [ ] **T-70** Implementar `Program.cs` con `[STAThread] Main()`. `ApplicationConfiguration.Initialize()`. `Host.CreateDefaultBuilder().UseEnvironment(envVar).ConfigureLogging(lb => lb.AddTextBoxLogger())`.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:34-50`
  - Crit. de pronto: app arranca con `DOTNET_ENVIRONMENT=Development` por defecto.
  - Confianza: 🟢

- [ ] **T-71** Registrar en `ConfigureServices` (en este orden): `services.AddDataBase(config, true)`, `services.AddUseCases()`, `services.Configure<OverlayConfig>(...)`, `services.Configure<StrategyProfile>(...)`, `services.AddSingleton<StrategyProfileService>()`, `services.AddSingleton<ThresholdsRegistry>() + IFace forwarding`, `services.Configure<GameLoopOptions>(...)`, `services.Configure<FeatureFlags>(...)`.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:52-69`
  - Crit. de pronto: secciones de `appsettings.json` enlazadas a tipos correspondientes.
  - Confianza: 🟢

- [ ] **T-72** Registrar algoritmos del `DecisionMaker` con forwarding pattern (clase concreta + interfaz singleton compartiendo instancia): `MonteCarloSimulator`/`IMonteCarloSimulator`, `BitHandEvaluator`/`IHandEvaluator`, `OutsCalculator`/`IOutsCalculator`, `BoardTextureAnalyzer`/`IBoardTextureAnalyzer`, `PreflopEquityCalculator` (sin interfaz).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:71-80`
  - Crit. de pronto: `sp.GetService<IFace>() == sp.GetService<Concrete>()` (referencias iguales).
  - Confianza: 🟢

- [ ] **T-73** Registrar 13 servicios DM con forwarding pattern: `BetSizingService`, `RangePolarizer`, `PostflopDecisionService`, `OpponentTracker`, `StrategyAnalyzerService`, `ExploitabilityCalculator`, `AutoCalibrationService`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `PreflopAnalyzer`, `EquityCalculatorService`, `StrategyBacktester`, `BankrollTrackerService` (lambda factory consumiendo `IDocumentStore` + `IOptions<StrategyProfile>`).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:82-113`
  - Crit. de pronto: cada interfaz inyectada en consumers funciona; tests resuelven sin error.
  - Confianza: 🟢

- [ ] **T-74** Registrar el calculator unificado: `services.AddSingleton<IPokerCalculator, UnifiedPokerCalculator>()`. **Este es el camino real de producción** — no `PokerDecisionFacade`.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:115-116`
  - Crit. de pronto: `sp.GetService<IPokerCalculator>()` retorna `UnifiedPokerCalculator`.
  - Confianza: 🟢

- [ ] **T-75** Registrar facade scoped (`IPokerDecisionFacade`/`PokerDecisionFacade`), coordinator del game loop scoped (`IGameLoopCoordinator`/`GameLoopCoordinator`), UI sync scoped (`IUiSyncService`/`UiSyncService`).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:118-125`
  - Crit. de pronto: lifetimes correctos verificables con tests de DI.
  - Confianza: 🟢

- [ ] **T-76** Registrar helpers extraídos singleton: `IActionFormatter`/`ActionFormatter`, `IOverlayPositioner`/`OverlayPositioner`, `CoordinateScaler`/`ICoordinateScaler` (forwarding).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:127-133`
  - Crit. de pronto: instancias singleton compartidas.
  - Confianza: 🟢

- [ ] **T-77** Registrar telemetría singleton: `IMetricsCollector`/`MetricsCollector`. Compartido entre scopes y la UI para mostrar la pestaña Métricas.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:135-136`
  - Crit. de pronto: `sp.GetService<IMetricsCollector>()` retorna misma instancia desde dos scopes distintos.
  - Confianza: 🟢

- [ ] **T-78** Registrar servicios de captura/OCR singleton: `OcrService`, `ColorDetectionService`, `ImageCropperService`, `DetectionLoggerService`. Singleton porque mantienen caches caros.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:138-142`
  - Crit. de pronto: tras 2 scopes consecutivos, los caches LRU mantienen entradas.
  - Confianza: 🟢

- [ ] **T-79** Registrar use cases: singleton (`SetFlopForceBoardUseCase`, `GetHashImageUseCase`, `GetCropImageUseCase`, `OutsCalculatorUseCase`, `GetWindowsScreenUseCase`, `GetCardsFlop/Turn/RiverUseCase`) + scoped (`SetPreflopActionUseCase` — es scoped porque depende de `ActionScenarioUseCases` que es scoped).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:143-148, 173-175`
  - Crit. de pronto: `SetPreflopActionUseCase` resoluble desde scope con `ActionScenarioUseCases`.
  - Confianza: 🟢

- [ ] **T-80** Registrar screen reader singleton: `ScreenReaderService` + `IScreenReaderService` forwarding.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:150-152`
  - Crit. de pronto: forwarding correcto.
  - Confianza: 🟢

- [ ] **T-81** Registrar game logger scoped + state machine singleton + caches singleton: `GameLoggerService` (scoped), `GameLoopStateMachine` (singleton), `RegionLookupCache` (singleton), `CardCacheService` (singleton).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:154-158`
  - Crit. de pronto: `GameLoggerService` no compartido entre scopes (sesiones distintas tienen logger distinto).
  - Confianza: 🟢

- [ ] **T-82** Registrar table layout scoped (`TableLayoutService` + `ITableLayoutService` forwarding), context holder scoped (`PostflopContextHolder` + `IPostflopContextHolder`), coordinator scoped (`GameCoordinator` + `IGameCoordinator`).
  - Origen en el legado: `src/OpenScrape.App/Program.cs:160-170`
  - Crit. de pronto: lifetimes correctos.
  - Confianza: 🟢

- [ ] **T-83** Registrar `FrmMain` como `Transient`. Resolverlo desde un `CreateAsyncScope` (no del root) en `Main()` para que las dependencias scoped vivan toda la vida de la ventana. `using` con `await scope.DisposeAsync().AsTask().GetAwaiter().GetResult()` en `finally`.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:178, 213-222`
  - Crit. de pronto: cierre de la app dispone limpiamente todos los scoped (incluyendo `IAsyncDisposable` como `GameLoopCoordinator`).
  - Confianza: 🟢

- [ ] **T-84** Implementar fail-fast del `StrategyProfile` en `Main()`: tras `host.Build()`, resolver `IOptions<StrategyProfile>`, llamar a `StrategyProfileValidator.Validate(profile)`, si lanza `StrategyProfileValidationException` mostrar `MessageBox` con `ex.Message` y `Environment.Exit(1)`.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:187-197`
  - Crit. de pronto: profile inválido aborta la app antes de mostrar `FrmMain`; profile válido continúa.
  - Confianza: 🟢

- [ ] **T-85** Inicializar `CoordinateScaler` desde `appsettings.json` § `CaptureSettings`. Si `IsReferenceSet=true` y `ReferenceImageWidth/Height` parseables y >0, llamar `scaler.Initialize(refW, refH)`. Si no, dejar el scaler en modo identidad.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:199-208`
  - Crit. de pronto: con sección presente, primer `ScaleRegion` aplica factor; sin sección, devuelve coords sin escalar.
  - Confianza: 🟢

### Bloque L — Resources y assets

- [ ] **T-86** Copiar `Resources/tessdata/eng.traineddata` (~30 MB) como `EmbeddedResource`. Generar manifest resource name `OpenScrape.App.Resources.tessdata.eng.traineddata`.
  - Origen en el legado: `src/OpenScrape.App/Resources/tessdata/eng.traineddata`
  - Crit. de pronto: `Assembly.GetManifestResourceStream("OpenScrape.App.Resources.tessdata.eng.traineddata")` retorna stream no nulo.
  - Confianza: 🟢

- [ ] **T-87** Generar `Properties/Resources.Designer.cs` (auto-generado por VS) y `Properties/Settings.Designer.cs` (sealed). NO editar manualmente.
  - Origen en el legado: `src/OpenScrape.App/Properties/Resources.Designer.cs`, `Settings.Designer.cs`
  - Crit. de pronto: archivos compilables sin warning.
  - Confianza: 🟢

- [ ] **T-88** Configurar `<ApplicationIcon>` y `<AssemblyName>OpenScrape.App</AssemblyName>` en csproj. Decidir `Interfaces/IAddImage.cs` (interfaz `Execute(IntPtr window)` consumida por `FormImage`/`FormListApps`).
  - Origen en el legado: `src/OpenScrape.App/Interfaces/IAddImage.cs`
  - Crit. de pronto: el `.exe` resultante tiene icono y nombre `OpenScrape.App.exe`.
  - Confianza: 🟢

---

## Tareas de Test

### Tests unitarios (puros, sin dependencias externas)

- [ ] **TT-01** Tests de `Helpers/CoordinateScaler` (3 escenarios — sin init, igual ref, distinto ref).
- [ ] **TT-02** Tests de `Helpers/CaptureWindowsHelper.BinaryImage` (LockBits + grayscale + threshold determinista).
- [ ] **TT-03** Tests de `Helpers/HandHelper.GetSuitHand`/`GetForceHand` y `Helpers/UserHandHelper.SetHandValue`/`Exist4Bet`.
- [ ] **TT-04** Tests de `Helpers/PlayerRegionParser.GetPlayerNumber` con regex `p(\d+){extraText}`.
- [ ] **TT-05** Tests de `Services/LruCache` (eviction, GetOrAdd, concurrencia).
- [ ] **TT-06** Tests de `Services/RegionLookupCache` (lookup O(1), Initialize idempotente).
- [ ] **TT-07** Tests de `Services/GameLoopStateMachine` — todas las transiciones válidas + 3 inválidas + ForceState + validación con visibleBoardCards.
- [ ] **TT-08** Tests de `Services/PostflopContextHolder` (Update inmutabilidad, StartNewHand, concurrencia).
- [ ] **TT-09** Tests de `Services/StrategyProfileValidator` (válido, falta clave, tier mal ordenado).
- [ ] **TT-10** Tests de `Services/ActionFormatter.EnrichActionWithBBAmount` con 5 inputs.
- [ ] **TT-11** Tests de `Services/OverlayPositioner.Calculate` con 5 ventanas.
- [ ] **TT-12** Tests de `Services/PositionCalculator.AssignAllPositions` (HU, 9-max, SitOut entre blinds).
- [ ] **TT-13** Tests de `Telemetry/Histogram` (buckets, percentiles, overflow).
- [ ] **TT-14** Tests de `Telemetry/MetricsCollector` (concurrencia 4 threads × 10K records, EndHand sin SessionOnly).
- [ ] **TT-15** Tests de `Aplication/UseCases/UnifiedPokerCalculator` (preflop heads-up, flop TPTK, river facing bet).
- [ ] **TT-16** Tests de `Services/GameCoordinator.DetectNewHand` (los 7 indicadores).
- [ ] **TT-17** Tests de `Services/GameCoordinator.DetectDonkBet` (cross-street).
- [ ] **TT-18** Tests de `Services/GameCoordinator.AdjustBetSize` (Pot, fraction, multiplicador SPR).
- [ ] **TT-19** Tests de `Services/GameCoordinator.AnalyzeBoardChange/Turn/River`.

### Tests de integración (con servicios reales o fakes)

- [ ] **TT-20** Test de integración OCR: capturar imagen sintética con texto conocido → `ScreenReaderService.ReadGenericText` retorna texto correcto en ≥2 de 3 reads.
- [ ] **TT-21** Test de integración GameLogger: `StartSessionAsync → StartNewHandAsync → LogStreetDecision ×3 → EndHand → SaveSessionAsync` produce `HandRecord` y `GameSession` consultables vía Marten.
- [ ] **TT-22** Test de integración fail-fast: lanzar la app con `appsettings.json` que tiene tier inválido → `Environment.Exit(1)` antes de mostrar UI.
- [ ] **TT-23** Test de integración del pipeline `btnCapture_Click` con imagen estática → `FrmOverlay.UpdateAction` recibe acción no-vacía en <600 ms.
- [ ] **TT-24** Test de integración auto-rebuy: simular subida de stack 12 → 100 BB en bordes de mano → `_heroStackPreRebuy` preserva el valor previo.
- [ ] **TT-25** Test de integración telemetry persisted: tras `EndHand`, `HandRecord.Telemetry` contiene 16 categorías (sin `Persistence.SaveHand`).

### Tests end-to-end (manuales con cliente real)

- [ ] **TT-26** Sesión heads-up de 50 manos: validar que `DetectNewHand` detecta correctamente las 50 transiciones.
- [ ] **TT-27** Sesión 9-max de 30 manos: validar que `TableLayoutService.SetDealerPlayer` detecta el botón en cada mano.
- [ ] **TT-28** Cambio de mesa: cerrar mesa A, abrir mesa B → `GameLoggerService.StartSessionAsync` persiste mesa A y arranca mesa B sin pérdida de datos.
- [ ] **TT-29** Modo Test (cbTest): cargar imagen estática del flop → forzar `FlopDetected` → recibir acción correcta en overlay.
- [ ] **TT-30** Cierre de la app durante mano activa: validar que `FrmMain_FormClosing` persiste `_currentHand` antes de cerrar.

---

## Tareas de Migración de Datos

> El módulo `OpenScrape.App` no introduce migraciones — los esquemas de `GameSession`/`HandRecord` ya están definidos en `OpenScrape.Domain` y el index setup vive en `OpenScrape.Infrastructure.AddDataBase`. **Sin tareas de migración.**

---

## Ordem Sugerida

1. **Bloque A → D** (estructura, helpers, entities, configuración) — paralelizable a 3 desarrolladores.
2. **Bloque E** (telemetría) — independiente, paralelizable con A-D.
3. **Bloque F** (servicios singleton) — depende de Helpers (Bloque B); paralelizable a 4 desarrolladores con conflictos mínimos.
4. **Bloque G** (servicios scoped) — depende de F (caches singleton); cuello de botella en `GameCoordinator` (T-44) que toca todo el motor postflop.
5. **Bloque H** (logging) — depende de E (telemetría) y G (`DetectionLoggerService` consume `MetricsCollector`); paralelizable con G.
6. **Bloque I** (use cases) — depende de F + G; cuello de botella en `UnifiedPokerCalculator` (T-53) que es el camino real del motor.
7. **Bloque J** (Forms) — depende de F + G + I + H; **`FrmMain` (T-69) es la tarea más larga y debería partirse en sub-tareas por pestaña** (Refactor Fase 7).
8. **Bloque K** (composition root) — depende de TODOS los anteriores; última tarea, valida el grafo completo de DI.
9. **Bloque L** (resources) — independiente, puede ir en paralelo con cualquier bloque.

**Bloqueos clave:**
- `T-69 (FrmMain)` bloquea `T-83 (registrar FrmMain como Transient)`.
- `T-44 (GameCoordinator)` bloquea `T-69 (FrmMain)`.
- `T-53 (UnifiedPokerCalculator)` bloquea `T-69 (FrmMain)`.
- `T-29 (OcrService)`, `T-30 (ImageCropperService)`, `T-32 (RegionLookupCache)`, `T-33 (CardCacheService)` bloquean `T-41 (ScreenReaderService)` y `T-42 (TableLayoutService)`.
- `T-39 (GameLoopStateMachine)` y `T-40 (PostflopContextHolder)` bloquean `T-44 (GameCoordinator)`.

---

## Lacunas Pendentes (🔴)

> Decisiones que dependen de validación humana antes de la implementación. Se duplican aquí desde `questions.md` para visibilidad.

- 🔴 **Credenciales reales en `appsettings.json` versionado.** Decidir antes de **T-19**: ¿se rotan, se purgan del histórico Git y se mueven a User Secrets?
- 🔴 **`EncrypterHelper` con IV fija.** Decidir antes de **T-09**: ¿se reescribe con IV aleatoria o se mantiene 1:1?
- 🔴 **`FormImage` con path hardcoded `C:\Code\Poker\...\Games`.** Decidir antes de **T-66**: ¿se hace configurable o se elimina la feature?
- 🔴 **Dual ubicación de `eng.traineddata`** (embebida + output). Decidir antes de **T-86**: ¿se mantiene la dualidad por safety o se elimina la copia output (auto-extracción siempre)?
- 🟡 **`PokerHandEvaluator` legacy duplicado** con `BitHandEvaluator`. Decidir antes de **T-12** si se incluye en el scope de `OpenScrape.App` o se elimina del proyecto.
- 🟡 **`PokerDecisionFacade` planeado pero NO usado.** Decidir antes de **T-47** y **T-75**: ¿se mantiene como cutover futuro o se elimina del scope?
- 🟡 **`GameLoopCoordinator` inactivo bajo feature flag.** Decidir antes de **T-46** y **T-75**: ¿se activa Fase 4 o se elimina y se mantiene `BackgroundWorker`?
- 🟡 **Filtro `"NL H"` global de `FormListApps`.** Decidir antes de **T-68**: ¿se vuelve configurable o solo soportamos NL Hold'em?
- 🟡 **`FrmMain` 4 502 LOC violando SRP.** Decidir antes de **T-69**: ¿se reimplementa monolítico o se split en 5 partial classes (una por pestaña)?
