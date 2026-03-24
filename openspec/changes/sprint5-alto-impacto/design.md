# Sprint 5 — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `OutsCalculator.cs` | S5.1: Contar overcards siempre, separar clean/overlap |
| `DangerPenaltyCalculator.cs` | S5.2: Multiplicador por street |
| `StrategyProfile.cs` | S5.2: Nuevos parámetros DangerPenaltyFlopMultiplier, DangerPenaltyRiverMultiplier |
| `PokerConstants.cs` | S5.3: Nuevas constantes MultiwayFoldBelowIP, MultiwayFoldBelowOOP |
| `PostflopDecisionService.cs` | S5.3: Multiway IP/OOP, S5.4: case "Wet" |
| `BoardTextureAnalyzer.cs` | S5.4: SimplifiedTexture → "Wet" para Wet category |
| `StreetThresholds.cs` | S5.4: Nuevo campo WetBoardBetSize |
| `appsettings.json` | S5.4: WetBoardBetSize en configs, S5.5: Flop_DonkBet, Flop_DonkBetVsOpenRaise |

---

## S5.1 — Overcards con draws

### Antes
```csharp
// OutsCalculator.cs:86
bool hasMainDraw = flushOuts > 0 || straightCompletingRanks.Count >= 2;
bool hasMadeHand = HasMadeFlush(...) || HasFiveCardStraight(...);
if (communityCards.Count >= 3 && !hasMainDraw && !hasMadeHand)
{
    // Contar overcards (3 outs cada una)
}
```

### Después
```csharp
// Siempre contar overcards, pero excluir las que ya están contadas como straight outs
if (communityCards.Count >= 3 && !hasMadeHand)
{
    int maxBoardRank = communityRanks.Max();
    var overcardRanks = heroRanks.Where(r => r > maxBoardRank).Distinct().ToList();

    foreach (var rank in overcardRanks)
    {
        // Si este rank ya es un straight completing rank, no doble-contar
        bool isAlreadyStraightOut = straightCompletingRanks.Contains(rank);
        if (!isAlreadyStraightOut)
        {
            result.CleanOuts += PokerConstants.OvercardOutsPerCard; // 3
            result.DrawTypes.Add($"Overcard ({RankToString(rank)})");
        }
    }
}
```

### Ejemplo: AK en 9-8-7

| Antes | Después |
|-------|---------|
| OESD: 8 outs (6,Q completan) | OESD: 8 outs (6,Q completan) |
| Overcards: 0 (hasMainDraw=true) | Overcards: +6 (A y K no son straight completing ranks) |
| Total: 8 outs | Total: 14 outs |
| Equity estimada: ~35% | Equity estimada: ~55% |

---

## S5.2 — Danger penalty escalada por street

### Antes
```csharp
// DangerPenaltyCalculator.cs:20-28
double flushCompletePenalty = boardChange.FlushCompleted
    ? rawEquity * (profile.DangerFlushCompletePct / 100.0)  // 35% fijo
    : 0;
```

### Después
```csharp
// Multiplicador por street: flop más riesgo (2 calles), river menos (definitivo)
double streetDangerMultiplier = street switch
{
    BoardPosition.Flop => profile.DangerPenaltyFlopMultiplier,    // 1.3
    BoardPosition.River => profile.DangerPenaltyRiverMultiplier,  // 0.8
    _ => 1.0  // Turn sin ajuste
};

double flushCompletePenalty = boardChange.FlushCompleted
    ? rawEquity * (profile.DangerFlushCompletePct / 100.0) * streetDangerMultiplier
    : 0;
double straightCompletePenalty = boardChange.StraightCompleted
    ? rawEquity * (profile.DangerStraightCompletePct / 100.0) * streetDangerMultiplier
    : 0;
```

**Nota:** Requiere agregar `BoardPosition street` como parámetro a `DangerPenaltyCalculator.Calculate()` y propagarlo desde `PostflopDecisionService`.

### Nuevos parámetros — StrategyProfile.cs
```csharp
public double DangerPenaltyFlopMultiplier { get; set; } = 1.3;
public double DangerPenaltyRiverMultiplier { get; set; } = 0.8;
```

---

## S5.3 — Multiway penalty IP vs OOP

### Antes
```csharp
// PostflopDecisionService.cs:173-178
if (isMultiway)
{
    int extraOpponents = numOpponents - 1;
    adjustedFoldBelow += extraOpponents * PokerConstants.MultiwayFoldBelowPerOpponent;  // 4.0
    adjustedThinValueAbove += extraOpponents * PokerConstants.MultiwayThinValuePerOpponent;  // 3.0
}
```

### Después
```csharp
if (isMultiway)
{
    int extraOpponents = numOpponents - 1;
    double multiwayFoldPenalty = isInPosition
        ? PokerConstants.MultiwayFoldBelowIP      // 2.0 — IP puede aislar
        : PokerConstants.MultiwayFoldBelowOOP;     // 6.0 — OOP muy vulnerable
    double multiwayValuePenalty = isInPosition
        ? PokerConstants.MultiwayThinValueIP       // 2.0
        : PokerConstants.MultiwayThinValueOOP;     // 4.0
    adjustedFoldBelow += extraOpponents * multiwayFoldPenalty;
    adjustedThinValueAbove += extraOpponents * multiwayValuePenalty;
}
```

### Nuevas constantes — PokerConstants.cs
```csharp
// Multiway: IP puede aislar, OOP vulnerable
public const double MultiwayFoldBelowIP = 2.0;
public const double MultiwayFoldBelowOOP = 6.0;
public const double MultiwayThinValueIP = 2.0;
public const double MultiwayThinValueOOP = 4.0;
```

### Bluff bloqueado OOP multiway
```csharp
// En HandleLowEquity, antes del bluff check:
if (!isFacingBet && !isMultiway && thresholds.CanBluff && ...)
// Cambiar a:
bool canBluffHere = !isFacingBet && thresholds.CanBluff &&
    !(isMultiway && !isInPosition);  // No bluffear multiway OOP
```

---

## S5.4 — Separar SemiWet y Wet

### Antes
```csharp
// BoardTextureAnalyzer.cs:23-28
public string SimplifiedTexture => IsMonotone
    ? "Monotone"
    : Category switch
    {
        BoardTextureCategory.Paired => "Paired",
        BoardTextureCategory.Wet or BoardTextureCategory.SemiWet => "Coordinated",
        _ => "Dry"
    };
```

### Después
```csharp
public string SimplifiedTexture => IsMonotone
    ? "Monotone"
    : Category switch
    {
        BoardTextureCategory.Paired => "Paired",
        BoardTextureCategory.Wet => "Wet",
        BoardTextureCategory.SemiWet => "Coordinated",
        _ => "Dry"
    };
```

### Nuevo campo — StreetThresholds.cs
```csharp
public string WetBoardBetSize { get; init; } = "Bet 1/3";
```

### HandleNoBet actualizado — PostflopDecisionService.cs
```csharp
var baseBetThreshold = boardTexture switch
{
    "Dry" => thresholds.DryBoardBetSize,
    "Coordinated" => thresholds.CoordinatedBoardBetSize,
    "Paired" => thresholds.PairedBoardBetSize,
    "Monotone" => thresholds.MonotoneBoardBetSize,
    "Wet" => thresholds.WetBoardBetSize,
    _ => thresholds.DryBoardBetSize
};
```

---

## S5.5 — Configs Flop_DonkBet y Flop_DonkBetVsOpenRaise

### Flop_DonkBet
Donk bet en flop indica que el villain (no agresor) apuesta primero. Suele ser mano débil-media (draws, middle pair) intentando tomar la iniciativa.

```json
"Flop_DonkBet": {
    "FoldBelow": 32,
    "ThinValueAbove": 38,
    "ValueAbove": 52,
    "StrongValueAbove": 72,
    "StrongValueBetSize": "Bet 3/4",
    "ValueBetSize": "Bet 1/2",
    "ThinValueBetSize": "Bet 1/3",
    "DryBoardBetSize": "Bet 1/2",
    "CoordinatedBoardBetSize": "Bet 1/2",
    "PairedBoardBetSize": "Bet 3/4",
    "MonotoneBoardBetSize": "Bet 1/4",
    "WetBoardBetSize": "Bet 1/3",
    "CanBluff": true,
    "BluffFrequencyMultiplier": 1.2,
    "BluffBetSize": "Bet 1/3",
    "BluffCondition": "Always",
    "LowEquityAction": "Call",
    "ThinValueIPOnly": false,
    "ThinValueOOPFallback": "CheckCall",
    "ReduceSizeForLargeBet": false,
    "ReduceSizeForOOP": false,
    "IsSimplified": false,
    "CanCheckRaise": true,
    "CheckRaiseThreshold": 75
}
```

### Flop_DonkBetVsOpenRaise
Hero fue agresor preflop y villain lidera con donk bet. Hero tiene ventaja de rango → puede raise más.

```json
"Flop_DonkBetVsOpenRaise": {
    "FoldBelow": 38,
    "ThinValueAbove": 42,
    "ValueAbove": 55,
    "StrongValueAbove": 75,
    "StrongValueBetSize": "Bet 3/4",
    "ValueBetSize": "Bet 1/2",
    "ThinValueBetSize": "Bet 1/3",
    "DryBoardBetSize": "Bet 1/2",
    "CoordinatedBoardBetSize": "Bet 2/3",
    "PairedBoardBetSize": "Bet 3/4",
    "MonotoneBoardBetSize": "Bet 1/4",
    "WetBoardBetSize": "Bet 1/3",
    "CanBluff": true,
    "BluffFrequencyMultiplier": 1.0,
    "BluffBetSize": "Bet 1/3",
    "BluffCondition": "Always",
    "LowEquityAction": "Fold",
    "ThinValueIPOnly": false,
    "ReduceSizeForLargeBet": true,
    "ReduceSizeForOOP": false,
    "IsSimplified": false
}
```

### Justificación de thresholds

| Campo | DonkBet | DonkBetVsOpenRaise | Razón |
|-------|---------|-------------------|-------|
| FoldBelow | 32 | 38 | Donk suele ser débil → call más. Vs OpenRaise hero tiene rango fuerte → fold más selectivo |
| ThinValueAbove | 38 | 42 | Hero puede thin-value vs donk débil |
| ValueAbove | 52 | 55 | Estándar flop |
| StrongValueAbove | 72 | 75 | Estándar flop |
| CanCheckRaise | true | false | Contra donk pure → check-raise trap. Vs open raise → raise directo |
| LowEquityAction | Call | Fold | Donk puede ser draw → call más. Vs agresor → fold sin equity |
