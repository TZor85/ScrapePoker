# Tasks: Motor de Decisiones v2

## S18: Sprint Explotacion (prerequisito: S18.3 primero)

### S18.3: Stats de Villain Ampliados
- [ ] Agregar campos a `OpponentProfile.cs`: TimesWentToShowdown, TimesWonAtShowdown, TimesCheckRaised, TimesCheckRaiseOpportunity, TimesDonkBet, TimesDonkBetOpportunity
- [ ] Agregar propiedades calculadas: WTSDPct, WSDPct, CheckRaisePct, DonkBetPct
- [ ] Agregar reliability checks: HasReliableWTSDData (>=15), HasReliableCheckRaiseData (>=10)
- [ ] Agregar metodos a `OpponentTracker.cs`: TrackShowdownResult, TrackCheckRaise, TrackDonkBet
- [ ] Integrar tracking en `GameCoordinator.cs` / `FrmMain.cs` (llamar a Track* en momentos correctos)
- [ ] Agregar adjustments en `PostflopDecisionService.cs`:
  - WTSD > 50%: BluffFreq x 0.6, ValueBetThreshold -3
  - WTSD < 25%: BluffFreq x 1.4
  - W$SD > 60%: FoldBelow +2
  - CR% > 15%: CbetFreq x 0.7
- [ ] Agregar parametros a `StrategyProfile.cs`
- [ ] Tests: 16 tests (tracking + adjustments)

### S18.2: Barrel Frequency Tracking
- [ ] Agregar campos a `OpponentProfile.cs`: TimesBarreled, TimesBarrelOpportunity, BarrelFrequency, HasReliableBarrelData (>=8)
- [ ] Agregar TrackBarrel a `OpponentTracker.cs`
- [ ] Integrar tracking en game loop (detectar barrel = bet flop + bet turn)
- [ ] Agregar adjustment en `PostflopDecisionService.cs`: comparar observedBarrelFreq vs expectedBarrelFreq por villainType
- [ ] Agregar parametros: BarrelFrequencyOverThreshold, BarrelOverAdjustment, etc.
- [ ] Tests: 10 tests

### S18.1: Explotacion de Donk Bets
- [ ] Agregar parametro `isDonkBet` en `HandleFacingBet()` o usar context existente
- [ ] Implementar raise agresivo vs donk (3.5x, freq 70%)
- [ ] Implementar call amplio vs donk (FoldBelow -3)
- [ ] Implementar raise pot vs donk con nut hand
- [ ] Integrar con DonkBetPct de S18.3 (+20% raise si villain donkea >20%)
- [ ] Agregar parametros: DonkBetRaiseFrequency, DonkBetCallBonus, DonkBetRaiseSizing
- [ ] Tests: 8 tests

---

## S19: Sprint Balance

### S19.1: Check-Raise Mixing
- [ ] Agregar `CheckRaiseMixingEnabled` a `StrategyProfile.cs`
- [ ] Agregar parametros de frecuencia por situacion (CRMixFreqOOPStrong, etc.)
- [ ] Refactorizar seccion check-raise en `PostflopDecisionService.cs` para ser probabilistico
- [ ] Implementar tabla de frecuencias: OOP TwoPair+ 40%, OOP TP+FD 35%, OOP OESD 30%, IP TwoPair+ 20%
- [ ] Respetar SPR guard existente (SPR < 1.5 + equity < 60% → skip CR)
- [ ] Tests: 10 tests (mixing, SPR guard, disabled mode, distribuciones)

### S19.2: C-Bet Turn Ajustada por Textura
- [ ] Crear metodo `GetAdjustedCbetFrequency(street, boardChange, baseCbetFreq)`
- [ ] Implementar multiplicadores: FlushCompleted x0.30, FlushDraw x0.50, Paired x0.60, Straight x0.40, Brick x1.10
- [ ] Acumulacion multiplicativa para multiples cambios
- [ ] Solo aplica a Turn (flop/river sin cambio)
- [ ] Agregar parametros: CbetTurnFlushCompletedMultiplier, etc.
- [ ] Tests: 10 tests

### S19.3: Defensa en 3Bet Pots Postflop
- [ ] Crear metodo `Handle3BetPotDefenseOOP()` en `PostflopDecisionService.cs`
- [ ] Flop OOP: TwoPair+ CR 50%, comboDraw CR 35%/fold 65%, equity baja fold (no float)
- [ ] Turn OOP: agresor check → probe 40%, agresor barrel → CR 20% con TwoPair+
- [ ] IP caller: call 85%, raise 15% con TPTK
- [ ] Agregar parametros: ThreeBetPotCRFreqStrong, ThreeBetPotNoFloat, etc.
- [ ] Tests: 12 tests

---

## S20: Sprint Spots

### S20.1: Blind vs Blind Thresholds
- [ ] Crear metodo `GetPositionalThresholdAdjustment(heroPosition, villainPosition)`
- [ ] SBvsBB: FoldBelow -3, ThinValue -2
- [ ] BBvsSB: FoldBelow -5, ThinValue -3
- [ ] BBvsBTN: FoldBelow -1
- [ ] Aplicar ajuste en DetermineAction antes de evaluar thresholds
- [ ] Agregar parametros
- [ ] Tests: 6 tests

### S20.2: Deteccion de Limp-Raise
- [ ] Agregar `LimpRaise` a `HandSituation` enum
- [ ] Detectar limp-raise en `PreflopAnalyzer.cs`
- [ ] Agregar rango super-premium en `VillainRange.cs` (~3%)
- [ ] Thresholds postflop: FoldBelow +8, ThinValue +5
- [ ] Tests: 8 tests

### S20.3: Reverse Implied Odds — Bluff Risk
- [ ] Agregar componente bluffRiskPenalty a `CalculateReverseImpliedOdds()`
- [ ] Solo aplica: Turn, OnePair, board con draws, villain no all-in
- [ ] Ajuste por villainType: LAG x1.5, TP x0.5
- [ ] Skip si heroHandRank >= TwoPair
- [ ] Tests: 8 tests

### S20.4: Squeeze Defense
- [ ] Implementar `HandleSqueezeDecision()` en `PreflopAnalyzer.cs`
- [ ] Premium 4bet siempre, QQ/AQ condicional por SPR
- [ ] Call con blocker IP, fold sin blocker OOP
- [ ] Thresholds postflop squeeze: FoldBelow +6, ThinValue +4
- [ ] Tests: 8 tests

---

## S21: Sprint Precision

### S21.1: Overcard Outs por Texture
- [ ] Modificar calculo de overcard outs en `OutsCalculator.cs`
- [ ] Board conectado: 2 outs (tainted), Board paired: 2, Seco: 3, Blocker: x1.2
- [ ] Agregar parametros
- [ ] Tests: 6 tests

### S21.2: Multiway Penalty por Posicion
- [ ] Modificar oopMultiplier en `PostflopDecisionService.cs`: SB 0.70, BB 0.50, EP 0.60
- [ ] Amplificar x1.3 si villain IP y agresor
- [ ] Agregar parametros
- [ ] Tests: 8 tests

### S21.3: Broadway Wet
- [ ] Agregar broadwayConnectedBonus en `BoardTextureAnalyzer.cs`
- [ ] Bonus +20 si broadwayCount >= 2 y conectadas
- [ ] Ajuste en thresholds: FoldBelow +3, ThinValue +2, Cbet x0.8
- [ ] Tests: 8 tests

### S21.4: Backdoor Draw Overlap
- [ ] Verificar overlap entre main flush draw y backdoor straight
- [ ] Descuento x0.5 por cada out que overlappea
- [ ] Solo aplica si hay main flush draw activo
- [ ] Tests: 5 tests

### S21.5: Randomizacion Margen Variable
- [ ] Cambiar margen fijo a variable por villainType: LAG 5, TAG 3, LP 4, TP 2, Unknown 3
- [ ] Agregar parametros
- [ ] Tests: 5 tests

---

## Resumen

| Sprint | Items | Tests est. | Dependencias |
|--------|-------|------------|--------------|
| S18 | 3 | ~34 | S18.3 → S18.2 → S18.1 |
| S19 | 3 | ~32 | S19.1 antes de S19.3 |
| S20 | 4 | ~30 | Independientes |
| S21 | 5 | ~32 | Independientes |
| **Total** | **15** | **~128** | |
