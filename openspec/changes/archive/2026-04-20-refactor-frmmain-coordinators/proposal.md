## Why

Tras el refactoring arquitectónico en 8 fases (`refactoring-arquitectura`), `FrmMain.cs` sigue siendo una God Class de **4,244 LOC** con el loop de captura, la coordinación entre `GameCoordinator`, `ScreenReaderService` y `TableLayoutService`, la sincronización con `FrmOverlay`/tabs, y la persistencia vía `GameLoggerService`. El constructor inyecta **27 dependencias** —muchas de ellas deberían estar encapsuladas tras facades— y los `event handlers` (p.ej. `btnCapture_Click`, `ProcessPostFlopAsync`) concentran lógica de negocio no testeable. La consecuencia es que cualquier cambio en el pipeline de decisión requiere tocar UI, y no existe cobertura de tests sobre el loop completo.

Este cambio extrae el loop de juego y la sincronización UI fuera de `FrmMain`, reduce las dependencias del form a ≤10 mediante un facade, y habilita tests de integración del pipeline sin requerir una instancia WinForms.

## What Changes

- Extraer el loop de captura/procesado desde `FrmMain` a un nuevo servicio **`GameLoopCoordinator`** (scoped) que orquesta `Capture → OCR → TableLayout → Decision → Result` y expone eventos/`IObservable` para la UI.
- Extraer la sincronización con `FrmOverlay` y los `tabs` (Logs/Historial) a **`UiSyncService`** (scoped), que recibe `GameLoopResult` y aplica cambios vía `ISynchronizeInvoke` sin que `FrmMain` conozca qué controles actualizar.
- Introducir **`IPokerDecisionFacade`** que agrupe `IPokerCalculator + IPostflopDecisionService + IBetSizingService + IBoardTextureAnalyzer + IOpponentTracker` en una sola superficie consumida por `GameLoopCoordinator`. Las 5+ dependencias actuales en `FrmMain` se consolidan en 1.
- Reemplazar los `volatile bool _executeCapture/_backgroundExecute` y el `while(_executeCapture)` por un **`CancellationTokenSource` + `PeriodicTimer`** gestionado por `GameLoopCoordinator.StartAsync/StopAsync`.
- Reducir `FrmMain` a presentación pura: resolución DI del coordinator + facade + overlay + logger service; wiring de botones a comandos; suscripción a eventos del coordinator. Objetivo: **≤1,500 LOC** y **≤10 dependencias** en el constructor.
- Añadir suite de **tests de integración** (`OpenScrape.App.Tests`) sobre `GameLoopCoordinator` con fakes de `IScreenReaderService` / `ITableLayoutService`, cubriendo: nueva mano, transición flop→turn→river, decisión facing-bet vs no-bet, cancelación limpia.
- **BREAKING** (interno): la firma de `IGameCoordinator` pasa a ser consumida solo por `GameLoopCoordinator`; las llamadas directas desde `FrmMain` se eliminan.

## Capabilities

### New Capabilities

- `game-loop-coordinator`: Orquestación del ciclo de captura/decisión independiente de UI, con control de ciclo de vida (`StartAsync/StopAsync`), eventos de progreso y cancelación cooperativa.
- `ui-sync-service`: Proyección de `GameLoopResult` a controles WinForms (overlay, tabs de logs/historial) respetando el thread de UI, sin acoplar el coordinador a `Control`.
- `poker-decision-facade`: Fachada que expone `EvaluateAsync(GameState)` → `DecisionResult`, agregando equity, decisión postflop, sizing, textura y stats de oponente.

### Modified Capabilities

<!-- Ninguna capability existente cambia sus requirements: game-coordinator (archivado) permanece como pieza interna usada por el nuevo game-loop-coordinator. -->

## Impact

- **Código afectado**:
  - `src/OpenScrape.App/Forms/FrmMain.cs` (reducción ~4,244 → ~1,500 LOC).
  - `src/OpenScrape.App/Services/GameCoordinator.cs` (pasa a ser consumido solo por el nuevo coordinator).
  - `src/OpenScrape.App/Program.cs` (registro DI: `AddScoped<IGameLoopCoordinator>`, `AddScoped<IUiSyncService>`, `AddScoped<IPokerDecisionFacade>`).
  - Nuevo: `src/OpenScrape.App/Services/GameLoopCoordinator.cs`, `IGameLoopCoordinator.cs`, `UiSyncService.cs`, `IUiSyncService.cs`.
  - Nuevo: `src/OpenScrape.DecisionMaker/Services/PokerDecisionFacade.cs`, `IPokerDecisionFacade.cs`.
  - Nuevo: `OpenScrape.App.Tests/GameLoopCoordinatorTests.cs`.
- **APIs públicas**: sin cambios externos. Todas las modificaciones son internas al ejecutable.
- **Dependencias**: ninguna nueva. Se usa `System.Threading.PeriodicTimer` (disponible en .NET 10).
- **Performance**: neutro o positivo (`PeriodicTimer` evita busy-waiting del `while(_executeCapture)`).
- **Tests**: 638 existentes siguen verdes; se añaden ~15 tests de integración nuevos para el coordinator.
- **Riesgos**: el loop actual tiene estado implícito en campos de `FrmMain` (p.ej. `_heroStackPreRebuy`, contadores de reintentos OCR). Migrar sin regresiones exige preservar la semántica exacta; se cubre con tests de integración paralelos antes de eliminar el código viejo.
