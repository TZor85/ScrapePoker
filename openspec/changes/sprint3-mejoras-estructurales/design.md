# Sprint 3 — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `PostflopDecisionService.cs` | S2.3 facing bet scaling, S3.1 bluff catch turn, S3.2 floating IP, S3.3 fold equity bluff, S3.4 monotone sizing |
| `PokerConstants.cs` | Nuevas constantes de street multiplier para facing bet |
| `StrategyProfile.cs` | Nuevos parámetros: `BluffCatchTurnEquityMultiplier`, `FloatingIPMinOuts`, `MonotoneBoardBetSize` + validaciones |
| `StreetThresholds.cs` | Nuevo campo `MonotoneBoardBetSize` |
| `UnifiedPokerCalculator.cs` | Pasar `handSituation` al calculador preflop |
| `PreflopEquityCalculator.cs` | Overload con `VillainRange` para situaciones 3Bet+ |

---

## S2.3 — Facing bet penalty escalado por street

### Problema
Las penalizaciones por bet size son planas: Small+1, Medium+4, Large+8 en cualquier street. En flop, large bets son comunes (c-bets, range bets); en river, large bets indican rango fuerte.

### Antes
```csharp
// PostflopDecisionService.cs:147-155
double facingBetPenalty = villainBetSize switch
{
    BetSizeCategory.Large => PokerConstants.FacingBetPenaltyLarge,    // 8.0
    BetSizeCategory.Medium => PokerConstants.FacingBetPenaltyMedium,  // 4.0
    BetSizeCategory.Small => PokerConstants.FacingBetPenaltySmall,    // 1.0
    _ => 0
};
adjustedFoldBelow += facingBetPenalty;
adjustedThinValueAbove += facingBetPenalty / 2;
```

### Después
```csharp
// PostflopDecisionService.cs:147-160
double facingBetPenalty = villainBetSize switch
{
    BetSizeCategory.Large => PokerConstants.FacingBetPenaltyLarge,
    BetSizeCategory.Medium => PokerConstants.FacingBetPenaltyMedium,
    BetSizeCategory.Small => PokerConstants.FacingBetPenaltySmall,
    _ => 0
};

// Escalar por street: flop bets grandes son normales, river bets grandes = rango fuerte
double streetMultiplier = street switch
{
    BoardPosition.Turn => PokerConstants.FacingBetTurnMultiplier,     // 1.15
    BoardPosition.River => PokerConstants.FacingBetRiverMultiplier,   // 1.30
    _ => 1.0  // Flop sin ajuste
};
facingBetPenalty *= streetMultiplier;

adjustedFoldBelow += facingBetPenalty;
adjustedThinValueAbove += facingBetPenalty / 2;
```

```csharp
// PokerConstants.cs — nuevas constantes
public const double FacingBetTurnMultiplier = 1.15;
public const double FacingBetRiverMultiplier = 1.30;
```

### Impacto numérico
| Street | Large bet penalty antes | Large bet penalty después |
|--------|------------------------|--------------------------|
| Flop   | 8.0                    | 8.0 (×1.0)               |
| Turn   | 8.0                    | 9.2 (×1.15)              |
| River  | 8.0                    | 10.4 (×1.3)              |

---

## S3.1 — Bluff catch en turn (no solo river)

### Problema
`HandleLowEquity` (línea 704) solo permite bluff catch en `street == BoardPosition.River`. En turn, hero con OnePair+ facing small bet con equity marginal pierde spots +EV al no poder call.

### Antes
```csharp
// PostflopDecisionService.cs:704-730
if (street == BoardPosition.River && heroHandRank >= HandRank.OnePair &&
    villainBetSize != BetSizeCategory.Large &&
    pairClassification != PairClassification.BoardPaired)
{
    double bluffCatchThreshold = thresholds.FoldBelow * _profile.BluffCatchFoldBelowMultiplier;
    // ... blocker/bottom pair logic
    if (equity >= bluffCatchThreshold)
        return new PostflopDecisionResult("Call", reason);
}
```

### Después
```csharp
// PostflopDecisionService.cs — extender bluff catch a turn+river
bool isBluffCatchStreet = street == BoardPosition.River ||
    (street == BoardPosition.Turn && villainBetSize == BetSizeCategory.Small);

if (isBluffCatchStreet && heroHandRank >= HandRank.OnePair &&
    villainBetSize != BetSizeCategory.Large &&
    pairClassification != PairClassification.BoardPaired)
{
    // Turn: umbral más estricto (0.90 del FoldBelow, configurado con BluffCatchTurnEquityMultiplier)
    double baseMultiplier = street == BoardPosition.Turn
        ? _profile.BluffCatchTurnEquityMultiplier   // 0.90 — más exigente
        : _profile.BluffCatchFoldBelowMultiplier;    // 0.75 — river estándar
    double bluffCatchThreshold = thresholds.FoldBelow * baseMultiplier;

    // En turn: requiere MiddlePair+ (no BottomPair, más riesgo con 1 calle por venir)
    if (street == BoardPosition.Turn && pairClassification == PairClassification.BottomPair)
        goto skipBluffCatch;

    // ... blocker/bottom pair logic existente (sin cambios) ...

    if (equity >= bluffCatchThreshold)
    {
        string streetLabel = street == BoardPosition.Turn ? "turn" : "river";
        return new PostflopDecisionResult("Call",
            $"Call — bluff catch {streetLabel} ({parLabel})");
    }
}
skipBluffCatch:
```

```csharp
// StrategyProfile.cs — nuevo parámetro
public double BluffCatchTurnEquityMultiplier { get; set; } = 0.90;
```

### Condiciones turn vs river

| Condición | Turn | River |
|-----------|------|-------|
| Bet size del villano | Solo Small | Small o Medium |
| HandRank mínimo | OnePair (MiddlePair+) | OnePair (cualquiera) |
| Umbral equity | FoldBelow × 0.90 | FoldBelow × 0.75 |
| BottomPair | NO bluff catch | Sí (con umbral ×1.15) |

---

## S3.2 — Floating IP requiere draw real

### Problema
La condición `totalOuts >= 4` para floating incluye overcards sin draw legítimo. Hero float-calls con 9h2d en AcKcQc board con "4 outs" pero sin ningún draw real — un float -EV.

### Antes
```csharp
// PostflopDecisionService.cs:343-346
if (isInPosition && street == BoardPosition.Flop &&
    villainBetSize != BetSizeCategory.Large &&
    heroHandRank <= HandRank.OnePair && totalOuts >= 4 &&
    equity >= _profile.FloatingIPMinEquity && equity <= _profile.FloatingIPMaxEquity)
```

### Después
```csharp
// PostflopDecisionService.cs — requiere draw real
bool hasRealDraw = hasFlushDraw || hasComboDraw || totalOuts >= _profile.FloatingIPMinOuts;

if (isInPosition && street == BoardPosition.Flop &&
    villainBetSize != BetSizeCategory.Large &&
    heroHandRank <= HandRank.OnePair && totalOuts >= 4 && hasRealDraw &&
    equity >= _profile.FloatingIPMinEquity && equity <= _profile.FloatingIPMaxEquity)
```

**Nota:** `hasFlushDraw`, `hasComboDraw` y `totalOuts` ya son parámetros del método `HandleFacingBet`, por lo que no se requiere añadir nuevas dependencias.

```csharp
// StrategyProfile.cs — nuevo parámetro (outs mínimos para considerar draw real sin flush/combo draw)
public double FloatingIPMinOuts { get; set; } = 6;
```

### Tabla de validación

| Mano | Board | totalOuts | hasFlushDraw | hasComboDraw | hasRealDraw | Float? |
|------|-------|-----------|-------------|-------------|-------------|--------|
| 9h2d | AcKcQc | 4 (overcards) | false | false | false (4<6) | NO |
| Th9h | AcKc3d | 4 (overcard+gut) | false | false | false (4<6) | NO |
| 8h7h | Ac5c3d | 8 (OESD) | false | false | true (8>=6) | SÍ |
| Jh9h | Ac5h3h | 9 (flush draw) | true | false | true | SÍ |
| 8h7h | Ac5h3h | 15 (combo) | true | true | true | SÍ |

---

## S3.3 — Fold equity check en bluffs puros

### Problema
`ShouldBluff()` decide bluffear solo con frecuencia aleatoria, sin verificar si el bluff es matemáticamente rentable. Un bluff de 1/2 pot necesita ~33% fold equity para ser break-even.

### Antes
```csharp
// PostflopDecisionService.cs:678-685
if (!isFacingBet && !isMultiway && thresholds.CanBluff &&
    ShouldBluff(thresholds, isInPosition, boardTexture, street))
{
    return new PostflopDecisionResult(
        thresholds.BluffBetSize + " (Bluff)",
        "Bluff según condiciones",
        IsBluff: true);
}
```

### Después
```csharp
// PostflopDecisionService.cs — verificar fold equity antes de bluffear
if (!isFacingBet && !isMultiway && thresholds.CanBluff &&
    ShouldBluff(thresholds, isInPosition, boardTexture, street))
{
    // Verificar que el bluff sea +EV: fold equity >= breakeven threshold
    double betFraction = BetStringToFraction(thresholds.BluffBetSize);
    double breakevenFoldEquity = betFraction / (1.0 + betFraction);
    double actualFoldEquity = foldEquity / 100.0;  // foldEquity ya está en parámetros de DetermineAction

    if (actualFoldEquity >= breakevenFoldEquity)
    {
        return new PostflopDecisionResult(
            thresholds.BluffBetSize + " (Bluff)",
            $"Bluff +EV (fold equity={foldEquity:F0}% >= {breakevenFoldEquity * 100:F0}%)",
            IsBluff: true);
    }
    // Si fold equity insuficiente → cae al check de equity baja
}
```

**Nota:** `foldEquity` ya se calcula en `UnifiedPokerCalculator.CalculateFoldEquity()` y se pasa como parámetro a `DetermineAction`. `BetStringToFraction()` ya existe en el servicio (usado por `ApplyDynamicSizing`).

### Tabla de break-even fold equity

| Bluff size | betFraction | Break-even fold equity |
|-----------|-------------|----------------------|
| Bet 1/4   | 0.25        | 20%                  |
| Bet 1/3   | 0.33        | 25%                  |
| Bet 1/2   | 0.50        | 33%                  |
| Bet 3/4   | 0.75        | 43%                  |
| Bet Pot    | 1.00        | 50%                  |

---

## S3.4 — Board texture "Monotone" con sizing específico

### Problema
Boards con 3+ cartas del mismo palo se clasifican como "Dry" o "Coordinated" pero no tienen sizing propio. El c-bet en board monotone debería ser menor (1/4 pot) porque:
- Flush draw es muy probable para el villano (~25% de starting hands contienen suited cards del mismo palo)
- Bet grande da odds incorrectas al flush draw y extrae valor negativo cuando hero pierde

### Antes
```csharp
// PostflopDecisionService.cs:424-430
var baseBetThreshold = boardTexture switch
{
    "Dry" => thresholds.DryBoardBetSize,
    "Coordinated" => thresholds.CoordinatedBoardBetSize,
    "Paired" => thresholds.PairedBoardBetSize,
    _ => thresholds.DryBoardBetSize
};
```

### Después
```csharp
// PostflopDecisionService.cs — agregar case Monotone
var baseBetThreshold = boardTexture switch
{
    "Dry" => thresholds.DryBoardBetSize,
    "Coordinated" => thresholds.CoordinatedBoardBetSize,
    "Paired" => thresholds.PairedBoardBetSize,
    "Monotone" => thresholds.MonotoneBoardBetSize,
    _ => thresholds.DryBoardBetSize
};
```

```csharp
// StreetThresholds.cs — nuevo campo
public string MonotoneBoardBetSize { get; init; } = "Bet 1/4";
```

### Requisito en BoardTextureAnalyzer

Verificar que `BoardTextureAnalyzer.Analyze()` ya clasifica boards como "Monotone" cuando 3+ cartas comparten palo. Si no, agregar detección:

```csharp
// Si 3+ cartas del mismo palo → Category = "Monotone"
if (suitCounts.Any(c => c >= 3))
    return new BoardTextureResult { Category = "Monotone", ... };
```

**Nota:** `PokerConstants.WetnessMonotoneScore = 35.0` ya existe, lo que sugiere que el analyzer ya detecta monotone pero puede clasificarlo como "SemiWet" en vez de "Monotone" como categoría propia.

---

## S3.5 — Preflop equity vs VillainRange (no vs random)

### Problema
La equity preflop siempre se calcula contra rango aleatorio:
```csharp
// UnifiedPokerCalculator.cs:192-194
if (communityCards.Count == 0) // Preflop
{
    return _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
}
```

En situaciones 3Bet/4Bet, el rango del villano es mucho más estrecho. JTs tiene ~55% equity vs random pero ~38% vs rango de 3Bet.

### Después
```csharp
// UnifiedPokerCalculator.cs — usar VillainRange en preflop si disponible
if (communityCards.Count == 0) // Preflop
{
    // Si hay VillainRange para esta situación, usar Monte Carlo con ese rango
    VillainRange? villainRange = null;
    if (handSituation != null && Enum.TryParse<HandSituation>(handSituation, out var situation))
    {
        villainRange = VillainRange.GetForSituation(situation);
    }

    if (villainRange != null)
    {
        // Monte Carlo preflop contra rango filtrado (sin community cards)
        var result = _monteCarloSimulator.CalculateEquity(
            playerHand, new List<CardDataOuts>(), numOpponents,
            _monteCarloIterations, villainRange);
        return result.Equity * 100;
    }

    return _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
}
```

### Rangos afectados

| HandSituation | Rango del villano | Equity con lookup | Equity con MC |
|--------------|-------------------|-------------------|---------------|
| OpenRaise | Random | lookup table | — (sin cambio) |
| ThreeBet | ~5-8% rango | lookup (incorrecto) | MC vs rango |
| OpenRaiseVs3Bet | ~5-8% rango | lookup (incorrecto) | MC vs rango |
| FourBet | ~2-3% rango | lookup (muy incorrecto) | MC vs rango |
| Squeeze | ~5-10% rango | lookup (incorrecto) | MC vs rango |

### Riesgo
El Monte Carlo preflop sin community cards es más lento que el lookup table. Con 1000 iteraciones (default) el impacto debería ser <50ms. Si es problemático, se puede cachear por `(hand, situation, numOpponents)`.

---

## Resumen de nuevos parámetros

| Parámetro | Ubicación | Valor | Propósito |
|-----------|-----------|-------|-----------|
| `FacingBetTurnMultiplier` | PokerConstants | 1.15 | Escalar penalty en turn |
| `FacingBetRiverMultiplier` | PokerConstants | 1.30 | Escalar penalty en river |
| `BluffCatchTurnEquityMultiplier` | StrategyProfile | 0.90 | Umbral bluff catch en turn |
| `FloatingIPMinOuts` | StrategyProfile | 6 | Outs mínimos para float sin flush/combo draw |
| `MonotoneBoardBetSize` | StreetThresholds | "Bet 1/4" | Sizing en board monotone |
