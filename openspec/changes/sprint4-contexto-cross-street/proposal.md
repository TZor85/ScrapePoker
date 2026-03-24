# Sprint 4 — Contexto Cross-Street

## Why

El motor de decisión pierde información valiosa entre streets porque el contexto cross-street es incompleto:

1. **Villain bet size no se rastrea**: `PostflopGameContext` solo guarda booleans (`VillainBetFlop: bool`, `VillainBetTurn: bool`). No registra el tamaño (`BetSizeCategory`). El sistema no puede detectar "villain escaló sizing de Small a Large" (sizing tell fuerte) vs "villain siempre bet small" (rango débil). Esta información es clave para ajustar facing bet penalties en calles posteriores.

2. **Peligros del flop no se propagan**: El flop siempre pasa `BoardChangeResult.Safe` al motor de decisión. Los peligros iniciales del flop (2-tone board, board connected, board paired) se pierden y no se acumulan en turn/river. `CombineBoardChanges()` ya existe y combina turn+river, pero nunca recibe el estado base del flop. Resultado: boards como AhQh3d (flush draw desde flop) no generan penalización acumulada en turn.

3. **Villain stack siempre es 0**: Las 4 llamadas a `Calculate()` en `FrmMain.cs` pasan `villainStack: 0`. El cálculo de SPR en `UnifiedPokerCalculator` usa `Math.Min(heroStack, villainStack)`, que siempre da 0 cuando villainStack=0. Resultado: el effective stack nunca se considera, y decisiones SPR-dependientes (deep stack adjustments, bet sizing) son subóptimas.

## What Changes

Se implementan 3 mejoras de contexto cross-street:

- `PostflopGameContext` almacena `BetSizeCategory` por street además del boolean existente. `PostflopDecisionService` detecta sizing tells (villain escaló bet size entre streets) y ajusta facing bet penalties.
- El flop se analiza como "estado base de peligro" usando un nuevo método `AnalyzeInitialBoard()`. Este estado se propaga a turn/river via `CombineBoardChanges()` (ya existente).
- El villain stack se extrae de `_playerGameState.Players` y se pasa a `UnifiedPokerCalculator.Calculate()`, habilitando los cálculos SPR con effective stack real.

## Capabilities

### Modified Capabilities

- `cross-street-context`: `PostflopGameContext` almacena `VillainBetSizeFlop` y `VillainBetSizeTurn` como `BetSizeCategory`.
- `villain-sizing-tell`: `PostflopDecisionService` detecta escalado de sizing entre streets y aplica penalización adicional.
- `board-danger-propagation`: El flop genera un `BoardChangeResult` base que se propaga a turn/river via `CombineBoardChanges()`.
- `effective-stack-spr`: `villainStack` real se pasa a `UnifiedPokerCalculator.Calculate()`, habilitando SPR con effective stack.

### New Capabilities

- `initial-board-analysis`: `BoardTextureAnalyzer.AnalyzeInitialBoard()` genera un `BoardChangeResult` para el estado base del flop (flush draw presence, connectivity, paired).

## Impact

- **`PostflopGameContext.cs`**: S4.1 — Nuevos campos `VillainBetSizeFlop`, `VillainBetSizeTurn`. Reset ampliado.
- **`PostflopDecisionService.cs`**: S4.1 — Nuevos parámetros opcionales para sizing tell. Penalización por escalado de sizing.
- **`StrategyProfile.cs`**: S4.1 — Nuevo parámetro `VillainSizingEscalationPenalty`.
- **`BoardTextureAnalyzer.cs`**: S4.2 — Nuevo método `AnalyzeInitialBoard()`.
- **`FrmMain.cs`**: S4.1 (guardar betSize en contexto), S4.2 (analizar flop como base danger, combinar en turn), S4.3 (extraer villainStack de Players).
- **`UnifiedPokerCalculator.cs`**: S4.3 — Ya soporta villainStack, solo falta pasar valor real.
- **Sin nuevas dependencias externas.**
