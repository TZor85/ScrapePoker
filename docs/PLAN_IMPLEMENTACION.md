# Plan de Implementacion - ScrapePoker

## Fase 1 — Fundamentos (Prioridad Critica)

> **Estado: COMPLETADA**

### 1.1 Tests reales

- [x] Reemplazar los 8 placeholders con tests que validen HandEvaluator, MonteCarloSimulator, OutsCalculator, BetSizingService y EquityCalculatorService
- [ ] Anadir tests de integracion para el flujo OCR -> Decision
- [x] Prerequisito para todo lo demas — sin tests no se puede refactorizar con confianza

**Archivos creados:**
- `OpenScrape.App.Tests/HandEvaluatorTests.cs` (15 tests)
- `OpenScrape.App.Tests/MonteCarloSimulatorTests.cs` (6 tests)
- `OpenScrape.App.Tests/OutsCalculatorTests.cs` (6 tests)
- `OpenScrape.App.Tests/BetSizingServiceTests.cs` (9 tests)
- `OpenScrape.App.Tests/EquityCalculatorServiceTests.cs` (7 tests)
- `OpenScrape.App.Tests/GameLoopStateMachineTests.cs` (9 tests)

**Resultado:** 58 tests, todos pasan.

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
- [ ] Retry logic para OCR fallido
- [ ] Reemplazar completamente los booleans `_isPreflop`, `_isFlop`, `_isTurn`, `_isRiver` con CurrentState

**Archivo creado:**
- `src/OpenScrape.App/Services/GameLoopStateMachine.cs`

---

## Fase 2 — Motor de Decision Avanzado

> **Estado: PENDIENTE**

### 2.1 Equity Curves configurables

**Objetivo:** Definir umbrales minimos de equity por tamano de pot en cada street, en lugar de thresholds fijos.

**Problema actual:** ~40 thresholds hardcodeados dispersos por FrmMain.cs (ej: `if (equity < 45)` en turn, `if (equity > 80)` para value bet). No son configurables ni optimizables.

**Implementacion:**
- Crear value object `EquityCurve`: lista de puntos `(potSizeBB, minEquity)` con interpolacion lineal
- Una curva por street (Preflop, Flop, Turn, River) x accion (Call, Bet, Raise)
- Almacenar en JSON configurable en `src/OpenScrape.App/Data/EquityCurves.json`
- Servicio `EquityCurveService` que dado un potSize y street retorna el minEquity interpolado

**Archivos a crear:**
- `src/OpenScrape.Domain/ValueObjects/EquityCurve.cs`
- `src/OpenScrape.App/Data/EquityCurves.json`
- `src/OpenScrape.App/Services/EquityCurveService.cs`

**Archivos a modificar:**
- `src/OpenScrape.App/Forms/FrmMain.cs` — reemplazar thresholds hardcodeados con llamadas a EquityCurveService

**Dependencias:** Ninguna (puede empezar inmediatamente).

### 2.2 Parametros de estrategia expandidos

**Objetivo:** Evolucionar de los JSON actuales (solo rangos preflop) a un sistema con ~40-80 parametros configurables.

**Problema actual:** `appsettings.json` solo tiene `FlopBluffFrequency` y `TurnBluffFrequency`. Todo lo demas esta hardcodeado: fold equity base (20%), multiplicadores SPR, ajustes por posicion, c-bet frequency, etc.

**Parametros a externalizar:**
- `FlopCBetFrequency`, `TurnBarrelEquity`, `RiverBluffMaxEquity`
- `CheckRaiseThreshold`, `DonkBetFrequency`
- Multiplicadores por posicion (IP/OOP)
- Fold equity: base, position bonus, 3-bet penalty
- Bet sizing: default sizes, high/low equity multipliers

**Implementacion:**
- Crear entidad `StrategyProfile` en Domain con todos los parametros
- Cargar desde `appsettings.json` seccion `StrategyProfile`
- Registrar como `IOptions<StrategyProfile>` en DI
- Persistir perfiles en Marten para versionado

**Archivos a crear:**
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs`
- `src/OpenScrape.Features/StrategyProfile/` (CQRS queries)

**Archivos a modificar:**
- `appsettings.json` — agregar seccion completa de parametros
- `src/OpenScrape.App/Program.cs` — registrar IOptions
- `src/OpenScrape.App/Forms/FrmMain.cs` — reemplazar constantes hardcodeadas con parametros inyectados

**Dependencias:** Ninguna (puede empezar en paralelo con 2.1).

### 2.3 Logica postflop mejorada (Turn/River)

**Objetivo:** Completar Turn y River con logica de decision mas sofisticada.

**Problema actual:** Turn y River usan equity-only con thresholds simples. No hay:
- Continuation bet logic (2nd/3rd barrel)
- Showdown value vs bluff decisions en river
- Board texture analysis profundo (monotone, paired, connected)
- Pot odds integrados en la decision (solo en overlay)

**Implementacion:**
- Crear `TurnDecisionService` y `RiverDecisionService` en DecisionMaker
- Board texture scoring: monotone (3 suited), paired, connected, high-card-heavy
- Barrel logic: si hero c-bet en flop y equity se mantuvo, 2nd barrel; si board cambio drasticamente, check-back
- River: evaluar showdown value (mano tiene equity suficiente para check-call pero no para bet?)
- Integrar pot odds directamente en la decision (no solo mostrar en overlay)

**Archivos a crear:**
- `src/OpenScrape.DecisionMaker/Services/TurnDecisionService.cs`
- `src/OpenScrape.DecisionMaker/Services/RiverDecisionService.cs`
- `src/OpenScrape.DecisionMaker/Algorithms/BoardTextureAnalyzer.cs`

**Archivos a modificar:**
- `src/OpenScrape.App/Forms/FrmMain.cs` — delegar logica de turn/river a los nuevos servicios

**Dependencias:** 2.1 (equity curves) y 2.2 (parametros configurables).

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

> **Estado: PENDIENTE**

### 4.1 Strategy Analyzer

**Objetivo:** Dashboard con metricas de rendimiento por estrategia.

**Metricas:**
- BB/100 manos (winrate principal)
- Win/loss por accion (fold/call/bet) por street
- Equity vs Outcome scatter plot (la equity predijo bien?)
- Funds timeline (evolucion del stack)
- ROI por posicion (BTN, CO, SB, BB, etc.)
- Equity realizada vs equity esperada

**Implementacion:**
- Crear `StrategyAnalyzerService` que consulta `GameRound`s de Marten
- Agregar metricas por periodo (sesion, dia, semana)
- Form `FrmAnalytics` con graficos (WinForms chart controls o libreria LiveCharts)

**Archivos a crear:**
- `src/OpenScrape.App/Services/StrategyAnalyzerService.cs`
- `src/OpenScrape.App/Forms/FrmAnalytics.cs`
- `src/OpenScrape.Features/GameRound/GetGameRoundStats.cs`

**Dependencias:** 1.2 (Game Logger con datos suficientes, minimo ~500 manos).

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

**Dependencias:** 2.2 (parametros configurables) + 4.1 (strategy analyzer) + suficientes datos.

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
Fase 1 (COMPLETADA)
  |
  v
Fase 2.1 + 2.2 (en paralelo)
  |
  v
Fase 2.3 (depende de 2.1 y 2.2)
  |
  v
Fase 3.1 -> 3.2 (secuencial)
  |
  v
Fase 4.1 (requiere datos de Game Logger)
  |
  v
Fase 4.2 (requiere 4.1 + 2.2 + datos suficientes)
  |
  v
Fase 5 (opcional, en cualquier momento)
```

## Datos Perdidos Actualmente (a resolver en Fase 2)

| Dato | Se detecta | Se persiste | Necesario para |
|------|-----------|-------------|----------------|
| Acciones de villanos | Si | No | Opponent modeling |
| Stacks de villanos | Si | No | SPR analysis |
| Posiciones de villanos | Si | No | Position-based stats |
| Resultado de la mano | No | No | Winrate, fitness |
| Fold rates por villano | No | No | Fold equity dinamica |
| Bet sizing de villanos | Si (Small/Med/Large) | No | Opponent profiling |
| Session aggregates | No | No | BB/hour, ROI |

## Verificacion por Fase

| Fase | Criterio de aceptacion |
|------|----------------------|
| 1 | `dotnet test` -> 58 tests pasan, `dotnet build` sin errores |
| 2.1 | Thresholds cargados desde JSON, interpolacion funcional |
| 2.2 | StrategyProfile persistido y cargado, parametros inyectados |
| 2.3 | Turn/River usan servicios dedicados, barrel logic funcional |
| 3.1 | Bot ejecuta acciones automaticamente en mesa de prueba |
| 3.2 | Validacion pre-accion detecta cambios de estado |
| 4.1 | Dashboard muestra BB/100, equity vs outcome, timeline |
| 4.2 | Genetic optimizer produce generacion con mejor fitness |
| 5.1 | Template matching detecta 52 cartas con >95% accuracy |
