# Spec L2: Flush Draw Penalty con Blocker Adjustment

## Descripcion

`DangerPenaltyCalculator` aplica blocker adjustment para flush **completados** (lineas 81-93) pero NO para flush **draws** (lineas 61-71). Esto crea una asimetria donde hero con el As de la suit peligrosa recibe la misma penalizacion de flush draw que hero sin ningun blocker.

## Ubicacion

`src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs`, lineas 61-71

## Codigo Actual

```csharp
// Lineas 61-71: Flush draw penalty — SIN blocker adjustment
if (!boardChange.FlushCompleted && boardChange.FlushDrawAppeared)
{
    double flushDrawPenalty = rawEquity * 0.08 * streetDangerMultiplier;
    if (heroHandRank >= HandRank.TwoPair)
        flushDrawPenalty *= 0.5;
    else if (heroHandRank == HandRank.OnePair)
        flushDrawPenalty *= 0.75;
    penalty += flushDrawPenalty;
}

// Lineas 81-93: Flush completed — CON blocker adjustment (ya existente)
if (heroBlocksDangerSuit)
{
    bool isBoard4Flush = boardChange.FlushCompleted && boardChange.DangerLevel >= 4;
    double blockerReduction;
    if (isBoard4Flush)
        blockerReduction = profile.DangerBlockerBoard4FlushReduction;    // 0.70
    else if (heroHasNutBlocker)
        blockerReduction = profile.DangerNutBlockerReduction;            // 0.35
    else
        blockerReduction = profile.DangerNonNutBlockerReduction;         // 0.55
    penalty *= blockerReduction;
}
```

## Analisis del Gap

La reduccion por blocker en flush completado usa 3 niveles:
- **Nut blocker** (As del suit): ×0.35 (reduce 65%)
- **Non-nut blocker** (Ks/Qs del suit): ×0.55 (reduce 45%)
- **Board 4-flush**: ×0.70 (reduce 30%)

Para flush draws, la reduccion deberia ser **menor** porque el draw aun no se completo (el blocker reduce probabilidad de completar, no elimina):
- Nut blocker contra draw: ~50% reduccion (villain tiene 1 menos de las cartas altas del suit)
- Non-nut blocker contra draw: ~30% reduccion

## Fix Propuesto

```csharp
if (!boardChange.FlushCompleted && boardChange.FlushDrawAppeared)
{
    double flushDrawPenalty = rawEquity * 0.08 * streetDangerMultiplier;

    // Reducir por hand rank
    if (heroHandRank >= HandRank.TwoPair)
        flushDrawPenalty *= 0.5;
    else if (heroHandRank == HandRank.OnePair)
        flushDrawPenalty *= 0.75;

    // Reducir por blocker (nuevo)
    if (heroBlocksDangerSuit)
    {
        flushDrawPenalty *= heroHasNutBlocker
            ? profile.DangerFlushDrawNutBlockerReduction
            : profile.DangerFlushDrawNonNutBlockerReduction;
    }

    penalty += flushDrawPenalty;
}
```

## Nuevos Parametros en StrategyProfile

```csharp
// Blocker reduction para flush DRAW (no completado)
public double DangerFlushDrawNutBlockerReduction { get; set; } = 0.50;
public double DangerFlushDrawNonNutBlockerReduction { get; set; } = 0.70;
```

## Escenarios BDD

### Escenario 1: Flush draw sin blocker — penalty completo
```
Dado rawEquity 60, board con FlushDrawAppeared, heroHandRank HighCard
Y hero NO bloquea danger suit
Cuando se calcula DangerPenalty en Turn (streetMult 1.0)
Entonces flushDrawPenalty = 60 × 0.08 × 1.0 = 4.8
Y penalty incluye 4.8
```

### Escenario 2: Flush draw con nut blocker — penalty reducido 50%
```
Dado rawEquity 60, board con FlushDrawAppeared, heroHandRank HighCard
Y hero tiene As del danger suit (nut blocker)
Cuando se calcula DangerPenalty en Turn
Entonces flushDrawPenalty = 60 × 0.08 × 1.0 × 0.50 = 2.4
Y penalty incluye 2.4
```

### Escenario 3: Flush draw con non-nut blocker — penalty reducido 30%
```
Dado rawEquity 60, board con FlushDrawAppeared, heroHandRank HighCard
Y hero tiene Ks del danger suit (non-nut blocker)
Cuando se calcula DangerPenalty en Turn
Entonces flushDrawPenalty = 60 × 0.08 × 1.0 × 0.70 = 3.36
Y penalty incluye 3.36
```

### Escenario 4: Flush draw + OnePair + nut blocker — ambas reducciones
```
Dado rawEquity 55, board con FlushDrawAppeared, heroHandRank OnePair
Y hero tiene As del danger suit
Cuando se calcula DangerPenalty en Turn
Entonces flushDrawPenalty = 55 × 0.08 × 1.0 × 0.75 × 0.50 = 1.65
```

### Escenario 5: Flush draw + TwoPair sin blocker
```
Dado rawEquity 70, board con FlushDrawAppeared, heroHandRank TwoPair
Y hero NO bloquea danger suit
Cuando se calcula DangerPenalty en Turn
Entonces flushDrawPenalty = 70 × 0.08 × 1.0 × 0.5 = 2.8
```

### Escenario 6: Flush completado usa blocker existente (no cambia)
```
Dado rawEquity 60, board con FlushCompleted
Y hero tiene nut blocker
Cuando se calcula DangerPenalty
Entonces penalty total *= DangerNutBlockerReduction (0.35) — logica existente sin cambios
```

## Tests Requeridos

1. **Test_FlushDraw_NoBlocker_FullPenalty** — sin blocker → penalty completo
2. **Test_FlushDraw_NutBlocker_ReducedPenalty** — As suit → ×0.50
3. **Test_FlushDraw_NonNutBlocker_ReducedPenalty** — Ks suit → ×0.70
4. **Test_FlushDraw_OnePair_NutBlocker_BothReductions** — ×0.75 × ×0.50
5. **Test_FlushDraw_TwoPair_NonNutBlocker_BothReductions** — ×0.5 × ×0.70
6. **Test_FlushCompleted_BlockerUnchanged** — regresion: logica existente intacta
