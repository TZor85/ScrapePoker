# Proposal: Motor de Decisiones v3 — Refinamiento y Consistencia Cross-Street

## Contexto

Tras completar los sprints S18-S21 (Motor v2: Explotacion, Balance, Spots Especificos, Precision), el motor cubre 50+ paths de decision con 60+ parametros configurables, opponent tracking avanzado (WTSD/W$SD/CR%/DonkBet%/Barrel), y ajustes por textura, posicion y SPR.

Un analisis exhaustivo post-implementacion revela **8 gaps residuales** que afectan el win rate estimado en 4-7 BB/100 acumulados. Los gaps se agrupan en 2 categorias:

1. **Consistencia** — Informacion calculada pero no usada, o decisiones por calle sin planificacion cross-street
2. **Precision avanzada** — Refinamientos que mejoran equity calculations y decision quality en spots marginales

---

## S22: Sprint Refinamiento (Unico Sprint)

### S22.1: Aplicar Tainted Outs al Equity Pipeline

**Archivo:** `PostflopDecisionService.cs`, `OutsCalculator.cs`
**Impacto:** +1-2 BB/100

**Problema:** `OutsCalculator` calcula `TaintedOuts` y `EffectiveOuts` correctamente (descuento 0.3-0.7 por outs que mejoran al villain), pero `PostflopDecisionService` usa `TotalOuts` (sin descuento) en todas las decisiones: semi-bluff, draw calling, implied odds. Resultado: hero sobreestima equity de draws en ~2-4 puntos.

**Solucion:**
- Pasar `EffectiveOuts` en vez de `TotalOuts` a PostflopDecisionService
- Usar `EffectiveOuts` en semi-bluff EV calculation y draw calling threshold
- Mantener `TotalOuts` solo para clasificacion de draws (HasFlushDraw, HasComboDraw)

---

### S22.2: River Runout Distinction (Blank vs Scare Card)

**Archivo:** `PostflopDecisionService.cs` — `HandleNoBet()`, `HandleFacingBet()`
**Impacto:** +1-2 BB/100

**Problema:** El river decision no distingue entre blank (2♦ que no cambia nada) y scare card (K♠ que completa flush draw). Los ajustes de `BoardChangeResult` aplican danger penalties al equity, pero las decisiones de bet/check/call no adaptan sizing ni frecuencia segun el tipo de carta que cayo.

**Solucion:**
- Clasificar river card como: `Blank` (sin cambio de textura), `Scare` (completa draw o overcard), `Neutral` (cambia algo menor)
- Blank river: hero puede value bet thinner (villain's missed draws → wider call range)
- Scare river: hero reduce sizing o check-back con manos marginales (villain rep draw)
- Facing bet en scare river: bluff catch threshold mas bajo (villain puede representar draw)

---

### S22.3: Opponent Profile por Posicion (BTN vs BB)

**Archivo:** `OpponentProfile.cs`, `OpponentTracker.cs`
**Impacto:** +1 BB/100

**Problema:** Un unico `OpponentProfile` por villain. En la practica, un jugador puede ser LAG 45/35 desde BTN pero TAG 18/14 desde BB. Usar stats globales contra un villain que abre 45% desde BTN pero foldea 80% desde EP pierde precision exploitativa.

**Solucion:**
- Agregar `Dictionary<TablePosition, PositionStats>` a `OpponentProfile` con contadores VPIP/PFR/AF por posicion
- `OpponentTracker.RecordHandPlayed` recibe posicion y acumula en sub-perfil
- Nuevo metodo `GetProfileForPosition(TablePosition)` que retorna stats posicionales (fallback a global si < 10 manos)
- PostflopDecisionService usa villain stats posicionales cuando estan disponibles

---

### S22.4: Stackoff Planning Cross-Street

**Archivo:** `PostflopDecisionService.cs`, `PostflopGameContext.cs`
**Impacto:** +1-2 BB/100

**Problema:** Cada calle decide independientemente. En turn con SPR 2.5 y TwoPair, hero apuesta 2/3 pot sin considerar que esto compromete al river. El resultado: hero apuesta turn, queda con SPR 0.8 en river, y luego la logica push/fold lo obliga a all-in con equity marginal.

**Solucion:**
- Calcular `projectedRiverSPR` antes de decidir sizing en turn
- Si `projectedRiverSPR < 1.5` tras bet: elegir sizing que mantenga river playable O commit ahora con all-in
- Flag `isCommittedAfterBet` en turn → si true, solo bet grande o check (no sizing intermedio que deja awkward river)
- Nuevo campo en PostflopGameContext: `TurnBetCommitsToRiver`

---

### S22.5: Multiway Nut Advantage

**Archivo:** `PostflopDecisionService.cs`
**Impacto:** +0.5-1 BB/100

**Problema:** En pots 3-way+, si hero tiene nuts (flush, straight, set), el penalty multiway se aplica igual que con manos marginales. Pero con nuts en multiway, villain no puede bluffear efectivamente (otro villain puede call/raise), y hero extrae valor de ambos.

**Solucion:**
- Si `heroHandRank >= Flush` (o `ThreeOfAKind` en board no paired) en multiway: reducir multiway penalty 50%
- Si `heroHandRank >= Straight` en multiway IP: penalty reducido 30%
- No aplica a OnePair/TwoPair (aun vulnerables multiway)

---

### S22.6: Bluff Frequency Basada en Equity

**Archivo:** `PostflopDecisionService.cs` — `HandleLowEquity()`
**Impacto:** +0.5-1 BB/100

**Problema:** Bluff frequencies son estaticas: 15% flop, 12% turn, 10% river (× BluffFrequencyMultiplier). GTO sugiere que la frecuencia optima depende de la equity en el boundary: cuanto mas cerca del threshold, mas mixing.

**Solucion:**
- Reemplazar freq estatica por: `bluffFreq = baseFreq × (1 - (threshold - equity) / threshold)`
- Equity muy baja (ej: 5%): bluff freq reducida (no desperdicia chips)
- Equity cerca del threshold (ej: 38% vs FoldBelow 40%): bluff freq maxima (mixing zone)
- Mantener modulacion por opponent type (WTSD, etc.) como multiplicador final

---

### S22.7: Pot Commitment Range Expandido

**Archivo:** `PostflopDecisionService.cs` — `HandleFacingBet()`, `HandleLowEquity()`
**Impacto:** +0.5 BB/100

**Problema:** Pot commitment se activa solo con SPR < 0.5. Teoria dice que con equity > 33% y SPR < 1.5, hero deberia commitear (call/shove). Hero actualmente foldea manos +EV cuando SPR esta entre 0.5-1.5.

**Solucion:**
- Expandir pot commitment check a SPR < 1.5 con equity minima escalada:
  - SPR < 0.5: commit si EV(call) > 0 (como ahora)
  - SPR 0.5-1.0: commit si equity > 30%
  - SPR 1.0-1.5: commit si equity > 38%
- Parametros configurables: `PotCommitmentSPRExpanded`, `PotCommitmentEquityMedium`, `PotCommitmentEquityWide`

---

### S22.8: Hand Strength Re-Evaluation en River

**Archivo:** `PostflopDecisionService.cs`
**Impacto:** +0.5 BB/100

**Problema:** `heroHandRank` se calcula una vez y no se re-evalua en contexto. Cuando flush completa en river, hero's TwoPair sigue siendo "TwoPair" pero su valor relativo cayo drasticamente. El danger penalty ajusta equity, pero las decisiones de raise/call siguen confiando en HandRank absoluto.

**Solucion:**
- Nuevo concepto `relativeHandStrength`: HandRank ajustado por board completions
- Si flush completed y hero no tiene flush: TwoPair → tratar como OnePair para raise decisions
- Si straight completed y hero no tiene straight: TwoPair → tratar como OnePair
- No raise con TwoPair cuando draw completo en board (actualmente puede raise porque HandRank >= TwoPair)
- Afecta: HandleFacingBet raise thresholds, HandleNoBet overbet decisions

---

## Resumen de Impacto

| Item | Descripcion | BB/100 | Tests | Complejidad |
|------|-------------|--------|-------|-------------|
| S22.1 | Tainted outs al pipeline | +1-2 | 6 | Baja |
| S22.2 | River blank vs scare | +1-2 | 8 | Media |
| S22.3 | Opponent profile posicional | +1 | 8 | Media |
| S22.4 | Stackoff planning | +1-2 | 8 | Alta |
| S22.5 | Multiway nut advantage | +0.5-1 | 6 | Baja |
| S22.6 | Bluff freq por equity | +0.5-1 | 6 | Baja |
| S22.7 | Pot commitment expandido | +0.5 | 6 | Baja |
| S22.8 | Hand strength re-eval | +0.5 | 6 | Media |
| **Total** | | **+5.5-9.5** | **~54** | |

## Dependencias

- S22.1 es independiente (puede ir primero por facilidad)
- S22.2 antes de S22.8 (river runout classification alimenta re-evaluation)
- S22.3 es independiente
- S22.4 es independiente pero complejo (puede ir ultimo)
- S22.5-S22.7 son independientes y simples

## Orden Recomendado

1. S22.1 + S22.5 + S22.6 + S22.7 (bajo riesgo, alto ROI)
2. S22.2 + S22.8 (river improvements, dependientes)
3. S22.3 (opponent modeling)
4. S22.4 (cross-street planning, mas complejo)
