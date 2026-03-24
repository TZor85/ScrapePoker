# Mejora Detección de Jugadores

## Why

El análisis del sistema de detección de jugadores reveló 6 problemas que afectan el conteo de oponentes, las posiciones y el tracking:

1. **Empty marca SitOut**: `SetEmptyPlayer()` hace `player.SitOut = true` cuando detecta asiento vacío. Un asiento vacío NO es sitout — son estados distintos. Contamina `numOpponents` y la asignación de posiciones.

2. **SitOut marca Empty**: `SetSitOutPlayer()` hace `player.Empty = true` cuando detecta "SIT". Un jugador en sitout tiene asiento ocupado y puede volver. Marcarlo como Empty impide que `SetDealerPlayer` lo considere correctamente.

3. **Color detection sin tolerancia**: `_colorPlaying = [17]` usa un único valor exacto del canal B. Sin tolerancia, cualquier variación de renderizado (antialiasing, transparencia, tema oscuro) causa falsos negativos.

4. **Sin detección de fold mid-hand**: Si un villano foldea durante la mano, sigue con `Active=true` hasta la siguiente mano. El `numOpponents` es incorrecto para turn/river.

5. **Empty/SitOut no se re-evalúa mid-session**: Solo `SetActivePlayer()` se llama en el else branch del game loop. Si un jugador se va o hace sitout durante una mano, su estado no se actualiza hasta la siguiente mano nueva.

6. **Sin validación cruzada de estado**: No hay fallback cuando la detección por color falla. Un jugador con Alias vacío + Stack=0 + Bet=0 probablemente es Empty, pero el sistema no lo deduce.

## What Changes

- `SetEmptyPlayer()`: Solo marca `Empty=true`, NO `SitOut`.
- `SetSitOutPlayer()`: Solo marca `SitOut=true`, NO `Empty`. Jugador sitout es un estado separado.
- `SetActivePlayer()`: Tolerancia ±5 en canal B para color detection.
- Nuevo `DetectFoldedPlayers()`: En cada street postflop, marcar `Active=false` a villanos que foldearon (bet=0 cuando debían actuar).
- Game loop else branch: Re-evaluar Empty y SitOut (no solo Active).
- Nuevo `ValidatePlayerStates()`: Validación cruzada — si alias vacío + stack=0 + bet=0 + no active → marcar Empty.

## Capabilities

### Modified Capabilities
- `empty-detection`: Solo marca Empty, no contamina SitOut.
- `sitout-detection`: Solo marca SitOut, no contamina Empty.
- `active-detection`: Tolerancia ±5 en canal B.
- `player-state-refresh`: Empty/SitOut re-evaluados en cada iteración.

### New Capabilities
- `fold-detection`: Detecta villanos que foldearon mid-hand.
- `cross-validation`: Inferir estado Empty cuando múltiples señales coinciden.

## Impact

- **`FrmMain.cs`** — SetEmptyPlayer, SetSitOutPlayer, SetActivePlayer, DetectFoldedPlayers, ValidatePlayerStates, game loop
- **`Player.cs`** — Nuevo campo `HasFolded` para tracking de fold mid-hand
- **Sin nuevas dependencias externas.**
