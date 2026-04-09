# S18: Sprint Explotacion — Especificacion BDD

## S18.1: Explotacion de Donk Bets

### Scenario: Raise agresivo vs donk bet con mano fuerte
```
Given hero tiene equity > ValueAbove
  And villain hizo donk bet (no es agresor de calle anterior)
  And heroHandRank >= OnePair
When PostflopDecisionService determina accion
Then retorna "Raise 3.5x (Value)"
  And reason contiene "donk bet"
  And raise frequency es 70% (vs 50% normal)
```

### Scenario: Call amplio vs donk bet con equity marginal
```
Given hero tiene equity entre FoldBelow y ValueAbove
  And villain hizo donk bet
  And heroHandRank >= OnePair
When PostflopDecisionService determina accion
Then retorna "Call"
  And FoldBelow se reduce en DonkBetCallBonus (default -3)
```

### Scenario: Raise pot vs donk bet con nut hand
```
Given hero tiene equity > StrongValueAbove
  And villain hizo donk bet
  And heroHandRank >= TwoPair
When PostflopDecisionService determina accion
Then retorna "Raise Pot (Value)"
  And reason contiene "exploit weakness"
```

### Scenario: Donk bet sin ajuste si villain es conocido como agresivo
```
Given villain tiene DonkBetPct > 20% (stat ampliado S18.3)
  And villain hizo donk bet
When PostflopDecisionService determina accion
Then raise frequency aumenta +20% adicional (villain donkea amplio)
```

---

## S18.2: Barrel Frequency Tracking

### Scenario: Registrar barrel del villain
```
Given villain aposto en flop y en turn (barrel)
When OpponentTracker.TrackBarrel(villainAlias, didBarrel: true)
Then TimesBarreled incrementa en 1
  And TimesBarrelOpportunity incrementa en 1
  And BarrelFrequency se recalcula
```

### Scenario: Registrar oportunidad sin barrel
```
Given villain aposto en flop pero check en turn
When OpponentTracker.TrackBarrel(villainAlias, didBarrel: false)
Then TimesBarreled no cambia
  And TimesBarrelOpportunity incrementa en 1
```

### Scenario: Villain barrelea mas que lo esperado para su tipo
```
Given villain tipo TAG (expectedBarrelFreq = 30%)
  And villain tiene BarrelFrequency = 45% (> 30% x 1.2 = 36%)
  And HasReliableBarrelData = true (>= 8 muestras)
When PostflopDecisionService enfrenta barrel en turn/river
Then FoldBelow se ajusta +3 (rango mas fuerte de lo esperado)
```

### Scenario: Villain barrelea menos que lo esperado
```
Given villain tipo LAG (expectedBarrelFreq = 60%)
  And villain tiene BarrelFrequency = 35% (< 60% x 0.8 = 48%)
  And HasReliableBarrelData = true
When PostflopDecisionService enfrenta barrel en turn/river
Then FoldBelow se ajusta -2 (probablemente bluffeando mas)
```

### Scenario: Datos insuficientes de barrel no generan ajuste
```
Given villain tiene BarrelFrequency = 80%
  And HasReliableBarrelData = false (< 8 muestras)
When PostflopDecisionService enfrenta barrel
Then FoldBelow no se ajusta por barrel frequency
```

---

## S18.3: Stats de Villain Ampliados

### WTSD%

#### Scenario: Tracking went to showdown
```
Given villain llega a showdown en river
When OpponentTracker.TrackShowdownResult(alias, wentToSD: true, wonSD: true)
Then TimesWentToShowdown incrementa en 1
  And TimesWonAtShowdown incrementa en 1
```

#### Scenario: WTSD alto reduce bluff frequency
```
Given villain tiene WTSDPct > 50% (calling station)
  And HasReliableWTSDData = true (>= 15 muestras)
When PostflopDecisionService calcula bluff decision
Then BluffFrequency se multiplica x 0.6
  And ValueBetThreshold se reduce -3 (value bet mas amplio)
```

#### Scenario: WTSD bajo aumenta bluff frequency
```
Given villain tiene WTSDPct < 25% (fold happy)
When PostflopDecisionService calcula bluff decision
Then BluffFrequency se multiplica x 1.4
```

### W$SD%

#### Scenario: W$SD alto indica rango fuerte en showdown
```
Given villain tiene WSDPct > 60%
When PostflopDecisionService enfrenta bet en river
Then FoldBelow se ajusta +2 (respetar mas)
```

### CheckRaise%

#### Scenario: Tracking check-raise
```
Given villain hace check-raise en flop
When OpponentTracker.TrackCheckRaise(alias, didCR: true, hadOpportunity: true)
Then TimesCheckRaised incrementa en 1
  And TimesCheckRaiseOpportunity incrementa en 1
```

#### Scenario: CheckRaise alto reduce c-bet frequency
```
Given villain tiene CheckRaisePct > 15%
When PostflopDecisionService calcula c-bet
Then CbetFrequency se multiplica x 0.7
```

### DonkBet%

#### Scenario: Tracking donk bet
```
Given villain hace donk bet sin ser agresor
When OpponentTracker.TrackDonkBet(alias, didDonk: true, hadOpportunity: true)
Then TimesDonkBet incrementa en 1
```

#### Scenario: DonkBet% alto amplifica explotacion S18.1
```
Given villain tiene DonkBetPct > 20%
  And villain hizo donk bet
When PostflopDecisionService determina accion
Then raise frequency se amplifica +20% sobre base de S18.1
```

---

## Parametros Nuevos en StrategyProfile

```
DonkBetRaiseFrequency: 0.70          // Base raise freq vs donk
DonkBetCallBonus: 3.0                // Reduccion FoldBelow vs donk
DonkBetRaiseSizing: 3.5              // Multiplicador de sizing
BarrelFrequencyOverThreshold: 1.2    // Ratio para considerar "over-barreling"
BarrelFrequencyUnderThreshold: 0.8   // Ratio para considerar "under-barreling"
BarrelOverAdjustment: 3.0            // FoldBelow bonus cuando over-barrel
BarrelUnderAdjustment: -2.0          // FoldBelow bonus cuando under-barrel
MinBarrelSamples: 8                  // Muestras minimas para barrel data
WTSDBluffMultiplierHigh: 0.6         // Multiplicador bluff si WTSD > 50%
WTSDBluffMultiplierLow: 1.4          // Multiplicador bluff si WTSD < 25%
WTSDValueBetBonus: -3.0              // Reduccion threshold si calling station
WSDFoldBelowAdjust: 2.0              // FoldBelow bonus si W$SD > 60%
CheckRaiseCbetMultiplier: 0.7        // C-bet freq multiplier si CR% > 15%
MinWTSDSamples: 15
MinCheckRaiseSamples: 10
```

## Dependencias

- S18.3 es prerequisito de S18.1 (usa DonkBetPct) y S18.2 (usa BarrelFrequency)
- Orden de implementacion: S18.3 → S18.2 → S18.1

## Tests Estimados

- S18.1: 8 tests (raise/call/fold vs donk, sizing, con stats)
- S18.2: 10 tests (track barrel, reliable data, adjustment por tipo)
- S18.3: 16 tests (WTSD/W$SD/CR%/DonkBet% tracking + adjustments)
- **Total: ~34 tests**
