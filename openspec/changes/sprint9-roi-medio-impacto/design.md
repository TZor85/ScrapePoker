# Sprint 9 — Diseño Detallado

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `PostflopDecisionService.cs` | S9.1: fold equity con stats reales. S9.2: semi-bluff fold equity. S9.3: check-raise draws. S9.4: pot commitment. |
| `OpponentTracker.cs` | S9.1: nuevo método `GetFoldToBetPct()`. |
| `FrmMain.cs` | S9.1: propagar `villainFoldToBetPct`. |
| `StrategyProfile.cs` | S9.3: parámetros check-raise draws. S9.4: pot commitment threshold. |
| `PokerConstants.cs` | S9.4: constantes pot commitment. |

---

## S9.1 — Fold equity con stats reales del OpponentTracker

### Problema
```csharp
// PostflopDecisionService.cs línea 234-250
var (opponentFoldAdj, opponentValueAdj) = villainType switch
{
    OpponentType.LP => (-4.0, -2.0),     // Multiplier fijo
    OpponentType.LAG when isFacingBet => (-5.0, -3.0),  // Multiplier fijo
    // ...
};
```
Un LAG con 80% fold-to-3bet se trata igual que uno con 20%.

### Solución

Nuevo parámetro en `DetermineAction`:
```csharp
double villainFoldToBetPct = -1  // -1 = no disponible, usar fallback estático
```

Nuevo método en `OpponentTracker`:
```csharp
/// <summary>
/// Retorna % de veces que el villano foldeó ante una apuesta postflop.
/// Retorna -1 si no hay suficientes manos (< 10 situaciones).
/// </summary>
public double GetFoldToBetPct(string villainId)
{
    if (!_profiles.TryGetValue(villainId, out var profile))
        return -1;

    int totalFacingBet = profile.PostflopFacingBetCount;
    if (totalFacingBet < 10) return -1;

    return (double)profile.PostflopFoldCount / totalFacingBet * 100.0;
}
```

Requiere trackear `PostflopFacingBetCount` y `PostflopFoldCount` en `OpponentProfile`.

### Uso en PostflopDecisionService

En la sección de ajuste por oponente:
```csharp
if (villainFoldToBetPct >= 0)
{
    // Stats reales disponibles: ajustar fold equity directamente
    // >60% fold = fish que foldea mucho → FoldBelow más bajo
    // <30% fold = calling station → FoldBelow más alto (no bluffear)
    double foldAdj = villainFoldToBetPct > 60 ? -5.0
                   : villainFoldToBetPct > 45 ? -2.0
                   : villainFoldToBetPct < 30 ? 4.0
                   : villainFoldToBetPct < 40 ? 2.0
                   : 0.0;
    adjustedFoldBelow += foldAdj;
}
else if (villainType != OpponentType.Unknown)
{
    // Fallback: multipliers estáticos por tipo (código existente)
    // ...
}
```

---

## S9.2 — Semi-bluff con verificación de fold equity

### Problema
```csharp
// PostflopDecisionService.cs línea 717-731
// Semi-bluff se ejecuta si tiene outs suficientes, SIN verificar fold equity
if (totalOuts >= PokerConstants.MinOutsForDraw && street != BoardPosition.River &&
    !isFacingBet && !isMultiway)
{
    return new PostflopDecisionResult(semiBluffSize + " (Semi-Bluff)", ...);
}
```

### Solución

Aplicar el mismo check de fold equity que ya existe para bluffs puros:

```csharp
if (totalOuts >= PokerConstants.MinOutsForDraw && street != BoardPosition.River &&
    !isFacingBet && !isMultiway)
{
    // Verificar fold equity: semi-bluff debe ser +EV considerando equity del draw
    double betFraction = BetStringToFraction(semiBluffSize);
    double breakevenFE = betFraction / (1.0 + betFraction);

    // Draw equity reduce el breakeven FE necesario (semi-bluff tiene backup)
    double drawEquity = totalOuts * (street == BoardPosition.Turn
        ? PokerConstants.TurnOutsMultiplier
        : PokerConstants.RiverOutsMultiplier) / 100.0;
    double adjustedBreakevenFE = Math.Max(0, breakevenFE - drawEquity);
    double actualFE = foldEquity / 100.0;

    if (actualFE >= adjustedBreakevenFE)
    {
        return new PostflopDecisionResult(
            semiBluffSize + " (Semi-Bluff)",
            $"Semi-bluff +EV: {totalOuts} outs, FE={foldEquity:F0}% >= {adjustedBreakevenFE*100:F0}%",
            IsBluff: true);
    }
    // Si fold equity insuficiente → no semi-bluffear, seguir al siguiente path
}
```

---

## S9.3 — Check-raise ampliado a draws fuertes

### Problema
```csharp
// PostflopDecisionService.cs línea 441-449
if (thresholds.CanCheckRaise && !isInPosition && !isMultiway &&
    equity > thresholds.CheckRaiseThreshold &&
    heroHandRank >= HandRank.TwoPair &&  // ← SOLO TwoPair+
    !heroIsAggressor)
```

### Solución

Permitir check-raise con draws fuertes:

```csharp
// Check-raise: OOP con mano premium O draw fuerte
bool hasStrongMade = heroHandRank >= HandRank.TwoPair;
bool hasStrongDraw = street == BoardPosition.Flop &&
    (hasComboDraw || (hasFlushDraw && totalOuts >= 9)) &&
    equity >= _profile.CheckRaiseDrawMinEquity;  // Default: 40

if (thresholds.CanCheckRaise && !isInPosition && !isMultiway &&
    equity > thresholds.CheckRaiseThreshold &&
    (hasStrongMade || hasStrongDraw) &&
    !heroIsAggressor)
{
    string reason = hasStrongDraw
        ? $"Check-raise semi-bluff — {totalOuts} outs OOP"
        : $"Check-raise trap — {heroHandRank} OOP";
    return new PostflopDecisionResult(
        "Check (Check-Raise)", reason, IsCheckRaise: true);
}
```

Nuevo parámetro en `StrategyProfile`:
```csharp
public double CheckRaiseDrawMinEquity { get; init; } = 40.0;
```

Requiere propagar `hasComboDraw`, `hasFlushDraw`, `totalOuts` a `HandleNoBet()`.

---

## S9.4 — Pot commitment detection

### Problema
Hero con SPR 0.3 restante: pot odds son ~77%, equity 25%. EV(call) puede ser +EV pero el bot foldea porque equity < adjustedFoldBelow (~35).

### Solución

En `HandleFacingBet`, antes del fold final:

```csharp
// Pot commitment: si hero ya invirtió mucho, fold puede ser -EV
if (heroStack > 0 && potSize > 0)
{
    double spr = (double)(heroStack / potSize);
    // Con SPR < 0.5, hero está pot committed
    if (spr < PokerConstants.PotCommitmentSPRThreshold)
    {
        // Calcular si call es +EV: equity × (pot + call) > call
        double callAmount = (double)heroStack; // all-in call
        double totalPot = (double)potSize + callAmount;
        double evCall = (equity / 100.0) * totalPot - (1.0 - equity / 100.0) * callAmount;

        if (evCall > 0)
            return new PostflopDecisionResult("Call",
                $"Call — pot committed (SPR={spr:F2}, EV call={evCall:F1})");
    }
}
```

Nuevas constantes:
```csharp
// PokerConstants.cs
public const double PotCommitmentSPRThreshold = 0.5;
```

Este check debe ir ANTES del `return Fold` en `HandleFacingBet` y también antes del fold final en `HandleLowEquity`.
