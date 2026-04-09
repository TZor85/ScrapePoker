# Proposal: Bugfixes Críticos del Motor de Equity y Decisiones

## Contexto

Tras un análisis exhaustivo del motor de decisiones, se han identificado **16 issues** de diversa severidad que afectan la precisión del cálculo de equity, la calidad de las decisiones postflop, la estabilidad del game loop, y el modelado de oponentes.

Este documento prioriza los **3 bugfixes de impacto inmediato** que afectan directamente a las decisiones en tiempo real, y cataloga el resto para sprints posteriores.

## Prioridad Inmediata — 3 Bugfixes Críticos

### BF1: CalculateAllinEV — Fórmula Matemática Incorrecta

**Archivo:** `PostflopDecisionService.cs:1072-1079`
**Severidad:** CRÍTICA
**Impacto:** Sobreestima EV del all-in en TODAS las decisiones push/fold (SPR < 2.0)

**El bug:**
```csharp
// Fórmula actual (INCORRECTA):
double totalPotIfCalled = pot + 2 * stack;
return (equity / 100.0) * totalPotIfCalled - (1.0 - equity / 100.0) * stack;
```

**Derivación del error:**
- Hero va all-in con `stack`. Si villain call, pot total = `pot + 2*stack`
- Si hero gana: beneficio neto = `pot + 2*stack - stack` = `pot + stack`
- Si hero pierde: pérdida neta = `-stack`
- **EV correcto** = `(equity/100) × (pot + stack) - (1-equity/100) × stack`
  - Simplificado: `(equity/100) × (pot + 2×stack) - stack`
- **EV del código** = `(equity/100) × (pot + 2×stack) - (1-equity/100) × stack`
  - Simplificado: `(equity/100) × (pot + 3×stack) - stack`

**Diferencia:** El código sobreestima por `(equity/100) × stack`.

**Ejemplo numérico:** equity=50%, stack=100BB, pot=200BB:
- EV correcto: `0.5 × 400 - 100 = +100`
- EV del código: `0.5 × 400 - 0.5 × 100 = +150` ← sobreestima 50BB

**Consecuencia:** Hero hace push demasiado agresivo con manos marginales. En spots de SPR corto (muy frecuentes en turn/river), el bot va all-in cuando debería fold/call.

---

### BF2: TryDrawFromRange — Break Pierde Iteraciones Monte Carlo

**Archivo:** `MonteCarloSimulator.cs:529-565`
**Severidad:** ALTA
**Impacto:** Reduce precisión de equity vs ranges específicos en streets avanzados

**El bug:**
```csharp
foreach (var combo in combos)
{
    cumulative += combo.Weight;
    if (roll <= cumulative)
    {
        if (IsCardAvailable(...))
        {
            card1 = combo.Card1;
            card2 = combo.Card2;
            return true;
        }
        break;  // ← Sale del loop, desperdicia el intento
    }
}
```

Cuando el combo seleccionado está bloqueado, `break` descarta todo el intento en vez de buscar otro combo disponible. Con 10 intentos máximo y tasas de bloqueo altas en turn/river (7-9 cartas conocidas), iteraciones enteras se descartan silenciosamente.

**Ejemplo de impacto:** Con 70% de combos bloqueados (river común), cada intento tiene ~30% de éxito. En 10 intentos: ~97% de encontrar combo. Con 85% bloqueados: solo ~80%. Con 90%: ~65%. Las iteraciones perdidas reducen la muestra efectiva sin que el caller lo sepa.

**Consecuencia:** En river con ranges estrechos, la equity calculada tiene mayor varianza de la esperada, pudiendo desviar decisiones marginales.

---

### BF3: C-Bet con Ventana de Equity Demasiado Estrecha

**Archivo:** `PostflopDecisionService.cs:373-386`
**Severidad:** ALTA
**Impacto:** Hero solo hace c-bet con bluffs puros, nunca con manos de valor medio

**El bug:**
```csharp
if (!isFacingBet && heroIsAggressor && !isMultiway &&
    effectiveEquity < adjustedFoldBelow && effectiveEquity > adjustedFoldBelow - 15)
{
    double cbetFreq = GetCbetFrequency(street);
    if (Random.Shared.NextDouble() < cbetFreq)
        return new PostflopDecisionResult(cbetSize + " (C-Bet)", ...);
}
```

El c-bet **solo se activa cuando equity está en [FoldBelow-15, FoldBelow)** — una ventana de 15 puntos de equity baja. Esto significa:
- Equity por encima de FoldBelow → nunca se etiqueta como c-bet (pasa a lógica genérica de bet)
- Solo manos con equity DEBAJO del threshold de fold hacen c-bet

**Problema estratégico:** En GTO, el c-bet del agresor preflop cubre un **rango balanceado** de valor + bluff (típicamente 40-80% del range). El código actual solo apuesta como "c-bet" los bluffs más puros, creando una estrategia explotable donde el c-bet siempre = mano débil.

**Consecuencia:** El opponent puede explotar al hero: ante c-bet → siempre raise (porque hero siempre tiene manos débiles). Las manos de valor del hero nunca se etiquetan como c-bet, perdiendo el tracking de barrel consistency.

---

## Backlog — Issues por Prioridad

### Prioridad Alta (Sprint siguiente)

| ID | Descripción | Archivo | Impacto |
|----|-------------|---------|---------|
| H1 | Range narrowing no distingue bet-check-bet de bet-bet-bet | PostflopDecisionService:291-300 | Hero foldea demasiado vs patrones con check intercalado |
| H2 | Thin value ignora heroHandRank y pairClassification | PostflopDecisionService:538-549 | Bottom pair = overpair al mismo equity |
| H3 | Rangos del villain estáticos, sin adaptación a stats | VillainRange:31-73 | Equity mal calibrada vs villains observados |
| H4 | Double barrel fallthrough (no return en bad runout) | PostflopDecisionService:933-953 | Puede activar thin value tras decidir no barrel |
| H5 | Kicker strength solo aplica facing bet, no en bet sizing | PostflopDecisionService:302-309 | TPTK no apuesta más agresivo que TPWK |

### Prioridad Media (Game Loop y Concurrencia)

| ID | Descripción | Archivo | Impacto |
|----|-------------|---------|---------|
| M1 | Race condition en `_executeCapture` (volatile insuficiente) | FrmMain.cs | Capturas duplicadas/perdidas |
| M2 | `CurrentState` sin lock entre hilos | GameLoopStateMachine:37 | Transiciones inválidas posibles |
| M3 | OCR retries no limpian state entre intentos | FrmMain:1362-1384 | Datos corruptos en board |
| M4 | PostflopContext.Reset() antes de restaurar estado | FrmMain:725 | InitialBoardDanger = null |
| M5 | ForceState() bypasses toda validación | GameLoopStateMachine:135 | Estado inconsistente |

### Prioridad Baja (Refinamiento)

| ID | Descripción | Archivo | Impacto |
|----|-------------|---------|---------|
| L1 | Backdoor outs hardcoded a 1 (deberían ser ~2-3) | OutsCalculator:207-249 | Draws infravalorados ~2% |
| L2 | Flush draw penalty sin ajuste por blocker | DangerPenaltyCalculator:61-71 | Penalty inexacto con blockers |
| L3 | Danger penalties ignoran mejora del hero | DangerPenaltyCalculator:1-97 | Penaliza cuando hero mejoró |
| L4 | AF cliff en passive=0 (salta a 3.0) | OpponentProfile:43-52 | Clasificación poco fiable |
| L5 | Combo draw bonus sin ajustar por board texture | PostflopDecisionService:162-165 | Sobrevalora en monotone |
| L6 | Multiway OOP ×0.5 arbitrario sin justificación | PostflopDecisionService:251-262 | Penalty inconsistente |

## Nota sobre Issues Descartados

**Enumeración del Turn (MonteCarloSimulator:266-298):** Verificada manualmente como CORRECTA. El loop `for(i) skip(r) for(j>i) skip(r)` enumera C(n-1, 2) pares correctamente para cada river card.

**Combo weighting en BuildVillainCombos:** Verificado como correcto. Cada combo individual recibe el peso de la notación de mano, y la multiplicidad natural (12 combos offsuit vs 6 pairs) se maneja correctamente por la expansión de combos.

## Alcance de Este Cambio

Solo los 3 bugfixes de prioridad inmediata (BF1-BF3). El backlog queda documentado para sprints futuros.

## Impacto Esperado

- **BF1:** Decisiones push/fold correctas → menos pérdidas en spots de SPR corto
- **BF2:** Equity más precisa en river → mejores calls/folds marginales
- **BF3:** C-bet balanceado → menos explotabilidad, mejor tracking de barrels
