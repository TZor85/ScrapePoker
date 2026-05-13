# US-03 — Sesión de juego: lifecycle, tracking, persistencia

> **Historia de gestión de sesión.** Envuelve a US-01 (captura/decisión por mano) con el ciclo de vida superior: cuándo empieza una sesión, qué se acumula, cuándo se persiste, cómo se cierra. Sin este flujo no hay stats, ni `BBPer100`, ni Historial — el bot decidiría manos en el vacío.

---

## 1. Persona

**Pablo** — operando 4 mesas durante 5 horas, mezcla de NL5 y NL10.
- Ya completó US-02 (onboarding); regions calibradas.
- Espera ver al final de la sesión: cuántas manos jugó, cuál fue su `BBPer100`, qué decisiones tomó por calle, y poder revisar manos puntuales en Historial.
- Si su sesión muere por crash, espera mínima pérdida de datos.

---

## 2. Historia (formato narrativo)

> **Como** Pablo,
> **quiero** que el bot agrupe automáticamente todas las manos que juego en una mesa en una "sesión", calcule mis stats acumuladas, y persista cada mano para que pueda revisarla luego,
> **para** medir mi performance, identificar fugas y cumplir con mi rutina de coaching post-sesión.

---

## 3. Criterios de aceptación

### CA-01 — Inicio de sesión al primer ciclo de captura

**Dado** que Pablo presiona "Iniciar captura" tras seleccionar la ventana,
**Cuando** el primer ciclo del `BackgroundWorker` detecta hero presente en la mesa (player con alias y stack válido),
**Entonces** `GameLoggerService.StartSessionAsync(sessionId, tableName, bigBlind)` ejecuta:
1. Genera `sessionId: Guid.NewGuid()`.
2. Crea `_currentSession = new GameSession { SessionId, TableName, StartTime: DateTime.UtcNow, BigBlind, Hands: [], EndTime: null }`.
3. Resetea `_sessionTotalHands = 0`, `_sessionTotalProfit = 0m` (Interlocked).
4. Persiste el `GameSession` con `Result = InProgress` (placeholder hasta `EndSession`).

🟢 Confirmado en `GameLoggerService.cs:65-80` + DD-15.

### CA-02 — Detección de nueva mano (consenso de 7 indicadores)

**Dado** que el bot está en una sesión activa,
**Cuando** ocurre un cambio de mano en el cliente (showdown anterior cerrado, nueva mano repartida),
**Entonces** `DetectNewHand` evalúa 7 indicadores:
1. `handNumber` distinto del anterior.
2. Cartas hero distintas.
3. Pot reseteado (vuelve a 1.5 BB en preflop).
4. Board vacío (sin cartas comunitarias).
5. `currentDealer != previousDealer`.
6. `currentSB != previousSB`.
7. `currentBB != previousBB`.

**Y** si ≥4 indicadores activan, se considera nueva mano,
**Y** `_contextHolder.StartNewHand()` resetea `PostflopGameContext` incondicionalmente,
**Y** `GameLoggerService.StartNewHandAsync(handNumber, position, heroStackStart, heroCards)` crea `_currentHand`.

🟢 Confirmado en R-05 + `FrmMain.DetectNewHand`.

### CA-03 — Logging de decisión por calle

**Dado** que el bot toma una decisión postflop en flop/turn/river,
**Cuando** se ejecuta el path correspondiente en `PostflopDecisionService`,
**Entonces** `GameLoggerService.LogStreetDecision(streetDecision)` añade un `StreetDecision` al `_currentHand.Decisions`:
- `Street: Flop|Turn|River`.
- `EquityPercent`, `PotOddsPercent`, `ExpectedValue`.
- `RecommendedAction`, `ActionTaken` (puede diferir si Pablo override manualmente).
- `PotSizeAtDecision`, `BetSize`.
- `Situation`, `IsInPosition`.
- Opcionales: `Reason`, `BoardTexture`, `TotalOuts`, `SPR`.

**Y** la decisión queda en memoria hasta `EndHand` (no persistida individualmente).

🟢 Confirmado en `StreetDecision` record + `GameLoggerService.LogStreetDecision`.

### CA-04 — Cierre de mano con cálculo de profit

**Dado** que la mano termina (showdown, fold del hero, all-in resuelto),
**Cuando** Pablo o el cliente cierran la mano,
**Entonces** `GameLoggerService.EndHand(prevHeroStack: _heroStackPreRebuy > 0 ? _heroStackPreRebuy : _playerGameState.HeroStack)` ejecuta:
1. Calcula `profit = currentHeroStack - prevHeroStack` (con compensación auto-rebuy DD-06).
2. Determina `Result: Won | Lost | Push | Unknown` según signo + reset.
3. Marca `_currentHand.EndTime = DateTime.UtcNow`.
4. `Interlocked.Increment(ref _sessionTotalHands)`.
5. `Interlocked.Add(ref _sessionTotalProfitCents, (long)(profit * 100))` (decimal a long para Interlocked).

**Y** `await FinalizeAndPersistHandAsync()` persiste `HandRecord` independiente en Marten,
**Y** `_currentSession.Hands` mantiene solo las últimas `MaxHandsInMemory=20` (FIFO).

🟢 Confirmado en `GameLoggerService.EndHand` + DD-15.

### CA-05 — Truncado en memoria + acumuladores como fuente de verdad

**Dado** que Pablo lleva 250 manos en la sesión,
**Cuando** consulta los stats actuales,
**Entonces:**
- `_sessionTotalHands == 250` (Interlocked, fuente de verdad).
- `_sessionTotalProfit == sum(hand.Profit for hand in all 250 hands)` (Interlocked).
- `_currentSession.Hands.Count == 20` (las 20 más recientes; las 230 anteriores ya persistidas individualmente).
- `BBPer100 = (_sessionTotalProfit / _sessionTotalHands * 100) / bigBlind` calculado on-demand.

🟢 Confirmado en DD-15 + `GameSession.BBPer100`.

### CA-06 — Visualización de stats en pestaña Historial

**Dado** que Pablo va a pestaña Historial,
**Cuando** `dgvSessions` se popula vía `GameLoggerService.GetRecentSessionsWithStatsAsync`,
**Entonces** ve filas con: `SessionId`, `TableName`, `StartTime`, `EndTime`, `TotalHands`, `TotalProfit`, `BBPer100`,
**Y** ordenadas por `StartTime DESC` (más reciente arriba),
**Y** al hacer doble clic sobre una sesión, `dgvSessionHands` se popula con las manos de esa sesión vía `GetHandsForSessionAsync(sessionId)`.

🟢 Confirmado en `GameLoggerService.GetRecentSessionsWithStatsAsync`.

### CA-07 — Detalle de mano en `FrmHandDetail`

**Dado** que Pablo hace doble clic sobre una mano en `dgvSessionHands`,
**Cuando** `FrmHandDetail` se abre,
**Entonces** ve un `RichTextBox` con Hand History coloreada:
- Header: `Hand #1234 — UTC 2026-05-07 14:30 — BTN`.
- Hero cards: `[Ah Kh]` en color verde.
- Board: `Flop [Qd 7c 2s]` `Turn [Jh]` `River [3d]`.
- Cada `StreetDecision` con su acción, equity, pot odds, EV.
- Resultado: `Won 12.5 BB` (verde) / `Lost 8.0 BB` (rojo) / `Push` (gris).

🟢 Confirmado en `FrmHandDetail.cs` (RichTextBox).

### CA-08 — Cierre de sesión al cerrar app o cambiar mesa

**Dado** que Pablo presiona Alt+F4 o cambia de mesa (nuevo `_handle`),
**Cuando** `FrmMain_FormClosing:304` o `OnTableChange` se ejecuta,
**Entonces:**
1. `_uiSyncService.Detach()`.
2. `_gameLoopCts.Cancel()`.
3. `await _gameLoopCoordinator.StopAsync()` (si activo, hoy dormant Q-APP-04).
4. `_currentSession.EndTime = DateTime.UtcNow`.
5. `await _gameLoggerService.SaveSessionAsync()` persiste el `GameSession` con `EndTime` populated y todos los acumuladores.
6. `await host.DisposeAsync()` libera DI graph y conexiones Marten.

🟢 Confirmado en `FrmMain_FormClosing:304` + EC-08.

### CA-09 — Cleanup defensivo de mano activa al cerrar

**Dado** que Pablo cierra la app durante el flop (mano no completada),
**Cuando** se ejecuta el cleanup,
**Entonces** `_currentHand` se persiste con:
- `Result = Unknown`.
- `EndTime = DateTime.UtcNow`.
- `Decisions` parciales (las tomadas hasta el momento).

**Y** Pablo puede ver en pestaña Historial la sesión cerrada con N manos `Won/Lost` + 1 mano `Unknown` (decisión Q-APP-20).

🟡 Comportamiento actual; UX final pendiente de Q-APP-20.

### CA-10 — Persistencia robusta ante outage de Marten

**Dado** que PostgreSQL está apagado o desconectado durante `await SaveChangesAsync`,
**Cuando** Marten lanza `NpgsqlException`,
**Entonces** `GameLoggerService` captura la excepción internamente, loggea el error, y libera el `_dbWriteLock: SemaphoreSlim`,
**Y** la mano se pierde silenciosamente,
**Y** `_sessionTotalHands` (Interlocked en memoria) divergente del recuento real persistido.

🟡 **Comportamiento actual; mitigación pendiente:** cola de fallback en disco (Q-APP-12).

---

## 4. Diagrama de flujo (lifecycle)

```
[Pre: US-02 completado, ventana seleccionada, regions cargadas]
         │
         ▼
Pablo presiona "Iniciar captura"
         │
         ▼
Primer ciclo BG worker detecta hero
         │
         ▼
GameLoggerService.StartSessionAsync
  ├─ sessionId = Guid.NewGuid
  ├─ _currentSession = new GameSession
  ├─ _sessionTotalHands = 0 (Interlocked)
  ├─ _sessionTotalProfit = 0 (Interlocked)
  └─ Marten persist (placeholder InProgress)
         │
         ▼
┌─── LOOP por cada mano ─────────────────────────┐
│                                                │
│  DetectNewHand (7 indicadores, ≥4 activan)     │
│  ├─ no nueva → siguiente tick (US-01)          │
│  └─ nueva   ▼                                  │
│                                                │
│  _contextHolder.StartNewHand                   │
│  GameLoggerService.StartNewHandAsync           │
│  ├─ _currentHand = new HandRecord              │
│  ├─ heroCards, position, heroStackStart        │
│  └─ Decisions: []                              │
│                                                │
│  ┌─── LOOP por cada calle ───────┐             │
│  │ US-01 toma decisión postflop  │             │
│  │ LogStreetDecision (in-memory) │             │
│  └────────────────────────────────┘             │
│                                                │
│  EndHand(prevHeroStack)                        │
│  ├─ profit = current - prev (auto-rebuy comp)  │
│  ├─ Result: Won/Lost/Push/Unknown              │
│  ├─ Interlocked.Increment(_sessionTotalHands)  │
│  ├─ Interlocked.Add(_sessionTotalProfit, profit)│
│  ├─ FinalizeAndPersistHandAsync (Marten)       │
│  └─ _currentSession.Hands FIFO trim to 20      │
│                                                │
└────────────────────────────────────────────────┘
         │
         ▼ [Pablo cierra app o cambia mesa]
         │
FrmMain_FormClosing
  ├─ _uiSyncService.Detach
  ├─ _gameLoopCts.Cancel
  ├─ _currentSession.EndTime = UtcNow
  ├─ SaveSessionAsync (final state)
  └─ host.DisposeAsync
         │
         ▼
[Pablo abre Historial → ve sesión cerrada con stats]
```

---

## 5. Variantes (caminos secundarios)

### V-01 — Sesión muy larga (1000+ manos, 6 h)
- `_currentSession.Hands` mantiene solo 20; las anteriores ya persistidas individualmente.
- `_sessionTotalHands` (Interlocked) refleja 1000 correctamente.
- `BBPer100` calculado correctamente desde acumuladores.
- Pestaña Historial detalle abre `GetHandsForSessionAsync` que queries todas (puede ser lento — query N=1000).

🟢 EC-19 validado.

### V-02 — Cross-mesa con cambio en sesión activa
- Pablo cierra mesa A (87 BB final), abre mesa B sin reiniciar app.
- Comportamiento: `FrmMain_FormClosing` NO ejecuta (la app sigue abierta), pero `OnTableChange` debería:
  - Cerrar `_currentSession` de mesa A con `EndTime`.
  - Iniciar nueva `_currentSession` para mesa B.
  - Resetear `_heroStackPreRebuy = 0`.

🟡 **Limitación:** no se localizó handler explícito de `OnTableChange` en el código analizado. Posible gap.

### V-03 — Auto-rebuy del cliente (stack salta de 7 BB a 100 BB)
- `_heroStackPreRebuy = 7` queda preservado.
- Próxima mano: `EndHand(prevHeroStack: 7)` calcula profit correcto.
- ✅ DD-06 validado.

### V-04 — App cerrada durante mano activa
- `_currentHand != null`; cleanup persiste con `Result = Unknown`.
- Pablo ve en Historial mano "fantasma" con decisiones parciales.
- Decisión Q-APP-20: ¿persistir, descartar o flag separado?

### V-05 — Marten outage durante persist
- Mano se pierde; acumuladores divergentes.
- Pablo no ve indicador en UI (Q-APP-12).

### V-06 — `DetectNewHand` falla (handNumber no cambió por OCR malo)
- Indicadores insuficientes (<4 activan); el bot cree que sigue la misma mano.
- `_contextHolder.Current` mantiene state de mano vieja → decisiones erradas en mano nueva (EC-12).
- Mitigación parcial: reset defensivo si `IsAnyoneAllIn && boardCards.Count == 0`.

### V-07 — Pablo override manual (decisión bot vs decisión humana)
- Bot recomienda "BET 5.4 BB"; Pablo decide "Check" en cliente.
- `StreetDecision.RecommendedAction = "Bet"` y `ActionTaken = "Check"` → divergence detectada.
- Coaching post-sesión: Pablo ve su tasa de match con el bot (Q-DOM-07 raíz).

🟡 Inferido del modelo `StreetDecision`; sin spec explícita de coaching divergence.

### V-08 — Sesión sin ninguna mano completada (Pablo se levanta, vuelve, cierra)
- `_sessionTotalHands == 0` al cierre.
- `_currentSession` persiste con `Hands: []`, `EndTime`, stats nulas.
- Pestaña Historial muestra sesión "vacía" — informativa, no error.

---

## 6. Métricas de éxito

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Tasa de manos persistidas** | >99.5 % | `_sessionTotalHands` vs `COUNT(hand_record)` |
| **Drift de acumuladores** | <0.5 % | comparar `_sessionTotalProfit` vs SUM Marten |
| **Tiempo de start session** | <100 ms | log |
| **Tiempo de end hand + persist** | <200 ms p95 | `MetricsCollector` (no instrumentado actualmente) |
| **Tiempo de close session** | <2 s incluyendo `host.DisposeAsync` | log |
| **Sesiones huérfanas** (`EndTime == null` al abrir app) | 0 | manual / health check |
| **Manos `Unknown`** (cierre durante mano) | <1 % por sesión normal | `WHERE Result = Unknown` |

---

## 7. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| Crash sin cleanup → sesión sin `EndTime` | 🟡 | `FormClosing` cleanup explícito | Health check al arranque que cierra sesiones huérfanas |
| Marten outage → manos perdidas | 🟡 | try/catch loggea | Cola fallback (Q-APP-12) |
| `DetectNewHand` falso negativo → state leak | 🟡 | reset incondicional al detect | Reset defensivo extra (EC-12) |
| Acumuladores divergentes | 🟡 | Interlocked thread-safe | Reconciliación periódica vs Marten |
| Cross-mesa sin handler | 🟡 | desconocido (gap) | Validar y especificar V-02 |
| Sesión con mano `Unknown` confunde Pablo | 🟢 | persiste con flag implícito | UI distinción visual (Q-APP-20) |
| Multi-mesa: 4 instancias = 4 sesiones | 🟡 | cada instancia gestiona su propia sesión | Documentar limitación |

---

## 8. Dependencias

**Otras user stories:**
- US-01 (captura/decisión) ocurre dentro de cada mano de US-03.
- US-04 (revisión historial) consume los datos persistidos por US-03.
- US-05 (telemetría/bankroll) agrega datos de US-03 cross-sesión.

**Specs por unit:**
- `OpenScrape.App/` — `GameLoggerService`, `FrmMain_FormClosing`, `FrmHandDetail`, `_heroStackPreRebuy`.
- `OpenScrape.Domain/` — `GameSession`, `HandRecord`, `StreetDecision`, `HandResult`, `Position`.
- `OpenScrape.Features/` — `GameRoundUseCases`, `GetRecentGameRounds`.
- `OpenScrape.Infrastructure/` — Marten document store + indices.

**Externo:**
- PostgreSQL accesible durante toda la sesión (sin outages > duración del retry interno de Marten).

---

## 9. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] CA-01 a CA-10 pasan en testing manual con sesión real.
- [ ] Una sesión de 50 manos persiste 100 % (no hay drift entre `_sessionTotalHands` y `COUNT(hand_record)`).
- [ ] Auto-rebuy detection funciona en sesión con ≥3 rebuys.
- [ ] Cleanup en `FormClosing` produce sesión cerrada con `EndTime` populated.
- [ ] Cross-mesa (V-02) tiene handler explícito documentado.
- [ ] Outage de Marten no pierde manos (Q-APP-12 implementada).
- [ ] Manos `Unknown` tienen distinción visual en Historial (Q-APP-20).

---

## 10. Notas

- **Sesión = unit de análisis:** Pablo razona en términos de sesión ("hoy jugué 4 horas de NL5"). El `GameSession` document es la unidad lógica que vincula manos individuales a una intención.
- **Stats efímeras vs persistidas:** los acumuladores Interlocked viven en memoria pero se re-derivan al cargar `GameSession` desde Marten (los campos `TotalHands`, `TotalProfit` ya están persistidos).
- **Tracking cross-sesión:** ojo que `OpponentTracker` NO se persiste cross-sesión hoy (Q-FSM-02 raíz). Los profiles de oponentes se pierden al cerrar — limitación importante para análisis longitudinal.
- **`MaxHandsInMemory=20` justifica la eficiencia:** 20 × ~10 KB = 200 KB es el techo de memoria de la lista in-memory; las queries históricas leen Marten on-demand. Decisión arquitectónica DD-15.
- **Telemetría dual:** la mano persiste tanto el `StreetDecision` (decisión derivada) como el `HandRecord.Telemetry` (cargas latencia/contadores). Útil para coaching y para diagnóstico técnico — vale documentar dónde vive cada uno (cubierto en US-05).
