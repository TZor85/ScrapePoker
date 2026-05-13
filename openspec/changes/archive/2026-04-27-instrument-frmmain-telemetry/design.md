## Context

FrmMain.cs tiene un ciclo principal de captura que incluye: captura de pantalla, procesamiento OCR, actualización de overlay, y logging. Hay métricas existentes en el proyecto via `IMetricsCollector` pero FrmMain no las usa.

## Goals

- Integrar FrmMain con el sistema de telemetría existente
- Medir ciclo completo, captura de pantalla, y renderizado de overlay
- Eliminar código duplicado de Stopwatch manual

## Decisions

1. **Inyectar IMetricsCollector** en FrmMain constructor
2. **Usar `Measure<T>`** en lugar de Stopwatch manual
3. **Scope con metadatos** para correlacionar ciclos: `CycleId`, `HandId`
4. **Eliminar logging de tiempo manual** de tbResume

## Risks

- Ninguno - es instrumentation pura sin cambios de lógica de negocio