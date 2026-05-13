# US-04 — Revisión de historial: post-sesión, coaching, divergencia

> **Historia de coaching post-sesión.** Cubre lo que Pablo hace tras cerrar una sesión: abre la pestaña Historial, navega entre sesiones, examina manos puntuales, identifica decisiones donde divergió del bot y aprende. Sin este flujo, los `HandRecord` persistidos serían datos muertos.

---

## 1. Persona

**Pablo** — terminó una sesión de 4 h hace 30 minutos. Ahora está fresco, café en mano, con el log abierto.
- Tiene apetito de revisar 10-20 manos clave (las que perdió grande, las que le hicieron dudar).
- No quiere reabrir el cliente — quiere ver Hand History sin reconstruir el juego.
- Su rutina de coaching: identificar 2-3 patrones de fuga por sesión.

---

## 2. Historia (formato narrativo)

> **Como** Pablo,
> **quiero** revisar mi historial de sesiones, abrir manos individuales con su Hand History coloreada, y ver dónde mi acción difirió de la recomendación del bot,
> **para** identificar patrones de fuga, validar si el bot acertó en spots difíciles, y mejorar mi juego semana a semana.

---

## 3. Criterios de aceptación

### CA-01 — Listado de sesiones recientes

**Dado** que Pablo abre la pestaña Historial,
**Cuando** `dgvSessions` se carga,
**Entonces** `GameLoggerService.GetRecentSessionsWithStatsAsync(top: 50)` retorna las 50 sesiones más recientes,
**Y** Pablo ve filas con: `StartTime` (DESC), `TableName`, `EndTime`, `TotalHands`, `TotalProfit`, `BBPer100`, `Duration`,
**Y** las sesiones aún en progreso (`EndTime IS NULL`) aparecen con marca visual distintiva (ej. fondo amarillo).

🟢 Confirmado en `GameLoggerService.GetRecentSessionsWithStatsAsync` + `FrmMain` Historial tab.

### CA-02 — Carga de manos por sesión

**Dado** que Pablo hace doble clic en una sesión de `dgvSessions`,
**Cuando** `GetHandsForSessionAsync(sessionId)` se ejecuta,
**Entonces** `dgvSessionHands` se popula con todas las manos de esa sesión ordenadas por `HandNumber ASC`,
**Y** cada fila muestra: `HandNumber`, `StartTime`, `Position`, `HeroCards`, `BoardCards`, `Result`, `Profit`, `OpponentsCount`, `Situation`,
**Y** Pablo puede filtrar/ordenar por columnas (sort en `DataGridView`).

🟢 Confirmado en `GameLoggerService.GetHandsForSessionAsync`.

### CA-03 — Detalle de mano en `FrmHandDetail`

**Dado** que Pablo hace doble clic sobre una mano en `dgvSessionHands`,
**Cuando** `FrmHandDetail.ShowHand(handRecord)` se invoca,
**Entonces** se abre un popup con `RichTextBox` que muestra Hand History coloreada:

```
═════════════════════════════════════════════════════════
  Hand #1234 — UTC 2026-05-07 14:30 — Table NL5_Cash_01
  Hero @ BTN — Stack: 100.0 BB → 108.5 BB
═════════════════════════════════════════════════════════

  HOLE CARDS: [Ah Kh]                      ← verde, primary

  PREFLOP (pot 1.5 BB):
  ├─ Hero raises 2.5 BB
  ├─ Villain (CO) calls 2.5 BB
  └─ Situation: OpenRaise

  FLOP [Qd 7c 2s] (pot 5.5 BB):
  ├─ Equity: 42.3 % | PotOdds: -- | EV: +1.2
  ├─ Recommended: BET 3.5 BB         (Reason: ValueBet:OnePair:CO)
  ├─ Action taken: BET 3.5 BB        ✅ match
  └─ Villain calls 3.5 BB

  TURN [Jh] (pot 12.5 BB):
  ├─ Equity: 58.7 % | PotOdds: -- | EV: +3.4
  ├─ Recommended: BET 7.0 BB         (Reason: ValueBet:TwoPair:Wet)
  ├─ Action taken: CHECK             ❌ DIVERGENCE
  └─ Villain bets 5.0 BB → Hero calls

  RIVER [3d] (pot 22.5 BB):
  ├─ Equity: 71.2 % | PotOdds: 27 % | EV: +5.1
  ├─ Recommended: BET 12.0 BB        (Reason: ValueBet:TwoPair:Polarized)
  ├─ Action taken: BET 12.0 BB       ✅ match
  └─ Villain folds

═════════════════════════════════════════════════════════
  RESULT: Won +8.5 BB                      ← verde si Won
═════════════════════════════════════════════════════════
```

🟢 Confirmado en `FrmHandDetail.cs` + esquema `StreetDecision`.

### CA-04 — Marcado visual de divergencia bot vs hero

**Dado** que una `StreetDecision` tiene `RecommendedAction != ActionTaken`,
**Cuando** `FrmHandDetail` la renderiza,
**Entonces** la línea muestra **❌ DIVERGENCE** en color rojo,
**Y** el equity y reason permiten a Pablo evaluar si SU decisión fue mejor o peor que la del bot,
**Y** las divergencias se cuentan en una métrica al pie del popup (`Divergencias: 2/4 calles`).

🟢 Confirmado en design.md `OpenScrape.App` + DD relacionada.

### CA-05 — Filtros y búsqueda en historial

**Dado** que Pablo busca solo manos perdidas grandes (≥10 BB),
**Cuando** aplica filtro `Result = Lost AND Profit <= -10`,
**Entonces** `dgvSessionHands` filtra in-memory (LINQ sobre las manos cargadas),
**Y** Pablo puede combinar múltiples filtros: `Position = BTN`, `Situation = ThreeBet`, etc.

🟡 **Inferido** del modelo de `DataGridView`; sin spec explícita de filtros UI.

### CA-06 — Exportación de hand history (formato HM/PT)

**Dado** que Pablo quiere importar las manos a Holdem Manager o PokerTracker,
**Cuando** presiona "Exportar" sobre una sesión,
**Entonces** `GameLoggerService.ExportToHmFormatAsync(sessionId, outputPath)` genera un archivo `.txt` con el formato estándar de hand history,
**Y** el archivo es importable por HM4/PT4 sin errores.

🔴 **Lacuna:** funcionalidad NO confirmada en el código actual; podría ser feature futura. Documentar como spec pendiente.

### CA-07 — Estadísticas agregadas en pestaña Estadísticas

**Dado** que Pablo va a la pestaña Estadísticas (si existe) o a un panel agregado en Historial,
**Cuando** `StrategyAnalyzerService.GetAggregatedStats` retorna,
**Entonces** Pablo ve:
- VPIP / PFR / 3Bet% del hero (calculadas desde `HandRecord.Decisions`).
- Distribución de resultados por situación: `OpenRaise: Won 60% / Lost 30% / Push 10%`.
- BB/100 por posición.
- Heatmap de fugas: `Position × Street × Action` con BB/100 negativo.

🟡 **Inferido:** `StrategyAnalyzerService.cs` existe pero el coaching panel UI puede estar parcial. ADR-0017 y BUG Q-DM-07 (BigBlind hardcoded) afectan la fiabilidad de estos números.

### CA-08 — Auto-calibración basada en divergencias acumuladas

**Dado** que Pablo divergió del bot en >30 % de las decisiones de "Bet Turn OOP",
**Cuando** `AutoCalibrationService.PreviewAndApply` analiza los patrones,
**Entonces** sugiere ajuste a `StrategyProfile` (ej. *"Reducir ThinValueAbove en Turn_OpenRaise de 45 a 42 — basado en 87 divergencias últimos 30 días"*),
**Y** Pablo puede aceptar el cambio o rechazarlo,
**Y** si acepta, el JSON se actualiza y el motor usa los nuevos thresholds tras reinicio (Q-APP-09).

🟡 **Comportamiento parcial:** `AutoCalibrationService` existe pero tiene BUG Q-DM-06 (`OldValue` hardcoded en `:174-208`) → preview muestra valores incorrectos hasta el fix.

### CA-09 — Persistencia robusta de la query histórica

**Dado** que Pablo abre Historial con 200+ sesiones acumuladas,
**Cuando** `GetRecentSessionsWithStatsAsync(top: 50)` se ejecuta,
**Entonces** Marten retorna en <500 ms con índice sobre `StartTime DESC`,
**Y** la query NO carga las `Hands: List<HandRecord>` embebidas (lazy — solo al pedir detalle).

🟡 **Inferido:** índice sobre `StartTime` es razonable pero NO confirmado en `Services.cs` de Infrastructure.

### CA-10 — Privacidad / GDPR de aliases de oponentes

**Dado** que las manos persisten con aliases OCR de oponentes (`"Bob"`, `"Alice"`, etc.),
**Cuando** Pablo exporta o comparte hand history,
**Entonces** los nombres deberían anonimizarse a `player1, player2, ...` (Q-PERM-DATA-02 raíz),
**Y** Pablo puede ver pero no exportar los aliases reales.

🔴 **Lacuna:** no confirmado en código; la decisión de anonimizar está en `questions.md` raíz pero la implementación no se localizó.

---

## 4. Diagrama de flujo (revisión)

```
[Pre: Pablo cerró sesión, abre app de nuevo o ya está abierta]
         │
         ▼
Pablo va a pestaña Historial
         │
         ▼
GetRecentSessionsWithStatsAsync(top: 50)
  ├─ Marten query con índice StartTime DESC
  ├─ Sin cargar Hands embebidas (lazy)
  └─ retorna DTO ligero
         │
         ▼
dgvSessions populated
  ├─ filas: SessionId, TableName, StartTime, EndTime, Hands, Profit, BBPer100
  └─ Pablo ordena/filtra por columnas
         │
         ▼ [Pablo doble clic en sesión]
         │
GetHandsForSessionAsync(sessionId)
  ├─ Marten query: WHERE GameSessionId = X
  └─ Retorna List<HandRecord>
         │
         ▼
dgvSessionHands populated
  ├─ filas: HandNumber, StartTime, Position, HeroCards, Board, Result, Profit
  └─ Pablo ordena/filtra
         │
         ▼ [Pablo doble clic en mano]
         │
FrmHandDetail.ShowHand(handRecord)
  ├─ RichTextBox con Hand History coloreada
  ├─ Por cada StreetDecision:
  │  ├─ Equity, PotOdds, EV
  │  ├─ Recommended vs ActionTaken
  │  └─ DIVERGENCE marker si difieren
  └─ Resultado final con color
         │
         ▼ [Opcional: Pablo aplica auto-calibración]
         │
AutoCalibrationService.PreviewAndApply
  ├─ Analiza divergencias últimos 30 días
  ├─ Sugiere ajustes a thresholds
  ├─ Pablo acepta/rechaza
  └─ Update appsettings.json
         │
         ▼
[Pablo cierra Historial; al relanzar app, motor usa nuevos thresholds]
```

---

## 5. Variantes (caminos secundarios)

### V-01 — Sesión muy larga abierta en Historial (1000+ manos)
- `GetHandsForSessionAsync` retorna 1000 docs.
- Carga puede ser lenta (varios segundos) sin paginación.
- Mitigación pendiente: paginación con `Skip`/`Take` Marten o lazy DataGridView.

### V-02 — Mano con `Result = Unknown` (cierre durante mano)
- Decisión Q-APP-20 sobre cómo distinguir visualmente:
  - Color gris oscuro / icono "incompleto".
  - Excluida de stats `BBPer100`.
  - Click muestra Hand History parcial (solo decisiones tomadas hasta el cierre).

### V-03 — Auto-calibración con BUG Q-DM-06
- Preview muestra `OldValue: 45 → NewValue: 42` cuando en realidad el profile activo tiene `45.5`.
- Pablo aplica un cambio basado en valor incorrecto.
- 🔴 **Activo bug;** mitigación en Q-DM-06.

### V-04 — Privacidad: Pablo comparte mano en foro de coaching
- Si exporta hand history sin anonimizar → aliases reales de oponentes en plain text.
- Pendiente Q-PERM-DATA-02 raíz.

### V-05 — Backtest A/B desde Historial
- Pablo selecciona una sesión de 200 manos y presiona "Backtest A/B".
- `StrategyBacktester.RunBacktestAsync(sessionId, profileA, profileB)` replica las decisiones con dos profiles.
- Resultado: tabla comparativa con `BB/100 (A) vs BB/100 (B)`, divergencias por calle, ejemplo de las 10 manos donde más difieren.

🟢 Confirmado en `StrategyBacktester.cs` + `FrmMain` botón Backtest A/B.

### V-06 — Exportar a HM4/PT4 (lacuna CA-06)
- Si la funcionalidad NO existe, Pablo tiene que copiar manualmente.
- Spec futura.

### V-07 — Marten con datos corruptos (esquema viejo)
- Pablo upgradea la app y `HandRecord` schema cambió.
- Marten rechaza o reescribe automáticamente (depende de policy `AutoCreate.CreateOrUpdate`).
- Pendiente: documentar política de migración de schema.

### V-08 — Búsqueda full-text de manos
- Pablo quiere "todas las manos donde tuve QQ vs 3-bet OOP".
- Hoy: filtro manual en DataGridView.
- Spec futura: query Marten con criterios estructurados.

---

## 6. Métricas de éxito

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Tiempo de carga de pestaña Historial** | <500 ms con 50 sesiones | log |
| **Tiempo de apertura de FrmHandDetail** | <200 ms | log |
| **Cobertura visual de StreetDecision** | 100 % de campos rendered | review manual |
| **Tasa de identificación de fugas semanal** | ≥2 patrones identificados | feedback Pablo |
| **Tasa de aceptación de auto-calibraciones** | >50 % (señal de que las sugerencias son razonables) | counter en `AutoCalibrationService` |
| **Latencia de query histórica con 1000 manos** | <2 s | log |

---

## 7. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| `AutoCalibration` BUG `OldValue` hardcoded | 🔴 | — | Inyectar `IOptionsMonitor<StrategyProfile>` (Q-DM-06) |
| `Exploitability BB/100` con `BigBlind=1.0` hardcoded | 🔴 | — | Parametrizar (Q-DM-07) |
| Privacidad aliases sin anonimizar | 🟡 | — | Implementar anon mapping (Q-PERM-DATA-02) |
| Sesión >1000 manos lenta | 🟡 | — | Paginación |
| Mano `Unknown` confunde sin distinción | 🟢 | persiste con flag implícito | UI distinción (Q-APP-20) |
| Schema migration sin política | 🟡 | Marten `CreateOrUpdate` automático | Documentar policy + tests de migración |
| Export HM4/PT4 lacuna | 🟢 | — | Implementar si demand exists |
| Coaching divergence sin tracking explícito | 🟡 | datos en `StreetDecision` | UI dedicada con stats agregadas |

---

## 8. Dependencias

**Otras user stories:**
- US-03 (sesión) genera los datos consumidos aquí.
- US-05 (telemetría/bankroll) puede compartir el panel de Estadísticas.

**Specs por unit:**
- `OpenScrape.App/` — `FrmHandDetail`, `dgvSessions`/`dgvSessionHands` en `FrmMain`, `StrategyBacktester`, `AutoCalibrationService` (consumido vía interface en App).
- `OpenScrape.DecisionMaker/` — `StrategyAnalyzerService`, `ExploitabilityCalculator`, `StrategyBacktester`, `AutoCalibrationService`.
- `OpenScrape.Domain/` — `GameSession`, `HandRecord`, `StreetDecision`, `HandResult`.
- `OpenScrape.Features/` — `GameRoundUseCases`, `GetRecentGameRounds`.

**Externo:**
- PostgreSQL accesible para queries históricas.
- Sesiones previas persistidas (precondición; sesión cero = pestaña Historial vacía).

---

## 9. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] CA-01 a CA-10 pasan en testing manual con datasets de ≥30 sesiones reales.
- [ ] FrmHandDetail muestra Hand History coloreada con todos los campos de `StreetDecision`.
- [ ] Divergencias bot/hero se distinguen visualmente.
- [ ] AutoCalibration BUG Q-DM-06 fixed → preview muestra `OldValue` real.
- [ ] Exploitability `BigBlind` parametrizado (Q-DM-07).
- [ ] Privacidad: aliases anonimizados al exportar (Q-PERM-DATA-02).
- [ ] Sesión >1000 manos carga en <2 s con paginación.

---

## 10. Notas

- **Coaching workflow real:** Pablo usa este flujo no para validar el bot, sino para validarse a sí mismo. La pregunta clave es siempre *"¿yo erré o el bot erró?"*. Por eso CA-04 (marcado de divergencia) es tan importante: es el detonador del aprendizaje.
- **Auto-calibración bidireccional:** las divergencias acumuladas pueden ajustar el bot (auto-calibración) o ajustar a Pablo (coaching). Hoy solo la primera dirección está parcialmente implementada.
- **Datos sensibles:** los `HandRecord` contienen información que en algunas jurisdicciones puede ser regulada (poker en línea + apuestas). Documentar explícitamente que la BD del usuario NO se comparte con servicios externos.
- **Backtest A/B distinto a coaching:** el backtest aplica un nuevo profile a manos pasadas para estimar mejora; el coaching examina decisiones individuales para identificar fugas. Ambos consumen el mismo dataset pero responden preguntas distintas.
- **Q-DM-06 / Q-DM-07 son blockers de coaching:** sin esos fixes, las métricas de coaching son cuestionables. Priorizarlos antes de marketear el coaching feature.
