# ADR-0017 — Telemetría con `IMetricsCollector` thread-safe y pre-merge quality checkpoints

- **Estado:** 🟢 ACEPTADO (vigente). Última feature mergeada.
- **Fecha:** 2026-04-27 a 2026-04-30 (sprint telemetría — commits `2d55bc4` ... `93316f3`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude (varios autores)

## Contexto

El proyecto carecía de **observabilidad estructurada** del game loop. Los logs textuales informaban de eventos pero no permitían:

- Medir latencia P50/P95/P99 de OCR, decisión, persistencia.
- Detectar regresiones de performance entre versiones.
- Validar que un refactor no degradó el motor.
- Reportar al usuario "tu hand está siendo procesada en 120ms en mediana".

Adicionalmente, los pre-merge eran manuales (`scripts/verify-pre-merge.ps1`): dotnet build, dotnet test, dotnet format. Sin checkpoint de cobertura de telemetría, era posible mergear código que rompía silenciosamente la instrumentación.

## Decisión

1. **`IMetricsCollector`** singleton thread-safe (commit `2d55bc4 feat(telemetria): MetricsCollector thread-safe con tests`):
   - `Measure(category, action)` o `MeasureAsync(...)` rodea bloques con cronómetro y registra en histograma.
   - **Histograma logarítmico de 30 buckets** (commit `f30b634`) cubriendo desde ~0.1ms hasta ~6.3s. Ajuste fino de bucket layout (commit `aa86eed`) para evitar precision off-by-37%.
   - **Clamp antes de max** (commit `d40ee82`) en cálculo de buckets — evita overflow.
2. **Categorías de telemetría predefinidas** (commit `ac0b363`): `OCR.HeroCards`, `OCR.HeroStack`, `OCR.Bets`, `Decision.Equity`, `Decision.PostflopAction`, `Persistence.SaveHand`, `TableLayout.Dealer`, `TableLayout.Position`, `Capture.Frame`, etc.
3. **DTOs `TelemetryAggregate` y `CategoryStats`** (commit `4afb9f6`) en `OpenScrape.Domain.ValueObjects`. Incluyen Count, P50, P95, P99, Mean, StdDev por categoría. JSON-serializable.
4. **`HandRecord.Telemetry?`** (commit `975bfaa`) opcional por mano (back-compat). Manos persistidas antes son nullable.
5. **Migración de instrumentación ad-hoc**: `PhaseTimings` propio del decision pipeline → `IMetricsCollector` (commit `ce15060`).
6. **Instrumentación punto a punto**:
   - `ScreenReaderService` (commit `2db8b27`).
   - `TableLayoutService` (commit `7230985`).
   - `GameLoggerService.SaveHand` (commit `06d7a93` integration tests).
   - `FrmMain` capture cycle (commit `9ed8094`).
7. **UI: pestaña "Métricas"** (commit `95438e6 docs/telemetry-ui`) muestra histogram y stats por categoría.
8. **Pre-merge quality checkpoints** (commit `93316f3 feat/telemetry: add pre-merge quality checkpoints`):
   - Validación de presencia de instrumentación esperada.
   - Test integration garantiza que `Decision.*` categorías están todas medidas (commit `06d7a93`).

## Alternativas consideradas

1. **Sin telemetría — inferirlo de logs.** Estado original. Rechazado: parsing logs es frágil y lento.
2. **OpenTelemetry / Application Insights.** Considerado. Rechazado por ahora: app desktop sin colector externo, bullshit incluir SDK pesado para mostrar histograma local. Aceptable para una versión SaaS futura.
3. **dotnet diagnostics counters (`System.Diagnostics.Metrics`).** Usable pero no expone histograma logarítmico custom fácilmente. **Posible upgrade futuro.** El `IMetricsCollector` actual es trivialmente envolvible sobre `Meter`.
4. **Solo Stopwatch ad-hoc en cada lugar.** Descentralizado y duplicación. Rechazado.
5. **BenchmarkDotNet para perf.** Excelente para benchmark estático (`BenchmarkSuite1/`). No reemplaza telemetría runtime — son cosas distintas.

## Consecuencias

**Positivas:**

- **Histograma 30 buckets logarítmico** captura distribución bien (P95 / P99 visibles).
- **Thread-safe** soportado en game loop background + UI thread reads.
- **Roundtrip JSON con compat retroactiva** (commit `f4c32e6`): manos viejas sin Telemetry siguen funcionando.
- **Tests integración** previenen regresión silenciosa de instrumentación.
- **UI pestaña "Métricas"** da visibilidad inmediata al usuario.
- **Pre-merge quality checkpoints** elevan la barra: no solo build + test, también "telemetría intacta".

**Negativas:**

- **Histograma logarítmico tiene precision aprox ±12-37%** en cada bucket (commit `aa86eed` documenta el rango). Para uso clínico, insuficiente. Para baseline + detección de regresión, suficiente.
- **`HandRecord.Telemetry` nullable** introduce duda: ¿siempre rellenar? ¿qué hace una sesión con manos parcialmente rellenadas? Mitigado parcialmente con docstring del campo.
- **`Persistence.SaveHand` por diseño no se incluye en `HandRecord.Telemetry`** (se mide después del snapshot del hand → solo en agregado de sesión). **Sutileza fácil de olvidar** — documentada en el field docstring.

**Implicaciones para una migración:**

- En cualquier stack moderno (Java Micrometer, Python prometheus-client, Rust metrics) hay equivalente. **Las categorías predefinidas son un contrato** que documenta qué medir.
- La **regla "instrumentar en el límite del servicio público"** (no medir helpers internos) aplica universalmente.

## Referencias

- Commit `2d55bc4 feat(telemetria): MetricsCollector thread-safe con tests`.
- Commit `f30b634 feat(telemetria): histograma logaritmico 30 buckets con tests`.
- Commit `4afb9f6 feat(domain): DTOs TelemetryAggregate y CategoryStats`.
- Commit `93316f3 feat/telemetry: add pre-merge quality checkpoints`.
- `openspec/changes/telemetry-quality-checkpoints/` — spec abierta de validación.
- `src/OpenScrape.Domain/ValueObjects/TelemetryAggregate.cs`, `CategoryStats.cs`.
