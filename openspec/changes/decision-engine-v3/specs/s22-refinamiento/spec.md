# S22: Sprint Refinamiento — Especificacion BDD

## S22.1: Aplicar Tainted Outs al Equity Pipeline

### Scenario: Semi-bluff usa EffectiveOuts en vez de TotalOuts
```
Given hero tiene flush draw (9 total outs, 2 tainted)
  And EffectiveOuts = 7 + 2×0.7 = 8.4
  And street es Turn
When PostflopDecisionService calcula semi-bluff EV
Then draw equity usa EffectiveOuts (8.4) en vez de TotalOuts (9)
  And breakevenFE se ajusta por EffectiveOuts
```

### Scenario: Draw calling usa EffectiveOuts para implied odds
```
Given hero tiene OESD (8 total outs, 3 tainted)
  And EffectiveOuts = 5 + 3×0.3 = 5.9
When PostflopDecisionService evalua call con implied odds
Then draw equity = EffectiveOuts × multiplier (no TotalOuts)
```

### Scenario: Clasificacion de draw sigue usando TotalOuts
```
Given hero tiene 9 flush outs (2 tainted)
When OutsResult clasifica draws
Then HasFlushDraw = true (usa TotalOuts >= 9)
  And HasComboDraw se basa en TotalOuts (no EffectiveOuts)
```

### Scenario: Sin tainted outs no hay diferencia
```
Given hero tiene 8 outs con 0 tainted
When PostflopDecisionService calcula decisiones
Then EffectiveOuts == TotalOuts == 8
  And comportamiento identico al actual
```

---

## S22.2: River Runout Distinction (Blank vs Scare)

### Scenario: Blank river permite thin value bet
```
Given street es River
  And river card es 2d (no completa draws, no overcard)
  And hero tiene OnePair TopPair
  And equity > ThinValueAbove
When PostflopDecisionService determina accion
Then puede value bet thinner (threshold reducido -2)
  And reason contiene "blank river"
```

### Scenario: Scare river reduce sizing y frecuencia
```
Given street es River
  And river card completa flush draw (4th suited card)
  And hero tiene TwoPair (no flush)
When PostflopDecisionService determina accion sin facing bet
Then sizing se reduce un nivel (ReduceBetSize)
  And puede checkear con equity marginal
```

### Scenario: Facing bet en scare river — bluff catch mas facil
```
Given street es River
  And river card es scare (completa draw)
  And villain apuesta
  And hero tiene OnePair con equity >= bluff catch threshold
When PostflopDecisionService determina accion
Then bluff catch threshold se reduce ×0.90 (villain puede representar draw)
```

### Scenario: Blank river facing bet — respetar mas
```
Given street es River
  And river card es blank
  And villain apuesta
When PostflopDecisionService determina accion
Then bluff catch threshold no se reduce (villain menos probable bluffing)
```

### Scenario: Clasificacion de river card
```
Given board previo es Ah-7h-3c-Ks
  And river card es 2d
When se clasifica river card
Then tipo = Blank (no completa draws, no overcard, no parea board)
```

### Scenario: Scare card completa straight
```
Given board previo es Jh-Tc-9s-2d
  And river card es 8c
When se clasifica river card
Then tipo = Scare (completa straight 8-9-T-J)
```

---

## S22.3: Opponent Profile por Posicion

### Scenario: Trackear VPIP por posicion
```
Given villain juega mano desde Button
  And villain pone dinero voluntariamente
When OpponentTracker.RecordHandPlayed(alias, position)
Then VPIP counter de Button se incrementa
  And VPIP global tambien se incrementa
```

### Scenario: GetProfileForPosition retorna stats posicionales
```
Given villain tiene 20 manos desde Button (VPIP 45%)
  And villain tiene 15 manos desde BigBlind (VPIP 18%)
When GetProfileForPosition(Button)
Then retorna VPIP 45% (no global)
```

### Scenario: Fallback a global con pocas manos
```
Given villain tiene 5 manos desde Button (< 10 minimo)
  And villain tiene 30 manos globales (VPIP 30%)
When GetProfileForPosition(Button)
Then retorna VPIP global 30% (fallback)
```

### Scenario: PostflopDecisionService usa stats posicionales
```
Given villain esta en Button
  And villain tiene stats posicionales fiables para Button
When PostflopDecisionService evalua villain type
Then usa VPIP/AF posicional de Button (no global)
```

---

## S22.4: Stackoff Planning Cross-Street

### Scenario: Turn bet que compromete river — all-in directo
```
Given street es Turn
  And heroStack = 30, potSize = 20 (SPR = 1.5)
  And hero quiere apostar 2/3 pot (13.3)
  And projectedRiverSPR = (30-13.3) / (20+26.6) = 0.36
When PostflopDecisionService evalua sizing
Then projectedRiverSPR < 0.5 → isCommittedAfterBet = true
  And sizing cambia a All-In (commit ahora, no dejar river awkward)
```

### Scenario: Turn bet que mantiene river playable
```
Given street es Turn
  And heroStack = 60, potSize = 20 (SPR = 3.0)
  And hero quiere apostar 1/2 pot (10)
  And projectedRiverSPR = (60-10) / (20+20) = 1.25
When PostflopDecisionService evalua sizing
Then projectedRiverSPR > 1.0 → isCommittedAfterBet = false
  And sizing normal (Bet 1/2)
```

### Scenario: Turn con equity marginal y SPR bajo — check-back
```
Given street es Turn
  And SPR = 2.0
  And equity = 48% (marginal)
  And bet 1/2 → projectedRiverSPR = 0.75
When PostflopDecisionService evalua sizing
Then projectedRiverSPR < 1.0 con equity marginal → preferir Check
  And razon "pot control, evitar river committed"
```

### Scenario: Facing bet en turn con SPR que compromete
```
Given villain apuesta en turn
  And call dejaria SPR < 0.5
  And hero tiene equity > 35%
When PostflopDecisionService evalua call
Then pot commitment expandido: SPR < 1.0 + equity > 35% → Call o Raise
```

---

## S22.5: Multiway Nut Advantage

### Scenario: Flush en 3-way — penalty reducido 50%
```
Given numOpponents = 2 (3-way pot)
  And heroHandRank >= Flush
When PostflopDecisionService calcula multiway penalty
Then penalty se reduce 50% (hero tiene nuts, villains no pueden bluffear entre si)
```

### Scenario: Set en 3-way IP — penalty reducido 30%
```
Given numOpponents = 2
  And heroHandRank == ThreeOfAKind
  And board no paired (set real, no trips)
  And hero IP
When PostflopDecisionService calcula multiway penalty
Then penalty se reduce 30%
```

### Scenario: OnePair en 3-way — penalty completo
```
Given numOpponents = 2
  And heroHandRank == OnePair
When PostflopDecisionService calcula multiway penalty
Then penalty sin reduccion (mano vulnerable multiway)
```

### Scenario: TwoPair en 3-way — penalty completo
```
Given numOpponents = 2
  And heroHandRank == TwoPair
When PostflopDecisionService calcula multiway penalty
Then penalty sin reduccion (TwoPair aun vulnerable en multiway)
```

---

## S22.6: Bluff Frequency Basada en Equity

### Scenario: Equity muy baja — bluff freq reducida
```
Given equity = 5% (muy lejos del threshold FoldBelow 40%)
  And street es Turn
When PostflopDecisionService calcula bluff frequency
Then bluffFreq = baseFreq × (1 - (40-5)/40) = baseFreq × 0.125
  And casi nunca bluffea (desperdiciar chips)
```

### Scenario: Equity cerca del threshold — bluff freq maxima
```
Given equity = 38% (cerca de FoldBelow 40%)
  And street es Turn
When PostflopDecisionService calcula bluff frequency
Then bluffFreq = baseFreq × (1 - (40-38)/40) = baseFreq × 0.95
  And bluffea casi a frecuencia base (mixing zone)
```

### Scenario: Equity en medio — bluff freq intermedia
```
Given equity = 25% (medio entre 0 y FoldBelow 40%)
  And street es Turn
When PostflopDecisionService calcula bluff frequency
Then bluffFreq = baseFreq × (1 - (40-25)/40) = baseFreq × 0.625
```

### Scenario: Modulacion por opponent type se mantiene
```
Given equity = 30%
  And villainType = LAG (WTSD bluff multiplier activo)
When PostflopDecisionService calcula bluff frequency
Then bluffFreq = equityBasedFreq × wtsdMultiplier × opponentModifier
  And todos los multiplicadores existentes se aplican sobre la freq basada en equity
```

---

## S22.7: Pot Commitment Range Expandido

### Scenario: SPR 0.8 con equity 32% — commit
```
Given hero facing bet
  And SPR = 0.8 (entre 0.5 y 1.0)
  And equity = 32% (> 30% threshold para SPR 0.5-1.0)
When PostflopDecisionService evalua pot commitment
Then retorna Call (committed, EV positivo)
```

### Scenario: SPR 1.2 con equity 40% — commit
```
Given hero facing bet
  And SPR = 1.2 (entre 1.0 y 1.5)
  And equity = 40% (> 38% threshold para SPR 1.0-1.5)
When PostflopDecisionService evalua pot commitment
Then retorna Call (committed)
```

### Scenario: SPR 1.2 con equity 35% — no commit
```
Given hero facing bet
  And SPR = 1.2
  And equity = 35% (< 38% threshold)
When PostflopDecisionService evalua pot commitment
Then no activa pot commitment (equity insuficiente para SPR 1.0-1.5)
  And decision normal (probablemente Fold)
```

### Scenario: SPR 2.0 — fuera de rango commitment
```
Given hero facing bet
  And SPR = 2.0 (> 1.5)
  And equity = 35%
When PostflopDecisionService evalua pot commitment
Then pot commitment no aplica (SPR fuera de rango)
```

---

## S22.8: Hand Strength Re-Evaluation en River

### Scenario: TwoPair con flush completed — no raise
```
Given street es River
  And heroHandRank = TwoPair
  And boardChange.FlushCompleted = true
  And hero no tiene flush
When PostflopDecisionService evalua raise vs bet
Then TwoPair se trata como OnePair para raise decisions
  And no raise (pot control ante posible flush villain)
```

### Scenario: TwoPair con straight completed — no overbet
```
Given street es River
  And heroHandRank = TwoPair
  And boardChange.StraightCompleted = true
  And hero no tiene straight
When PostflopDecisionService evalua overbet
Then overbet bloqueado (TwoPair degradado, straight posible)
```

### Scenario: TwoPair sin draw completed — raise normal
```
Given street es River
  And heroHandRank = TwoPair
  And boardChange = Safe (sin draws completados)
When PostflopDecisionService evalua raise
Then TwoPair mantiene su valor → raise permitido normalmente
```

### Scenario: Flush con board paired — no degradar
```
Given street es River
  And heroHandRank = Flush
  And boardChange.BoardPaired = true
When PostflopDecisionService evalua
Then Flush no se degrada (solo full house o quads le ganan)
  And value bet normal
```

---

## Parametros Nuevos en StrategyProfile

```
// S22.2 — River Runout
RiverBlankThinValueBonus: -2.0
RiverScareCheckThreshold: 0.55
RiverScareBluffCatchReduction: 0.90
RiverScareSizingReduction: true

// S22.4 — Stackoff Planning
StackoffProjectedSPRThreshold: 1.0
StackoffCommitEquityMin: 0.35

// S22.5 — Multiway Nut Advantage
MultiwayNutPenaltyReduction: 0.50
MultiwayStrongPenaltyReduction: 0.30

// S22.6 — Bluff Frequency
BluffFreqEquityScaling: true

// S22.7 — Pot Commitment Expandido
PotCommitmentSPRExpanded: 1.5
PotCommitmentEquityMedium: 0.30
PotCommitmentEquityWide: 0.38

// S22.8 — Hand Strength Re-Eval
HandReEvalOnDrawCompletion: true
```

## Tests Estimados

- S22.1: 6 tests (effective outs en semi-bluff, draw call, clasificacion, sin tainted)
- S22.2: 8 tests (blank/scare classification, value/check/bluff catch adjustments)
- S22.3: 8 tests (track posicional, fallback, uso en decisions)
- S22.4: 8 tests (commit sizing, check-back, facing bet, projected SPR)
- S22.5: 6 tests (flush/set/pair multiway, IP/OOP)
- S22.6: 6 tests (equity baja/media/alta, con modifiers)
- S22.7: 6 tests (SPR ranges, equity thresholds, fuera de rango)
- S22.8: 6 tests (TwoPair degradado, flush ok, sin draw)
- **Total: ~54 tests**
