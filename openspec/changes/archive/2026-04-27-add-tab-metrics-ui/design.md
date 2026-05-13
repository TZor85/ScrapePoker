## Context

FrmMain ya tiene un DataGridView `dgvMetrics` y un Timer `_metricsRefreshTimer` en la región "Pestaña Métricas" (líneas 4360+). La infraestructura básica existe pero no está conectada a un TabPage.

## Goals

- Integrar el DataGridView existente en una pestaña visible
- Controlar el timer según visibilidad de la pestaña
- Agregar botón Reset

## Decisions

1. **Crear TabPage** `tabMetrics` entre `tabLogs` y `tabHistorial`
2. **Reutilizar** `dgvMetrics` y `_metricsRefreshTimer` existentes
3. **Conectar** `tabControl_Selected` para controlar timer
4. **Agregar** botón `btnResetMetrics` con handler que llama `ResetSession`
5. **Ordenar** filas según `TelemetryCategories.DisplayOrder`

## Risks

- Ninguno - solo UI, no afecta lógica de negocio