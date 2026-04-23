# Plan de implementación — Telemetría y métricas de rendimiento

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Añadir instrumentación de latencias a lo largo del pipeline `capture → OCR → layout → decisión → render → persistencia`, con agregados `p50/p95/max/count` en memoria por mano/sesión, persistidos por mano en `HandRecord.Telemetry` y visualizados en una pestaña nueva de `FrmMain`.

**Architecture:** Singleton `IMetricsCollector` en `OpenScrape.App.Telemetry` con `Histogram` logarítmico de 30 buckets, `ConcurrentDictionary` por categoría, `ScopedMeasurement` como `struct` (zero-alloc `using`). Instrumentación por inyección en 7 servicios existentes. UI con `DataGridView` refrescado por `Timer` de 1 s activo solo cuando la pestaña es visible.

**Tech Stack:** .NET 10 · WinForms · NUnit · Marten (PostgreSQL) · `System.Diagnostics.Stopwatch` · `ConcurrentDictionary` · `Microsoft.Extensions.Logging` (ya migrado).

**Spec de referencia:** `docs/superpowers/specs/2026-04-23-telemetria-metricas-design.md`.

**Convenciones:**
- Idioma: todo en castellano (XML docs, mensajes de commit, strings de log).
- File-scoped namespaces, records para DTOs, nullable reference types.
- Allman braces, 4 espacios, max 120 chars/línea.
- Conventional commits en castellano (`feat`, `fix`, `refactor`, `test`, `docs`, `chore`).
- Tests con NUnit, sin mocking framework (fakes manuales).
- Pattern DI: concreto + interface forwarding (`services.AddSingleton<Clase>(); services.AddSingleton<IInterface>(sp => sp.GetRequiredService<Clase>());`) — **excepto** para `IMetricsCollector` que puede ir directo al ser un servicio de telemetría sin consumidores adicionales.

---

## Estructura de archivos

**Archivos nuevos:**

| Ruta | Responsabilidad |
|---|---|
| `src/OpenScrape.App/Telemetry/IMetricsCollector.cs` | Interfaz pública del recolector de métricas |
| `src/OpenScrape.App/Telemetry/MetricsCollector.cs` | Implementación singleton con `ConcurrentDictionary` |
| `src/OpenScrape.App/Telemetry/Histogram.cs` | Histograma logarítmico 30 buckets, zero-alloc |
| `src/OpenScrape.App/Telemetry/ScopedMeasurement.cs` | `readonly struct IDisposable` para `using` |
| `src/OpenScrape.App/Telemetry/MetricsSnapshot.cs` | Record con snapshot de sesión + última mano |
| `src/OpenScrape.App/Telemetry/TelemetryCategories.cs` | Constantes de 17 categorías (contrato estable) |
| `src/OpenScrape.Domain/ValueObjects/TelemetryAggregate.cs` | DTO persistido por `HandRecord` |
| `src/OpenScrape.Domain/ValueObjects/CategoryStats.cs` | DTO con `{P50Ms, P95Ms, MaxMs, Count}` |
| `OpenScrape.App.Tests/HistogramTests.cs` | Tests unitarios del histograma |
| `OpenScrape.App.Tests/MetricsCollectorTests.cs` | Tests unitarios del recolector |
| `OpenScrape.App.Tests/TelemetryCategoryStatsTests.cs` | Tests de mapeo y DTOs |
| `OpenScrape.App.Tests/TelemetryInstrumentationIntegrationTests.cs` | Test integración con `PokerDecisionFacade` |
| `OpenScrape.App.Tests/HandRecordTelemetryPersistenceTests.cs` | Test roundtrip `Telemetry` en Marten |

**Archivos modificados:**

| Ruta | Cambio |
|---|---|
| `src/OpenScrape.Domain/Entities/GameSession.cs` | Añadir `public TelemetryAggregate? Telemetry { get; set; }` a `HandRecord` |
| `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs` | Eliminar `PhaseTimings` |
| `src/OpenScrape.App/Services/PokerDecisionFacade.cs` | Reemplazar `Stopwatch` ad-hoc por `IMetricsCollector.Measure(...)` |
| `src/OpenScrape.App/Services/OcrService.cs` | Parámetro opcional `string? metricsCategory` en `ExtractTextFromRegionAndDebug` |
| `src/OpenScrape.App/Services/ScreenReaderService.cs` | Pasar etiquetas de categoría en cada llamada OCR |
| `src/OpenScrape.App/Services/TableLayoutService.cs` | Instrumentar `SetDealerPlayer` y asignación de posiciones |
| `src/OpenScrape.App/Services/GameLoggerService.cs` | `StartHand`/`EndHand` sobre `IMetricsCollector` + persistir `Telemetry` + `Persistence.SaveHand` |
| `src/OpenScrape.App/Forms/FrmMain.cs` | `Cycle.Total` + `Capture.Screenshot` + `Overlay.Render` + scope `CycleId`/`HandId` + pestaña `tabMetrics` + timer |
| `src/OpenScrape.App/Forms/FrmMain.Designer.cs` | Añadir `TabPage tabMetrics` con `DataGridView`, `Button` Reset, `Label`s |
| `src/OpenScrape.App/Program.cs` | Registrar `IMetricsCollector` singleton |

---

## Fase 0 — Setup de rama

### Task 0.1: Crear rama feature desde `develop`

**Files:**
- Ninguno todavía; solo movimiento git.

- [ ] **Step 1: Verificar estado limpio**

Run:
```bash
git status
```
Expected: Working tree limpio salvo `.claude/settings.local.json` (aceptable).

- [ ] **Step 2: Actualizar develop**

Run:
```bash
git fetch origin && git checkout develop && git pull origin develop
```
Expected: Fast-forward o already up-to-date.

- [ ] **Step 3: Crear rama nueva**

Run:
```bash
git checkout -b feature/telemetria-metricas
```
Expected: `Switched to a new branch 'feature/telemetria-metricas'`.

- [ ] **Step 4: Verificar build limpio en base**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 5: Verificar tests pasan en base**

Run:
```bash
dotnet test OpenScrape.sln --configuration Debug --no-build
```
Expected: Todos verdes (~638 tests). Anotar el conteo exacto para referencia.

---

## Fase 1 — Núcleo de telemetría (TDD)

### Task 1.1: Constantes de categorías

**Files:**
- Create: `src/OpenScrape.App/Telemetry/TelemetryCategories.cs`

- [ ] **Step 1: Crear fichero con constantes**

```csharp
namespace OpenScrape.App.Telemetry;

/// <summary>
/// Contrato estable de categorías de telemetría. Las cadenas forman parte del esquema
/// persistido en <c>HandRecord.Telemetry</c> y se consumen en la UI, no se renombran
/// sin migración.
/// </summary>
public static class TelemetryCategories
{
    // Ciclo completo
    public const string CycleTotal = "Cycle.Total";

    // Captura
    public const string CaptureScreenshot = "Capture.Screenshot";

    // OCR por tipo de lectura
    public const string OcrCards = "OCR.Cards";
    public const string OcrBets = "OCR.Bets";
    public const string OcrStacks = "OCR.Stacks";
    public const string OcrHandNumber = "OCR.HandNumber";
    public const string OcrPlayerNames = "OCR.PlayerNames";

    // Detección de layout
    public const string LayoutDealer = "Layout.Dealer";
    public const string LayoutPositions = "Layout.Positions";

    // Pipeline de decisión
    public const string DecisionTotal = "Decision.Total";
    public const string DecisionEquity = "Decision.Equity";
    public const string DecisionTexture = "Decision.Texture";
    public const string DecisionProfile = "Decision.Profile";
    public const string DecisionDecisionService = "Decision.DecisionService";
    public const string DecisionSizing = "Decision.Sizing";

    // Render overlay
    public const string OverlayRender = "Overlay.Render";

    // Persistencia (solo acumula en sesión, no en última mano)
    public const string PersistenceSaveHand = "Persistence.SaveHand";

    /// <summary>
    /// Orden de presentación en la pestaña "Métricas" de <c>FrmMain</c>.
    /// Categorías no listadas se muestran al final del grid.
    /// </summary>
    public static readonly IReadOnlyList<string> DisplayOrder = new[]
    {
        CycleTotal,
        CaptureScreenshot,
        OcrCards, OcrBets, OcrStacks, OcrHandNumber, OcrPlayerNames,
        LayoutDealer, LayoutPositions,
        DecisionTotal, DecisionEquity, DecisionTexture, DecisionProfile, DecisionDecisionService, DecisionSizing,
        OverlayRender,
        PersistenceSaveHand,
    };

    /// <summary>
    /// Categorías que <b>no</b> se persisten en <c>HandRecord.Telemetry</c>.
    /// <c>Persistence.SaveHand</c> se mide después del snapshot de la mano,
    /// así que solo tiene sentido en el acumulado de sesión.
    /// </summary>
    public static readonly IReadOnlySet<string> SessionOnly = new HashSet<string>
    {
        PersistenceSaveHand,
    };
}
```

- [ ] **Step 2: Build**

Run:
```bash
dotnet build src/OpenScrape.App/OpenScrape.App.csproj
```
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add src/OpenScrape.App/Telemetry/TelemetryCategories.cs
git commit -m "feat(telemetria): constantes de categorias y orden de display"
```

---

### Task 1.2: DTOs Domain — `CategoryStats` y `TelemetryAggregate`

**Files:**
- Create: `src/OpenScrape.Domain/ValueObjects/CategoryStats.cs`
- Create: `src/OpenScrape.Domain/ValueObjects/TelemetryAggregate.cs`

- [ ] **Step 1: Crear `CategoryStats`**

```csharp
namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Estadísticos agregados de una categoría de telemetría (percentiles, máximo y conteo).
/// Unidades en milisegundos para facilitar serialización JSON y consultas SQL sobre <c>jsonb</c>.
/// </summary>
public sealed record CategoryStats(
    double P50Ms,
    double P95Ms,
    double MaxMs,
    long Count);
```

- [ ] **Step 2: Crear `TelemetryAggregate`**

```csharp
namespace OpenScrape.Domain.ValueObjects;

/// <summary>
/// Snapshot de telemetría asociado a una mano concreta. Se persiste dentro del
/// <c>HandRecord</c> correspondiente en <c>HandRecord.Telemetry</c>.
/// </summary>
public sealed record TelemetryAggregate(
    string HandId,
    DateTime CapturedAt,
    IReadOnlyDictionary<string, CategoryStats> Phases);
```

- [ ] **Step 3: Build**

Run:
```bash
dotnet build src/OpenScrape.Domain/OpenScrape.Domain.csproj
```
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add src/OpenScrape.Domain/ValueObjects/CategoryStats.cs src/OpenScrape.Domain/ValueObjects/TelemetryAggregate.cs
git commit -m "feat(domain): DTOs TelemetryAggregate y CategoryStats"
```

---

### Task 1.3: `Histogram` — tests primero (TDD)

**Files:**
- Create: `OpenScrape.App.Tests/HistogramTests.cs`
- Create: `src/OpenScrape.App/Telemetry/Histogram.cs`

- [ ] **Step 1: Escribir tests fallidos del histograma**

Archivo `OpenScrape.App.Tests/HistogramTests.cs`:

```csharp
using NUnit.Framework;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

[TestFixture]
public class HistogramTests
{
    [Test]
    public void Add_MuestraUnica_CuentaUno()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(10));
        Assert.That(h.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_MuestraUnica_MaxEsEsaMuestra()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(10));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.FromMilliseconds(10)));
    }

    [Test]
    public void Max_ConMultiplesMuestras_DevuelveLaMayor()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(5));
        h.Add(TimeSpan.FromMilliseconds(50));
        h.Add(TimeSpan.FromMilliseconds(10));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.FromMilliseconds(50)));
    }

    [Test]
    public void GetPercentile_MuestrasIguales_PercentilCaeEnMismoBucket()
    {
        var h = new Histogram();
        for (int i = 0; i < 1000; i++)
            h.Add(TimeSpan.FromMilliseconds(10));

        var p50 = h.GetPercentile(0.5).TotalMilliseconds;
        // Tolerancia 12% por granularidad de buckets logarítmicos
        Assert.That(p50, Is.InRange(9.0, 12.5));
    }

    [Test]
    public void GetPercentile_DistribucionBimodal_SeparaP50YP95()
    {
        var h = new Histogram();
        for (int i = 0; i < 500; i++) h.Add(TimeSpan.FromMilliseconds(10));
        for (int i = 0; i < 500; i++) h.Add(TimeSpan.FromMilliseconds(100));

        var p50 = h.GetPercentile(0.5).TotalMilliseconds;
        var p95 = h.GetPercentile(0.95).TotalMilliseconds;

        // Con 50/50 de 10ms y 100ms, p50 cae en el bloque de 10ms (o frontera), p95 en el de 100ms
        Assert.That(p50, Is.LessThan(20.0), "p50 debería estar cerca de 10ms");
        Assert.That(p95, Is.GreaterThan(80.0), "p95 debería estar cerca de 100ms");
    }

    [Test]
    public void Add_ValorMayorQueUltimoBucket_NoDesborda()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromSeconds(60));
        Assert.That(h.Count, Is.EqualTo(1));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.FromSeconds(60)));
    }

    [Test]
    public void Add_ValorMenorQuePrimerBucket_CaeEnPrimerBucket()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromTicks(1)); // <10μs
        Assert.That(h.Count, Is.EqualTo(1));
        Assert.That(h.GetPercentile(0.5).TotalMicroseconds, Is.LessThanOrEqualTo(15));
    }

    [Test]
    public void CountCero_Percentiles_DevuelveZero()
    {
        var h = new Histogram();
        Assert.That(h.GetPercentile(0.5), Is.EqualTo(TimeSpan.Zero));
        Assert.That(h.GetPercentile(0.95), Is.EqualTo(TimeSpan.Zero));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void Reset_VuelveAEstadoInicial()
    {
        var h = new Histogram();
        h.Add(TimeSpan.FromMilliseconds(10));
        h.Add(TimeSpan.FromMilliseconds(20));
        h.Reset();
        Assert.That(h.Count, Is.EqualTo(0));
        Assert.That(h.Max, Is.EqualTo(TimeSpan.Zero));
        Assert.That(h.GetPercentile(0.5), Is.EqualTo(TimeSpan.Zero));
    }
}
```

- [ ] **Step 2: Verificar que fallan los tests (compilación falla)**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~HistogramTests"
```
Expected: FAIL por clase `Histogram` no existe.

- [ ] **Step 3: Implementar `Histogram`**

Archivo `src/OpenScrape.App/Telemetry/Histogram.cs`:

```csharp
namespace OpenScrape.App.Telemetry;

/// <summary>
/// Histograma logarítmico de 30 buckets que cubre de 10μs a ~30s.
/// Zero-allocation en <c>Add</c>, percentiles en O(buckets).
/// Precisión de percentiles: ±12% por la granularidad logarítmica.
/// </summary>
/// <remarks>
/// No es thread-safe; la sincronización vive en <see cref="MetricsCollector"/>,
/// que envuelve cada histograma en un <c>lock</c> por categoría.
/// </remarks>
public sealed class Histogram
{
    private const int BucketCount = 30;

    /// <summary>Umbrales superiores por bucket, en ticks. Precomputados.</summary>
    private static readonly long[] BucketBoundsTicks = BuildBounds();

    private readonly long[] _buckets = new long[BucketCount];
    private long _count;
    private TimeSpan _max;

    public long Count => _count;
    public TimeSpan Max => _max;

    public void Add(TimeSpan elapsed)
    {
        if (elapsed > _max) _max = elapsed;

        long ticks = elapsed.Ticks;
        if (ticks < 0) ticks = 0;

        int idx = FindBucket(ticks);
        _buckets[idx]++;
        _count++;
    }

    public TimeSpan GetPercentile(double percentile)
    {
        if (_count == 0) return TimeSpan.Zero;

        long target = (long)Math.Ceiling(_count * percentile);
        if (target < 1) target = 1;

        long accumulated = 0;
        for (int i = 0; i < BucketCount; i++)
        {
            accumulated += _buckets[i];
            if (accumulated >= target)
                return TimeSpan.FromTicks(BucketBoundsTicks[i]);
        }
        return TimeSpan.FromTicks(BucketBoundsTicks[BucketCount - 1]);
    }

    public void Reset()
    {
        Array.Clear(_buckets, 0, BucketCount);
        _count = 0;
        _max = TimeSpan.Zero;
    }

    private static int FindBucket(long ticks)
    {
        // Búsqueda lineal: 30 comparaciones, más rápido que binaria en este tamaño.
        for (int i = 0; i < BucketCount; i++)
        {
            if (ticks <= BucketBoundsTicks[i]) return i;
        }
        return BucketCount - 1;
    }

    private static long[] BuildBounds()
    {
        // bucket[i] upper bound (segundos) = 1e-5 * 10^(i * 0.2)
        // bucket[0]  ≈ 10μs
        // bucket[29] ≈ 10μs * 10^5.8 ≈ 63s
        var bounds = new long[BucketCount];
        for (int i = 0; i < BucketCount; i++)
        {
            double seconds = 1e-5 * Math.Pow(10.0, i * 0.2);
            bounds[i] = (long)(seconds * TimeSpan.TicksPerSecond);
        }
        return bounds;
    }
}
```

- [ ] **Step 4: Ejecutar tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~HistogramTests"
```
Expected: PASS los 9 tests.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Telemetry/Histogram.cs OpenScrape.App.Tests/HistogramTests.cs
git commit -m "feat(telemetria): histograma logaritmico 30 buckets con tests"
```

---

### Task 1.4: `ScopedMeasurement` struct

**Files:**
- Create: `src/OpenScrape.App/Telemetry/ScopedMeasurement.cs`

- [ ] **Step 1: Crear `ScopedMeasurement`**

```csharp
using System.Diagnostics;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Medición ambiente zero-allocation. Se devuelve desde
/// <see cref="IMetricsCollector.Measure(string)"/> y al liberarse en el
/// <c>using</c> registra el tiempo transcurrido en la categoría correspondiente.
/// </summary>
public readonly struct ScopedMeasurement : IDisposable
{
    private readonly IMetricsCollector? _collector;
    private readonly string? _category;
    private readonly long _startTicks;

    internal ScopedMeasurement(IMetricsCollector collector, string category)
    {
        _collector = collector;
        _category = category;
        _startTicks = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (_collector is null || _category is null) return;

        long endTicks = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_startTicks, endTicks);
        _collector.Record(_category, elapsed);
    }
}
```

- [ ] **Step 2: Build (fallará porque `IMetricsCollector` aún no existe — es intencional, se completa en 1.5)**

Run:
```bash
dotnet build src/OpenScrape.App/OpenScrape.App.csproj
```
Expected: FAIL por `IMetricsCollector` no definido. **No commitear todavía.** Continuar con 1.5.

---

### Task 1.5: `IMetricsCollector` + `MetricsCollector` (TDD)

**Files:**
- Create: `src/OpenScrape.App/Telemetry/IMetricsCollector.cs`
- Create: `src/OpenScrape.App/Telemetry/MetricsSnapshot.cs`
- Create: `src/OpenScrape.App/Telemetry/MetricsCollector.cs`
- Create: `OpenScrape.App.Tests/MetricsCollectorTests.cs`

- [ ] **Step 1: Crear `IMetricsCollector`**

```csharp
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Recolector de métricas de rendimiento del pipeline. Singleton compartido entre
/// servicios scoped y la UI. Thread-safe.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Devuelve una medición ambiente. Uso: <c>using var _ = metrics.Measure("OCR.Cards");</c>
    /// </summary>
    ScopedMeasurement Measure(string category);

    /// <summary>
    /// Registra una medida manual. Usar cuando <c>using</c> no aplica (tests, código async
    /// con await en medio).
    /// </summary>
    void Record(string category, TimeSpan elapsed);

    /// <summary>
    /// Como <see cref="Record"/> pero solo acumula en el agregado de sesión, no en el de
    /// la última mano. Útil para <c>Persistence.SaveHand</c>, que se mide después del
    /// snapshot de mano y por tanto no cabe en <c>HandRecord.Telemetry</c>.
    /// </summary>
    void RecordSessionOnly(string category, TimeSpan elapsed);

    /// <summary>
    /// Marca el inicio de una nueva mano. Descarta el bucket de "última mano" previo
    /// (con log si tenía muestras) y reinicia los contadores de mano.
    /// </summary>
    void StartHand(string handId);

    /// <summary>
    /// Cierra la mano en curso: devuelve snapshot agregado, lo fusiona en el bucket de
    /// sesión y reinicia el bucket de última mano. Devuelve <c>null</c> si no había mano.
    /// </summary>
    TelemetryAggregate? EndHand();

    /// <summary>
    /// Snapshot inmutable del estado actual: última mano en curso + acumulado de sesión.
    /// Consumido por la UI de métricas.
    /// </summary>
    MetricsSnapshot SnapshotSession();

    /// <summary>
    /// Reinicia el acumulado global de sesión y el bucket de última mano.
    /// No afecta a datos ya persistidos.
    /// </summary>
    void ResetSession();
}
```

- [ ] **Step 2: Crear `MetricsSnapshot`**

```csharp
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Vista inmutable del estado del recolector en un instante dado.
/// </summary>
public sealed record MetricsSnapshot(
    string? CurrentHandId,
    IReadOnlyDictionary<string, CategoryStats> LastHand,
    IReadOnlyDictionary<string, CategoryStats> Session);
```

- [ ] **Step 3: Escribir tests fallidos de `MetricsCollector`**

Archivo `OpenScrape.App.Tests/MetricsCollectorTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

[TestFixture]
public class MetricsCollectorTests
{
    private IMetricsCollector _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
    }

    [Test]
    public void Record_UnaMuestra_ApareceEnSesion()
    {
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));
        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session.ContainsKey("OCR.Cards"), Is.True);
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(1));
    }

    [Test]
    public void Measure_ConUsing_RegistraMuestraAlDispose()
    {
        using (_ = _sut.Measure("OCR.Cards"))
        {
            Thread.Sleep(5);
        }
        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(1));
        Assert.That(snap.Session["OCR.Cards"].MaxMs, Is.GreaterThan(0));
    }

    [Test]
    public void Record_MultiplesThreads_NoPierdeMuestras()
    {
        const int iterations = 1000;
        Parallel.For(0, iterations, i =>
        {
            _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(1));
        });
        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(iterations));
    }

    [Test]
    public void StartHand_YRecord_SeAcumulaEnLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));
        var snap = _sut.SnapshotSession();
        Assert.That(snap.CurrentHandId, Is.EqualTo("hand-1"));
        Assert.That(snap.LastHand["OCR.Cards"].Count, Is.EqualTo(1));
    }

    [Test]
    public void EndHand_DevuelveAgregadoYLimpiaLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(20));

        var agg = _sut.EndHand();

        Assert.That(agg, Is.Not.Null);
        Assert.That(agg!.HandId, Is.EqualTo("hand-1"));
        Assert.That(agg.Phases["OCR.Cards"].Count, Is.EqualTo(2));

        var snap = _sut.SnapshotSession();
        Assert.That(snap.LastHand.ContainsKey("OCR.Cards"), Is.False,
            "last-hand debe haberse reseteado tras EndHand");
        Assert.That(snap.Session["OCR.Cards"].Count, Is.EqualTo(2),
            "sesión debe acumular las dos muestras");
    }

    [Test]
    public void StartHand_SinEndHandPrevio_DescartaLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));

        _sut.StartHand("hand-2");

        var snap = _sut.SnapshotSession();
        Assert.That(snap.CurrentHandId, Is.EqualTo("hand-2"));
        Assert.That(snap.LastHand.ContainsKey("OCR.Cards"), Is.False);
    }

    [Test]
    public void RecordSessionOnly_NoApareceEnLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.RecordSessionOnly("Persistence.SaveHand", TimeSpan.FromMilliseconds(50));

        var snap = _sut.SnapshotSession();
        Assert.That(snap.LastHand.ContainsKey("Persistence.SaveHand"), Is.False);
        Assert.That(snap.Session["Persistence.SaveHand"].Count, Is.EqualTo(1));

        var agg = _sut.EndHand();
        Assert.That(agg!.Phases.ContainsKey("Persistence.SaveHand"), Is.False,
            "RecordSessionOnly no debe persistirse en TelemetryAggregate");
    }

    [Test]
    public void ResetSession_LimpiaSesionYLastHand()
    {
        _sut.StartHand("hand-1");
        _sut.Record("OCR.Cards", TimeSpan.FromMilliseconds(10));

        _sut.ResetSession();

        var snap = _sut.SnapshotSession();
        Assert.That(snap.Session.Count, Is.EqualTo(0));
        Assert.That(snap.LastHand.Count, Is.EqualTo(0));
    }

    [Test]
    public void EndHand_SinStartHand_DevuelveNull()
    {
        var agg = _sut.EndHand();
        Assert.That(agg, Is.Null);
    }
}
```

- [ ] **Step 4: Implementar `MetricsCollector`**

Archivo `src/OpenScrape.App/Telemetry/MetricsCollector.cs`:

```csharp
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Implementación thread-safe del recolector de métricas. Cada categoría tiene dos
/// histogramas (última mano y sesión) bajo un lock propio; no hay contención global.
/// </summary>
public sealed class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, CategoryState> _categories = new();

    // Acceso protegido por _handLock
    private readonly object _handLock = new();
    private string? _currentHandId;

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger;
    }

    public ScopedMeasurement Measure(string category) => new(this, category);

    public void Record(string category, TimeSpan elapsed)
    {
        var state = _categories.GetOrAdd(category, _ => new CategoryState());
        lock (state.Lock)
        {
            state.LastHand.Add(elapsed);
            state.Session.Add(elapsed);
        }
    }

    public void RecordSessionOnly(string category, TimeSpan elapsed)
    {
        var state = _categories.GetOrAdd(category, _ => new CategoryState());
        lock (state.Lock)
        {
            state.Session.Add(elapsed);
        }
    }

    public void StartHand(string handId)
    {
        lock (_handLock)
        {
            if (_currentHandId is not null)
            {
                bool hadSamples = false;
                foreach (var state in _categories.Values)
                {
                    lock (state.Lock)
                    {
                        if (state.LastHand.Count > 0) hadSamples = true;
                        state.LastHand.Reset();
                    }
                }
                if (hadSamples)
                {
                    _logger.LogWarning(
                        "Telemetria: StartHand({HandId}) llamado con agregado de mano anterior pendiente, se descarta",
                        handId);
                }
            }
            _currentHandId = handId;
        }
    }

    public TelemetryAggregate? EndHand()
    {
        lock (_handLock)
        {
            if (_currentHandId is null) return null;

            var phases = new Dictionary<string, CategoryStats>();
            foreach (var (category, state) in _categories)
            {
                if (TelemetryCategories.SessionOnly.Contains(category)) continue;

                lock (state.Lock)
                {
                    if (state.LastHand.Count == 0) continue;
                    phases[category] = ToStats(state.LastHand);
                    state.LastHand.Reset();
                }
            }

            var agg = new TelemetryAggregate(
                _currentHandId,
                DateTime.UtcNow,
                phases);

            _currentHandId = null;
            return agg;
        }
    }

    public MetricsSnapshot SnapshotSession()
    {
        var lastHand = new Dictionary<string, CategoryStats>();
        var session = new Dictionary<string, CategoryStats>();
        string? handId;

        lock (_handLock) { handId = _currentHandId; }

        foreach (var (category, state) in _categories)
        {
            lock (state.Lock)
            {
                if (state.LastHand.Count > 0)
                    lastHand[category] = ToStats(state.LastHand);
                if (state.Session.Count > 0)
                    session[category] = ToStats(state.Session);
            }
        }

        return new MetricsSnapshot(handId, lastHand, session);
    }

    public void ResetSession()
    {
        foreach (var state in _categories.Values)
        {
            lock (state.Lock)
            {
                state.LastHand.Reset();
                state.Session.Reset();
            }
        }
    }

    private static CategoryStats ToStats(Histogram h) => new(
        P50Ms: h.GetPercentile(0.5).TotalMilliseconds,
        P95Ms: h.GetPercentile(0.95).TotalMilliseconds,
        MaxMs: h.Max.TotalMilliseconds,
        Count: h.Count);

    private sealed class CategoryState
    {
        public readonly object Lock = new();
        public readonly Histogram LastHand = new();
        public readonly Histogram Session = new();
    }
}
```

- [ ] **Step 5: Build y tests**

Run:
```bash
dotnet build src/OpenScrape.App/OpenScrape.App.csproj
```
Expected: `Build succeeded`.

```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~MetricsCollectorTests"
```
Expected: PASS los 9 tests.

- [ ] **Step 6: Commit**

```bash
git add src/OpenScrape.App/Telemetry/IMetricsCollector.cs src/OpenScrape.App/Telemetry/MetricsCollector.cs src/OpenScrape.App/Telemetry/MetricsSnapshot.cs src/OpenScrape.App/Telemetry/ScopedMeasurement.cs OpenScrape.App.Tests/MetricsCollectorTests.cs
git commit -m "feat(telemetria): MetricsCollector thread-safe con tests"
```

---

### Task 1.6: Tests del mapeo `Histogram → CategoryStats`

**Files:**
- Create: `OpenScrape.App.Tests/TelemetryCategoryStatsTests.cs`

- [ ] **Step 1: Escribir tests**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenScrape.App.Telemetry;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TelemetryCategoryStatsTests
{
    [Test]
    public void SnapshotSession_ConMuestraUnica_CategoryStatsCoherente()
    {
        var sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        sut.Record("X", TimeSpan.FromMilliseconds(10));

        var snap = sut.SnapshotSession();
        var stats = snap.Session["X"];

        Assert.That(stats.Count, Is.EqualTo(1));
        // Con una sola muestra, p50/p95/max coinciden con el bucket que la contiene
        Assert.That(stats.P50Ms, Is.InRange(8.0, 16.0));
        Assert.That(stats.P95Ms, Is.InRange(8.0, 16.0));
        Assert.That(stats.MaxMs, Is.EqualTo(10.0).Within(0.001));
    }

    [Test]
    public void SnapshotSession_CountLargo_NoSeTrunca()
    {
        var sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        const int n = 2_000_000;
        for (int i = 0; i < n; i++)
            sut.Record("X", TimeSpan.FromMilliseconds(1));

        var snap = sut.SnapshotSession();
        Assert.That(snap.Session["X"].Count, Is.EqualTo(n));
    }

    [Test]
    public void SnapshotSession_SinMuestras_DiccionarioVacio()
    {
        var sut = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        var snap = sut.SnapshotSession();
        Assert.That(snap.Session, Is.Empty);
        Assert.That(snap.LastHand, Is.Empty);
        Assert.That(snap.CurrentHandId, Is.Null);
    }
}
```

- [ ] **Step 2: Tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~TelemetryCategoryStatsTests"
```
Expected: PASS los 3 tests.

- [ ] **Step 3: Commit**

```bash
git add OpenScrape.App.Tests/TelemetryCategoryStatsTests.cs
git commit -m "test(telemetria): mapeo Histogram a CategoryStats"
```

---

### Checkpoint A — Fin del núcleo de telemetría

- [ ] **Step 1: Build de la solución completa**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2: Test completo del proyecto App.Tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Todos verdes. Conteo = base + 21 tests nuevos (9 Histogram + 9 MetricsCollector + 3 CategoryStats).

- [ ] **Step 3: Formato**

Run:
```bash
dotnet format OpenScrape.sln --verify-no-changes
```
Expected: Sin cambios. Si hay cambios, ejecutar `dotnet format OpenScrape.sln` y `git commit -am "chore(format): dotnet format"`.

---

## Fase 2 — Domain: `HandRecord.Telemetry`

### Task 2.1: Añadir campo `Telemetry` a `HandRecord`

**Files:**
- Modify: `src/OpenScrape.Domain/Entities/GameSession.cs`

- [ ] **Step 1: Añadir el campo al final de la clase `HandRecord`**

Localizar en `src/OpenScrape.Domain/Entities/GameSession.cs` el final de la clase `HandRecord` (alrededor de la línea 107, justo antes del cierre `}`). Añadir tras `public HandSituation Situation`:

```csharp
    /// <summary>
    /// Agregado de telemetría de rendimiento de esta mano. Null en manos persistidas
    /// antes de activar telemetría. No contiene <c>Persistence.SaveHand</c> por diseño
    /// (esa medida se captura después del snapshot y solo existe en agregado de sesión).
    /// </summary>
    public TelemetryAggregate? Telemetry { get; set; }
```

Asegurar que hay `using OpenScrape.Domain.ValueObjects;` al principio del fichero. Si no está, añadirlo.

- [ ] **Step 2: Build**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded`.

- [ ] **Step 3: Tests — asegurar que nada se rompe**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Mismo número de tests verdes que en Checkpoint A.

- [ ] **Step 4: Commit**

```bash
git add src/OpenScrape.Domain/Entities/GameSession.cs
git commit -m "feat(domain): HandRecord.Telemetry opcional para agregados por mano"
```

---

### Task 2.2: Test de roundtrip `HandRecord.Telemetry` (serialización in-memory)

**Files:**
- Create: `OpenScrape.App.Tests/HandRecordTelemetryPersistenceTests.cs`

> Este test valida que el DTO roundtrip bien usando el mismo serializador que Marten usa por defecto (`System.Text.Json`). Es un test barato y rápido que no requiere BD. Un test con BD real se puede añadir más tarde.

- [ ] **Step 1: Escribir test**

```csharp
using System.Text.Json;
using NUnit.Framework;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Tests;

[TestFixture]
public class HandRecordTelemetryPersistenceTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = false,
        PropertyNamingPolicy = null,
    };

    [Test]
    public void HandRecord_SinTelemetry_Roundtrip()
    {
        var hr = new HandRecord { HandNumber = 1 };
        var json = JsonSerializer.Serialize(hr, Options);
        var back = JsonSerializer.Deserialize<HandRecord>(json, Options);

        Assert.That(back, Is.Not.Null);
        Assert.That(back!.Telemetry, Is.Null);
    }

    [Test]
    public void HandRecord_ConTelemetry_Roundtrip()
    {
        var hr = new HandRecord
        {
            HandNumber = 1,
            Telemetry = new TelemetryAggregate(
                HandId: "hand-1",
                CapturedAt: new DateTime(2026, 4, 23, 12, 0, 0, DateTimeKind.Utc),
                Phases: new Dictionary<string, CategoryStats>
                {
                    ["OCR.Cards"] = new CategoryStats(P50Ms: 10.0, P95Ms: 25.0, MaxMs: 40.0, Count: 50),
                    ["Decision.Total"] = new CategoryStats(P50Ms: 120.0, P95Ms: 180.0, MaxMs: 210.0, Count: 3),
                }),
        };

        var json = JsonSerializer.Serialize(hr, Options);
        var back = JsonSerializer.Deserialize<HandRecord>(json, Options);

        Assert.That(back, Is.Not.Null);
        Assert.That(back!.Telemetry, Is.Not.Null);
        Assert.That(back.Telemetry!.HandId, Is.EqualTo("hand-1"));
        Assert.That(back.Telemetry.Phases, Has.Count.EqualTo(2));
        Assert.That(back.Telemetry.Phases["OCR.Cards"].Count, Is.EqualTo(50));
        Assert.That(back.Telemetry.Phases["Decision.Total"].P95Ms, Is.EqualTo(180.0));
    }

    [Test]
    public void HandRecord_JsonAntiguo_SinCampoTelemetry_Deserializa()
    {
        // JSON que representa una mano persistida antes de añadir Telemetry
        const string legacyJson = """
        {
            "Id": "abc",
            "GameSessionId": "sess-1",
            "HandNumber": 42,
            "HeroCard1": "Ah",
            "HeroCard2": "Kh",
            "HeroStackStart": 100.0,
            "HeroStackEnd": 105.0,
            "FlopCards": [],
            "Decisions": [],
            "LastStreetPlayed": 0,
            "NumOpponents": 1,
            "Result": 0,
            "Situation": 0
        }
        """;

        var back = JsonSerializer.Deserialize<HandRecord>(legacyJson, Options);

        Assert.That(back, Is.Not.Null);
        Assert.That(back!.Telemetry, Is.Null, "Telemetry debe deserializar como null en manos antiguas");
        Assert.That(back.HandNumber, Is.EqualTo(42));
    }
}
```

- [ ] **Step 2: Run tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~HandRecordTelemetryPersistenceTests"
```
Expected: PASS los 3 tests.

- [ ] **Step 3: Commit**

```bash
git add OpenScrape.App.Tests/HandRecordTelemetryPersistenceTests.cs
git commit -m "test(telemetria): roundtrip JSON de HandRecord.Telemetry con compat retro"
```

---

## Fase 3 — Registro en DI

### Task 3.1: Registrar `IMetricsCollector` singleton

**Files:**
- Modify: `src/OpenScrape.App/Program.cs`

- [ ] **Step 1: Ver estructura actual del `ConfigureServices`**

Run:
```bash
grep -n "ConfigureServices\|services.Add" src/OpenScrape.App/Program.cs | head -5
```

- [ ] **Step 2: Añadir el registro**

Localizar el bloque donde se registran los servicios `Singleton` de la App (tras `services.AddSingleton<CoordinateScaler>();` o similar, antes de los registros `Scoped`). Añadir:

```csharp
                    // Telemetría de rendimiento (singleton: snapshot compartido entre scopes y UI)
                    services.AddSingleton<IMetricsCollector, MetricsCollector>();
```

Asegurar el `using OpenScrape.App.Telemetry;` al principio del fichero.

- [ ] **Step 3: Build**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded`.

- [ ] **Step 4: Tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Todos verdes, sin nuevos fallos.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Program.cs
git commit -m "feat(di): registrar IMetricsCollector como singleton"
```

---

## Fase 4 — Instrumentación de servicios

> Orden deliberado: empezamos por `PokerDecisionFacade` porque ya tiene lógica de timings ad-hoc que reemplazamos 1:1 (bajo riesgo de regresión). Luego OCR/layout/FrmMain/GameLogger.

### Task 4.1: `PokerDecisionFacade` — reemplazar `Stopwatch` + borrar `PhaseTimings`

**Files:**
- Modify: `src/OpenScrape.App/Services/PokerDecisionFacade.cs`
- Modify: `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs`
- Modify: `OpenScrape.App.Tests/PokerDecisionFacadeTests.cs` (si referencia `PhaseTimings`)

- [ ] **Step 1: Ver si hay tests que dependan de `PhaseTimings`**

Run:
```bash
grep -rn "PhaseTimings" OpenScrape.App.Tests src/
```
Anotar los ficheros que mencionan. Deben actualizarse.

- [ ] **Step 2: Inyectar `IMetricsCollector` en `PokerDecisionFacade`**

Editar `src/OpenScrape.App/Services/PokerDecisionFacade.cs`. Añadir using:

```csharp
using OpenScrape.App.Telemetry;
```

Sustituir el constructor:

```csharp
public sealed class PokerDecisionFacade : IPokerDecisionFacade
{
    private readonly IPokerCalculator _calculator;
    private readonly IPostflopDecisionService _decisionService;
    private readonly IBetSizingService _betSizingService;
    private readonly IBoardTextureAnalyzer _boardTextureAnalyzer;
    private readonly IOpponentTracker _opponentTracker;
    private readonly IMetricsCollector _metrics;

    public PokerDecisionFacade(
        IPokerCalculator calculator,
        IPostflopDecisionService decisionService,
        IBetSizingService betSizingService,
        IBoardTextureAnalyzer boardTextureAnalyzer,
        IOpponentTracker opponentTracker,
        IMetricsCollector metrics)
    {
        _calculator = calculator;
        _decisionService = decisionService;
        _betSizingService = betSizingService;
        _boardTextureAnalyzer = boardTextureAnalyzer;
        _opponentTracker = opponentTracker;
        _metrics = metrics;
    }
```

- [ ] **Step 3: Sustituir `EvaluateAsync` — borrar timings, usar `Measure`**

Reemplazar el cuerpo del método `EvaluateAsync`:

```csharp
    public Task<DecisionResult> EvaluateAsync(DecisionRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var _totalTimer = _metrics.Measure(TelemetryCategories.DecisionTotal);

        // --- Fase 1: equity y outs (Monte Carlo / enumeración) ---
        PokerCalculationResult calculation;
        OpponentProfile? profile;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionEquity))
        {
            profile = ResolveVillainProfile(request);
            calculation = _calculator.Calculate(
                playerHand: [.. request.HeroCards],
                communityCards: [.. request.CommunityCards],
                currentPotSize: request.PotSize,
                betToCall: request.BetToCall,
                numOpponents: request.NumOpponents,
                monteCarloIterations: request.MonteCarloIterations,
                isInPosition: request.IsInPosition,
                heroStack: request.HeroStack,
                villainStack: request.VillainStack,
                handSituation: request.HandSituationTag ?? request.Situation.ToString(),
                villainPosition: request.VillainPosition,
                opponentProfile: profile);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 2: textura del board y board change ---
        BoardTextureResult textureResult;
        BoardChangeResult? boardChange;
        RiverCardType riverCardType;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionTexture))
        {
            textureResult = _boardTextureAnalyzer.Analyze([.. request.CommunityCards]);
            boardChange = ComputeBoardChange(request);
            riverCardType = request.Street == BoardPosition.River && boardChange != null
                ? _boardTextureAnalyzer.ClassifyRiverCard(boardChange)
                : RiverCardType.Neutral;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 3: perfil del oponente ---
        OpponentType villainType;
        double villainFoldToBetPct;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionProfile))
        {
            villainType = profile?.HasReliablePreflopData == true
                ? (request.IsInPosition ? profile.GetTypeForPosition(false) : profile.GetTypeForPosition(true))
                : OpponentType.Unknown;
            villainFoldToBetPct = profile?.HasReliableFoldData == true
                ? _opponentTracker.GetFoldToBetPct(request.VillainId)
                : -1.0;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 4: decisión postflop ---
        PostflopDecisionResult decision;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionDecisionService))
        {
            var hasFlushDraw = calculation.DrawTypes?.Any(d =>
                d.Contains("Flush", StringComparison.OrdinalIgnoreCase) &&
                !d.Contains("Backdoor", StringComparison.OrdinalIgnoreCase)) ?? false;

            var input = new PostflopDecisionInput
            {
                Equity = calculation.EquityPercentage,
                Street = request.Street,
                Situation = request.Situation,
                BoardTexture = textureResult.SimplifiedTexture,
                IsInPosition = request.IsInPosition,
                VillainBetSize = request.VillainBetSize,
                PotOdds = calculation.PotOddsPercentage,
                TotalOuts = calculation.TotalOuts,
                PreviousStreetBet = request.PreviousStreetBet,
                VillainShowedAggression = request.VillainShowedAggression,
                BoardChange = boardChange,
                HeroBlocksDangerSuit = request.HeroBlocksDangerSuit,
                HeroStack = request.HeroStack,
                PotSize = request.PotSize,
                HasFlushDraw = hasFlushDraw,
                NumOpponents = request.NumOpponents,
                HeroIsAggressor = request.HeroIsAggressor,
                HeroHandRank = calculation.HeroHandRank,
                HasComboDraw = calculation.HasComboDraw,
                VillainAggressorCheckedPreviousStreet = request.VillainAggressorCheckedPreviousStreet,
                VillainBarreling = request.VillainBarreling,
                VillainType = villainType,
                PairClassification = calculation.PairType,
                FoldEquity = calculation.FoldEquity,
                VillainBetSizeFlop = request.VillainBetSizeFlop,
                VillainBetSizeTurn = request.VillainBetSizeTurn,
                VillainCheckedMiddleStreet = request.VillainCheckedMiddleStreet,
                HeroHasNutBlocker = request.HeroHasNutBlocker,
                HeroFloatedFlop = request.HeroFloatedFlop,
                VillainFoldToBetPct = villainFoldToBetPct,
                HeroKickerStrength = calculation.HeroKickerStrength,
                TurnCalledWithFlushDanger = request.TurnCalledWithFlushDanger,
                HeroBlocksTopCard = request.HeroBlocksTopCard,
                HeroCheckedAllStreets = request.HeroCheckedAllStreets,
                IsAnyoneAllIn = request.IsAnyoneAllIn,
                IsDonkBet = request.IsDonkBet,
                VillainProfile = profile,
                HeroPosition = request.HeroPosition,
                VillainPosition = request.VillainPosition,
                IsBroadwayWet = request.IsBroadwayWet,
                EffectiveOuts = calculation.EffectiveOuts,
                RiverCardType = riverCardType,
            };

            decision = _decisionService.DetermineAction(input);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // --- Fase 5: sizing (extracción del string de acción) ---
        double? betSize;
        using (_ = _metrics.Measure(TelemetryCategories.DecisionSizing))
        {
            betSize = ExtractBetSize(decision.Action);
        }

        return Task.FromResult(new DecisionResult
        {
            RecommendedAction = decision.Action,
            EquityPercent = calculation.EquityPercentage,
            Reason = decision.Reason ?? $"{request.Street}/{request.Situation}",
            BoardTexture = textureResult.SimplifiedTexture,
            BetSize = betSize,
            PotOddsPercent = calculation.PotOddsPercentage,
            ExpectedValue = calculation.ExpectedValue,
            IsBluff = decision.IsBluff,
            IsBarrel = decision.IsBarrel,
            IsCheckRaise = decision.IsCheckRaise,
            IsFloating = decision.IsFloating,
            CalculationDetail = calculation,
        });
    }
```

Eliminar el `using System.Diagnostics;` si ya no se usa `Stopwatch`.

> **Nota:** Los tipos locales de las variables (`BoardTextureResult`, `PostflopDecisionResult`, `PokerCalculationResult`) deben coincidir con los que devuelven los servicios reales. Si el compilador falla por nombres, inspecciona las firmas de `_boardTextureAnalyzer.Analyze`, `_decisionService.DetermineAction` y `_calculator.Calculate` y ajusta las declaraciones.

- [ ] **Step 4: Borrar `PhaseTimings` de `DecisionResult`**

En `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs`, localizar y eliminar la propiedad:

```csharp
public IReadOnlyDictionary<string, TimeSpan> PhaseTimings { get; init; }
```

Y su inicializador por defecto si existe (p.ej. `= new Dictionary<string, TimeSpan>();`).

- [ ] **Step 5: Adaptar tests que referencian `PhaseTimings`**

Para cada fichero encontrado en Step 1, eliminar aserciones sobre `PhaseTimings` o sustituirlas por lectura del collector mockeado. En la mayoría, simplemente borrar las aserciones si son triviales del tipo "existe la clave X".

- [ ] **Step 6: Build**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded`.

- [ ] **Step 7: Tests específicos**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PokerDecisionFacade"
```
Expected: PASS.

- [ ] **Step 8: Tests completos**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Todos verdes.

- [ ] **Step 9: Commit**

```bash
git add src/OpenScrape.App/Services/PokerDecisionFacade.cs src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs OpenScrape.App.Tests/
git commit -m "refactor(decision): migrar PhaseTimings ad-hoc a IMetricsCollector"
```

---

### Task 4.2: Test de integración — instrumentación real de `PokerDecisionFacade`

**Files:**
- Create: `OpenScrape.App.Tests/TelemetryInstrumentationIntegrationTests.cs`

- [ ] **Step 1: Inspeccionar cómo instancia `PokerDecisionFacade` el test existente**

Run:
```bash
grep -n "new PokerDecisionFacade\|PokerDecisionFacadeTests" OpenScrape.App.Tests/PokerDecisionFacadeTests.cs | head -10
```

Ver cómo construyen el facade y sus dependencias (probablemente fakes o reales).

- [ ] **Step 2: Escribir test de integración**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using OpenScrape.App.Services;
using OpenScrape.App.Telemetry;
// Añadir los using que usa PokerDecisionFacadeTests para construir dependencias reales.

namespace OpenScrape.App.Tests;

/// <summary>
/// Comprueba que la instrumentación de telemetría produce las categorías esperadas
/// cuando se ejecuta el <see cref="PokerDecisionFacade"/> real.
/// No verifica valores absolutos (dependen del hardware), solo la existencia de
/// categorías y coherencia de conteos.
/// </summary>
[TestFixture]
public class TelemetryInstrumentationIntegrationTests
{
    private MetricsCollector _metrics = null!;
    private PokerDecisionFacade _facade = null!;

    [SetUp]
    public void SetUp()
    {
        _metrics = new MetricsCollector(NullLogger<MetricsCollector>.Instance);
        // Reutilizar la misma construcción de dependencias que usa PokerDecisionFacadeTests.
        // Si ese fixture expone un helper, llamarlo; si no, replicarlo aquí en privado.
        _facade = PokerDecisionFacadeTestBuilder.Build(_metrics);
    }

    [Test]
    public async Task EvaluateAsync_RegistraCategoriasDecisionEsperadas()
    {
        var request = PokerDecisionFacadeTestBuilder.SimpleFlopRequest();

        _ = await _facade.EvaluateAsync(request);

        var snap = _metrics.SnapshotSession();
        Assert.Multiple(() =>
        {
            Assert.That(snap.Session.ContainsKey(TelemetryCategories.DecisionTotal), "Decision.Total");
            Assert.That(snap.Session.ContainsKey(TelemetryCategories.DecisionEquity), "Decision.Equity");
            Assert.That(snap.Session.ContainsKey(TelemetryCategories.DecisionTexture), "Decision.Texture");
            Assert.That(snap.Session.ContainsKey(TelemetryCategories.DecisionProfile), "Decision.Profile");
            Assert.That(snap.Session.ContainsKey(TelemetryCategories.DecisionDecisionService), "Decision.DecisionService");
            Assert.That(snap.Session.ContainsKey(TelemetryCategories.DecisionSizing), "Decision.Sizing");

            Assert.That(snap.Session[TelemetryCategories.DecisionTotal].Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task EvaluateAsync_CategoriasConConteoExacto()
    {
        var request = PokerDecisionFacadeTestBuilder.SimpleFlopRequest();
        _ = await _facade.EvaluateAsync(request);
        _ = await _facade.EvaluateAsync(request);

        var s = _metrics.SnapshotSession().Session;
        Assert.Multiple(() =>
        {
            Assert.That(s[TelemetryCategories.DecisionTotal].Count, Is.EqualTo(2));
            Assert.That(s[TelemetryCategories.DecisionEquity].Count, Is.EqualTo(2));
            Assert.That(s[TelemetryCategories.DecisionSizing].Count, Is.EqualTo(2));
        });
    }
}
```

> **Nota importante sobre `PokerDecisionFacadeTestBuilder`**: si el fichero `PokerDecisionFacadeTests.cs` no expone un helper reutilizable, crearlo junto a este test:
>
> ```csharp
> // OpenScrape.App.Tests/PokerDecisionFacadeTestBuilder.cs
> internal static class PokerDecisionFacadeTestBuilder { ... }
> ```
>
> Extraer la lógica de construcción desde `PokerDecisionFacadeTests`. Si ya hay un helper similar, reutilizarlo. No duplicar lógica de construcción.

- [ ] **Step 3: Run**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~TelemetryInstrumentationIntegrationTests"
```
Expected: PASS los 2 tests.

- [ ] **Step 4: Commit**

```bash
git add OpenScrape.App.Tests/TelemetryInstrumentationIntegrationTests.cs OpenScrape.App.Tests/PokerDecisionFacadeTestBuilder.cs
git commit -m "test(telemetria): integracion de instrumentacion PokerDecisionFacade"
```

---

### Task 4.3: `OcrService` — parámetro opcional `metricsCategory`

**Files:**
- Modify: `src/OpenScrape.App/Services/OcrService.cs`

- [ ] **Step 1: Inyectar `IMetricsCollector` en `OcrService`**

Editar constructor:

```csharp
using OpenScrape.App.Telemetry;

public class OcrService
{
    private readonly IMetricsCollector _metrics;
    // ... campos existentes

    public OcrService(IMetricsCollector metrics)
    {
        _metrics = metrics;
        _tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        _engine = new TesseractEngine(Path.Combine(_tessdataPath, "tessdata"), "eng", EngineMode.Default);
        // ... resto igual
    }
```

- [ ] **Step 2: Decisión de diseño — la instrumentación OCR se hace en `ScreenReaderService`, NO en `OcrService`**

Releyendo la Task 4.4, `ScreenReaderService` ya envuelve los bloques con `_metrics.Measure(cat)` a nivel de "lectura lógica" (el método completo `ReadPlayerName` incluye las 2-3 re-lecturas de consenso). Eso cubre el valor de telemetría deseado sin necesidad de parámetro extra en `OcrService`.

**Por tanto: esta task (4.3) se reduce a inyectar `IMetricsCollector` en `OcrService` por si más adelante se añaden categorías de grano más fino, pero NO se modifican firmas públicas de los métodos OCR.** La inyección hoy queda sin uso, pero el coste es una línea; alternativamente omitir por completo.

**Elegir una de estas dos opciones y marcar:**

Opción A — Omitir la inyección ahora (YAGNI estricto):
- No modificar `OcrService`. La instrumentación OCR vive íntegramente en `ScreenReaderService` (Task 4.4).
- Commit: `git commit --allow-empty -m "chore(ocr): sin cambios, instrumentacion OCR vive en ScreenReaderService"` (para marcar la decisión en el log; o simplemente saltar esta task entera).

Opción B — Inyectar `IMetricsCollector` en `OcrService` sin uso actual (tolerancia a evolución):
- Solo cambio en constructor (Step 1 arriba), nada más.
- Advertencia de `field never used` no aparece porque sí lo asignamos.
- Commit: `feat(ocr): inyectar IMetricsCollector (sin uso actual, preparado para categorias futuras)`

**Recomendación: Opción A.** Menos cambio, menos riesgo, sin código muerto. Si más adelante hace falta grano fino, se añade entonces.

Saltar al Step 6 (commit vacío o ninguno) y continuar con Task 4.4.

- [ ] **Step 3: Si se eligió Opción B, ajustar instanciadores**

Run:
```bash
grep -rn "new OcrService" OpenScrape.App.Tests/ src/
```

Para cada uso encontrado fuera de DI, pasar `new MetricsCollector(NullLogger<MetricsCollector>.Instance)` o un stub. Si no hay ninguno (DI-only), saltar.

- [ ] **Step 4: Build + tests (ambas opciones)**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Todos verdes.

- [ ] **Step 5: Commit según opción elegida**

Opción A (sin cambios): no hace falta commit, continuar con Task 4.4.

Opción B:
```bash
git add src/OpenScrape.App/Services/OcrService.cs OpenScrape.App.Tests/
git commit -m "feat(ocr): inyectar IMetricsCollector para instrumentacion futura"
```

---

### Task 4.4: `ScreenReaderService` — pasar etiquetas en cada lectura

**Files:**
- Modify: `src/OpenScrape.App/Services/ScreenReaderService.cs`

- [ ] **Step 1: Inyectar `IMetricsCollector`**

```csharp
public class ScreenReaderService : IScreenReaderService
{
    private readonly OcrService _ocrService;
    private readonly IMetricsCollector _metrics;
    private readonly ILogger<ScreenReaderService> _logger;

    public ScreenReaderService(
        OcrService ocrService,
        IMetricsCollector metrics,
        ILogger<ScreenReaderService> logger)
    {
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
```

Añadir `using OpenScrape.App.Telemetry;`.

- [ ] **Step 2: Etiquetar cada llamada OCR según tipo**

Inspeccionar cada método público del servicio y añadir la categoría apropiada:

- `ReadPlayerName` → `TelemetryCategories.OcrPlayerNames`
- `ReadBetValue` → `TelemetryCategories.OcrBets`
- Lecturas de stack → `TelemetryCategories.OcrStacks`
- Lecturas de cartas → `TelemetryCategories.OcrCards`
- Lecturas de número de mano → `TelemetryCategories.OcrHandNumber`

Para cada llamada `_ocrService.ExtractTextFromRegionAndDebug(...)` o `ExtractTextFromRegionAsync(...)`, **envolver el bloque** con `using var _ = _metrics.Measure(cat)` en lugar de pasar el parámetro a `OcrService` — más simple porque hay 2-3 lecturas consecutivas en cada método:

```csharp
public string ReadPlayerName(Image screenshot, int x, int y, int w, int h, double umbral, double inactiveUmbral)
{
    if (screenshot == null) return string.Empty;

    using var _ = _metrics.Measure(TelemetryCategories.OcrPlayerNames);

    // Lectura 1
    string read1;
    using (var ocrResult1 = _ocrService.ExtractTextFromRegionAndDebug(
        screenshot, x, y, w, h, umbral, false))
    {
        read1 = CleanOcrPlayerName(ocrResult1.Text);
    }
    // ... resto
}
```

Repetir para `ReadBetValue`, `ReadStackValue`, `ReadCard*`, `ReadHandNumber`. Un `Measure` por método público (cubre todas las re-lecturas internas de consenso).

- [ ] **Step 3: Build + tests**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: `Build succeeded` + todos verdes. Si falla la construcción en tests que instancian `ScreenReaderService`, pasar fake metrics como en Task 4.3.

- [ ] **Step 4: Commit**

```bash
git add src/OpenScrape.App/Services/ScreenReaderService.cs OpenScrape.App.Tests/
git commit -m "feat(screen-reader): medir latencia por tipo de lectura OCR"
```

---

### Task 4.5: `TableLayoutService` — `Layout.Dealer` y `Layout.Positions`

**Files:**
- Modify: `src/OpenScrape.App/Services/TableLayoutService.cs`

- [ ] **Step 1: Inyectar `IMetricsCollector`**

Añadir al constructor (patrón igual a tasks previas). Añadir `using OpenScrape.App.Telemetry;`.

- [ ] **Step 2: Envolver `SetDealerPlayer` y método de asignación de posiciones**

Identificar los métodos:
```bash
grep -n "SetDealerPlayer\|AssignPositions\|public.*Position" src/OpenScrape.App/Services/TableLayoutService.cs | head -20
```

Añadir al inicio del método `SetDealerPlayer`:

```csharp
public void SetDealerPlayer(/* ... */)
{
    using var _ = _metrics.Measure(TelemetryCategories.LayoutDealer);
    // cuerpo existente
}
```

E igual para el método público que asigna posiciones (nombre probable `AssignPositions`, `CalculatePositions` o similar):

```csharp
using var _ = _metrics.Measure(TelemetryCategories.LayoutPositions);
```

- [ ] **Step 3: Build + tests**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Verde. Si tests instancian `TableLayoutService`, proveer fake metrics.

- [ ] **Step 4: Commit**

```bash
git add src/OpenScrape.App/Services/TableLayoutService.cs OpenScrape.App.Tests/
git commit -m "feat(layout): medir latencia de deteccion de dealer y posiciones"
```

---

### Task 4.6: `GameLoggerService` — `StartHand`/`EndHand` + `Persistence.SaveHand` + persistir `Telemetry`

**Files:**
- Modify: `src/OpenScrape.App/Services/GameLoggerService.cs`

- [ ] **Step 1: Inyectar `IMetricsCollector`**

Constructor:

```csharp
using OpenScrape.App.Telemetry;

public class GameLoggerService : IGameLoggerService
{
    private readonly IMetricsCollector _metrics;
    // ... otros campos

    public GameLoggerService(
        IDocumentStore store,
        ILogger<GameLoggerService> logger,
        IMetricsCollector metrics)
    {
        _store = store;
        _logger = logger;
        _metrics = metrics;
    }
```

> **Nota**: verificar la firma actual del constructor y mantener el orden de parámetros previos — solo añadir `metrics` al final.

- [ ] **Step 2: Llamar `StartHand` en `StartNewHandAsync`**

En el método `StartNewHandAsync` (o el que cree `_currentHand`), tras inicializar el `_currentHand`, añadir:

```csharp
_metrics.StartHand(_currentHand.Id);
```

- [ ] **Step 3: Capturar snapshot y persistirlo en `FinalizeAndPersistHandAsync`**

Modificar `FinalizeAndPersistHandAsync` (línea ~195):

```csharp
private async Task FinalizeAndPersistHandAsync()
{
    if (_currentHand == null || _currentSession == null) return;

    _currentHand.GameSessionId = _currentSession.Id;
    _currentSession.EndTime = DateTime.UtcNow;

    // Snapshot de telemetría ANTES del save (el save mismo se mide aparte)
    _currentHand.Telemetry = _metrics.EndHand();

    // Actualizar acumuladores antes de truncar (como antes)
    Interlocked.Increment(ref _sessionTotalHands);
    if (_currentHand.Result != HandResult.Unknown)
    {
        var profit = _currentHand.HeroStackEnd - _currentHand.HeroStackStart;
        lock (_dbWriteLock) { _sessionTotalProfit += profit; }
    }

    _currentSession.Hands.Add(_currentHand);
    if (_currentSession.Hands.Count > MaxHandsInMemory)
        _currentSession.Hands.RemoveAt(0);

    // Medir la persistencia Marten aparte (solo sesión, no last-hand: paradoja temporal)
    var saveStart = System.Diagnostics.Stopwatch.GetTimestamp();

    await _dbWriteLock.WaitAsync();
    try
    {
        await using var session = _store.LightweightSession();
        session.Store(_currentHand);
        await session.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error al persistir HandRecord #{HandNumber}", _currentHand.HandNumber);
    }
    finally
    {
        _dbWriteLock.Release();
    }

    var saveEnd = System.Diagnostics.Stopwatch.GetTimestamp();
    _metrics.RecordSessionOnly(
        TelemetryCategories.PersistenceSaveHand,
        System.Diagnostics.Stopwatch.GetElapsedTime(saveStart, saveEnd));

    _currentHand = null;
    _handScope?.Dispose();
    _handScope = null;
}
```

- [ ] **Step 4: Build + tests**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Verde. Tests que instancian `GameLoggerService` necesitarán fake metrics.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/GameLoggerService.cs OpenScrape.App.Tests/
git commit -m "feat(game-logger): persistir HandRecord.Telemetry y medir Persistence.SaveHand"
```

---

### Task 4.7: `FrmMain` — ciclo, captura, overlay, scope de logger

**Files:**
- Modify: `src/OpenScrape.App/Forms/FrmMain.cs`

- [ ] **Step 1: Inyectar `IMetricsCollector`**

Constructor de `FrmMain`. Añadir al final el parámetro, guardarlo en campo privado:

```csharp
private readonly IMetricsCollector _metrics;
private int _cycleCounter;

public FrmMain(
    // ... parámetros existentes
    IMetricsCollector metrics)
{
    // ... cuerpo existente
    _metrics = metrics;
}
```

Añadir `using OpenScrape.App.Telemetry;`.

- [ ] **Step 2: Envolver `btnCapture_Click` con `Cycle.Total` y scope de logger**

Localizar el método `btnCapture_Click` (línea ~638) y reestructurar el inicio:

```csharp
private async void btnCapture_Click(object sender, EventArgs e)
{
    var cycleId = Interlocked.Increment(ref _cycleCounter);
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["CycleId"] = cycleId,
        ["HandId"] = _tableHand ?? string.Empty,
    });
    using var _cycleTimer = _metrics.Measure(TelemetryCategories.CycleTotal);

    // ❌ ELIMINAR: var stopwatch = Stopwatch.StartNew();

    try
    {
        // ... cuerpo existente sin cambios en su mayoría
    }
    catch (Exception ex)
    {
        // ... manejo existente
    }
    finally
    {
        // ❌ ELIMINAR: stopwatch.Stop();
        // ❌ ELIMINAR: tbResume.Text += $"\nProcessing time: {stopwatch.ElapsedMilliseconds} ms";
    }
}
```

Borrar las líneas 640 (`Stopwatch.StartNew()`), 802 (`stopwatch.Stop()`) y 805 (`tbResume.Text += ...`). El timing end-to-end queda capturado por `Cycle.Total`.

- [ ] **Step 3: Envolver captura de pantalla**

Localizar el código que llama `GetImageWhilePlaying()` (o equivalente). Envolver:

```csharp
using (_ = _metrics.Measure(TelemetryCategories.CaptureScreenshot))
{
    await GetImageWhilePlaying();
}
```

- [ ] **Step 4: Envolver render overlay**

Localizar el bloque donde se llaman los `_frmOverlay.UpdateXxx(...)` del ciclo (típicamente al final, tras calcular acción/equity). Envolver el bloque completo con:

```csharp
using (_ = _metrics.Measure(TelemetryCategories.OverlayRender))
{
    _frmOverlay.UpdateEquityPercentage(...);
    _frmOverlay.UpdatePotOddsPercentage(...);
    _frmOverlay.UpdateAction(...);
    // ... demás updates
}
```

> **Nota**: si los `UpdateXxx` están repartidos por el método (uno al principio para `ClearAll`, otros al final), envolver solo los del final donde se pinta la decisión. Los `Clear*` al inicio no son "render" útil.

- [ ] **Step 5: Build**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded`.

- [ ] **Step 6: Tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Verde. `FrmMain` rara vez se instancia en tests, pero verificar.

- [ ] **Step 7: Commit**

```bash
git add src/OpenScrape.App/Forms/FrmMain.cs
git commit -m "feat(frm-main): instrumentar ciclo completo, captura y overlay render"
```

---

### Checkpoint B — Fin de instrumentación

- [ ] **Step 1: Build + format + tests**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
dotnet format OpenScrape.sln --verify-no-changes
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Todo verde. Si `format` reporta cambios, ejecutar sin `--verify-no-changes`, commit `chore(format): dotnet format` y repetir.

- [ ] **Step 2: Arranque manual (sin interacción con mesa, solo app viva)**

Run:
```bash
dotnet run --project src/OpenScrape.App/OpenScrape.App.csproj
```
Expected: La app arranca sin excepciones en logs. Verificar en Logs tab que no hay errores de DI.

Cerrar la app.

---

## Fase 5 — UI pestaña "Métricas"

### Task 5.1: Añadir `tabMetrics` en el diseñador de `FrmMain`

**Files:**
- Modify: `src/OpenScrape.App/Forms/FrmMain.Designer.cs`
- Modify: `src/OpenScrape.App/Forms/FrmMain.resx` (auto-editado por el diseñador)

> **Aviso**: editar el Designer a mano es frágil. Si es posible, abrir el form en Visual Studio y añadir los controles ahí. Si se hace a mano, seguir los pasos.

- [ ] **Step 1: Localizar la declaración del `TabControl`**

Run:
```bash
grep -n "TabPage\|tabControl\|tabJuego\|tabLogs\|tabHistorial" src/OpenScrape.App/Forms/FrmMain.Designer.cs | head -20
```

Identificar el nombre del `TabControl` padre y el orden actual de pestañas.

- [ ] **Step 2: Declarar los controles nuevos**

En la región de declaraciones de `FrmMain.Designer.cs`, añadir (NO redeclarar `components`, que ya existe):

```csharp
private System.Windows.Forms.TabPage tabMetrics;
private System.Windows.Forms.DataGridView dgvMetrics;
private System.Windows.Forms.Button btnResetMetrics;
private System.Windows.Forms.Label lblCurrentHand;
private System.Windows.Forms.Label lblCycleCount;
private System.Windows.Forms.Label lblLastUpdate;
private System.Windows.Forms.Timer _metricsRefreshTimer;
```

> **Importante:** el campo `private System.ComponentModel.IContainer components;` ya está declarado e inicializado en `InitializeComponent()` por el diseñador — NO redeclararlo aquí.

- [ ] **Step 3: Inicializar los controles dentro de `InitializeComponent()`**

Añadir tras la inicialización de las otras pestañas:

```csharp
// tabMetrics
this.tabMetrics = new System.Windows.Forms.TabPage();
this.tabMetrics.Text = "Métricas";
this.tabMetrics.Padding = new System.Windows.Forms.Padding(3);
this.tabMetrics.BackColor = System.Drawing.Color.White;
this.tabMetrics.UseVisualStyleBackColor = false;

// btnResetMetrics
this.btnResetMetrics = new System.Windows.Forms.Button();
this.btnResetMetrics.Location = new System.Drawing.Point(10, 10);
this.btnResetMetrics.Size = new System.Drawing.Size(120, 28);
this.btnResetMetrics.Text = "Reset sesión";
this.btnResetMetrics.Click += new System.EventHandler(this.btnResetMetrics_Click);

// lblCurrentHand
this.lblCurrentHand = new System.Windows.Forms.Label();
this.lblCurrentHand.Location = new System.Drawing.Point(150, 14);
this.lblCurrentHand.Size = new System.Drawing.Size(250, 20);
this.lblCurrentHand.Text = "Mano actual: —";

// lblCycleCount
this.lblCycleCount = new System.Windows.Forms.Label();
this.lblCycleCount.Location = new System.Drawing.Point(410, 14);
this.lblCycleCount.Size = new System.Drawing.Size(200, 20);
this.lblCycleCount.Text = "Ciclos: 0";

// lblLastUpdate
this.lblLastUpdate = new System.Windows.Forms.Label();
this.lblLastUpdate.Location = new System.Drawing.Point(620, 14);
this.lblLastUpdate.Size = new System.Drawing.Size(200, 20);
this.lblLastUpdate.Text = "Actualizado: —";

// dgvMetrics
this.dgvMetrics = new System.Windows.Forms.DataGridView();
this.dgvMetrics.Location = new System.Drawing.Point(10, 48);
this.dgvMetrics.Size = new System.Drawing.Size(900, 500);
this.dgvMetrics.Anchor = ((System.Windows.Forms.AnchorStyles)(
    System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom
    | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
this.dgvMetrics.ReadOnly = true;
this.dgvMetrics.AllowUserToAddRows = false;
this.dgvMetrics.AllowUserToDeleteRows = false;
this.dgvMetrics.AllowUserToResizeRows = false;
this.dgvMetrics.RowHeadersVisible = false;
this.dgvMetrics.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
this.dgvMetrics.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;

this.tabMetrics.Controls.Add(this.btnResetMetrics);
this.tabMetrics.Controls.Add(this.lblCurrentHand);
this.tabMetrics.Controls.Add(this.lblCycleCount);
this.tabMetrics.Controls.Add(this.lblLastUpdate);
this.tabMetrics.Controls.Add(this.dgvMetrics);

// Insertar la pestaña en el TabControl ENTRE Logs e Historial.
// Si la llamada actual es:
//   this.tabControl.Controls.Add(this.tabLogs);
//   this.tabControl.Controls.Add(this.tabHistorial);
// intercalar así:
this.tabControl.Controls.Add(this.tabLogs);
this.tabControl.Controls.Add(this.tabMetrics);
this.tabControl.Controls.Add(this.tabHistorial);

// _metricsRefreshTimer
this._metricsRefreshTimer = new System.Windows.Forms.Timer(this.components);
this._metricsRefreshTimer.Interval = 1000;
this._metricsRefreshTimer.Tick += new System.EventHandler(this._metricsRefreshTimer_Tick);

// Suscribir al Selected del TabControl para arrancar/parar el timer
this.tabControl.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabControl_Selected);
```

> **Advertencia**: si el `TabControl` ya registra `Selected` en otro handler, no sobrescribir — añadir al mismo handler o usar `+=` pero asegurando que ambos se invocan. Lo normal es que no esté registrado; se puede verificar con grep.

- [ ] **Step 4: Build**

Run:
```bash
dotnet build src/OpenScrape.App/OpenScrape.App.csproj
```
Expected: `Build succeeded`. Si falla, revisar nombres de controles (p. ej. `tabControl` puede llamarse `tcMain`).

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Forms/FrmMain.Designer.cs src/OpenScrape.App/Forms/FrmMain.resx
git commit -m "feat(frm-main): anadir pestana Metricas con grid, labels y timer"
```

---

### Task 5.2: Handlers de la pestaña Métricas en `FrmMain.cs`

**Files:**
- Modify: `src/OpenScrape.App/Forms/FrmMain.cs`

- [ ] **Step 1: Configurar columnas del grid al cargar**

En `FrmMain_Load` (o el constructor), añadir:

```csharp
private void ConfigureMetricsGrid()
{
    dgvMetrics.Columns.Clear();
    dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn
    {
        Name = "Fase", HeaderText = "Fase", FillWeight = 180, ReadOnly = true,
    });
    foreach (var prefix in new[] { "Last", "Session" })
    foreach (var suffix in new[] { "P50", "P95", "Max", "Count" })
    {
        dgvMetrics.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = $"{prefix}{suffix}",
            HeaderText = prefix == "Last" ? $"Últ. {suffix}" : $"Ses. {suffix}",
            FillWeight = 60, ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight },
        });
    }

    // Pre-poblar filas vacías en el orden canónico
    foreach (var cat in TelemetryCategories.DisplayOrder)
    {
        int rowIdx = dgvMetrics.Rows.Add(cat, "—", "—", "—", "0", "—", "—", "—", "0");
        dgvMetrics.Rows[rowIdx].Tag = cat;
    }
}
```

Invocar `ConfigureMetricsGrid()` al final de `FrmMain_Load`.

- [ ] **Step 2: Implementar `tabControl_Selected`**

```csharp
private void tabControl_Selected(object? sender, TabControlEventArgs e)
{
    if (e.TabPage == tabMetrics)
    {
        RefreshMetricsGrid();
        _metricsRefreshTimer.Start();
    }
    else
    {
        _metricsRefreshTimer.Stop();
    }
}
```

- [ ] **Step 3: Implementar `_metricsRefreshTimer_Tick`**

```csharp
private void _metricsRefreshTimer_Tick(object? sender, EventArgs e)
{
    RefreshMetricsGrid();
}
```

- [ ] **Step 4: Implementar `RefreshMetricsGrid`**

```csharp
private void RefreshMetricsGrid()
{
    var snap = _metrics.SnapshotSession();

    lblCurrentHand.Text = $"Mano actual: {snap.CurrentHandId ?? "—"}";
    lblCycleCount.Text = $"Ciclos: {_cycleCounter:N0}";
    lblLastUpdate.Text = $"Actualizado: {DateTime.Now:HH:mm:ss}";

    foreach (DataGridViewRow row in dgvMetrics.Rows)
    {
        if (row.Tag is not string category) continue;

        FillCells(row, 1, snap.LastHand.TryGetValue(category, out var last) ? last : null);
        FillCells(row, 5, snap.Session.TryGetValue(category, out var session) ? session : null);
    }

    // Añadir al final categorías no listadas en DisplayOrder (tolerancia a ampliaciones futuras)
    var listed = new HashSet<string>(TelemetryCategories.DisplayOrder);
    var extras = snap.Session.Keys.Concat(snap.LastHand.Keys)
                                  .Where(k => !listed.Contains(k))
                                  .Distinct()
                                  .ToList();
    foreach (var cat in extras)
    {
        var existingRow = FindOrAddRow(cat);
        FillCells(existingRow, 1, snap.LastHand.TryGetValue(cat, out var last) ? last : null);
        FillCells(existingRow, 5, snap.Session.TryGetValue(cat, out var session) ? session : null);
    }
}

private static void FillCells(DataGridViewRow row, int startCol, Domain.ValueObjects.CategoryStats? stats)
{
    if (stats is null)
    {
        row.Cells[startCol + 0].Value = "—";
        row.Cells[startCol + 1].Value = "—";
        row.Cells[startCol + 2].Value = "—";
        row.Cells[startCol + 3].Value = "0";
        return;
    }
    row.Cells[startCol + 0].Value = stats.P50Ms.ToString("N0");
    row.Cells[startCol + 1].Value = stats.P95Ms.ToString("N0");
    row.Cells[startCol + 2].Value = stats.MaxMs.ToString("N0");
    row.Cells[startCol + 3].Value = stats.Count.ToString("N0");
}

private DataGridViewRow FindOrAddRow(string category)
{
    foreach (DataGridViewRow r in dgvMetrics.Rows)
        if (r.Tag is string t && t == category) return r;

    int idx = dgvMetrics.Rows.Add(category, "—", "—", "—", "0", "—", "—", "—", "0");
    var row = dgvMetrics.Rows[idx];
    row.Tag = category;
    return row;
}
```

- [ ] **Step 5: Implementar `btnResetMetrics_Click`**

```csharp
private void btnResetMetrics_Click(object? sender, EventArgs e)
{
    _metrics.ResetSession();
    RefreshMetricsGrid();
    _logger.LogInformation("Telemetría: sesión reseteada por el usuario");
}
```

- [ ] **Step 6: Build**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded`.

- [ ] **Step 7: Tests**

Run:
```bash
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --configuration Debug --no-build
```
Expected: Verde.

- [ ] **Step 8: Commit**

```bash
git add src/OpenScrape.App/Forms/FrmMain.cs
git commit -m "feat(frm-main): handlers de pestana Metricas con refresco por timer"
```

---

## Fase 6 — Checkpoints finales

### Task 6.1: Build + format + test suite completa

- [ ] **Step 1: Clean build**

Run:
```bash
dotnet clean OpenScrape.sln
dotnet build OpenScrape.sln --configuration Debug
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2: Release build**

Run:
```bash
dotnet build OpenScrape.sln --configuration Release
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3: Formato**

Run:
```bash
dotnet format OpenScrape.sln --verify-no-changes
```
Expected: Sin cambios.

- [ ] **Step 4: Test suite completa**

Run:
```bash
dotnet test OpenScrape.sln --configuration Debug --no-build
```
Expected: Todos verdes. Conteo = base (638) + 23 tests (9 Histogram + 9 MetricsCollector + 3 CategoryStats + 2 Integration + 3 Persistence) = **~661 tests**.

> Si el conteo es menor, revisar que cada fichero de test se compiló y ejecutó.

- [ ] **Step 5: Contar warnings**

Run:
```bash
dotnet build OpenScrape.sln --configuration Debug 2>&1 | grep -i "warning" | wc -l
```

Apuntar el número. Debe ser igual o menor que al inicio del plan. Si subió, inspeccionar.

- [ ] **Step 6: Verificar ausencia de dependencias nuevas**

Run:
```bash
git diff main --stat -- '**/*.csproj'
```
Expected: Ningún cambio en ficheros `.csproj`. (Si algo aparece, investigar.)

---

### Task 6.2: Smoke test manual

Siguiente lista requiere ejecución manual con la aplicación. Marcar cada punto.

- [ ] **Step 1: Arrancar la app**

Run:
```bash
dotnet run --project src/OpenScrape.App/OpenScrape.App.csproj
```

Expected: Ventana `FrmMain` se abre. Pestaña "Métricas" aparece entre "Logs" e "Historial".

- [ ] **Step 2: Estado inicial de la pestaña Métricas**

Seleccionar la pestaña **Métricas**.

Expected:
- Grid con 17 filas, una por categoría en `TelemetryCategories.DisplayOrder`.
- Todas las celdas de percentiles muestran `—`; columnas Count muestran `0`.
- `Mano actual: —`, `Ciclos: 0`, `Actualizado: HH:MM:SS` (tiempo de apertura).

- [ ] **Step 3: Ejecutar un ciclo de captura**

Volver a pestaña Juego, pulsar "Capture" (o el botón principal). Esperar a que termine el ciclo. Volver a pestaña Métricas.

Expected:
- `Cycle.Total`, `Capture.Screenshot`, `OCR.*`, `Layout.*`, `Overlay.Render` tienen Count ≥ 1 y valores de percentiles no-cero en columnas "Últ." y "Ses.".
- `Decision.*` y `Persistence.SaveHand` siguen en `—` si no se llegó a postflop / cerrar mano.

- [ ] **Step 4: Ejecutar una mano postflop completa**

Jugar una mano hasta río (o cargar un replay postflop si hay modo test).

Expected tras cerrar la mano:
- `Decision.*` tienen Count ≥ 1 en columna "Ses.". La columna "Últ." puede estar vacía si ya pasó a la siguiente mano.
- `Persistence.SaveHand` tiene Count ≥ 1 en "Ses." y `—` en "Últ." (por diseño).

- [ ] **Step 5: Verificar persistencia en Marten**

Consultar la última mano persistida (pestaña Historial → abrir Hand Detail, o query directo). Verificar que el JSON incluye el campo `Telemetry` poblado.

Opción rápida con `psql`:
```sql
SELECT data->>'HandNumber', data->'Telemetry'->'Phases'
FROM public.mt_doc_handrecord
ORDER BY data->>'Timestamp' DESC
LIMIT 1;
```

Expected: `Phases` no es `null` y contiene categorías como `OCR.Cards`, `Decision.Total`, etc. **NO** debe contener `Persistence.SaveHand` (por diseño).

- [ ] **Step 6: Cambio de pestaña detiene el timer**

Observar la CPU en Task Manager (o similar). Con pestaña Métricas seleccionada: refresco cada 1 s. Cambiar a pestaña Juego: el timer debe detenerse (verificable porque `lblLastUpdate` ya no avanza).

- [ ] **Step 7: Botón Reset**

Pulsar "Reset sesión".

Expected:
- Todas las columnas "Ses." vuelven a `—` / `0`.
- Columna "Últ." sigue poblada si había mano en curso; no afectada.
- Log `Telemetría: sesión reseteada por el usuario`.

- [ ] **Step 8: Cerrar app sin errores**

Cerrar la ventana principal.

Expected: Sin excepciones en logs. La sesión Marten se guarda con normalidad.

---

### Task 6.3: Commit final y preparar PR

- [ ] **Step 1: Verificar que no quedan cambios sin commitear**

Run:
```bash
git status
```
Expected: Working tree limpio.

- [ ] **Step 2: Log de cambios**

Run:
```bash
git log --oneline develop..feature/telemetria-metricas
```
Expected: Lista completa de commits de las Fases 0-5.

- [ ] **Step 3: Opcional — push (solo si el usuario lo pide)**

Pausar aquí. **No hacer push ni abrir PR automáticamente**. El usuario decide cuándo y cómo integrar (`git flow finish feature`, PR manual, etc.).

---

## Resumen de entregables

**Código nuevo:**
- 6 clases en `src/OpenScrape.App/Telemetry/` (~300 LOC)
- 2 records en `src/OpenScrape.Domain/ValueObjects/` (~30 LOC)
- 1 campo en `HandRecord`

**Código modificado:**
- 7 servicios instrumentados con `IMetricsCollector`
- `DecisionResult.PhaseTimings` eliminado
- `Program.cs` con registro DI
- `FrmMain` con pestaña Métricas completa
- `FrmMain.Designer.cs` con controles nuevos

**Tests nuevos (~23):**
- `HistogramTests` (9)
- `MetricsCollectorTests` (9)
- `TelemetryCategoryStatsTests` (3)
- `TelemetryInstrumentationIntegrationTests` (2)
- `HandRecordTelemetryPersistenceTests` (3)

**Sin dependencias nuevas.** Sin cambios en `appsettings.json`.
