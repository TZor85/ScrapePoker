## Why

Sin medir el ciclo completo no podemos correlacionar latencia total vs subfases. Actualmente hay Stopwatch ad-hoc en líneas 640, 802, 805 de FrmMain.cs que no están integrados en el sistema de métricas.

## What Changes

- Agregar `_cycleCounter` usando `Interlocked` + scope `BeginScope({CycleId, HandId})`
- Agregar `Measure(CycleTotal)` envolviendo `btnCapture_Click`
- Agregar `Measure(CaptureScreenshot)` envolviendo `GetImageWhilePlaying`
- Agregar `Measure(OverlayRender)` envolviendo bloque `_frmOverlay.Update*`
- Eliminar código Stopwatch ad-hoc (líneas 640, 802, 805)

## Capabilities

### Modified Capabilities

- `telemetry-instrumentation`: Instrumentar ciclo completo en FrmMain

## Impact

- Código afectado: `src/OpenScrape.App/Forms/FrmMain.cs`

## Acceptance Criteria

- Snapshot reporta `CycleTotal`, `CaptureScreenshot`, `OverlayRender`
- No queda código de Stopwatch directo en FrmMain
- Se elimina "Processing time..." en `tbResume`