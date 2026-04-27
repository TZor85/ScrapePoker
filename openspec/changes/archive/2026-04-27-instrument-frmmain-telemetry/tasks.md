## 1. Preparar FrmMain para inyección de métricas

- [x] 1.1 Agregar campo `_metricsCollector` de tipo `IMetricsCollector` (ya existía)
- [x] 1.2 Agregar campo `_cycleCounter` de tipo `long` con `Interlocked` (ya existía)
- [x] 1.3 Inyectar `IMetricsCollector` en constructor de FrmMain (ya existía)
- [x] 1.4 Compilar proyecto ✓

## 2. Instrumentar ciclo completo

- [x] 2.1 Envolvir `btnCapture_Click` con `Measure(CycleTotal)` + `Interlocked.Increment`
- [x] 2.2 Agregar scope `BeginScope` con `CycleId` y `HandId` (no existe BeginScope, se usa StartHand)
- [x] 2.3 Compilar y verificar ✓

## 3. Instrumentar captura de pantalla

- [x] 3.1 Envolvir `GetImageWhilePlaying` con `Measure(CaptureScreenshot)`
- [x] 3.2 Compilar y verificar ✓

## 4. Instrumentar overlay

- [x] 4.1 Envolvir bloque `_frmOverlay.Update*` con `Measure(OverlayRender)`
- [x] 4.2 Compilar y verificar ✓

## 5. Eliminar código redundante

- [x] 5.1 Eliminar Stopwatch línea 640 (ahora usa Measure)
- [x] 5.2 Eliminar Stopwatch líneas 802, 805 (ahora usa Measure)
- [x] 5.3 Eliminar "Processing time..." de tbResume ✓
- [x] 5.4 Compilar y verificar ✓
- [ ] 5.5 Ejecutar y confirmar métricas en snapshot (requiere prueba manual)