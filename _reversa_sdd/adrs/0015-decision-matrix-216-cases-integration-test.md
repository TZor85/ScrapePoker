# ADR-0015 — Test fixture `DecisionMatrix` con 216 casos como red de seguridad del motor

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** 2026-04-20 (commit `bb401c1 test(decision): add 216-case integration matrix for postflop engine`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude Opus 4.7

## Contexto

`PostflopDecisionService` tiene 10+ paths de decisión, ~70 parámetros configurables y 36 entradas en `ThresholdsRegistry`. Cada cambio en el motor (ajuste de threshold, nueva regla, refactor) **podía** romper sutilmente combinaciones específicas (ej: "Flop Squeeze IP facing Medium bet con equity 80%").

Antes del refactor existían tests unitarios por path pero **no test sistemático** del producto de las dimensiones. Bugs como "Squeze" (typo, ADR-0008) o "agresor cross-street" (HF4) podían colarse hasta producción.

## Decisión

Crear **`OpenScrape.App.Tests/DecisionMatrixIntegrationTests.cs`** con un `[TestCaseSource]` matriz que cubre 216 combinaciones del producto cartesiano:

- **3 streets** (Flop, Turn, River)
- **9 hand situations** (`OpenRaise`, `RaiseOverLimper`, `ThreeBet`, `OpenRaiseVs3Bet`, `Squeeze`, `VsSqueeze`, `FourBet`, `Cold4Bet`, `LimpRaise`)
- **2 positions** (IP / OOP)
- **2 bet states** (NoBet / facing Medium)
- **2 equity buckets** (20% / 80%)

Cada caso usa `MakeNeutralInput(...)` que produce un `PostflopDecisionInput` neutro (SPR ~10, `SemiDry`, `HighCard`, villain `Unknown`) — la equity es el único driver del resultado.

**4 invariantes direccionales** asertivos por caso (no comparan valores exactos, solo dirección):

1. `NoBet` → nunca `Call` (no hay nada que pagar).
2. `Eq80% + NoBet` → nunca `Fold` (sin necesidad de proteger nada y con equity premium).
3. `Eq80% + Medium bet` → nunca `Fold` (call/raise siempre).
4. `Eq20% + Medium bet` → nunca `Raise` (sin equity para bluff sustancial).

**Plus:** `Matrix_EachStreetThresholdsKeyHasEntry` guardrail que verifica que las claves `{Street}_{Situation}` en `appsettings.json` coinciden con las requeridas. **Detecta typos automáticamente.**

El fixture **carga el `StrategyProfile` real desde `appsettings.json`** vía `ConfigurationBuilder` — refleja producción, no un mock.

## Alternativas consideradas

1. **Tests más específicos por path.** Estado original parcial. Rechazado **como única estrategia**: cada path tiene sus propios tests, pero la combinatoria del matrix detecta interacciones que ningún test individual cubre.
2. **Tests con valores exactos esperados.** Tentador (ej: "case X debe retornar Bet 1/2"). Rechazado: cualquier ajuste de threshold rompería 100+ tests sin que indique un bug — solo señalaría tuning. Las invariantes direccionales son **estables a través de re-tuneos** y solo fallan en bug real.
3. **Property-based testing (FsCheck).** Considerado. Rechazado por verbosidad: el matrix es declarativo y suficientemente exhaustivo para los rangos relevantes. PBT sería overkill para 216 casos discretos.
4. **Snapshots.** Capturar el output de cada caso y comparar al test posterior. Rechazado: snapshots cambian con tuning legítimo y se vuelven ruido.
5. **Solo test el producto cartesiano completo (sin invariantes direccionales).** Rechazado: 216 casos sin assertion no aportan; al menos hay que aserir algo robusto.

## Consecuencias

**Positivas:**

- **+217 tests verdes en una batería** (1094 totales). El motor pasa los 216 al primer run sin bug encontrado — **confirma que el motor está internamente consistente** en la frontera de aserción.
- **Detección automática de drift** en `appsettings.json`: typo en clave threshold → guardrail falla.
- **Determinístico** (no usa Monte Carlo, equity llega como input). 7-9s en cold cache.
- **Invariantes direccionales robustas a tuning.** El test no se rompe cuando se ajusta un threshold de 25→27.
- Permite añadir invariantes (ej: "FlushDraw + flush completed → equity > base") sin reescribir el matrix.

**Negativas:**

- **216 casos cubren solo el producto principal.** Nuevas dimensiones (multiway, all-in, blocker) no están en la matrix. **Riesgo:** bugs en la combinación específica de 3+ flags raros.
- **No detecta bugs de tuning.** Si un threshold está mal calibrado pero la dirección sigue siendo correcta, el test pasa.
- Carga del `StrategyProfile` real ata el test a `appsettings.json` — si alguien edita el archivo en otro PR, los tests pueden romperse.

**Implicaciones para una migración:**

- En cualquier reimplementación, **mantener este matrix** (con las mismas invariantes direccionales) es la forma más eficiente de verificar paridad funcional.
- Los 216 casos son la **especificación viva** del motor más útil que cualquier documento.

## Referencias

- Commit `bb401c1 test(decision): add 216-case integration matrix for postflop engine`.
- `OpenScrape.App.Tests/DecisionMatrixIntegrationTests.cs`.
- `openspec/specs/decision-matrix-coverage/spec.md`.
- ADR-0006 (Pipeline unificado de equity y decisión).
- ADR-0008 (ThresholdsRegistry — el guardrail valida sus claves).
