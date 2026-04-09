# Spec: Validar conteo real de cartas visibles antes de transicionar calle

## Descripcion

`ProcessPostFlopAsync` transiciona de calle basandose en `IsBoardCardVisible("Card4"/"Card5")` con conteo hardcodeado. Un falso positivo en una sola region causa transicion incorrecta.

## Ubicacion

`src/OpenScrape.App/Forms/FrmMain.cs`, lineas 1178-1207 (`ProcessPostFlopAsync`)

## Codigo Actual

```csharp
if (_gameLoopStateMachine.CurrentState == GameState.FlopAction)
{
    if (IsBoardCardVisible("Card4"))
        _gameLoopStateMachine.TryTransition(GameState.TurnDetected, 4); // 4 hardcodeado
    else
        // reprocess flop
}

if (_gameLoopStateMachine.CurrentState == GameState.TurnAction)
{
    if (IsBoardCardVisible("Card5"))
        _gameLoopStateMachine.TryTransition(GameState.RiverDetected, 5); // 5 hardcodeado
    else
        // reprocess turn
}
```

## Fix Propuesto

### 1. Nuevo metodo `CountVisibleBoardCards()`

```csharp
private int CountVisibleBoardCards()
{
    int count = 0;
    string[] cardRegions = { "Card1", "Card2", "Card3", "Card4", "Card5" };
    foreach (var region in cardRegions)
    {
        if (IsBoardCardVisible(region))
            count++;
        else
            break; // Las cartas son secuenciales: si Card3 no es visible, Card4/5 tampoco
    }
    return count;
}
```

### 2. Uso en ProcessPostFlopAsync

```csharp
if (_gameLoopStateMachine.CurrentState == GameState.FlopAction)
{
    if (IsBoardCardVisible("Card4"))
    {
        int visibleCards = CountVisibleBoardCards();
        if (_gameLoopStateMachine.TryTransition(GameState.TurnDetected, visibleCards))
            ; // Transicion exitosa, sera procesada abajo
        else
        {
            // Transicion bloqueada (cartas insuficientes) → reprocessar flop
            SetPotValue();
            var reprocessMaxBet = _playerGameState.Players.Max(m => m.Bet);
            _postflopContext.VillainBetSizeFlop = GetOpponentBetSize(reprocessMaxBet, _playerGameState.PotSize);
            _postflopContext.VillainBetFlop = reprocessMaxBet > 0;
            await ProcessFlopAsync(potOddsResult);
        }
    }
    else
    {
        SetPotValue();
        var reprocessMaxBet = _playerGameState.Players.Max(m => m.Bet);
        _postflopContext.VillainBetSizeFlop = GetOpponentBetSize(reprocessMaxBet, _playerGameState.PotSize);
        _postflopContext.VillainBetFlop = reprocessMaxBet > 0;
        await ProcessFlopAsync(potOddsResult);
    }
}
```

Misma logica para TurnAction → RiverDetected.

## Escenarios BDD

### Escenario 1: Turn card visible + 3 flop cards visibles → transicion correcta
```
Dado estado FlopAction
Y Card1, Card2, Card3, Card4 visibles (4 cartas)
Cuando se evalua transicion
Entonces CountVisibleBoardCards = 4
Y TryTransition(TurnDetected, 4) → exitosa
```

### Escenario 2: Card4 falso positivo, Card1-3 no visibles → transicion bloqueada
```
Dado estado FlopAction
Y solo Card4 "visible" (falso positivo), Card1-3 no visibles
Cuando se evalua transicion
Entonces CountVisibleBoardCards = 0 (break en Card1)
Y TryTransition(TurnDetected, 0) → bloqueada (0 < 4)
Y se reprocesa flop con bets actualizadas
```

### Escenario 3: Villain raise en flop, Card4 no visible → reprocess flop
```
Dado estado FlopAction
Y Card4 NO visible
Cuando se evalua transicion
Entonces no se intenta transicion
Y se reprocesa flop con bet size actualizado
```

### Escenario 4: River card visible + 4 cards previas → transicion correcta
```
Dado estado TurnAction
Y Card1-Card5 visibles (5 cartas)
Cuando se evalua transicion
Entonces CountVisibleBoardCards = 5
Y TryTransition(RiverDetected, 5) → exitosa
```
