## Why

Detección de dealer y asignación de posiciones tienen coste OCR + lógica posicional; necesitamos visibilidad para priorizar optimizaciones en el pipeline de poker.

## What Changes

Inyectar IMetricsCollector en TableLayoutService, envolver SetDealerPlayer y método de asignación de posiciones.

## Capabilities

### Modified Capabilities

- `telemetry-instrumentation`: Agregar métricas de timing en TableLayoutService

## Impact

- Código afectado: `src/OpenScrape.App/Services/TableLayoutService.cs`