# Integración OpponentTracker + Optimización Monte Carlo — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `FrmMain.cs` | Inyectar OpponentTracker, capturar acciones villano, pasar villainType real |
| `UnifiedPokerCalculator.cs` | Cache preflop MC, iteraciones adaptativas, tolerancia cache |
| `MonteCarloSimulator.cs` | Sin cambios (ya soporta `iterations` variable) |

---

## Parte 1: Integración OpponentTracker

### 1.1 — Inyectar OpponentTracker en FrmMain

`OpponentTracker` ya está registrado como Singleton en `Program.cs`. Solo falta inyectar en `FrmMain`:

```csharp
// FrmMain — constructor
private readonly OpponentTracker _opponentTracker;

public FrmMain(..., OpponentTracker opponentTracker, ...)
{
    _opponentTracker = opponentTracker;
    // ...
}
```

### 1.2 — Identificar al villano activo

En el game loop, el villano que apuesta se identifica por `_playerGameState.Players.Where(p => p.Active && p.Bet > 0)`. Para el tracker necesitamos un identificador consistente:

```csharp
private string GetActiveVillainId()
{
    var villain = _playerGameState.Players
        .Where(p => p.Active && !string.IsNullOrEmpty(p.Name))
        .OrderByDescending(p => p.Bet)
        .FirstOrDefault();
    return villain?.Name ?? "Unknown";
}
```

### 1.3 — Puntos de captura en el game loop

#### Inicio de mano (`DetectNewHand`)
```csharp
// Registrar que cada villano activo jugó una mano
foreach (var player in _playerGameState.Players.Where(p => p.Active && !string.IsNullOrEmpty(p.Name)))
{
    _opponentTracker.RecordHandPlayed(player.Name);
}
```

#### Preflop (después de detectar situación)
```csharp
// Si villain hizo raise preflop → RecordVPIP + RecordPFR
var raisers = _playerGameState.Players.Where(p => p.Active && p.Bet > _playerGameState.BigBlind);
foreach (var raiser in raisers)
{
    _opponentTracker.RecordVPIP(raiser.Name);
    if (raiser.Bet > _playerGameState.BigBlind * 2)
        _opponentTracker.RecordPFR(raiser.Name);
}
```

#### Flop (`DetermineFlopActionUnified`)
```csharp
// Después de detectar bet del villano
var villainId = GetActiveVillainId();
if (maxBet > 0)
{
    _opponentTracker.RecordPostflopAction(villainId, PostflopAction.Bet);
    // Si villain fue agresor preflop y apuesta flop → c-bet
    if (PreflopAnalyzer.IsPreflopAggressor(effectiveSituation))
        _opponentTracker.RecordCBetOpportunity(villainId, didCBet: true);
}
else if (PreflopAnalyzer.IsPreflopAggressor(effectiveSituation))
{
    // Agresor checkeó → missed c-bet
    _opponentTracker.RecordCBetOpportunity(villainId, didCBet: false);
}
```

#### Turn y River (similar al flop)
```csharp
if (maxBet > 0)
    _opponentTracker.RecordPostflopAction(villainId, PostflopAction.Bet);
```

### 1.4 — Pasar villainType a DetermineAction

```csharp
// En DetermineFlopActionUnified, ProcessTurnAsync, ProcessRiverAsync:
var villainId = GetActiveVillainId();
var villainProfile = _opponentTracker.GetProfile(villainId);
var villainType = villainProfile?.IsReliable == true
    ? villainProfile.Type
    : OpponentType.Unknown;

var decision = _postflopDecisionService.DetermineAction(
    // ... todos los parámetros existentes ...
    villainType: villainType);
```

### 1.5 — Fold equity ajustada por oponente

`OpponentTracker.GetAdjustedFoldEquity()` ya ajusta fold equity por tipo:
- LAG ×0.70, TAG ×0.85, TP ×1.10, LP ×1.25

```csharp
// En FrmMain, antes de pasar foldEquity:
double baseFoldEquity = _streetResult.FoldEquity;
double adjustedFE = _opponentTracker.GetAdjustedFoldEquity(villainId, baseFoldEquity);

// Pasar a DetermineAction:
foldEquity: adjustedFE
```

---

## Parte 2: Optimización Monte Carlo

### 2.1 — Iteraciones adaptativas

En `UnifiedPokerCalculator.CalculateEquity()`, calcular iteraciones basándose en una estimación rápida (lookup table para preflop, o equity previa para postflop):

```csharp
private int GetAdaptiveIterations(List<CardDataOuts> playerHand, List<CardDataOuts> communityCards,
    int numOpponents, string? handSituation)
{
    // Si hay equity previa cacheada (±cualquier tolerancia), usar para decidir iteraciones
    string cacheKey = BuildEquityCacheKey(playerHand, communityCards, numOpponents, handSituation);
    if (_equityCache.TryGetValue(cacheKey, out double cachedEquity))
    {
        // Decisión clara → menos iteraciones
        if (cachedEquity > 75 || cachedEquity < 25)
            return 500;
        if (cachedEquity > 65 || cachedEquity < 35)
            return 750;
    }

    // Preflop: lookup table da estimación rápida
    if (communityCards.Count == 0)
    {
        double roughEquity = _preflopEquityCalculator.GetEquity(playerHand, numOpponents) * 100;
        if (roughEquity > 75 || roughEquity < 25)
            return 500;
    }

    return PokerConstants.DefaultMonteCarloIterations; // 1000
}
```

### 2.2 — Cache preflop MC con VillainRange

Actualmente, cuando `VillainRange` existe para preflop (3Bet/4Bet), siempre ejecuta MC sin cache:

```csharp
// Actual (UnifiedPokerCalculator.cs):
if (preflopRange != null)
{
    var mcResult = _monteCarloSimulator.CalculateEquity(
        playerHand, new List<CardDataOuts>(), numOpponents,
        monteCarloIterations, preflopRange);
    return mcResult.Equity * 100;
}
```

Fix: cachear resultado por (hand sorted, situation, numOpponents):

```csharp
if (preflopRange != null)
{
    string preflopCacheKey = $"preflop|{hand}|{handSituation}|{numOpponents}";
    if (_equityCache.TryGetValue(preflopCacheKey, out double cachedPreflopEquity))
        return cachedPreflopEquity;

    int iterations = GetAdaptiveIterations(playerHand, communityCards, numOpponents, handSituation);
    var mcResult = _monteCarloSimulator.CalculateEquity(
        playerHand, new List<CardDataOuts>(), numOpponents,
        iterations, preflopRange);
    double equity = mcResult.Equity * 100;

    if (_equityCache.Count >= EquityCacheMaxSize)
        _equityCache.Clear();
    _equityCache[preflopCacheKey] = equity;

    return equity;
}
```

### 2.3 — Aumentar tamaño de cache

```csharp
// Actual:
private const int EquityCacheMaxSize = 256;

// Nuevo:
private const int EquityCacheMaxSize = 512;
```

256 entries es muy conservador. En una sesión de 200+ manos, el cache se llena y limpia repetidamente. 512 reduce limpiezas un 50%.

---

## Resumen de cambios

| Componente | Cambio | Esfuerzo |
|-----------|--------|----------|
| FrmMain constructor | Inyectar OpponentTracker | Bajo |
| FrmMain helper | GetActiveVillainId() | Bajo |
| FrmMain DetectNewHand | RecordHandPlayed para cada villano activo | Bajo |
| FrmMain preflop | RecordVPIP/RecordPFR | Medio |
| FrmMain flop/turn/river | RecordPostflopAction + RecordCBetOpportunity | Medio |
| FrmMain DetermineAction calls (×3) | Pasar villainType real + adjustedFoldEquity | Bajo |
| UnifiedPokerCalculator | GetAdaptiveIterations + cache preflop MC + cache 512 | Medio |
