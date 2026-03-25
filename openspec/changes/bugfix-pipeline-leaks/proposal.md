# Bugfix Pipeline Leaks — Corrección de leaks en el pipeline de decisión

## Why

Análisis del pipeline completo (OCR → estado → decisión) revela 6 leaks prácticos que causan decisiones incorrectas:

1. **Preflop equity log inflada**: `numOpp` incluye hero → equity mostrada 5-10% más alta de lo real.
2. **DetectFoldedPlayers solo postflop**: Folds preflop no detectados por color si hay delay → numOpponents puede estar inflado en primera iteración de flop.
3. **VillainBetSize stale en reprocess**: Al reprocesar flop/turn por raise, el betSize del contexto no se actualiza.
4. **IP/OOP solo vs dealer**: No considera posición relativa al bettor actual. En multiway, hero puede estar IP vs dealer pero OOP vs el bettor.
5. **Preflop sin ajuste por TablePosition**: La equity preflop mostrada no varía por EP/MP/CO/BTN. Falta bonus posicional.
6. **Kicker no influye en thin value**: Hero con TPTK (Ace kicker) debería thin-value bet más que TPWK (weak kicker). KickerStrength existe pero no se pasa a PostflopDecisionService.

Nota: Squeeze detection (item 6 original) YA existe en SetPreflopActionUseCase.cs:110.

## What Changes

- `FrmMain.cs:2085`: Fix `-1` en numOpp preflop.
- `FrmMain.cs:DetectFoldedPlayers()`: Eliminar guard de street, permitir en preflop.
- `FrmMain.cs:ProcessPostFlopAsync()`: Actualizar `_postflopContext.VillainBetSizeFlop/Turn` en reprocess.
- `FrmMain.cs:SetIsInPosition()`: Considerar posición del bettor si hay bet activa.
- `FrmMain.cs`: Agregar ajuste posicional a equity preflop display.
- `PostflopDecisionService.cs`: Nuevo parámetro `heroKickerStrength`, influye en thin value y value bet sizing.

## Impact

- **`src/OpenScrape.App/Forms/FrmMain.cs`**: BF1-BF5.
- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: BF7.
- **`src/OpenScrape.Domain/Enums/OutsDataEnum.cs`**: Ya tiene KickerStrength.
- **Tests**: +10-15 tests nuevos.
