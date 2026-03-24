# Sprint 7 — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `ImpliedOddsCalculator.cs` | S7.1: Interpolación cuadrática (Math.Sqrt) |
| `OutsCalculator.cs` | S7.2: Tainted outs descuento variable |
| `PostflopDecisionService.cs` | S7.3: Bluff frequency × factor SPR |
| `StrategyProfile.cs` | S7.3: Nuevos parámetros BluffSPRShort/DeepMultiplier |
| `appsettings.json` | S7.4: FoldBelow en Turn/River_RaiseOverLimper |

---

## S7.1 — Implied odds interpolación cuadrática

### Problema
Interpolación lineal entre SPR 2.0 (shallow, factor 0.95) y SPR 4.0 (deep, factor 0.65):
- SPR 3.0 lineal → 0.80
- SPR 3.0 real → ~0.72 (implied odds crecen más rápido acercándose a deep)

### Antes
```csharp
// ImpliedOddsCalculator.cs:40-43
double range = profile.ImpliedOddsSPRDeepThreshold - profile.ImpliedOddsSPRShallowThreshold;
double position = (spr - profile.ImpliedOddsSPRShallowThreshold) / range;
sprFactor = profile.ImpliedOddsSPRShallowFactor +
    (position * (profile.ImpliedOddsSPRDeepFactor - profile.ImpliedOddsSPRShallowFactor));
```

### Después
```csharp
double range = profile.ImpliedOddsSPRDeepThreshold - profile.ImpliedOddsSPRShallowThreshold;
double position = (spr - profile.ImpliedOddsSPRShallowThreshold) / range;
// Interpolación cuadrática: implied odds crecen más rápido acercándose a deep
double curvedPosition = Math.Sqrt(position);
sprFactor = profile.ImpliedOddsSPRShallowFactor +
    (curvedPosition * (profile.ImpliedOddsSPRDeepFactor - profile.ImpliedOddsSPRShallowFactor));
```

### Tabla comparativa

| SPR | Lineal (position) | Sqrt (curvedPosition) | Factor lineal | Factor sqrt |
|-----|-------------------|----------------------|---------------|-------------|
| 2.0 | 0.00 | 0.00 | 0.95 | 0.95 |
| 2.5 | 0.25 | 0.50 | 0.875 | 0.80 |
| 3.0 | 0.50 | 0.71 | 0.80 | 0.74 |
| 3.5 | 0.75 | 0.87 | 0.725 | 0.69 |
| 4.0 | 1.00 | 1.00 | 0.65 | 0.65 |

---

## S7.2 — Tainted outs descuento variable

### Problema
Descuento uniforme `TaintedOutsDiscount = 0.5` para todos los tainted outs. Pero:
- Out que da trips al villano cuando hero hace flush → hero gana → descuento alto (0.7)
- Out que da flush al villano cuando hero hace trips → hero pierde → descuento bajo (0.3)

### Enfoque
En `CalculateTaintedOuts`, ya se identifica cada out como "tainted" (mejora al villano también). El descuento variable se basa en si el out mejora más al hero (strong draw: flush/straight) que al villano (weak improvement: pair/trips):

```csharp
// Si hero tiene flush draw y el tainted out también paired el board:
// Hero: completa flush (muy fuerte). Villano: trips (débil vs flush). Descuento: 0.7
// Si hero tiene gutshot y el tainted out da flush al villano:
// Hero: straight (fuerte). Villano: flush (más fuerte). Descuento: 0.3
```

### Después — OutsCalculator.cs

En `CalculateOuts()`, después de calcular TaintedOuts:

```csharp
// Descuento variable: hero mejora más → descuento alto; villano mejora más → descuento bajo
double taintedDiscount = hasFlushDraw
    ? _profile.TaintedOutsDiscountHeroStrong     // 0.7 — flush es casi siempre mejor
    : _profile.TaintedOutsDiscountHeroWeak;       // 0.3 — trips/pair pierde vs flush
result.EffectiveOuts = result.CleanOuts + (result.TaintedOuts * taintedDiscount);
```

### Nuevos parámetros — StrategyProfile.cs
```csharp
// Tainted outs: descuento cuando hero tiene draw fuerte (flush) vs débil (pair/trips)
public double TaintedOutsDiscountHeroStrong { get; set; } = 0.7;
public double TaintedOutsDiscountHeroWeak { get; set; } = 0.3;
```

**Nota:** `TaintedOutsDiscount = 0.5` se mantiene como fallback por retrocompatibilidad.

---

## S7.3 — Bluff frequency modulada por SPR

### Problema
Bluff frequency es fija (Flop 0.15, Turn 0.12, River 0.10) sin importar SPR.
- SPR < 2: bluffs son menos rentables (stacks cortos, más committed)
- SPR > 4: bluffs son más rentables (más fold equity, menos riesgo relativo)

### Después — PostflopDecisionService.cs

En `HandleLowEquity`, antes de `ShouldBluff`:

```csharp
// Nota: ShouldBluff ya usa GetBluffFrequency(street) * thresholds.BluffFrequencyMultiplier
// Agregar modulación por SPR al GetBluffFrequency o al check de canBluffHere
```

Mejor: modular en `GetBluffFrequency` directamente o aplicar factor antes de llamar a `ShouldBluff`. Pero `GetBluffFrequency` no tiene acceso a SPR. Alternativa: aplicar factor en la condición de bluff:

```csharp
bool canBluffHere = !isFacingBet && thresholds.CanBluff &&
    !(isMultiway && !isInPosition);
if (canBluffHere && ShouldBluff(thresholds, isInPosition, boardTexture, street))
{
    // Factor SPR sobre bluff frequency (ya integrado en ShouldBluff via random)
    // Verificar fold equity breakeven...
}
```

Enfoque más limpio: agregar `sprBluffMultiplier` a la verificación de fold equity:

```csharp
// En HandleLowEquity, después de ShouldBluff:
double sprBluffMultiplier = 1.0;
if (heroStack > 0 && potSize > 0)
{
    double spr = (double)(heroStack / potSize);
    if (spr < _profile.BluffSPRShortThreshold)  // < 2.0
        sprBluffMultiplier = _profile.BluffSPRShortMultiplier;  // 0.5
    else if (spr > _profile.BluffSPRDeepThreshold)  // > 4.0
        sprBluffMultiplier = _profile.BluffSPRDeepMultiplier;  // 1.2
}

// Aplicar: reducir fold equity requerida (deep) o aumentarla (short)
double adjustedBreakevenFE = breakevenFoldEquity / sprBluffMultiplier;
if (actualFoldEquity >= adjustedBreakevenFE) { ... bluff ... }
```

### Nuevos parámetros — StrategyProfile.cs
```csharp
// Bluff frequency modulada por SPR
public double BluffSPRShortThreshold { get; set; } = 2.0;
public double BluffSPRShortMultiplier { get; set; } = 0.5;
public double BluffSPRDeepThreshold { get; set; } = 4.0;
public double BluffSPRDeepMultiplier { get; set; } = 1.2;
```

---

## S7.4 — FoldBelow=0 en RaiseOverLimper

### Problema
```json
"Turn_RaiseOverLimper": { "FoldBelow": 0, ... }
"River_RaiseOverLimper": { "FoldBelow": 0, ... }
```

Con FoldBelow=0, el bot nunca foldea postflop contra limpers, incluso con 5% equity. Es demasiado agresivo y pierde EV en manos sin equity.

### Fix
```json
"Turn_RaiseOverLimper": { "FoldBelow": 25, ... }
"River_RaiseOverLimper": { "FoldBelow": 30, ... }
```

Turn: 25 (más permisivo, hay carta por venir). River: 30 (más selectivo, última calle).

Ambos mantienen `IsSimplified: true` y el resto de campos sin cambios.
