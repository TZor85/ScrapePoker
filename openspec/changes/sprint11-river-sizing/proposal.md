# Sprint 11 — River Sizing y Contexto Posicional

## Why

5 leaks identificados tras análisis de mano real con TPTK en board con flush draw:

1. **River bet sizing ignora board danger**: Bet 3/4 con TPTK cuando 3 corazones en mesa arriesga demasiado. Sizing menor (1/3) extrae valor sin overcommit.
2. **VillainRange no ajusta por posición del villain**: EP open tiene rango más estrecho que BTN open. La equity preflop y postflop no lo refleja.
3. **Sin sizing diferenciado value vs bluff river**: GTO dicta big bet = polarizado, small bet = merged. TPTK es merged → sizing menor.
4. **Turn call sin plan de river**: Cuando hero call turn, no hay lógica para "si cae 4º corazón → fold river".

## What Changes

- `PostflopDecisionService.HandleNoBet()`: River value bet sizing reducido cuando board tiene flush/straight draw sin blocker.
- `VillainRange.GetForSituation()`: Nuevo overload con `TablePosition villainPosition` para ajustar rango.
- `PostflopDecisionService.HandleNoBet()`: River sizing diferenciado: TPTK → sizing menor (merged), TwoPair+ → sizing mayor (polarizado).
- `PostflopDecisionService`: Nuevo flag `turnCalledWithDanger` → river auto-fold si draw completa.

## Impact

- **`PostflopDecisionService.cs`**: S11.1, S11.3, S11.4.
- **`VillainRange.cs`**: S11.2.
- **`FrmMain.cs`**: S11.2 (propagar villain position), S11.4 (flag turn danger).
- **`PostflopGameContext.cs`**: S11.4 (nuevo campo).
