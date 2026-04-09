# Spec BF3: C-Bet — Rango Desbalanceado del Agresor

## Descripción

La lógica de continuation bet (c-bet) en `PostflopDecisionService` aplica control de frecuencia **solo al rango de air** (equity < FoldBelow), mientras que el rango de valor (equity >= FoldBelow) **siempre apuesta** a través de `HandleNoBet`. Esto crea una estrategia desbalanceada:

- Cuando hero (agresor preflop) chequea → villain sabe que hero tiene air (equity < FoldBelow - 15 o falló el random) o mano premium en slowplay
- Cuando hero apuesta → es siempre valor (HandleNoBet) o bluff controlado (c-bet path)
- No hay mixing en el rango medio: manos con equity en [FoldBelow, ValueAbove) SIEMPRE apuestan

En GTO, el agresor debería chequear parte de su rango de valor para proteger su checking range y crear indifference en el villain.

## Ubicación

`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`, líneas 373-386 (c-bet) y 424-430 (HandleNoBet entry)

## Flujo Actual

```
DetermineAction:
  1. Si equity < FoldBelow Y equity > FoldBelow-15 Y agresor:
     → C-bet con frecuencia (65%/45%/30%) → Bet o fall-through
  2. Si equity < FoldBelow:
     → HandleLowEquity (bluff/semi-bluff/fold)
  3. Si !isFacingBet:
     → HandleNoBet (siempre evalúa bet/check basado en equity tiers)
```

**Problema:** Paso 3 no tiene frecuencia de c-bet. Hero con equity en [FoldBelow, ThinValueAbove) en board coordinated como agresor debería a veces chequear para proteger su range, pero siempre apuesta.

## Flujo Propuesto

```
DetermineAction:
  1. Si equity < FoldBelow:
     a. Si agresor Y equity > FoldBelow-15:
        → C-bet con frecuencia (65%/45%/30%) → Bet
     b. Sino → HandleLowEquity
  2. Si !isFacingBet Y agresor Y !isMultiway:
     → HandleNoBet CON frecuencia de c-bet aplicada al rango medio
       (equity en [FoldBelow, ThinValueAbove) puede chequear a frecuencia 1-cbetFreq)
  3. Si !isFacingBet:
     → HandleNoBet normal
```

**Clave:** El rango medio del agresor (entre FoldBelow y ThinValueAbove) ahora tiene una probabilidad de check en vez de apostar siempre. Esto:
- Protege el checking range del hero
- Hace que el c-bet del hero no sea siempre "valor o bluff puro"
- Crea indifference: villain no sabe si check = debilidad o trapping

## Escenarios BDD

### Escenario 1: Agresor con equity media en flop — a veces chequea
```
Dado hero como agresor preflop, !isFacingBet, !isMultiway
Y effectiveEquity = 45% (entre FoldBelow y ThinValueAbove)
Y street = Flop
Y cbetFrequency = 65%
Cuando se decide acción 1000 veces
Entonces ~650 veces retorna Bet (value/thin value via HandleNoBet)
Y ~350 veces retorna Check (protección de range)
```

### Escenario 2: Agresor con equity alta — siempre apuesta
```
Dado hero como agresor preflop, !isFacingBet
Y effectiveEquity = 75% (> ValueAbove)
Cuando se decide acción
Entonces SIEMPRE retorna Bet (equity fuerte, sin mixing)
```

### Escenario 3: Agresor con equity baja — c-bet bluff con frecuencia (sin cambio)
```
Dado hero como agresor preflop, !isFacingBet
Y effectiveEquity = 25% (< FoldBelow, > FoldBelow-15)
Y street = Flop, cbetFrequency = 65%
Cuando se decide acción
Entonces 65% de las veces retorna C-Bet
Y 35% de las veces va a HandleLowEquity (bluff/check/fold)
```

### Escenario 4: No agresor — sin mixing de c-bet
```
Dado hero NO es agresor preflop, !isFacingBet
Y effectiveEquity = 45%
Cuando se decide acción
Entonces HandleNoBet decide normalmente (sin frecuencia de c-bet)
```

### Escenario 5: Multiway — sin mixing de c-bet
```
Dado hero como agresor preflop, !isFacingBet, isMultiway = true
Y effectiveEquity = 45%
Cuando se decide acción
Entonces HandleNoBet decide normalmente (multiway = sin mixing)
```

### Escenario 6: Turn/River — frecuencia menor
```
Dado hero como agresor preflop con barrel previo
Y effectiveEquity = 45% (rango medio)
Y street = Turn, cbetFrequency = 45%
Cuando se decide acción 1000 veces
Entonces ~450 veces retorna Bet
Y ~550 veces retorna Check
```

### Escenario 7: Board seco con valor fuerte — check-back para trap
```
Dado hero como agresor preflop
Y effectiveEquity = 50% (rango medio), boardTexture = "Dry"
Y heroHandRank >= ThreeOfAKind
Cuando se decide acción
Entonces la lógica de slow play tiene prioridad sobre c-bet mixing
Y retorna Check (slow play)
```

### Escenario 8: El mixing no afecta manos en zona de thin value
```
Dado hero como agresor preflop
Y effectiveEquity = 58% (> ThinValueAbove pero < ValueAbove)
Y isInPosition = true
Cuando se decide acción
Entonces SIEMPRE retorna Bet (thin value IP, sin mixing)
Nota: El mixing solo aplica al rango ENTRE FoldBelow y ThinValueAbove
```

## Implementación Propuesta

En `DetermineAction`, antes de entrar a `HandleNoBet`, añadir check de mixing para el agresor:

```csharp
// --- NO FACING BET ---
// C-bet mixing: agresor con equity media puede chequear para proteger range
if (!isFacingBet && heroIsAggressor && !isMultiway &&
    effectiveEquity >= adjustedFoldBelow && effectiveEquity < adjustedThinValueAbove)
{
    double cbetFreq = GetCbetFrequency(street);
    if (cbetFreq > 0 && Random.Shared.NextDouble() >= cbetFreq)
    {
        // Nota: >= (complemento) porque estamos decidiendo CHECK, no BET
        return new PostflopDecisionResult("Check",
            $"Check — protección de range como agresor ({1 - cbetFreq:P0} check freq)");
    }
}

return HandleNoBet(...);
```

**Nota:** Este check va DESPUÉS de check-raise/slow play/float exit (que tienen prioridad propia) pero ANTES de HandleNoBet. Dentro de HandleNoBet, la lógica de board texture y equity tiers sigue funcionando normalmente para las manos que pasan el filtro.

## Consideraciones

1. **No afectar slow play/check-raise:** Estas decisiones tienen prioridad y ya incluyen su propia lógica de frecuencia
2. **No afectar double barrel:** El barrel path (previousStreetBet) ya tiene su propio control en HandleNoBet
3. **No afectar push/fold:** SPR corto tiene prioridad antes de este punto
4. **Solo rango medio:** Equity alta (>= ThinValueAbove) siempre apuesta por valor
5. **Solo heads-up:** Multiway no tiene mixing (rango más tight)

## Tests Requeridos

1. **Test_CbetMixing_AgresorEquityMedia_CheckAFrecuencia** — verificar que ~35% check en flop (seed fijo)
2. **Test_CbetMixing_NoAgresor_SinMixing** — no agresor → HandleNoBet siempre
3. **Test_CbetMixing_Multiway_SinMixing** — multiway → HandleNoBet siempre
4. **Test_CbetMixing_EquityAlta_SinMixing** — equity > ThinValueAbove → siempre bet
5. **Test_CbetMixing_TurnFrecuenciaMenor** — turn 45% c-bet → 55% check
6. **Test_CbetMixing_RiverFrecuenciaMenor** — river 30% c-bet → 70% check
7. **Test_CbetMixing_SlowPlayTienePrioridad** — ThreeOfAKind + Dry → slow play (no mixing)
8. **Test_CbetMixing_DistribucionEstadistica** — 1000 runs, chi-squared test vs frecuencia esperada
