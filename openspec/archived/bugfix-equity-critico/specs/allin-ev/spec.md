# Spec BF1: Corregir Fórmula CalculateAllinEV

## Descripción

La fórmula de `CalculateAllinEV` en `PostflopDecisionService` sobreestima el Expected Value del all-in por `(equity/100) × heroStack`. Esto provoca que hero haga push con manos marginales donde debería fold o call.

## Ubicación

`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`, líneas 1067-1079

## Fórmula Actual (Incorrecta)

```csharp
double totalPotIfCalled = pot + 2 * stack;
return (equity / 100.0) * totalPotIfCalled - (1.0 - equity / 100.0) * stack;
```

Expandida: `(equity/100) × (pot + 3×stack) - stack`

## Fórmula Correcta

```
EV(all-in) = P(win) × ganancia_neta - P(lose) × pérdida_neta
           = (equity/100) × (pot + stack) - (1 - equity/100) × stack
           = (equity/100) × (pot + 2×stack) - stack
```

Donde:
- `pot` = bote actual (incluye apuestas previas)
- `stack` = stack del hero que arriesga
- `pot + stack` = ganancia neta si gana (bote + call del villain con effective stacks)
- `stack` = pérdida neta si pierde

## Call Sites

Se invoca en dos puntos de `DetermineAction`:
1. Línea ~401: `isFacingBet && isPushFold` — hero decide push vs fold facing bet
2. Línea ~418: `!isFacingBet && isPushFold` — hero decide push vs check sin apuesta

## Escenarios BDD

### Escenario 1: EV negativo con equity baja debe fold
```
Dado que hero tiene equity 30%, stack 80BB, pot 100BB
Cuando se calcula CalculateAllinEV
Entonces EV = 0.30 × (100 + 2×80) - 80 = 0.30 × 260 - 80 = 78 - 80 = -2.0
Y la decisión es Fold (EV < 0)
```

**Nota:** Con la fórmula incorrecta: `0.30 × 260 - 0.70 × 80 = 78 - 56 = +22` → Push INCORRECTO

### Escenario 2: EV positivo con equity alta debe push
```
Dado que hero tiene equity 65%, stack 50BB, pot 150BB
Cuando se calcula CalculateAllinEV
Entonces EV = 0.65 × (150 + 2×50) - 50 = 0.65 × 250 - 50 = 162.5 - 50 = +112.5
Y la decisión es All-In (EV > 0)
```

### Escenario 3: EV marginal cerca de breakeven
```
Dado que hero tiene equity 40%, stack 100BB, pot 200BB
Cuando se calcula CalculateAllinEV
Entonces EV = 0.40 × (200 + 200) - 100 = 160 - 100 = +60
Y la decisión es All-In (EV > 0)
```

**Con fórmula incorrecta:** `0.40 × 400 - 0.60 × 100 = 160 - 60 = +100` → Sobreestima 40BB

### Escenario 4: Breakeven exacto
```
Dado que hero tiene equity E%, stack S, pot P
Cuando EV = 0 (breakeven)
Entonces (E/100) × (P + 2S) = S
Y E_breakeven = 100 × S / (P + 2S)
```

Para stack=100, pot=100: `E_breakeven = 100 × 100 / 300 = 33.33%`

**Con fórmula incorrecta:** `(E/100) × (P + 2S) = (1-E/100) × S` → `E_breakeven = 100 × S / (P + 3S) = 25%` → Hero hace push con 25% cuando necesita 33%

### Escenario 5: Stack = 0 o pot = 0 retorna 0
```
Dado que heroStack = 0 o potSize = 0
Cuando se calcula CalculateAllinEV
Entonces retorna 0
```

### Escenario 6: Equity 0% siempre retorna -stack
```
Dado que hero tiene equity 0%, cualquier stack y pot
Cuando se calcula CalculateAllinEV
Entonces EV = 0 - stack = -stack
```

### Escenario 7: Equity 100% retorna pot + stack
```
Dado que hero tiene equity 100%, stack 100BB, pot 200BB
Cuando se calcula CalculateAllinEV
Entonces EV = 1.0 × (200 + 200) - 100 = 400 - 100 = +300
```

## Fix Propuesto

```csharp
private static double CalculateAllinEV(double equity, decimal heroStack, decimal potSize)
{
    if (heroStack <= 0 || potSize <= 0) return 0;
    double pot = (double)potSize;
    double stack = (double)heroStack;
    double equityFraction = equity / 100.0;
    // EV = P(win) × ganancia_neta - P(lose) × pérdida
    // ganancia_neta = pot + stack (effective stacks: villain iguala hero)
    // pérdida = stack
    return equityFraction * (pot + stack) - (1.0 - equityFraction) * stack;
}
```

## Tests Requeridos

1. **Test_CalculateAllinEV_EquityBaja_RetornaNegativo** — equity 30%, stack 80, pot 100 → EV ≈ -2.0
2. **Test_CalculateAllinEV_EquityAlta_RetornaPositivo** — equity 65%, stack 50, pot 150 → EV ≈ +112.5
3. **Test_CalculateAllinEV_Breakeven_RetornaCero** — equity 33.33%, stack 100, pot 100 → EV ≈ 0
4. **Test_CalculateAllinEV_StackCero_RetornaCero** — edge case
5. **Test_CalculateAllinEV_Equity100_RetornaPotMasStack** — equity 100% → pot + stack
6. **Test_CalculateAllinEV_Equity0_RetornaMenosStack** — equity 0% → -stack
7. **Test_DetermineAction_PushFold_FacingBet_EquityInsuficiente_Fold** — integración: equity baja + SPR corto → Fold (antes era Push)
8. **Test_DetermineAction_PushFold_NoBet_EquityInsuficiente_Check** — integración: equity baja sin bet → Check
