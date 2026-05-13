# OpenScrape.DecisionMaker — Tareas de Implementación

> Secuencia de tareas para reimplementar el motor de decisión a partir del legado, con rastreabilidad línea a línea. **Módulo grande** (~6,800 LOC, 38 archivos `.cs`): 6 algoritmos + 15 servicios + 13 interfaces + 3 DTOs + 1 archivo de constantes. La columna vertebral es `PostflopDecisionService` (1893 LOC con anomalías documentadas), `MonteCarloSimulator` (654 LOC) y `OpponentTracker` (274 LOC). Las tareas se agrupan por bloque funcional para permitir paralelización por desarrolladores diferentes.

---

## Pré-requisitos

- [ ] .NET 10 SDK instalado.
- [ ] `OpenScrape.Domain` compilable (provee `Card`, `CardDataOuts`, `Hand`, `HandRank`, `KickerStrength`, `BoardPosition`, `HandSituation`, `TablePosition`, `StrategyProfile`, `StreetThresholds`, `ThresholdKey`, `OpponentProfile`, `VillainRange`, `StreetDecision`, `BankrollSnapshot`, `GameSession`, `HandRecord`, enum `JugadasEnum`).
- [ ] PostgreSQL local accesible para los tests de `BankrollTrackerService`, `StrategyBacktester` y `StrategyAnalyzerService` (consumen `IDocumentStore`).
- [ ] BenchmarkDotNet 0.15.2 disponible para benchmarks de MC + Outs + BitHandEvaluator.
- [ ] Decisiones humanas pendientes (ver `questions.md`):
  - [ ] ¿`HandEvaluator` legacy se elimina o queda en módulo aparte? 🟡
  - [ ] ¿`PreflopEquityCalculator` recibe interfaz `IPreflopEquityCalculator`? 🟡
  - [ ] ¿`PreflopAnalyzer` se queda estático o se elimina la interfaz trivial? 🟡
  - [ ] ¿Los tipos `nested` en interfaces (`EquityResult`, `OutsResult`, `FullEquityAnalysis`) se mueven a `DTOs/`? 🟡
  - [ ] ¿`PostflopDecisionService` se refactoriza por extracción de paths a clases dedicadas? 🟡
  - [ ] ¿`AutoCalibrationService.PreviewAndApply` lee del `StrategyProfile` actual (fix bug)? 🔴
  - [ ] ¿`ExploitabilityCalculator` parametriza `BigBlind`? 🔴
  - [ ] ¿`OpponentTracker` se persiste cross-sesión (Q-FSM-02)? 🔴
  - [ ] ¿`PostflopDecisionService` valida input (equity ∈ [0,100]) con guardia explícita? 🔴

---

## Tareas

> Cada tarea referencia el archivo legado de origen y su número de línea cuando aplica.

### Bloque A — Estructura del proyecto

- [ ] **T-01** Crear `OpenScrape.DecisionMaker.csproj` con `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` e `InternalsVisibleTo("OpenScrape.App.Tests")`.
  - Origen en el legado: `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj:1-23`
  - Crit. de pronto: `dotnet build` pasa sin warnings.
  - Confianza: 🟢

- [ ] **T-02** Declarar dependencias NuGet: `Marten 8.24.0`, `Microsoft.Extensions.Logging.Abstractions 10.0.3`, `Microsoft.Extensions.Options 10.0.3`. La `<ProjectReference>` única apunta a `OpenScrape.Domain`.
  - Origen en el legado: `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj:7-21`
  - Crit. de pronto: `dotnet list reference` muestra solo `OpenScrape.Domain`. `dotnet restore` exitoso.
  - Confianza: 🟢

- [ ] **T-03** No declarar `<ProjectReference>` a `OpenScrape.App`, `OpenScrape.Features` ni `OpenScrape.Infrastructure`. Garantía de inversión de dependencias.
  - Origen en el legado: invariante arquitectural (`code-analysis.md § OpenScrape.DecisionMaker`)
  - Crit. de pronto: `dotnet list reference` no incluye App/Features/Infrastructure.
  - Confianza: 🟢

- [ ] **T-04** Definir constantes algorítmicas en `PokerConstants.cs` con ~25 valores (`MaxOutsPossible=15`, `RuleOf2Multiplier=2.0`, `BackdoorFlushImpliedOuts=1.5`, `BackdoorStraightImpliedOuts=1.0`, `BackdoorOverlapDiscount=0.5`, `OvercardOutsBase=3`, `OvercardOutsConnectedBoard=2`, `OvercardOutsPairedBoard=1`, `OvercardOutsBlockerBoost=1.5`, `WetnessDryMax=15`, `WetnessSemiDryMax=35`, `WetnessSemiWetMax=60`, `BlockedComboUnreliableThreshold=20.0`, `DangerCompletedDrawNoBetCap=45`, `TaintedOutsDiscountHeroStrong=0.7`, `TaintedOutsDiscountHeroWeak=0.3`, `MCFlopIterations=50_000`, `MCPreflopIterations=30_000`, `MCDrawRetries=20`, etc.).
  - Origen en el legado: `src/OpenScrape.DecisionMaker/PokerConstants.cs:1-111`
  - Crit. de pronto: las ~25 constantes existen como `public const` o `public static readonly`. Test simple: leer cada constante y verificar el valor esperado.
  - Confianza: 🟢

### Bloque B — Algoritmos numéricos

#### Algoritmo 1: `BitHandEvaluator`

- [ ] **T-05** Definir `readonly struct HandScore` en `Algorithms/IHandEvaluator.cs` con campos `(int Rank, int K1, K2, K3, K4, K5)` y propiedad calculada `long CompositeScore = (long)Rank<<20 | K1<<16 | K2<<12 | K3<<8 | K4<<4 | K5`. Implementar `IComparable<HandScore>` delegando a `CompositeScore.CompareTo`.
  - Origen en el legado: `src/OpenScrape.DecisionMaker/Algorithms/IHandEvaluator.cs:1-58`
  - Crit. de pronto: comparación `score1 > score2` retorna correctamente; struct ocupa 16 bytes; sin allocations en heap.
  - Confianza: 🟢

- [ ] **T-06** Implementar `BitHandEvaluator.EvaluateHandScore(List<CardDataOuts> cards) → HandScore` con `Span<int> rankCount = stackalloc int[15]`, `Span<int> suitCount = stackalloc int[5]`, `int rankBits`, `Span<int> suitRankBits = stackalloc int[5]`. Fases: (1) escaneo único, (2) flush detection, (3) straight flush o flush, (4) grupos (quads/trips/pairs), (5) straight, (6) clasificación jerárquica.
  - Origen en el legado: `Algorithms/BitHandEvaluator.cs:253-408`
  - Crit. de pronto: benchmark < 1 µs y 0 allocations. Test: 7 cartas con un set de 4 → retorna `HandRank.FourOfAKind` con kicker correcto.
  - Confianza: 🟢

- [ ] **T-07** Implementar `BitHandEvaluator.FindStraightHigh(int rankBits)` iterando `high = 14 → 6` con `(rankBits & (0x1F << (high-4))) == mask`. Manejar **wheel** (A-2-3-4-5) con máscara especial `WheelMask = (1<<14) | (1<<2) | (1<<3) | (1<<4) | (1<<5)`, retornar `5` (no 14).
  - Origen en el legado: `Algorithms/BitHandEvaluator.cs:24, 415-429`
  - Crit. de pronto: test wheel `Ah 2c 3d 4h 5s` → `Straight` con high=5; test broadway `T-J-Q-K-A` → `Straight` con high=14.
  - Confianza: 🟢

- [ ] **T-08** Implementar `BitHandEvaluator.EvaluateBestHand(List<CardDataOuts>)` que retorna la mejor combinación de 5 cartas como `(HandRank, kickers, kickerStrength)`.
  - Origen en el legado: `Algorithms/BitHandEvaluator.cs:26-251`
  - Crit. de pronto: 15 tests del corpus existente (`OpenScrape.App.Tests`) pasan.
  - Confianza: 🟢

#### Algoritmo 2: `MonteCarloSimulator`

- [ ] **T-09** Definir `IMonteCarloSimulator.CalculateEquity(...) → EquityResult` con parámetros `(List<CardDataOuts> heroHand, List<CardDataOuts> villainHand, List<CardDataOuts> board, VillainRange villainRange, List<CardDataOuts> deadCards, int? customIterations = null)`. `EquityResult` contiene `Equity`, `SkippedSimulations`, `TotalSimulations`, `IsReliable`, `BlockedComboPercentage`. 🟡 Si las anomalías "interfaces con tipos nested" se aceptan, dejar `EquityResult` como nested type; si se decide moverlo, ubicarlo en `DTOs/`.
  - Origen en el legado: `Algorithms/IMonteCarloSimulator.cs:1-16`, `Algorithms/MonteCarloSimulator.cs:50-125`
  - Crit. de pronto: la interfaz compila; consumidores en App pueden inyectar mock.
  - Confianza: 🟡

- [ ] **T-10** Implementar `MonteCarloSimulator.GetAdaptiveIterations(int communityCount) → int` con: `5 → exact`, `4 → exact`, `3 → 50_000`, `0 → 30_000`. Sobrescribible por `customIterations`.
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs:127-135`
  - Crit. de pronto: test parametrizado con los 4 valores.
  - Confianza: 🟢

- [ ] **T-11** Implementar `MonteCarloSimulator.BuildVillainCombos(VillainRange, List<CardDataOuts> deadCards, List<CardDataOuts> board)` que pre-expande el rango a `List<(CardDataOuts, CardDataOuts, double weight)>`, descarta combos con cartas en `deadCards` o `board`, y calcula `precomputedTotalWeight = combos.Sum(weight)`.
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs` (función `BuildVillainCombos`)
  - Crit. de pronto: test: rango de pares con `As` en deadCards → `AA` se reduce a `1` combo (no `6`).
  - Confianza: 🟢

- [ ] **T-12** Implementar `MonteCarloSimulator.ExactEnumerationRiver(...)` que itera C(45,2)=990 manos del villano, evalúa con `BitHandEvaluator.EvaluateHandScore` y cuenta wins/ties/losses. Retorna `EquityResult { Equity, SkippedSimulations=0, IsReliable=true }`.
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs:142-237`
  - Crit. de pronto: dado `(AhAd vs random, Qh Jh 7h 2c 3d)`, retorna equity exacta determinística (idéntica entre llamadas).
  - Confianza: 🟢

- [ ] **T-13** Implementar `MonteCarloSimulator.ExactEnumerationTurn(...)` que itera 45 rivers × C(44,2) hands ≈ 42 K evaluaciones. Acumula equity ponderada.
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs:244-362`
  - Crit. de pronto: test: equity en turn coincide ±0.01 % entre dos llamadas idénticas (determinístico).
  - Confianza: 🟢

- [ ] **T-14** Implementar `MonteCarloSimulator.RunMonteCarloSimulation(int iterations)` con `Parallel.For` + `LocalInit/LocalFinally`, `ThreadLocal<CardDataOuts[]>` deck por thread, `ThreadLocal<List<CardDataOuts>>` para hand/opponent buffers, `Interlocked.Add` para acumular wins/ties/losses sin lock.
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs:369-424`
  - Crit. de pronto: stress test con 100 ejecuciones paralelas no produce race conditions; equity converge a ±0.5 % del valor esperado.
  - Confianza: 🟢

- [ ] **T-15** Implementar `MonteCarloSimulator.TryDrawFromRange(...)` con **20 retries** antes de devolver "no draw posible". Si retornar false: incrementar `SkippedSimulations` y saltar la iteración (NO contaminar equity con random fallback).
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs:582-618` (reescritura post-bugfix)
  - Crit. de pronto: test con rango bloqueado al 100 % → todas las iteraciones se saltan, `Equity = 0` (no NaN), `IsReliable = false`.
  - Confianza: 🟢

- [ ] **T-16** Marcar `IsReliable = false` si `BlockedComboPercentage > BlockedComboUnreliableThreshold (20.0)`.
  - Origen en el legado: `Algorithms/MonteCarloSimulator.cs`, `PokerConstants.cs`
  - Crit. de pronto: test: rango bloqueado al 25 % → `IsReliable = false`.
  - Confianza: 🟢

#### Algoritmo 3: `OutsCalculator`

- [ ] **T-17** Implementar `OutsCalculator.CalculateOuts(...)` con inclusión-exclusión: `flushOuts + straightOuts − overlapOuts + overcardOuts(textura, blocker) + backdoorOuts`. Retorna `OutsResult { CleanOuts, TaintedOuts, EffectiveOuts, FlushDraw, StraightDraw, ComboDraw, BackdoorFlush, BackdoorStraight }`.
  - Origen en el legado: `Algorithms/OutsCalculator.cs:46-202`
  - Crit. de pronto: test: hero `Ah Kh`, board `Qh 7h 2c` → `EffectiveOuts ≈ 12` (no 13, por overlap).
  - Confianza: 🟢

- [ ] **T-18** Implementar `OutsCalculator` overcard outs ajustados por textura S21.1: `Coordinated/Wet → OvercardOutsConnectedBoard`, `Paired → OvercardOutsPairedBoard`, default `OvercardOutsBase`. Boost `× OvercardOutsBlockerBoost` si `heroBlocksTopCard`. No doble-cuenta si el rank ya es straight-completing.
  - Origen en el legado: `Algorithms/OutsCalculator.cs` (sección overcards)
  - Crit. de pronto: test parametrizado con 3 texturas + blocker on/off.
  - Confianza: 🟢

- [ ] **T-19** Implementar backdoor flush + backdoor straight (S21.4). Solo aplica en flop. Backdoor flush requiere 3 cartas mismo palo + hero con 1; backdoor straight requiere 3 cartas en ventana 5 ranks + hero con 1. **S21.4 overlap discount**: si coexisten, descontar `BackdoorStraightImpliedOuts × BackdoorOverlapDiscount`.
  - Origen en el legado: `Algorithms/OutsCalculator.cs:142-149`
  - Crit. de pronto: test: hero con backdoor flush + backdoor straight en flop → outs adicionales = 1.5 + (1.0 − 1.0×0.5) = 2.0 (no 2.5).
  - Confianza: 🟢

- [ ] **T-20** Implementar `OutsCalculator.CalculateTaintedOuts(...)` con 3 categorías: (1) añadir la out pone 3+ del mismo palo en board, (2) parea el board, (3) crea 3 ranks consecutivas. Aplicar `discount = 0.7` si hero tiene flush draw, `0.3` si no. `EffectiveOuts = cleanOuts + taintedOuts × discount`.
  - Origen en el legado: `Algorithms/OutsCalculator.cs:286-336`
  - Crit. de pronto: test: hero `Ah Kh` flush draw + board `Qh 7h 2c`, una out al `5h` que parea con `5d` ya en deadCards → tainted con discount 0.7.
  - Confianza: 🟢

- [ ] **T-21** Implementar `OutsCalculator.OutsToEquity(int outs, int cardsToCome) → double` aplicando regla del 2/4: `outs × cardsToCome × RuleOf2Multiplier`.
  - Origen en el legado: `Algorithms/OutsCalculator.cs`
  - Crit. de pronto: test: 9 outs en flop con 2 cardsToCome → 36 % equity.
  - Confianza: 🟢

#### Algoritmo 4: `BoardTextureAnalyzer`

- [ ] **T-22** Implementar `BoardTextureAnalyzer.Analyze(int[] ranks, int[] suits) → BoardTextureResult` con `wetnessScore ∈ [0,100]`. Sumar 10 contribuciones: Monotone+35, TwoTone+15, Connected+20 o `connectedCount×8`, FlushPossibility+15, StraightPossibility+15, BroadwayHeavy+10, S21.3 BroadwayConnected+20, Paired-10, Trips-15, ExtraCards+5.
  - Origen en el legado: `Algorithms/BoardTextureAnalyzer.cs:62, 308-343`
  - Crit. de pronto: test: `Ah Kh Qh` → `Wet`, score ≥ 60; `2h 7c Td` → `Dry`, score < 15.
  - Confianza: 🟢

- [ ] **T-23** Implementar categorización con umbrales de `PokerConstants`: `WetnessDryMax=15`, `WetnessSemiDryMax=35`, `WetnessSemiWetMax=60`. Devolver `BoardTextureCategory` enum.
  - Origen en el legado: `Algorithms/BoardTextureAnalyzer.cs`
  - Crit. de pronto: test parametrizado con 5 boards (Dry, SemiDry, SemiWet, Wet, Paired).
  - Confianza: 🟢

- [ ] **T-24** Implementar `BoardTextureAnalyzer.AnalyzeBoardChange(previousBoard, newCard) → BoardChangeResult` que detecta `FlushCompleted` (4+ same suit), `FlushDrawAppeared` (3 mismo palo nuevo), `StraightCompleted` (4+ consecutive con `prevHadDraw`), `BoardPaired`, `OvercardAppeared`. Calcular `DangerLevel ∈ [0,10]`: flushCompleted+4, flushDrawAppeared+2, straightCompleted+3, boardPaired+2, overcardAppeared+1.
  - Origen en el legado: `Algorithms/BoardTextureAnalyzer.cs:153-225`
  - Crit. de pronto: test: flop `Qh Jh 7c` + turn `Th` → `FlushDrawAppeared = false` (ya estaba flush draw), `StraightCompleted = true` (prevHadDraw), `DangerLevel = 5`.
  - Confianza: 🟢

- [ ] **T-25** Implementar `BoardTextureAnalyzer.AnalyzeInitialBoard(flop)` que distingue `flushDrawPresent` (2+ same suit, DangerLevel+1) vs `flushPossible` (3+ same suit, DangerLevel+3). Esta distinción es crítica para `dangerousFlushBoard` en `PostflopDecisionService`.
  - Origen en el legado: `Algorithms/BoardTextureAnalyzer.cs:349-387`
  - Crit. de pronto: test: flop monotone (3 mismo palo) → `flushPossible = true`, DangerLevel +3.
  - Confianza: 🟢

- [ ] **T-26** Implementar `BoardTextureAnalyzer.ClassifyRiverCard(prevBoard, riverCard) → RiverCardType` (S22.2): `Brick` (no overcard, no flush, no straight) o `Scare` (overcard al top pair, completes flush/straight).
  - Origen en el legado: `Algorithms/BoardTextureAnalyzer.cs:227-249`
  - Crit. de pronto: test: river card brick → `Brick`; river card overcard al top pair → `Scare`.
  - Confianza: 🟢

#### Algoritmo 5: `PreflopEquityCalculator`

- [ ] **T-27** Cargar tabla estática de 169 manos heads-up con equity vs villain random. Implementar `GetEquity(handName, suited, numOpponents)` con multiway adjustment `equity^(1+log2(n)×0.35)` para `n > 1`.
  - Origen en el legado: `Algorithms/PreflopEquityCalculator.cs:1-232`
  - Crit. de pronto: test: `AhAd vs random` heads-up → 85.2 %; vs 5 oponentes → ≈ 49.0 %.
  - Confianza: 🟢

- [ ] **T-28** 🟡 Decisión: ¿implementar `IPreflopEquityCalculator` para mejor testabilidad? Si sí, exponer interface con la firma de T-27 y registrar en DI desde App.
  - Origen en el legado: anomalía documentada (no tiene interfaz)
  - Crit. de pronto: si se aplica, mock funciona en `EquityCalculatorService` tests.
  - Confianza: 🟡

#### Algoritmo 6: `HandEvaluator` (legacy)

- [ ] **T-29** 🟡 Decisión: ¿eliminar `HandEvaluator` brute-force? Si se elimina, redirigir su único caller (`EvaluateHandScore` que ya delega a `BitHandEvaluator`) directamente al bit-evaluator.
  - Origen en el legado: `Algorithms/HandEvaluator.cs:1-206`
  - Crit. de pronto: si se elimina, todos los tests siguen pasando (15+ tests).
  - Confianza: 🟡

### Bloque C — Servicios de decisión

#### Servicio 1: `PostflopGameContext`

- [ ] **T-30** Implementar `PostflopGameContext` como `sealed record` inmutable con campos: `HeroBetFlop`, `HeroBetTurn`, `HeroBetRiver`, `VillainBetFlop`, `VillainBetTurn`, `VillainBetRiver`, `VillainBetSizeFlop`, `VillainBetSizeTurn`, `VillainBetSizeRiver`, `FloatedFlop`, `TurnCalledWithFlushDanger`, `HeroCheckedAllStreets`, `IsAnyoneAllIn`, `_heroStackPreRebuy`.
  - Origen en el legado: `Services/PostflopGameContext.cs:1-171`
  - Crit. de pronto: instancia es inmutable; modificar via `with { … }` retorna nueva instancia.
  - Confianza: 🟢

- [ ] **T-31** Implementar helpers `WithFlopState(...)`, `WithTurnState(...)`, `WithRiverState(...)` que retornan nueva instancia con campos actualizados.
  - Origen en el legado: `Services/PostflopGameContext.cs:106-124`
  - Crit. de pronto: test: original no muta tras `WithFlopState`.
  - Confianza: 🟢

- [ ] **T-32** Implementar `TrackHeroStack(double currentStack)` con detección de auto-rebuy 100 BB: si stack < 50 BB y de repente 100 BB → preservar `_heroStackPreRebuy`. No actualizar P/L con el rebuy.
  - Origen en el legado: `Services/PostflopGameContext.cs:94, 133-147`
  - Crit. de pronto: test: stack 50 → 100 → `_heroStackPreRebuy = 50`, P/L de la mano sigue contabilizando con 50.
  - Confianza: 🟢

- [ ] **T-33** Implementar `CombineBoardChanges(prevChange, newChange) → BoardChangeResult` que acumula DangerLevel y flags entre calles.
  - Origen en el legado: `Services/PostflopGameContext.cs`
  - Crit. de pronto: flop FlushDrawAppeared + turn FlushCompleted → DangerLevel acumula 2+4=6.
  - Confianza: 🟢

#### Servicio 2: `ThresholdsRegistry`

- [ ] **T-34** Implementar `ThresholdsRegistry` con constructor `(IOptions<StrategyProfile> options)` que construye `Dictionary<ThresholdKey, StreetThresholds>` desde `options.Value.Thresholds`. **Fail-fast**: validar que existen las 30+ combinaciones `(BoardPosition × HandSituation)` requeridas; si falta cualquiera, lanzar excepción descriptiva con la clave faltante.
  - Origen en el legado: `Services/ThresholdsRegistry.cs:23-32`
  - Crit. de pronto: test: profile sin `Flop_OpenRaise` → constructor lanza excepción con mensaje `"Missing threshold: Flop_OpenRaise"`.
  - Confianza: 🟢

- [ ] **T-35** Implementar `Get(BoardPosition, HandSituation) → StreetThresholds` con lookup O(1).
  - Origen en el legado: `Services/ThresholdsRegistry.cs:35-58`
  - Crit. de pronto: test: lookup de 10 K llamadas concurrentes en < 50 ms.
  - Confianza: 🟢

#### Servicio 3: `PreflopAnalyzer`

- [ ] **T-36** Implementar `PreflopAnalyzer` con métodos `IsPreflopAggressor`, `HasRangeAdvantageOnBoard`, `CategorizeOpponentBet`, `DetectDonkBet`, `CalculateCbetAdjustment`. 🟡 Decisión: ¿estático o instanciado vía DI? El legado tiene ambos (static + interface trivial).
  - Origen en el legado: `Services/PreflopAnalyzer.cs:1-162`
  - Crit. de pronto: test: en 3-bet pot con `Q-high`, `HasRangeAdvantageOnBoard = true` (S22.X — 1+ broadway suficiente, no 2+).
  - Confianza: 🟢

#### Servicio 4: `BetSizingService`

- [ ] **T-37** Definir `BetSizingType` enum: `Standard`, `Reduced`, `Polarized`, `Merged`, `Overbet`. Definir `BetSizingOption` record.
  - Origen en el legado: `Services/BetSizingService.cs:1-50`
  - Crit. de pronto: enum y record públicos.
  - Confianza: 🟢

- [ ] **T-38** Implementar `BetSizingService.CalculateDynamicBetSize(...)` que modula por SPR (smooth interpolation), multiway, textura, posición, street y flags (`Overbet`, `Reduced`, `Merged`).
  - Origen en el legado: `Services/BetSizingService.cs:50-222`
  - Crit. de pronto: 9 tests existentes (`OpenScrape.App.Tests/BetSizing*`) pasan.
  - Confianza: 🟢

#### Servicio 5: `DangerPenaltyCalculator`

- [ ] **T-39** Implementar `DangerPenaltyCalculator.Calculate(equity, danger, hasFlushDraw, heroBlocksTopCard, isFacingBet, street, heroCompletedDraw) → DangerPenaltyResult`. Penalty proporcional: `FlushComplete = equity × 35%`, `StraightComplete = equity × 18%`, `FlushDraw = equity × 8%`. Street multipliers: Flop ×1.3, Turn ×1.0, River ×0.8. `FacingBetMultiplier=1.4`. Hero blocker: nut ×0.35, non-nut ×0.55, board4flush ×0.7. Skip si `heroCompletedDraw`.
  - Origen en el legado: `Services/DangerPenaltyCalculator.cs:31-110`
  - Crit. de pronto: 13 tests existentes (`OpenScrape.App.Tests/DangerPenalty*`) pasan.
  - Confianza: 🟢

#### Servicio 6: `ImpliedOddsCalculator`

- [ ] **T-40** Implementar `ImpliedOddsCalculator.CalculateImpliedOddsFactor(spr, position, numOpponents, draw)` con interpolación cuadrática en SPR. OOP multiway peor; IP con draw mejor.
  - Origen en el legado: `Services/ImpliedOddsCalculator.cs:55-64`
  - Crit. de pronto: 18 tests existentes pasan.
  - Confianza: 🟢

- [ ] **T-41** Implementar `ImpliedOddsCalculator.CalculateReverseImpliedOdds(...)` con multiplier por `PairClassification` + S20.3 bluff risk. Desactivar si villain all-in.
  - Origen en el legado: `Services/ImpliedOddsCalculator.cs:142-164`
  - Crit. de pronto: test: villain all-in → factor = 0.
  - Confianza: 🟢

#### Servicio 7: `RangePolarizer`

- [ ] **T-42** Implementar `RangePolarizer.GetOptimalRangeType(textura, isIP, spr, street) → RangeType` (Linear/Polarized/Condensed).
  - Origen en el legado: `Services/RangePolarizer.cs:1-85`
  - Crit. de pronto: test: river + Wet + IP → `Polarized`.
  - Confianza: 🟢

#### Servicio 8: `EquityCalculatorService`

- [ ] **T-43** Implementar `EquityCalculatorService.CalculateFullEquity(...)` que decide preflop (`PreflopEquityCalculator` lookup) o postflop (`MonteCarloSimulator` + `OutsCalculator` + `BoardTextureAnalyzer`). Genera `RecommendedAction` heurística.
  - Origen en el legado: `Services/EquityCalculatorService.cs:1-173`
  - Crit. de pronto: test: `community = []` invoca PreflopEquityCalculator; `community.Count >= 3` invoca MC + Outs.
  - Confianza: 🟡

- [ ] **T-44** 🟡 Decisión: ¿extraer thresholds de `RecommendedAction` heurística a `StrategyProfile`? Hoy están hardcoded.
  - Origen en el legado: anomalía documentada
  - Crit. de pronto: si se aplica, los thresholds se leen de `IOptions<StrategyProfile>`.
  - Confianza: 🟡

### Bloque D — `PostflopDecisionService` (corazón del motor)

- [ ] **T-45** Definir `BetSizeCategory` enum (`Underbet < 15%`, `Small 15-50%`, `Medium 50-100%`, `Large > 100%`). Definir `PostflopDecisionResult` record con `Action`, `BetSize`, `DecisionTag`, `Reason`.
  - Origen en el legado: `Services/PostflopDecisionService.cs:1-68`
  - Crit. de pronto: enum y record públicos.
  - Confianza: 🟢

- [ ] **T-46** Implementar `DetermineAction(PostflopDecisionInput input)` que ejecuta el pipeline en orden estricto (ver `design.md § Fluxo A`). Salida: `PostflopDecisionResult`.
  - Origen en el legado: `Services/PostflopDecisionService.cs:69-1893`
  - Crit. de pronto: matriz de 216 casos integration test (`OpenScrape.App.Tests/PostflopDecisionMatrixTests`) pasa.
  - Confianza: 🟢

- [ ] **T-47** Implementar el bloque de **equity efectiva**: `effective = equity − dangerPenalty + comboDrawBonus(textura) − reverseImpliedPenalty`, con cap `DangerCompletedDrawNoBetCap=45` si flush/straight completó sin hero. Final `Math.Max(0, …)`.
  - Origen en el legado: `Services/PostflopDecisionService.cs:140-181`
  - Crit. de pronto: test parametrizado con 5 combinaciones (clean, danger, completed-draw cap, all-zero floor, combo bonus).
  - Confianza: 🟢

- [ ] **T-48** Implementar **multiway penalty**: IP `extra × 2.0`; OOP `extra² × 6.0 × positionDamping × streetMult` (Turn ×1.2, River ×1.4); ×amplifier si villain IP+aggressor; reducido a ×0.5 con Flush+, ×0.7 con set en board no-paired IP (S22.5).
  - Origen en el legado: `Services/PostflopDecisionService.cs:230-284`
  - Crit. de pronto: test: 4 oponentes OOP turn → penalty cuadrático ×1.2.
  - Confianza: 🟢

- [ ] **T-49** Implementar **range narrowing** (S13.1): villain apostó 2+ calles → `+RangeNarrowingPerStreet=3.0 × (n−1)`. Bet-check-bet → multiplier ×0.5.
  - Origen en el legado: `Services/PostflopDecisionService.cs:344-356`
  - Crit. de pronto: test: bet-bet en flop+turn → +3 FoldBelow; bet-check-bet → +1.5.
  - Confianza: 🟢

- [ ] **T-50** Implementar **C-bet path** (S16.1): agresor preflop con equity en `[FoldBelow−15, FoldBelow)` apuesta a `GetCbetFrequency(street)` (Flop 65%, Turn 45%, River 30%).
  - Origen en el legado: `Services/PostflopDecisionService.cs:458-481`
  - Crit. de pronto: test: agresor flop, equity 30%, FoldBelow 35 → cbet 65% del tiempo.
  - Confianza: 🟢

- [ ] **T-51** Implementar **C-bet mixing** protección de range: equity `[FoldBelow, ThinValueAbove)` → check con probabilidad `1 − cbetFreq` (solo HU).
  - Origen en el legado: `Services/PostflopDecisionService.cs:523-544`
  - Crit. de pronto: test: HU + equity media → 35% check.
  - Confianza: 🟢

- [ ] **T-52** Implementar **`HandleFacingBet`**: push/fold mode (SPR < 1.0), 3-bet pot defense (S19.3), donk bet exploitation (S18.1), raise vs underbet, raise con TwoPair+, OnePair en board sin flush peligroso, agresor vs donk, thin value strong hand, floating IP, pot commitment.
  - Origen en el legado: `Services/PostflopDecisionService.cs:589-806`
  - Crit. de pronto: 50+ tests existentes pasan.
  - Confianza: 🟢

- [ ] **T-53** Implementar **`HandleNoBet`**: 3-bet pot OOP (S19.3), turn-river plan flush danger, river opportunity con draw completado, river delayed value tras check-check, check-raise OOP/IP con SPR guard (S19.1) mixing, slow play, float exit con bad runout abort, probe bet, overbet con nuts, river sizing contextual, strong/value bets con sizing por SPR, pot control, stackoff planning (S22.4), randomización adaptativa por villainType, thin value, double barrel con runout check.
  - Origen en el legado: `Services/PostflopDecisionService.cs:807-1306`
  - Crit. de pronto: 80+ tests existentes pasan.
  - Confianza: 🟢

- [ ] **T-54** Implementar **`CalculateAllinEV(equity, pot, stack) = equity×(pot+stack) − (1−equity)×stack`** y aplicarlo en push/fold mode.
  - Origen en el legado: `Services/PostflopDecisionService.cs:1427-1434`
  - Crit. de pronto: 8 tests existentes (`AllinEV*`) pasan.
  - Confianza: 🟢

- [ ] **T-55** Implementar **bluff catch turn/river** con multipliers: villainType (LAG ×0.80, TP ×1.20), runout (brick ×0.85, scare ×1.15), card removal (heroBlocksTopCard ×0.90), blocker bonus (×0.85). Saltar a `skipBluffCatch` si pot odds adversos.
  - Origen en el legado: `Services/PostflopDecisionService.cs:1591-1662`
  - Crit. de pronto: 21+ tests existentes (`BluffCatch*`) pasan.
  - Confianza: 🟡 (`goto` explícito en `:1602,1663` — refactor opcional)

- [ ] **T-56** Implementar **randomización adaptativa por villainType**: LAG 85%, LP 80%, TAG 60%, TP 55%, Unknown 70% (frecuencia de check en spots ±3% del threshold). Usar `Random.Shared.NextDouble()` 🟡 (decisión: ¿inyectar `IRandomProvider`?).
  - Origen en el legado: `Services/PostflopDecisionService.cs:1205-1236`
  - Crit. de pronto: test estadístico: 1000 ejecuciones con villainType=LAG → ratio check ≈ 0.85 ± 0.05.
  - Confianza: 🟡

- [ ] **T-57** Implementar **stackoff planning** (S22.4) con SPR proyectado al river: `CalculateProjectedRiverSPR(currentSPR, betSize, pot)`.
  - Origen en el legado: `Services/PostflopDecisionService.cs:1172-1196, 1441-1449`
  - Crit. de pronto: test: SPR 2.0 + bet 1/2 → projectedRiver = 0.5.
  - Confianza: 🟢

- [ ] **T-58** 🟡 **Anomalía:** eliminar bloque `if` vacío en `:1198-1203` (posible debug residual). Verificar que ningún test depende de él.
  - Origen en el legado: `Services/PostflopDecisionService.cs:1198-1203`
  - Crit. de pronto: 638 tests siguen pasando tras la eliminación.
  - Confianza: 🔴 (decisión humana — puede ser path no terminado)

- [ ] **T-59** 🟡 **Anomalía:** consolidar pot commitment block duplicado (`:772-795` y `:1665-1688`). Si ambos bloques tienen idéntica lógica, extraer a método `EvaluatePotCommitment(equity, pot, stack)`.
  - Origen en el legado: `Services/PostflopDecisionService.cs:772-795, 1665-1688`
  - Crit. de pronto: 638 tests pasan; el método extraído se llama desde ambos puntos.
  - Confianza: 🟡

### Bloque E — Tracking, telemetría y análisis

#### Servicio 9: `OpponentTracker`

- [ ] **T-60** Definir `enum PostflopAction` (Bet, Raise, Call, Check, Fold) en `Services/OpponentTracker.cs`.
  - Origen en el legado: `Services/OpponentTracker.cs:1-30`
  - Crit. de pronto: enum público.
  - Confianza: 🟢

- [ ] **T-61** Implementar `OpponentTracker` con `ConcurrentDictionary<string, OpponentProfile>` para profiles + `ConcurrentDictionary<int, string>` para seat→alias.
  - Origen en el legado: `Services/OpponentTracker.cs:16, 234-248`
  - Crit. de pronto: stress test con 100 threads concurrentes 5s sin race conditions.
  - Confianza: 🟢

- [ ] **T-62** Implementar `RegisterSeatAlias(int seat, string alias)` que mantiene mapping bidireccional. Usado para reconciliar cambios de posición tras moving blinds.
  - Origen en el legado: `Services/OpponentTracker.cs:234-248`
  - Crit. de pronto: test: registrar (1, "Alice") → `GetAliasBySeat(1) == "Alice"`.
  - Confianza: 🟢

- [ ] **T-63** Implementar `RecordPostflopAction(playerId, action, isIP, street)` que incrementa contadores totales + IP/OOP separados (BetIP, RaiseIP, CallIP, BetOOP, RaiseOOP, CallOOP) + per-street (CBetFlop/Turn/River, CheckRaise, BarrelTurn/River, DonkBet).
  - Origen en el legado: `Services/OpponentTracker.cs`
  - Crit. de pronto: test: 100 acciones de Alice → contadores correctos.
  - Confianza: 🟢

- [ ] **T-64** Implementar `GetTypeForPosition(playerId, bool villainIsIP) → OpponentType` (TAG/LAG/LP/TP/Unknown). Usar contadores de la posición; fallback a global si sample size insuficiente.
  - Origen en el legado: `Services/OpponentTracker.cs`
  - Crit. de pronto: 21 tests existentes pasan.
  - Confianza: 🟢

- [ ] **T-65** Implementar `GetAdjustedFoldEquity(playerId, defaultFE)` modulada por tipo de villano (TAG ajuste pequeño, LAG menor, LP mayor).
  - Origen en el legado: `Services/OpponentTracker.cs:191-208`
  - Crit. de pronto: test: villain LAG, default 35% → adjusted ~28%.
  - Confianza: 🟢

- [ ] **T-66** Implementar `GetFoldToBetPct(playerId)` con sentinel `−1` si no hay datos suficientes (`HasReliableFoldData < 8`).
  - Origen en el legado: `Services/OpponentTracker.cs:214-221`
  - Crit. de pronto: test: 5 muestras → retorna −1; 10 muestras → retorna fold%.
  - Confianza: 🟢

- [ ] **T-67** Implementar AF Laplace smoothing `(a+1)/(p+1)` separado IP/OOP.
  - Origen en el legado: `Services/OpponentTracker.cs`
  - Crit. de pronto: test: 0 bets / 0 calls IP → AF = 1.0 (no NaN).
  - Confianza: 🟢

- [ ] **T-68** 🔴 Decisión: ¿persistir `OpponentTracker` cross-sesión (Q-FSM-02)? Si sí, agregar `IDocumentStore` y métodos `SaveAsync` / `LoadAsync` por playerId.
  - Origen en el legado: lacuna documentada
  - Crit. de pronto: si se aplica, profile de Alice persiste tras reinicio de la app.
  - Confianza: 🔴

#### Servicio 10: `BankrollTrackerService`

- [ ] **T-69** Implementar `BankrollTrackerService` con `IDocumentStore` inyectado. Método `CalculateRiskOfRuin(bankroll, bigBlind, sessions) → double` con fórmula clásica `RoR = ((1−winrate/σ) / (1+winrate/σ))^(bankroll/σ)`. Varianza muestral, no teórica.
  - Origen en el legado: `Services/BankrollTrackerService.cs:177-213`
  - Crit. de pronto: test: dataset known → RoR coincide ±0.001 con el cálculo a mano.
  - Confianza: 🟡 (N+1 queries documentadas)

- [ ] **T-70** Resolver N+1 queries: cargar todas las sesiones en una sola query Marten, no una por sesión.
  - Origen en el legado: anomalía documentada
  - Crit. de pronto: profiling muestra 1 query, no N.
  - Confianza: 🟡

- [ ] **T-71** Reemplazar `try/catch (Exception)` silencioso por log explícito vía `ILogger`.
  - Origen en el legado: anomalía documentada
  - Crit. de pronto: cada catch escribe `LogError("BankrollTracker fallo: {Reason}", ex.Message)`.
  - Confianza: 🟡

#### Servicio 11: `ExploitabilityCalculator`

- [ ] **T-72** Definir `enum LeakCategory` y 6 DTOs (`DecisionRecord`, `Leak`, `LeakReport`, etc.). Implementar `ExploitabilityCalculator` con `ConcurrentQueue<DecisionRecord>` cap 10 K (FIFO).
  - Origen en el legado: `Services/ExploitabilityCalculator.cs:1-322`
  - Crit. de pronto: tras 10 001 records, queue tiene exactamente 10 K (oldest descartado).
  - Confianza: 🟢

- [ ] **T-73** Implementar `RecordDecision(record)`, `GetTopLeaks(n)` ordenados por magnitud.
  - Origen en el legado: `Services/ExploitabilityCalculator.cs`
  - Crit. de pronto: test: registrar 100 records con leaks variados → top 5 ordenados por magnitud descendente.
  - Confianza: 🟢

- [ ] **T-74** 🔴 **Anomalía:** parametrizar `BigBlind` (hoy hardcoded a 1.0). Inyectar vía `IOptions<StrategyProfile>` o como argumento.
  - Origen en el legado: `Services/ExploitabilityCalculator.cs:93, 322-348`
  - Crit. de pronto: test: con BigBlind=2.0, los reportes en BB/100 escalan correctamente.
  - Confianza: 🔴

#### Servicio 12: `AutoCalibrationService`

- [ ] **T-75** Implementar `AutoCalibrationService.PreviewAndApply(topLeaks) → CalibrationResult` que propone ajustes a parámetros del `StrategyProfile`. **🔴 BUG: leer `OldValue` del `StrategyProfile` actual** (NO hardcoded a 45/40).
  - Origen en el legado: `Services/AutoCalibrationService.cs:174-208`
  - Crit. de pronto: test: si profile actual tiene `FoldBelow=50`, calibración muestra `OldValue=50` (no 45).
  - Confianza: 🔴

#### Servicio 13: `StrategyBacktester`

- [ ] **T-76** Implementar `StrategyBacktester.RunBacktest(sessions) → BacktestResult` que recorre cada `StreetDecision` persistido, vuelve a evaluar con el motor actual (vía `IPostflopDecisionService`) y reporta divergencias en `List<DecisionDivergence>`.
  - Origen en el legado: `Services/StrategyBacktester.cs:1-243`
  - Crit. de pronto: 5 tests existentes pasan.
  - Confianza: 🟢

- [ ] **T-77** Calcular estimación de impacto en BB/100 sumando `(currentEV − historicalEV)` × frequencyOfDecision.
  - Origen en el legado: `Services/StrategyBacktester.cs`
  - Crit. de pronto: test: con motor sin cambios, divergences == 0 y BB/100 impact == 0.
  - Confianza: 🟢

#### Servicio 14: `StrategyAnalyzerService`

- [ ] **T-78** Implementar `StrategyAnalyzerService.Analyze(sessions) → StrategyAnalysisResult` con stats por posición, street, situación + `EquityVsOutcome` por buckets.
  - Origen en el legado: `Services/StrategyAnalyzerService.cs:1-397`
  - Crit. de pronto: 12 tests existentes pasan.
  - Confianza: 🟢

- [ ] **T-79** Definir 6 satellite types (`PositionStats`, `StreetStats`, `SituationStats`, `SessionSummary`, `EquityVsOutcome`, `StrategyAnalysisResult`).
  - Origen en el legado: `Services/StrategyAnalyzerService.cs`
  - Crit. de pronto: tipos públicos con campos correctos.
  - Confianza: 🟢

### Bloque F — DTOs

- [ ] **T-80** Definir `PostflopDecisionInput` como `record` con 6 required + 36 con defaults (equity, street, situation, board texture, posiciones, profiles villano, flags cross-street, R/I outs, kicker strength, etc.).
  - Origen en el legado: `DTOs/PostflopDecisionInput.cs:1-66`
  - Crit. de pronto: 42 propiedades; constructor con required params no-nullables.
  - Confianza: 🟢

- [ ] **T-81** Definir `DecisionRequest` y `DecisionResult` como `sealed record` (DTOs del facade `IPokerCalculator`).
  - Origen en el legado: `DTOs/DecisionRequest.cs:1-72`, `DTOs/DecisionResult.cs:1-47`
  - Crit. de pronto: types públicos.
  - Confianza: 🟢

### Bloque G — Hardening y limpieza

- [ ] **T-82** 🟡 Limpiar `obj/Debug/net8.0/` y `obj/Debug/net9.0/` (residuos de migraciones de target framework).
  - Origen en el legado: `legacy-mapping.md § obj/`
  - Crit. de pronto: solo `obj/Debug/net10.0/` y `obj/Release/net10.0/` permanecen.
  - Confianza: 🟢

- [ ] **T-83** 🟡 Decisión: ¿mover tipos `nested` (`MonteCarloSimulator.EquityResult`, `OutsCalculator.OutsResult`, `EquityCalculatorService.FullEquityAnalysis`) a `DTOs/` o `ValueObjects/`? Hoy las interfaces los referencian por path nested.
  - Origen en el legado: `Interfaces/IMonteCarloSimulator.cs`, `IOutsCalculator.cs`, `IEquityCalculatorService.cs`
  - Crit. de pronto: si se aplica, los mocks de las interfaces no requieren conocer la implementación.
  - Confianza: 🟡

- [ ] **T-84** 🔴 Validar input de `DetermineAction`: equity ∈ [0, 100], pot > 0, stack ≥ 0. Si fuera de rango → log + `ArgumentException` o devolver `Action=Check` (decisión humana).
  - Origen en el legado: lacuna documentada (Q-FSM-01)
  - Crit. de pronto: test: equity=−10 → comportamiento documentado (no NaN).
  - Confianza: 🔴

---

## Tareas de Testes

- [ ] **TT-01** Test happy path `DetermineAction`: 216 casos de matriz integration test (`OpenScrape.App.Tests/PostflopDecisionMatrixTests`) cubriendo todas las combinaciones `(BoardPosition × HandSituation × EquityTier × Position)`.
- [ ] **TT-02** Test edge cases `MonteCarloSimulator`: river determinístico, turn determinístico, flop con varianza < 0.5%.
- [ ] **TT-03** Test concurrencia `OpponentTracker`: 100 writers + 100 readers concurrentes 5s sin exceptions.
- [ ] **TT-04** Test `BitHandEvaluator`: 15 casos del corpus existente (cubre cada `HandRank`).
- [ ] **TT-05** Test `OutsCalculator`: 23 casos del corpus existente (incluye tainted, backdoor, S21.4 overlap).
- [ ] **TT-06** Test `BoardTextureAnalyzer`: 26 casos del corpus existente (Dry/SemiDry/SemiWet/Wet/Paired/Monotone + AnalyzeBoardChange + AnalyzeInitialBoard + ClassifyRiverCard).
- [ ] **TT-07** Test `ThresholdsRegistry` fail-fast: profile incompleto → constructor throws con mensaje descriptivo.
- [ ] **TT-08** Test `PostflopGameContext`: inmutabilidad, `WithFlopState/TurnState`, `TrackHeroStack` auto-rebuy.
- [ ] **TT-09** Test integración `EquityCalculatorService.CalculateFullEquity` con `community = []` → `PreflopEquityCalculator`; con `community.Count >= 3` → MC + Outs.
- [ ] **TT-10** Test `BankrollTrackerService.CalculateRiskOfRuin` con dataset known → resultado coincide con cálculo a mano.
- [ ] **TT-11** Test `StrategyBacktester` con motor sin cambios → divergences == 0.
- [ ] **TT-12** Test `StrategyAnalyzerService` con 100 manos → stats agregadas correctas.
- [ ] **TT-13** Test `AutoCalibrationService` 🔴 (fallaría hoy): `OldValue` coincide con `StrategyProfile` actual.
- [ ] **TT-14** Benchmark `BitHandEvaluator.EvaluateHandScore` < 1 µs y 0 allocations (BenchmarkDotNet).
- [ ] **TT-15** Benchmark `MonteCarloSimulator.RunMonteCarloSimulation(50_000)` < 200 ms en CPU consumer (8 cores).
- [ ] **TT-16** Test `DecisionRequest`/`DecisionResult` round-trip via `IPokerCalculator` facade.
- [ ] **TT-17** Test `RangePolarizer.GetOptimalRangeType` con 6 escenarios (Dry/Wet × IP/OOP × shallow/deep SPR).
- [ ] **TT-18** Test `DangerPenaltyCalculator`: 13 casos del corpus (incluye blocker effect granular nut/non-nut/board4flush).
- [ ] **TT-19** Test `ImpliedOddsCalculator`: 18 casos del corpus.

## Tareas de Migração de Datos

> No aplica. Este módulo no maneja persistencia propia. La única dependencia de Marten (`BankrollTrackerService`) consume documentos existentes (`GameSession`, `HandRecord`, `BankrollSnapshot`) cuya migración cae bajo `OpenScrape.Infrastructure`.

---

## Ordem Sugerida

1. **Bloque A (T-01..T-04)** primero — sin proyecto compilable nada funciona.
2. **Bloque B (T-05..T-29)** en paralelo:
   - `BitHandEvaluator` (T-05..T-08) bloquea a `MonteCarloSimulator` (T-09..T-16) porque MC consume `EvaluateHandScore`.
   - `OutsCalculator` (T-17..T-21) y `BoardTextureAnalyzer` (T-22..T-26) son independientes entre sí y de MC.
   - `PreflopEquityCalculator` (T-27..T-28) es independiente.
   - `HandEvaluator` legacy (T-29) puede dejarse al final (decisión humana sobre eliminación).
3. **Bloque C (T-30..T-44)**:
   - `PostflopGameContext` (T-30..T-33) primero — lo consumen `PostflopDecisionService` y App.
   - `ThresholdsRegistry` (T-34..T-35) — lo consumen `PostflopDecisionService`.
   - `PreflopAnalyzer`, `BetSizingService`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `RangePolarizer` (T-36..T-42) en paralelo.
   - `EquityCalculatorService` (T-43..T-44) consume MC + Outs + BTA + PreflopEquity.
4. **Bloque D (T-45..T-59)** **al final del bloque "puro"**: `PostflopDecisionService` consume todo lo anterior. T-58/T-59 (anomalías) son refactors opcionales.
5. **Bloque E (T-60..T-79)** en paralelo con Bloque D:
   - `OpponentTracker` (T-60..T-68) sin dependencias en otros servicios DM.
   - `BankrollTrackerService` (T-69..T-71) consume Marten.
   - `ExploitabilityCalculator` (T-72..T-74), `AutoCalibrationService` (T-75) consumen `StrategyProfile`.
   - `StrategyBacktester` (T-76..T-77) y `StrategyAnalyzerService` (T-78..T-79) consumen `IPostflopDecisionService`.
6. **Bloque F (T-80..T-81)** puede hacerse al inicio del Bloque C (los DTOs son contratos de entrada).
7. **Bloque G (T-82..T-84)** al final.

---

## Lacunas Pendientes (🔴)

- **T-58** Eliminación del bloque `if` vacío en `PostflopDecisionService.cs:1198-1203` — verificar si es debug residual o path no terminado.
- **T-68** Persistencia de `OpponentTracker` cross-sesión (Q-FSM-02) — pendiente decisión sobre schema Marten.
- **T-74** Parametrización de `BigBlind` en `ExploitabilityCalculator` (hardcoded 1.0).
- **T-75** Fix de `AutoCalibrationService.PreviewAndApply` `OldValue` hardcoded — el cálculo de calibración actual está roto si el profile difiere de los defaults históricos.
- **T-84** Validación de input en `DetermineAction` (Q-FSM-01 watchdog) — cómo manejar inputs fuera de rango (excepción vs fallback).
- **T-44** Mover thresholds de `RecommendedAction` heurística en `EquityCalculatorService` desde hardcoded a `StrategyProfile`.
- **T-29** Decisión sobre eliminar `HandEvaluator` legacy.
- **T-83** Decisión sobre mover tipos `nested` de las 3 interfaces problemáticas a `DTOs/`.
- **T-56** Decisión sobre inyectar `IRandomProvider` en lugar de `Random.Shared.NextDouble()` directo (testabilidad y reproducibilidad).
