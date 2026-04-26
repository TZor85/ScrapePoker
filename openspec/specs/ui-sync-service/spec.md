## ADDED Requirements

### Requirement: UiSyncService proyecta GameLoopResult a controles WinForms

El sistema SHALL proveer un servicio `UiSyncService : IUiSyncService` (scoped) que consuma `GameLoopResult` y actualice `FrmOverlay`, `tbResume` (logs tab) y `dgvSessions`/`dgvSessionHands` (historial tab) respetando el thread de UI mediante `ISynchronizeInvoke`. `GameLoopCoordinator` MUST NO conocer qué controles concretos se actualizan.

#### Scenario: Suscripción al coordinator y actualización de overlay

- **GIVEN** `UiSyncService` está registrado como suscriptor de `IGameLoopCoordinator.ResultReady`
- **WHEN** el coordinator emite un `GameLoopResult` con `RecommendedAction = "Bet 1/2"` y `CalculationResult.EquityPercent = 65`
- **THEN** `UiSyncService` invoca `FrmOverlay.UpdateAction("Bet 1/2")` y `FrmOverlay.UpdateEquity(65)` en el thread de UI
- **AND** si la llamada proviene de un thread no-UI, se usa `Control.BeginInvoke`

### Requirement: UiSyncService serializa escrituras a controles

Cuando varios eventos `ResultReady` llegan en rápida sucesión (p.ej. tras reanudar tras pausa), `UiSyncService` SHALL serializar las escrituras de modo que la UI refleje siempre el último resultado emitido, sin condiciones de carrera entre actualizaciones parciales (overlay actualizado con mano N mientras logs muestra mano N-1).

#### Scenario: Secuencia de resultados rápidos

- **GIVEN** se emiten 3 `GameLoopResult` en 50ms
- **WHEN** `UiSyncService` los procesa
- **THEN** al finalizar todos los marshalling a UI thread, overlay, logs y historial reflejan el estado del tercer resultado
- **AND** no se observa flicker ni estado inconsistente entre overlay y logs

### Requirement: UiSyncService registra log en tbResume sin bloquear UI

Al recibir un `GameLoopResult` con `LogText` no vacío, `UiSyncService` SHALL anexar el texto a `tbResume` en la pestaña Logs sin bloquear el thread UI más de 16ms por evento. La inserción MUST hacer auto-scroll al final.

#### Scenario: Log largo no congela UI

- **GIVEN** un `GameLoopResult.LogText` con 2 KB de texto estructurado
- **WHEN** `UiSyncService` lo escribe en `tbResume`
- **THEN** la operación de append toma menos de 16ms
- **AND** `tbResume.SelectionStart = tbResume.TextLength` y se hace `ScrollToCaret()`

### Requirement: UiSyncService es intercambiable por implementación nula en tests

El sistema SHALL permitir registrar `NullUiSyncService` (patrón Null Object) cuando se ejecuten tests de integración sobre `GameLoopCoordinator`. El coordinator MUST funcionar idénticamente con ambas implementaciones.

#### Scenario: Tests con NullUiSyncService

- **GIVEN** un test que registra `NullUiSyncService` en el contenedor DI
- **WHEN** el coordinator emite resultados
- **THEN** `NullUiSyncService` ignora los eventos sin lanzar excepción
- **AND** el test no depende de ningún `Form` instanciado
