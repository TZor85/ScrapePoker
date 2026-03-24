# Bugfix Live Session — Diseño Técnico

## Archivos a modificar

| Archivo | Bug |
|---------|-----|
| `PreflopAnalyzer.cs` | Bug 2: HasRangeAdvantageOnBoard para boards mixtos |
| `FrmMain.cs` | Bug 3: pot reprocess, Bug 4: numOpponents Math.Max |
| `PostflopDecisionService.cs` | Bug 5: DetermineSimplifiedAction con board texture |

---

## Bug 2 — HasRangeAdvantageOnBoard false positives en 3Bet pots

### Problema
Board T-5-2 en 3Bet pot: `isLowBoard = false` (T=10 > 9), así que la condición `!(isLowBoard && isConnected)` retorna `true`. Pero T-5-2 **no favorece al rango de 3Bet** (AA-QQ, AK, AQs) — es un board donde el caller conecta con más combos (Tx, pocket pairs 55, 22).

### Antes
```csharp
if (is3BetPot)
    return !(isLowBoard && boardTexture.IsConnected);
```

### Después
```csharp
if (is3BetPot)
{
    // 3Bet range tiene ventaja en boards con A/K (overpairs + TPTK)
    // NO tiene ventaja en boards bajos, medios sin A/K, o mixtos (una alta + bajas)
    if (hasAceOrKing)
        return true;
    if (isLowBoard)
        return false;
    // Board mixto: una carta alta (T-Q) + cartas bajas → caller conecta más
    bool isMixedLowBoard = highCards <= 0 && flopRanks.Min() <= 6;
    if (isMixedLowBoard)
        return false;
    return highCards >= 2;  // 2+ cartas altas → rango 3Bet tiene ventaja
}
```

### Tabla de validación

| Board | hasA/K | isLow | highCards | isMixedLow | Antes | Después | Correcto? |
|-------|--------|-------|-----------|------------|-------|---------|-----------|
| AcKd3h | true | false | 2 | false | true | true | ✓ |
| Tc5d2d | false | false | 0 | true (min=2) | true | **false** | ✓ (fix) |
| QhJdTs | false | false | 2 | false | true | true | ✓ |
| 7h5d3c | false | true | 0 | true | false | false | ✓ |
| Kh8d4c | true | false | 1 | true | true | true | ✓ |

---

## Bug 3 — Pot size = 0 al re-procesar turn

### Problema
`ProcessPostFlopAsync` re-procesa el turn sin llamar a `SetPotValue()`:
```csharp
// Línea ~970 de FrmMain.cs (ProcessPostFlopAsync)
else
{
    // Misma calle, reprocessar turn con info actualizada
    await ProcessTurnAsync();  // ← pot no actualizado
}
```

### Después
Agregar `SetPotValue()` y `SetBetValues()` antes de re-procesar:
```csharp
else
{
    // Actualizar pot y bets antes de re-procesar (villain puede haber apostado)
    SetPotValue();
    SetBetValues();
    await ProcessTurnAsync();
}
```

**Nota:** Hay que encontrar TODOS los puntos donde se re-procesa (turn y river) y agregar la actualización de pot/bets.

---

## Bug 4 — numOpponents sin Math.Max en turn/river

### Problema
```csharp
// Turn (línea ~1482):
var numOpponents = _playerGameState.Players.Count(p => p.Active) - 1;  // Puede ser 0 o -1

// Flop (línea ~1383) — correcto:
var numOpponents = Math.Max(1, _playerGameState.Players.Count(p => p.Active) - 1);
```

### Después
```csharp
// Turn y River: agregar Math.Max(1, ...)
var numOpponents = Math.Max(1, _playerGameState.Players.Count(p => p.Active) - 1);
```

---

## Bug 5 — DetermineSimplifiedAction ignora board texture

### Problema
```csharp
private static PostflopDecisionResult DetermineSimplifiedAction(
    double equity, StreetThresholds thresholds, bool isInPosition, bool isFacingBet)
```
No recibe `boardTexture`. KK en board monotone 3c5c7c usa "Bet 1/2" fijo.

### Después
Agregar `string boardTexture = "Dry"` como parámetro. Adaptar sizing en NoBet:

```csharp
private static PostflopDecisionResult DetermineSimplifiedAction(
    double equity, StreetThresholds thresholds, bool isInPosition, bool isFacingBet,
    string boardTexture = "Dry")
{
    if (isInPosition)
    {
        if (equity > thresholds.StrongValueAbove)
        {
            // Adaptar sizing por board texture
            string betSize = boardTexture switch
            {
                "Monotone" => "Bet 1/4 (Value)",
                "Wet" => "Bet 1/3 (Value)",
                _ => thresholds.SimplifiedIPStrongBet
            };
            return new PostflopDecisionResult(
                isFacingBet ? "Raise 3x (Value)" : betSize,
                "Strong value IP");
        }
        // ... thin value y low equity similar
    }
    // OOP: adaptar también
    if (equity > thresholds.StrongValueAbove)
    {
        string betSize = boardTexture switch
        {
            "Monotone" => "Bet 1/4 (Value)",
            "Wet" => "Bet 1/3 (Value)",
            _ => thresholds.SimplifiedOOPStrongBet
        };
        return new PostflopDecisionResult(
            isFacingBet ? "Raise 3x (Value)" : betSize,
            "Strong value OOP");
    }
    // ... resto
}
```

Propagar `boardTexture` desde `DetermineAction`:
```csharp
if (thresholds.IsSimplified)
    return DetermineSimplifiedAction(effectiveEquity, thresholds, isInPosition, isFacingBet, boardTexture);
```
