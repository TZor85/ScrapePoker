# Spec: DonkBet requiere agresor real

## Descripcion

`GameCoordinator.DetectDonkBet` detecta DonkBet cuando `!villainWasPreflopAggressor`, pero no verifica que hero sea el agresor. Ademas, `villainWasPreflopAggressor` incluye a hero (P0) en el check, causando falsos negativos cuando hero raiseo.

## Ubicacion

`src/OpenScrape.App/Services/GameCoordinator.cs`, lineas 183-193

## Codigo Actual

```csharp
public (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(
    PlayerGameState state, decimal maxBet, bool isHeroInPosition, HandSituation currentSituation)
{
    bool villainWasPreflopAggressor = state.Players
        .Any(p => p.Active && p.WasPreflopAggressor);  // BUG: incluye hero (P0)

    bool heroWasPreviousStreetAggressor =
        (_gameLoopStateMachine.IsTurn && PostflopContext.HeroBetFlop) ||
        (_gameLoopStateMachine.IsRiver && PostflopContext.HeroBetTurn);

    bool effectiveVillainAggressor = villainWasPreflopAggressor && !heroWasPreviousStreetAggressor;
    return PreflopAnalyzer.DetectDonkBet(maxBet, effectiveVillainAggressor, currentSituation);
}
```

## Problemas

1. **Pot limpeado:** Nadie tiene WasPreflopAggressor. `!villainWasPreflopAggressor = true` → DonkBet detectado sin agresor.
2. **Hero raiseo:** Hero (ValuePosition=0) esta en Players. Si hero raiseo, hero.WasPreflopAggressor=true → `villainWasPreflopAggressor=true` → DonkBet NO detectado.

## Fix Propuesto

```csharp
public (bool IsDonkBet, HandSituation DonkBetSituation) DetectDonkBet(
    PlayerGameState state, decimal maxBet, bool isHeroInPosition, HandSituation currentSituation)
{
    // Excluir hero (P0) del check de agresor villain
    bool villainWasPreflopAggressor = state.Players
        .Any(p => p.Active && p.WasPreflopAggressor && p.ValuePosition != 0);

    bool heroWasPreviousStreetAggressor =
        (_gameLoopStateMachine.IsTurn && PostflopContext.HeroBetFlop) ||
        (_gameLoopStateMachine.IsRiver && PostflopContext.HeroBetTurn);

    // Hero es agresor si: raiseo preflop (WasPreflopAggressor en P0) O aposto calle anterior
    var heroPlayer = state.Players.FirstOrDefault(p => p.ValuePosition == 0);
    bool heroWasPreflopAggressor = heroPlayer?.WasPreflopAggressor == true;
    bool heroIsAggressor = heroWasPreflopAggressor || heroWasPreviousStreetAggressor;

    // DonkBet requiere que hero sea el agresor y villain no lo sea
    if (!heroIsAggressor)
        return (false, currentSituation);

    bool effectiveVillainAggressor = villainWasPreflopAggressor && !heroWasPreviousStreetAggressor;
    return PreflopAnalyzer.DetectDonkBet(maxBet, effectiveVillainAggressor, currentSituation);
}
```

## Escenarios BDD

### Escenario 1: Pot limpeado — NO donk bet
```
Dado pot limpeado (nadie raiseo, todos WasPreflopAggressor=false)
Y villain apuesta 7BB en turn
Cuando se evalua DetectDonkBet
Entonces heroIsAggressor = false
Y retorna (false, currentSituation)
Y la decision usa thresholds de RaiseOverLimper (no DonkBet)
```

### Escenario 2: Hero raiseo preflop, villain betea — ES donk bet
```
Dado hero raiseo preflop (hero.WasPreflopAggressor = true)
Y villain NO raiseo (villain.WasPreflopAggressor = false)
Y villain apuesta en flop
Cuando se evalua DetectDonkBet
Entonces heroIsAggressor = true
Y villainWasPreflopAggressor = false (excluye hero P0)
Y retorna (true, DonkBet o DonkBetVsOpenRaise)
```

### Escenario 3: Villain raiseo preflop — NO donk bet
```
Dado villain raiseo preflop (villain.WasPreflopAggressor = true)
Y villain apuesta en flop (c-bet del villain)
Cuando se evalua DetectDonkBet
Entonces villainWasPreflopAggressor = true
Y effectiveVillainAggressor = true
Y PreflopAnalyzer retorna (false, currentSituation)
```

### Escenario 4: Hero aposto flop, villain betea turn — ES donk bet (cross-street)
```
Dado pot limpeado (nadie raiseo preflop)
Pero hero aposto en flop (PostflopContext.HeroBetFlop = true)
Y villain apuesta en turn
Cuando se evalua DetectDonkBet
Entonces heroWasPreviousStreetAggressor = true
Y heroIsAggressor = true
Y retorna (true, DonkBet)
```

### Escenario 5: Sin bet — NO donk bet
```
Dado cualquier situacion
Y maxBet = 0 (nadie aposto)
Cuando se evalua DetectDonkBet
Entonces PreflopAnalyzer retorna (false, currentSituation)
```

## Tests Requeridos

1. **Test_DetectDonkBet_PotLimpeado_NoDonkBet** — nadie raiseo → false
2. **Test_DetectDonkBet_HeroRaiseo_VillainBetea_EsDonkBet** — hero aggressor + villain bet → true
3. **Test_DetectDonkBet_VillainRaiseo_NoDonkBet** — villain aggressor → false
4. **Test_DetectDonkBet_HeroBetFlop_VillainBeteaTurn_EsDonkBet** — cross-street → true
5. **Test_DetectDonkBet_SinBet_NoDonkBet** — maxBet=0 → false
6. **Test_DetectDonkBet_HeroP0ExcluidoDeVillainCheck** — hero con WasPreflopAggressor no cuenta como villain
