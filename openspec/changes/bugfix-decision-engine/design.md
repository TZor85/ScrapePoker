## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `PostflopDecisionService.cs` | `ShouldBluff()`: eliminar parámetro `BetSizeCategory betSize`, renombrar condición. Combo draw bonus: agregar guard `heroHandRank < Straight`. `HandleLowEquity`: actualizar llamada a `ShouldBluff`. |
| `DangerPenaltyCalculator.cs` | Separar flush y straight penalty, aplicar `Math.Max` en vez de suma. |
| `FrmMain.cs` | River `DetermineAction`: agregar `villainAggressorCheckedPreviousStreet`. |
| `StrategyProfile.cs` | Cambiar defaults de 6 parámetros. |
| `appsettings.json` | Actualizar valores de calibración StrategyProfile. |

---

## Fix 1: Bluff condition `IPCoordinatedSmallOnly`

### Antes
```csharp
// ShouldBluff recibe betSize del villano
private bool ShouldBluff(StreetThresholds thresholds, bool isInPosition,
    string boardTexture, BetSizeCategory betSize, BoardPosition street)
{
    // ...
    BluffConditionType.IPCoordinatedSmallOnly =>
        isInPosition && boardTexture == "Coordinated" &&
        betSize == BetSizeCategory.Small &&  // ← SIEMPRE NoBet aquí
        Random.Shared.NextDouble() < bluffFreq,
}

// HandleLowEquity pasa villainBetSize (siempre NoBet porque !isFacingBet)
ShouldBluff(thresholds, isInPosition, boardTexture, villainBetSize, street)
```

### Después
```csharp
// ShouldBluff ya no recibe betSize — no tiene sentido sin facing bet
private bool ShouldBluff(StreetThresholds thresholds, bool isInPosition,
    string boardTexture, BoardPosition street)
{
    // ...
    // Renombrada: "IP en board coordinated" (sin referencia a bet size)
    BluffConditionType.IPCoordinatedSmallOnly =>
        isInPosition && boardTexture == "Coordinated" &&
        Random.Shared.NextDouble() < bluffFreq,
}
```

---

## Fix 2: Danger penalty doble conteo

### Antes
```csharp
if (boardChange.FlushCompleted)
    penalty += rawEquity * (profile.DangerFlushCompletePct / 100.0);
// ...
if (boardChange.StraightCompleted)
    penalty += rawEquity * (profile.DangerStraightCompletePct / 100.0);
// Resultado: flush(25%) + straight(18%) = 43% de equity como penalty
```

### Después
```csharp
double flushCompletePenalty = boardChange.FlushCompleted
    ? rawEquity * (profile.DangerFlushCompletePct / 100.0)
    : 0;
double straightCompletePenalty = boardChange.StraightCompleted
    ? rawEquity * (profile.DangerStraightCompletePct / 100.0)
    : 0;

// Villano tiene UNA de las dos, no ambas → usar la mayor
penalty += Math.Max(flushCompletePenalty, straightCompletePenalty);

// Flush draw (sin complete) sigue siendo flat
if (!boardChange.FlushCompleted && boardChange.FlushDrawAppeared)
    penalty += profile.DangerFlushDrawPenalty;
```

---

## Fix 3: Combo draw bonus cuando draw completó

### Antes
```csharp
if (hasComboDraw && street != BoardPosition.River)
    effectiveEquity += _profile.ComboDrawEquityBonus;
```

### Después
```csharp
// Solo aplicar bonus si hero todavía tiene draw pendiente (no completado)
if (hasComboDraw && street != BoardPosition.River && heroHandRank < HandRank.Straight)
    effectiveEquity += _profile.ComboDrawEquityBonus;
```

---

## Fix 4: River probe bet missing parameter

### Antes (FrmMain.cs, llamada river DetermineAction)
```csharp
var decision = _postflopDecisionService.DetermineAction(
    equity, BoardPosition.River, effectiveSituation, texture, inPosition,
    betSize,
    // ... otros parámetros ...
    villainBarreling: _postflopContext.VillainBetTurn && maxBet > 0,
    pairClassification: _riverResult.PairType);
    // FALTA: villainAggressorCheckedPreviousStreet
```

### Después
```csharp
var decision = _postflopDecisionService.DetermineAction(
    equity, BoardPosition.River, effectiveSituation, texture, inPosition,
    betSize,
    // ... otros parámetros ...
    villainAggressorCheckedPreviousStreet:
        !_postflopContext.VillainBetTurn && !riverIsAggressor,
    villainBarreling: _postflopContext.VillainBetTurn && maxBet > 0,
    pairClassification: _riverResult.PairType);
```

---

## Fix 5: Calibración de parámetros

| Parámetro | Valor anterior | Valor nuevo | Razón |
|-----------|---------------|-------------|-------|
| `DangerFlushCompletePct` | 25.0 | 35.0 | Flush completado = villano tiene flush ~35-40% en rangos normales |
| `ReverseImpliedFlushDrawPenalty` | 4.0 | 7.0 | OnePair facing turn bet en board con flush draw pierde mucho más |
| `ReverseImpliedCoordinatedPenalty` | 2.0 | 4.0 | Board coordinated DangerLevel ≥ 2 requiere más cautela |
| `BluffCatchFoldBelowMultiplier` | 0.85 | 0.75 | 85% demasiado generoso; hero paga bluff catches -EV |
| `FloatingIPMinEquity` | 20.0 | 25.0 | 20% equity demasiado wide; pierde contra cualquier par |
| `SlowPlayMinEquity` | 80.0 | 72.0 | 80% tan restrictivo que slow play nunca ocurre con sets |

No se modifica `ImpliedOddsSPRShallowFactor` (0.95→0.98) en este sprint para evitar cambios acumulativos. Se evaluará tras medir impacto de los otros ajustes.
