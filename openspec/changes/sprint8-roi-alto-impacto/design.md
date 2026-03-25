# Sprint 8 — Diseño Detallado

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `PostflopDecisionService.cs` | S8.1: EV allin explícito. S8.2: bluff catch × villainType. S8.4: barrel vs runout. S8.5: thin value river. S8.6: float exit. |
| `PokerConstants.cs` | S8.2: multiplicadores bluff catch. S8.3: penalty underbet. |
| `PostflopDecisionService.cs` (enum) | S8.3: `BetSizeCategory.Underbet`. |
| `FrmMain.cs` | S8.3: clasificar underbet. S8.6: propagar heroFloatedFlop. |
| `StrategyProfile.cs` | S8.2: parámetros bluff catch por oponente. |
| `appsettings.json` | S8.2: valores bluff catch. |

---

## S8.1 — All-In EV explícito para SPR < 1.5

### Problema
```csharp
// PostflopDecisionService.cs línea 268
if (isPushFold && effectiveEquity > thresholds.ValueAbove && heroHandRank >= HandRank.OnePair)
    return new PostflopDecisionResult("All-In (Value)", ...);
```
Requiere equity > ValueAbove (~55) Y OnePair+. Con SPR 1.2 y 42% equity, pot odds son ~33% → all-in es +EV, pero foldea.

### Solución

Nuevo método `CalculateAllinEV()`:

```csharp
/// <summary>
/// Calcula EV de ir all-in vs fold. Usado cuando SPR < 1.5.
/// EV(allin) = equity × (pot + villainEffectiveStack) - (1-equity) × heroRemainingStack
/// Si EV > 0, all-in es +EV independientemente de HandRank.
/// </summary>
private static double CalculateAllinEV(double equity, decimal heroStack, decimal potSize)
{
    double pot = (double)potSize;
    double stack = (double)heroStack;
    // Asumimos villain cubre: totalPot si call = pot + 2×stack (hero + villain)
    double totalPotIfCalled = pot + 2 * stack;
    return (equity / 100.0) * totalPotIfCalled - (1.0 - equity / 100.0) * stack;
}
```

### Antes (push/fold)
```csharp
if (isPushFold && effectiveEquity > thresholds.ValueAbove && heroHandRank >= HandRank.OnePair)
    return new PostflopDecisionResult("All-In (Value)", ...);
```

### Después (push/fold con EV)
```csharp
if (isPushFold)
{
    double allinEV = CalculateAllinEV(effectiveEquity, heroStack, potSize);
    if (allinEV > 0)
        return new PostflopDecisionResult("All-In (Value)",
            $"Push +EV — SPR corto (EV={allinEV:F1}, equity={effectiveEquity:F1}%)");
}
```

Aplicar en ambos puntos: facing bet (línea 268) y no facing bet (línea 279).

---

## S8.2 — Bluff catch ajustado por tipo de oponente

### Problema
```csharp
// PostflopDecisionService.cs línea 795-807
// bluffCatchThreshold se calcula SIN considerar villainType
double bluffCatchThreshold = thresholds.FoldBelow * baseMultiplier;
```

### Solución

Nuevas constantes en `PokerConstants.cs`:
```csharp
// Multiplicadores bluff catch por tipo de oponente
// < 1.0 = call más amplio (villain bluffea más)
// > 1.0 = call más estrecho (villain bluffea menos)
public const double BluffCatchLAGMultiplier = 0.80;
public const double BluffCatchLPMultiplier = 0.85;
public const double BluffCatchTAGMultiplier = 1.00;
public const double BluffCatchTPMultiplier = 1.20;
```

### Antes
```csharp
double bluffCatchThreshold = thresholds.FoldBelow * baseMultiplier;
```

### Después
```csharp
double bluffCatchThreshold = thresholds.FoldBelow * baseMultiplier;

// Ajustar por tipo de oponente: LAG bluffea más → call más amplio
double opponentBluffMultiplier = villainType switch
{
    OpponentType.LAG => PokerConstants.BluffCatchLAGMultiplier,
    OpponentType.LP => PokerConstants.BluffCatchLPMultiplier,
    OpponentType.TAG => PokerConstants.BluffCatchTAGMultiplier,
    OpponentType.TP => PokerConstants.BluffCatchTPMultiplier,
    _ => 1.0
};
bluffCatchThreshold *= opponentBluffMultiplier;
```

Requiere propagar `villainType` a `HandleLowEquity()`.

---

## S8.3 — Underbet tell detection

### Problema
```csharp
public enum BetSizeCategory { NoBet, Small, Medium, Large }
// Un bet de 1/5 pot se clasifica como Small → misma penalty que 1/3 pot
```

### Solución

Nuevo valor en enum:
```csharp
public enum BetSizeCategory { NoBet, Underbet, Small, Medium, Large }
```

Nueva constante:
```csharp
// PokerConstants.cs
public const double FacingBetPenaltyUnderbet = 0; // Sin penalty — indica debilidad
```

### Clasificación en FrmMain.cs

Donde se clasifica el bet size del villano (antes del call a DetermineAction):

```csharp
// Antes
var betSize = maxBet > potSize * 0.6m ? BetSizeCategory.Large
            : maxBet > potSize * 0.3m ? BetSizeCategory.Medium
            : maxBet > 0 ? BetSizeCategory.Small
            : BetSizeCategory.NoBet;

// Después
var betSize = maxBet > potSize * 0.6m ? BetSizeCategory.Large
            : maxBet > potSize * 0.3m ? BetSizeCategory.Medium
            : maxBet > potSize * 0.15m ? BetSizeCategory.Small
            : maxBet > 0 ? BetSizeCategory.Underbet
            : BetSizeCategory.NoBet;
```

### Lógica en HandleFacingBet

Underbet con equity razonable → raise para explotar debilidad:
```csharp
// En HandleFacingBet, antes del return final
if (villainBetSize == BetSizeCategory.Underbet && equity > thresholds.ThinValueAbove)
{
    return new PostflopDecisionResult("Raise 3x (Value)",
        $"Raise vs underbet — villano muestra debilidad ({heroHandRank})");
}
```

### Facing bet penalty

```csharp
BetSizeCategory.Underbet => PokerConstants.FacingBetPenaltyUnderbet, // 0
```

---

## S8.4 — Double barrel condicionado al runout

### Problema
```csharp
// PostflopDecisionService.cs línea 567-581
// Barrelea solo con: previousStreetBet + heroIsAggressor + equity en rango
// NO evalúa si el runout fue bueno o malo para hero
```

### Solución

Evaluar `boardChange` antes de barrelear:

```csharp
// Double barrel: condicionado al runout
if (thresholds.CanDoubleBarrel && previousStreetBet && heroIsAggressor &&
    !isMultiway && street != BoardPosition.Flop &&
    equity >= thresholds.FoldBelow && equity < thresholds.ValueAbove)
{
    // Bad runout: overcard, draw completado, board paireó → check (no barrelear)
    bool badRunout = boardChange != null &&
        (boardChange.NewOvercard || boardChange.FlushCompleted ||
         boardChange.StraightCompleted || boardChange.BoardPaired);

    if (!badRunout)
    {
        var barrelBet = boardTexture == "Dry"
            ? thresholds.ThinValueBetSize
            : thresholds.BluffBetSize;
        barrelBet = AdjustBetSizeForSPR(barrelBet, heroStack, potSize, street);
        return new PostflopDecisionResult(
            barrelBet + " (Barrel)",
            "Double barrel — brick, consistencia de rango",
            IsBarrel: true);
    }
    // Bad runout → cae al check de abajo
}
```

### Nuevo campo en BoardChangeResult

Si `NewOvercard` no existe, añadirlo:
```csharp
// BoardChangeResult
public bool NewOvercard { get; init; } // Carta nueva es overcard al board anterior
```

`AnalyzeBoardChange()` debe detectar si la nueva carta es mayor que la carta más alta previa del board.

---

## S8.5 — Thin value river conservador

### Problema
```csharp
// PostflopDecisionService.cs línea 554
if (equity > thresholds.ThinValueAbove)  // ~45
// Apuesta thin value sin considerar draws completados en board
```

### Solución

En el bloque thin value de `HandleNoBet`, verificar board:

```csharp
// Thin value → bet solo IP (OOP check para proteger rango)
// River: NO thin value si board tiene draws completados y hero no los tiene
if (equity > thresholds.ThinValueAbove)
{
    // River con draw completado: thin value es trampa — villain tiene draw o aire
    bool riverDrawCompleted = street == BoardPosition.River && boardChange != null &&
        (boardChange.FlushCompleted || boardChange.StraightCompleted) &&
        !heroHasCompletedDraw;  // Variable ya existe en DetermineAction

    if (riverDrawCompleted)
        return new PostflopDecisionResult("Check",
            "Check — thin value peligroso, draw completado en river");

    if (!thresholds.ThinValueIPOnly || isInPosition)
    {
        var betSize = AdjustBetSizeForSPR(thresholds.ThinValueBetSize, heroStack, potSize, street);
        return new PostflopDecisionResult(
            betSize + " (Thin Value)",
            "Bet — thin value");
    }

    return new PostflopDecisionResult("Check", "Check — thin value OOP (showdown)");
}
```

Requiere propagar `boardChange` y `heroHasCompletedDraw` al contexto de thin value (ya disponibles en `HandleNoBet`).

---

## S8.6 — Float IP exit strategy

### Problema
El bot floatea en flop (`IsFloating=true`) pero en turn no hay lógica especial. Villain chequea (lo esperado) y hero chequea back con aire → inversión perdida.

### Solución

Nuevo parámetro en `DetermineAction`:
```csharp
bool heroFloatedFlop = false
```

En `HandleNoBet`, antes de probe bet y después de check-raise:
```csharp
// Float exit: hero floateó flop → debe apostar turn si villano chequea
if (heroFloatedFlop && street == BoardPosition.Turn && !isMultiway)
{
    var floatBet = AdjustBetSizeForSPR("Bet 1/2", heroStack, potSize, street);
    return new PostflopDecisionResult(
        floatBet + " (Float Exit)",
        "Bet — exit strategy del float IP");
}
```

### Propagación en FrmMain.cs

Trackear `IsFloating` del resultado del flop y pasar a turn:

```csharp
// En el procesamiento del flop
if (flopDecision.IsFloating)
    _postflopContext.HeroFloatedFlop = true;

// En la llamada a DetermineAction del turn
heroFloatedFlop: _postflopContext.HeroFloatedFlop
```

Nuevo campo en `PostflopGameContext`:
```csharp
public bool HeroFloatedFlop { get; set; }
```
