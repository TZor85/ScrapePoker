# US-06 — Backtest A/B y auto-calibración de estrategia

> **Historia de evolución de estrategia.** Cubre cómo Pablo (o el dev del producto) prueba cambios al `StrategyProfile` antes de aplicarlos en sesión real. El backtest replica decisiones históricas con un profile alternativo; la auto-calibración sugiere ajustes incrementales basados en patrones de divergencia. Sin este flujo, cualquier cambio al motor sería ciego.

---

## 1. Persona

**Pablo** — quiere mejorar su winrate en NL5. Tiene 30 días de manos persistidas (~3000 manos).

**Dev del producto** — está iterando sobre el motor: ajusta un threshold (`Flop_OpenRaise.FoldBelow` de 35 a 32) y quiere validar el impacto antes de pushear a producción.

---

## 2. Historia (formato narrativo)

> **Como** Pablo o el dev,
> **quiero** ejecutar el motor sobre manos pasadas con dos profiles distintos (A vs B) para comparar BB/100 estimado y divergencias por calle, y opcionalmente aceptar sugerencias automáticas de calibración,
> **para** mejorar la estrategia con datos en lugar de intuición, y validar cambios antes de comprometerlos a sesiones en vivo.

---

## 3. Criterios de aceptación — Backtest A/B

### CA-01 — Selección del scope de backtest

**Dado** que Pablo está en pestaña Historial,
**Cuando** presiona "Backtest A/B",
**Entonces** se abre un dialog con:
- Selector de scope: `Una sesión` / `Últimas N sesiones` / `Rango de fechas` / `Todas las manos`.
- Selector de Profile A: por defecto el activo (`appsettings.json`).
- Selector de Profile B: navegar a otro JSON o pegar JSON inline.
- Filtro opcional: situación (`OpenRaise` / `ThreeBet` / etc.), posición, calle.

🟡 **Inferido** del modelo `StrategyBacktester` + botón "Backtest A/B" en `FrmMain`; UI de scope no se confirmó al detalle.

### CA-02 — Replay de decisiones con dos profiles

**Dado** que Pablo confirmó scope + profiles A y B,
**Cuando** `StrategyBacktester.RunBacktestAsync(scope, profileA, profileB)` ejecuta,
**Entonces** para cada `HandRecord` en el scope:
1. Reconstruye el contexto de cada calle (cartas, board, pot, villain bet, oponentes).
2. Invoca el motor con `profileA` → obtiene `decisionA`.
3. Invoca el motor con `profileB` → obtiene `decisionB`.
4. Compara `decisionA` vs `decisionB` y vs `actionTaken` real (registrado en `StreetDecision.ActionTaken`).
5. Acumula deltas de EV estimado (`decisionB.EV - decisionA.EV`).

🟢 Confirmado en `StrategyBacktester.cs` + DD-15 + ADR (no específico).

### CA-03 — Resultado consolidado

**Dado** que el backtest completa sobre N manos,
**Cuando** se renderiza el reporte,
**Entonces** Pablo ve:
- **BB/100 estimado A:** +5.2 BB/100 (intervalo de confianza ±1.8 con N=3000).
- **BB/100 estimado B:** +6.7 BB/100 (intervalo de confianza ±1.9).
- **Diferencia estimada:** +1.5 BB/100 (con N=3000, significancia: ≥80 % confianza).
- **Total divergencias:** 234 / 3000 calles (7.8 %).
- **Distribución de divergencias por:** calle (Flop/Turn/River), situación, posición.
- **Top 10 manos donde A y B más divergen** (link a `FrmHandDetail`).

🟡 **Inferido:** estructura del reporte basada en domain.md y MEMORY.md; UI exacta no confirmada.

### CA-04 — Cobertura del backtest

**Dado** que el motor tiene 10+ paths postflop con state cross-street (`PostflopGameContext`),
**Cuando** el backtest replica una mano,
**Entonces** debe reconstruir fielmente:
- `HeroBetFlop`, `HeroBetTurn` (cross-street aggression).
- `FloatedFlop`, `TurnCalledWithFlushDanger`, `HeroCheckedAllStreets`.
- `IsAnyoneAllIn`.
- `villainBarreling`, `villainCheckedMiddleStreet`.

**Y** si el `HandRecord` no tiene los campos suficientes (sesión vieja con esquema parcial), la mano se omite con flag `IncompleteContext`.

🟡 **Inferido:** `StrategyBacktester` debe reconstruir contexto pero el grado de fidelidad no está documentado al detalle.

### CA-05 — Backtest determinístico (con seed)

**Dado** que el motor usa `Random.Shared.NextDouble()` en 11 callsites de `PostflopDecisionService` (DD-11),
**Cuando** se ejecuta backtest en modo "determinístico",
**Entonces** se inyecta un `IRandomProvider` con seed fija → mismo input produce mismo output across runs,
**Y** Pablo puede reproducir el reporte exacto al re-ejecutar.

🟡 **Inferido / pendiente:** dependiente de Q-DM-10 (`IRandomProvider` inyectable). Hoy NO existe.

### CA-06 — Persistencia del reporte

**Dado** que un backtest produce un reporte de comparación,
**Cuando** Pablo presiona "Guardar reporte",
**Entonces** se persiste en disco (`reports/backtest_2026-05-07_14-30.md`) o en Marten (collection `BacktestReport`),
**Y** Pablo puede comparar reportes a lo largo del tiempo (evolución de su decision quality).

🔴 **Lacuna:** funcionalidad de persistencia del reporte NO confirmada en código; spec pendiente.

---

## 4. Criterios de aceptación — Auto-calibración

### CA-07 — Identificación de patrones de divergencia

**Dado** que el motor tiene ≥30 días de manos con `StreetDecision.ActionTaken != RecommendedAction`,
**Cuando** `AutoCalibrationService.AnalyzeDivergences` ejecuta,
**Entonces** identifica clusters como:
- *"En `Flop_OpenRaise OOP`, Hero foldea 60 % más que el bot recomienda con equity ∈ [30, 35]"* → sugiere subir `FoldBelow` de 35 a 32.
- *"En `Turn_ThreeBet IP`, Hero apuesta menos que el bot recomienda con equity ∈ [50, 60]"* → sugiere bajar `ThinValueAbove` de 50 a 48.

**Y** cada sugerencia incluye: clave afectada, `OldValue`, `NewValue`, evidencia (N divergencias, equity range), confianza estimada.

🟡 **Inferido:** lógica conceptual existe; implementación parcial en `AutoCalibrationService.cs`.

### CA-08 — Preview con BUG `OldValue` hardcoded

**Dado** que `AutoCalibrationService.PreviewAndApply` ejecuta sobre un perfil con `Flop_OpenRaise.FoldBelow = 38` (no 35),
**Cuando** el preview se renderiza,
**Entonces** Pablo ve incorrectamente: `OldValue: 35 → NewValue: 32` (literal hardcoded).
**Y** si Pablo aplica, el JSON se actualiza a `32` perdiendo el `38` original.

🔴 **BUG ACTIVO Q-DM-06:** `Services/AutoCalibrationService.cs:174-208` tiene `OldValue` literales (`45`, `40`, `35`) en lugar de leer de `IOptionsMonitor<StrategyProfile>`. **Bloquea adopción del feature** hasta que se resuelva.

### CA-09 — Aplicación de sugerencia

**Dado** que Pablo acepta una sugerencia (`ThinValueAbove: 50 → 48`),
**Cuando** `AutoCalibrationService.ApplyChange` ejecuta,
**Entonces:**
1. Lee el `appsettings.json` actual.
2. Modifica solo la clave afectada (preserva el resto).
3. Escribe el archivo con backup `appsettings.json.bak.{timestamp}`.
4. Avisa a Pablo: *"Cambio aplicado. Reiniciar app para que el motor use el nuevo valor."* (Q-APP-09 hot reload no funciona hoy).

🟡 **Inferido:** mecanismo de write + backup razonable pero no confirmado al detalle.

### CA-10 — Histórico de calibraciones

**Dado** que Pablo aplicó ≥10 calibraciones en el último mes,
**Cuando** abre pestaña Calibraciones (si existe) o panel agregado,
**Entonces** ve cronología:
- `2026-04-15: Flop_OpenRaise.FoldBelow 38 → 35 (basado en 87 divergencias)`.
- `2026-04-22: Turn_ThreeBet.ThinValueAbove 50 → 48 (basado en 45 divergencias)`.
- ...

**Y** Pablo puede revertir cambios o ver el impacto post-calibración (BB/100 antes vs después).

🔴 **Lacuna:** UI de histórico no confirmada; spec pendiente.

### CA-11 — Confianza mínima para sugerencias

**Dado** que `OpponentTracker` y los datos de divergencia tienen sample sizes mínimos por categoría (Q-DM-S-15, granular reliability),
**Cuando** una sugerencia se basa en <`AutoCalibrationMinSamples=20` divergencias,
**Entonces** se omite del listado (NO se sugiere con datos insuficientes),
**Y** Pablo solo ve sugerencias estadísticamente robustas.

🟡 **Inferido / pendiente:** threshold no confirmado en código.

---

## 5. Diagrama de flujo

```
[Pre: Pablo tiene 30+ días de manos persistidas]


─── BACKTEST A/B ─────────────────────────────────

Pablo presiona "Backtest A/B"
         │
         ▼
Dialog scope + profiles
  ├─ Scope: una sesión / N sesiones / rango / todas
  ├─ Profile A: actual (appsettings.json)
  └─ Profile B: alternativo (otro JSON)
         │
         ▼
StrategyBacktester.RunBacktestAsync
  ├─ Para cada HandRecord en scope:
  │  ├─ Reconstruir contexto (cartas, board, pot, etc.)
  │  ├─ Invocar motor con profileA → decisionA
  │  ├─ Invocar motor con profileB → decisionB
  │  ├─ Comparar A vs B vs ActionTaken real
  │  └─ Acumular deltas EV
  └─ Retorna BacktestReport
         │
         ▼
Reporte UI:
  ├─ BB/100 estimado A vs B (CI ±x)
  ├─ Diferencia con significancia
  ├─ Distribución divergencias por calle/situación/posición
  ├─ Top 10 manos divergentes (link FrmHandDetail)
  └─ [Guardar reporte] (lacuna)


─── AUTO-CALIBRACIÓN ──────────────────────────────

[Background: AutoCalibrationService analiza periódicamente]
         │
         ▼
AnalyzeDivergences (últimos 30 días)
  ├─ Cluster por (situación, posición, calle)
  ├─ Filtrar: N divergencias ≥ AutoCalibrationMinSamples
  └─ Generar sugerencias con OldValue, NewValue, evidencia
         │
         ▼ [Pablo abre pestaña Calibración]
         │
PreviewAndApply
  ├─ Renderiza tabla: clave, OldValue, NewValue, N, confianza
  ├─ ⚠️ BUG Q-DM-06: OldValue hardcoded literal
  └─ Pablo acepta / rechaza por sugerencia
         │
         ▼ [Pablo acepta]
         │
ApplyChange
  ├─ Backup: appsettings.json.bak.<timestamp>
  ├─ Modifica solo la clave afectada
  ├─ Escribe appsettings.json
  └─ Notifica: "Reinicia para aplicar"
         │
         ▼
[Pablo reinicia app — Q-APP-09 hot reload no funciona]
```

---

## 6. Variantes (caminos secundarios)

### V-01 — Backtest sobre dataset insuficiente
- Pablo ejecuta backtest sobre 50 manos.
- Intervalos de confianza muy amplios (±10 BB/100).
- Comportamiento esperado: disclaimer "Dataset pequeño, resultado no significativo".
- Pendiente: definir threshold (sugerencia: `MinHandsForSignificance=500`).

### V-02 — Profile B con clave inválida
- Pablo pega JSON con typo (`"Flop_OpenRase"` en vez de `"Flop_OpenRaise"`).
- `StrategyProfileValidator.Validate(profileB)` lanza.
- Comportamiento esperado: el dialog muestra el error, NO ejecuta backtest.

### V-03 — Backtest con manos `Result = Unknown`
- Manos cerradas durante mano activa (Q-APP-20) tienen decisiones parciales.
- Comportamiento esperado: omitir esas manos del backtest (flag `IncompleteHand`).
- Reporte cuenta: `3000 manos, 12 omitidas (Unknown), 2988 procesadas`.

### V-04 — Auto-calibración con BUG Q-DM-06
- Preview muestra `OldValue` incorrecto.
- Pablo aplica → el cambio sí se aplica al valor real (read-modify-write actual), pero la UI ha mentido.
- Pablo confunde el origen del cambio.
- 🔴 **Activo bug;** mitigación en Q-DM-06.

### V-05 — Pablo aplica calibración, valida con backtest
- Workflow ideal: aplicar sugerencia → ejecutar backtest A=anterior, B=con cambio → validar mejora.
- Hoy: posible pero requiere Pablo guardar manualmente el profile anterior antes de aplicar.

### V-06 — Backtest con `IRandomProvider` no determinístico
- Hoy `Random.Shared` se usa en 11 callsites → backtests no reproducibles bit-a-bit.
- Comportamiento actual: variación ±2-3 BB/100 entre runs sobre mismo dataset.
- Pendiente Q-DM-10: inyectar provider con seed fija.

### V-07 — Backtest cross-stake (manos NL5 + NL10)
- `BB/100` se mantiene neutro (BB unit), agregable cross-stake.
- ✅ Trabajable (DD-15 acumuladores en BB).

### V-08 — Auto-calibración sugiere cambio "exótico"
- Sugerencia: `BoardTextureWetMultiplier: 1.0 → 0.7` (cambio grande).
- Comportamiento esperado: limitar magnitud de cambio (máx ±15 % por iteración) para evitar oscilaciones.
- Pendiente: definir `AutoCalibrationMaxDeltaPct=0.15`.

---

## 7. Métricas de éxito

### Backtest A/B:

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Tiempo de backtest 1000 manos** | <30 s | benchmark |
| **Reproducibilidad** (con seed) | 100 % across runs | tests + Q-DM-10 |
| **Cobertura de paths** | ≥80 % de paths ejercitados en backtest típico | log |
| **Manos omitidas por contexto incompleto** | <5 % | log |
| **Precisión BB/100 estimado vs real** | error <2 BB/100 (cross-validation) | manual |

### Auto-calibración:

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Sugerencias con `OldValue` correcto** | 100 % (Q-DM-06 fixed) | review |
| **Tasa de aceptación de sugerencias** | >40 % (señal de utilidad) | counter |
| **Mejora de BB/100 post-calibración** | promedio +0.5 BB/100 por iteración | comparativo backtests |
| **Sugerencias por mes** | 5-10 (no spam) | log |
| **Reverts** | <10 % de cambios aplicados | counter |

---

## 8. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| Q-DM-06 BUG `OldValue` hardcoded | 🔴 | — | Inyectar `IOptionsMonitor` |
| Backtest no reproducible (Random.Shared) | 🟡 | — | `IRandomProvider` (Q-DM-10) |
| Reconstrucción de contexto incompleta | 🟡 | flag `IncompleteContext` | Validar fidelidad cross-street |
| Sample size insuficiente sin disclaimer | 🟡 | — | Threshold + UI warning |
| Auto-calibración oscilante | 🟡 | — | `MaxDeltaPct` por iteración |
| Persistencia de reporte lacuna | 🟢 | — | Implementar si demand |
| Histórico de calibraciones lacuna | 🟢 | — | Implementar si demand |
| Backtest cruza profile inválido | 🟡 | `StrategyProfileValidator` | Block dialog si validación falla |
| Hot reload no funciona post-apply | 🟡 | requiere reinicio | Q-APP-09 |

---

## 9. Dependencias

**Otras user stories:**
- US-03 genera el dataset de manos.
- US-04 (revisión historial) consume el reporte de backtest.
- US-05 (telemetría/bankroll) puede integrar reportes en pestaña Estadísticas.

**Specs por unit:**
- `OpenScrape.App/` — botón "Backtest A/B" en `FrmMain`, `FrmHandDetail` para drill-down de manos divergentes.
- `OpenScrape.DecisionMaker/` — `StrategyBacktester`, `AutoCalibrationService`, `PostflopDecisionService` (replay), `StrategyAnalyzerService`.
- `OpenScrape.Domain/` — `StrategyProfile`, `StreetThresholds`, `StreetDecision` (replay input).

**Externo:**
- PostgreSQL accesible.
- File system accesible para backups de `appsettings.json`.

---

## 10. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] CA-01 a CA-11 pasan en testing manual.
- [ ] Q-DM-06 BUG `OldValue` hardcoded fixed.
- [ ] Q-DM-10 `IRandomProvider` inyectado → backtest reproducible.
- [ ] Backtest 3000 manos termina en <2 minutos.
- [ ] Auto-calibración produce ≥3 sugerencias razonables tras 30 días de manos.
- [ ] Backup automático de `appsettings.json` antes de aplicar calibración.
- [ ] Tests cubren replay de los 10+ paths postflop.

---

## 11. Notas

- **Backtest = experimentación segura:** el valor del feature es permitir cambios de profile sin "apostar dinero". Es la diferencia entre "probar en live" y "probar en simulación".
- **Auto-calibración requiere madurez de datos:** sin ≥30 días de uso real, las sugerencias son ruido. No prematuramente.
- **Feedback loop coaching ↔ calibración:** Pablo identifica fugas en US-04, las valida en backtest, las cierra con calibración. Tres user stories trabajando juntas.
- **Q-DM-06 es blocker prioritario:** sin él, auto-calibración miente. Si solo hay capacidad para 1 fix antes de release, este es.
- **Backtest cross-stake = unidad neutra:** BB/100 permite consolidar resultados de NL5 + NL10 + NL2. Una decisión arquitectónica explícita.
- **Disponibilidad de datos:** la calidad del backtest depende del esquema completo de `HandRecord`. Manos viejas con esquema parcial (`StreetDecision.Reason` faltante, etc.) se omiten — documentar la versión del esquema y migración si necesaria.
