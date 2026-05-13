# OpenScrape.DecisionMaker — Requisitos

> Capa de **motor de decisión y cálculo numérico** del bot. Es la única autorizada a contener algoritmos de equity, evaluación de manos, decisiones postflop y telemetría GTO. **Sin lógica de UI, sin OCR, sin persistencia de UI** (la única dependencia de Marten vive en `BankrollTrackerService` y consume `IDocumentStore` inyectado). 🟢 (`src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj`)

---

## Visión General

`OpenScrape.DecisionMaker` agrupa **38 archivos C# / ~6,800 LOC** organizados en cuatro carpetas funcionales:

| Carpeta | Tipos | Rol |
|---------|-------|-----|
| `Algorithms/` | `BitHandEvaluator`, `MonteCarloSimulator`, `OutsCalculator`, `BoardTextureAnalyzer`, `PreflopEquityCalculator`, `HandEvaluator` (legacy) + 4 interfaces + struct `HandScore` | Motores numéricos puros, sin estado mutable, sin Marten |
| `Services/` | `PostflopDecisionService` (1893 LOC), `PostflopGameContext`, `PreflopAnalyzer`, `BetSizingService`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `RangePolarizer`, `ThresholdsRegistry`, `OpponentTracker`, `EquityCalculatorService`, `StrategyBacktester`, `StrategyAnalyzerService`, `BankrollTrackerService`, `ExploitabilityCalculator`, `AutoCalibrationService` | Servicios de decisión, telemetría y análisis |
| `Interfaces/` | 13 contratos (`IPostflopDecisionService`, `IMonteCarloSimulator`, `IOutsCalculator`, `IBoardTextureAnalyzer`, `IBetSizingService`, `IDangerPenaltyCalculator`, `IImpliedOddsCalculator`, `IRangePolarizer`, `IThresholdsRegistry`, `IOpponentTracker`, `IEquityCalculatorService`, `IStrategyAnalyzerService`, `IStrategyBacktester`, `IBankrollTrackerService`, `IExploitabilityCalculator`, `IAutoCalibrationService`, `IPreflopAnalyzer`) | Inversión de dependencias hacia `OpenScrape.App` |
| `DTOs/` | `PostflopDecisionInput` (record con 6 required + 36 con defaults), `DecisionRequest`, `DecisionResult` | Inputs/outputs inmutables del motor |
| (raíz) | `PokerConstants` (~25 constantes algorítmicas no configurables) | Constantes derivadas de teoría del poker |

🟢 El módulo declara `<TargetFramework>net10.0</TargetFramework>` y `InternalsVisibleTo("OpenScrape.App.Tests")`. Sus únicas dependencias externas son `Marten 8.24.0` (consumida sólo por `BankrollTrackerService`) y `Microsoft.Extensions.{Logging.Abstractions,Options} 10.0.3`. (`src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj`)

> ⚠️ **Importante:** El facade público que CLAUDE.md describe (`IPokerCalculator → UnifiedPokerCalculator`) **no vive aquí**. Está en `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` y es quien orquesta los building blocks expuestos por este módulo. 🟢

---

## Responsabilidades

- **Calcular equity heads-up y multi-way con precisión adaptativa al street.** En river y turn se enumera exhaustivamente (C(45,2)=990 manos en river, 45×C(44,2)≈42K en turn); en flop se simulan 50K iteraciones MC paralelas; en preflop se hace lookup en tabla de 169 manos heads-up con ajuste `equity^(1+log2(n)×0.35)` para n>1 oponentes. 🟢 (`Algorithms/MonteCarloSimulator.cs:50,127-135,142-237,244-362,369-424`, `Algorithms/PreflopEquityCalculator.cs`)
- **Evaluar la fuerza de una mano de 5–7 cartas en O(1) sin allocations.** `BitHandEvaluator.EvaluateHandScore` retorna un `readonly struct HandScore` con `CompositeScore` (long con `rank<<20 | k1<<16 | … | k5`) que permite comparación O(1) vía `long.CompareTo`. Reemplaza al brute-force `C(7,5)` del legacy `HandEvaluator`. 🟢 (`Algorithms/BitHandEvaluator.cs:253-408`, `Algorithms/IHandEvaluator.cs`)
- **Contar outs y proyectos con inclusión-exclusión, backdoor, tainted-outs y combo-draw.** `OutsCalculator.CalculateOuts` aplica `flushOuts + straightOuts − overlapOuts + overcardOuts(textura, blocker) + backdoorOuts`, descuenta tainted (3 categorías: completa flush para villano, parea board, abre straight) y aplica S21.4 backdoor overlap discount. 🟢 (`Algorithms/OutsCalculator.cs:46-202,142-149,286-336`)
- **Clasificar la textura del board y detectar cambios entre calles.** `BoardTextureAnalyzer.Analyze` calcula `wetnessScore ∈ [0,100]` con 10 contribuciones (Monotone+35, TwoTone+15, BroadwayConnected+20, etc.) y categoriza en {Dry, SemiDry, SemiWet, Wet, Paired}. `AnalyzeBoardChange(previousBoard, newCard)` detecta `FlushCompleted/FlushDrawAppeared/StraightCompleted/BoardPaired/OvercardAppeared` y produce un `DangerLevel ∈ [0,10]`. 🟢 (`Algorithms/BoardTextureAnalyzer.cs:62,153,308-343,349-387`)
- **Decidir la acción postflop (Bet/Raise/Call/Check/Fold + sizing) según ≥10 paths.** `PostflopDecisionService.DetermineAction(PostflopDecisionInput)` es el único punto de entrada postflop. Pipeline en orden: (1) equity efectiva con dangerPenalty + comboDrawBonus + reverseImpliedPenalty, (2) modo simplificado para `RaiseOverLimper` con `IsSimplified=true`, (3) ajustes de thresholds (FoldBelow/ThinValueAbove) por polarizer, facing-bet penalty, multiway, 3-bet/4-bet/squeeze, blind-vs-blind, broadway-wet, agresor/caller, range narrowing, kicker, barreling, sizing escalation, stats reales, WSD/Barrel/DonkBet/WTSD overrides, SPR push/fold con interpolación suave, (4) c-bet path con frequency mixing, (5) ramas según facing-bet/no-bet con sus 10+ subpaths. 🟢 (`Services/PostflopDecisionService.cs:69`, `flowcharts/OpenScrape.DecisionMaker-DetermineAction.md`)
- **Mantener el estado cross-street inmutable de la mano.** `PostflopGameContext` es un `sealed record` con flags por calle (`HeroBetFlop/HeroBetTurn`, `VillainBetFlop/Turn/River` + sizes), `FloatedFlop`, `TurnCalledWithFlushDanger`, `HeroCheckedAllStreets`, `IsAnyoneAllIn`. Helpers `WithFlopState/WithTurnState`, `TrackHeroStack` (auto-rebuy detection) y `CombineBoardChanges` devuelven nuevas instancias. 🟢 (`Services/PostflopGameContext.cs:94,106-124,133-147`)
- **Trackear y consultar el perfil estadístico de cada villano de forma thread-safe.** `OpponentTracker` mantiene un `ConcurrentDictionary<playerId, OpponentProfile>` con 17 contadores por jugador, separación IP/OOP de Aggression Factor, mapeo seat→alias y métodos de fold equity ajustados (`GetAdjustedFoldEquity`, `GetFoldToBetPct` con sentinel −1 para "sin datos"). 🟢 (`Services/OpponentTracker.cs:16,191-208,214-221,234-248`)
- **Calcular sizing dinámico por contexto.** `BetSizingService.CalculateDynamicBetSize` modula el size por SPR (smooth interpolation, no buckets), multiway, textura, posición, street y flags (overbet/reduced/merged). 🟢 (`Services/BetSizingService.cs`)
- **Calcular danger penalty proporcional con blocker effects.** `DangerPenaltyCalculator.Calculate` retorna `equity × pct` (FlushComplete 35 %, StraightComplete 18 %, FlushDraw 8 %), escalado por street multiplier (Flop ×1.3, Turn ×1.0, River ×0.8) y `FacingBetMultiplier=1.4`. Hero blocker effect granular (nut ×0.35, non-nut ×0.55, board4flush ×0.7) y skip si hero ya completó la draw. 🟢 (`Services/DangerPenaltyCalculator.cs:31-110`)
- **Calcular implied odds e implied reverse odds con interpolación cuadrática SPR.** `ImpliedOddsCalculator` ajusta el factor por número de oponentes (OOP multiway peor, IP con draw mejor) y aplica S20.3 bluff risk en `CalculateReverseImpliedOdds`. Desactiva si villano all-in. 🟢 (`Services/ImpliedOddsCalculator.cs:55-64,142-164`)
- **Polarizar el rango (Linear/Polarized/Condensed) según contexto.** `RangePolarizer.GetOptimalRangeType` decide por textura, IP/OOP, SPR y street. 🟢 (`Services/RangePolarizer.cs`)
- **Cargar y validar thresholds tipados con fail-fast en arranque.** `ThresholdsRegistry` expone lookup `Dictionary<ThresholdKey, StreetThresholds>` O(1); en su constructor valida que las 30+ combinaciones `{BoardPosition} × {HandSituation}` exigidas estén presentes y aborta el arranque si falta alguna. 🟢 (`Services/ThresholdsRegistry.cs:23-32`)
- **Calcular equity completa con outs+textura (orquestador).** `EquityCalculatorService.CalculateFullEquity` decide preflop (lookup) vs postflop (MC + Outs + textura) y emite `RecommendedAction` heurística. 🟡 Los thresholds de la heurística están **hardcoded** en el servicio (anomalía: deberían venir de `StrategyProfile`). (`Services/EquityCalculatorService.cs`)
- **Replay histórico de decisiones contra el motor actual.** `StrategyBacktester.RunBacktest(sessions)` recorre cada `StreetDecision` persistido, vuelve a evaluar con el motor actual y reporta divergencias (`DecisionDivergence`) + estimación de BB/100 impact. 🟢 (`Services/StrategyBacktester.cs`)
- **Generar métricas agregadas para la pestaña Estadísticas.** `StrategyAnalyzerService` calcula stats por posición, street, situación + `EquityVsOutcome` por buckets (5-10 buckets). Define 6 satellite types (`PositionStats`, `StreetStats`, `SituationStats`, `SessionSummary`, `EquityVsOutcome`, `StrategyAnalysisResult`). 🟢 (`Services/StrategyAnalyzerService.cs`)
- **Calcular Risk-of-Ruin del bankroll.** `BankrollTrackerService.CalculateRiskOfRuin(bankroll, bigBlind, sessions)` aplica fórmula clásica con varianza de muestra (no de teoría) y umbral configurable `RiskOfRuinThreshold=0.05`. Único servicio que toca Marten (consume `IDocumentStore` para cargar sesiones). 🟢 (`Services/BankrollTrackerService.cs:177-213`)
- **Telemetría GTO de explotabilidad.** `ExploitabilityCalculator` mantiene una `ConcurrentQueue<DecisionRecord>` FIFO de hasta 10K entradas, calcula `LeakCategory` por bucket (overfold, underfold, overbluff, etc.) y produce un report agregable. 🟡 Tiene `BigBlind=1.0` hardcoded — anomalía si el usuario opera en otro stake. (`Services/ExploitabilityCalculator.cs:93,322-348`)
- **Auto-calibración de parámetros basada en TopLeaks.** `AutoCalibrationService.PreviewAndApply(topLeaks)` propone ajustes a parámetros del `StrategyProfile`. 🔴 **BUG conocido:** los `OldValue` están hardcoded a 45/40 en lugar de leerse del profile actual. (`Services/AutoCalibrationService.cs:174-208`)
- **Centralizar constantes algorítmicas inmutables.** `PokerConstants` define ~25 constantes (`MaxOutsPossible=15`, `RuleOf2Multiplier=2.0`, `BackdoorFlushImpliedOuts=1.5`, `OvercardOutsBase=3`, `WetnessDryMax=15`, `BlockedComboUnreliableThreshold=20.0`, etc.) que NO se exponen como `IOptions` porque no son configurables por estrategia (son leyes del juego o defaults teóricos). 🟢 (`PokerConstants.cs`)

---

## Reglas de Negocio

### Reglas de equity y evaluación de manos

- **Equity en river es siempre exacta y determinística.** `MonteCarloSimulator` enumera C(45,2)=990 manos posibles del villano sin sampling. 🟢 (`Algorithms/MonteCarloSimulator.cs:142-237`)
- **Equity en turn es exacta para el rival pero enumera todos los river cards.** Para cada uno de los 45 rivers posibles, enumera C(44,2) hands del villano → 990 hands por river × 45 rivers ≈ 42 K evaluaciones. 🟢 (`Algorithms/MonteCarloSimulator.cs:244-362`)
- **Equity en flop es Monte Carlo paralelizado, 50 K iteraciones.** Error esperado ±0.5 %. 🟢 (`Algorithms/MonteCarloSimulator.cs:127-135`)
- **Equity preflop es lookup en tabla de 169 manos heads-up.** Fórmula multiway: `equity^(1+log2(n)×0.35)`. 🟢 (`Algorithms/PreflopEquityCalculator.cs`)
- **Skipped iterations no contaminan el resultado.** `TryDrawFromRange` con 20 intentos de selección antes de skip-on-block; `SkippedSimulations` separado de `TotalSimulations`. 🟢 (`Algorithms/MonteCarloSimulator.cs:582-618`)
- **Si el rango villano está bloqueado >20 % por hero/board, equity se marca como `IsReliable=false`.** Constante `BlockedComboUnreliableThreshold=20.0`. 🟢 (`PokerConstants.cs`, `Algorithms/MonteCarloSimulator.cs`)
- **`HandScore.CompositeScore` codifica la mano completa en un `long`.** `rank<<20 | k1<<16 | k2<<12 | k3<<8 | k4<<4 | k5`. Permite comparación O(1) sin GC pressure. 🟢 (`Algorithms/IHandEvaluator.cs`)
- **El wheel (A-2-3-4-5) cuenta como straight con `high=5`, no `high=14`.** `BitHandEvaluator.FindStraightHigh` itera de 14 hacia abajo hasta 6 y aplica máscara especial `WheelMask = (1<<14) | (1<<2) | (1<<3) | (1<<4) | (1<<5)` retornando 5. 🟢 (`Algorithms/BitHandEvaluator.cs:24,415-429`)
- **Coexisten dos evaluadores activos:** `HandEvaluator` (legacy brute-force) y `BitHandEvaluator` (zero-alloc). El legacy delega a `new BitHandEvaluator()` en `EvaluateHandScore`. 🟡 No hay justificación técnica para mantener ambos; candidato a eliminación. (`Algorithms/HandEvaluator.cs`, `code-analysis.md § Anomalías`)

### Reglas de outs y proyectos

- **Outs aplica inclusión-exclusión entre flush y straight.** `totalOuts = flushOuts + straightOuts − overlapOuts + overcardOuts + backdoorOuts`. 🟢 (`Algorithms/OutsCalculator.cs:46-202`)
- **Overcards solo se cuentan si `community.Count >= 3` y hero no tiene mano hecha.** Outs por overcard se ajustan por textura (S21.1: Coordinated/Wet usa `OvercardOutsConnectedBoard`, Paired usa `OvercardOutsPairedBoard`, default `OvercardOutsBase`). Boost adicional si `heroBlocksTopCard` (×`OvercardOutsBlockerBoost`). 🟢 (`Algorithms/OutsCalculator.cs`)
- **Backdoor solo cuenta en flop.** Backdoor flush requiere 3 cartas del mismo palo Y al menos una del hero (+`BackdoorFlushImpliedOuts=1.5`). Backdoor straight requiere 3 cartas en ventana de 5 ranks consecutivos Y al menos una del hero (+`BackdoorStraightImpliedOuts=1.0`). 🟢 (`Algorithms/OutsCalculator.cs`)
- **S21.4 backdoor overlap discount:** si coexisten flush draw + backdoor straight, descuenta `BackdoorStraightImpliedOuts × BackdoorOverlapDiscount` para evitar doble conteo. 🟢 (`Algorithms/OutsCalculator.cs:142-149`)
- **Tainted outs descuentan 30 % o 70 % según fuerza.** Si hero tiene flush draw, descuento `TaintedOutsDiscountHeroStrong=0.7`; sin flush, `TaintedOutsDiscountHeroWeak=0.3`. `EffectiveOuts = cleanOuts + taintedOuts × discount`. 🟢 (`Algorithms/OutsCalculator.cs:286-336`)
- **Combo draw = flush draw + (OESD || gutshot).** Marca semi-bluff premium con bonus de equity en `PostflopDecisionService`. 🟢 (`Algorithms/OutsCalculator.cs`)
- **`OutsToEquity` aplica regla del 2 (turn) y del 4 (river): `outs × cardsToCome × 2.0`.** Constante `RuleOf2Multiplier=2.0`. 🟢 (`PokerConstants.cs`, `Algorithms/OutsCalculator.cs`)

### Reglas de textura del board

- **`wetnessScore ∈ [0,100]` se calcula sumando 10 contribuciones.** Monotone+35, TwoTone+15, Connected+20 (o `connectedCount × 8`), FlushPossibility+15, StraightPossibility+15, BroadwayHeavy+10, S21.3 BroadwayConnected+20, Paired-10, Trips-15, ExtraCards+5. 🟢 (`Algorithms/BoardTextureAnalyzer.cs:308-343`)
- **Categoría se asigna por umbrales:** `Dry < 15`, `SemiDry [15-35)`, `SemiWet [35-60)`, `Wet ≥ 60`. `Paired` y `Monotone` son ortogonales y se exponen como flags adicionales. 🟢 (`Algorithms/BoardTextureAnalyzer.cs:62`)
- **`AnalyzeBoardChange` produce un `DangerLevel ∈ [0,10]`.** flushCompleted+4, flushDrawAppeared+2, straightCompleted+3, boardPaired+2, overcardAppeared+1. Se acumula entre calles vía `PostflopGameContext.CombineBoardChanges`. 🟢 (`Algorithms/BoardTextureAnalyzer.cs:153`)
- **`AnalyzeInitialBoard` distingue flushDrawPresent (2+) vs flushPossible (3+).** Crítico: `dangerousFlushBoard` en `PostflopDecisionService` solo bloquea raise con OnePair en flop si es **monotone** (DangerLevel ≥ 3), no con simple FlushDrawAppeared. 🟢 (`Algorithms/BoardTextureAnalyzer.cs:349-387`, `Services/PostflopDecisionService.cs:648-651`)
- **`ClassifyRiverCard` (S22.2)** clasifica el river card como brick (no-overcard, no-flush, no-straight) o scare (overcard al top pair, completes flush/straight). Modula bluff catch multipliers. 🟢 (`Algorithms/BoardTextureAnalyzer.cs:227-249`)

### Reglas de la decisión postflop (PostflopDecisionService)

- **`equity efectiva = equity − dangerPenalty + comboDrawBonus(textura) − reverseImpliedPenalty`, con cap.** Si flush/straight completó y hero no lo tiene → cap a `DangerCompletedDrawNoBetCap=45`. Final clamp `Math.Max(0, …)`. 🟢 (`Services/PostflopDecisionService.cs:140-181`)
- **C-bet path tiene prioridad sobre bluff genérico.** Agresor preflop con equity en `[FoldBelow−15, FoldBelow)` apuesta a `GetCbetFrequency(street)` (Flop 65 %, Turn 45 %, River 30 %) modulada por textura/runout/CheckRaise%. 🟢 (`Services/PostflopDecisionService.cs:458-481`)
- **C-bet mixing protege el checking range (solo HU).** Equity media `[FoldBelow, ThinValueAbove)` chequea con probabilidad `1 − cbetFreq`. No aplica multiway. 🟢 (`Services/PostflopDecisionService.cs:523-544`)
- **Multiway penalty: IP lineal, OOP cuadrático.** IP `extra × 2.0`; OOP `extra² × 6.0 × positionDamping × streetMult` (Turn ×1.2, River ×1.4). Reducido a ×0.5 con Flush+, ×0.7 con set en board no-paired IP (S22.5). 🟢 (`Services/PostflopDecisionService.cs:230-284`)
- **Range narrowing: villain apostó 2+ calles → +`RangeNarrowingPerStreet=3.0 × (n−1)`.** Bet-check-bet aplica multiplier ×0.5 (debilidad → no narrow agresivo). 🟢 (`Services/PostflopDecisionService.cs:344-356`)
- **3-bet/4-bet/squeeze/limp-raise pot adjustment (S20.4).** Incrementos sobre FoldBelow y ThinValueAbove para manos en pots inflados. 🟢 (`Services/PostflopDecisionService.cs`)
- **Push/fold mode solo con SPR < 1.0.** `CalculateAllinEV(equity, pot, stack) = equity×(pot+stack) − (1−equity)×stack`; aplicado tras interpolación suave (no buckets). 🟢 (`Services/PostflopDecisionService.cs:1427-1434`)
- **Check-raise con SPR guard.** Si `SPR < 1.5` y `equity < 60%` → skip check-raise, ir a otra rama. Aplicable OOP/IP. 🟢 (`Services/PostflopDecisionService.cs:927-999`)
- **Float exit aborta en bad runout.** Si `heroFloatedFlop` + turn villain check, antes de bet verifica runout: overcard / flush draw completed / straight completed → Check (no float exit). 🟢 (`Services/PostflopDecisionService.cs:1017-1045`)
- **Stackoff planning (S22.4) usa SPR proyectado al river.** `CalculateProjectedRiverSPR(currentSPR, betSize, pot)` adelanta el SPR para decidir si vale la pena meter chips ya. 🟢 (`Services/PostflopDecisionService.cs:1172-1196,1441-1449`)
- **Randomización adaptativa por villainType.** En spots cercanos al threshold (±3 % equity), check con frecuencia: LAG 85 %, LP 80 %, TAG 60 %, TP 55 %, Unknown 70 %. Usa `Random.Shared.NextDouble()`. 🟢 (`Services/PostflopDecisionService.cs:1205-1236`)
- **Bluff catch turn/river ajustado por villainType, runout y blockers.** `bluffCatchAdjustedEquity = baseEquity × villainTypeMultiplier × runoutMultiplier × blockerEffect`. Multipliers: LAG ×0.80, TP ×1.20, brick ×0.85, scare ×1.15, heroBlocksTopCard ×0.90. 🟢 (`Services/PostflopDecisionService.cs:1591-1662`)
- **Pot commitment: SPR < 0.5 + EV(call) > 0 → Call (no Fold).** 🟡 **Anomalía:** este bloque está duplicado en `PostflopDecisionService.cs:772-795` y `:1665-1688` (DUPLICADO). Si las dos ramas evalúan distinto, hay riesgo de inconsistencia. (`Services/PostflopDecisionService.cs:772-795,1665-1688`)
- **Modo simplificado (`RaiseOverLimper` + `IsSimplified=true`) usa 5 bets fijos por equity tier.** No consulta textura, multiway, polarizer ni opponent profile. Es atajo deliberado para una situación bien tipada. 🟢 (`Services/PostflopDecisionService.cs`, `domain.md § 3.6`)
- **Nunca hace fold sin estar en facing-bet.** Si la rama de "no facing-bet" devolvería Fold, fuerza Check (regla dura del motor). 🟢 (`Services/PostflopDecisionService.cs`, `domain.md § 5`)

### Reglas del estado cross-street (`PostflopGameContext`)

- **El context se reasigna incondicionalmente al detectar nueva mano.** No se "limpia" — se crea un nuevo record. Garantía dura para evitar leaks de estado entre manos. 🟢 (`Services/PostflopGameContext.cs`)
- **`TrackHeroStack` ignora aumentos súbitos al rango de 100 BB durante mano activa (auto-rebuy).** Mantiene `_heroStackPreRebuy` para no contaminar P/L de la mano. 🟢 (`Services/PostflopGameContext.cs:94,133-147`)
- **`WithFlopState/WithTurnState` retornan nuevas instancias inmutables.** Acumulan `HeroBet*`, `VillainBet*`, sizes y flags ortogonales (`FloatedFlop`, `TurnCalledWithFlushDanger`). 🟢 (`Services/PostflopGameContext.cs:106-124`)
- **`HeroCheckedAllStreets = !HeroBetFlop && !HeroBetTurn`.** Preconditioner para "river delayed value tras check-check". 🟢 (`Services/PostflopGameContext.cs`)
- **`IsAnyoneAllIn` se computa por calle (`villainStack <= 0`).** Si activo: `foldEquity = 0`, `reverseImpliedPenalty = 0`. 🟢 (`Services/PostflopGameContext.cs`, `Services/PostflopDecisionService.cs`)

### Reglas del Opponent Tracker

- **Es thread-safe vía `ConcurrentDictionary<string, OpponentProfile>`.** Soporta lecturas concurrentes desde múltiples threads del game loop sin lock externo. 🟢 (`Services/OpponentTracker.cs:16`)
- **`PlayerId = alias OCR del jugador`.** Se mapea a `seatNumber` mediante `RegisterSeatAlias(seat, alias)` para reconciliar cambios de posición tras moving blinds. 🟢 (`Services/OpponentTracker.cs:234-248`)
- **Aggression Factor se trackea separado por posición IP/OOP.** 4 contadores (BetIP, RaiseIP, CallIP, BetOOP, RaiseOOP, CallOOP). `GetTypeForPosition(bool villainIsIP)` devuelve el tipo (TAG/LAG/LP/TP/Unknown) específico de la posición. 🟢 (`Services/OpponentTracker.cs`)
- **Sample size granular por stat.** `HasReliableCBetData ≥ 5`, `HasReliableAFData ≥ 10`, `HasReliableFoldData ≥ 8`. 🟢 (`Services/OpponentTracker.cs`)
- **`GetFoldToBetPct` usa sentinel −1 si no hay datos.** Permite a `PostflopDecisionService` distinguir "sin datos" de "fold% bajo" sin ambigüedad. 🟢 (`Services/OpponentTracker.cs:214-221`)
- **`GetAdjustedFoldEquity` modula la base del profile por tipo de villano.** TAG ajuste pequeño, LAG fold equity menor, LP fold equity mayor. 🟢 (`Services/OpponentTracker.cs:191-208`)
- **El tracker NO se persiste en BD.** Vive en memoria entre sesiones de la app y se reinicia al cerrar. 🔴 Hay intención documentada de persistirlo en futuro (`questions.md → Q-FSM-02`). (`Services/OpponentTracker.cs`)

### Reglas de thresholds

- **`ThresholdsRegistry` valida en arranque que existan todos los `(BoardPosition, HandSituation)` requeridos.** Aborta el arranque con excepción si falta alguno. Construye un `Dictionary<ThresholdKey, StreetThresholds>` para lookup O(1). 🟢 (`Services/ThresholdsRegistry.cs:23-32`)
- **Convive con un diccionario `string`-keyed legacy (`"{Street}_{Situation}"`) en `StrategyProfile`.** `ThresholdsRegistry` lee de ese diccionario en arranque y lo migra a `ThresholdKey` tipado. 🟡 La coexistencia es transitoria — se espera eliminar la clave string. (`Services/ThresholdsRegistry.cs`, `code-analysis.md:161`)

### Reglas de telemetría y bankroll

- **`BankrollTrackerService` usa varianza muestral, no varianza teórica.** Aplica fórmula clásica `RoR = ((1−winrate/σ) / (1+winrate/σ))^(bankroll/σ)`. 🟢 (`Services/BankrollTrackerService.cs:177-213`)
- **Trigger automático de subir/bajar de stake NO existe.** El servicio solo calcula RoR; el usuario decide. 🔴 (`questions.md → Q-DOM-04`)
- **`ExploitabilityCalculator` mantiene queue FIFO de hasta 10K decisiones.** `BigBlind=1.0` está hardcoded en el cálculo de leaks; resultado en BB/100 escala mal si el usuario opera en otro stake. 🟡 Anomalía conocida. (`Services/ExploitabilityCalculator.cs:93,322-348`)
- **`AutoCalibrationService.PreviewAndApply` tiene OldValue hardcoded (45/40).** No lee del `StrategyProfile` actual. 🔴 BUG: las recomendaciones que muestra al usuario son incorrectas si el profile activo difiere de los defaults históricos. (`Services/AutoCalibrationService.cs:174-208`)

### Anomalías detectadas (rastreadas, no negadas)

- **`PostflopDecisionService` viola SRP fuerte (1893 LOC, 10+ paths, mixing aleatorio con `Random.Shared` directo en 11 puntos, pot commitment duplicado, `goto skipBluffCatch` explícito).** Candidato a refactor por extracción de paths a clases dedicadas. 🟡 (`Services/PostflopDecisionService.cs:473,539,597,608,634,842,861,871,968,989,1231,1602,1663`)
- **Bloque `if` vacío en `PostflopDecisionService.cs:1198-1203`.** Sin lógica, posible debug residual. 🔴
- **`HandEvaluator` legacy convive con `BitHandEvaluator` sin justificación documentada.** 🟡 (`Algorithms/HandEvaluator.cs`)
- **Interfaces `IMonteCarloSimulator`, `IOutsCalculator`, `IEquityCalculatorService` referencian tipos `nested` de sus implementaciones (`MonteCarloSimulator.EquityResult`, etc.).** Acopla la interfaz a la implementación; impide mocks limpios. 🟡 (`Interfaces/`)
- **`PreflopEquityCalculator` no implementa interfaz** — consumido directamente por `EquityCalculatorService`. 🟡
- **`PreflopAnalyzer` es estática + interface impl trivial.** El interface es `IPreflopAnalyzer` con métodos que delegan a la versión static. 🟡 (`Services/PreflopAnalyzer.cs`)
- **`obj/` versionado con `net8.0/`, `net9.0/`, `net10.0/`.** El csproj solo declara net10.0; los demás son artefactos de migraciones no limpiados. 🟡 (`legacy-mapping.md § obj/`)

---

## Requisitos Funcionales

| ID | Requisito | Prioridad | Critério de Aceite |
|----|-----------|-----------|--------------------|
| RF-01 | `PostflopDecisionService.DetermineAction` retorna acción y sizing en una sola llamada para cualquier `(equity, street, situation, board, position, profiles)`. | Must | Para los 216 casos de la matriz integration test (`OpenScrape.App.Tests`), retorna acción no-nula y sizing > 0 cuando `decision == Bet/Raise`. |
| RF-02 | `MonteCarloSimulator.CalculateEquity` produce equity exacta en river (variance = 0). | Must | Test: dada misma `(heroHand, board)` repetida 100 veces, el `equity` resultante es idéntico bit-a-bit. |
| RF-03 | `MonteCarloSimulator.CalculateEquity` produce equity ±0.5 % en flop (50 K iter) y preflop (30 K iter). | Must | Test: dada `(AhAd vs random opponent, board=∅)`, equity entre 84.5 % y 85.5 %. |
| RF-04 | `BitHandEvaluator.EvaluateHandScore` retorna `HandScore` zero-alloc y comparable O(1). | Must | Benchmark: `EvaluateHandScore(7 cartas)` < 1 µs y 0 allocations en heap. |
| RF-05 | `OutsCalculator.CalculateOuts` aplica inclusión-exclusión + tainted + backdoor + S21.4 overlap. | Must | Test: hero `Ah Kh`, board `Qh 7h 2c`, devuelve 9 flush + 4 straight (BJT) − overlap = 12 outs (no 13). |
| RF-06 | `BoardTextureAnalyzer.Analyze` clasifica board en {Dry, SemiDry, SemiWet, Wet, Paired, Monotone} con `wetnessScore ∈ [0,100]`. | Must | Test: `(Ah Kh Qh)` → `Wet`, score ≥ 60; `(2h 7c Td)` → `Dry`, score < 15. |
| RF-07 | `OpponentTracker` admite lectura/escritura concurrente desde múltiples threads sin race conditions. | Must | Test: 100 threads escribiendo + 100 leyendo durante 5s no producen exceptions ni datos corruptos. |
| RF-08 | `PostflopGameContext` es inmutable; `WithFlopState/WithTurnState/TrackHeroStack` retornan nuevas instancias. | Must | Test: la instancia original mantiene sus campos sin cambios tras llamar a cualquier `With*`. |
| RF-09 | `ThresholdsRegistry` aborta el arranque si falta cualquier `(BoardPosition × HandSituation)` requerido. | Must | Test: si se elimina `Flop_OpenRaise` del JSON, el constructor lanza excepción con nombre de la clave faltante. |
| RF-10 | `BankrollTrackerService.CalculateRiskOfRuin` retorna 0 si no hay datos suficientes (sesiones < N). | Should | Test: con 0 sesiones, devuelve 0; con 1 sesión, devuelve 0; con N+ sesiones, devuelve un float entre 0 y 1. |
| RF-11 | `StrategyBacktester.RunBacktest` reporta divergencias entre la decisión histórica y la del motor actual. | Should | Test: dada una sesión con 100 manos y motor sin cambios, divergences = 0. Con motor modificado, > 0. |
| RF-12 | `EquityCalculatorService.CalculateFullEquity` orquesta preflop (lookup) o postflop (MC + Outs + textura). | Must | Test: con `community = []`, llama `PreflopEquityCalculator`; con `community.Count >= 3`, llama MC + Outs. |
| RF-13 | El módulo NO depende de `OpenScrape.App` ni `OpenScrape.Features`. | Must | `dotnet list reference` sobre `OpenScrape.DecisionMaker.csproj` no incluye App ni Features. |
| RF-14 | El motor postflop NUNCA hace fold sin estar enfrentando una bet. | Must | Test: con `villainBet = 0`, todos los paths retornan Check o Bet, nunca Fold. |
| RF-15 | El facade `IPokerCalculator` (en App) puede orquestar todos los servicios mediante DI. | Must | Test integración: resolver `IPokerCalculator` y llamar `Calculate(DecisionRequest)` sin instanciar nada manualmente. |
| RF-16 | `AutoCalibrationService.PreviewAndApply` 🔴 lee el `StrategyProfile` actual (no hardcoded) — **bug pendiente**. | Could | Test fallaría hoy: `OldValue` debe coincidir con el profile activo. |

---

## Requisitos No Funcionales

| Tipo | Requisito inferido | Evidencia en código | Confianza |
|------|--------------------|---------------------|-----------|
| Performance | MC equity en flop (50 K iter) en < 200 ms en CPU consumer | `Algorithms/MonteCarloSimulator.cs:369-424` (`Parallel.For` con `LocalInit/LocalFinally`, `Interlocked.Add`) | 🟢 |
| Performance | `BitHandEvaluator` zero-alloc para soportar 42 K evaluaciones por turn equity | `Algorithms/BitHandEvaluator.cs:253-408` (`stackalloc Span<int>`, struct `HandScore`) | 🟢 |
| Performance | `ThreadLocal<CardDataOuts[]>` y `ThreadLocal<List<CardDataOuts>>` para deck/buffers en MC | `Algorithms/MonteCarloSimulator.cs` | 🟢 |
| Concurrencia | `OpponentTracker` thread-safe vía `ConcurrentDictionary` | `Services/OpponentTracker.cs:16` | 🟢 |
| Concurrencia | `ExploitabilityCalculator` thread-safe vía `ConcurrentQueue` | `Services/ExploitabilityCalculator.cs` | 🟢 |
| Determinismo | River equity es exacta y reproducible (no depende de seed) | `Algorithms/MonteCarloSimulator.cs:142-237` | 🟢 |
| Robustez | MC marca `IsReliable = false` si rango villano bloqueado > 20 % | `PokerConstants.cs` (`BlockedComboUnreliableThreshold=20.0`) | 🟢 |
| Robustez | `ThresholdsRegistry` fail-fast en arranque con clave faltante | `Services/ThresholdsRegistry.cs:23-32` | 🟢 |
| Robustez | `PostflopGameContext` se reasigna incondicionalmente al detectar nueva mano (no clean) | `Services/PostflopGameContext.cs` + `domain.md § 3.5` | 🟢 |
| Mantenibilidad | 13 interfaces para inversión de dependencias | `Interfaces/` (13 archivos) | 🟢 |
| Mantenibilidad | `PostflopDecisionService` 1893 LOC viola SRP | `Services/PostflopDecisionService.cs` | 🔴 |
| Trazabilidad | Todas las decisiones generan tags en `tbResume` (`[CHECK-RAISE]`, `[BLUFF]`, `[BARREL]`, `[FLOAT-EXIT]`, etc.) | `Services/PostflopDecisionService.cs` (referencias a `decisionTag` en `PostflopDecisionResult`) | 🟢 |

> Inferido a partir del código y los flowcharts. Validar con benchmarks reales de `BenchmarkSuite1/`.

---

## Critérios de Aceitação

```gherkin
Scenario: Equity en river es exacta
  Dado heroHand = "Ah Kh", board = "Qh Jh 7h 2c 3d", community.Count = 5
  Cuando llamo MonteCarloSimulator.CalculateEquity(heroHand, [], board, opponentRange, deadCards)
  Entonces el equity es exactamente igual entre dos llamadas idénticas
    Y SkippedSimulations == 0
    Y IsReliable == true

Scenario: Equity preflop multiway aplica fórmula log2(n)
  Dado heroHand = "AhAd", community = [], numOpponents = 3
  Cuando llamo PreflopEquityCalculator.GetEquity(...)
  Entonces equity = lookupEquity^(1 + log2(3) × 0.35)

Scenario: PostflopDecisionService nunca hace fold sin facing bet
  Dado villainBet == 0, equity = 5%
  Cuando llamo DetermineAction(input)
  Entonces decision != Fold (puede ser Check)

Scenario: Modo simplificado RaiseOverLimper omite textura
  Dado situation = RaiseOverLimper, IsSimplified = true
  Cuando llamo DetermineAction(input)
  Entonces no se invoca BoardTextureAnalyzer.Analyze
    Y el sizing viene de la tabla de 5 bets fijos por equity tier

Scenario: Push/fold mode solo con SPR < 1.0
  Dado spr = 0.8, equity = 65%, allinEV > 0
  Cuando llamo DetermineAction(input)
  Entonces decision == Raise allin O decision == Call allin

Scenario: Check-raise SPR guard
  Dado spr = 1.2, equity = 50%, OOP, board no-Wet/Monotone
  Cuando llamo DetermineAction(input)
  Entonces no se intenta check-raise (ramifica a slow play o probe bet)

Scenario: ThresholdsRegistry fail-fast
  Dado un StrategyProfile sin la clave "Flop_OpenRaise"
  Cuando se construye ThresholdsRegistry
  Entonces lanza excepción con mensaje "Missing threshold: Flop_OpenRaise"
    Y la app aborta el arranque

Scenario: PostflopGameContext.TrackHeroStack ignora auto-rebuy
  Dado heroStack = 50 BB durante mano activa
  Y de repente heroStack = 100 BB
  Cuando llamo TrackHeroStack(newStack=100)
  Entonces _heroStackPreRebuy = 50 (preserved)
    Y la mano sigue contabilizando con el stack original

Scenario: OpponentTracker thread-safe
  Dado 100 threads que escriben y 100 que leen el mismo player
  Cuando ejecutan durante 5 segundos
  Entonces no se producen exceptions
    Y los counters convergen al valor esperado

Scenario: Float exit aborta en bad runout
  Dado heroFloatedFlop = true, turn = villain check
  Y board = "Qh Jh 8c 4h" (4h completa flush draw)
  Cuando llamo DetermineAction(input)
  Entonces decision == Check (no float exit Bet)
```

---

## Prioridade (MoSCoW)

| Requisito | MoSCoW | Justificativa |
|-----------|--------|---------------|
| `PostflopDecisionService.DetermineAction` produce acción + sizing válidos | Must | Único punto de entrada postflop. Sin él, la app no decide. |
| `MonteCarloSimulator` exacto en river/turn, MC en flop/preflop | Must | Sin equity correcta, todas las decisiones son ruido. |
| `BitHandEvaluator` zero-alloc | Must | 42 K evaluaciones por turn × 50 K MC por flop → cualquier alloc dispara GC. |
| `OutsCalculator` con tainted + backdoor + overlap | Must | Las decisiones de semi-bluff y draw-call dependen de outs precisos. |
| `BoardTextureAnalyzer.Analyze` + `AnalyzeBoardChange` | Must | Sizing y range polarizer ramifican por textura. |
| `OpponentTracker` thread-safe | Must | Game loop multithread + UI que lee perfiles. |
| `ThresholdsRegistry` fail-fast | Must | Arranque con thresholds incompletos causaría decisiones erróneas silenciosamente. |
| `PostflopGameContext` inmutable + reset por mano | Must | Cross-street state leak entre manos = bugs catastróficos. |
| Push/fold con `CalculateAllinEV` | Must | Shoves con EV negativo destruyen bankroll. |
| `BankrollTrackerService.CalculateRiskOfRuin` | Should | Informativo; el usuario decide actuar. |
| `ExploitabilityCalculator` (telemetría GTO) | Should | Útil para coaching, no afecta decisiones en vivo. |
| `StrategyBacktester` | Should | Solo se usa al pulsar el botón "Backtest A/B" en `FrmMain`. |
| `AutoCalibrationService.PreviewAndApply` | Could | Bug conocido (OldValue hardcoded) limita su valor actual. |
| `HandEvaluator` legacy | Won't | Reemplazado por `BitHandEvaluator`; candidato a eliminar. |

> Prioridad inferida por frecuencia de invocación, posición en la cadena de dependencias y presencia de tests.

---

## Rastreabilidade de Código

| Archivo | Función / Clase | Cobertura |
|---------|-----------------|-----------|
| `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` | `PostflopDecisionService.DetermineAction` (línea 69) | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs` | `PostflopGameContext` (record + helpers) | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/PreflopAnalyzer.cs` | `IsPreflopAggressor`, `HasRangeAdvantageOnBoard`, `DetectDonkBet` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/BetSizingService.cs` | `CalculateDynamicBetSize` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs` | `Calculate` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/ImpliedOddsCalculator.cs` | `CalculateImpliedOddsFactor`, `CalculateReverseImpliedOdds` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/RangePolarizer.cs` | `GetOptimalRangeType` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/ThresholdsRegistry.cs` | `Get(BoardPosition, HandSituation)` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs` | `RecordPostflopAction`, `RegisterSeatAlias`, `GetTypeForPosition`, `GetAdjustedFoldEquity` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/EquityCalculatorService.cs` | `CalculateFullEquity` | 🟡 (`RecommendedAction` heurística hardcoded) |
| `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs` | `RunBacktest` | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/StrategyAnalyzerService.cs` | `Analyze`, satellite types (`PositionStats` etc.) | 🟢 |
| `src/OpenScrape.DecisionMaker/Services/BankrollTrackerService.cs` | `CalculateRiskOfRuin` (línea 177-213) | 🟡 (N+1 queries, exceptions silenciadas) |
| `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs` | `LeakCategory` enum + 6 DTOs + queue FIFO | 🟡 (`BigBlind=1.0` hardcoded) |
| `src/OpenScrape.DecisionMaker/Services/AutoCalibrationService.cs` | `PreviewAndApply` | 🔴 (bug `OldValue` 45/40 hardcoded) |
| `src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs` | `CalculateEquity` + `ExactEnumeration{Turn,River}` + `RunMonteCarloSimulation` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/BitHandEvaluator.cs` | `EvaluateBestHand`, `EvaluateHandScore`, `FindStraightHigh` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs` | `CalculateOuts`, `CalculateTaintedOuts`, `OutsToEquity` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs` | `Analyze`, `AnalyzeBoardChange`, `AnalyzeInitialBoard`, `ClassifyRiverCard` | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/PreflopEquityCalculator.cs` | `GetEquity` con tabla 169 + multiway adjustment | 🟢 |
| `src/OpenScrape.DecisionMaker/Algorithms/HandEvaluator.cs` (legacy) | `EvaluateBestHand` brute-force | 🟡 (legacy, conviviendo) |
| `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs` | record con 6 required + 36 con defaults | 🟢 |
| `src/OpenScrape.DecisionMaker/DTOs/DecisionRequest.cs` | sealed record (DTO del facade) | 🟢 |
| `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs` | sealed record (output del facade) | 🟢 |
| `src/OpenScrape.DecisionMaker/PokerConstants.cs` | ~25 constantes algorítmicas | 🟢 |
| `src/OpenScrape.DecisionMaker/Interfaces/*.cs` (13) | Contratos para DI | 🟢 / 🟡 (algunos exponen tipos `nested`) |
