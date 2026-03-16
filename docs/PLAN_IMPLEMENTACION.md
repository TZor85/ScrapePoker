# Plan de Implementacion - ScrapePoker

## Fase 1 — Fundamentos (Prioridad Critica)

> **Estado: COMPLETADA**

### 1.1 Tests reales

- [x] Reemplazar los 8 placeholders con tests que validen HandEvaluator, MonteCarloSimulator, OutsCalculator, BetSizingService y EquityCalculatorService
- [x] Anadir tests de integracion para el flujo Equity -> Decision (DecisionIntegrationTests)
- [x] Prerequisito para todo lo demas — sin tests no se puede refactorizar con confianza

**Archivos de tests:**
- `OpenScrape.App.Tests/HandEvaluatorTests.cs` (15 tests)
- `OpenScrape.App.Tests/MonteCarloSimulatorTests.cs` (6 tests)
- `OpenScrape.App.Tests/OutsCalculatorTests.cs` (6 tests)
- `OpenScrape.App.Tests/BetSizingServiceTests.cs` (9 tests)
- `OpenScrape.App.Tests/EquityCalculatorServiceTests.cs` (7 tests)
- `OpenScrape.App.Tests/GameLoopStateMachineTests.cs` (14 tests)
- `OpenScrape.App.Tests/StrategyProfileTests.cs` (12 tests)
- `OpenScrape.App.Tests/DecisionIntegrationTests.cs` (8 tests)
- `OpenScrape.App.Tests/BoardTextureAnalyzerTests.cs` (19 tests: 12 textura + 7 board change)
- `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs` (32 tests: facing bet, danger cards, blocker, cap)

**Resultado:** 225 tests, todos pasan (acumulado final incluyendo fases posteriores).

### 1.2 Game Logger / Historial de partidas

- [x] Crear entidad `GameRound` en Domain con: mano hero, board, posicion, equity calculada, accion tomada, resultado, pot size, timestamp
- [x] Persistir en Marten (PostgreSQL)
- [x] Registrar cada decision del bot para analisis posterior
- [x] Base necesaria para el genetic algorithm y strategy analyzer

**Archivos creados:**
- `src/OpenScrape.Domain/Entities/GameRound.cs`
- `src/OpenScrape.Domain/ValueObjects/StreetDecision.cs`
- `src/OpenScrape.App/Services/GameLoggerService.cs`
- `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs`
- `src/OpenScrape.Features/GameRound/GameRoundUseCases.cs`

### 1.3 State Machine para el Game Loop

- [x] Extraer a un servicio `GameLoopStateMachine` con estados: WaitingForHand -> HandDetected -> PreflopAction -> FlopDetected -> FlopAction -> TurnDetected -> TurnAction -> RiverDetected -> RiverAction -> HandComplete
- [x] Validacion de transiciones (impide saltos de estado invalidos)
- [x] Inyectado en FrmMain, integrado en deteccion de nueva mano y ProcessTableInfoAsync
- [x] Retry logic para OCR fallido (hasta 2 reintentos con delay 200ms en flop/turn/river)
- [x] Reemplazar booleans `_isPreflop`, `_isFlop`, `_isTurn`, `_isRiver` con propiedades derivadas del state machine (`IsFlop`, `IsTurn`, `IsRiver` consultan `CurrentState`). Modo test usa `ForceState()`.

**Archivo creado:**
- `src/OpenScrape.App/Services/GameLoopStateMachine.cs`

---

## Fase 2 — Motor de Decision Avanzado

> **Estado: COMPLETADA**

### Diagnostico del estado actual

**Bugs criticos encontrados en el analisis:**

1. **heroStack y villainStack siempre son 0** — En todas las llamadas a `_pokerCalculator.Calculate()` (lineas 1643, 2248, 2302 de FrmMain.cs), heroStack=0 y villainStack=0. Esto anula los calculos de SPR en BetSizingService y UnifiedPokerCalculator.

2. **Board texture del turn solo analiza 1 carta** — `AnalyzeTurnBoardTexture()` filtra `Position == BoardPosition.Turn` y obtiene solo la 4ta carta. Deberia analizar las 4 cartas comunitarias completas (flop + turn).

3. **Bluff frequencies inconsistentes** — appsettings.json define 15% (FlopBluffFrequency), pero el codigo usa 2% (linea 658) y 1% (linea 851). Los valores del config se ignoran en turn/river.

4. **ProcessRiverAsync guarda resultado en _turnResult** — Variable con nombre incorrecto, deberia ser _riverResult (linea 2302).

**Inventario de thresholds hardcodeados:**

80+ valores dispersos en 3 archivos:

| Archivo | Cantidad | Ejemplo |
|---------|----------|---------|
| FrmMain.cs (Turn handlers) | ~36 thresholds | `equity < 45`, `equity > 80`, `equity > 55` |
| FrmMain.cs (River handlers) | ~36 thresholds | `equity < 40`, `equity > 75`, `equity > 60` |
| UnifiedPokerCalculator.cs | ~12 thresholds | `baseFoldEquity = 20.0`, `+10.0 IP`, `EVWithFoldEq > EV + 5.0` |
| BetSizingService.cs | ~8 thresholds | `spr > 3.0: +25%`, `isPaired: +15%`, `OOP: -10%` |

**Patron repetitivo en FrmMain.cs (20 metodos con mismo patron):**

Cada HandleXxxYyyAction() sigue esta estructura identica:
```
1. if (equity < LOW_THRESHOLD)
   -> fold (con bluff chance de 1-2%)
2. Switch board texture (Dry/Coordinated/Paired):
   -> determinar bet size string
3. if (equity > HIGH_TIER) -> bet size grande + "(Value)"
   elif (equity > MID_TIER) -> bet size medio + "(Value)"
   elif (equity > LOW_TIER && IP) -> bet size chico + "(Thin Value)"
   else -> "Check/Call"
```

Los 20 metodos son:
- HandleOpenRaiseTurnAction, HandleCallTurnAction, HandleRaiseOverLimperTurnAction
- HandleThreeBetTurnAction, HandleOpenRaiseVs3BetTurnAction, HandleOpenRaiseVs3BetAndCallTurnAction
- HandleFourBetTurnAction, HandleCold4BetTurnAction, HandleSqueezeTurnAction, HandleVsSqueezeTurnAction
- (mismos 10 para River)

---

### 2.0 Bugfixes criticos (prerequisito)

> **Estado: COMPLETADA**

#### 2.0.1 Pasar heroStack real a _pokerCalculator

- [x] En `ProcessFlopAsync`, `ProcessTurnAsync`, `ProcessRiverAsync` — pasar `heroStack: _playerGameState.HeroStack` (4 call sites).

#### 2.0.2 Board texture con todas las cartas comunitarias

- [x] `AnalyzeTurnBoardTexture()` ahora filtra `Position != BoardPosition.Hand` (incluye flop + turn). Count check cambiado de `< 1` a `< 4`.

#### 2.0.3 Usar _riverResult en lugar de _turnResult para river

- [x] Verificado: el codigo ya usaba `_riverResult` correctamente. No se requirio cambio.

#### 2.0.4 Bluff frequencies consistentes

- [x] Reemplazados todos los `0.02` con `_turnBluffFrequency` (14 ocurrencias) y `0.01` con `_turnBluffFrequency * 0.5` (1 ocurrencia) en FrmMain.cs.

---

### 2.1 StrategyProfile — Parametros configurables

> **Estado: COMPLETADA**

**Objetivo:** Centralizar TODOS los thresholds en una unica entidad configurable, eliminando los 80+ valores hardcodeados.

#### Implementacion realizada

**Entidad `StrategyProfile`** (`src/OpenScrape.Domain/Entities/StrategyProfile.cs`):
- Parametros globales: fold equity (base, bonuses, penalties, min/max), bet sizing (SPR multipliers, board texture multipliers, OOP/multi-opponent), bluff frequencies (flop/turn/river), decision adjustments (draw bonus, river penalty, IP bonus, EV threshold).
- `Dictionary<string, StreetThresholds> Thresholds` con clave `"{BoardPosition}_{HandSituation}"` (20 entradas: Turn×10 + River×10).

**Value Object `StreetThresholds`** (`src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`):
- Umbrales de equity: `FoldBelow`, `ThinValueAbove`, `ValueAbove`, `StrongValueAbove`.
- Bet sizes por tier: `StrongValueBetSize`, `ValueBetSize`, `ThinValueBetSize`.
- Bet sizes por board texture: `DryBoardBetSize`, `CoordinatedBoardBetSize`, `PairedBoardBetSize`.
- Comportamiento de bluff: `CanBluff`, `BluffFrequencyMultiplier`, `BluffBetSize`, `BluffCondition` ("None"/"Always"/"OOPOnly"/"IPCoordinatedSmallOnly").
- Control de accion: `LowEquityAction` ("Fold"/"Call"), `ThinValueIPOnly`, `ThinValueOOPFallback` ("CheckFold"/"CheckCall").
- Ajustes de sizing: `ReduceSizeForLargeBet`, `ReduceSizeForOOP`.
- Modo simplificado (RaiseOverLimper): `IsSimplified` con bets fijos IP/OOP.

**Servicio `StrategyProfileService`** (`src/OpenScrape.App/Services/StrategyProfileService.cs`):
- Carga via `IOptions<StrategyProfile>` desde appsettings.json.
- `GetThresholds(BoardPosition, HandSituation)` con fallback conservador si no hay config.
- `GetBluffFrequency(BoardPosition)` para obtener frecuencia por street.

**Configuracion JSON** (`appsettings.json` seccion `StrategyProfile`):
- 20 configs de thresholds (10 Turn + 10 River) con valores exactamente iguales a los que estaban hardcodeados.
- Parametros globales de fold equity, bet sizing, bluff y decision adjustments.

**Integracion en FrmMain**:
- Metodo generico `DeterminePostflopAction(equity, street, situation, boardTexture, isInPosition, betSize)` reemplaza los 20 handlers individuales (~800 lineas → ~80 lineas).
- Helpers: `ShouldBluff()` (evalua condicion de bluff configurable), `DetermineSimplifiedAction()` (RaiseOverLimper IP/OOP), `ReduceBetSize()` (escala Pot→3/4→2/3→1/2→1/3).
- `DetermineTurnAction()` y `DetermineRiverAction()` ahora calculan parametros y delegan al metodo generico.

**Archivos creados:**
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- `src/OpenScrape.Domain/ValueObjects/StreetThresholds.cs`
- `src/OpenScrape.App/Services/StrategyProfileService.cs`
- `OpenScrape.App.Tests/StrategyProfileTests.cs` (12 tests)

**Archivos modificados:**
- `appsettings.json` — seccion StrategyProfile con 20 thresholds + parametros globales
- `src/OpenScrape.App/Program.cs` — registro de `IOptions<StrategyProfile>` y `StrategyProfileService`
- `src/OpenScrape.App/Forms/FrmMain.cs` — inyeccion de servicio, metodo generico, dispatchers simplificados

**Integracion en UnifiedPokerCalculator y BetSizingService:**
- [x] Inyectar `IOptions<StrategyProfile>` en `UnifiedPokerCalculator` — fold equity (base, bonuses, penalties, min/max), draw/river/IP equity adjustments, EV threshold
- [x] Inyectar `IOptions<StrategyProfile>` en `BetSizingService` — SPR multipliers, board texture multipliers, OOP/multi-opponent multipliers
- [x] Agregar `Microsoft.Extensions.Options` como dependencia de `OpenScrape.DecisionMaker.csproj`

**Resultado:** 110 tests, todos pasan.

---

### 2.2 Board Texture Analyzer

> **Estado: COMPLETADA**

**Objetivo:** Extraer el analisis de board texture a un servicio dedicado en DecisionMaker con scoring mas fino que Dry/Coordinated/Paired.

#### Implementacion realizada

**`BoardTextureAnalyzer`** (`src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`):
- `Analyze(List<CardDataOuts>)` y `Analyze(List<int> ranks, List<int> suits)` para flexibilidad.
- **Wetness score** (0-100) basado en: monotone (+35), two-tone (+15), connectivity (+20), flush possibility (+15), straight possibility (+15), broadway heavy (+10), paired (-10), trips (-15).
- **5 categorias**: `Dry` (<15), `SemiDry` (15-35), `SemiWet` (35-60), `Wet` (>=60), `Paired`.
- **Flags detallados**: `IsMonotone`, `IsTwoTone`, `IsRainbow`, `IsPaired`, `IsConnected`, `IsBroadwayHeavy`, `IsLowBoard`, `HasFlushPossibility`, `HasStraightPossibility`.
- **`SimplifiedTexture`**: mapeo retrocompatible a "Dry"/"Coordinated"/"Paired" para integracion con StreetThresholds.

**`BoardTextureResult`** es un record inmutable con todas las propiedades.

**Archivos creados:**
- `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`
- `OpenScrape.App.Tests/BoardTextureAnalyzerTests.cs` (12 tests)

**Archivos modificados:**
- `src/OpenScrape.App/Program.cs` — registrado como Singleton

---

### 2.3 Postflop Decision Service

> **Estado: COMPLETADA**

**Objetivo:** Extraer la logica de decision postflop a un servicio testeable en DecisionMaker, con logica avanzada de facing bet, cartas peligrosas y posicion.

#### Implementacion realizada

**`PostflopDecisionService`** (`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`):
- `DetermineAction(equity, street, situation, boardTexture, isInPosition, villainBetSize, potOdds, totalOuts, previousStreetBet, villainShowedAggression, boardChange, heroBlocksDangerSuit)` — metodo principal.
- **3 paths de decision:**
  - `HandleFacingBet()` — Fold/Call/Raise segun equity ajustada y thresholds
  - `HandleNoBet()` — Check/Bet con sizing por board texture
  - `HandleLowEquity()` — semi-bluff, implied odds, pot odds marginales
- **Facing bet adjustments:** penalty por tamaño de bet (Small+1, Medium+4, Large+8) y villain aggression (+3) sobre FoldBelow.
- **Nunca fold sin facing bet:** siempre Check cuando villainBetSize=NoBet.
- **Semi-bluff solo sin facing bet**: con facing bet + draws → Call por implied odds.
- **Pot odds integration**: equity marginal con pot odds favorables → call en vez de fold.
- **Showdown value**: river sin apuesta con equity marginal → check.
- **Barrel logic**: marca `IsBarrel = true` cuando hay bet continuado en river tras bet en turn.
- **Modo simplificado**: RaiseOverLimper con bets fijos IP/OOP, distingue facing bet vs no bet.

**`PostflopDecisionResult`** record con: `Action`, `Reason`, `IsBluff`, `IsBarrel`.

**`BetSizeCategory`** enum: `NoBet`, `Small`, `Medium`, `Large`.

#### 2.3.1 Deteccion de cartas peligrosas (Board Change Analysis)

**Problema resuelto:** Monte Carlo calcula equity vs rango RANDOM, pero cuando una carta completa un flush/straight, la equity real baja drasticamente (villano que apuesta en board peligroso tiene rango mucho mas fuerte).

**`BoardChangeResult`** record en `BoardTextureAnalyzer.cs`:
- `FlushCompleted`, `FlushDrawAppeared`, `StraightCompleted`, `BoardPaired`, `OvercardAppeared`
- `CompletedFlushSuit` (para detectar blocker effect de hero)
- `DangerLevel` (0-10, acumulativo)

**`AnalyzeBoardChange(previousRanks, previousSuits, newCardRank, newCardSuit)`** en `BoardTextureAnalyzer`:
- Compara board anterior con nueva carta para detectar cambios
- Detecta flush completado (3+ del mismo suit), straight completado (4+ consecutivas), board paired, overcard
- Wheel check (A-2-3-4-5) para straight detection

**`CalculateDangerPenalty(rawEquity, boardChange, heroBlocksDangerSuit, isFacingBet)`** en `PostflopDecisionService`:
- **Penalizacion porcentual** (proporcional a equity, no flat):
  - Flush completado: equity × 25% (`DangerFlushCompletePct`)
  - Straight completado: equity × 18% (`DangerStraightCompletePct`)
- **Penalizacion flat** para cambios menores:
  - Board paired: -5, Overcard: -3, Flush draw: -5
- **Multiplicador facing bet** (`DangerFacingBetMultiplier = 1.4`): villano representa el draw completado
- **Blocker effect** (`DangerHeroBlocksReduction = 0.5`): hero tiene carta del suit peligroso
- **Tope para apostar** (`DangerCompletedDrawNoBetCap = 45`): cuando flush/straight completado y hero no lo tiene, equity se topa para evitar value bet en boards donde solo nos pagan manos mejores

**Arrastre de peligro entre streets:**
- `_lastBoardChange` almacena el BoardChangeResult del turn
- En river, `CombineBoardChanges()` combina peligro del turn con cambio del river
- Si turn completo flush, river hereda ese peligro aunque la carta del river sea safe

**Ejemplo validado (AsQc en Qh3h7s-2h-Tc):**
- Turn 2h: FlushCompleted=True, Penalty=33.0 (94.3×0.25×1.4), EffEquity=61.3 → **Call** (antes: Raise 3x)
- River Tc: Flush arrastrado, cap aplica, EffEquity=45 → **Check/Thin** (antes: Bet Pot)

**Logging en pestaña Logs:**
- `LogError()` ahora escribe a `tbResume` (pestaña Logs de UI) ademas de Console
- Logs detallados `[TURN]`/`[RIVER]` con: Equity, DangerLevel, Penalty, EffEquity, FlushComplete, HeroBlocks, Texture, FacingBet, Situation, Decision, Reason

**Parametros configurables en `StrategyProfile`:**
- `DangerFlushCompletePct` (25.0), `DangerStraightCompletePct` (18.0)
- `DangerBoardPairedPenalty` (5.0), `DangerOvercardPenalty` (3.0), `DangerFlushDrawPenalty` (5.0)
- `DangerFacingBetMultiplier` (1.4), `DangerHeroBlocksReduction` (0.5)
- `DangerCompletedDrawNoBetCap` (45.0)

**Archivos creados:**
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs` (32 tests)

**Archivos modificados:**
- `src/OpenScrape.App/Program.cs` — registrado como Singleton
- `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` — agregado `Microsoft.Extensions.Options`
- `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs` — BoardChangeResult + AnalyzeBoardChange()
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs` — parametros Danger*
- `src/OpenScrape.App/Forms/FrmMain.cs` — inyeccion BoardTextureAnalyzer, AnalyzeBoardChange(), CombineBoardChanges(), logging a tbResume

---

### 2.4 Mejoras de Logica de Juego

> **Estado: COMPLETADA**

**Objetivo:** Corregir bugs y mejorar la precision del motor de decision.

#### 2.4.1 Reescritura de OutsCalculator

- [x] Reescrito con inclusion-exclusion: `flush + straight - overlap` (sin doble conteo)
- [x] `GetStraightCompletingRanks()` — prueba cada rank 2-14 para ver si crea escalera
- [x] `HasFiveCardStraight()` — detecta escaleras regulares + rueda (A-2-3-4-5)
- [x] `GetFlushDrawSuit()` — null si ya hay flush (5+), solo retorna suit con exactamente 4 cartas
- [x] Overcards como outs: solo sin flush draw, OESD ni mano hecha, sin doble conteo con straight outs
- [x] `HasMadeFlush()` — verifica flush completo para no contar overcards innecesarias

**Tests:** 17 tests (13 base + 4 overcards)

#### 2.4.2 Implied Odds Dinamico

- [x] Reemplaza constante fija 0.75 con calculo SPR-based
- [x] `CalculateImpliedOddsFactor()` — factor entre 0.5 y 1.0 segun SPR, posicion, street, tipo de draw
- [x] Interpolacion lineal entre SPR shallow y deep
- [x] River siempre retorna 1.0 (no hay implied odds en ultima calle)
- [x] Parametros configurables en `StrategyProfile`: ImpliedOddsSPR*, ImpliedOddsIP*, ImpliedOddsFlush*, ImpliedOddsFlop/Turn*
- [x] Aplicado en HandleFacingBet, HandleLowEquity

**Tests:** 9 tests nuevos de implied odds

#### 2.4.3 Villain Range Filtering (Monte Carlo)

- [x] `VillainRange` (Domain/ValueObjects): 12 rangos predefinidos por HandSituation
- [x] `ExpandHandNotation()` — convierte "AKs", "QQ", "JTo" a combos concretos de cartas
- [x] `GetForSituation()` — retorna rango segun accion preflop (ej: 3Bettor ~8%, Limper ~40%)
- [x] `MonteCarloSimulator.CalculateEquity()` acepta `VillainRange?` opcional
- [x] `BuildVillainCombos()` — pre-expande rango filtrando cartas bloqueadas
- [x] `TryDrawFromRange()` — seleccion ponderada por frecuencia con fallback aleatorio
- [x] `UnifiedPokerCalculator` pasa handSituation → VillainRange al Monte Carlo

**Tests:** 12 tests (4 MonteCarlo rango + 8 VillainRange)

#### 2.4.4 Opponent Modeling Basico

- [x] `OpponentProfile` (Domain/Entities): VPIP, PFR, 3Bet%, AggressionFactor, CBet%, FoldToCBet%, Type
- [x] `OpponentType`: TAG, LAG, TP (nit), LP (fish), Unknown
- [x] `OpponentTracker` (DecisionMaker/Services): acumula stats por sesion, case-insensitive
- [x] `GetAdjustedFoldEquity()`: LP×1.25, TP×1.10, TAG×0.85, LAG×0.70
- [x] `IsReliable` >= 20 manos para considerar stats fiables
- [x] Registrado como Singleton en DI

**Tests:** 18 tests

#### 2.4.5 Multi-way Pot Adjustments

- [x] `numOpponents` parametro en `DetermineAction()`
- [x] FoldBelow +4 por oponente extra, ThinValueAbove +3 por extra
- [x] No bluff ni semi-bluff en multiway (>= 2 oponentes)
- [x] Integrado en `DetermineTurnAction()` y `DetermineRiverAction()` de FrmMain
- [x] Logging incluye numero de oponentes

**Tests:** 5 tests multiway

**Archivos creados:**
- `src/OpenScrape.Domain/ValueObjects/VillainRange.cs`
- `src/OpenScrape.Domain/Entities/OpponentProfile.cs`
- `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs`
- `OpenScrape.App.Tests/OpponentTrackerTests.cs`

**Archivos modificados:**
- `src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs` — reescritura + overcards
- `src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs` — VillainRange opcional
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` — implied odds + multiway
- `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` — pasa VillainRange
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs` — 10 params ImpliedOdds*
- `src/OpenScrape.App/Forms/FrmMain.cs` — numOpponents en Turn/River
- `src/OpenScrape.App/Program.cs` — registro OpponentTracker

**Resultado:** 225 tests, todos pasan.

---

### Orden de implementacion Fase 2

```
2.0 Bugfixes criticos ✅ COMPLETADA
  |
  +---> 2.0.1 heroStack real ✅
  +---> 2.0.2 Board texture fix ✅
  +---> 2.0.3 _riverResult (ya estaba OK) ✅
  +---> 2.0.4 Bluff frequencies ✅
  |
  v
2.1 StrategyProfile ✅ COMPLETADA
  |  Entidad + JSON + service + metodo generico en FrmMain
  |  80+ hardcoded values externalizados a appsettings.json
  |  20 handlers → 1 metodo generico (~800 lineas eliminadas)
  |  Inyectado en UnifiedPokerCalculator y BetSizingService
  |  12 tests de configuracion
  |
  v
2.2 BoardTextureAnalyzer ✅ COMPLETADA
  |  Servicio dedicado en DecisionMaker
  |  Wetness score (0-100) + 5 categorias
  |  Flags: monotone, two-tone, rainbow, connected, broadway, low board
  |  BoardChangeResult: flush/straight/paired/overcard detection
  |  AnalyzeBoardChange(): compara board previo con nueva carta
  |  19 tests unitarios
  |
  v
2.3 PostflopDecisionService ✅ COMPLETADA
     3 paths: HandleFacingBet / HandleNoBet / HandleLowEquity
     Facing bet adjustments: penalty por tamaño + villain aggression
     Danger card penalty: porcentual (flush -25%, straight -18%) + facingBet ×1.4
     Blocker effect: hero con suit peligroso reduce penalty ×0.5
     NoBet cap: equity topada a 45 en boards con draw completado sin tenerlo
     Arrastre de peligro turn→river via CombineBoardChanges()
     Logging a pestaña Logs (tbResume) con Equity/Penalty/EffEquity/Decision
     Semi-bluff con outs, pot odds, showdown value, barrel logic
     32 tests unitarios
```

### Verificacion Fase 2

| Paso | Criterio de aceptacion | Estado |
|------|----------------------|--------|
| 2.0 | `dotnet test` pasa, heroStack > 0 en logs, board texture correcta | ✅ |
| 2.1 | Todos los thresholds cargados desde JSON, inyectados en UnifiedPokerCalculator y BetSizingService | ✅ |
| 2.2 | BoardTextureAnalyzer: textura + board change (flush/straight/paired/overcard) | ✅ |
| 2.3 | PostflopDecisionService: facing bet, danger cards, blocker, cap, logging | ✅ |
| Final | `dotnet build` sin errores, `dotnet test` todos pasan | ✅ 225 tests |

### Riesgos y mitigacion

| Riesgo | Probabilidad | Mitigacion |
|--------|-------------|-----------|
| Regresion en decisiones al migrar thresholds | Alta | Tests snapshot: ejecutar con thresholds actuales, guardar output, comparar despues de migrar |
| StrategyProfile demasiado grande para appsettings | Baja | Archivo JSON separado `Data/StrategyProfile.json` si crece mucho |
| Board texture wetness score no calibrado | Media | Empezar con mapping directo a Dry/Coordinated/Paired, refinar despues con datos |
| PostflopDecisionService cambia comportamiento | Media | Implementar con feature flag: si flag off, usa handlers viejos; si on, usa servicio nuevo |

---

## Fase 3 — Automatizacion y Ejecucion

> **Estado: PENDIENTE**

### 3.1 Action Executor (Mouse Automation)

**Objetivo:** Ejecutar las acciones recomendadas automaticamente en la mesa.

**Problema actual:** El bot solo recomienda acciones en el overlay. El usuario debe ejecutarlas manualmente.

**Implementacion:**
- Crear `ActionExecutorService` en OpenScrape.App/Services
- Detectar botones de accion (Fold/Call/Raise) por template matching o color/posicion en las regiones del tableMap
- Mover mouse con curvas de Bezier (no teleport)
- Randomizar timing (delay entre 0.5-2s, distribucion gaussiana)
- Input del bet size en el campo de raise
- Win32 API: `SetCursorPos`, `mouse_event` (ya se usa `CaptureWindowsHelper`)

**Archivos a crear:**
- `src/OpenScrape.App/Services/ActionExecutorService.cs`
- `src/OpenScrape.App/Services/MouseHumanizer.cs`

**Archivos a modificar:**
- `src/OpenScrape.App/Forms/FrmMain.cs` — llamar a ActionExecutor despues de cada decision
- `src/OpenScrape.App/Data/tableMap.json` — agregar regiones de botones Fold/Call/Raise

**Dependencias:** Fase 2 completada (decisiones confiables antes de automatizar).

### 3.2 Verificacion pre-accion

**Objetivo:** Confirmar que el estado de mesa no cambio antes de ejecutar una accion.

**Implementacion:**
- Antes de ejecutar: re-capturar screenshot y comparar con el estado que genero la decision
- Detectar popups, captchas, sit-out
- Validar que los botones siguen visibles
- Si falla -> re-capturar y re-evaluar
- Integrar con `GameLoopStateMachine` (permanecer en estado actual si validacion falla)

**Archivos a crear:**
- `src/OpenScrape.App/Services/PreActionValidator.cs`

**Archivos a modificar:**
- `src/OpenScrape.App/Services/ActionExecutorService.cs` — validar antes de ejecutar

**Dependencias:** 3.1 (Action Executor).

---

## Fase 4 — Analisis y Optimizacion

> **Estado: EN PROGRESO (4.1 completada)**

### 4.1 Strategy Analyzer + Persistencia de Datos

> **Estado: COMPLETADA**

**Objetivo:** Servicio de analisis de rendimiento con metricas completas y persistencia de datos de juego.

#### 4.1.1 StrategyAnalyzerService

**`StrategyAnalyzerService`** (`src/OpenScrape.DecisionMaker/Services/StrategyAnalyzerService.cs`):
- `Analyze(List<GameRound>, bigBlind)` — metodo principal de analisis
- **Metricas generales:** TotalHands, HandsWon/Lost/Push/Unknown, WinRate, TotalProfit, BiggestWin/Loss, BBPer100
- **Stats por posicion** (`PositionStats`): hands, won, lost, profit, winrate, avgProfitPerHand por cada `TablePosition`
- **Stats por street** (`StreetStats`): totalDecisions, bets/calls/raises/folds/checks, avgEquity, avgPotOdds por cada `BoardPosition`
- **Stats por situacion** (`SituationStats`): hands, won, profit, winrate por cada `HandSituation`
- **Sesiones** (`SessionSummary`): sessionId, startTime, endTime, hands, profit, BBPer100
- **Equity vs Outcome** (`EquityVsOutcome`): equity predicha, resultado real, profit, accion — para evaluar precision del modelo
- `CalculateEquityAccuracy()` — agrupa por buckets de 10%, compara winrate real vs equity predicha, accuracy = 100 - promedio diff absoluta
- `GenerateReport()` — resumen formateado para UI/logs con tablas por posicion, street, situacion y sesiones recientes

#### 4.1.2 Persistencia completa de GameRound

**Campos agregados a `GameRound`:**
- `HeroStackEnd` — stack al finalizar la mano
- `Result` (`HandResult` enum: Unknown, Won, Lost, Push) — calculado automaticamente al cerrar la mano
- `Situation` (`HandSituation`) — situacion preflop detectada
- `SessionId` — identificador de sesion para agrupacion

**Metodos en `GameLoggerService`:**
- `EndRound(heroStackEnd)` — calcula HandResult por diferencia de stacks (Won si diff > 0, Lost si diff < 0, Push si diff == 0)
- `UpdateSituation(HandSituation)` — persiste situacion en GameRound
- `UpdateSessionId(string)` — persiste session ID

**Integracion en `FrmMain`:**
- `HandleNewHandAsync()` llama a `EndRound(heroStack)` + `SaveRoundAsync()` antes de iniciar nueva mano
- `HandleNewHandAsync()` llama a `UpdateSessionId(_session)` al iniciar nueva mano
- `ProcessFlopAsync` llama a `UpdateBoard(flopCards)` y `LogStreetDecision()`
- `DetermineTurnAction()` llama a `LogStreetDecision()`, `UpdateBoard(turnCard)`, `UpdateSituation()`
- `DetermineRiverAction()` llama a `LogStreetDecision()`, `UpdateBoard(riverCard)`

**No se necesitan nuevas regiones de OCR** — todos los datos necesarios (heroStack, position, cartas, acciones, pot) ya se detectan con las regiones existentes del tableMap.

**Archivos creados:**
- `src/OpenScrape.DecisionMaker/Services/StrategyAnalyzerService.cs`
- `OpenScrape.App.Tests/StrategyAnalyzerServiceTests.cs` (12 tests)

**Archivos modificados:**
- `src/OpenScrape.Domain/Entities/GameRound.cs` — HeroStackEnd, Result, Situation, SessionId
- `src/OpenScrape.App/Services/GameLoggerService.cs` — EndRound(), UpdateSituation(), UpdateSessionId()
- `src/OpenScrape.App/Forms/FrmMain.cs` — llamadas a persistencia en flujo de juego
- `src/OpenScrape.App/Program.cs` — registro StrategyAnalyzerService como Singleton

**Pendiente para 4.1:** Form `FrmAnalytics` con graficos (LiveCharts o WinForms chart controls).

### 4.2 Genetic Algorithm

**Objetivo:** Optimizacion automatica de los parametros de estrategia.

**Implementacion:**
- Fitness function: BB/100 en las ultimas N manos
- Poblacion: variantes del `StrategyProfile` con parametros mutados
- Mutacion: ajustar parametros +/-5-15% basado en correlaciones estadisticas
- Seleccion: "if call_wins > call_losses x 2.0 -> mas agresivo"
- Versionado: timestamp + nombre para tracking evolutivo
- Requiere minimo 2000-5000 manos de datos
- Implementar como servicio background que analiza periodicamente

**Archivos a crear:**
- `src/OpenScrape.DecisionMaker/Optimization/GeneticOptimizer.cs`
- `src/OpenScrape.DecisionMaker/Optimization/StrategyMutator.cs`
- `src/OpenScrape.DecisionMaker/Optimization/FitnessEvaluator.cs`
- `src/OpenScrape.Domain/Entities/StrategyGeneration.cs`

**Dependencias:** 2.1 (parametros configurables) + 4.1 (strategy analyzer) + suficientes datos.

---

## Fase 5 — Reconocimiento Avanzado (Opcional)

> **Estado: PENDIENTE**

### 5.1 Template Matching para cartas

**Objetivo:** Complementar OCR con matching visual de templates por carta.

**Justificacion:** Mas robusto que OCR puro para cartas (imagenes conocidas y finitas: 52 cartas).

**Implementacion:**
- Almacenar templates (52 imagenes de cartas) por plataforma en Marten o filesystem
- Comparar crop de carta con templates usando OpenCvSharp `MatchTemplate`
- Fallback a OCR si template matching tiene confianza baja

**Archivos a crear:**
- `src/OpenScrape.App/Services/TemplateMatchingService.cs`
- `src/OpenScrape.App/Data/CardTemplates/` (52 imagenes por plataforma)

**Dependencias:** Ninguna.

### 5.2 CNN para card recognition

**Objetivo:** Entrenar modelo ML para clasificacion de cartas cuando template matching y OCR fallan.

**Implementacion:**
- ML.NET ya esta parcialmente integrado
- Entrenar con imagenes de cartas especificas de cada mesa
- Clasificacion multiclase (52 clases)
- Fallback cuando template matching y OCR fallan

**Archivos a crear:**
- `src/OpenScrape.App/Services/CardClassifierService.cs`
- Dataset de entrenamiento por plataforma

**Dependencias:** 5.1 (template matching como baseline).

### 5.3 Multi-plataforma

**Objetivo:** Abstraer table scraper con interfaz `ITableScraper` para soportar multiples plataformas de poker.

**Implementacion:**
- Interfaz `ITableScraper` con metodos: `CaptureTable()`, `DetectCards()`, `DetectButtons()`, `GetPlayerInfo()`
- Implementaciones por plataforma (cada una con sus coordenadas, themes, templates)
- El `tableMap.json` actual ya va en esta direccion
- Factory pattern para seleccionar implementacion segun plataforma detectada

**Archivos a crear:**
- `src/OpenScrape.App/Services/ITableScraper.cs`
- `src/OpenScrape.App/Services/Platform/` (una implementacion por plataforma)

**Dependencias:** 5.1 (template matching).

---

## Orden de Implementacion Recomendado

```
Fase 1 ✅ COMPLETADA (tests reales, game logger, state machine)
  |
  v
Fase 2.0 ✅ COMPLETADA (bugfixes criticos)
  |
  v
Fase 2.1 ✅ COMPLETADA (StrategyProfile configurable)
  |
  v
Fase 2.2 ✅ COMPLETADA (BoardTextureAnalyzer)
  |
  v
Fase 2.3 ✅ COMPLETADA (PostflopDecisionService)
  |
  v
Fase 2.4 ✅ COMPLETADA (OutsCalculator, VillainRange, OpponentModeling, Multi-way)
  |
  v
Fase 4.1 ✅ COMPLETADA (StrategyAnalyzerService + persistencia)
  |
  v
Fase 3.1 -> 3.2 (aplazada — automatizacion)
  |
  v
Fase 4.2 (requiere 4.1 + datos suficientes ~2000+ manos)
  |
  v
Fase 5 (opcional, en cualquier momento)
```

## Datos Perdidos Actualmente (a resolver en fases futuras)

| Dato | Se detecta | Se persiste | Necesario para |
|------|-----------|-------------|----------------|
| Acciones de villanos | Si | No | Opponent modeling (OpponentTracker en memoria, no persistido) |
| Stacks de villanos | Si | No | SPR analysis |
| Posiciones de villanos | Si | No | Position-based stats |
| Resultado de la mano | Si | **Si** ✅ | Winrate, fitness (HandResult calculado en EndRound) |
| Fold rates por villano | No | No | Fold equity dinamica |
| Bet sizing de villanos | Si (Small/Med/Large) | No | Opponent profiling |
| Session aggregates | **Si** ✅ | **Si** ✅ | BB/hour, ROI (SessionId en GameRound, SessionSummary en Analyzer) |

## Verificacion por Fase

| Fase | Criterio de aceptacion | Estado |
|------|----------------------|--------|
| 1 | Tests reales, game logger, state machine, retry OCR, booleans reemplazados | ✅ |
| 2.0 | Bugfixes: heroStack real, board texture correcta, bluff freq consistente | ✅ |
| 2.1 | StrategyProfile cargado desde JSON, inyectado en UnifiedPokerCalculator y BetSizingService, 20 handlers → 1 generico | ✅ |
| 2.2 | BoardTextureAnalyzer: wetness score, 5 categorias, flags detallados, retrocompatible | ✅ |
| 2.3 | PostflopDecisionService: semi-bluff, pot odds, showdown value, barrel logic | ✅ |
| 2.4 | OutsCalculator, VillainRange, OpponentModeling, Multi-way | ✅ |
| Final Fase 2 | `dotnet build` sin errores, `dotnet test` → 225 tests pasan | ✅ |
| 4.1 | StrategyAnalyzerService + persistencia completa de GameRound | ✅ |
| 3.1 | Bot ejecuta acciones automaticamente en mesa de prueba | Aplazada |
| 3.2 | Validacion pre-accion detecta cambios de estado | Aplazada |
| 4.2 | Genetic optimizer produce generacion con mejor fitness | Pendiente |
| 5.1 | Template matching detecta 52 cartas con >95% accuracy | Pendiente |
