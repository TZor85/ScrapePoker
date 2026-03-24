# Bugfix — Sesión en vivo (6 bugs)

## Why

Análisis de logs de partida real (~26 manos, 22 minutos) reveló 6 bugs que afectan decisiones en juego:

1. **ThreeOfAKind con equity MC 42.9%** — Hero tiene trips con kicker A en board 3d5s5c5d pero Monte Carlo reporta 42.9%. Contra rango random, el MC genera villanos con trips/full house de forma desproporcionada. La equity real contra un rango de limp debería ser 75-85%.

2. **HasRangeAdvantageOnBoard true para T-5-2 en 3Bet** — Board bajo-desconectado donde el caller tiene ventaja de rango, pero el método retorna `true` porque `isLowBoard = false` (T=10 > 9). La lógica solo chequea `isLowBoard && isConnected` pero no captura boards mixtos (una carta alta + dos bajas).

3. **Pot size = 0 al re-procesar turn** — Cuando `ProcessPostFlopAsync` re-procesa el turn sin nueva carta (villain raise), no llama a `SetPotValue()` antes, usando el pot anterior o 0. Esto genera SPR=0 y decisiones incorrectas.

4. **numOpponents sin Math.Max en turn/river** — Flop usa `Math.Max(1, count - 1)` pero turn y river usan `count - 1` directo, que puede dar 0 o negativo si Players está vacío.

5. **IsSimplified ignora board texture** — `DetermineSimplifiedAction` (RaiseOverLimper) no recibe `boardTexture` ni adapta sizing. KK en board monotone 3c5c7c usa "Bet 1/2" fijo en vez del sizing menor apropiado para monotone.

6. **C-bet adjustment +12 inflado** — `CbetRangeAdvantageBonus(8) + CbetAggressorBonus(4) = +12` se aplica cuando `HasRangeAdvantageOnBoard` retorna true incorrectamente (bug 2). Fix del bug 2 corrige esto también.

## What Changes

- `PreflopAnalyzer.HasRangeAdvantageOnBoard()`: Mejorar detección para boards mixtos (carta alta + cartas bajas).
- `FrmMain.ProcessPostFlopAsync()`: Llamar a `SetPotValue()` antes de re-procesar streets.
- `FrmMain.ProcessTurnAsync/RiverAsync`: Agregar `Math.Max(1, ...)` a numOpponents.
- `PostflopDecisionService.DetermineSimplifiedAction()`: Recibir `boardTexture` y adaptar sizing.

## Capabilities

### Modified Capabilities
- `range-advantage`: Detección mejorada para boards mixtos en 3Bet pots.
- `pot-size-reprocess`: Pot actualizado antes de re-procesar streets.
- `simplified-decision`: Ahora considera board texture para sizing.

## Impact

- **`PreflopAnalyzer.cs`** — Bug 2: range advantage
- **`FrmMain.cs`** — Bug 3: pot reprocess, Bug 4: numOpponents
- **`PostflopDecisionService.cs`** — Bug 5: simplified board texture
- **Sin nuevas dependencias externas.**
