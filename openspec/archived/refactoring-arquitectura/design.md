# Design: Refactoring Arquitectónico

## Contexto

Aplicación .NET 10.0 WinForms con Clean Architecture de 5 capas. La lógica de dominio y algoritmos están bien separados, pero la capa App (18,484 LOC) concentra orquestación, OCR, detección y UI en un solo formulario. El motor de decisiones tiene excelente cobertura de tests (~90%) pero la orquestación que lo invoca tiene 0%.

### Estado Actual de FrmMain (5,977 LOC)

```
FrmMain.cs
├── 87 campos privados
├── 18 dependencias inyectadas + 6 creadas con new
├── 15 grupos de responsabilidad:
│   ├── A. Game Loop (ProcessPreflop/Flop/Turn/River/HandleNewHand)
│   ├── B. OCR & Extracción (ReadPlayerNameOCR, SetBetValue, SetStackValue)
│   ├── C. Regiones & Coordenadas (LoadRegionTableMap, MoveRegion)
│   ├── D. Detección de Jugadores (SetActivePlayer, SetDealerPlayer, DetectFoldedPlayers)
│   ├── E. Equity & Cálculos (GetPotOddsCalculator, NormalizeBetValue)
│   ├── F. Decisiones (DetermineFlopActionUnified, DetermineTurnAction, DetermineRiverAction)
│   ├── G. Cartas & Board (ObtainCardsPlayerAsync, AnalyzeBoardChange, IsBoardCardVisible)
│   ├── H. Sesiones & BD (LoadTablesAsync, SaveReferenceDimensionsToConfig)
│   ├── I. Overlay & Display (UpdateOverlayWithPotOdds, UpdateUIWithResults)
│   ├── J. Logging & Debug (LogError, AppendLog, PerformEnhancedDetection)
│   ├── K. Configuración de Regiones UI (btnSaveMap_Click, coordinate buttons)
│   ├── L. Captura Background (BackgroundWorker1_DoWork)
│   ├── M. Historial & Stats (LoadSessionsWithStatsAsync, BtnBacktest_Click)
│   ├── N. Análisis & Calibración (BtnExploitability_Click, BtnCalibrate_Click)
│   └── O. Estilos UI (InitializeVisualStyles, Apply*TabStyle)
└── 76 patrones async sin sincronización
```

### Estado Actual de PostflopDecisionService (1,411 LOC)

```
PostflopDecisionService.cs
├── DetermineAction() — 36 parámetros, 289 líneas
├── HandleFacingBet() — 159 líneas
├── HandleNoBet() — 373 líneas
├── 14 métodos auxiliares privados
└── Sin interfaz
```

---

## Goals

1. **Reducir FrmMain a <1,500 LOC** — solo delegación UI a servicios
2. **Hacer testeable la orquestación del game loop** — GameCoordinator con interfaz
3. **Eliminar los 36 parámetros** — PostflopDecisionInput como record
4. **Completar abstracciones** — interfaces para todos los servicios de DecisionMaker
5. **Eliminar estado estático mutable** — CoordinateScaler como servicio DI
6. **Garantizar thread safety** — ConcurrentDictionary, volatile, SemaphoreSlim
7. **Mantener retrocompatibilidad** — 519 tests pasan sin cambios

## Non-Goals

- No se cambia la lógica de decisión (PostflopDecisionService paths permanecen idénticos)
- No se migra de WinForms a otro framework UI
- No se introduce MediatR ni pipeline de mediación
- No se cambia Marten ni la capa de persistencia
- No se reestructuran los algoritmos (MC, HandEvaluator, etc.)
- No se añaden tests de UI automatizados (solo tests unitarios de servicios extraídos)

---

## Decisiones

### D1: Extraer GameCoordinator como servicio (no como clase base)

**Decisión:** Crear `GameCoordinator : IGameCoordinator` como servicio inyectable Scoped, no como clase base de FrmMain.

**Alternativas consideradas:**
- Herencia (`FrmMainBase` con game loop) — acopla lógica a WinForms
- Partial classes — no resuelve testeabilidad, sigue acoplado a Form
- Mediator pattern — sobreingeniería para un solo consumidor

**Rationale:** Composición sobre herencia. GameCoordinator es independiente de UI, testeable con mocks, y FrmMain simplemente llama `await _coordinator.ProcessCurrentStateAsync()`.

### D2: PostflopDecisionInput como record inmutable

**Decisión:** Crear `record PostflopDecisionInput` con los 36 campos. `DetermineAction(PostflopDecisionInput input)` como método principal. Overload legacy `DetermineAction(double equity, ...)` delega al nuevo.

**Alternativas consideradas:**
- Builder pattern — más código sin beneficio real (los campos ya se conocen en el call site)
- Múltiples objetos (EquityContext, BoardContext, VillainContext) — fragmenta el estado innecesariamente
- Eliminar overload legacy — rompe 186+ tests de golpe

**Rationale:** Record es inmutable, tiene equality by value, y deconstruction gratis. El overload legacy permite migración gradual de tests.

### D3: Interfaces en directorio separado dentro de DecisionMaker

**Decisión:** Crear `src/OpenScrape.DecisionMaker/Interfaces/` con las 13 interfaces. No en Domain (las interfaces dependen de tipos de DecisionMaker).

**Alternativas consideradas:**
- Interfaces en Domain — Domain no debe conocer servicios de decisión
- Interfaces junto a la clase — dificulta encontrarlas, mezcla contrato con implementación
- Proyecto separado de contratos — sobreingeniería para este tamaño

**Rationale:** DecisionMaker ya es referenciado por App. Las interfaces junto a sus servicios pero en subdirectorio organizado facilitan navegación.

### D4: CoordinateScaler como Singleton inyectable con interfaz

**Decisión:** Convertir `static class CoordinateScaler` a `class CoordinateScaler : ICoordinateScaler`, registrado como Singleton. Inicialización lazy en primer uso.

**Alternativas consideradas:**
- Mantener static + agregar locks — no resuelve testeabilidad
- Hacer Scoped — las coordenadas son globales por sesión, Singleton es correcto

**Rationale:** Permite mockear en tests, elimina estado estático global, inicialización controlada por DI.

### D5: ScreenReaderService encapsula OCR + captura + color

**Decisión:** Un solo servicio `ScreenReaderService : IScreenReaderService` que agrupa:
- Lectura OCR de texto (nombres, stacks, bets, hand number)
- Captura de pantalla (GetWindowsScreenUseCase)
- Detección de color (ColorDetectionService)
- Preprocesamiento de imagen (PreprocessImageForOCR)

**Alternativas consideradas:**
- Tres servicios separados (OcrReader, ScreenCapture, ColorDetector) — granularidad excesiva, siempre se usan juntos
- Mantener en FrmMain con wrapper delegado — no reduce complejidad real

**Rationale:** Estos servicios son cohesivos (todos operan sobre screenshots). Un servicio con sub-métodos claros es más manejable que tres micro-servicios con dependencias cruzadas.

### D6: TableLayoutService para detección de jugadores y posiciones

**Decisión:** Extraer toda la lógica de detección de jugadores, posiciones, dealer, alias y fold a `TableLayoutService : ITableLayoutService`.

**Alternativas consideradas:**
- Dejar en GameCoordinator — sobrecarga GameCoordinator con detección de bajo nivel
- Separar en PlayerDetectionService + PositionService — demasiado granular, comparten estado de mesa

**Rationale:** La detección de layout es una responsabilidad cohesiva que opera sobre el mismo estado de mesa (PlayerGameState[], dealer position, regiones).

### D7: Thread safety con ConcurrentDictionary + volatile (no locks pesados)

**Decisión:**
- `OpponentTracker._profiles` → `ConcurrentDictionary<string, OpponentProfile>`
- `FrmMain._executeCapture`, `_newHand` → `volatile bool`
- Operaciones BD en GameLoggerService → `SemaphoreSlim(1,1)` para serializar writes

**Alternativas consideradas:**
- `lock` en cada acceso — riesgo de deadlocks con async
- `ReaderWriterLockSlim` — complejidad innecesaria para este patrón de acceso
- Channel<T> para comunicación entre hilos — sobreingeniería

**Rationale:** ConcurrentDictionary es drop-in replacement. `volatile` es suficiente para flags booleanos. SemaphoreSlim es compatible con async/await.

---

## Arquitectura Destino

```
FrmMain.cs (~1,200 LOC)
├── Constructor (DI injection de 8-10 servicios)
├── Event handlers UI (delegación directa)
├── Estilos visuales
└── Configuración de regiones UI

GameCoordinator : IGameCoordinator (~800 LOC)
├── ProcessCurrentStateAsync()
├── ProcessPreflopAsync()
├── ProcessFlopAsync() / ProcessTurnAsync() / ProcessRiverAsync()
├── HandleNewHandAsync()
├── DetermineAction() orchestration
└── Board analysis delegation

ScreenReaderService : IScreenReaderService (~600 LOC)
├── CaptureScreenAsync()
├── ReadPlayerNameAsync()
├── ReadBetValueAsync()
├── ReadStackValueAsync()
├── ReadHandNumberAsync()
├── DetectColorAsync()
└── PreprocessImage()

TableLayoutService : ITableLayoutService (~500 LOC)
├── InitializePlayersAsync()
├── DetectDealerPosition()
├── DetectActivePlayers()
├── DetectFoldedPlayers()
├── DetermineHeroPosition()
├── SetVillainPositions()
└── ValidatePositionAssignments()

CoordinateScaler : ICoordinateScaler (~50 LOC)
├── Initialize()
├── ScaleRegion()
└── Properties (IsInitialized, ReferenceWidth, ReferenceHeight)
```

### Flujo de Datos Refactorizado

```
FrmMain (UI events)
    │
    ▼
GameCoordinator (orquestación)
    ├── ScreenReaderService (lectura de pantalla)
    ├── TableLayoutService (detección de jugadores)
    ├── IPokerCalculator (equity)
    ├── PostflopDecisionService (decisión via PostflopDecisionInput)
    ├── GameLoggerService (persistencia)
    └── GameLoopStateMachine (estado)
    │
    ▼
FrmMain.UpdateUI() / FrmOverlay.Update() (resultado visual)
```

### Diagrama DI

```
Program.cs
├── Singleton
│   ├── ICoordinateScaler → CoordinateScaler
│   ├── IScreenReaderService → ScreenReaderService
│   ├── IPostflopDecisionService → PostflopDecisionService
│   ├── IBetSizingService → BetSizingService
│   ├── IOpponentTracker → OpponentTracker
│   ├── IDangerPenaltyCalculator → DangerPenaltyCalculator
│   ├── IImpliedOddsCalculator → ImpliedOddsCalculator
│   ├── IPreflopAnalyzer → PreflopAnalyzer
│   ├── IRangePolarizer → RangePolarizer
│   ├── IStrategyAnalyzerService → StrategyAnalyzerService
│   ├── IExploitabilityCalculator → ExploitabilityCalculator
│   ├── IAutoCalibrationService → AutoCalibrationService
│   ├── IBankrollTrackerService → BankrollTrackerService
│   ├── IEquityCalculatorService → EquityCalculatorService
│   ├── IStrategyBacktester → StrategyBacktester
│   ├── RegionLookupCache (ya existe)
│   ├── CardCacheService (ya existe)
│   └── GameLoopStateMachine (ya existe)
├── Scoped
│   ├── IGameCoordinator → GameCoordinator
│   ├── ITableLayoutService → TableLayoutService
│   ├── GameLoggerService (ya existe)
│   └── FrmMain
└── Transient
    └── (use cases Features, ya existen)
```

---

## Migración por Fases

### Fase 1 → 2: Sin dependencias entre sí (paralelizables)
### Fase 3 → 4: Sin dependencias entre sí (paralelizables)
### Fase 5 → 6 → 7: Secuenciales (cada una reduce FrmMain)
### Fase 8: Independiente (thread safety)

```
Fase 1 (Interfaces) ──┐
                       ├── Fase 3 (DI manual) ──┐
Fase 2 (Input DTO)  ──┘                         ├── Fase 5 (GameCoordinator) ── Fase 6 (ScreenReader) ── Fase 7 (TableLayout)
                       Fase 4 (CoordScaler) ─────┘
                       Fase 8 (Thread Safety) ── independiente
```

### Rollback

Cada fase es un commit independiente. Rollback = `git revert <fase-commit>`. No hay migraciones de BD ni cambios de schema.

---

## Riesgos

### R1: Regresión en game loop al mover métodos
- **Mitigación:** Cada método se mueve sin cambios internos (copy-paste + delegación). Los 519 tests existentes validan que la lógica no cambia.
- **Trade-off aceptado:** Algunos métodos tendrán firmas temporalmente feas hasta que todas las fases completen.

### R2: Dependencias circulares al extraer GameCoordinator
- **Mitigación:** GameCoordinator no conoce FrmMain. La comunicación UI es via callbacks/events o return values. FrmMain actualiza UI tras recibir resultado.
- **Trade-off aceptado:** Se necesita un DTO `GameLoopResult` para devolver datos a la UI.

### R3: Performance overhead de interfaces + DI en hot path
- **Mitigación:** Las interfaces se resuelven una vez en constructor (no por iteración). El hot path real (MonteCarloSimulator) ya usa interfaces sin overhead medible.
- **Trade-off aceptado:** Ninguno — overhead es despreciable.

### R4: Estado compartido entre GameCoordinator y FrmMain
- **Mitigación:** `PlayerGameState[]` pasa como parámetro o se inyecta como servicio Scoped. GameCoordinator no retiene estado de UI.
- **Trade-off aceptado:** Puede requerir un `IGameStateStore` intermedio si el acoplamiento es excesivo.

---

## Open Questions

1. **FrmOverlay** (560 LOC): refactorizar en esta fase o postergar?
   - Recomendación: Postergar. No es God Object, tiene responsabilidad única (rendering).

2. **PlayerGameState**: debería ser un servicio Scoped inyectable en vez de array en FrmMain?
   - Recomendación: Sí, crear `IPlayerStateManager` que gestione el array. Fase 7.

3. **BackgroundWorker vs Task.Run**: migrar el capture loop?
   - Recomendación: Postergar. BackgroundWorker funciona, el cambio es cosmético.

4. **GameLoggerService**: mover a Features o mantener en App?
   - Recomendación: Mantener en App. Depende de Marten (Infrastructure) y el patrón actual funciona.
