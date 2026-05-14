# Tasks: Motor de Decisiones v3

## S22: Sprint Refinamiento

### S22.1: Aplicar Tainted Outs al Equity Pipeline
- [x] Modificar `PostflopDecisionInput.cs`: agregar campo `EffectiveOuts` (double)
- [x] Modificar `GameCoordinator.cs`: pasar `outsResult.EffectiveOuts` al input
- [x] Modificar `PostflopDecisionService.cs` — semi-bluff path: usar EffectiveOuts para drawEquity
- [x] Modificar `PostflopDecisionService.cs` — draw calling path: usar EffectiveOuts para implied odds
- [x] Mantener `TotalOuts` en draw classification (HasFlushDraw, HasComboDraw checks)
- [x] Tests: 6 tests (semi-bluff con tainted, draw call, clasificacion, sin tainted)

### S22.2: River Runout Distinction
- [x] Crear enum `RiverCardType { Blank, Scare, Neutral }` en Domain
- [x] Agregar metodo `ClassifyRiverCard()` en `BoardTextureAnalyzer.cs`
- [x] Agregar campo `RiverCardType` a `PostflopDecisionInput.cs`
- [x] Modificar `HandleNoBet()`: blank → thin value bonus -2, scare → ReduceBetSize + check marginal
- [x] Modificar `HandleFacingBet()`: scare → bluff catch threshold ×0.90
- [x] Agregar parametros a `StrategyProfile.cs`: RiverBlankThinValueBonus, RiverScareBluffCatchReduction, etc.
- [x] Tests: 8 tests (clasificacion, value/check adjustments, bluff catch)

### S22.3: Opponent Profile por Posicion
- [ ] Crear record `PositionStats { HandsPlayed, VPIP, PFR, AF }` en `OpponentProfile.cs`
- [ ] Agregar `Dictionary<TablePosition, PositionStats>` a `OpponentProfile.cs`
- [ ] Modificar `OpponentTracker.RecordHandPlayed()`: recibir posicion, acumular en sub-perfil
- [ ] Modificar `OpponentTracker.RecordVPIP()` / `RecordPFR()`: recibir posicion
- [ ] Agregar metodo `GetProfileForPosition(TablePosition)` con fallback a global si < 10 manos
- [ ] Modificar `GameCoordinator.cs`: pasar posicion villain al tracker
- [ ] Modificar `PostflopDecisionService.cs`: usar stats posicionales si disponibles
- [ ] Tests: 8 tests (track posicional, fallback, tipo posicional, uso en decisions)

### S22.4: Stackoff Planning Cross-Street
- [x] Agregar metodo `CalculateProjectedRiverSPR()` en `PostflopDecisionService.cs`
- [x] Agregar campo `TurnBetCommitsToRiver` a `PostflopGameContext.cs`
- [x] Modificar `HandleNoBet()` turn: si projected SPR < 1.0 con equity buena → All-In directo
- [x] Modificar `HandleNoBet()` turn: si projected SPR < 1.0 con equity marginal → Check (pot control)
- [x] Modificar `HandleFacingBet()` turn: pot commitment expandido con projected SPR
- [x] Agregar parametros: StackoffProjectedSPRThreshold, StackoffCommitEquityMin
- [x] Tests: 8 tests (commit sizing, check-back, facing bet, projected SPR scenarios)

### S22.5: Multiway Nut Advantage
- [x] Modificar multiway penalty block en `DetermineAction()`:
  - Si heroHandRank >= Flush → penalty ×0.50
  - Si heroHandRank == ThreeOfAKind + board no paired + IP → penalty ×0.70
  - OnePair/TwoPair: sin reduccion
- [x] Agregar parametros: MultiwayNutPenaltyReduction, MultiwayStrongPenaltyReduction
- [x] Tests: 6 tests (flush/set/pair en multiway, IP/OOP, board paired)

### S22.6: Bluff Frequency Basada en Equity
- [x] Modificar `HandleLowEquity()`: reemplazar freq estatica por `baseFreq × (1 - (threshold-equity)/threshold)`
- [x] Mantener multiplicadores existentes (WTSD, opponent type, SPR) como overlay
- [x] Agregar parametro: BluffFreqEquityScaling (bool, default true)
- [x] Tests: 6 tests (equity baja/media/alta freq, con modifiers, disabled)

### S22.7: Pot Commitment Range Expandido
- [x] Modificar pot commitment check en `HandleFacingBet()` y `HandleLowEquity()`:
  - SPR 0.5-1.0: commit si equity > PotCommitmentEquityMedium (30%)
  - SPR 1.0-1.5: commit si equity > PotCommitmentEquityWide (38%)
- [x] Agregar parametros: PotCommitmentSPRExpanded, PotCommitmentEquityMedium, PotCommitmentEquityWide
- [x] Tests: 6 tests (SPR ranges, equity thresholds, boundaries)

### S22.8: Hand Strength Re-Evaluation en River
- [x] Agregar metodo `GetRelativeHandRank()` que degrada HandRank si draws completaron:
  - TwoPair + FlushCompleted (hero sin flush) → tratar como OnePair para raise decisions
  - TwoPair + StraightCompleted (hero sin straight) → tratar como OnePair
  - Flush/Straight+ → no degradar
- [x] Modificar `HandleFacingBet()` river: usar relativeHandRank para raise conditions
- [x] Modificar `HandleNoBet()` river: usar relativeHandRank para overbet/value conditions
- [x] Agregar parametro: HandReEvalOnDrawCompletion (bool, default true)
- [x] Tests: 6 tests (TwoPair degradado, flush ok, sin draw, OnePair no afectado)

---

## Orden de Implementacion

**Fase 1 — Quick wins (bajo riesgo):**
1. S22.1: Tainted outs (ya calculados, solo conectar)
2. S22.5: Multiway nut advantage (3 lineas)
3. S22.6: Bluff freq por equity (formula simple)
4. S22.7: Pot commitment expandido (extender check existente)

**Fase 2 — River improvements:**
5. S22.2: River blank vs scare (nuevo concepto)
6. S22.8: Hand strength re-eval (depende de S22.2)

**Fase 3 — Structural:**
7. S22.3: Opponent profile posicional (refactor tracking)
8. S22.4: Stackoff planning (cross-street logic)
