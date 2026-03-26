## Why

Los Sprints 14-16 añadieron capacidades que no estaban conectadas al game loop real de FrmMain:
1. `TryTransition(state, boardCards)` existía pero no se llamaba → street validation inactiva.
2. `GetTypeForPosition()` existía pero `GetVillainType()` usaba AF global → AF posicional no se aprovechaba.
3. Contadores IP/OOP en OpponentProfile existían pero `RecordPostflopAction` no los trackeaba.
4. `IsAnyoneAllIn` existía pero nunca se seteaba en el game loop.

## What Changes

1. **Street validation activa** — Las 3 transiciones principales (Flop/Turn/River Detected) ahora pasan el nº de board cards esperado.
2. **Villain type posicional** — `GetVillainType(inPosition)` usa `GetTypeForPosition(!heroIP)` con fallback.
3. **Tracking IP/OOP** — `TrackVillainPostflopAction` pasa `heroIsInPosition`, `RecordPostflopAction` trackea contadores posicionales.
4. **All-in detection** — Detecta `villainStack <= 0` después de bet en flop/turn/river y setea el flag. Pasa `isAnyoneAllIn` a DetermineAction.

## Impact

- **`src/OpenScrape.App/Forms/FrmMain.cs`**: 12 líneas modificadas (transitions, GetVillainType, tracking, all-in detection, DetermineAction calls).
- **`src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs`**: RecordPostflopAction acepta isVillainInPosition opcional.
