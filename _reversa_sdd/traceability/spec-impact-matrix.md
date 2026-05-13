# Spec Impact Matrix — ScrapePoker

> Generado por el **Architect** del Reversa el 2026-05-06.
> Matriz de impacto entre componentes y capacidades.
>
> Escala de confianza: 🟢 CONFIRMADO · 🟡 INFERIDO · 🔴 LACUNA

---

## 1. Propósito

Esta matriz responde a tres preguntas clave para cualquier cambio futuro:

1. **¿Qué componentes implementan cada capacidad del sistema?** (capability ↔ component)
2. **¿Qué componentes se ven afectados si toco una entidad de dominio?** (entity ↔ component)
3. **¿Qué componentes consumen cada decisión arquitectural (ADR)?** (ADR ↔ component)

Las matrices son **direccionales**: la fila es el origen del impacto, la columna es el receptor.

🟢 Inferida cruzando `code-analysis.md`, `c4-components.md`, `domain.md`, `state-machines.md` y los ADRs.

---

## 2. Capabilities ↔ Components

🟢 Capabilities derivadas de las reglas de negocio (`domain.md` §3) y de los flujos en `c4-context.md`.

Leyenda:
- ⭐ = ownership (responsable principal)
- ◯ = consumidor / dependiente
- · = sin involucración

| Capability | FrmMain | FrmOverlay | GameCoordinator | ScreenReaderService | TableLayoutService | OcrService | UnifiedPokerCalculator | PokerDecisionFacade | PostflopDecisionService | OpponentTracker | MonteCarloSimulator | OutsCalculator | BoardTextureAnalyzer | BetSizingService | DangerPenaltyCalculator | ImpliedOddsCalculator | ThresholdsRegistry | RangePolarizer | StrategyBacktester | StrategyAnalyzerService | BankrollTrackerService | ExploitabilityCalculator | AutoCalibrationService | GameLoggerService | MetricsCollector | DetectionLoggerService | GameLoopStateMachine | PostflopContextHolder | CardCacheService | RegionLookupCache | CaptureWindowsHelper | EncrypterHelper | SetPreflopActionUseCase | GetActionScenario | StrategyProfileValidator |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| **C-01** Capturar ventana del cliente de poker | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · |
| **C-02** Detectar cuándo es turno del hero (color B≈24) | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | · | · | · | · | · |
| **C-03** Reconocer cartas hole/board (OCR + dHash) | ◯ | · | · | ⭐ | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | ◯ | · | · | · | · | · |
| **C-04** Leer bets/stacks/hand# (multi-lectura consenso) | ◯ | · | · | ⭐ | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | ◯ | · | · | · | · | · |
| **C-05** Detectar dealer button (color dorado) | ◯ | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | · |
| **C-06** Asignar posiciones (moving blinds) | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-07** Detectar cambio de mano | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | · | · | ◯ | ◯ | · | · | · | · | · | · | · |
| **C-08** Detectar transición de calle (Flop/Turn/River) | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · |
| **C-09** Calcular equity (preflop / postflop) | · | · | ◯ | · | · | · | ⭐ | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-10** Calcular outs / draws | · | · | ◯ | · | · | · | ⭐ | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-11** Analizar textura del board | · | · | ◯ | · | · | · | ⭐ | · | ◯ | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-12** Evaluar mano (hand rank + kicker + pair classification) | · | · | ◯ | · | · | · | ⭐ | · | · | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-13** Decidir acción preflop (tabla-driven) | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | ⭐ | · |
| **C-14** Decidir acción postflop (10+ paths) | ⭐ | · | ⭐ | · | · | · | · | ◯ | ⭐ | ◯ | · | · | ◯ | ⭐ | ⭐ | ⭐ | ⭐ | ⭐ | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · |
| **C-15** Tracking de oponentes (VPIP/PFR/AF/...) | · | · | ◯ | · | · | · | · | · | ◯ | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-16** Modular bet sizing | · | · | ◯ | · | · | · | ◯ | ◯ | ◯ | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-17** Detectar auto-rebuy (50 BB) | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · |
| **C-18** Mostrar overlay con recomendación | ⭐ | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-19** Persistir sesión + manos en Marten | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · |
| **C-20** Loggear `StreetDecision` por calle | ⭐ | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · |
| **C-21** Telemetría histograma (latencias por fase) | ⭐ | · | ◯ | ◯ | ◯ | ◯ | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | ⭐ | · | · | · | · | · | · | · | · | · | · |
| **C-22** Backtest A/B histórico | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-23** Métricas agregadas Historial / Bankroll | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | ⭐ | · | · | ◯ | · | · | · | · | · | · | · | · | · | · | · |
| **C-24** Análisis GTO mbb + TopLeaks | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-25** Validar StrategyProfile en arranque | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ |
| **C-26** Lookup tipado de StreetThresholds | · | · | · | · | · | · | · | · | ◯ | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| **C-27** Cifrado simétrico (AES) | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · |
| **C-28** Caching cartas (52 lazy) | · | · | · | · | · | · | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · |
| **C-29** Caching regiones por mapa (O(1)) | · | · | · | ◯ | ◯ | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · |
| **C-30** Detección estadística (loop) | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | ⭐ | · | · | · | · | · | · | · | · | · |

---

## 3. Domain Entities ↔ Components

🟢 Cuál entidad consume / produce cada componente.

| Entidad | Productores | Consumidores |
|---------|-------------|--------------|
| `GameSession` | `GameLoggerService` (StartSession, SaveSession) | `BankrollTrackerService`, `GameLoggerService.GetRecentSessionsWithStatsAsync`, `StrategyAnalyzerService`, UI `dgvSessions`, `GetRecentGameRounds` |
| `HandRecord` | `GameLoggerService.FinalizeAndPersistHandAsync` | `BankrollTrackerService`, `StrategyBacktester`, `StrategyAnalyzerService`, UI `dgvSessionHands`, `FrmHandDetail` |
| `StreetDecision` | `GameCoordinator.DetermineFlopAction/Turn/River` (vía `GameLoggerService.LogStreetDecision`) | `StrategyBacktester`, `StrategyAnalyzerService`, `ExploitabilityCalculator` |
| `Card` | `CardCacheService` (lazy load) | `GetCardsFlopUseCase/Turn/River`, `UnifiedPokerCalculator` |
| `Table` | `Extractor/ExtractorTablas` (CLI), updates manuales | `GetActionScenario`, `GetTable` |
| `RegionTableMap` | `UpdateRegionTableMap`, `FrmDetectionDebug` | `RegionLookupCache`, `ScreenReaderService`, `TableLayoutService`, `ColorDetectionService` |
| `StrategyProfile` | `appsettings.json` + `IOptions<StrategyProfile>` | `PostflopDecisionService`, `BetSizingService`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `RangePolarizer`, `ThresholdsRegistry`, `StrategyProfileValidator`, `OpponentTracker`, `BankrollTrackerService` |
| `OpponentProfile` | `OpponentTracker` (ConcurrentDictionary in-memory) | `PostflopDecisionService`, `GameCoordinator`, `PokerDecisionFacade`, `UnifiedPokerCalculator` |
| `OpponentPositionProfile` | `OpponentTracker.RecordPostflopAction` (track IP/OOP) | `OpponentProfile.GetTypeForPosition`, `PostflopDecisionService` |
| `OverlayConfig` | `appsettings.json` + `IOptions<OverlayConfig>` | `FrmOverlay`, `OverlayPositioner` |
| `PostflopGameContext` | `PostflopContextHolder.Update` desde `GameCoordinator` y `FrmMain` | `PostflopDecisionService.DetermineAction`, `GameCoordinator` |
| `BoardChangeResult` | `BoardTextureAnalyzer.AnalyzeBoardChange / AnalyzeInitialBoard` | `PostflopGameContext.CombineBoardChanges`, `PostflopDecisionService` |
| `Hand` (preflop hand definition) | `Data/*.json` deserializado por `GetActionScenario` | `GetActionScenario.GetRandomAction` |
| `VillainRange` | static (modelos predefinidos) | `MonteCarloSimulator.BuildVillainCombos`, `UnifiedPokerCalculator.CalculateEquity` |
| `StreetThresholds` | `StrategyProfile.Thresholds` Dict | `ThresholdsRegistry`, `PostflopDecisionService` |
| `ThresholdKey` | `ThresholdsRegistry` ctor (parser) | `ThresholdsRegistry.Get` |
| `TelemetryAggregate` | `MetricsCollector.EndHand` | `HandRecord.Telemetry` (persistido), UI `MetricsTab` |
| `BankrollSnapshot/Stats` | `BankrollTrackerService.GetBankrollStats` | UI `BankrollTab` |
| `DecisionRecord` / `LeakInfo` / `SessionAnalysis` | `ExploitabilityCalculator.RecordDecision/AnalyzeSession` | `AutoCalibrationService.GenerateAdjustments` |
| `CalibrationResult` | `AutoCalibrationService.GenerateAdjustments` | UI `CalibrationTab` (🔴 sin validar implementación actual) |

---

## 4. ADR ↔ Components

🟢 Qué componentes implementan cada decisión arquitectural.

| ADR | Decisión | Componentes implicados | Si se reemplaza, impacta a |
|-----|----------|------------------------|----------------------------|
| **0001** Clean Arch 5 capas | — | Estructura `*.csproj`, `Program.cs` DI registration | Toda la organización del repo |
| **0002** WinForms net10-windows | — | `OpenScrape.App.csproj`, `Program.Main()` STA, todos los Forms, P-Invoke User32+GDI32 | Migrar a WPF/MAUI/Web reescribiría toda la UI y la captura |
| **0003** Marten + Postgres | — | `OpenScrape.Infrastructure/Services.cs`, `IDocumentStore` consumers (`Features/*`, `GameLoggerService`, `BankrollTrackerService`, `CardCacheService`) | Migrar a EF Core o Mongo cambia ~30 archivos |
| **0004** Scraping pasivo (OCR + visión) | — | `CaptureWindowsHelper`, `OcrService`, `ScreenReaderService`, `ColorDetectionService`, `ImageCropperService`, `ImagePreprocessorHelper`, `RegionLookupCache`, `TableLayoutService`, `FrmMain.btnCapture_Click` | Migrar a integración API del cliente eliminaría el ~40% del código |
| **0005** Eliminar ML.NET | — | (sin componentes ML actuales) | N/A — ya implementada |
| **0006** Pipeline unificado equity/decision | — | `UnifiedPokerCalculator`, `PostflopDecisionService`, `EquityCalculatorService`, `MonteCarloSimulator`, `OutsCalculator`, `BoardTextureAnalyzer`, `PreflopEquityCalculator` | Cambiar pipeline reescribe el motor entero |
| **0007** PostflopContext inmutable + holder scoped | — | `PostflopGameContext`, `PostflopContextHolder`, `IPostflopContextHolder`, consumers (`GameCoordinator`, `PostflopDecisionService`, `PokerDecisionFacade`, `FrmMain`) | Volver a estado mutable readjustaría 4-5 servicios |
| **0008** ThresholdsRegistry tipado + startup validation | — | `ThresholdsRegistry`, `ThresholdKey`, `StrategyProfileValidator`, `StrategyProfile.Thresholds`, `PostflopDecisionService` | Volver a fallback silencioso ocultaría typos en config |
| **0009** ILogger + TextBox sink + scopes | — | `TextBoxLogger`, `TextBoxLoggerProvider`, `TextBoxLoggerOptions`, `TextBoxLoggerExtensions`, todos los `_logger` consumers (~20 servicios) | Cambiar logging cambia inyección en muchos servicios |
| **0010** Monte Carlo híbrido | — | `MonteCarloSimulator`, `BitHandEvaluator`, `HandScore`, `EquityCalculatorService`, `UnifiedPokerCalculator` | Cambiar a otra estrategia de equity tocaría todo el pipeline numérico |
| **0011** OpponentTracker reliability + Laplace | — | `OpponentTracker`, `OpponentProfile`, `OpponentPositionProfile`, `OpponentType`, `PostflopDecisionService`, `GameCoordinator` | Cambiar la heurística cambia bluff catching, fold equity y range narrowing |
| **0012** GameLoopStateMachine + board cards | — | `GameLoopStateMachine`, `FrmMain.ProcessFlop/Turn/RiverAsync`, `IsBoardCardVisible` | Cambiar la FSM cambia el game loop entero |
| **0013** Auto-rebuy 50 BB | — | `PostflopGameContext.AutoRebuyThreshold`, `TrackHeroStack`, `FrmMain.SetHeroStack`, `HandRecord.AutoRebuy`, `GameSession.NetProfit` | Cambiar threshold afecta cálculo de profit y BB/100 |
| **0014** FrmMain scoped no root | — | `Program.cs:CreateAsyncScope`, `FrmMain` constructor (44 servicios), `Features/Services.cs` (Scoped registration) | Cambiar a Singleton fragmentaría DI con use cases scoped |
| **0015** DecisionMatrix 216 casos | — | Tests `OpenScrape.App.Tests` | Eliminar tests = perder red de seguridad |
| **0016** Perf optimization | — | `LockBits` en `OcrService`/`ImageCropperService`/`PerformEnhancedDetection`, `RegionLookupCache`, `CardCacheService`, `LruCache`, `ThreadLocal` buffers en `MonteCarloSimulator` | Revertir = caída de throughput de loop |
| **0017** Telemetría + checkpoints | — | `MetricsCollector`, `Histogram`, `ScopedMeasurement`, `MetricsSnapshot`, `TelemetryCategories`, `IMetricsCollector` consumers (~6 servicios), `scripts/verify-pre-merge.ps1` | Eliminar telemetría reduce visibilidad operacional |
| **0018** Config por ambiente | — | `appsettings.json`, `appsettings.Development.json`, `Program.cs` env detection, `.gitignore` | Anomalía: `IsDevelopment=true` hardcoded — solucionar requiere tocar `Program.cs:52` |
| **0019** Posiciones moving blinds | — | `PositionCalculator`, `TableLayoutService.SetVillainPosition`, `TableLayoutService.DetermineP0Position` | Cambiar lógica afecta a todas las decisiones (todas dependen de la posición) |
| **0020** OpenSpec spec-driven | — | `openspec/`, `_reversa_sdd/` (Reversa) | Cambiar workflow no afecta código productivo |

---

## 5. State Machines ↔ Components

🟢 7 FSMs documentadas en `state-machines.md`. Resumen del impacto:

| FSM | Estados | Componente primario | Componentes que observan |
|-----|---------|---------------------|--------------------------|
| **GameLoopStateMachine** | 10 (WaitingForHand → HandComplete) | `GameLoopStateMachine` | `FrmMain`, `GameCoordinator`, `BackgroundWorker1_DoWork` |
| **PostflopGameContext lifecycle** | flop → turn → river (transición vía `WithFlopState`/`WithTurnState`) | `PostflopGameContext` (record) + `PostflopContextHolder` | `GameCoordinator`, `PostflopDecisionService`, `FrmMain` |
| **OpponentProfile reliability** | Unknown → reliable cuando hands ≥10 (AF), ≥5 (CBet), ≥8 (Fold) | `OpponentProfile.HasReliableXxxData` | `PostflopDecisionService`, `OpponentTracker.GetAdjustedFoldEquity` |
| **HandRecord lifecycle** | StartNewHand → LogStreetDecision* → EndHand → Save | `GameLoggerService` | `FrmMain.HandleNewHandAsync` |
| **GameSession lifecycle** | StartSession → … → SaveSessionAsync | `GameLoggerService` | `FrmMain` (start/stop) |
| **Decision pipeline state** | Equity → Texture → Profile → DecisionService → Sizing | `PokerDecisionFacade.EvaluateAsync` | `MetricsCollector` (5 fases medidas) |
| **MetricsCollector hand cycle** | Idle → StartHand → Measure* → EndHand → SnapshotInHandRecord | `MetricsCollector` | `GameLoggerService`, `FrmMain.MetricsTab` |

---

## 6. Anomalías ↔ Componentes (cross-impact)

🔴 Cuando se resuelva cada anomalía, qué componentes hay que tocar.

| Anomalía | Componentes a tocar | Esfuerzo estimado |
|----------|---------------------|-------------------|
| `FrmMain` god class (DT-1) | `FrmMain`, nuevos coordinators, `GameLoopCoordinator`, todos los Services consumidos | 🔴 Alto (refactor planeado pero no ejecutado) |
| `PostflopDecisionService` god service (DT-2) | `PostflopDecisionService`, nuevas sub-strategies, todos los servicios inyectados | 🔴 Alto |
| Credenciales en `appsettings.json` (DT-3) | `appsettings.json` (revertir a placeholder), rotación de password, validación de `Program.cs` | 🟡 Medio (rotación + Git history scrub) |
| `IsDevelopment=true` hardcoded (DT-4) | `Program.cs:52` | 🟢 Bajo (1 línea) |
| `BankrollTrackerService` accede a Marten (DT-5) | Mover a `Infrastructure` o introducir `IBankrollRepository` en App | 🟡 Medio |
| `Random.Shared` directo (DT-6) | `IRandomProvider` interface + 10+ ramas en `PostflopDecisionService` | 🟡 Medio (refactor + tests) |
| Doble entry point (DT-7) | Decidir cutover; eliminar `UnifiedPokerCalculator` o `PokerDecisionFacade` | 🟡 Medio |
| `HandEvaluator` legacy (DT-8) | Eliminar `HandEvaluator` (verificar consumers) | 🟢 Bajo |
| Interfaces con tipos nested (DT-9) | Mover `EquityResult`, `OutsResult`, `FullEquityAnalysis` a `DTOs/` | 🟢 Bajo |
| `BackgroundWorker` sin CancellationToken (DT-10) | `FrmMain.BackgroundWorker1_DoWork` + `OnFormClosing` | 🟢 Bajo |
| `EncrypterHelper` IV fija (DT-12) | `EncrypterHelper` + consumers (🔴 sin identificar — `Encrypter.Key` en `appsettings.json` no tiene consumer visible) | 🟡 Medio |
| 3 use cases vacíos (DT-13) | Eliminar `GetAllTables`, `GetAllRegionTableMap`, `GetFlopCards` + `Services.cs` | 🟢 Bajo |
| `eng.traineddata` duplicado (DT-15) | Eliminar `Resources/tessdata/` o `tessdata/` | 🟢 Bajo |
| Carpetas vestigio (DT-16) | Borrar `src/OpenScrape.Application/` y `src/OpenScrape.Core/` | 🟢 Bajo |
| MainPage.xaml legacy (DT-17) | Borrar `MainPage.xaml` + `MainPage.xaml.cs` | 🟢 Bajo |
| `GetActionScenario` envuelve excepción base (DT-18) | `GetActionScenario` (relanzar con `throw;` o `InnerException`) | 🟢 Bajo |
| `UpdateRegionTableMap` ignora flags (DT-19) | `UpdateRegionTableMap` lógica de creación inicial | 🟢 Bajo |
| N+1 queries (DT-20, DT-21) | `BankrollTrackerService.GetBankrollStats` + `GameLoggerService.GetRecentSessionsWithStatsAsync` con `Include` o batch query | 🟡 Medio |
| `ExploitabilityCalculator.BigBlind=1.0` (DT-24) | Inyectar BB de la sesión actual; recalcular fórmula | 🟢 Bajo |
| `OcrService.GetCroppedBitmap` cache stale (DT-25) | Cambiar key a `dHash(image)` o invalidar al setear nuevo Bitmap | 🟢 Bajo |
| Path absoluto hardcoded (DT-26) | `FrmMain.GetImageWhilePlaying` + `FormImage.btnLoad_Click` | 🟢 Bajo |
| Sin CI pipeline (DT-28) | GitHub Actions o equivalente con `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes` | 🟡 Medio |

---

## 7. OpenSpec changes ↔ Components afectados

🟢 Specs en `openspec/changes/` (consultadas vía `Glob`).

| Spec | Estado | Componentes nuevos / afectados |
|------|--------|--------------------------------|
| `login-sistema-licencias` | abierta | Nuevos: `LicenseService`, `HardwareFingerprint`, `LoginForm`, tabla DB de licencias. Afectados: `Program.Main()` (validar licencia al arranque), `appsettings.json`, `EncrypterHelper` (uso real) |
| `bankroll-dashboard` | abierta | Afectados: `FrmMain` (nueva tab), `BankrollTrackerService` (ya existente — verificar API), `GameSession`, `HandRecord`. Nuevos: gráficas (?) |
| `decision-engine-v3` | abierta | Afectados: `PostflopDecisionService`, `UnifiedPokerCalculator`, `StrategyProfile` (nuevos parámetros). Posibles nuevos: sub-strategies refactor (resolvería DT-2) |
| 9 specs archivadas en `openspec/archived/` | completadas | Ver `openspec/archived/` para detalle |

---

## 8. Lectura cruzada con otros artefactos

Esta matriz se complementa con:

- `_reversa_sdd/architecture.md` §6 — listado de dívidas técnicas con severidad.
- `_reversa_sdd/code-analysis.md` — anomalías por módulo.
- `_reversa_sdd/state-machines.md` — FSMs detalladas con preguntas abiertas.
- `_reversa_sdd/permissions.md` — RBAC (no implementado, spec abierta).
- `_reversa_sdd/questions.md` — 24 preguntas para validación humana.

---

## 9. Reglas de uso de la matriz

🟢 **Para añadir una nueva capacidad:**
1. Identifica qué componentes existentes pueden ownearla (⭐ poco usados son candidatos).
2. Si introduces componentes nuevos, añade columna y revisa qué entidades de la sección §3 los pueden producir/consumir.
3. Si la decisión cambia un ADR, marca el componente como afectado (sección §4) y abre un nuevo ADR.

🟢 **Para tocar un componente:**
1. Mira su columna en §2 — qué capabilities owns o consume.
2. Mira sus filas en §3 — qué entidades produce/consume.
3. Mira los ADRs aplicables (§4) — entiende por qué la decisión es como es antes de cambiarla.
4. Mira las anomalías (§6) — quizás sea oportunidad de pagar deuda técnica al mismo tiempo.

🟡 **Para eliminar dead code:**
- Verifica que ninguna columna de §2 lo marca como ⭐ ni como ◯.
- Verifica que ninguna fila de §3 depende de él.
- En este repo: `GetAllTables`, `GetAllRegionTableMap`, `GetFlopCards`, `HandEvaluator` (legacy), `MainPage.xaml*`, `OpenScrape.Application/`, `OpenScrape.Core/`, `Resources/tessdata/eng.traineddata` (duplicado).

---

## 10. Referencias

- `_reversa_sdd/c4-context.md`
- `_reversa_sdd/c4-containers.md`
- `_reversa_sdd/c4-components.md`
- `_reversa_sdd/architecture.md`
- `_reversa_sdd/code-analysis.md`
- `_reversa_sdd/domain.md`
- `_reversa_sdd/state-machines.md`
- `_reversa_sdd/adrs/` (20 ADRs)
- `openspec/changes/` (specs abiertas)
