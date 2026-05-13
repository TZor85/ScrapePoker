# US-01 — Flujo principal: captura → decisión → recomendación

> **Historia primaria del producto.** Cubre el ciclo completo que ocurre cada 100 ms mientras el bot está activo: capturar la ventana del cliente, leer estado vía OCR, detectar transición de calle, calcular equity, decidir acción y mostrarla en el overlay. Cualquier degradación en este flujo afecta directamente la ganancia/pérdida del usuario.

---

## 1. Persona

**Pablo** — Jugador profesional de poker cash NLHE, micro-stakes (NL2/NL5/NL10).
- Operación: 4-8 mesas simultáneas, sesiones de 4-6 h, varios miles de manos por semana.
- Conocimiento: GTO básico, post-flop heurístico, depende del bot para escalar volumen.
- Restricciones: máquina Windows 11 desktop, monitor único 1920×1080 (a veces 2560×1440).
- Objetivo principal: maximizar EV manteniendo decisiones correctas durante 6 h sin fatiga.

---

## 2. Historia (formato narrativo)

> **Como** Pablo (jugador cash NLHE),
> **quiero** que el bot lea automáticamente el estado de la mesa cada vez que es mi turno y me recomiende la acción correcta en el overlay,
> **para** poder jugar 6 h con consistencia y volumen, sin fatiga cognitiva ni errores de ejecución.

---

## 3. Criterios de aceptación

🟢 Derivados de los flujos en `OpenScrape.App/design.md` § 3-5 y de las reglas de negocio R-01 a R-12 de `_reversa_sdd/domain.md`.

### CA-01 — Detección de turno hero (color B≈24)

**Dado** que Pablo tiene la app abierta y la ventana del cliente seleccionada,
**Y** que el cliente muestra un indicador de turno azul oscuro (canal B≈24) sobre el slot del hero,
**Cuando** el `BackgroundWorker` ejecuta el ciclo de captura (cada `CaptureIntervalMs=100` ms),
**Entonces** `ColorDetectionService` detecta el indicador y `FrmMain` invoca el pipeline de decisión.

🟢 Confirmado en `FrmMain.cs:Color B≈24` + R-02 de `domain.md`.

### CA-02 — Lectura OCR multi-consenso

**Dado** que el bot detecta turno del hero,
**Cuando** `ScreenReaderService.ReadXxx` ejecuta 3 lecturas con preprocesamientos variados (sin filtro / grayscale / contrast+binarize),
**Entonces** el resultado emitido es la mayoría (≥2/3 lecturas idénticas tras `NormalizeBetValue`),
**Y** la confianza promedio es ≥0.70 (`IsHighConfidence=true`),
**Y** los datos críticos (cartas hero, board cards, pot, villain bet, hero stack, dealer alias) están todos completos.

🟢 Confirmado en `ScreenReaderService.cs` + ADR-0010.

### CA-03 — Asignación de posiciones tras detectar dealer

**Dado** que `TableLayoutService.SetDealerPlayer` detecta el botón dorado en un slot,
**Cuando** se asignan posiciones moving blinds (SB → BB → … → BTN según número de jugadores activos),
**Entonces** Pablo ve en el overlay su posición correcta (`Hero@CO`, `Hero@BB`, etc.) en menos de 2 ciclos consecutivos.

🟢 Confirmado en R-04 + `TableLayoutService.cs`.

### CA-04 — Detección de cambio de calle con validación

**Dado** que el bot está en estado `FlopAction` (3 cartas board visibles),
**Cuando** la lectura del slot `Card4` retorna una carta válida con confianza ≥0.80,
**Entonces** `GameLoopStateMachine.TryTransition(TurnDetected, visibleBoardCards=4)` permite la transición,
**Y** el motor recalcula equity considerando la nueva carta antes de decidir.

🟢 Confirmado en DD-07 + ADR-0012.

**Negativo:** si el OCR sólo detecta 4 cartas pero `Card4` es ambiguo (confianza <0.70), la transición se rechaza y el ciclo siguiente reintenta. Pablo sigue viendo recomendación de Flop.

### CA-05 — Cálculo de equity con cache hit

**Dado** que el bot tiene cartas hero `[As, Kh]` y board `[Qd, 7c, 2s]` con 1 oponente activo,
**Cuando** el bot ya calculó equity para esa misma combinación 5 ciclos antes,
**Entonces** `UnifiedPokerCalculator._equityCache.TryGetValue` retorna `true` con valor cacheado en <1 µs,
**Y** Pablo NO percibe latencia adicional en el overlay (latencia total <50 ms desde captura a recomendación).

🟢 Confirmado en DD-09 + `UnifiedPokerCalculator.cs`.

### CA-06 — Cálculo de equity con cache miss (Monte Carlo)

**Dado** que el bot encuentra una combinación nueva (cache miss),
**Cuando** se invoca `MonteCarloSimulator.CalculateEquity(playerHand, communityCards, numOpp, situation)`,
**Entonces** se ejecuta enumeración exacta (river: C(45,2)=990 manos / turn: ~42K evals) o MC (flop: 50K iter / preflop: 30K iter),
**Y** el resultado se persiste en cache para los siguientes ciclos,
**Y** la latencia total del ciclo se mantiene <200 ms (60 % bajo el `CaptureIntervalMs=100`).

🟢 Confirmado en `MonteCarloSimulator.cs` + ADR-0010.

### CA-07 — Decisión postflop con paths múltiples

**Dado** que el bot tiene equity calculada, board texture analizada y opponent profile (si reliable),
**Cuando** `PostflopDecisionService.DetermineAction(input)` se invoca,
**Entonces** retorna una `DecisionResult { Action, BetSize, Reason, Confidence }` aplicando el path correcto entre los 10+ disponibles (Facing Bet / No Bet / Check-Raise / Float Exit / Probe / Pot Control / Delayed Value / Low Equity / Randomización / C-Bet / Bluff),
**Y** la `Reason` contiene el path activado para diagnóstico (`"FacingBet:RaiseValue"`, `"NoBet:CheckBack:PotControl"`, etc.).

🟢 Confirmado en R-08 a R-12 + `PostflopDecisionService.cs`.

### CA-08 — Recomendación visible en el overlay

**Dado** que el bot tiene una decisión `DecisionResult { Action: Bet, BetSize: 5.4, Reason: "ValueBet:OnePair:CO" }`,
**Cuando** `FrmOverlay.UpdateDecision(action, betSize)` se invoca cross-thread (`BeginInvoke`),
**Entonces** Pablo ve en el overlay encima del cliente:
- Acción recomendada: **"BET 5.4 BB"** en color verde sobre fondo magenta-key.
- Indicador de calle activo: **FLOP** (3/4/5 según street).
- Equity y pot odds: `"42.3 % / 25 %"`.
**Y** la actualización ocurre en menos de 200 ms desde la captura inicial,
**Y** el cliente sigue siendo clickeable (magenta-key NO roba foco).

🟢 Confirmado en DD-08 + `FrmOverlay.cs`.

### CA-09 — Telemetría capturada por ciclo

**Dado** que el ciclo completa una decisión,
**Cuando** se ejecuta el bloque `using var measurement = _metrics.Measure(TelemetryCategories.DECISION_TOTAL)`,
**Entonces** la latencia se acumula en `MetricsCollector` con bucket logarítmico,
**Y** Pablo puede inspeccionar en pestaña Métricas: `Capture`, `OCR.Cards`, `OCR.Bets`, `Equity`, `Decision.Total` con histogramas p50/p95/p99 actualizados por sesión.

🟢 Confirmado en ADR-0017 + `TelemetryCategories.cs`.

### CA-10 — Persistencia de la decisión

**Dado** que la decisión está tomada y la calle no es la última (Hand → Flop → Turn → River),
**Cuando** Pablo confirma manualmente (clic en cliente, no bot que actúa),
**Entonces** `GameLoggerService.LogStreetDecision(streetDecision)` registra la decisión en `_currentHand.Decisions`,
**Y** al cerrar la mano (showdown o fold del hero), `GameLoggerService.EndHand(prevHeroStack)` calcula `Profit = currentStack - prevHeroStack` (con compensación auto-rebuy DD-06),
**Y** `await SaveSessionAsync()` persiste el `HandRecord` en Marten.

🟢 Confirmado en DD-15 + `GameLoggerService.cs`.

---

## 4. Diagrama de flujo (golden path)

```
Pablo abre cliente de poker (ventana visible)
         │
         ▼
Pablo lanza OpenScrape.App.exe
         │  Program.Main
         │  → Host.Build()
         │  → DI graph + StrategyProfileValidator
         │  → CreateAsyncScope() + FrmMain
         ▼
Pablo selecciona ventana en FormListApps (filtro "NL H")
         │
         ▼
Pablo presiona "Iniciar captura" → BackgroundWorker.RunWorkerAsync
         │
         ▼
┌────────────────── LOOP 100 ms ──────────────────┐
│                                                 │
│  CaptureWindowsHelper.GetWindowsScreenAsync      │
│  → Bitmap (1920x1080 escalado a DPI 600)         │
│                                                 │
│  ColorDetectionService.IsHeroTurn(B≈24)?         │
│  ├─ no  → continue (next tick)                   │
│  └─ yes ▼                                        │
│                                                 │
│  ScreenReaderService.ReadXxx (3-read consensus)  │
│  → cards hero, board, bets, stacks, dealer       │
│                                                 │
│  TableLayoutService.SetDealerPlayer +            │
│    AssignPositions (moving blinds)               │
│                                                 │
│  GameLoopStateMachine.TryTransition              │
│  ├─ FlopDetected ≥3 cards?  PASS                 │
│  ├─ TurnDetected ≥4 cards?  PASS                 │
│  └─ RiverDetected ≥5 cards? PASS                 │
│                                                 │
│  IPokerCalculator.Calculate                      │
│  ├─ cache hit  → equity ready (<1 µs)            │
│  └─ cache miss → MonteCarloSimulator (50K iter)  │
│                                                 │
│  IPostflopDecisionService.DetermineAction        │
│  ├─ FacingBet path                               │
│  ├─ NoBet path (with c-bet mixing)               │
│  ├─ Check-Raise path                             │
│  ├─ Float Exit / Probe / Pot Control / …         │
│  └─ Bluff catch / Randomización                  │
│                                                 │
│  GameCoordinator.BuildOverlayPayload             │
│  → FrmOverlay.UpdateDecision (BeginInvoke)       │
│                                                 │
│  MetricsCollector.Measure(DECISION_TOTAL)        │
│                                                 │
│  GameLoggerService.LogStreetDecision (queue)     │
│                                                 │
└─────────────────────────────────────────────────┘
         │
         ▼
Pablo ejecuta acción manualmente en cliente
         │
         ▼
[Próximo evento: nueva calle, fold del hero, showdown, nueva mano]
```

---

## 5. Variantes (caminos secundarios)

### V-01 — Lectura OCR de baja confianza
- 3 reads divergentes; `IsHighConfidence=false`.
- Comportamiento esperado: skip ciclo (NO decidir con datos malos) — pendiente Q-APP-14.
- Comportamiento actual: continuar con primera lectura no-vacía (best-effort) → riesgo de decisión errada.

### V-02 — Cliente cerrado durante captura
- `User32.GetWindowRect` retorna false; bitmap inválido.
- Comportamiento esperado: detectar y notificar (Q-APP-18).
- Comportamiento actual: ciclos vacíos silenciosos.

### V-03 — Cache miss en sesión muy larga (>2048 boards)
- `_equityCache.TryAdd` falla; cada ciclo recalcula.
- Comportamiento actual: latencia degrada de <1 µs (cache hit) a 30-100 ms (MC fresh) — Q-APP-17.

### V-04 — Auto-rebuy del cliente (stack salta a 100 BB)
- `_heroStackPreRebuy` preserva el valor pre-rebuy → `EndHand(prevHeroStack)` calcula profit correcto.
- ✅ Comportamiento robusto (DD-06).

### V-05 — Crash de Marten durante persist
- `SaveSessionAsync` lanza `NpgsqlException`; mano se pierde silenciosamente.
- Comportamiento esperado: cola de fallback en disco (Q-APP-12).

### V-06 — Multi-mesa (4 ventanas simultáneas)
- Pablo opera 4 mesas; cada una requiere su propia instancia del bot apuntada al handle correspondiente.
- **Limitación actual:** una sola instancia de la app maneja una sola ventana. Para 4 mesas, Pablo lanza 4 instancias de `OpenScrape.App.exe`. Cada instancia tiene su propio scope de DI, su propio `_currentSession`, su propia BD persistida (con `TableName` distinto).

🟡 Inferido del modelo de scope; sin spec explícita de multi-mesa.

---

## 6. Métricas de éxito

Cómo Pablo y el equipo del producto saben que esta historia funciona:

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Latencia ciclo completo (p95)** | <200 ms | `MetricsCollector` → `Decision.Total` p95 |
| **Latencia OCR (p95)** | <80 ms | `OCR.Cards` + `OCR.Bets` |
| **Cache hit ratio equity** | >70 % | log interno de `UnifiedPokerCalculator` |
| **Confianza OCR promedio** | >0.85 | `IsHighConfidence` ratio en `ScreenReaderService` |
| **Decisiones correctas vs control humano** | >80 % match | Backtest A/B (`StrategyBacktester`) |
| **EV proyectado (BB/100)** | positivo en NL5+ | `GameSession.BBPer100` |
| **Tasa de manos persistidas** | >99.5 % | `_sessionTotalHands` vs `HandRecord` count en BD |
| **Tasa de crashes** | 0 por sesión 6 h | logs Application + Windows EventLog |

---

## 7. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| OCR falla con animations/blur | 🟡 | 3-read consensus + dHash cache | Cache last-known (Q-APP-14) |
| Tesseract crash con bitmap inválido | 🔴 | — | Validar bounds (Q-APP-15) |
| `BackgroundWorker` zombie loop | 🟡 | try/catch defensivo | Contador de fallos consecutivos (Q-APP-13) |
| Multi-monitor DPI distinto | 🟡 | `SetProcessDPIAware()` global | Per-monitor DPI (Q-APP-10) |
| `BoardCards` con duplicado OCR | 🟡 | — | Validación unicidad (Q-APP-11) |
| `PostflopGameContext` leak entre manos | 🟡 | `StartNewHand()` incondicional | Reset defensivo si `IsAnyoneAllIn && boardCards.Count==0` (EC-12) |
| Marten outage | 🟡 | try/catch loggea | Cola fallback (Q-APP-12) |

---

## 8. Dependencias

**Otras user stories:**
- **US-02** Configuración inicial / table mapping — debe completarse antes de US-01 (regions calibradas).
- **US-03** Sesión de juego (lifecycle) — wraps US-01 con start/end session.

**Specs por unit:**
- `OpenScrape.App/` — captura, OCR, game loop, overlay, telemetría, persistencia orquestada.
- `OpenScrape.DecisionMaker/` — equity, MC, decisión postflop, opponent tracker.
- `OpenScrape.Domain/` — entidades (`Card`, `Table`, `GameSession`, `HandRecord`), value objects (`Hand`, `StreetDecision`, `StreetThresholds`).
- `OpenScrape.Features/` — `GetActionScenario`, `GetCardsFlop/Turn/River`, `LoadTableMap`.
- `OpenScrape.Infrastructure/` — Marten document store.

**Externo:**
- Cliente de poker (PokerStars / 888 / GG / etc.) abierto y visible.
- PostgreSQL accesible vía connection string (`appsettings.Development.json`).
- Tesseract `eng.traineddata` disponible.

---

## 9. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] Todos los CA-01 a CA-10 pasan en testing manual con cliente real abierto.
- [ ] La matriz de 216 casos (`DecisionMatrixIntegrationTests.cs`) está verde.
- [ ] La latencia p95 del ciclo completo es <200 ms en máquina de referencia (Windows 11, Ryzen 5, 16 GB).
- [ ] La pestaña Métricas muestra valores no-vacíos en `Decision.Total`, `OCR.Cards`, `OCR.Bets`, `Equity`, `Capture` tras 30 manos.
- [ ] Una sesión de 50 manos termina sin pérdida de datos: `_sessionTotalHands == COUNT(*) FROM hand_record WHERE session_id=X`.
- [ ] `BBPer100` se calcula correctamente con stack pre-rebuy.

---

## 10. Notas

- **Ciclo del producto:** esta historia NO es opcional ni adicional — es la razón por la que el producto existe. Cualquier bug que la rompa es 🔴 stopper.
- **Cobertura de tests:** ~645 tests (en `OpenScrape.App.Tests`) cubren las piezas individuales. La cobertura E2E del flujo completo es **manual** (Pablo prueba en cliente real). No hay test E2E automatizado (limitación de capturar cliente real en CI).
- **Dependencia con Q-APP-04:** si el cutover a `GameLoopCoordinator` se completa, este flujo sigue idéntico desde el punto de vista de Pablo, pero el primitivo de timing cambia (`PeriodicTimer` vs `BackgroundWorker`).
- **Dependencia con Q-APP-09:** si Pablo edita el `appsettings.json` durante la sesión esperando hot reload, hoy el cambio NO aplica (`ThresholdsRegistry` singleton no observa). Comportamiento documentado.
