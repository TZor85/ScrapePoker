## Why

El cálculo de distancia en `DetermineP0Position` está invertido. La fórmula actual usa `(heroIndex - dealerIndex)` pero debería ser `(dealerIndex - heroIndex)`. Esto causa que con dealer=P3 y 3 jugadores, la distancia se calcule como 1 (SB) en vez de 2 (BB), dando una posición incorrecta.

## What Changes

- Corregir la fórmula de distancia en `PositionCalculator.DetermineP0Position` línea 30
- Cambiar `(heroIndex - dealerIndex)` a `(dealerIndex - heroIndex)`

## Capabilities

### New Capabilities

No se introducen.

### Modified Capabilities

- `hero-position-detection`: Corrección del cálculo de posición de blinds

## Impact

- Código afectado: `src/OpenScrape.App/Services/PositionCalculator.cs`
- Solo 1 línea cambiada