# Spec L6: Multiway OOP Factor Configurable

## Descripcion

El penalty cuadratico OOP multiway usa un factor `×0.5` hardcodeado sin fundamentacion empirica. Este factor amortigua la progresion cuadratica pero su valor es arbitrario. Debe extraerse a `StrategyProfile` para calibracion via backtest A/B.

## Ubicacion

`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`, lineas 248-266

## Codigo Actual

```csharp
// Lineas 248-266
double multiwayFoldPenalty;
double multiwayValuePenalty;

if (isInPosition)
{
    // IP: lineal
    multiwayFoldPenalty = extraOpponents * PokerConstants.MultiwayFoldBelowIP;      // 2.0
    multiwayValuePenalty = extraOpponents * PokerConstants.MultiwayThinValueIP;      // 2.0
}
else
{
    // OOP: cuadratico con factor 0.5 arbitrario
    multiwayFoldPenalty = extraOpponents * extraOpponents * PokerConstants.MultiwayFoldBelowOOP * 0.5;
    multiwayValuePenalty = extraOpponents * extraOpponents * PokerConstants.MultiwayThinValueOOP * 0.5;
}

adjustedFoldBelow += multiwayFoldPenalty * streetMult;
adjustedThinValueAbove += multiwayValuePenalty * streetMult;
```

## Constantes Actuales (PokerConstants.cs)

```csharp
public const double MultiwayFoldBelowIP = 2.0;
public const double MultiwayFoldBelowOOP = 6.0;
public const double MultiwayThinValueIP = 2.0;
public const double MultiwayThinValueOOP = 4.0;
```

## Analisis del Factor 0.5

### Progresion actual (×0.5)

| Extra Opp | IP Fold | OOP Fold (×0.5) | OOP Value (×0.5) |
|-----------|---------|-----------------|-----------------|
| 1 | 2.0 | 3.0 | 2.0 |
| 2 | 4.0 | 12.0 | 8.0 |
| 3 | 6.0 | 27.0 | 18.0 |

### Comparacion con factores alternativos

| Extra Opp | OOP Fold (×0.3) | OOP Fold (×0.5) | OOP Fold (×0.7) |
|-----------|----------------|-----------------|-----------------|
| 1 | 1.8 | 3.0 | 4.2 |
| 2 | 7.2 | 12.0 | 16.8 |
| 3 | 16.2 | 27.0 | 37.8 |

Con ×0.7, 3 extra opp OOP sube FoldBelow +37.8 en Flop (×1.0), +45.4 en Turn (×1.2), +52.9 en River (×1.4). Probablemente excesivo.
Con ×0.3, 2 extra opp OOP sube FoldBelow +7.2, que es apenas mayor que 2 extra opp IP (4.0). Probablemente insuficiente.

**Rango razonable: 0.3 a 0.7.** Default 0.5 es sensato pero debe ser calibrable.

## Fix Propuesto

```csharp
else
{
    // OOP: cuadratico con factor configurable
    multiwayFoldPenalty = extraOpponents * extraOpponents
        * PokerConstants.MultiwayFoldBelowOOP * _profile.MultiwayOOPQuadraticDamping;
    multiwayValuePenalty = extraOpponents * extraOpponents
        * PokerConstants.MultiwayThinValueOOP * _profile.MultiwayOOPQuadraticDamping;
}
```

## Nuevo Parametro en StrategyProfile

```csharp
/// <summary>
/// Factor de amortiguacion para el penalty cuadratico OOP multiway.
/// Rango recomendado: 0.3-0.7. Default 0.5.
/// Valores mas bajos = menos agresivo contra multiway OOP.
/// Valores mas altos = mas conservador (folds mas frecuentes).
/// </summary>
public double MultiwayOOPQuadraticDamping { get; set; } = 0.5;
```

## Escenarios BDD

### Escenario 1: Factor default 0.5 — comportamiento identico al actual
```
Dado MultiwayOOPQuadraticDamping = 0.5, isMultiway, OOP, extraOpponents = 2
Cuando se calcula multiway penalty en Flop (streetMult 1.0)
Entonces multiwayFoldPenalty = 2 × 2 × 6.0 × 0.5 = 12.0
Y multiwayValuePenalty = 2 × 2 × 4.0 × 0.5 = 8.0
Y adjustedFoldBelow += 12.0
Y adjustedThinValueAbove += 8.0
```

### Escenario 2: Factor reducido 0.3 — penalty menor
```
Dado MultiwayOOPQuadraticDamping = 0.3, isMultiway, OOP, extraOpponents = 2
Cuando se calcula multiway penalty en Flop
Entonces multiwayFoldPenalty = 2 × 2 × 6.0 × 0.3 = 7.2
Y multiwayValuePenalty = 2 × 2 × 4.0 × 0.3 = 4.8
```

### Escenario 3: Factor aumentado 0.7 — penalty mayor
```
Dado MultiwayOOPQuadraticDamping = 0.7, isMultiway, OOP, extraOpponents = 2
Cuando se calcula multiway penalty en Flop
Entonces multiwayFoldPenalty = 2 × 2 × 6.0 × 0.7 = 16.8
Y multiwayValuePenalty = 2 × 2 × 4.0 × 0.7 = 11.2
```

### Escenario 4: IP no afectado — regresion
```
Dado MultiwayOOPQuadraticDamping = 0.3, isMultiway, IP, extraOpponents = 2
Cuando se calcula multiway penalty
Entonces multiwayFoldPenalty = 2 × 2.0 = 4.0 (lineal, sin damping)
Y multiwayValuePenalty = 2 × 2.0 = 4.0
```

### Escenario 5: Street multiplier aplicado correctamente
```
Dado MultiwayOOPQuadraticDamping = 0.5, OOP, extraOpponents = 1
Cuando se calcula multiway penalty en River (streetMult 1.4)
Entonces multiwayFoldPenalty = 1 × 1 × 6.0 × 0.5 = 3.0
Y adjustedFoldBelow += 3.0 × 1.4 = 4.2
```

### Escenario 6: 1 oponente extra OOP — minimo
```
Dado MultiwayOOPQuadraticDamping = 0.5, OOP, extraOpponents = 1
Cuando se calcula multiway penalty en Flop
Entonces multiwayFoldPenalty = 1 × 1 × 6.0 × 0.5 = 3.0 (cuadratico = lineal con 1)
```

### Escenario 7: No multiway — sin penalty (regresion)
```
Dado isMultiway = false
Cuando se calcula multiway penalty
Entonces adjustedFoldBelow no cambia por multiway
Y adjustedThinValueAbove no cambia por multiway
```

## Tests Requeridos

1. **Test_MultiwayOOP_DefaultDamping_MatchesCurrent** — 0.5 → identico al actual
2. **Test_MultiwayOOP_LowerDamping_ReducesPenalty** — 0.3 → penalty menor
3. **Test_MultiwayOOP_HigherDamping_IncreasesPenalty** — 0.7 → penalty mayor
4. **Test_MultiwayIP_NotAffectedByDamping** — IP siempre lineal
5. **Test_MultiwayOOP_StreetMultiplierApplied** — streetMult correcto
6. **Test_MultiwayOOP_OneExtraOpponent_QuadraticEqualsLinear** — n²=n cuando n=1
7. **Test_NotMultiway_NoPenalty** — regresion: sin multiway, sin penalty
