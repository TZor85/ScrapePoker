## Why

El `MonteCarloSimulator` actualmente trackea las simulaciones saltadas (`SkippedSimulations`) pero no tiene un límite máximo. Cuando el rango del villano bloquea más del 20% de las combinaciones posibles, las estimaciones de equity se vuelven poco fiables, pero el sistema continúa sin marcar esta condición ni intentar resamplear con parámetros diferentes.

## What Changes

- Añadir límite máximo de skipped simulations en `MonteCarloSimulator`
- Implementar validación de fiabilidad de equity basada en porcentaje de combos bloqueados
- Marcar equity como "unreliable" cuando el bloqueo >20% y reintentar con resampleo

## Capabilities

### New Capabilities

- `equity-reliability-tracker`: Capacidad de validar y marcar la fiabilidad de cálculos de equity basada en el porcentaje de combos bloqueados por el rango del villano

### Modified Capabilities

- Ninguna existente con cambios de requisitos

## Impact

- `MonteCarloSimulator` en `OpenScrape.DecisionMaker`
- Servicios de cálculo de equity
- Lógica de decisión de poker que consume valores de equity