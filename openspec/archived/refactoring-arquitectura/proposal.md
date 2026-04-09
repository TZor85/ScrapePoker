# Proposal: Refactoring Arquitectónico

Change ID: refactoring-arquitectura
Fecha: 2026-04-07
Prioridad: Alta
Estimación: ~40 horas (8 fases incrementales)

## Resumen

FrmMain.cs ha crecido a 5,977 líneas con 15+ responsabilidades, 87 campos y 18 dependencias inyectadas. Es un God Object que centraliza game loop, OCR, detección de jugadores, decisiones, BD, overlay y configuración de regiones. Esto hace imposible testear la lógica de negocio del game loop (0% cobertura en App), dificulta el mantenimiento y crea riesgo de regresiones silenciosas.

Adicionalmente, `PostflopDecisionService.DetermineAction()` acepta 36 parámetros, hay 13 servicios sin interfaz en DecisionMaker, `CoordinateScaler` usa estado estático mutable sin thread-safety, y 6 servicios se instancian manualmente fuera de DI.

## Problema

### 1. God Object — FrmMain.cs (5,977 LOC)
- **15 grupos de responsabilidad** identificados: game loop, OCR, detección de posiciones, equity, decisiones, cartas, sesiones/BD, overlay, logging, regiones, historial, calibración, captura background, estilos UI, utilidades
- **87 campos privados** — estado centralizado imposible de razonar
- **76 patrones async** — flujo de control complejo sin separación
- **27 try-catch dispersos** — manejo de errores inconsistente
- **0% cobertura de tests** para la orquestación del game loop

### 2. PostflopDecisionService — 36 parámetros (1,411 LOC)
- `DetermineAction()` con 36 parámetros viola SRP
- La información ya existe parcialmente en `PostflopGameContext` pero no se usa como objeto de entrada
- `HandleFacingBet()` (159 LOC) y `HandleNoBet()` (373 LOC) son métodos masivos

### 3. Servicios sin Interfaz en DecisionMaker (13 servicios)
- `PostflopDecisionService`, `BetSizingService`, `OpponentTracker`, `DangerPenaltyCalculator`, `ImpliedOddsCalculator`, `PreflopAnalyzer`, `RangePolarizer`, `StrategyAnalyzerService`, `ExploitabilityCalculator`, `AutoCalibrationService`, `BankrollTrackerService`, `EquityCalculatorService`, `StrategyBacktester`
- Los algoritmos (`BitHandEvaluator`, `MonteCarloSimulator`, `BoardTextureAnalyzer`, `OutsCalculator`) ya tienen interfaces

### 4. Estado Estático Mutable — CoordinateScaler
- 3 campos `static` mutables sin `volatile`, `lock` ni `Interlocked`
- `Reset()` puede corromper estado mid-operación
- Contamina tests entre ejecuciones

### 5. Instancias Manuales fuera de DI (6 servicios)
- `ColorDetectionService`, `OcrService`, `ImageCropperService`, `SetFlopForceBoardUseCase`, `GetHashImageUseCase`, `GetCropImageUseCase` — creados con `new` en FrmMain
- Imposible sustituir en tests o cambiar implementación

### 6. Singletons con Estado Mutable sin Sincronización
- `OpponentTracker` — `Dictionary<string, OpponentProfile>` sin lock
- `AutoCalibrationService` — `_decisionsSinceLastCalibration` modificado sin protección
- `BankrollTrackerService` — `_currentBankroll` actualizado sin sincronización
- BackgroundWorker accede a estado compartido con el hilo UI

## Solución

Refactoring incremental en 8 fases, cada una independiente y desplegable. Sin cambios funcionales — solo reestructuración.

### Fase 1: Interfaces para DecisionMaker
Extraer interfaces para los 13 servicios sin abstracción. Registrar en DI por interfaz.

### Fase 2: PostflopDecisionInput — Objeto Parámetro
Crear record `PostflopDecisionInput` que encapsule los 36 parámetros de `DetermineAction()`. Mantener overload legacy para compatibilidad temporal con tests.

### Fase 3: Migrar Servicios Manuales a DI
Registrar `ColorDetectionService`, `OcrService`, `ImageCropperService` y los 3 use cases en el contenedor. Inyectar en FrmMain.

### Fase 4: CoordinateScaler → Servicio Inyectable
Convertir de `static class` a servicio con interfaz `ICoordinateScaler`, inyectado como Singleton.

### Fase 5: Extraer GameCoordinator
Mover la orquestación del game loop (ProcessPreflop, ProcessFlop, ProcessTurn, ProcessRiver, HandleNewHand) a un servicio `GameCoordinator` con interfaz `IGameCoordinator`. FrmMain solo delega.

### Fase 6: Extraer ScreenReaderService
Mover OCR, captura de pantalla, detección de color, lectura de stacks/bets a `ScreenReaderService`. FrmMain deja de conocer Tesseract y OpenCV.

### Fase 7: Extraer TableLayoutService
Mover detección de jugadores, posiciones, dealer, alias, fold detection a `TableLayoutService`.

### Fase 8: Thread Safety
Agregar `ConcurrentDictionary` en OpponentTracker, `volatile` en flags compartidos, `SemaphoreSlim` para operaciones async compartidas.

## Capabilities

### Nuevas
- `IGameCoordinator` — Orquestación testeable del game loop
- `IScreenReaderService` — Abstracción de OCR y captura
- `ITableLayoutService` — Detección de jugadores y posiciones
- `ICoordinateScaler` — Escalado de coordenadas inyectable
- `PostflopDecisionInput` — Objeto parámetro tipado para decisiones

### Modificadas
- `PostflopDecisionService` → recibe `PostflopDecisionInput` en vez de 36 params
- `FrmMain` → delegación pura a servicios (objetivo: <1,500 LOC)
- `Program.cs` → registro DI completo (sin `new` manual)
- `OpponentTracker` → thread-safe con `ConcurrentDictionary`

## Impacto

### Archivos Nuevos
- `src/OpenScrape.DecisionMaker/Services/Interfaces/` — 13 interfaces
- `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs`
- `src/OpenScrape.App/Services/GameCoordinator.cs`
- `src/OpenScrape.App/Services/IGameCoordinator.cs`
- `src/OpenScrape.App/Services/ScreenReaderService.cs`
- `src/OpenScrape.App/Services/IScreenReaderService.cs`
- `src/OpenScrape.App/Services/TableLayoutService.cs`
- `src/OpenScrape.App/Services/ITableLayoutService.cs`
- `src/OpenScrape.App/Services/ICoordinateScaler.cs`

### Archivos Modificados
- `src/OpenScrape.App/Forms/FrmMain.cs` — reducción de ~4,500 LOC
- `src/OpenScrape.App/Program.cs` — registro DI ampliado
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` — nuevo overload
- `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs` — ConcurrentDictionary
- `src/OpenScrape.App/Helpers/CoordinateScaler.cs` — de static a instancia
- `OpenScrape.App.Tests/` — tests nuevos para servicios extraídos

## Acceptance Criteria

- [ ] FrmMain.cs < 1,500 LOC
- [ ] PostflopDecisionService.DetermineAction() acepta un solo objeto `PostflopDecisionInput`
- [ ] 0 instancias manuales de servicios (`new ServiceX()`) en FrmMain
- [ ] 0 campos `static` mutables en CoordinateScaler
- [ ] Todos los servicios de DecisionMaker tienen interfaz
- [ ] OpponentTracker usa ConcurrentDictionary
- [ ] Tests existentes (519) siguen pasando sin cambios
- [ ] Nuevos tests para GameCoordinator, ScreenReaderService, TableLayoutService
- [ ] Build sin warnings nuevos
