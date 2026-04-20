## Context

Tras las 8 fases del refactoring `refactoring-arquitectura` (ya archivado), la arquitectura quedó con tres servicios scoped bien delimitados —`GameCoordinator`, `ScreenReaderService`, `TableLayoutService`— consumidos directamente por `FrmMain.cs`. El form, sin embargo, retuvo:

- El **bucle de captura** (`btnCapture_Click` → `while(_executeCapture) { ... }`) con `volatile bool` y `Thread.Sleep`.
- El **procesado post-flop** (`ProcessPostFlopAsync`, ~500 LOC) que mezcla detección de board, transiciones de estado, invocación a `GameCoordinator` y actualización de `FrmOverlay`.
- La **sincronización UI** (overlay + 5 tabs) con llamadas directas del estilo `_frmOverlay.UpdateAction(...)` intercaladas en la lógica.
- El **wiring DI**: 27 dependencias inyectadas en el constructor, incluyendo `IPokerCalculator`, `IPostflopDecisionService`, `IBetSizingService`, `IBoardTextureAnalyzer`, `IOpponentTracker` etc., que serían consumidas una capa más abajo en un diseño limpio.
- Estado implícito en campos `_heroStackPreRebuy`, contadores de reintentos OCR, flags de debug.

El resultado: 4,244 LOC, testabilidad nula sobre el loop, y cualquier cambio del pipeline exige tocar el form.

Stakeholders: desarrollador único (el usuario). Restricciones: .NET 10 WinForms, no migrar a WPF/Avalonia, preservar overlay actual, no romper los 638 tests existentes.

## Goals / Non-Goals

**Goals:**
- Extraer el bucle de captura/decisión a `GameLoopCoordinator` (scoped) sin dependencia de WinForms.
- Extraer la actualización de overlay/tabs a `UiSyncService` (scoped) vía `ISynchronizeInvoke`.
- Consolidar 5 dependencias del motor de decisión en `IPokerDecisionFacade`.
- Reemplazar `volatile bool + Thread.Sleep` por `CancellationTokenSource + PeriodicTimer`.
- Reducir `FrmMain` a ≤1,500 LOC y ≤10 dependencias.
- Añadir ≥15 tests de integración del coordinator sin instanciar `Form`.

**Non-Goals:**
- **NO** re-diseñar `GameCoordinator`, `ScreenReaderService` ni `TableLayoutService` (ya refactorizados).
- **NO** migrar a MVVM ni reactive UI framework (MAUI, WPF, Avalonia).
- **NO** cambiar la estrategia de persistencia Marten.
- **NO** tocar `PostflopDecisionService`, `MonteCarloSimulator` ni el motor de decisión.
- **NO** introducir structured logging (Serilog) — se hará en otro change.
- **NO** eliminar `FrmMain`: sigue siendo el composition root del WinForms host.

## Decisions

### D1: `GameLoopCoordinator` como servicio scoped con `PeriodicTimer`

Se crea `IGameLoopCoordinator` + `GameLoopCoordinator` scoped, registrado en el mismo `IServiceScope` que `FrmMain`. El loop interno usa `System.Threading.PeriodicTimer` (disponible en .NET 10) con intervalo `CaptureIntervalMs` configurable en `appsettings.json`.

```csharp
public interface IGameLoopCoordinator : IAsyncDisposable
{
    event EventHandler<GameLoopResult>? ResultReady;
    bool IsRunning { get; }
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
}
```

**Alternativas consideradas:**
- *`Task.Run` con `while(!token.IsCancellationRequested)`*: igual de funcional pero menos idiomático que `PeriodicTimer`, y requiere `Task.Delay` manual.
- *`Timer` de WinForms*: ata el coordinator al thread UI, imposibilita tests.
- *Reactive Extensions (`Observable.Interval`)*: dependencia adicional no justificada para un único timer.

**Razonamiento**: `PeriodicTimer` es el API recomendado desde .NET 6, no tiene drift, y se integra limpiamente con `CancellationToken`.

### D2: `UiSyncService` como adaptador WinForms del evento `ResultReady`

`UiSyncService` se suscribe a `GameLoopCoordinator.ResultReady` en su constructor y marshalla cada actualización al thread UI vía `ISynchronizeInvoke.BeginInvoke`. `FrmMain` registra la instancia (vía `IUiSyncService.Attach(Form, Overlay)`) pero no es responsable de las actualizaciones.

```csharp
public interface IUiSyncService : IDisposable
{
    void Attach(ISynchronizeInvoke form, IFrmOverlay overlay, TextBox logsBox);
    void Detach();
}
```

**Alternativas consideradas:**
- *Eventos directos en `FrmMain`*: regresa al estado actual.
- *Mediator (`MediatR`)*: overhead desproporcionado para 1 consumer.
- *`IObservable<GameLoopResult>` + `ObserveOn`*: elegante pero añade dependencia `System.Reactive`.

**Razonamiento**: `ISynchronizeInvoke` es la abstracción estándar de WinForms para marshal cross-thread y es mockeable.

### D3: `IPokerDecisionFacade` agrega 5 servicios de DecisionMaker

Se consolida en un facade scoped:

```csharp
public interface IPokerDecisionFacade
{
    Task<DecisionResult> EvaluateAsync(DecisionRequest request, CancellationToken ct);
}
```

Internamente compone `IPokerCalculator`, `IPostflopDecisionService`, `IBetSizingService`, `IBoardTextureAnalyzer`, `IOpponentTracker`. No añade lógica propia: es orquestación pura.

**Alternativas consideradas:**
- *Mantener 5 servicios en `GameLoopCoordinator`*: reproduce el problema de `FrmMain` una capa más abajo.
- *Pipeline (`IDecisionStep[]`)*: flexibilidad innecesaria, los 5 pasos son fijos.
- *MediatR con un request por fase*: demasiada ceremonia.

**Razonamiento**: Facade es el patrón estándar para reducir superficie de API agregada sin re-implementar lógica.

### D4: `PostflopGameContext` se resetea en `GameLoopCoordinator.HandleNewHandAsync`

El contexto cross-street sigue siendo scoped (ya lo era). La responsabilidad del reset pasa de `FrmMain` al coordinator, que lo hace al detectar `HandCompleted || NewHandDetected`. El reset permanece incondicional para preservar la semántica actual documentada en CLAUDE.md línea 103.

### D5: Migración gradual con feature flag `UseGameLoopCoordinator`

Se añade `appsettings.json → "Features": { "UseGameLoopCoordinator": true }`. Si es `false`, `FrmMain` ejecuta el loop antiguo (código intacto). Esto permite A/B local y rollback sin revertir el PR. El flag se elimina en el change siguiente tras validación.

**Alternativas consideradas:**
- *Big-bang sin flag*: riesgo de regresión silenciosa.
- *Feature flag persistido en DB*: overkill para uso local.

### D6: `CancellationToken` reemplaza `volatile bool _executeCapture`

`FrmMain.btnCapture_Click` deja de manipular flags volátiles. Crea un `CancellationTokenSource` en `btnStart_Click`, lo pasa a `_coordinator.StartAsync(cts.Token)` y llama `_coordinator.StopAsync()` + `cts.Cancel()` en `btnStop_Click`. El `_heroStackPreRebuy` y demás estado de mano se mueve al `PostflopGameContext` (donde semánticamente pertenece).

### D7: Tests de integración con fakes, no mocks

`OpenScrape.App.Tests/GameLoopCoordinatorTests.cs` usa implementaciones `Fake*` simples (en-memoria) de `IScreenReaderService` y `ITableLayoutService`. No se introduce Moq/NSubstitute como dependencia nueva (coherente con el resto del proyecto).

**Razonamiento**: mantener la política existente del repo (`NUnit + fakes manuales`).

## Risks / Trade-offs

- **Riesgo: estado implícito de `FrmMain` se pierde en la migración** (contadores de reintentos OCR, `_heroStackPreRebuy`, flags de debug) → **Mitigación**: inventariar todos los campos mutables de `FrmMain` en la fase 1 de tasks; cada campo se mueve a `PostflopGameContext`, `GameLoopCoordinator` o `UiSyncService` según responsabilidad. Checklist en `tasks.md`.
- **Riesgo: regresión sutil en detección de transiciones de street** (el orden de comprobación en `ProcessPostFlopAsync` es sensible al timing del OCR) → **Mitigación**: feature flag `UseGameLoopCoordinator` permite rollback instantáneo; tests de integración con screenshots grabados de sesiones reales.
- **Riesgo: `PeriodicTimer` no preserva drift-free EXACTO 100ms que `Thread.Sleep(100)` daba en el código viejo** → **Mitigación**: `PeriodicTimer` tiene precisión superior; el intervalo real no debe cambiar comportamiento si el loop se diseñó correctamente.
- **Riesgo: deadlock al parar el coordinator si una iteración está en mitad de OCR síncrono** → **Mitigación**: `StopAsync` espera a que la iteración actual termine con timeout de 2s; si se excede, fuerza `Dispose` del timer y loguea warning. Tests cubren este caso.
- **Trade-off: el facade `IPokerDecisionFacade` introduce una capa de indirección** que obliga a pasar más datos por `DecisionRequest`. Ganancia en testabilidad y cohesión compensa sobradamente el coste.
- **Trade-off: `UiSyncService` depende de `ISynchronizeInvoke`**, que es una abstracción WinForms — si en el futuro se migra a WPF habría que reescribirlo. Aceptable porque no hay plan de migración.

## Migration Plan

1. **Fase 0 (preparación)**: inventariar campos mutables de `FrmMain` que deben migrar (producir lista en `tasks.md`). Añadir feature flag `UseGameLoopCoordinator=false` (OFF por defecto durante desarrollo).
2. **Fase 1 (facade)**: crear `IPokerDecisionFacade` + impl, registrar en DI, tests unitarios. No tocar `FrmMain`.
3. **Fase 2 (coordinator)**: crear `IGameLoopCoordinator` + impl consumiendo el facade. Tests de integración con fakes. No tocar `FrmMain`.
4. **Fase 3 (ui-sync)**: crear `IUiSyncService` + impl. `FrmMain` expone `PublicOverlay`/`PublicLogsBox` temporalmente para el `Attach()`.
5. **Fase 4 (wiring opcional)**: bajo feature flag, `FrmMain.btnStart_Click` arranca coordinator + ui-sync en vez del loop viejo. Validación manual con partidas reales. Tests E2E.
6. **Fase 5 (cutover)**: flip del flag a `true` por defecto. Código viejo del loop marcado `[Obsolete]`.
7. **Fase 6 (cleanup)**: eliminar código marcado obsoleto, eliminar feature flag, eliminar campos `_executeCapture`/`_backgroundExecute` y constructores 27-param. `FrmMain` ≤1,500 LOC.

**Rollback**: en cualquier fase anterior a 6, poner feature flag a `false` restaura el comportamiento previo sin redeploy.

## Open Questions

- ¿`DecisionRequest` debe incluir referencia al `PostflopGameContext` completo o solo campos necesarios? Inclinación: campos explícitos para reproducibilidad en backtest, pero exige ~15 campos.
- ¿`GameLoopResult` debe exponer `Exception?` o usar tipo `Result<GameLoopData>` (Ardalis)? Inclinación: `Result<>` para consistencia con capa Features.
- ¿`PokerDecisionFacade.PhaseTimings` se persiste en `StreetDecision` o solo se loguea? Inclinación: solo log en Debug, evitar hinchar schema Marten.
- Intervalo de captura actual: hardcoded a 100ms en `FrmMain`. ¿Mover a `StrategyProfile` o a sección `"GameLoop"` nueva en `appsettings.json`? Inclinación: sección nueva (no es poker-strategy).
