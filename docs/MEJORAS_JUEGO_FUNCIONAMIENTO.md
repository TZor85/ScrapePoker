# Mejoras de juego y funcionamiento

## Prioridad 1 - RNG reproducible

### Problema

El motor usa `Random.Shared` directamente en `PostflopDecisionService` y `MonteCarloSimulator`, y `new Random()` en rutas preflop. Eso hace que dos ejecuciones con los mismos inputs puedan producir acciones distintas sin trazabilidad.

### Impacto

- Tests probabilísticos pueden fallar de forma intermitente.
- No se puede reproducir una decisión histórica exacta.
- Backtest y coaching pierden precisión cuando una rama depende de mezcla aleatoria.
- Es difícil explicar una decisión si no se conserva el valor aleatorio usado.

### Implementación por fases

1. Crear `IRandomProvider` y `SystemRandomProvider`.
2. Inyectar RNG en `PostflopDecisionService`.
3. Actualizar tests de mezcla para usar RNG determinista.
4. Extender el patrón a `GetActionScenario`.
5. Extender el patrón a `MonteCarloSimulator` y guardar seed por mano/sesión.

### Estado

- Fases 1-3 implementadas: el motor postflop deja de depender de `Random.Shared` directamente y los tests pueden forzar ramas probabilísticas de forma determinista.
- Fase 4 implementada: `GetActionScenario` usa un proveedor de RNG inyectable para seleccionar acciones por porcentaje de forma reproducible en tests.
- Fase 5 implementada: `MonteCarloSimulator` acepta seed por mano o sesión, deriva RNG por iteración y guarda la seed usada en el resultado.

## Siguientes mejoras recomendadas

1. Persistir `OpponentProfile` por alias/GUID.
2. Añadir watchdog de mano atascada al `GameLoopCoordinator`.
3. Guardar `DecisionTrace` por decisión.
4. Hacer configurables rutas de recursos y filtro de ventana.
5. Persistir artefactos de debug OCR.
