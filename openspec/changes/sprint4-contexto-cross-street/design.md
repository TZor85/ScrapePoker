# Sprint 4 — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `PostflopGameContext.cs` | S4.1: campos `VillainBetSizeFlop`, `VillainBetSizeTurn`. S4.2: campo `InitialBoardDanger`. Reset ampliado. |
| `PostflopDecisionService.cs` | S4.1: parámetros opcionales `villainBetSizeFlop`, `villainBetSizeTurn`, penalización por sizing tell. |
| `StrategyProfile.cs` | S4.1: nuevo parámetro `VillainSizingEscalationPenalty`. |
| `BoardTextureAnalyzer.cs` | S4.2: nuevo método `AnalyzeInitialBoard()`. |
| `FrmMain.cs` | S4.1 (guardar betSize), S4.2 (analizar flop base, combinar en turn), S4.3 (extraer villainStack). |

---

## S4.1 — Rastrear villain bet SIZE por street

### Problema

`PostflopGameContext` solo guarda booleans:
```csharp
public bool VillainBetFlop { get; set; }
public bool VillainBetTurn { get; set; }
```

El `BetSizeCategory` se calcula en cada street via `GetOpponentBetSize()` pero se descarta:
```csharp
// FrmMain.cs:1305 (flop)
var betSize = GetOpponentBetSize(maxBet, potSize);
// ...
_postflopContext.VillainBetFlop = maxBet > 0;  // Solo boolean, betSize se pierde
```

### Después — PostflopGameContext.cs

```csharp
public class PostflopGameContext
{
    // Estado cross-street existente
    public bool VillainBetFlop { get; set; }
    public bool VillainBetTurn { get; set; }
    public bool HeroBetFlop { get; set; }
    public bool HeroBetTurn { get; set; }
    public bool PreviousStreetWasBet { get; set; }
    public bool VillainAggressorCheckedFlop { get; set; }
    public BoardChangeResult LastBoardChange { get; set; } = BoardChangeResult.Safe;

    // NUEVO: tamaño de apuesta del villano por street
    public BetSizeCategory VillainBetSizeFlop { get; set; } = BetSizeCategory.NoBet;
    public BetSizeCategory VillainBetSizeTurn { get; set; } = BetSizeCategory.NoBet;

    // ... UpdateFlopState y UpdateTurnState actualizados:

    public void UpdateFlopState(bool heroBet, bool villainBet, bool isPreflopAggressor,
        BetSizeCategory villainBetSize = BetSizeCategory.NoBet)
    {
        HeroBetFlop = heroBet;
        VillainBetFlop = villainBet;
        VillainBetSizeFlop = villainBetSize;
        PreviousStreetWasBet = heroBet;
        VillainAggressorCheckedFlop = !isPreflopAggressor && !villainBet;
    }

    public void UpdateTurnState(bool heroBet, bool villainBet,
        BetSizeCategory villainBetSize = BetSizeCategory.NoBet)
    {
        HeroBetTurn = heroBet;
        VillainBetTurn = villainBet;
        VillainBetSizeTurn = villainBetSize;
        PreviousStreetWasBet = heroBet;
    }

    public void Reset()
    {
        // ... existente + nuevos campos:
        VillainBetSizeFlop = BetSizeCategory.NoBet;
        VillainBetSizeTurn = BetSizeCategory.NoBet;
    }
}
```

### Después — FrmMain.cs (flop, línea ~1368)

```csharp
// Antes:
_postflopContext.VillainBetFlop = maxBet > 0;

// Después — pasar betSize al contexto:
_postflopContext.VillainBetFlop = maxBet > 0;
_postflopContext.VillainBetSizeFlop = betSize;
```

### Después — FrmMain.cs (turn, línea ~1451)

```csharp
// Antes:
_postflopContext.VillainBetTurn = maxBet > 0;

// Después:
_postflopContext.VillainBetTurn = maxBet > 0;
_postflopContext.VillainBetSizeTurn = betSize;
```

### Después — PostflopDecisionService.cs (sizing tell detection)

```csharp
// Nuevos parámetros en DetermineAction:
BetSizeCategory villainBetSizeFlop = BetSizeCategory.NoBet,
BetSizeCategory villainBetSizeTurn = BetSizeCategory.NoBet

// Después de calcular facingBetPenalty y streetMultiplier:
// Sizing tell: villain escaló bet size entre streets → rango más fuerte
if (isFacingBet && street >= BoardPosition.Turn)
{
    bool villainEscalatedSizing =
        (street == BoardPosition.Turn && villainBetSizeFlop != BetSizeCategory.NoBet &&
         villainBetSize > villainBetSizeFlop) ||
        (street == BoardPosition.River && villainBetSizeTurn != BetSizeCategory.NoBet &&
         villainBetSize > villainBetSizeTurn);

    if (villainEscalatedSizing)
        adjustedFoldBelow += _profile.VillainSizingEscalationPenalty;
}
```

### Nuevo parámetro — StrategyProfile.cs

```csharp
// Sizing tell: penalización cuando villano escala tamaño de apuesta entre streets
public double VillainSizingEscalationPenalty { get; set; } = 4.0;
```

### Tabla de sizing tells

| Flop bet | Turn bet | Escaló? | Penalty |
|----------|----------|---------|---------|
| Small    | Medium   | Sí      | +4.0    |
| Small    | Large    | Sí      | +4.0    |
| Medium   | Large    | Sí      | +4.0    |
| Large    | Large    | No      | 0       |
| NoBet    | Medium   | No (no hay referencia) | 0 |
| Medium   | Small    | No (desescaló) | 0 |

---

## S4.2 — Propagar board danger desde flop

### Problema

El flop siempre pasa `BoardChangeResult.Safe`:
```csharp
// FrmMain.cs:1318
var boardChange = DecisionMaker.Algorithms.BoardChangeResult.Safe;
```

Los peligros iniciales del flop (2-tone, connected, paired) se pierden. `CombineBoardChanges()` en turn/river nunca recibe el estado base del flop.

### Nuevo método — BoardTextureAnalyzer.cs

```csharp
/// <summary>
/// Analiza el estado base de peligro del flop (no es un "cambio" sino un estado inicial).
/// Detecta flush draw presence (2-tone), straight draw presence, board paired.
/// </summary>
public BoardChangeResult AnalyzeInitialBoard(List<int> ranks, List<int> suits)
{
    if (ranks.Count < 3)
        return BoardChangeResult.Safe;

    var suitGroups = suits.GroupBy(s => s).ToDictionary(g => g.Key, g => g.Count());
    var rankGroups = ranks.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count());

    // 2+ cartas del mismo palo → flush draw presente desde flop
    bool flushDrawPresent = suitGroups.Values.Any(c => c >= 2);
    int flushDrawSuit = flushDrawPresent
        ? suitGroups.Where(g => g.Value >= 2).OrderByDescending(g => g.Value).First().Key
        : -1;

    // 3 cartas del mismo palo → flush ya posible (monotone)
    bool flushPossible = suitGroups.Values.Any(c => c >= 3);

    // Board paired desde flop
    bool boardPaired = rankGroups.Values.Any(c => c >= 2);

    // Straight draw presente
    bool straightDrawPresent = HasStraightDraw(ranks.Distinct().OrderBy(r => r).ToList());

    // Calcular danger level
    int dangerLevel = 0;
    if (flushPossible) dangerLevel += 3;
    else if (flushDrawPresent) dangerLevel += 1;
    if (straightDrawPresent) dangerLevel += 1;
    if (boardPaired) dangerLevel += 1;

    return new BoardChangeResult(
        FlushCompleted: false,  // En flop no se "completa" nada (es estado base)
        FlushDrawAppeared: flushDrawPresent,
        StraightCompleted: false,
        BoardPaired: boardPaired,
        OvercardAppeared: false,  // No aplica en flop (no hay carta previa)
        CompletedFlushSuit: flushDrawSuit,
        DangerLevel: dangerLevel);
}
```

**Nota:** Se reutiliza el método privado `HasStraightDraw()` que ya existe en `BoardTextureAnalyzer`.

### Nuevo campo — PostflopGameContext.cs

```csharp
/// <summary>
/// Estado base de peligro del flop (flush draw presence, paired, connected).
/// Se combina con boardChange del turn via CombineBoardChanges().
/// </summary>
public BoardChangeResult InitialBoardDanger { get; set; } = BoardChangeResult.Safe;
```

Reset:
```csharp
public void Reset()
{
    // ... existente + nuevo campo:
    InitialBoardDanger = BoardChangeResult.Safe;
}
```

### Después — FrmMain.cs (flop, línea ~1318)

```csharp
// Antes:
var boardChange = DecisionMaker.Algorithms.BoardChangeResult.Safe;

// Después — analizar flop como estado base:
var flopCards = _playerGameState.BoardCards
    .Where(b => b.Position == BoardPosition.Flop)
    .ToList();
var flopRanks = flopCards.Select(c => c.Force).ToList();
var flopSuits = flopCards.Select(c => c.Suit).ToList();
var boardChange = _boardTextureAnalyzer.AnalyzeInitialBoard(flopRanks, flopSuits);
_postflopContext.InitialBoardDanger = boardChange;
```

### Después — FrmMain.cs (turn, línea ~1394-1402)

```csharp
// Antes:
var boardChange = AnalyzeBoardChange(_playerGameState.BoardCards, 3);
// ... (se usa directamente, sin combinar con flop)
_postflopContext.LastBoardChange = boardChange;

// Después — combinar con peligro base del flop:
var turnChange = AnalyzeBoardChange(_playerGameState.BoardCards, 3);
var boardChange = PostflopGameContext.CombineBoardChanges(
    _postflopContext.InitialBoardDanger, turnChange);
_postflopContext.LastBoardChange = boardChange;
```

### Después — FrmMain.cs (river, línea ~993-995)

Sin cambios necesarios. El river ya combina `_postflopContext.LastBoardChange` con `riverChange`, y ahora `LastBoardChange` incluye el peligro acumulado desde flop.

### Flujo de propagación

```
Flop:  AnalyzeInitialBoard() → InitialBoardDanger = {FlushDrawAppeared:true, DangerLevel:1}
Turn:  AnalyzeBoardChange()  → turnChange = {OvercardAppeared:true, DangerLevel:1}
       CombineBoardChanges(InitialBoardDanger, turnChange) → LastBoardChange = {FlushDraw+Overcard, DangerLevel:2}
River: AnalyzeBoardChange()  → riverChange = {FlushCompleted:true, DangerLevel:4}
       CombineBoardChanges(LastBoardChange, riverChange) → finalChange = {FlushDraw+Overcard+FlushCompleted, DangerLevel:6}
```

---

## S4.3 — Extraer `villainStack` de Players

### Problema

Se pasa siempre `villainStack: 0` en 4 puntos de FrmMain.cs:
```csharp
// Líneas 1265, 1580, 1658, 1935
villainStack: 0,
```

En `UnifiedPokerCalculator.cs` (línea ~350), el effective stack nunca se calcula:
```csharp
if (heroStack > 0 && villainStack > 0)  // Siempre false cuando villainStack=0
{
    double spr = (double)(Math.Min(heroStack, villainStack) / currentPotSize);
    if (spr > 2.0) baseSize += 0.1;
}
```

### Después — FrmMain.cs (helper method)

```csharp
/// <summary>
/// Obtiene el stack del villano principal (oponente activo con mayor stack).
/// </summary>
private decimal GetVillainStack()
{
    var activeVillains = _playerGameState.Players
        .Where(p => p.Active && !string.IsNullOrEmpty(p.Name));
    return activeVillains.Any() ? activeVillains.Max(p => p.Stack) : 0;
}
```

**Nota:** Se usa `.Active` y `.Name` no vacío para filtrar. No hay `.IsHero` en `Player`, pero el hero se identifica por position/alias y no se incluye en `Players` (lista solo contiene oponentes). Si `Players` incluye al hero, agregar filtro por nombre/alias.

### Después — FrmMain.cs (4 puntos)

```csharp
// Líneas 1265, 1580, 1658, 1935:
villainStack: GetVillainStack(),
```

### Impacto en UnifiedPokerCalculator

Ya soporta villainStack > 0. Con el valor real:
- `effectiveStack = Math.Min(heroStack, villainStack)` → SPR más preciso
- Deep stack (SPR > 2.0) → baseSize += 0.1 (sizing más agresivo)
- El cambio habilita la lógica existente sin modificar UnifiedPokerCalculator

---

## Resumen de nuevos parámetros

| Parámetro | Ubicación | Valor | Propósito |
|-----------|-----------|-------|-----------|
| `VillainBetSizeFlop` | PostflopGameContext | NoBet (reset) | Tamaño bet villano en flop |
| `VillainBetSizeTurn` | PostflopGameContext | NoBet (reset) | Tamaño bet villano en turn |
| `InitialBoardDanger` | PostflopGameContext | Safe (reset) | Estado base de peligro del flop |
| `VillainSizingEscalationPenalty` | StrategyProfile | 4.0 | Penalización cuando villano escala sizing |
