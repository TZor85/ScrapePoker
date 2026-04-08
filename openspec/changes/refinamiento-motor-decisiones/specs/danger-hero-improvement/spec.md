# Spec L3: Danger Penalties Skip Cuando Hero Completo el Draw

## Descripcion

`DangerPenaltyCalculator` aplica penalties por FlushCompleted (equity × 35%) y StraightCompleted (equity × 18%) basandose unicamente en el estado del board, sin verificar si hero es quien completo el draw. Esto penaliza absurdamente cuando hero tiene la mejor mano posible.

## Ubicacion

`src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs`, lineas 46-59

## Codigo Actual

```csharp
// Lineas 46-48: Solo depende de streetDangerMultiplier
double streetDangerMultiplier = GetStreetMultiplier(street, profile);

// Lineas 50-55: Penalties por completar — NO verifican si hero se beneficio
double flushCompletePenalty = boardChange.FlushCompleted
    ? rawEquity * (profile.DangerFlushCompletePct / 100.0) * streetDangerMultiplier
    : 0;
double straightCompletePenalty = boardChange.StraightCompleted
    ? rawEquity * (profile.DangerStraightCompletePct / 100.0) * streetDangerMultiplier
    : 0;

// Linea 57: Penalties maximas (Math.Max, no sumadas)
double completedPenalty = Math.Max(flushCompletePenalty, straightCompletePenalty);
penalty += completedPenalty;
```

## Analisis del Problema

Ejemplos concretos:
1. **Hero tiene As-Ks, board 3s-7s-Jh-2s** → Hero completo nut flush. DangerPenalty aplica -35% equity.
2. **Hero tiene 8h-9h, board 6d-7c-Ts-Jd** → Hero completo straight. DangerPenalty aplica -18% equity.
3. **Hero tiene Ah-Kh, board 3s-7s-Jh-2s** → Hero NO tiene flush, villain podria. Penalty correcto.

El blocker existente (lineas 81-93) mitiga parcialmente el caso 1 con ×0.35, pero no lo elimina. Hero con nut flush recibe `equity × 35% × 0.35 = equity × 12.25%` de penalizacion — injustificada.

## Solucion

Añadir parametro `heroCompletedFlush` y `heroCompletedStraight` al metodo `Calculate()`. Si hero completo el draw, skip la penalty correspondiente.

La deteccion de si hero completo el draw ya existe implicitamente en el caller (`PostflopDecisionService.DetermineAction`): `heroHandRank >= HandRank.Flush` para flush, `heroHandRank >= HandRank.Straight` para straight.

## Cambios en Firma

```csharp
public static double Calculate(
    double rawEquity,
    BoardChangeResult boardChange,
    bool heroBlocksDangerSuit,
    bool isFacingBet,
    StrategyProfile profile,
    BoardPosition street = BoardPosition.Turn,
    bool heroHasNutBlocker = false,
    HandRank heroHandRank = HandRank.HighCard,
    bool heroCompletedFlush = false,      // NUEVO
    bool heroCompletedStraight = false)    // NUEVO
```

## Fix Propuesto

```csharp
// Skip penalty si hero es quien completo el draw
double flushCompletePenalty = (boardChange.FlushCompleted && !heroCompletedFlush)
    ? rawEquity * (profile.DangerFlushCompletePct / 100.0) * streetDangerMultiplier
    : 0;

double straightCompletePenalty = (boardChange.StraightCompleted && !heroCompletedStraight)
    ? rawEquity * (profile.DangerStraightCompletePct / 100.0) * streetDangerMultiplier
    : 0;
```

## Call Site Update

En `PostflopDecisionService.DetermineAction()`, al llamar a `CalculateDangerPenalty`:

```csharp
double dangerPenalty = CalculateDangerPenalty(
    rawEquity, boardChange, heroBlocksDangerSuit, isFacingBet, street, heroHasNutBlocker, heroHandRank,
    heroCompletedFlush: heroHandRank >= HandRank.Flush,
    heroCompletedStraight: heroHandRank >= HandRank.Straight && heroHandRank < HandRank.Flush);
```

**Nota:** `heroCompletedStraight` usa `< HandRank.Flush` para evitar doble-skip cuando hero tiene flush (que ya es Flush+).

## Escenarios BDD

### Escenario 1: Hero completo flush — penalty flush = 0
```
Dado rawEquity 75, board FlushCompleted, heroCompletedFlush = true
Cuando se calcula DangerPenalty
Entonces flushCompletePenalty = 0
Y el penalty total NO incluye componente de flush completed
```

### Escenario 2: Villain completo flush — penalty normal
```
Dado rawEquity 40, board FlushCompleted, heroCompletedFlush = false
Cuando se calcula DangerPenalty en Turn (streetMult 1.0)
Entonces flushCompletePenalty = 40 × 0.35 × 1.0 = 14.0
```

### Escenario 3: Hero completo straight — penalty straight = 0
```
Dado rawEquity 70, board StraightCompleted, heroCompletedStraight = true
Cuando se calcula DangerPenalty
Entonces straightCompletePenalty = 0
```

### Escenario 4: Board tiene flush Y straight completed, hero solo tiene flush
```
Dado rawEquity 65, FlushCompleted + StraightCompleted
Y heroCompletedFlush = true, heroCompletedStraight = false
Cuando se calcula DangerPenalty en Turn
Entonces flushCompletePenalty = 0 (hero tiene flush)
Y straightCompletePenalty = 65 × 0.18 × 1.0 = 11.7 (hero no tiene straight)
Y completedPenalty = Math.Max(0, 11.7) = 11.7
```

### Escenario 5: Hero completo flush — blocker ya no importa
```
Dado rawEquity 80, FlushCompleted, heroCompletedFlush = true, heroBlocksDangerSuit = true
Cuando se calcula DangerPenalty
Entonces flushCompletePenalty = 0 (skip antes de llegar a blocker logic)
```

### Escenario 6: Ni flush ni straight completed — sin cambios
```
Dado rawEquity 50, FlushDrawAppeared (no completed)
Cuando se calcula DangerPenalty
Entonces flushCompletePenalty = 0, straightCompletePenalty = 0
Y comportamiento identico al actual
```

## Tests Requeridos

1. **Test_HeroCompletedFlush_SkipFlushPenalty** — heroCompletedFlush=true → 0
2. **Test_VillainCompletedFlush_NormalPenalty** — heroCompletedFlush=false → penalty normal
3. **Test_HeroCompletedStraight_SkipStraightPenalty** — heroCompletedStraight=true → 0
4. **Test_BothCompleted_HeroOnlyFlush_StraightPenaltyApplies** — flush skip, straight aplica
5. **Test_HeroCompletedFlush_BlockerIrrelevant** — skip antes de blocker
6. **Test_NoCompletion_BehaviorUnchanged** — regresion: sin completion, logica intacta
7. **Test_CallSite_FlushRank_SetsHeroCompletedFlush** — integracion: HandRank.Flush → true
8. **Test_CallSite_StraightRank_SetsHeroCompletedStraight** — integracion: HandRank.Straight → true
