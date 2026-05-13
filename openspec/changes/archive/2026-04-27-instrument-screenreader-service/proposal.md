## Why

OCR es el cuello de botella del scrapeo de poker; sin métricas no podemos priorizar optimizaciones. Decidimos Opción A: NO tocar OcrService, instrumentar en ScreenReaderService.

## What Changes

Inyectar IMetricsCollector en ScreenReaderService, envolver cada método público con `using var _ = _metrics.Measure(category)`.

## Capabilities

### Modified Capabilities

- `telemetry-instrumentation`: Agregar métricas de timing en ScreenReaderService

## Impact

- Código afectado: `src/OpenScrape.App/Services/ScreenReaderService.cs`