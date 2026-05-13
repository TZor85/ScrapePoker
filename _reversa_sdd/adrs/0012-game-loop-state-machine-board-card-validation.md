# ADR-0012 — `GameLoopStateMachine` con validación por conteo de board cards

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** 2026-04-02 (commit `2538e55 fix(gameloop): Validar conteo real de cartas visibles antes de transicionar calle`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude Opus 4.6

## Contexto

El game loop transitaba de calle (`FlopAction → TurnDetected`) cuando `IsBoardCardVisible("Card4")` devolvía true. Este check usa comparación de hash de imagen (dHash) sobre la región de la carta 4 con un umbral de 80%. Falsos positivos eran posibles:

- Animación de transición entre calles (carta apareciendo/desapareciendo).
- Artefactos visuales (transparencia, sombras del dealer pasando).
- Ruido de la textura de mesa con resolución baja.
- Glow / efectos durante hand showdown.

**Síntoma:** el motor pedía decisión en *turn* sin haber 4 cartas reales en el board, con resultados absurdos (recomendaba bet sobre flop con 3 cartas pero state=Turn).

## Decisión

Añadir **validación por conteo real de cartas visibles** antes de cualquier transición de calle:

1. **`CountVisibleBoardCards()`** verifica `Card1..Card5` secuencialmente y devuelve el número de cartas reales detectadas (con umbral de validez por carta).
2. **Sobrecarga `GameLoopStateMachine.TryTransition(GameState newState, int visibleBoardCards)`** que:
   - Bloquea la transición si `visibleBoardCards < expectedMinCards` para ese estado:
     - `FlopDetected`/`FlopAction` → mínimo 3.
     - `TurnDetected`/`TurnAction` → mínimo 4.
     - `RiverDetected`/`RiverAction` → mínimo 5.
     - Otros estados → 0 (no requieren cartas).
   - **Warn** (no bloquea) si `visibleBoardCards > expectedMaxCards` — posible desfase de state.
3. La sobrecarga simple `TryTransition(GameState)` se mantiene para casos donde no hay info de cartas (Reset, HandComplete).
4. **`ForceState`** (modo test/debug) **valida que el estado destino exista** en el mapa de transiciones antes de aplicar (commit `f2b3422`).
5. **`lock(_stateLock)`** defensivo en todas las transiciones.
6. **`MaxOcrRetries = 2`** const para reintentos en caso de OCR fallido.

## Alternativas consideradas

1. **Mejorar `IsBoardCardVisible`.** Subir el umbral del dHash, añadir averaging temporal. Rechazado: cualquier umbral tiene falsos positivos. La validación por conteo es **independiente** y robusta.
2. **Confirmación N consecutivas.** "Solo transitar si veo Card4 en 3 capturas seguidas." Rechazado: añade latencia (~600ms a 200ms/iteración). Inaceptable cuando la realidad cambia rápido.
3. **Bloqueo en el caller en vez del state machine.** Rechazado: scattered logic; mejor que la regla viva en el state machine como invariante.
4. **State machine con eventos pull (event-sourced).** Sobre-ingeniería para 10 estados.
5. **Aceptar falsos positivos, corregir post-hoc.** Rechazado: una decisión equivocada cuesta dinero real.

## Consecuencias

**Positivas:**

- **Imposible decidir en street equivocada.** El motor solo recibe input cuando hay 3/4/5 cartas reales.
- **Preserva la sobrecarga simple** para casos sin info (legacy, tests).
- 14 tests `GameLoopStateMachineTests` cubren transiciones válidas, inválidas, edge cases.
- **Logs útiles:** "Transición bloqueada por board cards: FlopAction → TurnDetected, cartas visibles=3, mínimo requerido=4".
- **Defensa contra OCR ruidoso** sin sacrificar latencia.

**Negativas:**

- **Si OCR falla en Card1-3 también**, no se puede transitar — pero ese caso ya estaba roto antes. Mitigación: `MaxOcrRetries=2`.
- Doble llamada `CountVisibleBoardCards()` + `IsBoardCardVisible(Card4)` en algunos paths (CountVisibleBoardCards itera Card1-5 y la otra solo verifica Card4). Cost: ~5ms extra. Tolerable.

**Implicaciones para una migración:**

- En cualquier reimplementación, **el invariant "no transitar a calle sin cartas suficientes"** debe persistir. Es un control de daños sobre el OCR ruidoso.
- En lenguajes con union types (Rust enum + variant fields, F# discriminated unions), el state puede llevar las cartas como tipo: `State::FlopDetected { cards: [Card; 3] }`. Hace imposible representar "FlopDetected sin 3 cartas". Más limpio que validación lateral.

## Referencias

- Commit `2538e55 fix(gameloop): Validar conteo real de cartas visibles antes de transicionar calle`.
- Commit `f2b3422 fix(gameloop): Lock defensivo y validación en GameLoopStateMachine`.
- `src/OpenScrape.App/Services/GameLoopStateMachine.cs`.
- `_reversa_sdd/state-machines.md` §1 — diagrama y reglas detalladas.
- `openspec/archived/bugfix-street-transition-falso-positivo/` — spec del bugfix.
