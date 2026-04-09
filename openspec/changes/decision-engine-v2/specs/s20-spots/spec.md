# S20: Sprint Spots Especificos — Especificacion BDD

## S20.1: Blind vs Blind Thresholds Dinamicos

### Scenario: SB vs BB — defender mas amplio
```
Given hero esta en SmallBlind
  And villain esta en BigBlind
  And situacion es OpenRaise o OpenRaiseVs3Bet
When PostflopDecisionService obtiene thresholds ajustados
Then FoldBelow se reduce en 3.0
  And ThinValueAbove se reduce en 2.0
```

### Scenario: BB vs SB raise — defensa mas amplia (ciega ya puesta)
```
Given hero esta en BigBlind
  And villain esta en SmallBlind
  And situacion es OpenRaise
When PostflopDecisionService obtiene thresholds ajustados
Then FoldBelow se reduce en 5.0
  And ThinValueAbove se reduce en 3.0
```

### Scenario: BB vs BTN — ajuste menor
```
Given hero esta en BigBlind
  And villain esta en Button
When PostflopDecisionService obtiene thresholds ajustados
Then FoldBelow se reduce en 1.0
  And ThinValueAbove no se modifica
```

### Scenario: Posiciones no-blind — sin ajuste
```
Given hero esta en CutOff
  And villain esta en Button
When PostflopDecisionService obtiene thresholds ajustados
Then FoldBelow no se modifica
  And ThinValueAbove no se modifica
```

---

## S20.2: Deteccion de Limp-Raise

### Scenario: Limper que 3betea se detecta como LimpRaise
```
Given jugador limpeó preflop (call BB)
  And hero raiseó sobre el limper
  And limper re-raiseó (3bet)
When PreflopAnalyzer.DetectSituation()
Then retorna HandSituation.LimpRaise
```

### Scenario: VillainRange para LimpRaise es super-premium
```
Given situacion es HandSituation.LimpRaise
When VillainRange.GetForSituation(LimpRaise)
Then rango es ~3%: AA(100%), KK(100%), QQ(70%), AKs(100%), AKo(50%)
  And rango es significativamente mas estrecho que ThreeBet normal (~8%)
```

### Scenario: Thresholds postflop vs LimpRaise son muy tight
```
Given situacion postflop viene de LimpRaise
When PostflopDecisionService obtiene thresholds
Then FoldBelow se ajusta +8 (similar a FourBet)
  And ThinValueAbove se ajusta +5
```

### Scenario: Hero con mano no-premium foldea ante limp-raise
```
Given hero tiene QJs
  And enfrenta limp-raise (rango ~3%)
When PreflopAnalyzer calcula equity vs rango
Then equity < 30%
  And recomendacion es Fold
```

### Scenario: Hero con premium defiende ante limp-raise
```
Given hero tiene KK
  And enfrenta limp-raise
When PreflopAnalyzer calcula equity vs rango
Then equity > 50%
  And recomendacion es 4Bet o Call
```

---

## S20.3: Reverse Implied Odds en River por Bluffs Futuros

### Scenario: Turn con TPWK en board con draws — penalty adicional
```
Given street es Turn
  And heroHandRank == OnePair
  And board tiene flush draw o straight draw
  And villain no esta all-in
When CalculateReverseImpliedOdds()
Then reverseImpliedPenalty incluye componente de bluff risk
  And bluff risk se calcula como: 0.3 x drawMissFrequency x (potSize / (potSize + heroStack))
```

### Scenario: Villain LAG amplifica bluff risk penalty
```
Given condiciones de S20.3 base
  And villainType es LAG
When CalculateReverseImpliedOdds()
Then bluffRiskPenalty se multiplica x 1.5
```

### Scenario: Villain TP reduce bluff risk penalty
```
Given condiciones de S20.3 base
  And villainType es TightPassive
When CalculateReverseImpliedOdds()
Then bluffRiskPenalty se multiplica x 0.5
```

### Scenario: Villain all-in no tiene bluff risk
```
Given condiciones de S20.3 base
  And IsAnyoneAllIn = true
When CalculateReverseImpliedOdds()
Then bluffRiskPenalty es 0 (no puede bluffear mas)
```

### Scenario: Hero con TwoPair+ no recibe bluff risk penalty
```
Given street es Turn
  And heroHandRank >= TwoPair
  And board tiene draws
When CalculateReverseImpliedOdds()
Then bluffRiskPenalty es 0 (mano fuerte para showdown)
```

---

## S20.4: Squeeze Defense

### Scenario: Premium hands siempre 4bet ante squeeze
```
Given hero enfrenta squeeze (3bet en pot con limper)
  And hero tiene AA, KK, o AKs
When PreflopAnalyzer.HandleSqueezeDecision()
Then recomendacion es 4Bet
```

### Scenario: QQ/AQs 4bet solo con SPR suficiente
```
Given hero enfrenta squeeze
  And hero tiene QQ o AQs
  And SPR > 2.5
When PreflopAnalyzer.HandleSqueezeDecision()
Then recomendacion es 4Bet
```

### Scenario: QQ con SPR bajo ante squeeze — call
```
Given hero enfrenta squeeze
  And hero tiene QQ
  And SPR <= 2.5
When PreflopAnalyzer.HandleSqueezeDecision()
Then recomendacion es Call (no 4bet con SPR bajo)
```

### Scenario: Mano con blocker IP — call ante squeeze
```
Given hero enfrenta squeeze
  And hero esta IP
  And hero tiene AJs (blocker con equity 4-8%)
  And SPR > 3.0
When PreflopAnalyzer.HandleSqueezeDecision()
Then recomendacion es Call
```

### Scenario: Mano debil OOP sin blocker — fold ante squeeze
```
Given hero enfrenta squeeze
  And hero esta OOP
  And hero tiene 87s (equity < 4% vs squeeze range)
  And no tiene blocker (no A/K)
When PreflopAnalyzer.HandleSqueezeDecision()
Then recomendacion es Fold
```

### Scenario: Thresholds postflop en squeeze pot mas estrictos que 3bet
```
Given mano paso por squeeze preflop
When PostflopDecisionService obtiene thresholds
Then FoldBelow se ajusta +6 (vs +5 en 3bet normal)
  And ThinValueAbove se ajusta +4 (vs +3 en 3bet normal)
```

---

## Parametros Nuevos en StrategyProfile

```
// S20.1 — Blind vs Blind
BvBSBvsBBFoldBelowAdj: -3.0
BvBSBvsBBThinValueAdj: -2.0
BvBBBvsSBFoldBelowAdj: -5.0
BvBBBvsSBThinValueAdj: -3.0
BvBBBvsBTNFoldBelowAdj: -1.0

// S20.2 — Limp-Raise
LimpRaiseFoldBelowAdj: 8.0          // Similar a 4bet
LimpRaiseThinValueAdj: 5.0
LimpRaiseRangeWidth: 3.0            // % de manos

// S20.3 — Reverse Implied (Bluff Risk)
BluffRiskBaseFactor: 0.30
BluffRiskLAGMultiplier: 1.5
BluffRiskTPMultiplier: 0.5
BluffRiskMinHandRank: TwoPair       // Por debajo aplica penalty

// S20.4 — Squeeze Defense
SqueezeFoldBelowAdj: 6.0
SqueezeThinValueAdj: 4.0
Squeeze4BetMinSPR: 2.5
SqueezeCallMinEquity: 0.04
SqueezeCallRequiresBlocker: true
Squeeze4BetEquityThreshold: 0.08
```

## Tests Estimados

- S20.1: 6 tests (SBvsBB, BBvsSB, BBvsBTN, sin ajuste, combinado con 3bet)
- S20.2: 8 tests (deteccion, rango, thresholds, fold/4bet scenarios)
- S20.3: 8 tests (penalty base, por villain type, all-in, TwoPair+ skip)
- S20.4: 8 tests (premium 4bet, SPR, blocker call, fold, thresholds)
- **Total: ~30 tests**
