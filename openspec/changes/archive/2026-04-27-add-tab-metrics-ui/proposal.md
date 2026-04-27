## Why

Las métricas son inútiles sin visualización en vivo. Actualmente los datos de telemetría se colectan pero no se muestran al usuario. Una pestaña dedicada permite supervisar P50/P95/Max/Count en tiempo real.

## What Changes

- Crear pestaña `tabMetrics` entre `tabLogs` y `tabHistorial`
- DataGridView con columnas: Categoría, P50ms, P95ms, Maxms, Count
- Botón Reset para limpiar métricas
- Timer de 1s que solo corre cuando la pestaña está visible

## Capabilities

### New Capabilities

- `telemetry-ui`: Visualización en tiempo real de métricas de rendimiento

## Impact

- Archivos afectados: `FrmMain.Designer.cs`, `FrmMain.cs`

## Acceptance Criteria

- Cambiar a tabMetrics arranca el timer; salir de la pestaña lo detiene
- Botón Reset llama ResetSession
- Orden de filas según TelemetryCategories.DisplayOrder
- Mostrar P50, P95, Max y Count por categoría