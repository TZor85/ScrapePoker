## Why

Cuando faltan jugadores en la mesa (algunos asientos están vacíos o en sit-out), el método `PositionCalculator.DetermineP0Position` retorna `TablePosition.None` porque excluye los asientos vacíos de los cálculos. Esto causa que no se pueda determinar la posición del Hero, afectando la toma de decisiones preflop y postflop.

## What Changes

- Modificar `PositionCalculator.DetermineP0Position` para manejar correctamente mesas con asientos vacíos
- Incluir el asiento del Hero (P0) siempre en el cálculo de posición aunque esté marcado como Empty
- Ajustar la lógica para calcular la distancia desde el dealer considerando solo los asientos activos entre el dealer y el Hero

## Capabilities

### New Capabilities

No se introducen nuevas capabilities.

### Modified Capabilities

- `hero-position-detection`: Corrección del cálculo de posición cuando faltan jugadores en la mesa

## Impact

- Código afectado: `src/OpenScrape.App/Services/PositionCalculator.cs`
- Sin cambios en APIs externas
- Afecta positivamente la detección de posición en mesas con 2-5 jugadores activos