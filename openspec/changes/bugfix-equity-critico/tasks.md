# Tasks: Bugfixes Críticos del Motor de Equity y Decisiones

## BF1 — CalculateAllinEV: Fórmula Incorrecta ✅

- [x] Corregir fórmula en `PostflopDecisionService.CalculateAllinEV` (línea 1078)
  - `equityFraction * (pot + stack) - (1.0 - equityFraction) * stack`
- [x] Actualizar comentario XML del método con fórmula correcta
- [x] Método cambiado a `internal static` + InternalsVisibleTo en .csproj
- [x] 8 tests unitarios para CalculateAllinEV
  - Equity baja → EV negativo (-2.0)
  - Equity alta → EV positivo (+112.5)
  - Breakeven exacto → EV ≈ 0
  - Edge cases: stack=0, pot=0, equity=0%, equity=100%
  - Verificación de no sobreestimación vs fórmula vieja
- [x] 9 tests existentes de push/fold siguen pasando
- [x] Build 0 errores, 600 tests pasan

## BF2 — TryDrawFromRange: Rejection Sampling Ineficiente ✅

- [x] Aumentar intentos máximos de 10 a 20 en `MonteCarloSimulator.TryDrawFromRange`
- [x] `EquityResult.Simulations` ya reportaba iteraciones efectivas (existente)
- [x] Añadir `EquityResult.SkippedSimulations` para diagnóstico por el caller
- [x] 19 tests MC existentes pasan
- [x] Build 0 errores, 600 tests pasan
- Nota: tracking de skipped ya existía en `RunMonteCarloSimulation` (totalSkipped, effectiveCount)

## BF3 — C-Bet Mixing: Rango Desbalanceado del Agresor ✅

- [x] Bloque de mixing añadido en `DetermineAction` antes de `HandleNoBet`
  - Condición: `heroIsAggressor && !isMultiway`
  - Rango: `effectiveEquity >= adjustedFoldBelow && effectiveEquity < adjustedThinValueAbove`
  - Con probabilidad `(1 - cbetFreq)`, retorna Check para proteger checking range
- [x] Prioridades respetadas: slow play, check-raise, float exit, delayed value van antes (en HandleNoBet)
- [x] 600 tests existentes pasan sin romper
- [x] Build 0 errores

## Verificación Final ✅

- [x] Suite completa: 600/600 tests pasan
- [x] Los 3 fixes no interfieren entre sí (BF1 afecta isPushFold, BF3 afecta rango medio no-facing-bet)
- [ ] Smoke test manual: revisar logs de decisión en escenarios push/fold y c-bet
