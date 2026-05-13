## Why

El log muestra `Players.Count=1` aunque hay bets en P5 y P0. P5 tiene bet=1 pero no se detecta como jugador activo, causando que las blinds se asignen incorrectamente (P1 SB en vez de P5 SB).

## What Changes

- Investigar por qué P5 no se detecta en la lista de jugadores
- Agregar logging en SetEmptyPlayer y SetActivePlayer para debug
- Corregir la detección de jugadores activos

## Capabilities

### Modified Capabilities

- `player-detection`: Investigar y corregir detección de P5

## Impact

- Código afectado: TableLayoutService.cs