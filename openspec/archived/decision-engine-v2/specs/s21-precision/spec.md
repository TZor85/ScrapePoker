# S21: Sprint Precision — Especificacion BDD

## S21.1: Outs de Overcards Ajustados por Board Texture

### Scenario: Overcard outs en board seco — 3 outs standard
```
Given board es K-7-2 rainbow (seco)
  And hero tiene A-Q (dos overcards al 7 y 2, una bloqueada por K)
When OutsCalculator calcula overcard outs para Q
Then overcardOuts = 3
```

### Scenario: Overcard outs en board conectado — reducidos a 2
```
Given board es K-9-8 (conectado)
  And hero tiene A-Q
  And Q completa potencial straight para villain (QJT, QJ9)
When OutsCalculator calcula overcard outs para Q
Then overcardOuts = 2 (tainted por straight completions)
```

### Scenario: Overcard outs en board paired — reducidos a 2
```
Given board es K-9-K
  And hero tiene A-Q
When OutsCalculator calcula overcard outs
Then overcardOuts = 2 (reducido por trips posibles del villain)
```

### Scenario: Overcard con blocker — boost
```
Given board es K-7-2
  And hero tiene A-K (heroBlocksTopCard)
  And overcard es A
When OutsCalculator calcula overcard outs para A
Then overcardOuts = 3 x 1.2 = 3.6 (redondeado a 3 o 4)
```

---

## S21.2: Multiway Penalty por Posicion Exacta

### Scenario: SB en multiway recibe penalty mayor
```
Given hero esta en SmallBlind
  And numOpponents = 2 (3way pot)
  And hero esta OOP
When PostflopDecisionService calcula multiway penalty
Then oopMultiplier = 0.70 (vs 0.50 default BB)
  And penalty total es mayor que si hero estuviera en BB
```

### Scenario: BB en multiway — penalty standard
```
Given hero esta en BigBlind
  And numOpponents = 2
When PostflopDecisionService calcula multiway penalty
Then oopMultiplier = 0.50
```

### Scenario: EP en multiway — penalty intermedia
```
Given hero esta en EarlyPosition
  And numOpponents = 2
When PostflopDecisionService calcula multiway penalty
Then oopMultiplier = 0.60
```

### Scenario: Villain IP agresor en multiway — amplifica penalty
```
Given numOpponents >= 2
  And villain esta IP
  And villain es agresor (aposto/raiseó)
When PostflopDecisionService calcula multiway penalty
Then oopMultiplier se amplifica x 1.3
  And penalty refleja que villain IP agresivo es mas peligroso
```

### Scenario: Hero IP en multiway — penalty base sin amplificacion
```
Given hero esta IP (Button)
  And numOpponents = 2
When PostflopDecisionService calcula multiway penalty
Then se usa penalty lineal IP estandar (sin oopMultiplier)
```

---

## S21.3: Board Texture "Broadway Wet"

### Scenario: AKQ se clasifica como Wet (no SemiDry)
```
Given board es A-K-Q
When BoardTextureAnalyzer.AnalyzeTexture()
Then wetnessScore incluye broadwayConnectedBonus (+20)
  And textura final es Wet o SemiWet (no SemiDry)
```

### Scenario: KQJ con 2 suits — Wet con broadway bonus
```
Given board es Kh-Qs-Jh
When BoardTextureAnalyzer.AnalyzeTexture()
Then wetnessScore incluye broadwayConnectedBonus
  And wetnessScore incluye suit bonus
  And textura es Wet
```

### Scenario: A72 rainbow no recibe broadway bonus
```
Given board es A-7-2 rainbow
When BoardTextureAnalyzer.AnalyzeTexture()
Then broadwayCount = 1 (solo A)
  And broadwayConnectedBonus = 0 (necesita >= 2 broadways conectadas)
  And textura sigue siendo Dry
```

### Scenario: KJ5 — parcialmente broadway pero no conectado
```
Given board es K-J-5
When BoardTextureAnalyzer.AnalyzeTexture()
Then broadwayCount = 2 (K, J)
  And son conectados (gap de 1: K-Q-J)
  And broadwayConnectedBonus = 20
  And wetnessScore sube a SemiWet
```

### Scenario: Broadway Wet afecta thresholds postflop
```
Given board tiene broadwayConnectedBonus > 0
When PostflopDecisionService ajusta thresholds
Then FoldBelow se ajusta +3 (villain hits broadway combos)
  And ThinValueAbove se ajusta +2
  And CbetFrequency se multiplica x 0.8 (board favorece caller range)
```

---

## S21.4: Backdoor Draw Overlap Prevention

### Scenario: Flush draw + backdoor straight sin overlap — suma completa
```
Given hero tiene 8h-7h
  And board es Ah-3h-Kd (flush draw hearts)
  And backdoor straight con 9 o 6 (no hearts)
When OutsCalculator calcula outs totales
Then flushDrawOuts = 9
  And backdoorStraightOuts = 1.0
  And totalOuts = 10.0 (sin descuento, no hay overlap)
```

### Scenario: Flush draw + backdoor straight con overlap — descuento
```
Given hero tiene 8h-7h
  And board es Ah-3h-5d (flush draw hearts)
  And backdoor straight necesita 6 o 9
  And 6h y 9h son hearts (overlappean con flush draw)
When OutsCalculator calcula outs totales
Then flushDrawOuts = 9
  And backdoorStraightOuts = 1.0 - (2 x 0.5) = 0.0 (descuento por overlap)
  And totalOuts = 9.0
```

### Scenario: Sin flush draw principal — sin descuento por overlap
```
Given hero tiene 8s-7s
  And board es Ah-3d-5c (no flush draw)
  And tiene backdoor flush (2 spades)
  And tiene backdoor straight
When OutsCalculator calcula outs totales
Then backdoorFlushOuts = 1.5
  And backdoorStraightOuts = 1.0
  And totalOuts suma ambos sin descuento (no hay main draw)
```

---

## S21.5: Randomizacion con Margen Variable

### Scenario: Margen amplio vs LAG
```
Given villainType es LAG
  And equity esta dentro de ±5% del ThinValueAbove
When PostflopDecisionService evalua randomizacion
Then zona de randomizacion es ThinValueAbove ± 5.0
  And decision se mezcla check/bet segun frecuencia adaptativa
```

### Scenario: Margen standard vs TAG
```
Given villainType es TAG
  And equity esta dentro de ±3% del ThinValueAbove
When PostflopDecisionService evalua randomizacion
Then zona de randomizacion es ThinValueAbove ± 3.0
```

### Scenario: Margen estrecho vs TP
```
Given villainType es TightPassive
  And equity esta dentro de ±2% del ThinValueAbove
When PostflopDecisionService evalua randomizacion
Then zona de randomizacion es ThinValueAbove ± 2.0
  And menos variacion porque TP no ajusta (no necesita anti-exploit)
```

### Scenario: Margen default para Unknown
```
Given villainType es Unknown
When PostflopDecisionService calcula margen de randomizacion
Then margen = 3.0 (default conservador)
```

---

## Parametros Nuevos en StrategyProfile

```
// S21.1 — Overcard Outs
OvercardOutsBase: 3
OvercardOutsConnectedBoard: 2
OvercardOutsPairedBoard: 2
OvercardOutsBlockerBoost: 1.2

// S21.2 — Multiway Position
MultiwayOOPMultiplierSB: 0.70
MultiwayOOPMultiplierBB: 0.50
MultiwayOOPMultiplierEP: 0.60
MultiwayIPAggressorAmplifier: 1.3

// S21.3 — Broadway Wet
BroadwayConnectedBonus: 20
BroadwayWetFoldBelowAdj: 3.0
BroadwayWetThinValueAdj: 2.0
BroadwayWetCbetMultiplier: 0.8

// S21.4 — Overlap Prevention
BackdoorOverlapDiscount: 0.5

// S21.5 — Randomization Margin
RandomizationMarginLAG: 5.0
RandomizationMarginTAG: 3.0
RandomizationMarginLP: 4.0
RandomizationMarginTP: 2.0
RandomizationMarginUnknown: 3.0
```

## Tests Estimados

- S21.1: 6 tests (seco, conectado, paired, blocker, edge cases)
- S21.2: 8 tests (SB, BB, EP, IP, villain IP agresor, combinaciones)
- S21.3: 8 tests (AKQ, KQJ, A72, KJ5, impacto en thresholds)
- S21.4: 5 tests (sin overlap, con overlap, sin main draw)
- S21.5: 5 tests (por villain type, unknown default)
- **Total: ~32 tests**
