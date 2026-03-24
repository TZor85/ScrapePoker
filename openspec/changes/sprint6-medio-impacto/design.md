# Sprint 6 — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `PostflopGameContext.cs` | S6.1: `VillainCheckedMiddleStreet` flag, actualizar `UpdateTurnState` |
| `PostflopDecisionService.cs` | S6.1: barrel vs bet-check-bet, S6.4: probe bet IP, S6.5: slowplay turn |
| `ImpliedOddsCalculator.cs` | S6.2: recibir `heroBlocksDangerSuit`, reducir penalty |
| `DangerPenaltyCalculator.cs` | S6.3: nut blocker vs non-nut, board 4+ flush |
| `StrategyProfile.cs` | S6.1, S6.2, S6.3: nuevos parámetros |
| `StreetThresholds.cs` | S6.4: `ProbeBetIPSize` |
| `FrmMain.cs` | S6.1: actualizar contexto, S6.2: propagar blocker |

---

## S6.1 — Bet-Check-Bet ≠ Barrel

### Problema
`villainBarreling` se activa en turn si `VillainBetFlop && maxBet > 0` y en river si `VillainBetTurn && maxBet > 0`. No distingue:
- **Barrel real**: bet-bet consecutivo (flop bet → turn bet) → rango fuerte, penalty +5
- **Bet-check-bet**: flop bet → turn check → river bet → draw fallido que reintenta, rango débil, penalty menor

### Después — PostflopGameContext.cs

```csharp
/// <summary>
/// El villano apostó en flop pero checkeó en turn → patrón bet-check-bet si apuesta en river.
/// Indica debilidad (draw fallido que reintenta) vs barrel real (rango fuerte).
/// </summary>
public bool VillainCheckedMiddleStreet { get; set; }
```

En `UpdateTurnState`:
```csharp
public void UpdateTurnState(bool heroBet, bool villainBet,
    BetSizeCategory villainBetSize = BetSizeCategory.NoBet)
{
    // Detectar patrón bet-check: villain apostó en flop pero no en turn
    VillainCheckedMiddleStreet = VillainBetFlop && !villainBet;
    // ... resto existente
}
```

En `Reset()`: `VillainCheckedMiddleStreet = false;`

### Después — PostflopDecisionService.cs

```csharp
// Nuevo parámetro en DetermineAction:
bool villainCheckedMiddleStreet = false

// Reemplazar bloque barrel:
if (isFacingBet && villainBarreling)
{
    if (villainCheckedMiddleStreet)
    {
        // Bet-check-bet: draw fallido reintentando, rango más débil
        adjustedFoldBelow += _profile.VillainBetCheckBetPenalty;  // +2
    }
    else
    {
        // Barrel real: bet-bet consecutivo, rango fuerte
        adjustedFoldBelow += _profile.VillainBarrelFoldIncrease;  // +5
        adjustedThinValueAbove += _profile.VillainBarrelThinValueIncrease;
    }
}
```

### Nuevo parámetro — StrategyProfile.cs
```csharp
// Bet-check-bet: penalty menor que barrel (draw fallido reintentando)
public double VillainBetCheckBetPenalty { get; set; } = 2.0;
```

---

## S6.2 — Reverse implied odds con blockers

### Problema
`CalculateReverseImpliedOdds` aplica penalty sin verificar si hero bloquea el draw. Si hero tiene A♠ y el board tiene flush draw en spades, hero reduce combinaciones del villano.

### Después — ImpliedOddsCalculator.cs

```csharp
public static double CalculateReverseImpliedOdds(
    BoardChangeResult? boardChange, HandRank heroHandRank, bool hasFlushDraw,
    BoardPosition street, bool isFacingBet,
    StrategyProfile profile,
    PairClassification pairClassification = PairClassification.None,
    bool heroBlocksDangerSuit = false)  // NUEVO parámetro
{
    // ... lógica existente hasta calcular penalty ...

    // Hero bloquea el draw del villano → reduce penalización
    if (heroBlocksDangerSuit && penalty > 0)
        penalty *= profile.ReverseImpliedBlockerReduction;  // 0.5

    // River: penalización reducida ...
}
```

### Nuevo parámetro — StrategyProfile.cs
```csharp
// Reducción de reverse implied odds cuando hero bloquea el palo del draw
public double ReverseImpliedBlockerReduction { get; set; } = 0.5;
```

### Propagación
- `PostflopDecisionService.CalculateReverseImpliedOdds()` ya recibe `hasFlushDraw`, necesita también `heroBlocksDangerSuit` → propagado desde `DetermineAction`.
- `FrmMain.cs`: ya calcula `heroBlocks` → pasa a `DetermineAction` como `heroBlocksDangerSuit`.

---

## S6.3 — Hero blocker granular

### Problema
La reducción por blocker es siempre `DangerHeroBlocksReduction = 0.5` sin distinguir:
- **Nut blocker** (As del palo completado): elimina la combo más fuerte → ×0.35
- **Non-nut blocker** (carta menor del palo): reduce menos combos → ×0.55
- **Board con 4+ del palo**: flush casi segura en board → blocker menos relevante → ×0.7

### Después — DangerPenaltyCalculator.cs

```csharp
// Blocker effect granular
if (heroBlocksDangerSuit)
{
    var suitCount = boardChange.CompletedFlushSuit >= 0
        ? CountSuitOnBoard(boardChange)  // Si disponible
        : 3;  // Default: 3 del palo en board

    double blockerReduction;
    if (suitCount >= 4)
        blockerReduction = profile.DangerBlockerBoard4FlushReduction;  // 0.7
    else if (isNutBlocker)
        blockerReduction = profile.DangerNutBlockerReduction;          // 0.35
    else
        blockerReduction = profile.DangerNonNutBlockerReduction;       // 0.55

    penalty *= blockerReduction;
}
```

**Nota:** Determinar `isNutBlocker` requiere saber si hero tiene el As del palo completado. Esto requiere un nuevo parámetro `bool heroHasNutBlocker` en `Calculate()`, que se calcula en FrmMain comparando hero cards con el suit completado y verificando si es Ace.

### Nuevos parámetros — StrategyProfile.cs
```csharp
public double DangerNutBlockerReduction { get; set; } = 0.35;
public double DangerNonNutBlockerReduction { get; set; } = 0.55;
public double DangerBlockerBoard4FlushReduction { get; set; } = 0.7;
```

**Nota:** `DangerHeroBlocksReduction` (0.5) se mantiene como fallback pero los nuevos parámetros toman precedencia cuando se pasa `heroHasNutBlocker`.

---

## S6.4 — Probe bet IP + sizing variable

### Problema
Solo permite probe bet OOP (`!isInPosition`). IP probe bet es muy rentable.

### Después — PostflopDecisionService.cs

```csharp
// Probe bet: villano agresor checkeó en street anterior → debilidad
// Permitido tanto OOP como IP (IP con sizing diferente)
if (thresholds.CanProbeBet && villainAggressorCheckedPreviousStreet &&
    !isMultiway &&
    equity >= thresholds.ProbeBetMinEquity)
{
    var probeSize = isInPosition
        ? thresholds.ProbeBetIPSize    // IP: sizing diferente
        : thresholds.ProbeBetSize;     // OOP: sizing original
    return new PostflopDecisionResult(
        probeSize + " (Probe)",
        $"Probe bet {(isInPosition ? "IP" : "OOP")} — agresor checkeó en street anterior");
}
```

### Nuevo campo — StreetThresholds.cs
```csharp
public string ProbeBetIPSize { get; init; } = "Bet 1/2";
```

### Actualización appsettings.json
Agregar `"ProbeBetIPSize": "Bet 1/2"` en thresholds que tienen `CanProbeBet: true`.

---

## S6.5 — Slowplay extendida a turn

### Problema
Solo en flop. En turn con nuts en board seco contra villano agresivo, slowplay induce bet en river.

### Después — PostflopDecisionService.cs

```csharp
// Slow play: check con nuts en board seco para inducir bluff del villano
// Flop: ThreeOfAKind+ en Dry, no agresor, no multiway
// Turn: ThreeOfAKind+ en Dry, equity >= 80%, no multiway, villano LAG o Unknown
bool isSlowPlayStreet = street == BoardPosition.Flop ||
    (street == BoardPosition.Turn && (villainType == OpponentType.LAG || villainType == OpponentType.Unknown));

if (isSlowPlayStreet && boardTexture == "Dry" && !isMultiway &&
    !heroIsAggressor &&
    heroHandRank >= HandRank.ThreeOfAKind &&
    equity >= _profile.SlowPlayMinEquity)
{
    return new PostflopDecisionResult("Check",
        $"Slow play — {heroHandRank} en board seco{(street == BoardPosition.Turn ? " (turn)" : "")}, inducir bluff");
}
```

**Nota:** HandleNoBet necesita recibir `villainType` como parámetro (actualmente no lo tiene). Propagarlo desde `DetermineAction`.

---

## Resumen de nuevos parámetros

| Parámetro | Ubicación | Valor | Propósito |
|-----------|-----------|-------|-----------|
| `VillainBetCheckBetPenalty` | StrategyProfile | 2.0 | Penalty bet-check-bet (< barrel) |
| `ReverseImpliedBlockerReduction` | StrategyProfile | 0.5 | Reducir reverse implied cuando hero bloquea |
| `DangerNutBlockerReduction` | StrategyProfile | 0.35 | Nut blocker (As del palo) |
| `DangerNonNutBlockerReduction` | StrategyProfile | 0.55 | Non-nut blocker |
| `DangerBlockerBoard4FlushReduction` | StrategyProfile | 0.7 | Board con 4+ del palo |
| `VillainCheckedMiddleStreet` | PostflopGameContext | false | Flag bet-check-bet |
| `ProbeBetIPSize` | StreetThresholds | "Bet 1/2" | Probe bet sizing IP |
