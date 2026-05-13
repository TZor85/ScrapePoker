# telemetry-instrumentation

## Descripción

Capacidad para medir y reportar tiempos de ejecución de las fases críticas del ciclo de captura de poker.

## Métricas requeridas

- **CycleTotal**: Tiempo total del ciclo de captura (ms)
- **CaptureScreenshot**: Tiempo de captura de pantalla (ms)
- **OverlayRender**: Tiempo de renderizado del overlay (ms)

## Metadatos de scope

- `CycleId`: Identificador único del ciclo (incremental)
- `HandId`: Identificador de mano (si está disponible)

## Implementación

1. Usar `IMetricsCollector.Measure<T>(...)` para envolver operaciones
2. Usar `BeginScope(...)` para agregar metadatos correlacionables
3. Usar `Interlocked` para `_cycleCounter` thread-safe