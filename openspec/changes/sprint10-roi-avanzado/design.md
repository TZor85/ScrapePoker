# Sprint 10 — Diseño Detallado

## S10.1 — Preflop equity con VillainRange fallback + fold equity multiway

### Problema
```csharp
// UnifiedPokerCalculator.cs línea 216
// Si no hay handSituation → lookup table vs random
return _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
```

### Solución: Preflop fallback
Cuando `handSituation` es null o no parseable, usar `VillainRange.GetForSituation(HandSituation.OpenRaise)` como rango genérico en vez de random:

```csharp
if (communityCards.Count == 0)
{
    VillainRange? preflopRange = null;
    if (handSituation != null && Enum.TryParse<HandSituation>(handSituation, out var preflopSituation))
        preflopRange = VillainRange.GetForSituation(preflopSituation);

    // Fallback: usar rango OpenRaise genérico en vez de random
    preflopRange ??= VillainRange.GetForSituation(HandSituation.OpenRaise);

    if (preflopRange != null)
    {
        var mcResult = _monteCarloSimulator.CalculateEquity(
            playerHand, new List<CardDataOuts>(), numOpponents,
            adaptiveIters, preflopRange);
        return mcResult.Equity * 100;
    }
    // Solo si VillainRange completamente null → lookup table
    return _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
}
```

### Solución: Fold equity multiway
```csharp
// CalculateFoldEquity recibe numOpponents
private double CalculateFoldEquity(double potOddsPercentage, bool isInPosition,
    string handSituation, int communityCardsCount, int numOpponents = 1)
{
    double baseFoldEquity = _profile.FoldEquityBase;
    // ... ajustes existentes ...

    // Multiway: fold equity se reduce drásticamente (más gente que defender)
    if (numOpponents >= 2)
        baseFoldEquity *= 1.0 / (1.0 + 0.3 * (numOpponents - 1));

    return Math.Max(_profile.FoldEquityMin, Math.Min(_profile.FoldEquityMax, baseFoldEquity));
}
```

---

## S10.2 — Bluff catch modulado por runout del river

### Problema
```csharp
// Bluff catch threshold estático, no considera si river fue brick o draw completado
double bluffCatchThreshold = thresholds.FoldBelow * baseMultiplier;
```

### Solución
Después de `opponentBluffMultiplier`, añadir factor por runout:

```csharp
// Runout factor: brick = villain falló draw → más probable bluffeando
// Draw completado = villain puede tenerlo → menos probable bluffeando
if (isRiverBluffCatch && boardChange != null)
{
    bool isBrickRiver = !boardChange.FlushCompleted && !boardChange.StraightCompleted &&
        !boardChange.BoardPaired && !boardChange.OvercardAppeared;
    if (isBrickRiver)
        bluffCatchThreshold *= PokerConstants.BluffCatchBrickRunoutMultiplier; // 0.85
    else if (boardChange.FlushCompleted || boardChange.StraightCompleted)
        bluffCatchThreshold *= PokerConstants.BluffCatchScareRunoutMultiplier; // 1.15
}
```

Nuevas constantes:
```csharp
public const double BluffCatchBrickRunoutMultiplier = 0.85;
public const double BluffCatchScareRunoutMultiplier = 1.15;
```

---

## S10.3 — Implied odds ajustadas por numOpponents

### Problema
```csharp
// ImpliedOddsCalculator no recibe numOpponents
public static double CalculateImpliedOddsFactor(
    BoardPosition street, bool isInPosition, bool hasFlushDraw,
    decimal heroStack, decimal potSize, StrategyProfile profile)
```

### Solución
Añadir `int numOpponents = 1` al método:

```csharp
public static double CalculateImpliedOddsFactor(
    BoardPosition street, bool isInPosition, bool hasFlushDraw,
    decimal heroStack, decimal potSize, StrategyProfile profile,
    int numOpponents = 1)
{
    // ... cálculo existente hasta sprFactor ...

    // Multiway: implied odds peores OOP (villain detrás puede raise/squeeze)
    // IP multiway: implied odds ligeramente mejores (más gente que pagar)
    if (numOpponents >= 2)
    {
        if (!isInPosition)
            sprFactor *= 1.0 + 0.05 * (numOpponents - 1); // OOP: peores implied (factor sube)
        else if (hasFlushDraw)
            sprFactor *= 1.0 - 0.03 * (numOpponents - 1); // IP con draw: mejores implied
    }

    return Math.Max(0.50, Math.Min(1.0, sprFactor));
}
```

Propagar `numOpponents` en `PostflopDecisionService.CalculateImpliedOddsFactor()` y en `DetermineAction`.

---

## S10.4 — Check-raise IP con mano premium

### Problema
```csharp
// Solo OOP
if (thresholds.CanCheckRaise && !isInPosition && !isMultiway && ...)
```

### Solución
Permitir check-raise IP como trap con TwoPair+ en flop/turn:

```csharp
// Check-raise: OOP con mano premium/draw fuerte, O IP con mano premium como trap
bool hasStrongMade = heroHandRank >= HandRank.TwoPair;
bool hasStrongDraw = street == BoardPosition.Flop &&
    (hasComboDraw || (hasFlushDraw && totalOuts >= 9)) &&
    equity >= _profile.CheckRaiseDrawMinEquity;

// IP trap: solo con mano premium (no draws), flop/turn, board no wet
bool ipTrap = isInPosition && hasStrongMade && !isMultiway &&
    heroHandRank >= HandRank.TwoPair &&
    boardTexture != "Wet" && boardTexture != "Monotone";

if (thresholds.CanCheckRaise && !isMultiway &&
    equity > thresholds.CheckRaiseThreshold &&
    !heroIsAggressor)
{
    if (!isInPosition && (hasStrongMade || hasStrongDraw))
    {
        // OOP: check-raise con mano fuerte o draw fuerte (existente)
        ...
    }
    else if (ipTrap)
    {
        return new PostflopDecisionResult(
            "Check (Check-Raise)",
            $"Check-raise IP trap — {heroHandRank}",
            IsCheckRaise: true);
    }
}
```
