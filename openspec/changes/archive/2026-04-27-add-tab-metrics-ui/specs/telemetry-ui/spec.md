# telemetry-ui

## Descripción

Capacidad para visualizar métricas de rendimiento en tiempo real en la UI de la aplicación.

## Componentes UI

- **TabPage**: Pestaña "Métricas" entre Logs e Historial
- **DataGridView**: dgvMetrics con columnas:
  - Categoría (string)
  - P50ms (decimal)
  - P95ms (decimal)
  - Maxms (decimal)
  - Count (int)
- **Button**: btnResetMetrics para limpiar métricas
- **Timer**: _metricsRefreshTimer (1s) activo solo cuando pestaña visible

## Comportamiento

1. Al seleccionar tabMetrics → timer.Start()
2. Al salir de tabMetrics → timer.Stop()
3. Cada tick del timer → RefreshMetricsGrid()
4. Botón Reset → _metrics.ResetSession()

## Datos

- Mostrar última mano (Last) y sesión (Session)
- Orden de filas: TelemetryCategories.DisplayOrder