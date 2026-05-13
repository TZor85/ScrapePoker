# Legacy Mapping — `OpenScrape.DecisionMaker`

> Mapeo de archivos del módulo legado a las specs generadas.
> Granularidad: módulo (per `[specs] granularity = "module"` en `.reversa/config.toml`).
> Generado por el Arqueólogo del Reversa.

## Estructura de archivos del legado

### `Algorithms/` (10 archivos, 6 implementaciones + 4 interfaces + 1 struct)

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.DecisionMaker/Algorithms/BitHandEvaluator.cs` | clase | 515 | Evaluador de manos por bit-manipulation con `stackalloc Span<int>`. Reemplaza brute-force `C(n,5)` del legacy `HandEvaluator`. Implementa `IHandEvaluator`. |
| `src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs` | clase | 654 | Equity híbrido: enumeración exacta turn/river + MC paralelizado flop/preflop. `ThreadLocal` deck/buffers para zero-alloc. Implementa `IMonteCarloSimulator`. |
| `src/OpenScrape.DecisionMaker/Algorithms/HandEvaluator.cs` | clase **legacy** | 206 | Brute-force `C(n,5)` original. **Anomalía:** convive con `BitHandEvaluator` sin justificación. `EvaluateHandScore` delega a `new BitHandEvaluator()` por llamada. |
| `src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs` | clase | 462 | Cuenta outs con inclusión-exclusión + backdoor + tainted discount. Implementa `IOutsCalculator`. |
| `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs` | clase + 4 records/enums | 412 | Análisis de textura, board change, river card classification. Define `BoardTextureCategory`, `RiverCardType`, records `BoardTextureResult`, `BoardChangeResult`. Implementa `IBoardTextureAnalyzer`. |
| `src/OpenScrape.DecisionMaker/Algorithms/PreflopEquityCalculator.cs` | clase | 232 | Tabla estática de 169 manos heads-up + ajuste por opponents `equity^(1+log2(n)×0.35)`. **No implementa interfaz** — consumido directamente por `EquityCalculatorService`. |
| `src/OpenScrape.DecisionMaker/Algorithms/IBoardTextureAnalyzer.cs` | interface | 18 | Contrato textura |
| `src/OpenScrape.DecisionMaker/Algorithms/IMonteCarloSimulator.cs` | interface | 16 | Contrato MC. **Anomalía:** referencia tipo nested `MonteCarloSimulator.EquityResult` |
| `src/OpenScrape.DecisionMaker/Algorithms/IOutsCalculator.cs` | interface | 12 | Contrato outs. **Anomalía:** referencia tipo nested `OutsCalculator.OutsResult` |
| `src/OpenScrape.DecisionMaker/Algorithms/IHandEvaluator.cs` | interface + struct | 58 | Contrato evaluator + `HandScore` readonly struct con `BuildComposite` |

### `Services/` (16 archivos)

| Archivo | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` | clase + record + enum | **1893** | Motor de decisión postflop. Único punto de entrada `DetermineAction`. Define `BetSizeCategory` enum y `PostflopDecisionResult` record. **Anomalía crítica:** SRP violado (10+ paths, mixing aleatorio con `Random.Shared` directo, pot commitment duplicado, `goto` explícito). |
| `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs` | sealed record | 171 | Estado cross-street inmutable. Helpers `WithFlopState`, `WithTurnState`, `TrackHeroStack` (auto-rebuy detection), `CombineBoardChanges`. |
| `src/OpenScrape.DecisionMaker/Services/PreflopAnalyzer.cs` | clase static + interface impl | 162 | `IsPreflopAggressor`, `HasRangeAdvantageOnBoard`, `CategorizeOpponentBet`, `DetectDonkBet`, `CalculateCbetAdjustment`. **Anomalía:** estática + interface impl trivial. |
| `src/OpenScrape.DecisionMaker/Services/BetSizingService.cs` | clase + record + enum | 222 | `CalculateDynamicBetSize` modula por SPR/multiway/textura/posición/street. Define `BetSizingType` enum y `BetSizingOption` record. |
| `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs` | clase + interface impl | 111 | `Calculate` retorna penalty proporcional escalado por street, blocker reduction granular. |
| `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs` | clase + interface impl | 168 | `CalculateImpliedOddsFactor` interpolación cuadrática SPR. `CalculateReverseImpliedOdds` con multiplier por PairClassification + S20.3 bluff risk. |
| `src/OpenScrape.DecisionMaker/Services/RangePolarizer.cs` | clase + enum | 85 | `RangeType` (Linear/Polarized/Condensed). `GetOptimalRangeType` por textura/IP/SPR/street. |
| `src/OpenScrape.DecisionMaker/Services/ThresholdsRegistry.cs` | sealed clase | 59 | Lookup tipado O(1) `Dictionary<ThresholdKey, StreetThresholds>` con fail-fast en arranque. |
| `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs` | clase + enum | 274 | `ConcurrentDictionary` thread-safe. 17 contadores por jugador, IP/OOP separation, seat-alias mapping. Define `PostflopAction` enum. |
| `src/OpenScrape.DecisionMaker/Services/EquityCalculatorService.cs` | clase + nested clase | 173 | Orquestador: PreflopEquityCalculator (community vacía) o MC + Outs (postflop). Genera `RecommendedAction` heurística con thresholds **hardcoded** (anomalía). Define nested `FullEquityAnalysis`. |
| `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs` | clase + 2 classes | 243 | Replaya decisiones históricas vs motor actual. Define `BacktestResult` y `DecisionDivergence`. |
| `src/OpenScrape.DecisionMaker/Services/StrategyAnalyzerService.cs` | clase + 6 satellites | 397 | Métricas agregadas por posición/street/situación + EquityAccuracy por buckets. Define `StrategyAnalysisResult`, `PositionStats`, `StreetStats`, `SituationStats`, `SessionSummary`, `EquityVsOutcome`. |
| `src/OpenScrape.DecisionMaker/Services/BankrollTrackerService.cs` | clase | 292 | Risk-of-ruin clásico. Consume `Marten.IDocumentStore`. **Anomalías:** N+1 queries, `using` síncrono, exceptions silenciadas. |
| `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs` | clase + 6 classes + 1 enum | 487 | Telemetría GTO. `ConcurrentQueue` FIFO de hasta 10K. Define `LeakCategory` enum y 6 DTOs. **Anomalía:** `BigBlind=1.0` hardcoded. |
| `src/OpenScrape.DecisionMaker/Services/AutoCalibrationService.cs` | clase + 3 classes | 237 | Auto-calibración basada en TopLeaks. **Anomalía crítica:** OldValue hardcoded 45/40 — bug latente. Define `CalibrationResult`, `ParameterAdjustment`, `CalibrationPreview`. |

### `Interfaces/` (13 archivos)

| Archivo | LOC | Para |
|---------|-----|------|
| `src/OpenScrape.DecisionMaker/Interfaces/IPostflopDecisionService.cs` | 35 | `PostflopDecisionService` |
| `src/OpenScrape.DecisionMaker/Interfaces/IPreflopAnalyzer.cs` | 29 | `PreflopAnalyzer` |
| `src/OpenScrape.DecisionMaker/Interfaces/IBetSizingService.cs` | 27 | `BetSizingService` |
| `src/OpenScrape.DecisionMaker/Interfaces/IDangerPenaltyCalculator.cs` | 20 | `DangerPenaltyCalculator` |
| `src/OpenScrape.DecisionMaker/Interfaces/IImpliedOddsCalculator.cs` | 24 | `ImpliedOddsCalculator` |
| `src/OpenScrape.DecisionMaker/Interfaces/IRangePolarizer.cs` | 26 | `RangePolarizer` |
| `src/OpenScrape.DecisionMaker/Interfaces/IThresholdsRegistry.cs` | 32 | `ThresholdsRegistry` |
| `src/OpenScrape.DecisionMaker/Interfaces/IOpponentTracker.cs` | 31 | `OpponentTracker` |
| `src/OpenScrape.DecisionMaker/Interfaces/IEquityCalculatorService.cs` | 18 | `EquityCalculatorService`. **Anomalía:** referencia tipo nested |
| `src/OpenScrape.DecisionMaker/Interfaces/IStrategyAnalyzerService.cs` | 14 | `StrategyAnalyzerService` |
| `src/OpenScrape.DecisionMaker/Interfaces/IStrategyBacktester.cs` | 12 | `StrategyBacktester` |
| `src/OpenScrape.DecisionMaker/Interfaces/IBankrollTrackerService.cs` | 19 | `BankrollTrackerService` |
| `src/OpenScrape.DecisionMaker/Interfaces/IExploitabilityCalculator.cs` | 23 | `ExploitabilityCalculator` |
| `src/OpenScrape.DecisionMaker/Interfaces/IAutoCalibrationService.cs` | 19 | `AutoCalibrationService` |

### `DTOs/` (3 archivos)

| Archivo | LOC | Tipo |
|---------|-----|------|
| `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs` | 66 | record con 6 required + 36 con defaults — input de `DetermineAction` |
| `src/OpenScrape.DecisionMaker/DTOs/DecisionRequest.cs` | 72 | sealed record — DTO del facade `IPokerCalculator` (ubicado en `OpenScrape.App`) |
| `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs` | 47 | sealed record — output del facade |

### Raíz del módulo

| Archivo | LOC | Propósito |
|---------|-----|-----------|
| `src/OpenScrape.DecisionMaker/PokerConstants.cs` | 111 | ~25 constantes algorítmicas (no configurables por estrategia) |
| `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` | 23 | Target net10.0, InternalsVisibleTo `OpenScrape.App.Tests`, refs Marten 8.24 + Microsoft.Extensions.{Logging.Abstractions, Options} 10.0.3 |

### Carpeta `obj/` (artefactos build versionados — **anomalía low**)

```
src/OpenScrape.DecisionMaker/obj/Debug/net8.0/         ⚠️ legacy target
src/OpenScrape.DecisionMaker/obj/Debug/net9.0/         ⚠️ legacy target
src/OpenScrape.DecisionMaker/obj/Debug/net10.0/        ✅ current
src/OpenScrape.DecisionMaker/obj/Release/net9.0/       ⚠️ legacy target
src/OpenScrape.DecisionMaker/obj/Release/net10.0/      ✅ current
```

El csproj solo declara `<TargetFramework>net10.0</TargetFramework>`. Los obj de net8.0/net9.0 son restos de migraciones de target framework no limpiados.

## Mapeo a las specs

| Archivo legacy | Sección de spec |
|----------------|-----------------|
| `Algorithms/MonteCarloSimulator.cs` | `code-analysis.md § OpenScrape.DecisionMaker / 1. Flujo / MonteCarloSimulator.CalculateEquity` + `flowcharts/OpenScrape.DecisionMaker-MonteCarloSimulator.md` |
| `Algorithms/BitHandEvaluator.cs` | `code-analysis.md § / 1. Flujo / BitHandEvaluator.EvaluateBestHand` + flowchart embebido en MC |
| `Algorithms/HandEvaluator.cs` | `code-analysis.md § / 5. Anomalías / coexistencia legacy + bit` |
| `Algorithms/OutsCalculator.cs` | `code-analysis.md § / 1. Flujo / OutsCalculator.CalculateOuts` + `flowcharts/OpenScrape.DecisionMaker-OutsCalculator.md` |
| `Algorithms/BoardTextureAnalyzer.cs` | `code-analysis.md § / 1. Flujo / BoardTextureAnalyzer.Analyze + AnalyzeBoardChange` |
| `Algorithms/PreflopEquityCalculator.cs` | `code-analysis.md § / 1. Flujo / Servicios complementarios / PreflopEquityCalculator` |
| `Services/PostflopDecisionService.cs` | `code-analysis.md § / 1. Flujo / PostflopDecisionService.DetermineAction` + `flowcharts/OpenScrape.DecisionMaker-DetermineAction.md` |
| `Services/PostflopGameContext.cs` | `code-analysis.md § / 1. Flujo / PostflopGameContext` + `data-dictionary.md § Estado cross-street` |
| `Services/PreflopAnalyzer.cs` | `code-analysis.md § / 1. Flujo / Servicios complementarios / PreflopAnalyzer` |
| `Services/BetSizingService.cs` | `code-analysis.md § / Servicios complementarios / BetSizingService.CalculateDynamicBetSize` |
| `Services/DangerPenaltyCalculator.cs` | `code-analysis.md § / Servicios complementarios / DangerPenaltyCalculator.Calculate` + flowchart embebido en effectiveEquity |
| `Services/ImpliedOddsCalculator.cs` | `code-analysis.md § / Servicios complementarios / ImpliedOddsCalculator.CalculateImpliedOddsFactor + ReverseImpliedOdds` |
| `Services/RangePolarizer.cs` | `code-analysis.md § / Servicios complementarios / RangePolarizer` |
| `Services/ThresholdsRegistry.cs` | `code-analysis.md § / Servicios complementarios / ThresholdsRegistry` |
| `Services/OpponentTracker.cs` | `code-analysis.md § / 1. Flujo / OpponentTracker` |
| `Services/EquityCalculatorService.cs` | `code-analysis.md § / Servicios complementarios / EquityCalculatorService.CalculateFullEquity` |
| `Services/StrategyBacktester.cs` | `code-analysis.md § / Servicios complementarios / StrategyBacktester.RunBacktest` |
| `Services/StrategyAnalyzerService.cs` | `code-analysis.md § / Servicios complementarios / StrategyAnalyzerService` |
| `Services/BankrollTrackerService.cs` | `code-analysis.md § / Servicios complementarios / BankrollTrackerService` |
| `Services/ExploitabilityCalculator.cs` | `code-analysis.md § / Servicios complementarios / ExploitabilityCalculator` |
| `Services/AutoCalibrationService.cs` | `code-analysis.md § / Servicios complementarios / AutoCalibrationService` |
| `Interfaces/*` (13) | Sección "13 interfaces de servicios" en estructura |
| `DTOs/*.cs` (3) | `data-dictionary.md § DTOs de decisión` |
| `PokerConstants.cs` | `data-dictionary.md § Constantes algorítmicas (PokerConstants)` |

## Líneas críticas referenciadas

| Concepto | Ubicación |
|----------|-----------|
| Pipeline de equity efectiva | `PostflopDecisionService.cs:140-181` |
| Multi-way penalty IP lineal / OOP cuadrático | `PostflopDecisionService.cs:230-284` |
| Range narrowing por línea agresiva | `PostflopDecisionService.cs:344-356` |
| C-bet path con frequency mixing | `PostflopDecisionService.cs:458-481` |
| C-bet mixing protección de range | `PostflopDecisionService.cs:523-544` |
| `HandleFacingBet` 3-bet pot defense (S19.3) | `PostflopDecisionService.cs:589-614` |
| `HandleFacingBet` Donk bet exploitation (S18.1) | `PostflopDecisionService.cs:616-643` |
| `dangerousFlushBoard` definition | `PostflopDecisionService.cs:648-651` |
| Pot commitment block (DUPLICADO 1) | `PostflopDecisionService.cs:772-795` |
| `HandleNoBet` 3-bet pot OOP path | `PostflopDecisionService.cs:835-880` |
| Turn-river plan flush danger | `PostflopDecisionService.cs:882-888` |
| Check-raise OOP/IP con SPR guard (S19.1) | `PostflopDecisionService.cs:927-999` |
| Float exit con bad runout abort | `PostflopDecisionService.cs:1017-1045` |
| Stackoff planning (S22.4) | `PostflopDecisionService.cs:1172-1196` |
| Randomización adaptativa por villainType (S21.5) | `PostflopDecisionService.cs:1205-1236` |
| Double barrel con runout check | `PostflopDecisionService.cs:1283-1306` |
| `CalculateAllinEV` (S8.1) | `PostflopDecisionService.cs:1427-1434` |
| `CalculateProjectedRiverSPR` (S22.4) | `PostflopDecisionService.cs:1441-1449` |
| Bloque `if` vacío (anomalía) | `PostflopDecisionService.cs:1198-1203` |
| `goto skipBluffCatch` (anomalía) | `PostflopDecisionService.cs:1602,1663` |
| Pot commitment block (DUPLICADO 2) | `PostflopDecisionService.cs:1665-1688` |
| Bluff catch turn/river con runout multipliers | `PostflopDecisionService.cs:1591-1662` |
| MC ExactEnumerationRiver | `MonteCarloSimulator.cs:142-237` |
| MC ExactEnumerationTurn | `MonteCarloSimulator.cs:244-362` |
| MC RunMonteCarloSimulation Parallel.For | `MonteCarloSimulator.cs:369-424` |
| MC TryDrawFromRange con 20 retries + skip | `MonteCarloSimulator.cs:582-618` |
| MC GetAdaptiveIterations (preflop 30K, flop 50K) | `MonteCarloSimulator.cs:127-135` |
| BitHandEvaluator FindStraightHigh + WheelMask | `BitHandEvaluator.cs:24,415-429` |
| BitHandEvaluator EvaluateHandScore zero-alloc | `BitHandEvaluator.cs:253-408` |
| OutsCalculator inclusión-exclusión + backdoor | `OutsCalculator.cs:46-202` |
| OutsCalculator S21.4 backdoor overlap discount | `OutsCalculator.cs:142-149` |
| OutsCalculator CalculateTaintedOuts (3 categorías) | `OutsCalculator.cs:286-336` |
| BoardTextureAnalyzer wetness scoring | `BoardTextureAnalyzer.cs:308-343` |
| BoardTextureAnalyzer AnalyzeInitialBoard | `BoardTextureAnalyzer.cs:349-387` |
| BoardTextureAnalyzer ClassifyRiverCard (S22.2) | `BoardTextureAnalyzer.cs:227-249` |
| DangerPenaltyCalculator pipeline | `DangerPenaltyCalculator.cs:31-110` |
| ImpliedOddsCalculator interpolación cuadrática | `ImpliedOddsCalculator.cs:55-64` |
| ImpliedOddsCalculator S20.3 bluff risk | `ImpliedOddsCalculator.cs:142-164` |
| OpponentTracker thread-safe ConcurrentDictionary | `OpponentTracker.cs:16` |
| OpponentTracker RegisterSeatAlias | `OpponentTracker.cs:234-248` |
| OpponentTracker GetAdjustedFoldEquity por tipo | `OpponentTracker.cs:191-208` |
| OpponentTracker GetFoldToBetPct con sentinel -1 | `OpponentTracker.cs:214-221` |
| ThresholdsRegistry fail-fast en arranque | `ThresholdsRegistry.cs:23-32` |
| BankrollTrackerService Risk-of-ruin clásico | `BankrollTrackerService.cs:177-213` |
| AutoCalibrationService OldValue hardcoded (BUG) | `AutoCalibrationService.cs:174-208` |
| ExploitabilityCalculator BigBlind=1.0 (anomalía) | `ExploitabilityCalculator.cs:93,322-348` |
| `Random.Shared.NextDouble()` directos en producción | `PostflopDecisionService.cs:473,539,597,608,634,842,861,871,968,989,1231` |
| PostflopGameContext.TrackHeroStack auto-rebuy | `PostflopGameContext.cs:94,133-147` |
| PostflopGameContext.WithFlopState/WithTurnState | `PostflopGameContext.cs:106-124` |
