# OpenScrape.DecisionMaker — Casos Extremos

> Casos límite del motor de decisión detectados en el código actual y la lógica del game loop, con disparador, comportamiento real, comportamiento esperado y consecuencias si se ignoran. `doc_level = detalhado` exige al menos 2 EC por unit; este archivo cubre 18.

---

## EC-01 — Input con `equity ∈ {<0, >100, NaN}` en `DetermineAction`

**Disparador:** Un caller construye `PostflopDecisionInput { Equity = -5 }` por error (cálculo previo retorna negativo) o `Equity = double.NaN` (división por cero en algún punto del pipeline upstream).

**Comportamiento real** (🔴 sin guardia):
- `PostflopDecisionService.DetermineAction(input)` no valida `input.Equity` al entrar.
- El cálculo de `effectiveEquity` opera sobre el valor inválido: `effectiveEquity = -5 - dangerPenalty + comboDrawBonus - reverseImpliedPenalty` puede dar números absurdos. Aunque al final hay `Math.Max(0, effectiveEquity)`, los pasos intermedios de comparación contra `FoldBelow`/`ThinValueAbove` evalúan con NaN como `false` siempre → caen en la rama "equity baja".
- Para `Equity = NaN`, todas las comparaciones (`equity > FoldBelow`, etc.) retornan `false`. El motor entra en `HandleLowEquity` y eventualmente puede emitir Bluff o Fold inesperado.
- Sin log explícito; el caller no sabe que el input estaba corrupto.

**Comportamiento esperado:**
- Validación al entrar: `if (double.IsNaN(equity) || equity < 0 || equity > 100) { Log.Error(...); return Check; }` o lanzar `ArgumentException`.
- Q-FSM-01 → "watchdog debería implementarse".

**Consecuencias si se ignora:**
- 🔴 Decisiones erróneas silenciosas en spots con bug upstream (típicamente `EquityCalculatorService.CalculateFullEquity` fallando con MC `IsReliable=false`).
- 🔴 Difícil de detectar en producción: el log no marca el input corrupto.

**Cobertura test:** Ninguna. Tarea T-84 lo cubre.

**Pregunta abierta:** Q-FSM-01.

---

## EC-02 — `villainStack <= 0` después de detección imperfecta de all-in

**Disparador:** Villain hace all-in en flop. La OCR del stack del villano lee `0` (correcto) pero antes de que el game loop actualice `PostflopGameContext.IsAnyoneAllIn = true`, una capture lee `villainStack = -1` (artefacto OCR).

**Comportamiento real** (🟡 fallback parcial):
- `PostflopGameContext` interpreta `villainStack <= 0` como `IsAnyoneAllIn = true`.
- Esto desactiva fold equity (`foldEquity = 0`) y reverse implied odds (`reverseImpliedPenalty = 0`).
- El motor evalúa el spot como "no hay folds posibles del villano". Decisión típica: Call si equity > pot odds, Fold si no.
- Sin log explícito que diferencie "villain all-in real" de "OCR retornó 0 falso".

**Comportamiento esperado:**
- Validación: `villainStack < 0` debería ser un input corrupto, no `IsAnyoneAllIn`. Solo `villainStack == 0` Y un BetSize >= stack reciente debería interpretarse como all-in.
- BF8 ya documentado: `VillainStack fallback a HeroStack si OCR = 0`.

**Consecuencias si se ignora:**
- 🟡 Spots con OCR transitorio degradan a "no fold equity"; el bot se vuelve más conservador del necesario.
- 🟡 Si villain en realidad NO está all-in pero el motor lo interpreta así, puede dejar valor en mesa al no semi-bluff.

**Cobertura test:** Indirecta vía tests de `MonteCarloSimulator` con `IsAnyoneAllIn=true`. No hay tests para `villainStack < 0` específicamente.

---

## EC-03 — Auto-rebuy 100 BB durante mano activa contamina P/L

**Disparador:** Hero está jugando una mano con stack 50 BB. La sala detecta stack < umbral y auto-rebuy a 100 BB durante el turn (entre captures). La siguiente captura lee `heroStack = 100`.

**Comportamiento real** (🟢 mitigado por `_heroStackPreRebuy`):
- `PostflopGameContext.TrackHeroStack(currentStack=100)` detecta el incremento súbito (50 → 100 con mano activa).
- Preserva `_heroStackPreRebuy = 50` y NO actualiza el "stack base" usado para P/L de la mano.
- `IsAnyoneAllIn` sigue calculándose con el stack de la mano activa.
- `EndHand` (en `App`) recibe `prevHeroStack = _heroStackPreRebuy = 50` para calcular `(stackEnd - stackStart)` correctamente.

**Comportamiento esperado:** Funciona como diseñado.

**Consecuencias si NO existiera el mitigador:**
- 🔴 P/L de la mano contabilizaría +50 BB (rebuy) como ganancia, falseando el bankroll.
- 🔴 BankrollTracker `RiskOfRuin` sobreestimaría el winrate; podría sugerir subir de stake con un sample falso.

**Cobertura test:** Implícita en tests de `PostflopGameContext.TrackHeroStack`. T-32 lo cubre.

**ADR relacionado:** ADR-0013 (Auto-rebuy detection 50 BB threshold).

---

## EC-04 — `MonteCarloSimulator` con rango villano 100% bloqueado por hero/board

**Disparador:** Hero tiene `AhAd`, board `AsAcKsKh`. El rango villano configurado para "Big pairs" (`AA, KK, QQ, JJ`) está totalmente bloqueado: AA tiene 0 combos (`hero blocks 2 As`), KK tiene 1 combo (`board blocks 2 Ks`), QQ y JJ son 6 combos cada uno → ratio bloqueado depende del peso.

**Comportamiento real** (🟢 detección + skip):
- `BuildVillainCombos` calcula `precomputedTotalWeight = sum(weights of unblocked combos)`.
- `BlockedComboPercentage` se calcula como `(totalWeightInRange - precomputedTotalWeight) / totalWeightInRange`.
- Si `BlockedComboPercentage > BlockedComboUnreliableThreshold (20.0%)` → `IsReliable = false` en `EquityResult`.
- `TryDrawFromRange` con 20 retries; si no logra, salta la iteración y `SkippedSimulations++`.
- Si TODOS los combos están bloqueados (corner case): `precomputedTotalWeight = 0` → equity podría retornar 0 o NaN (depende del manejo de división por cero en `RunMonteCarloSimulation`).

**Comportamiento esperado:**
- Verificar que `precomputedTotalWeight > 0` antes de iniciar el MC; si es 0, retornar `EquityResult { Equity=50, IsReliable=false, ReasonCode="EmptyRange" }` o equivalente.
- App debe detectar `IsReliable=false` y mostrar warning UI.

**Consecuencias si se ignora:**
- 🟡 Equity = NaN propaga al `PostflopDecisionService` y rompe todas las comparaciones (cae en `HandleLowEquity` siempre).
- 🟡 `IsReliable=false` no está expuesto en la UI actualmente; el usuario no sabe que la equity es no fiable.

**Cobertura test:** Parcial. Tarea T-15+T-16 cubre `IsReliable=false` para >20% bloqueado, pero no el caso 100% bloqueado.

---

## EC-05 — Wheel (A-2-3-4-5) en `BitHandEvaluator.FindStraightHigh`

**Disparador:** Hero tiene `Ah 2c` y board completa con `3h 4d 5s`. La straight resultante es A-2-3-4-5 (la "rueda").

**Comportamiento real** (🟢 caso especial):
- `BitHandEvaluator.FindStraightHigh(rankBits)` itera de `high = 14` hasta `6`, buscando 5 ranks consecutivos.
- Para `high = 14`, requiere bits 14, 13, 12, 11, 10 → no match (no hay K, Q, J, T en este board).
- ... continua bajando ...
- Para `high = 5`, normalmente buscaría bits 5, 4, 3, 2, 1 — pero no hay rank 1 en póker.
- **Caso especial**: máscara `WheelMask = (1<<14) | (1<<2) | (1<<3) | (1<<4) | (1<<5)` que incluye el As como rank 14 (alto) reinterpretado como rank 1 (bajo). Si `(rankBits & WheelMask) == WheelMask`, retorna `5` (no `14`).

**Comportamiento esperado:** Funciona como diseñado. La straight "wheel" es la más baja, su `high = 5`.

**Consecuencias si se ignora la máscara:**
- 🔴 Sin la máscara, una mano `As 2c 3h 4d 5s` se evaluaría como "no straight" porque `FindStraightHigh` no busca el bit 1.
- 🔴 Comparación de straights `wheel vs broadway`: con `high = 5` la wheel pierde correctamente contra cualquier otra straight; sin la máscara, retornaría `high = 14` y empataría con `T-J-Q-K-A` (catastrófico para equity).

**Cobertura test:** T-07 explícito (test wheel + broadway). 15 tests del corpus existente cubren el caso.

---

## EC-06 — `ThresholdsRegistry` con clave faltante en `appsettings.json`

**Disparador:** El usuario edita `appsettings.json` y por typo introduce `"Flop_OpenRise"` en lugar de `"Flop_OpenRaise"`. Reinicia la app.

**Comportamiento real** (🟢 fail-fast):
- Al construir DI, `ThresholdsRegistry(IOptions<StrategyProfile>)` itera las 30+ combinaciones requeridas `(BoardPosition × HandSituation)`.
- Detecta que falta `(Flop, OpenRaise)` (porque la clave es `"Flop_OpenRise"` ≠ `"Flop_OpenRaise"`).
- Lanza `InvalidOperationException("Missing threshold: Flop_OpenRaise")`.
- La app falla en arranque con stack trace que apunta a la clave.

**Comportamiento esperado:** Funciona como diseñado.

**Consecuencias si NO existiera el fail-fast:**
- 🔴 La app arranca normalmente. La primera vez que el motor enfrenta `(Flop, OpenRaise)`, lookup retorna `null` o `default(StreetThresholds)` con todos los thresholds en 0.
- 🔴 `FoldBelow=0` significa que el motor nunca foldea en flop OpenRaise → bleed de chips silencioso.
- 🔴 El usuario tarda decenas de manos en notar el patrón anómalo.

**Cobertura test:** T-34 + TT-07.

**ADR relacionado:** ADR-0008.

---

## EC-07 — Pot commitment block duplicado emite resultados distintos

**Disparador:** Hero tiene SPR < 0.5 con `EV(call) > 0`. El motor llega al primer bloque de pot commitment en `:772-795` y luego al segundo en `:1665-1688`.

**Comportamiento real** (🟡 anomalía DD-09 pero funcionalmente equivalente HOY):
- Ambos bloques implementan la misma lógica: `if (spr < 0.5 && allinEV > 0) return Call;`.
- Como están duplicados textualmente (no extraídos a método común), si un futuro commit modifica solo uno → divergencia silenciosa.
- En el código actual, los outputs son idénticos.

**Comportamiento esperado:**
- Extraer a método común `EvaluatePotCommitment(equity, pot, stack) → bool` y llamar desde ambos puntos.
- Tarea T-59.

**Consecuencias si se ignora:**
- 🔴 Refactor en uno solo de los dos bloques rompe el invariante "pot commitment es consistente". Bug latente.
- 🟡 Tests de regresión podrían no detectarlo si solo cubren un path.

**Cobertura test:** Parcial. Existen tests para pot commitment pero no validan que ambos bloques produzcan el mismo output sobre el mismo input.

---

## EC-08 — Bloque `if` vacío en `PostflopDecisionService.cs:1198-1203`

**Disparador:** El motor entra en una rama de `HandleNoBet` que llega al bloque `if` vacío.

**Comportamiento real** (🔴 path muerto o no terminado):
- El bloque `if (...) { }` no ejecuta ninguna acción dentro de las llaves.
- El control sigue al siguiente bloque de la pipeline.
- Sin tag de log; el motor no anota que pasó por aquí.

**Comportamiento esperado:**
- Si es debug residual: eliminar el bloque (T-58).
- Si es path no terminado: implementar la lógica que faltaba.
- Cualquier opción requiere decisión humana sobre qué condición evaluaba.

**Consecuencias si se ignora:**
- 🟡 Mantener código muerto degrada legibilidad y aumenta la superficie de mantenimiento.
- 🔴 Si era un path crítico no terminado, el motor toma decisiones subóptimas en spots específicos sin que nadie lo sepa.

**Cobertura test:** Ninguna. T-58 + decisión humana 🔴.

---

## EC-09 — `AutoCalibrationService` propone con `OldValue` distinto al perfil real (BUG)

**Disparador:** El usuario tiene `StrategyProfile.FoldBelow = 50` (modificó el default). `ExploitabilityCalculator` detecta un leak "overfold". `AutoCalibrationService.PreviewAndApply(topLeaks)` se invoca.

**Comportamiento real** (🔴 BUG conocido DD-13):
- El servicio compone una `ParameterAdjustment` con `OldValue = 45` (literal hardcoded en `:174-208`) y `NewValue = 48` (calculado).
- La UI muestra: "Tu FoldBelow actual: **45** → Sugerido: **48**".
- El usuario aplica la calibración: `StrategyProfile.FoldBelow = 48`.
- Pero el valor real era 50; pasó a 48 (no 53 como esperaría tras "incrementar 3 puntos").

**Comportamiento esperado:**
- Leer `IOptionsMonitor<StrategyProfile>` para obtener el valor activo y mostrarlo como `OldValue`.
- T-75 corrige el bug.

**Consecuencias si se ignora:**
- 🔴 Erosión de confianza del usuario en la auto-calibración.
- 🔴 Calibraciones acumulativas drift desde valores arbitrarios.

**Cobertura test:** TT-13 (failing today, marker para T-75).

**Pregunta abierta:** ninguna directa; bug a corregir.

---

## EC-10 — `ExploitabilityCalculator` con `BigBlind=1.0` en NL10 (BB=0.10)

**Disparador:** El usuario juega NL10 (BigBlind = $0.10). Tras 100 manos, abre la pestaña de Estadísticas → top leaks → "Overfold flop OOP: -3.5 BB/100".

**Comportamiento real** (🟡 anomalía DD-14):
- `ExploitabilityCalculator` calcula leaks usando `BigBlind = 1.0` hardcoded en `:93, 322-348`.
- Conversión de chips → BB: `chipsLost / 1.0` en lugar de `chipsLost / 0.10`.
- El "leak" reportado es 10× lo real.
- El usuario ve "-3.5 BB/100" cuando es "-0.35 BB/100".

**Comportamiento esperado:**
- Leer `BigBlind` de `IOptions<StrategyProfile>` o como parámetro de `RecordDecision`.
- T-74 corrige.

**Consecuencias si se ignora:**
- 🟡 El usuario sobreestima sus leaks 10× → sobre-corrige su estrategia.
- 🟡 Comparación entre stakes imposible (NL5 vs NL10 reportan en escalas distintas).

**Cobertura test:** Ninguna; depende de T-74.

---

## EC-11 — `OpponentTracker` lectura concurrente durante UI render

**Disparador:** El usuario abre la pestaña Stats. La UI itera `_profiles.Values` para renderizar la tabla de oponentes. Simultáneamente, el game loop llama `RecordPostflopAction(...)` añadiendo un nuevo profile.

**Comportamiento real** (🟢 thread-safe vía DD-05):
- `ConcurrentDictionary.Values` retorna un snapshot consistente para iteración.
- `GetOrAdd` en el game loop no bloquea la lectura.
- Los counters incrementados durante la iteración pueden mostrarse en el siguiente frame UI; no causan inconsistencia (eventual consistency aceptable para stats).

**Comportamiento esperado:** Funciona como diseñado.

**Consecuencias si NO fuera thread-safe:**
- 🔴 `InvalidOperationException("Collection was modified")` durante render UI → app crash o tab no-responsiva.
- 🔴 Lecturas parciales de un profile (counter A actualizado, counter B no) producirían stats incoherentes.

**Cobertura test:** TT-03 (stress 100 writers + 100 readers).

---

## EC-12 — `BackdoorOverlap` (S21.4) con flush + straight ambas activas

**Disparador:** Hero tiene `Ah Kh`, board `Qh 7c 2d` (flop). Detección:
- Flush draw nut: 4 cartas hearts (Ah, Kh + 2 future hearts).
- Straight draw backdoor: A-K-Q + 2 cartas más en ventana 5 ranks.
- Backdoor flush hearts (3 hearts en board + 2 con hero, ya tiene 4 → flush draw real, no backdoor).

**Comportamiento real** (🟢 S21.4 fix):
- `OutsCalculator.CalculateOuts` detecta: `flushOuts = 9` (heart outs).
- Backdoor straight (3 cartas en ventana A-K-Q-J-T, falta J + T) → `BackdoorStraightImpliedOuts = 1.0`.
- Sin overlap discount → outs total = 9 + 1.0 = 10.0.
- Pero el "backdoor straight" requiere `J` y `T` (cualquier palo). Si llega `Jh`, completaría straight Y mejoraría flush draw → out duplicado.
- S21.4 aplica `BackdoorOverlapDiscount = 0.5` → `1.0 × 0.5 = 0.5` se descuenta del backdoor straight.
- Outs final = 9 (flush) + 1.0 - 0.5 = 9.5.

**Comportamiento esperado:** Funciona como diseñado.

**Consecuencias si NO existiera S21.4:**
- 🟡 Doble conteo de outs → equity sobrestimada en flop.
- 🟡 Decisiones de semi-bluff con equity inflada → push/fold decisions equivocadas.

**Cobertura test:** T-19 (test específico S21.4 overlap).

---

## EC-13 — `goto skipBluffCatch` en `:1602, 1663` produce flujo no lineal

**Disparador:** Bluff catching en turn/river con pot odds adversos.

**Comportamiento real** (🟡 anomalía aceptada):
- El motor evalúa el path "bluff catch" con multipliers (villainType × runout × blockers).
- Si pot odds son demasiado adversos (`adjustedEquity < requiredEquity`), salta con `goto skipBluffCatch` a `:1663` para no aplicar el resto del bloque.
- Idiomáticamente C# debería usar early return o método extraído.

**Comportamiento esperado:**
- Refactor: extraer bluff catching a método dedicado, eliminar `goto`.
- T-55 marca como 🟡 (no urgente).

**Consecuencias si se ignora:**
- 🟡 Difícil de seguir para devs nuevos (`goto` rompe linealidad esperada).
- 🟡 Static analyzers (Roslyn warnings) flaggean el `goto`.

**Cobertura test:** Indirecta vía 21+ tests de bluff catching.

---

## EC-14 — `PostflopGameContext` leak entre manos si reset falla

**Disparador:** El game loop detecta nueva mano (board limpio, hero con cartas frescas). Por bug en `App.HandReset()` (no documentado), `_postflopContext` no se reasigna; se reutiliza la instancia anterior con flags `FloatedFlop=true` desde la mano previa.

**Comportamiento real** (🟢 mitigado por reasignación incondicional):
- El diseño establece (DD-03) que `HandReset()` SIEMPRE crea un nuevo `PostflopGameContext`.
- Si por bug se reutiliza, el motor heredaría flags como `FloatedFlop`, `TurnCalledWithFlushDanger`, `IsAnyoneAllIn`.
- Decisiones contaminadas: el motor podría intentar "float exit" en una mano nueva, decidir Bet erróneamente.

**Comportamiento esperado:**
- `HandReset()` en `App` se invoca en cada `WaitingForHand → HandDetected` transition (game loop state machine).
- Test de invariante: tras `HandReset`, todos los flags son default.

**Consecuencias si se ignora:**
- 🔴 Decisiones absurdas en el spot post-reset (mano nueva con contexto viejo).
- 🔴 Difícil de reproducir en debug porque depende de timing exacto.

**Cobertura test:** Indirecta via tests de game loop state machine; no hay test directo "context se resetea entre manos".

**ADR relacionado:** ADR-0007.

---

## EC-15 — `villainCheckedMiddleStreet` (bet-check-bet) range narrowing reducido

**Disparador:** Villain apuesta flop, checkea turn, vuelve a apostar river.

**Comportamiento real** (🟢 S13.1 con multiplier ×0.5):
- `Range narrowing` normalmente añade `+RangeNarrowingPerStreet=3.0 × (n−1)` por cada calle apostada por villain.
- Para bet-bet (flop+turn): `+3.0 × 1 = +3.0` FoldBelow.
- Para bet-check-bet (flop+turn check + river bet): el motor detecta el patrón y aplica multiplier `×0.5` → `+3.0 × 0.5 = +1.5` FoldBelow.
- Razón: el check intermedio sugiere debilidad/give-up; el bet final no debería estrechar tanto el rango.

**Comportamiento esperado:** Funciona como diseñado.

**Consecuencias si se ignora el multiplier:**
- 🟡 Hero foldearía manos marginales que en realidad pueden ganarle a un rango con check intermedio (más débil).
- 🟡 El motor sobreestima la fuerza del villano en bet-check-bet.

**Cobertura test:** T-49 explícito.

---

## EC-16 — `PreflopEquityCalculator` con `numOpponents = 0`

**Disparador:** Bug upstream pasa `numOpponents = 0` (todos los oponentes ya foldearon antes del flop, hero gana sin ver flop).

**Comportamiento real** (🟡 sin guardia):
- `PreflopEquityCalculator.GetEquity(handName, suited, 0)` aplica fórmula multiway `equity^(1+log2(0)×0.35)`.
- `log2(0) = -∞` → fórmula colapsa a `equity^(1 + (-∞)×0.35) = equity^(-∞)` → resultado matemáticamente indefinido.
- En práctica .NET: `Math.Log2(0) = double.NegativeInfinity`, `Math.Pow(0.85, -inf) = double.PositiveInfinity` o NaN.

**Comportamiento esperado:**
- Validar `numOpponents >= 1` al entrar; si 0, retornar 100 (hero ya ganó) o lanzar excepción.
- Caller upstream debería detectar "todos foldearon antes de ver flop" y no invocar el motor.

**Consecuencias si se ignora:**
- 🔴 Equity = NaN propaga al `PostflopDecisionService` → comportamiento absurdo.
- 🟡 En la práctica, este caso no llega al motor porque el game loop detecta el fold-around y no inicia postflop.

**Cobertura test:** Ninguna. Borderline gap.

---

## EC-17 — `BoardTextureAnalyzer.AnalyzeInitialBoard` con flop `2c 2d 2h` (trips en board)

**Disparador:** Flop `2c 2d 2h` (set en board, sin pareja del villano).

**Comportamiento real** (🟢 caso raro pero correcto):
- `wetnessScore`: Trips contribuye -15.
- `Paired`: tres del mismo rank → categorizado como `Paired` con flag adicional `IsTrips`.
- `DangerLevel`: alto porque cualquier carta del villano que matchee el rank del set le da quads o full.
- `dangerousFlushBoard = false` (no hay 3 mismo palo).

**Comportamiento esperado:** Funciona como diseñado.

**Consecuencias si se ignora la categoría `Paired+Trips`:**
- 🟡 Sizing del motor podría no adaptarse al spot. Trips en board son boards extremadamente "merged" — value bets deben ser delgados.

**Cobertura test:** Existe (26 tests `BoardTexture*`), incluye al menos 1 caso paired/trips.

---

## EC-18 — `Random.Shared.NextDouble()` no reproducible para debugging

**Disparador:** El usuario reporta una decisión inesperada en la mano #4521. Quiere reproducir la decisión exacta para debug.

**Comportamiento real** (🟡 DD-11):
- `PostflopDecisionService` usa `Random.Shared.NextDouble()` en 11 puntos (randomización adaptativa, c-bet mixing, check-raise mixing).
- El RNG no tiene seed configurable → cada ejecución produce decisiones distintas en spots cercanos al threshold.
- El usuario no puede reproducir la decisión exacta sin grabar el rng output original.

**Comportamiento esperado:**
- Inyectar `IRandomProvider` con interfaz `NextDouble()`. Test/debug usa implementación con seed fija; producción usa `Random.Shared`.
- T-56 marca decisión humana.

**Consecuencias si se ignora:**
- 🟡 Bugs estocásticos imposibles de reproducir; solo se atrapan con grabación de inputs+outputs en producción.

**Cobertura test:** Tests de randomización son estadísticos (1000 ejecuciones, ratio ±5%) — lentos y flakey. No hay tests reproducibles.

---

## Resumen

| EC | Severidad | Mitigación actual | Tarea de fix |
|----|-----------|--------------------|--------------|
| EC-01 — equity NaN/<0/>100 | 🔴 | Ninguna | T-84 |
| EC-02 — villainStack < 0 | 🟡 | Parcial (BF8 fallback) | — |
| EC-03 — auto-rebuy | 🟢 | `_heroStackPreRebuy` | T-32 (hecho) |
| EC-04 — MC range 100% bloqueado | 🟡 | `IsReliable=false` parcial | T-15+T-16 |
| EC-05 — Wheel A-2-3-4-5 | 🟢 | `WheelMask` especial | T-07 (hecho) |
| EC-06 — ThresholdsRegistry clave faltante | 🟢 | Fail-fast | T-34 (hecho) |
| EC-07 — Pot commitment duplicado | 🟡 | Idéntico HOY (riesgo a futuro) | T-59 |
| EC-08 — Bloque if vacío | 🔴 | Ninguna (decisión humana) | T-58 |
| EC-09 — AutoCalibration OldValue | 🔴 | Ninguna (BUG) | T-75 |
| EC-10 — ExploitabilityCalc BigBlind=1.0 | 🟡 | Ninguna | T-74 |
| EC-11 — OpponentTracker concurrente | 🟢 | `ConcurrentDictionary` | T-61 (hecho) |
| EC-12 — Backdoor overlap S21.4 | 🟢 | Discount 0.5 | T-19 (hecho) |
| EC-13 — goto skipBluffCatch | 🟡 | Funcional pero no ideal | T-55 (refactor opcional) |
| EC-14 — PostflopContext leak | 🟢 | Reasignación incondicional | T-30..T-33 |
| EC-15 — bet-check-bet narrowing | 🟢 | Multiplier ×0.5 | T-49 |
| EC-16 — PreflopEquity n=0 | 🔴 | Ninguna | (gap) |
| EC-17 — Trips en flop | 🟢 | Categorizado `Paired+Trips` | T-22..T-23 |
| EC-18 — Random no reproducible | 🟡 | Funcional pero no debuggable | T-56 |

**4 EC con severidad 🔴** requieren acción humana (EC-01, EC-08, EC-09, EC-16). Tres con tarea ya planificada; uno (EC-16) es gap pendiente de captura.
