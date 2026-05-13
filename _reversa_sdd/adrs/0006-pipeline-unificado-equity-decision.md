# ADR-0006 — Pipeline unificado de equity y decisión postflop

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-16 (commit `1db7eee refactor/poker: Unified equity/decision pipeline overhaul`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

Antes del refactor, la lógica de cálculo de equity y de decisión postflop estaba dispersa entre múltiples handlers en `FrmMain` (`HandleFlopAction`, `HandleTurnAction`, `HandleRiverAction`, etc.) con duplicación de código y caminos divergentes. Cada calle re-implementaba su propia interpretación de outs, fold equity, danger penalty, etc.

## Decisión

Unificar el pipeline en una **secuencia canónica** orquestada por:

1. **`IPokerCalculator → UnifiedPokerCalculator`** como punto de entrada.
2. **Pipeline de equity** en orden fijo:
   - Pot odds → equity necesaria.
   - Equity raw vía `MonteCarloSimulator` (con villain range adaptativo por `OpponentProfile`).
   - Outs + draws con `OutsCalculator` (tainted outs, combo draw, backdoor calibrado).
   - Hand evaluation (`BitHandEvaluator`) → `HandRank` + `KickerStrength` + `PairClassification`.
   - Fold equity (stats reales `OpponentTracker` o fallback estático).
   - Danger penalty (proporcional flush/straight, flat board paired/overcard, blocker reduction).
   - Combo draw bonus × textura.
   - Reverse implied odds.
3. **`PostflopDecisionService.DetermineAction(PostflopDecisionInput)`** como único decisor — input/output records inmutables, 10+ paths de decisión definidos en orden fijo (Facing Bet → No Bet → Check-Raise → Float Exit → Probe Bet → Pot Control → Delayed Value → Low Equity → Randomización → C-Bet).

## Alternativas consideradas

1. **Mantener handlers por calle.** Rechazado: duplicación masiva. Cada bug fix tenía que aplicarse en flop/turn/river separadamente.
2. **State machine de decisión** (cada path como estado). Rechazado por overkill: los paths son if-else evaluables linealmente. Una FSM añade indirección sin beneficio.
3. **Arbol de decisión declarativo (rules engine externo).** Considerado pero rechazado: añade dependencia y latencia. La lógica es manejable en código.
4. **Single-method monolítico.** Estado original. Rechazado por testabilidad — cada algoritmo debe ser testeable en aislamiento.

## Consecuencias

**Positivas:**

- **Una única ruta de bug fix.** Si flush draw está mal calibrado, se corrige en `OutsCalculator` y aplica a las 3 calles.
- **Cross-street state coherente** vía `PostflopGameContext` (ver ADR-0007).
- Test fixture unificado: `DecisionMatrixIntegrationTests` cubre 216 combinaciones (3 streets × 9 situations × 2 positions × 2 bet states × 2 equity buckets) en una sola batería (commit `bb401c1`).
- Performance: enumeración exacta turn/river (commits memoria sesión 2026-03-26) reemplaza MC con error +-0.5% por equity 100% determinística.
- Permite hot-tuning de thresholds vía `appsettings.json` con efecto inmediato en las 36 entradas (`ThresholdsRegistry`).

**Negativas:**

- `PostflopDecisionService.DetermineAction` es **gigante**: ~1893 LOC con 10+ paths. SRP violado. **Anomalía conocida** (ver `code-analysis.md`).
- **Cambios al pipeline son costosos** porque cualquier paso afecta 9×3×otros paths. Mitigado con `DecisionMatrix` (216 cases).
- El `PostflopDecisionInput` tiene **42 campos** (6 required + 36 con defaults). Llamadas son verbosas pero el record inmutable garantiza que ningún path muta state.
- El orden de los paths es **prelativo** y frágil: cambiar el orden cambia decisiones. La intención es "primero detectar si estamos facing bet, luego ofrecer alternativas". Sin documentación clara más allá de comentarios.

**Implicaciones para una migración:**

- `PostflopDecisionInput` es un contrato estable. Cualquier reimplementación lo respeta.
- Los 10+ paths se pueden migrar uno a uno con tests `DecisionMatrix` como red de seguridad.
- En un paradigma funcional puro (F#, Rust), descomponer en pipeline lineal de transformaciones sería natural.

## Referencias

- Commit `1db7eee refactor/poker: Unified equity/decision pipeline overhaul`.
- Commit `82e6a45 refactor(decision): remove obsolete DetermineAction overload` — consolidación final de la API.
- Commit `bb401c1 test(decision): add 216-case integration matrix for postflop engine` — test fixture.
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`.
- `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs`.
- `_reversa_sdd/code-analysis.md` — 10+ paths documentados.
