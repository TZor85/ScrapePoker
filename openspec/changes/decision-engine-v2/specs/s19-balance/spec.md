# S19: Sprint Balance — Especificacion BDD

## S19.1: Check-Raise Mixing (Probabilistico)

### Scenario: OOP con TwoPair+ hace check-raise 40% del tiempo
```
Given hero esta OOP
  And heroHandRank >= TwoPair
  And thresholds.CanCheckRaise = true
  And equity > CheckRaiseThreshold
  And CheckRaiseMixingEnabled = true
When PostflopDecisionService determina accion (1000 iteraciones)
Then ~40% de las veces retorna "Check-Raise"
  And ~60% de las veces retorna "Call"
  And nunca retorna "Fold"
```

### Scenario: OOP con TopPair + FlushDraw hace check-raise 35%
```
Given hero esta OOP
  And heroHandRank == OnePair con pairClassification >= TopPair
  And hasFlushDraw = true (9+ outs)
  And equity > CheckRaiseDrawMinEquity
When PostflopDecisionService determina accion (1000 iteraciones)
Then ~35% retorna "Check-Raise"
  And ~65% retorna "Call"
```

### Scenario: OOP con OESD puro hace check-raise 30%
```
Given hero esta OOP
  And heroHandRank < OnePair
  And totalOuts >= 8 (OESD)
  And equity > CheckRaiseDrawMinEquity
When PostflopDecisionService determina accion (1000 iteraciones)
Then ~30% retorna "Check-Raise"
  And ~70% retorna "Call" o "Fold" segun pot odds
```

### Scenario: IP con TwoPair+ hace check-raise 20% (trap)
```
Given hero esta IP
  And heroHandRank >= TwoPair
  And board no es Wet ni Monotone
  And thresholds.CanCheckRaise = true
When PostflopDecisionService determina accion (1000 iteraciones)
Then ~20% retorna "Check-Raise"
  And ~80% retorna "Call" (slow play)
```

### Scenario: CheckRaiseMixingEnabled = false mantiene comportamiento actual
```
Given CheckRaiseMixingEnabled = false
  And equity > CheckRaiseThreshold
When PostflopDecisionService determina accion
Then retorna "Check-Raise" 100% del tiempo (deterministic)
```

### Scenario: SPR guard sigue activo con mixing
```
Given hero OOP con TwoPair
  And SPR < 1.5 y equity < 60%
  And CheckRaiseMixingEnabled = true
When PostflopDecisionService determina accion
Then no hace check-raise (SPR guard prevalece sobre mixing)
```

---

## S19.2: C-Bet Turn Ajustada por Textura del Runout

### Scenario: Turn completa flush — c-bet frequency baja drasticamente
```
Given hero es agresor flop
  And turn completa flush (3+ suited + 4th suit card)
  And baseCbetFrequency = 45%
When PostflopDecisionService calcula c-bet turn
Then cbetFrequency = 45% x CbetTurnFlushCompletedMultiplier (default 0.30)
  And resultado ~13.5%
```

### Scenario: Turn parea board — c-bet frequency baja
```
Given hero es agresor flop
  And turn parea el board
  And baseCbetFrequency = 45%
When PostflopDecisionService calcula c-bet turn
Then cbetFrequency = 45% x CbetTurnPairedMultiplier (default 0.60)
  And resultado ~27%
```

### Scenario: Turn completa straight — c-bet frequency muy baja
```
Given hero es agresor flop
  And turn completa posible straight (4 conectadas)
  And baseCbetFrequency = 45%
When PostflopDecisionService calcula c-bet turn
Then cbetFrequency = 45% x CbetTurnStraightCompletedMultiplier (default 0.40)
  And resultado ~18%
```

### Scenario: Turn flush draw aparece — c-bet frequency media-baja
```
Given hero es agresor flop
  And turn crea flush draw (3 suited donde antes habia 2)
  And baseCbetFrequency = 45%
When PostflopDecisionService calcula c-bet turn
Then cbetFrequency = 45% x CbetTurnFlushDrawMultiplier (default 0.50)
  And resultado ~22.5%
```

### Scenario: Turn brick bajo — c-bet frequency sube
```
Given hero es agresor flop
  And turn es brick (carta baja desconectada, no cambia textura)
  And baseCbetFrequency = 45%
When PostflopDecisionService calcula c-bet turn
Then cbetFrequency = 45% x CbetTurnBrickMultiplier (default 1.10)
  And resultado ~49.5%
```

### Scenario: Multiples cambios se acumulan multiplicativamente
```
Given hero es agresor flop
  And turn parea board Y crea flush draw
  And baseCbetFrequency = 45%
When PostflopDecisionService calcula c-bet turn
Then cbetFrequency = 45% x 0.60 x 0.50 = ~13.5%
```

### Scenario: C-bet flop y river no se afectan
```
Given hero es agresor
  And street es Flop (no Turn)
When PostflopDecisionService calcula c-bet
Then baseCbetFrequency se usa sin multiplicadores de textura turn
```

---

## S19.3: Defensa en 3Bet Pots Postflop

### Scenario: OOP flop con TwoPair+ en 3bet pot — check-raise alto
```
Given HandSituation es ThreeBet o OpenRaiseVs3Bet
  And hero esta OOP
  And street es Flop
  And heroHandRank >= TwoPair
When PostflopDecisionService determina accion
Then check-raise frequency es 50%
  And call frequency es 50%
  And fold frequency es 0%
```

### Scenario: OOP flop con combo draw en 3bet pot — check-raise semi-bluff
```
Given situacion 3bet pot
  And hero esta OOP
  And street es Flop
  And hasComboDrawOrFlushDraw = true (9+ outs)
When PostflopDecisionService determina accion
Then check-raise frequency es 35%
  And fold frequency es 65% (no flotar OOP en 3bet)
```

### Scenario: OOP flop con equity baja en 3bet pot — fold sin float
```
Given situacion 3bet pot
  And hero esta OOP
  And equity < FoldBelow
  And no tiene draw significativo
When PostflopDecisionService determina accion
Then retorna "Fold"
  And no intenta float (a diferencia de single raised pot)
```

### Scenario: Turn — agresor check muestra debilidad en 3bet pot
```
Given situacion 3bet pot
  And hero esta OOP
  And street es Turn
  And villain (agresor) hizo check
  And equity > 45%
When PostflopDecisionService determina accion
Then probe bet frequency es 40%
  And check frequency es 60%
```

### Scenario: Turn — agresor barrelea en 3bet pot
```
Given situacion 3bet pot
  And hero esta OOP
  And street es Turn
  And villain barrelea (segunda apuesta)
  And heroHandRank >= TwoPair
When PostflopDecisionService determina accion
Then check-raise frequency es 20% (anti-barrel)
  And call frequency es 80%
```

### Scenario: IP caller en 3bet pot — flat call mas frecuente
```
Given situacion 3bet pot
  And hero esta IP (caller preflop)
  And street es Flop
  And heroHandRank == OnePair (TPTK)
When PostflopDecisionService determina accion
Then call frequency es 85%
  And raise frequency es 15%
  And fold frequency es 0%
```

---

## Parametros Nuevos en StrategyProfile

```
// S19.1 — Check-Raise Mixing
CheckRaiseMixingEnabled: true
CRMixFreqOOPStrong: 0.40           // TwoPair+ OOP
CRMixFreqOOPTopPairDraw: 0.35      // TP + FlushDraw OOP
CRMixFreqOOPDraw: 0.30             // OESD OOP
CRMixFreqIPTrap: 0.20              // TwoPair+ IP trap

// S19.2 — C-Bet Turn Texture
CbetTurnFlushCompletedMultiplier: 0.30
CbetTurnFlushDrawMultiplier: 0.50
CbetTurnPairedMultiplier: 0.60
CbetTurnStraightCompletedMultiplier: 0.40
CbetTurnBrickMultiplier: 1.10

// S19.3 — 3Bet Pot Defense
ThreeBetPotCRFreqStrong: 0.50      // OOP TwoPair+ flop
ThreeBetPotCRFreqDraw: 0.35        // OOP combo draw flop
ThreeBetPotProbeFreq: 0.40         // Turn probe cuando agresor check
ThreeBetPotAntiBarrelCR: 0.20      // Turn CR vs barrel
ThreeBetPotIPCallFreq: 0.85        // IP caller flop
ThreeBetPotNoFloat: true            // No floatar OOP en 3bet pot
```

## Tests Estimados

- S19.1: 10 tests (mixing por hand/position, SPR guard, disabled mode)
- S19.2: 10 tests (cada textura turn, acumulacion, solo turn)
- S19.3: 12 tests (OOP/IP, flop/turn, CR/probe/fold, 3bet especificos)
- **Total: ~32 tests**
