# US-05 — Telemetría operativa y tracking de bankroll

> **Historia de observabilidad y gestión de capital.** Cubre dos aspectos paralelos del producto: (1) métricas técnicas en pestaña Métricas (latencias, contadores, calidad OCR) que ayudan a Pablo y al equipo a detectar regresiones; (2) tracking de bankroll con risk-of-ruin que ayuda a Pablo a decidir si seguir jugando o parar.

---

## 1. Persona

**Pablo** — dos contextos de uso:
1. **Operativo (durante sesión):** mira métricas para entender por qué el bot está lento o por qué OCR falla esporádicamente.
2. **Estratégico (entre sesiones):** revisa bankroll evolución, decide si subir de nivel (NL5 → NL10) o bajar (NL10 → NL5).

Equipo (en menor grado):
- **Dev del bot:** usa la pestaña Métricas como dashboard para detectar regresiones tras un cambio.

---

## 2. Historia (formato narrativo)

> **Como** Pablo,
> **quiero** ver en tiempo real las métricas técnicas del bot (latencia OCR, equity, decisión, errores) y a la vez el estado de mi bankroll (evolución, risk-of-ruin, sugerencia de stake),
> **para** detectar problemas operativos antes de que arruinen una sesión y para gestionar mi capital con disciplina basada en datos.

---

## 3. Criterios de aceptación — Telemetría

### CA-01 — Recolección de métricas durante el ciclo

**Dado** que el bot está en una sesión activa,
**Cuando** ocurre cualquier operación instrumentada (`Capture`, `OCR.Cards`, `OCR.Bets`, `Equity`, `Decision.Total`, etc.),
**Entonces** un `using var measurement = _metrics.Measure(TelemetryCategories.X)` registra la latencia con bucket logarítmico,
**Y** los contadores asociados (`OCR.Cards.LowConfidence`, `Decision.CacheHit`, etc.) se incrementan via `Interlocked`,
**Y** la categoría se persiste como `string` (DD-11 contrato estable, no `enum`).

🟢 Confirmado en `MetricsCollector.cs` + `TelemetryCategories.cs` + ADR-0017.

### CA-02 — 17 categorías estables

**Dado** que el equipo del producto necesita queries históricas sobre `HandRecord.Telemetry`,
**Cuando** se persiste una mano,
**Entonces** las 17 categorías de `TelemetryCategories` están todas presentes con `DisplayOrder` conocido,
**Y** los nombres NO se renombran sin migración (`"OCR.Cards"` permanece estable),
**Y** las categorías marcadas `SessionOnly` (no persistidas) son excluidas del documento Marten.

🟢 Confirmado en DD-11.

### CA-03 — Histogramas con buckets logarítmicos

**Dado** que `MetricsCollector` acumula muestras,
**Cuando** se calcula p50/p95/p99,
**Entonces** se usan 30 buckets logarítmicos `bound[i] = 1e-5 × 10^(i × 0.2)` segundos (10 µs → 6.3 s),
**Y** la sobrestimación es ≤37 % (aceptable para detectar regresiones),
**Y** el costo de memoria es ~240 bytes/categoría/histograma (despreciable).

🟢 Confirmado en `Histogram.cs`.

### CA-04 — Pestaña Métricas: snapshot UI

**Dado** que Pablo va a la pestaña Métricas durante una sesión activa,
**Cuando** el panel se refresca (cada 1 s o on-demand),
**Entonces** ve para cada categoría:
- Nombre + descripción.
- Conteo total de muestras.
- p50, p95, p99 en ms.
- Sparkline o histogram visualization (si implementado).
- Contadores asociados (ej. `OCR.Cards.LowConfidence: 12 (3.2%)`).

🟡 **Inferido:** UI de pestaña Métricas existe pero el detalle de visualización (sparkline vs tabla simple) no se confirmó al detalle.

### CA-05 — Métricas persisten en `HandRecord.Telemetry`

**Dado** que una mano se cierra con `EndHand`,
**Cuando** `FinalizeAndPersistHandAsync` ejecuta,
**Entonces** un snapshot de las métricas relevantes se persiste en `HandRecord.Telemetry: TelemetryAggregate`,
**Y** queries históricas pueden agregar latencias por categoría cross-sesión,
**Y** las categorías `SessionOnly` (volátiles) NO se persisten.

🟢 Confirmado en `TelemetryAggregate` value object + DD-11.

### CA-06 — Quality checkpoints pre-merge

**Dado** que un dev abre un PR con cambios en motor o captura,
**Cuando** los tests `MetricsCollectorTests`, `HistogramTests`, `HandRecordTelemetryPersistenceTests` corren en CI,
**Entonces** validan:
- Las 17 categorías están presentes.
- Los buckets log son ≥30.
- La persistencia round-trip preserva todos los campos.
- No hay drift entre `Histogram` accumulator y `MetricsSnapshot`.

🟢 Confirmado en suites de test (~33 tests dedicados a telemetría).

---

## 4. Criterios de aceptación — Bankroll

### CA-07 — Snapshot de bankroll por sesión

**Dado** que Pablo termina una sesión con profit total -25 BB,
**Cuando** `BankrollTrackerService.RecordSessionEnd(sessionId, profitBB, bigBlind)` se invoca,
**Entonces** un nuevo `BankrollSnapshot { Timestamp, BankrollBB, SessionDeltaBB, BigBlind, Stake }` se persiste,
**Y** el snapshot mantiene el bankroll convertido a unidades neutras (BB) para permitir comparación cross-stake.

🟢 Confirmado en `BankrollTrackerService.cs` + value object `BankrollSnapshot`.

### CA-08 — Cálculo de risk-of-ruin

**Dado** que Pablo tiene un histórico de ≥100 sesiones con `BBPer100` y desviación estándar conocidas,
**Cuando** `BankrollTrackerService.CalculateRiskOfRuin(currentBankrollBB, winRate, stdDev)` se ejecuta,
**Entonces** retorna probabilidad de ruina `P(ruin) ∈ [0, 1]` usando la fórmula estándar de Kelly criterion / Sklansky bankroll,
**Y** Pablo ve el resultado en pestaña Bankroll con código de colores: `<5% verde, 5-15% amarillo, >15% rojo`.

🟡 **Inferido:** la lógica matemática es estándar pero el trigger de Risk-of-Ruin (cuándo se calcula) y el rendering UI específicos están en Q-DOM-04 raíz.

### CA-09 — Sugerencia de stake basada en bankroll

**Dado** que Pablo tiene 3000 BB de NL5 (≈$150 a $0.05 BB) y winrate +5 BB/100,
**Cuando** `BankrollTrackerService.SuggestStake` analiza,
**Entonces** sugiere mantener NL5 (heuristica: 30 buy-ins mínimos para estable + winrate positivo),
**Y** si Pablo cae a 1500 BB con winrate -2 BB/100, sugiere bajar a NL2,
**Y** si Pablo sube a 6000 BB con winrate sostenido +5 BB/100, sugiere considerar shot-take a NL10.

🔴 **Lacuna:** funcionalidad NO confirmada al detalle en código; podría ser feature parcial. Documentar como spec pendiente.

### CA-10 — Alertas de stop-loss / stop-win

**Dado** que Pablo configuró stop-loss de -50 BB por sesión,
**Cuando** durante una sesión `_sessionTotalProfit <= -50 * bigBlind`,
**Entonces** un indicador visual aparece en `FrmOverlay` (banda roja superior) con mensaje *"Stop-loss alcanzado: -50 BB. Considera parar."*,
**Y** la decisión de parar es de Pablo (el bot NO se auto-pausa).

🔴 **Lacuna:** stop-loss no localizado en código; spec pendiente o feature futura.

### CA-11 — `Exploitability BB/100` correctamente parametrizado

**Dado** que Pablo opera en NL5 (`BigBlind = 0.05`),
**Cuando** `ExploitabilityCalculator.RecordDecision` y `BankrollTrackerService.CalculateBBPer100` calculan,
**Entonces** los resultados escalan correctamente con el `BigBlind` real,
**Y** NO usan el `BigBlind = 1.0` hardcoded de la implementación actual (Q-DM-07 anomalía 🔴).

🔴 **Bug activo Q-DM-07:** hasta que se corrija, los reports BB/100 son incorrectos en NL≠1.

---

## 5. Diagrama de flujo

```
[En cada ciclo del BG worker — US-01]
         │
         ▼
ScopedMeasurement (using IDisposable)
  ├─ start = Stopwatch.GetTimestamp
  ├─ código instrumentado ejecuta
  └─ Dispose: histogram.AddSample(elapsed)
         │
         ▼
MetricsCollector mantiene en memoria:
  ├─ Histogram[category] (30 buckets log)
  ├─ Counter[name] (Interlocked)
  └─ Snapshot disponible on-demand
         │
         ▼ [Pablo abre pestaña Métricas]
         │
MetricsCollector.Snapshot
  ├─ Por categoría: count, p50, p95, p99
  └─ UI renderiza tabla / sparklines
         │
         ▼ [Pablo cierra mano — US-03 EndHand]
         │
TelemetryAggregate snapshot
  ├─ Filtra categorías SessionOnly
  └─ HandRecord.Telemetry = aggregate
         │
         ▼
Marten persiste HandRecord con telemetría embebida


[Tracking bankroll, paralelo]

[Sesión cierra — US-03 SaveSessionAsync]
         │
         ▼
BankrollTrackerService.RecordSessionEnd
  ├─ profitBB = sessionTotalProfit / bigBlind
  ├─ snapshot = new BankrollSnapshot
  └─ Marten persiste
         │
         ▼ [Pablo abre pestaña Bankroll]
         │
BankrollTrackerService.GetEvolution
  ├─ Time series de BankrollBB cross-stakes
  ├─ CalculateRiskOfRuin (Kelly/Sklansky)
  ├─ SuggestStake (basado en buy-ins)
  └─ UI renderiza gráfico + recomendación
```

---

## 6. Variantes (caminos secundarios)

### V-01 — Telemetría con `TraceLevel` activado pre-`SetTextBoxTarget`
- Buffer de logs satura `BufferCapacity=1000` antes del flush al TextBox (EC-16, Q-APP-21).
- Diagnóstico operativo incompleto.
- Mitigación: aumentar default + sink consola en paralelo.

### V-02 — Stake mixto (Pablo juega NL5 + NL10 en mismo período)
- `BankrollTrackerService` debe agregar BB cross-stake correctamente.
- Hoy `BankrollSnapshot` incluye `BigBlind` y `Stake` → conversion factible.
- Pendiente: validación de consolidación cross-stake en pestaña Bankroll.

### V-03 — Bug Q-DM-07 BigBlind=1.0 hardcoded
- `BBPer100` reportado es 10× el real en NL10, 5× en NL5.
- Pablo cree que tiene winrate de +50 BB/100 cuando en realidad es +5 BB/100.
- 🔴 Bug bloquea coaching y bankroll fiable.

### V-04 — Risk-of-Ruin con dataset insuficiente
- Si Pablo tiene <100 sesiones, varianza estimada no es robusta.
- Comportamiento esperado: mostrar disclaimer "Dataset insuficiente, esperar 100+ sesiones".
- Pendiente: definir threshold y UI.

### V-05 — Sesión sin manos (Pablo abrió y cerró sin jugar)
- `_sessionTotalHands == 0` → `BBPer100 = NaN` (división por 0).
- Comportamiento esperado: omitir snapshot del bankroll (no contar sesión vacía).
- Pendiente: validar en `BankrollTrackerService.RecordSessionEnd`.

### V-06 — Multi-mesa: 4 instancias generan 4 sesiones
- Cada instancia persiste sesión independiente con su propio `_sessionTotalProfit`.
- `BankrollTrackerService` debe agregar las 4 al consultar bankroll consolidado.
- 🟡 Validar agregación cross-instancia.

### V-07 — Telemetría con cliente offline (sin Marten)
- `MetricsCollector` sigue acumulando in-memory.
- `HandRecord.Telemetry` no se persiste si Marten falla (Q-APP-12).
- Mitigación pendiente: cola de fallback.

### V-08 — Categoría nueva añadida en versión X.Y
- Si dev añade `OCR.Players` sin migración, queries históricas sobre la nueva categoría retornan vacío para sesiones viejas.
- Comportamiento esperado: documentar la versión introductora; UI tolerante a categoría faltante.

---

## 7. Métricas de éxito

### Telemetría:

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Categorías persistidas** | 17 (todas las non-SessionOnly) | `TelemetryCategories.cs` |
| **Overhead de instrumentación** | <2 % del tiempo de ciclo | benchmark |
| **Tasa de pestaña Métricas usable** | >95 % de tiempo (sin freeze UI) | manual |
| **Drift entre snapshot y persisted aggregate** | 0 % | tests |
| **Tiempo de query histórica (telemetría 1000 manos)** | <1 s | log |

### Bankroll:

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Precisión `BBPer100`** | exacto al stake (Q-DM-07 fixed) | manual + test |
| **Risk-of-Ruin computado correctamente** | match con calculadora externa | comparación |
| **Sugerencia de stake aceptada por Pablo** | >70 % de las veces | feedback |
| **Detección de auto-rebuy en bankroll** | profit correcto en 100 % de manos con rebuy | DD-06 |

---

## 8. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| `BigBlind=1.0` hardcoded → BB/100 incorrecto | 🔴 | — | Parametrizar (Q-DM-07) |
| Buffer logger saturado | 🟡 | sink consola paralelo | Aumentar default (Q-APP-21) |
| Categoría telemetría renombrada por error | 🟡 | tests verifican presencia | CI bloquea rename |
| Risk-of-Ruin con dataset pequeño | 🟡 | — | Threshold + disclaimer |
| Bankroll cross-stake mal agregado | 🟡 | `BigBlind` en snapshot | Validar agregación |
| Multi-mesa cross-instancia | 🟡 | sesiones independientes | Consolidar en query |
| Stop-loss no implementado | 🟢 | — | Spec futura |
| Sugerencia de stake lacuna | 🟢 | — | Spec futura |
| Outage Marten pierde telemetría | 🟡 | try/catch loggea | Cola fallback (Q-APP-12) |
| Histograms log overestimación 37% | 🟢 | aceptable para regresiones | Documentar limitación |

---

## 9. Dependencias

**Otras user stories:**
- US-01 / US-03 generan los datos consumidos aquí.
- US-04 (revisión historial) puede compartir el panel de coaching agregado.

**Specs por unit:**
- `OpenScrape.App/` — `MetricsCollector`, `Histogram`, `TelemetryCategories`, `MetricsSnapshot`, `ScopedMeasurement`.
- `OpenScrape.DecisionMaker/` — `BankrollTrackerService`, `ExploitabilityCalculator`, `StrategyAnalyzerService`.
- `OpenScrape.Domain/` — `TelemetryAggregate`, `BankrollSnapshot`, `CategoryStats`.
- `OpenScrape.Infrastructure/` — Marten setup con índices sobre `HandRecord.Telemetry` (si aplica).

**Externo:**
- PostgreSQL accesible para queries agregadas históricas.

---

## 10. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] CA-01 a CA-11 pasan en testing manual.
- [ ] Pestaña Métricas muestra las 17 categorías con valores no-vacíos tras 30 manos.
- [ ] `BBPer100` correcto tras fix Q-DM-07.
- [ ] Risk-of-Ruin computa con dataset de ≥100 sesiones, valida contra calculadora externa.
- [ ] Bankroll cross-stake agrega correctamente.
- [ ] Telemetría persistida en `HandRecord.Telemetry` recupera 100 % en query histórica.
- [ ] Stop-loss implementado o documentado como out-of-scope explícito.

---

## 11. Notas

- **Telemetría = contrato:** los nombres de categorías son contrato estable persistido. Renombrarlos sin migración rompe queries históricas. ADR-0017 lo documenta.
- **Sobrestimación de buckets log:** 37 % es alto para SLOs absolutos pero aceptable para detectar regresiones (las regresiones suelen ser 2-3× — visibles a través del bucket log). No usar estos números para reportes de performance externos.
- **Bankroll = trust pillar:** si las cifras son incorrectas (Q-DM-07), el producto pierde credibilidad. Q-DM-07 + Q-DM-06 son blockers críticos del coaching feature.
- **Stop-loss vs auto-pause:** filosofía del producto — el bot NUNCA actúa autónomamente. Stop-loss es una advertencia visual; Pablo decide.
- **Telemetría dual con coaching:** las latencias técnicas (telemetría) ayudan al equipo del producto; el `BBPer100` y bankroll ayudan a Pablo. Ambos consumen `HandRecord` pero responden a personas distintas.
- **Privacidad de telemetría:** las latencias y contadores no contienen información sensible (no aliases, no cards). Seguros para enviar a un dashboard externo o reporting interno sin consideraciones GDPR.
