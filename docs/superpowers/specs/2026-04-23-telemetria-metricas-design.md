# Telemetría y métricas de rendimiento — Diseño

- **Fecha**: 2026-04-23
- **Autor**: Alberto (con asistencia Claude)
- **Rama de trabajo**: `feature/positions` (rama donde se propone)
- **Estado**: En revisión (pendiente aprobación usuario antes de generar plan)

## Contexto y motivación

El bot de ScrapePoker no dispone de telemetría estructurada de rendimiento. Actualmente:

- `PokerDecisionFacade` mide 5 fases con `Stopwatch` y expone `DecisionResult.PhaseTimings`, pero nadie agrega ni emite esos valores.
- `FrmMain.btnCapture_Click:640` mide el ciclo end-to-end y escribe el total como texto plano en `tbResume`.
- `OcrService` no tiene ninguna instrumentación, pese a que OCR es el cuello de botella típico.
- No hay agregación p50/p95/max, no hay histórico, no hay forma de detectar degradaciones entre sesiones o entre commits.

Esto impide responder preguntas operacionales básicas: ¿cuánto cuesta un ciclo? ¿dónde está el hotspot? ¿mi cambio reciente ralentizó algo? ¿vale la pena optimizar `CardCacheService` o es `OCR.Bets` lo que lastra?

## Objetivo

Añadir un subsistema de telemetría de rendimiento que:

1. Mida latencias a lo largo del pipeline `capture → OCR → layout → decisión → render → persistencia`.
2. Agregue en memoria por mano y por sesión, con percentiles `p50/p95/max` y contador.
3. Persista el agregado por mano en `HandRecord.Telemetry` para análisis histórico.
4. Muestre métricas en tiempo real en una pestaña dedicada de `FrmMain`.
5. No introduzca dependencias nuevas ni overhead perceptible en caliente.

## Alcance

**Incluye (fase 1):**

- Nuevo namespace `OpenScrape.App.Telemetry` con `IMetricsCollector`, `MetricsCollector`, `Histogram`, `MetricsSnapshot`.
- DTOs persistidos `TelemetryAggregate` y `CategoryStats` en `OpenScrape.Domain.ValueObjects`.
- Campo nuevo `HandRecord.Telemetry` (nullable, compat retroactiva).
- Instrumentación de 17 categorías cubriendo capture, OCR, layout, decisión, render, persistencia.
- Pestaña "Métricas" en `FrmMain` con `DataGridView`, botón Reset, refresco por timer 1 s solo cuando visible.
- Borrado de `DecisionResult.PhaseTimings` (reemplazado por categorías `Decision.*`).
- ~26 tests nuevos.

**No incluye:**

- Persistencia cruda por ciclo (NDJSON / trazas). Aceptable para diagnóstico avanzado en fase 2.
- Exportador OpenTelemetry / Prometheus / Grafana.
- Sparklines, gráficos o series temporales en UI.
- Export CSV desde la pestaña (los datos quedan en Marten vía `HandRecord.Telemetry`).
- Queries predefinidas en `FrmHandDetail` o Historial (el esquema las permite, se hacen cuando haga falta).

## Decisiones y trade-offs

### D1 — Agregación por mano persistida, no por ciclo

Persistimos `{p50, p95, max, count}` por fase dentro de cada `HandRecord`. Evita inflar Marten con un doc por ciclo (~900 docs/hora) manteniendo la unidad natural de análisis del proyecto: la mano.

**Trade-off**: se pierden outliers individuales. Si se necesitan más adelante, se añade un sink NDJSON paralelo sin tocar lo existente.

### D2 — Histograma logarítmico 30 buckets, no sort/reservoir

30 buckets desde 10μs hasta ~30s, cada bucket = `10μs × 10^(i × 0.2)`. Error relativo ≤ 12% en percentiles. Zero-allocation en hot path, `Record` en O(1), `GetPercentile` en O(buckets).

**Trade-off**: precisión de percentiles limitada al ancho de bucket. Aceptable para telemetría operacional; no aceptable para SLOs milimétricos (no es el caso).

### D3 — `double` ms en persistencia, no `TimeSpan`

Serialización más estable entre versiones de serializer, más legible en `jsonb`, consultable directo en SQL. La conversión ocurre solo en el borde de persistencia; el histograma mantiene `TimeSpan` internamente.

### D4 — Singleton en DI, no scoped

`IMetricsCollector` es singleton: el agregado de sesión debe compartirse entre servicios scoped (OCR, GameCoordinator) y la UI. El ciclo de vida de mano se maneja con `StartHand`/`EndHand`, no con scopes DI.

### D5 — Enfoque 1 (`IMetricsCollector` propio) sobre `System.Diagnostics.Metrics`

La API BCL `Meter`/`Histogram<double>` es canónica para OpenTelemetry, pero no hay backend OTel aquí. El consumidor es WinForms + `HandRecord`. Añadir `Meter` + `MeterListener` propio que acabe manteniendo el mismo agregador duplica boilerplate sin ganar nada. Migración futura a OTel es trivial: añadir un `Meter` paralelo que duplique las llamadas de `IMetricsCollector.Measure`.

### D6 — UI dedicada (pestaña), no overlay ni log

Pestaña nueva "Métricas" entre Logs e Historial. El overlay ya está cargado con info de juego; `tbResume` tiene narrativa de decisión. Las métricas son análisis, no información de mesa — merecen su sitio.

### D7 — Refresco UI solo con pestaña visible

Timer de 1 s que arranca al seleccionar la pestaña y se para al abandonarla. Evita repintado innecesario durante juego normal.

### D8 — Ventana de agregación: última mano + sesión + reset manual

La columna "Última mano" se obtiene gratis (se calcula igualmente para persistir). "Sesión acumulada" da referencia estable. Botón Reset permite cortar limpio tras un cambio de configuración. No hay ventana móvil (complejidad sin valor añadido dado que el reset manual cubre el caso).

## Arquitectura

```
OpenScrape.App
├── Services/                              (existentes, instrumentados)
│   ├── OcrService                  ──┐
│   ├── ScreenReaderService         ──┤
│   ├── TableLayoutService          ──┤
│   ├── GameCoordinator             ──┼─▶ IMetricsCollector
│   ├── PokerDecisionFacade         ──┤       (singleton)
│   ├── FrmOverlay                  ──┤
│   └── GameLoggerService           ──┘
├── Forms/
│   └── FrmMain.cs                  ──▶ IMetricsCollector (UI + StartHand/EndHand)
│       └── tabMetrics (nuevo)
└── Telemetry/                             (nuevo)
    ├── IMetricsCollector.cs
    ├── MetricsCollector.cs
    ├── Histogram.cs
    ├── MetricsSnapshot.cs           (record)
    └── ScopedMeasurement.cs         (struct IDisposable)

OpenScrape.Domain
└── ValueObjects/
    ├── TelemetryAggregate.cs        (nuevo, record)
    └── CategoryStats.cs             (nuevo, record)

OpenScrape.Domain.Entities.HandRecord
└── + Telemetry: TelemetryAggregate? (campo nuevo, nullable)
```

**Principios:**

- Cero dependencias nuevas.
- Singleton en DI; `ScopedMeasurement` es `struct` → `using` sin asignación en heap.
- Thread-safe con `lock` por `CategoryState`, no global.
- Snapshot es copia inmutable; UI nunca comparte mutable state con el hot path.

## Componentes

### `IMetricsCollector`

```csharp
namespace OpenScrape.App.Telemetry;

public interface IMetricsCollector
{
    ScopedMeasurement Measure(string category);
    void Record(string category, TimeSpan elapsed);
    void StartHand(string handId);
    TelemetryAggregate EndHand();
    MetricsSnapshot SnapshotSession();
    void ResetSession();
}

public readonly struct ScopedMeasurement : IDisposable { /* ... */ }
```

- `Measure` devuelve `ScopedMeasurement` concreto (no `IDisposable` — evita boxing del struct). `using var _ = metrics.Measure(...)` funciona porque el patrón `using` de C# 8+ no requiere que el tipo implemente `IDisposable`, solo tener `Dispose()`; aún así lo implementamos por compatibilidad.
- `Record` para tests o casos sin `using` posible.
- `StartHand(handId)` reinicia el bucket `_lastHand` de cada categoría. Si hay un `_lastHand` sin `EndHand` previo, se descarta con `LogWarning`.
- `EndHand()` devuelve snapshot de la última mano + fusiona en `_session` + limpia `_lastHand`.
- `SnapshotSession()` devuelve copia inmutable `MetricsSnapshot { LastHand, Session }`.
- `ResetSession()` limpia `_session` y `_lastHand`. No afecta a la mano en curso si ya tiene muestras pendientes.

### `MetricsCollector` (impl)

- `ConcurrentDictionary<string, CategoryState>` donde `CategoryState { lock, Histogram _lastHand, Histogram _session }`.
- `Measure` → `Stopwatch.StartNew()` + devolver `ScopedMeasurement(this, category, sw)` que en `Dispose` llama `Record`.
- `Record` → toma `lock(state)` breve, `_lastHand.Add(elapsed)`, `_session.Add(elapsed)`. Sin lock global.
- `SnapshotSession` → para cada categoría, toma `lock(state)`, calcula `CategoryStats` con percentiles, los mete en dicts inmutables.

### `Histogram`

- 30 buckets, bounds precomputados: `bounds[i] = 10μs × 10^(i × 0.2)` hasta `bucket[29] ≈ 30s`.
- Campos: `long[] _buckets`, `long _count`, `TimeSpan _max`.
- `Add(TimeSpan)`:
  - `if (elapsed > _max) _max = elapsed;`
  - Busca bucket por búsqueda binaria (o tabla lookup) sobre `bounds`.
  - `_buckets[i]++; _count++;`
- `GetPercentile(double p)`:
  - `target = _count * p`.
  - Suma acumulativa hasta superar `target`.
  - Devuelve `bounds[i]` del bucket encontrado.
- `Reset()` → todos los arrays/campos a cero.

**Precisión documentada**: ±12% por el ancho logarítmico de bucket. Tests verifican esta tolerancia explícitamente.

### DTOs

```csharp
// OpenScrape.App.Telemetry
public sealed record MetricsSnapshot(
    IReadOnlyDictionary<string, CategoryStats> LastHand,
    IReadOnlyDictionary<string, CategoryStats> Session);

// OpenScrape.Domain.ValueObjects
public sealed record CategoryStats(
    double P50Ms,
    double P95Ms,
    double MaxMs,
    long Count);

public sealed record TelemetryAggregate(
    string HandId,
    DateTime CapturedAt,
    IReadOnlyDictionary<string, CategoryStats> Phases);
```

### `HandRecord` — cambio

```csharp
public class HandRecord
{
    // ... campos existentes
    public TelemetryAggregate? Telemetry { get; set; }
}
```

Nullable: manos persistidas antes de este cambio deserializan con `Telemetry = null` sin excepción.

### DI (`Program.cs`)

```csharp
services.AddSingleton<IMetricsCollector, MetricsCollector>();
```

Singleton — el snapshot global se comparte entre servicios scoped y UI.

## Puntos de instrumentación

17 categorías estables. Las cadenas son contrato: no se renombran sin migración. Definidas como constantes en `TelemetryCategories` static class para evitar drift.

| Categoría | Servicio | Frecuencia/ciclo |
|---|---|---|
| `Cycle.Total` | `FrmMain` | 1 |
| `Capture.Screenshot` | `FrmMain` | 1 |
| `OCR.Cards` | `OcrService` (etiquetado por `ScreenReaderService`) | 2–7 |
| `OCR.Bets` | idem | 2–9 |
| `OCR.Stacks` | idem | 2–9 |
| `OCR.HandNumber` | idem | 1 |
| `OCR.PlayerNames` | idem | 0–9 |
| `Layout.Dealer` | `TableLayoutService` | 1 |
| `Layout.Positions` | `TableLayoutService` | 1 |
| `Decision.Total` | `PokerDecisionFacade` | 0–1 (solo postflop) |
| `Decision.Equity` | idem | 0–1 |
| `Decision.Texture` | idem | 0–1 |
| `Decision.Profile` | idem | 0–1 |
| `Decision.DecisionService` | idem | 0–1 |
| `Decision.Sizing` | idem | 0–1 |
| `Overlay.Render` | `FrmMain` | 1 |
| `Persistence.SaveHand` | `GameLoggerService` | 1/mano (ver nota abajo) |

**Nota sobre `Persistence.SaveHand`**: se acumula **solo en `_session`**, no en `_lastHand`. Motivo: la medida del save ocurre *después* del snapshot `EndHand()` de esa mano, así que nunca podría llegar a `HandRecord.Telemetry` de su propia mano (paradoja temporal). Como consecuencia:

- Columna "Última mano" de la UI → siempre `—` para `Persistence.SaveHand`.
- Columna "Sesión" → muestra percentiles normales.
- `HandRecord.Telemetry.Phases` → no contiene `Persistence.SaveHand`.

Implementación: `IMetricsCollector.Record` recibe un flag interno o hay un método `RecordSessionOnly(category, elapsed)` usado por la instrumentación del save.

**Memoria**: 17 categorías × 2 histogramas × 30 buckets × 8 bytes ≈ 8.2 KB. Negligible.

### Cambio en `OcrService`

`ExtractTextFromRegionAsync` recibe parámetro opcional `string? metricsCategory`. Si es `null`, no mide. Si no es `null`, la llamada se envuelve en `using var _ = _metrics.Measure(metricsCategory)`. `ScreenReaderService` pasa la etiqueta según tipo de lectura.

### Cambio en `PokerDecisionFacade`

- Eliminar `Dictionary<string, TimeSpan> timings` local.
- Reemplazar cada `sw.Restart()` / `timings[...] = sw.Elapsed` por `using (_metrics.Measure("Decision.<Phase>"))`.
- Envolver toda `EvaluateAsync` con `using var _ = _metrics.Measure("Decision.Total")`.
- Eliminar `DecisionResult.PhaseTimings`. Actualizar tests dependientes.

### Correlación con `ILogger`

`FrmMain` abre al inicio de cada ciclo:

```csharp
using var scope = _logger.BeginScope(new Dictionary<string, object>
{
    ["CycleId"] = Interlocked.Increment(ref _cycleCounter),
    ["HandId"] = _tableHand,
});
```

Los logs existentes ganan correlación sin modificarse. Las métricas no heredan scope — son snapshot. Correlación manual por timestamp cuando haga falta investigar un outlier.

## Flujo de datos

### Ciclo de juego

```
FrmMain.btnCapture_Click (o timer tick)
├─ logger.BeginScope({ CycleId, HandId })
├─ using cycleTimer = metrics.Measure("Cycle.Total")
│
├─ using metrics.Measure("Capture.Screenshot")
│    GetImageWhilePlaying()
│
├─ ScreenReaderService.ReadAll(image)
│    └─ OcrService.ExtractTextFromRegionAsync(..., category: "OCR.Cards")
│          using metrics.Measure("OCR.Cards")
│          Tesseract | cache hit
│
├─ using metrics.Measure("Layout.Dealer")     → SetDealerPlayer()
├─ using metrics.Measure("Layout.Positions")  → AssignPositions()
│
├─ if (postflop):
│   PokerDecisionFacade.EvaluateAsync(request)
│   ├─ using metrics.Measure("Decision.Total")
│   ├─ using metrics.Measure("Decision.Equity")          → Calculate(...)
│   ├─ using metrics.Measure("Decision.Texture")         → Analyze(...)
│   ├─ using metrics.Measure("Decision.Profile")         → ResolveProfile(...)
│   ├─ using metrics.Measure("Decision.DecisionService") → DetermineAction(...)
│   └─ using metrics.Measure("Decision.Sizing")          → ExtractBetSize(...)
│
└─ using metrics.Measure("Overlay.Render")
     _frmOverlay.UpdateEquity(...); UpdateAction(...); ...
```

### Límites de mano

```
Nueva mano detectada (cambio de _tableHand):
├─ metrics.StartHand(newHandId)       → reset _lastHand
└─ GameLoggerService.StartNewHandAsync(...)

Cierre de mano (GameLoggerService.EndHand):
├─ var agg = metrics.EndHand()        → snapshot + fusión a _session + reset _lastHand
├─ handRecord.Telemetry = agg
├─ var sw = Stopwatch.StartNew()
├─ session save → Marten
└─ metrics.RecordSessionOnly("Persistence.SaveHand", sw.Elapsed)  → solo en _session
```

**Casos especiales:**

- App cerrada con mano abierta → `_lastHand` se pierde sin persistir. Aceptable: métricas de mano truncada no son fiables.
- `StartHand` sin `EndHand` previo → `_lastHand` descartado + `LogWarning("Telemetry: pending last-hand aggregate discarded")`. Visible si ocurre demasiado.

### Refresco UI

```
_metricsRefreshTimer.Tick (1s, solo con pestaña Métricas visible):
├─ var snap = metrics.SnapshotSession()   (copia inmutable)
├─ RenderMetricsGrid(snap)                (17 filas fijas)
└─ lblLastUpdate.Text = DateTime.Now
```

Snapshot toma `lock` breve por categoría → sin contención observable (medidas duran microsegundos).

### Reset manual

```
btnResetMetrics_Click:
└─ metrics.ResetSession()
     (limpia _session y _lastHand; mano en curso continúa sin afectar)
```

## UI — pestaña "Métricas"

Nueva `TabPage tabMetrics` en el `TabControl` existente, entre Logs e Historial.

**Controles:**

- `DataGridView dgvMetrics` — read-only, 17 filas fijas, columnas:
  - `Fase` (string, 180 px)
  - `LastP50`, `LastP95`, `LastMax`, `LastCount`
  - `SessionP50`, `SessionP95`, `SessionMax`, `SessionCount`
  - Valores numéricos en ms, format `N0`.
- `Button btnResetMetrics` — "Reset sesión" → `IMetricsCollector.ResetSession()` + refresco inmediato.
- `Label lblCurrentHand` — `HandId` actual.
- `Label lblCycleCount` — contador de ciclos acumulado.
- `Label lblLastUpdate` — timestamp del último refresco.

**Orden de filas (fijo, agrupado):**

1. `Cycle.Total`
2. `Capture.Screenshot`
3. `OCR.Cards`, `OCR.Bets`, `OCR.Stacks`, `OCR.HandNumber`, `OCR.PlayerNames`
4. `Layout.Dealer`, `Layout.Positions`
5. `Decision.Total`, `Decision.Equity`, `Decision.Texture`, `Decision.Profile`, `Decision.DecisionService`, `Decision.Sizing`
6. `Overlay.Render`
7. `Persistence.SaveHand`

Categorías no enumeradas (ampliaciones futuras) se muestran al final del grid.

**Categorías sin muestras** (`Count = 0`): `—` en p50/p95/max, `0` en count. No filas vacías, no `NaN`.

**Refresco**: `System.Windows.Forms.Timer`, interval 1000 ms, inicia en `TabSelected == tabMetrics` y para al abandonarla.

## Persistencia — esquema Marten

- `HandRecord.Telemetry` es nullable → manos antiguas sin migración.
- `TelemetryAggregate` serializa como jsonb dentro del doc `HandRecord`.
- Tamaño adicional por mano: ~640 bytes (16 categorías persistidas × ~40 bytes JSON cada; `Persistence.SaveHand` no se persiste por mano). Negligible vs el tamaño actual (`List<StreetDecision>` ~200-500 bytes por decisión).
- Queries viables sin índices extra. Si en el futuro se vuelve lento filtrar por `Telemetry.Phases["Cycle.Total"].P95Ms > X`, se añade calculated index.

**No se persiste:**

- `_session` acumulado (estado en memoria, muere con la app).
- Valores individuales por ciclo (se pierden al `StartHand`).

## Testing

**Tests unitarios (~23):**

- `HistogramTests` (~9) — precisión percentiles, max, count, reset, edge cases bucket extremos, tolerancia ±12% documentada.
- `MetricsCollectorTests` (~8) — `Measure`/`Record`, thread safety (`Parallel.For`), ciclo `StartHand`/`EndHand`, warning en `StartHand` sin `EndHand`, `SnapshotSession`, `ResetSession`.
- `CategoryStatsMappingTests` (~3) — mapeo `Histogram → CategoryStats`, count sin truncar.
- Fake time vía `TimeProvider` (.NET 10).

**Tests de integración (~3):**

- `InstrumentationIntegrationTests` — ejecutar `PokerDecisionFacade.EvaluateAsync` real, verificar categorías presentes y `Decision.Total.Count == 1`, `Decision.Total >= Σ(Decision.Equity, Texture, Profile, DecisionService, Sizing)`.

**Tests de persistencia (~3):**

- Ampliar `GameLoggerServiceTests` — persistir `HandRecord` con `Telemetry`, recuperar y verificar contenido. Deserializar `HandRecord` antiguo (sin campo) con `Telemetry = null` sin excepción.

**Total**: ~26 tests nuevos. Proyecto pasa de 638 a ~664 tests.

**Smoke test manual** (en `tasks.md` del plan de implementación):

- Pestaña Métricas existe y refresca cada 1 s.
- Cambiar de pestaña detiene el timer.
- Botón Reset limpia sin afectar mano en curso.
- Cierre de mano persiste `HandRecord.Telemetry`.

**Tests deliberadamente omitidos:**

- Microbenchmark del collector (~50 ns por `Record`, no aporta).
- UI refresh performance (17 filas × 1 Hz).
- Placeholders para categorías no instrumentadas.

## Plan de despliegue y migración

- El cambio es **aditivo**: nuevo campo nullable en `HandRecord`, nuevos servicios en DI, nueva pestaña UI. Nada existente cambia de semántica salvo `DecisionResult.PhaseTimings` (borrado).
- Marten no requiere migración de esquema (jsonb acepta el campo nuevo).
- `appsettings.json` no cambia.
- No hay flags: la telemetría se activa siempre. Overhead es sub-microsegundo; no tiene sentido hacerla opcional.

## Riesgos y mitigaciones

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Contención de lock en escenarios multithread | Baja | Medio | `lock` por categoría (no global); medidas duran μs. Test `Parallel.For` × 100 verifica. |
| `TimeProvider` inyectado no disponible en .NET 10 | Ninguna | N/A | Confirmado en .NET 8+. Proyecto ya en .NET 10. |
| Renombrar categorías rompe tests y UI | Baja | Alto (contrato) | Cadenas como constantes en `TelemetryCategories` static class. Revisión al tocar. |
| Marten serializer maneja mal `IReadOnlyDictionary` | Baja | Alto | Verificar serializer configurado en `OpenScrape.Infrastructure/Services.cs` y añadir test de roundtrip `HandRecord.Telemetry` usando el serializer real de Marten. |
| UI repintado demasiado frecuente | Ninguna | Bajo | Timer solo activo con pestaña visible. 17 filas × 1 Hz nunca es problema. |
| `Telemetry = null` en manos antiguas rompe `FrmHandDetail` | Baja | Medio | `FrmHandDetail` no lee `Telemetry` en fase 1. Cuando se cablee, null-check obligatorio. |

## Criterios de aceptación

- [ ] Al jugar/capturar, la pestaña Métricas muestra valores no-cero en `Cycle.Total`, `Capture.Screenshot`, `OCR.*`, `Layout.*`, `Overlay.Render`.
- [ ] Al cerrar una mano postflop, `Decision.*` aparecen con `Count ≥ 1` en `HandRecord.Telemetry`. `Persistence.SaveHand` aparece solo en el acumulado de sesión (nunca en una mano individual por su naturaleza temporal).
- [ ] `HandRecord.Telemetry` persistido en Marten y recuperable con el mismo contenido.
- [ ] Botón Reset limpia el acumulado sin afectar a la mano en curso.
- [ ] Cambiar de pestaña detiene el timer de refresco (verificable con log debug o monitoreo CPU).
- [ ] `dotnet test` pasa los ~664 tests (638 existentes + 26 nuevos).
- [ ] `dotnet format --verify-no-changes` limpio.
- [ ] Sin nuevas dependencias en `.csproj`.
- [ ] `DecisionResult.PhaseTimings` eliminado; compilación limpia.

## Estimación

~800–1100 LOC productivo + ~500 LOC tests. Reparto aproximado:

- `Telemetry/` (collector + histogram + DTOs): ~300 LOC
- Instrumentación en servicios (cambios pequeños en ~7 archivos): ~150 LOC
- Persistencia (DTO Domain + cambio `HandRecord` + mapeo): ~80 LOC
- UI (tabMetrics + grid + timer + reset): ~250 LOC
- Borrado de `PhaseTimings` y adaptación tests: ~50 LOC negativo
- Tests nuevos: ~500 LOC

## Fuera de alcance (fase 2 potencial)

- Sink NDJSON paralelo para trazas crudas por ciclo (análisis de outliers con DuckDB/pandas).
- Exportador OpenTelemetry (si alguna vez se monta un backend observability).
- Sparkline / serie temporal en la pestaña Métricas.
- Alertas: "Cycle.Total p95 > X ms en últimas N manos" → log o notificación.
- Cableado en `FrmHandDetail` para ver métricas de una mano concreta del historial.
- Export CSV desde la pestaña Métricas.

---

**Próximo paso**: invocar la skill `writing-plans` para generar el plan de implementación paso a paso.
