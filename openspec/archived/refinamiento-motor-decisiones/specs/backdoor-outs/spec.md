# Spec L1: Backdoor Outs con Valores Calibrados

## Descripcion

Los backdoor draws (flush y straight) en `OutsCalculator.CalculateBackdoorOuts()` estan hardcodeados a 1 out cada uno. Los valores reales son ~1.5 para backdoor flush y ~1.0-1.5 para backdoor straight segun gaps. Esto infravalora la equity en flop con backdoor draws.

## Ubicacion

`src/OpenScrape.DecisionMaker/Algorithms/OutsCalculator.cs`, metodo `CalculateBackdoorOuts()`, lineas 200-250

## Codigo Actual

```csharp
// Linea 207: Backdoor Flush
if (hasBackdoorFlush)
{
    result.HasBackdoorFlushDraw = true;
    result.DrawTypes.Add("Backdoor Flush Draw");
    backdoorOuts += 1; // ~1.5 outs implicitos, redondeado a 1
}

// Linea 249: Backdoor Straight
if (hasBackdoorStraight)
{
    result.HasBackdoorStraightDraw = true;
    result.DrawTypes.Add("Backdoor Straight Draw");
    backdoorOuts += 1; // ~1 out implicito
}
```

## Fundamentacion Matematica

### Backdoor Flush Draw (3 suited, necesita 2 runner-runner)
- Cartas restantes del suit: 10 (de 13, con 3 en board+hero)
- P(runner-runner flush) = (10/47) × (9/46) = 90/2162 ≈ 4.16%
- Equity equivalente en outs: con 2 cartas por venir, 1 out ≈ 2× ~4.3% → **~1.5 outs implicitos**
- Variacion: si hero tiene 2 del suit → 9 restantes → (9/47)×(8/46) ≈ 3.33% → ~1.2 outs
- Si hero tiene 1 del suit → 10 restantes → 4.16% → ~1.5 outs

### Backdoor Straight Draw (3 en ventana de 5, necesita 2 runner-runner)
- Depende de gaps en la ventana:
  - **0 gaps** (3 consecutivas, ej. 7-8-9): necesita 2 de 4 ranks posibles → ~1.5 outs
  - **1 gap** (ej. 7-8-T): necesita 1 especifica + 1 de 2 → ~1.0 outs
  - **2 gaps** (ej. 7-9-J): necesita 2 especificas → ~0.5 outs
- Valor promedio ponderado: **~1.0 outs** (conservador)

## Constantes Propuestas (PokerConstants)

```csharp
public const double BackdoorFlushOuts = 1.5;
public const double BackdoorStraightOuts = 1.0;
```

## Fix Propuesto

```csharp
// Backdoor Flush
if (hasBackdoorFlush)
{
    result.HasBackdoorFlushDraw = true;
    result.DrawTypes.Add("Backdoor Flush Draw");
    backdoorOuts += PokerConstants.BackdoorFlushOuts;
}

// Backdoor Straight
if (hasBackdoorStraight)
{
    result.HasBackdoorStraightDraw = true;
    result.DrawTypes.Add("Backdoor Straight Draw");
    backdoorOuts += PokerConstants.BackdoorStraightOuts;
}
```

**Nota:** `backdoorOuts` debe cambiar de `int` a `double`. Propagar el tipo a `CalculateBackdoorOuts()` return type y al sumatorio en `Calculate()` (linea 124). El `TotalOuts` final puede redondearse a `int` al final via `(int)Math.Round()`.

## Escenarios BDD

### Escenario 1: Backdoor flush draw solo
```
Dado hero As-Kh, board 3s-7s-Jh (3 suited spades, hero tiene 1 spade)
Cuando se calculan los backdoor outs
Entonces backdoorOuts incluye 1.5 por backdoor flush draw
Y result.HasBackdoorFlushDraw == true
Y result.DrawTypes contiene "Backdoor Flush Draw"
```

### Escenario 2: Backdoor straight draw solo
```
Dado hero 8h-9d, board 5s-7c-Jh (ventana 5-7-8-9 = 3 en ventana con 1 gap)
Cuando se calculan los backdoor outs
Entonces backdoorOuts incluye 1.0 por backdoor straight draw
Y result.HasBackdoorStraightDraw == true
```

### Escenario 3: Ambos backdoor draws
```
Dado hero 8s-9s, board 5s-7c-Jh (3 suited + 3 en ventana de 5)
Cuando se calculan los backdoor outs
Entonces backdoorOuts total = 2.5 (1.5 flush + 1.0 straight)
Y result.HasBackdoorFlushDraw == true
Y result.HasBackdoorStraightDraw == true
```

### Escenario 4: No backdoor flush si ya tiene flush draw principal
```
Dado hero As-Ks, board 3s-7s-Jh-2d (4 suited = flush draw principal, no backdoor)
Cuando se calculan los backdoor outs
Entonces backdoor flush outs = 0 (flush draw principal ya contabilizado)
Y result.HasBackdoorFlushDraw == false
```

### Escenario 5: No backdoor straight si ya tiene OESD
```
Dado hero 8h-9h, board 6s-7c-Jd (OESD: 5-6-7-8-9-10)
Cuando se calculan los backdoor outs
Entonces backdoor straight outs = 0 (OESD principal ya contabilizado)
Y result.HasBackdoorStraightDraw == false
```

### Escenario 6: Solo aplica en flop (3 community cards)
```
Dado hero 8s-9s, board 5s-7c-Jh-2d (turn = 4 community cards)
Cuando se calculan los outs
Entonces backdoorOuts = 0 (no aplica fuera de flop)
```

### Escenario 7: Hero no contribuye al backdoor
```
Dado hero Ah-Kh, board 3s-7s-Js (3 suited spades, hero sin spades)
Cuando se calculan los backdoor outs
Entonces backdoor flush outs = 0 (hero no tiene suit del backdoor)
```

## Tests Requeridos

1. **Test_BackdoorFlush_Returns1_5Outs** — 3 suited con hero contributing → 1.5
2. **Test_BackdoorStraight_Returns1_0Outs** — 3 en ventana con hero contributing → 1.0
3. **Test_BothBackdoors_Returns2_5Outs** — Ambos draws → 2.5
4. **Test_BackdoorFlush_SkippedWhenFlushDrawExists** — flush draw principal presente → 0
5. **Test_BackdoorStraight_SkippedWhenOESDExists** — OESD presente → 0
6. **Test_BackdoorOuts_OnlyOnFlop** — turn/river → siempre 0
7. **Test_BackdoorFlush_HeroNotContributing_Returns0** — hero sin suit → 0
8. **Test_TotalOuts_IncludesBackdoorAsDouble** — total outs integra backdoor con precision decimal
