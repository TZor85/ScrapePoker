## 1. Configurar TabPage en Designer

- [x] 1.1 Agregar TabPage `tabMetrics` entre tabLogs y tabHistorial ✓
- [x] 1.2 Mover dgvMetrics a tabMetrics (o crear nuevo) ✓
- [x] 1.3 Agregar botón btnResetMetrics ✓
- [x] 1.4 Compilar y verificar ✓

## 2. Configurar grid de métricas

- [x] 2.1 Crear/ajustar ConfigureMetricsGrid con columnas: Categoría, P50ms, P95ms, Maxms, Count ✓
- [x] 2.2 Compilar y verificar ✓

## 3. Implementar control de visibilidad

- [x] 3.1 Agregar handler tabControl_Selected ✓
- [x] 3.2 Iniciar timer cuando tabMetrics seleccionado ✓
- [x] 3.3 Detener timer cuando se sale de tabMetrics ✓
- [x] 3.4 Compilar y verificar ✓

## 4. Implementar refresh del grid

- [x] 4.1 Crear handler _metricsRefreshTimer_Tick ✓
- [x] 4.2 Crear método RefreshMetricsGrid ✓
- [x] 4.3 Ordenar según TelemetryCategories.DisplayOrder ✓
- [x] 4.4 Compilar y verificar ✓

## 5. Implementar Reset

- [x] 5.1 Crear handler btnResetMetrics_Click ✓
- [x] 5.2 Llamar _metrics.ResetSession() ✓
- [x] 5.3 Compilar y verificar ✓

## Notas

La implementación ya existía en el código:
- tabMetrics TabPage en Designer.cs (línea 143)
- dgvMetrics DataGridView con columnas (línea 148)
- btnResetMetrics Button (línea 144)
- lblCurrentHand, lblCycleCount, lblLastUpdate Labels (líneas 145-147)
- InitializeMetricsTab() método (FrmMain.cs línea 4355)
- TabControl_Selected handler (línea 4391)
- RefreshMetricsGrid() método (línea 4404)
- BtnResetMetrics_Click handler (línea 4439)

Compilación exitosa sin errores.