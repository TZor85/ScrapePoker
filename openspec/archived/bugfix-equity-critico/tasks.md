# Tasks: Bugfixes y Mejoras del Motor de Decisiones (2026-04-07)

## BF1 — CalculateAllinEV: Fórmula Incorrecta ✅

- [x] Corregir fórmula: `equityFraction * (pot + stack) - (1 - equityFraction) * stack`
- [x] Método cambiado a `internal static` + InternalsVisibleTo en .csproj
- [x] 8 tests unitarios (equity baja/alta, breakeven, edge cases, no-sobreestimación)
- [x] 9 tests existentes push/fold siguen pasando
- [x] Build 0 errores, 600 tests pasan

## BF2 — TryDrawFromRange: Rejection Sampling ✅

- [x] Intentos máximos 10→20
- [x] `EquityResult.SkippedSimulations` expuesto para diagnóstico
- [x] Tracking de skipped ya existía (totalSkipped, effectiveCount)
- [x] 19 tests MC pasan

## BF3 — C-Bet Mixing: Rango del Agresor ✅

- [x] Bloque de mixing en `DetermineAction` antes de `HandleNoBet`
- [x] Condición: `heroIsAggressor && !isMultiway && equity ∈ [FoldBelow, ThinValueAbove)`
- [x] Check a frecuencia `(1-cbetFreq)` para proteger checking range
- [x] 600 tests pasan

## H1 — Range Narrowing: Bet-Check-Bet ✅

- [x] `narrowingMultiplier = villainCheckedMiddleStreet ? 0.5 : 1.0`
- [x] Check intermedio indica debilidad → mitad del range narrowing penalty

## H2 — Thin Value con HandRank ✅

- [x] TopPair+/TwoPair+ → Call en cualquier posición
- [x] BottomPair/MiddlePair OOP → Fold
- [x] Manos sin clasificación → fallback al comportamiento original

## H3 — Rangos Adaptativos por OpponentProfile ✅

- [x] `VillainRange.GetForSituation(situation, position, OpponentProfile?)`
- [x] VPIP escala ancho: ratio `observedVPIP / expectedVPIP`, clamp [0.5, 2.0]
- [x] 3Bet% ajusta rangos de 3bet vs promedio 6%
- [x] Sin datos fiables (<10 manos) → rango base sin cambios
- [x] `IPokerCalculator.Calculate` acepta `OpponentProfile?`
- [x] `IGameCoordinator.GetActiveVillainProfile(state)` nuevo método
- [x] 4 call sites en FrmMain (flop/turn/river/preflop) pasan opponentProfile
- [x] 9 tests nuevos (VpipMultiplier, clamp, adaptive range, 3bet, null profile)
- [x] Build 0 errores, 609 tests pasan

## H4 — Double Barrel Return Explícito ✅

- [x] bad runout → `return Check` explícito (antes fallthrough silencioso)

## H5 — Kicker en Bet Sizing ✅ (ya existía)

- [x] Verificado: kicker ya se usaba en facing bet (302-309) Y HandleNoBet (931-953)

## M2+M5 — GameLoopStateMachine Estabilidad ✅

- [x] `lock(_stateLock)` en TryTransition, Reset, ForceState
- [x] ForceState valida estado destino existe en `_validTransitions`
- [x] 609 tests pasan

## Descartados Tras Verificación

- M1: `_executeCapture` — `this.Invoke` sincrónico, `volatile` suficiente
- M3: OCR retries — `dataBoard` se reasigna cada iteración
- M4: PostflopContext.Reset() — `InitialBoardDanger` default `Safe` se maneja correctamente
- Turn enumeration loop — Correcto: enumera C(n-1,2) pares sin sesgo
- Combo weighting — Correcto: multiplicidad natural por expansión de combos

## Pendiente Baja Prioridad (L1-L6)

- [ ] L1: Backdoor outs hardcoded a 1 (OutsCalculator:207-249, deberían ser ~2-3)
- [ ] L2: Flush draw penalty sin blocker adjustment (DangerPenaltyCalculator:61-71)
- [ ] L3: Danger penalties ignoran mejora del hero (DangerPenaltyCalculator:1-97)
- [ ] L4: AF cliff passive=0 salta a 3.0 (OpponentProfile:43-52)
- [ ] L5: Combo draw bonus sin texture adjustment (PostflopDecisionService:162-165)
- [ ] L6: Multiway OOP ×0.5 arbitrario (PostflopDecisionService:251-262)
