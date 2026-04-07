# Tasks: Bugfixes Críticos del Motor de Equity y Decisiones

## BF1 — CalculateAllinEV: Fórmula Incorrecta

- [ ] Corregir fórmula en `PostflopDecisionService.CalculateAllinEV` (línea 1078)
  - Cambiar: `(equity / 100.0) * totalPotIfCalled - (1.0 - equity / 100.0) * stack`
  - Por: `(equity / 100.0) * (pot + stack) - (1.0 - equity / 100.0) * stack`
  - O equivalente: `(equity / 100.0) * totalPotIfCalled - stack`
- [ ] Actualizar comentario XML del método (línea 1069) con fórmula correcta
- [ ] Añadir 6 tests unitarios para CalculateAllinEV (refactorizar a `internal` + InternalsVisibleTo o extraer a método testeable)
  - Equity baja → EV negativo
  - Equity alta → EV positivo
  - Breakeven exacto → EV ≈ 0
  - Edge cases: stack=0, equity=0%, equity=100%
- [ ] Añadir 2 tests de integración: DetermineAction con push/fold donde fórmula vieja daba Push y nueva da Fold
- [ ] Build + 592+ tests pasan

## BF2 — TryDrawFromRange: Rejection Sampling Ineficiente

- [ ] Aumentar intentos máximos de 10 a 20 en `MonteCarloSimulator.TryDrawFromRange` (línea 540)
- [ ] Añadir contador de iteraciones efectivas en `RunMonteCarloSimulation` y `RunMonteCarloWithRange`
  - Variable local `int effectiveIterations` incrementada solo cuando TryDrawFromRange retorna true
  - `EquityResult.Simulations` = iteraciones efectivas (no intentadas)
- [ ] Añadir log de diagnóstico cuando tasa de rechazo > 5% (iteraciones perdidas / total)
- [ ] Añadir 3 tests unitarios
  - Combo disponible → retorna true
  - Todos bloqueados → retorna false
  - Alta tasa de bloqueo (80%) → éxito con reintentos
- [ ] Añadir 1 test de precisión: MC con rango estrecho en river vs exact enumeration, error < 1%
- [ ] Build + 592+ tests pasan

## BF3 — C-Bet Mixing: Rango Desbalanceado del Agresor

- [ ] Añadir bloque de mixing en `DetermineAction`, después de push/fold y antes de `HandleNoBet`
  - Condición: `!isFacingBet && heroIsAggressor && !isMultiway`
  - Rango: `effectiveEquity >= adjustedFoldBelow && effectiveEquity < adjustedThinValueAbove`
  - Acción: con probabilidad `(1 - cbetFreq)`, retornar Check en vez de pasar a HandleNoBet
- [ ] Asegurar que slow play, check-raise, float exit, y delayed value tienen prioridad (están antes en el flujo)
- [ ] Añadir 5 tests unitarios
  - Agresor con equity media → check a frecuencia esperada (seed fijo o mock Random)
  - No agresor → sin mixing
  - Multiway → sin mixing
  - Equity alta → sin mixing (siempre bet)
  - Turn/River → frecuencia menor
- [ ] Verificar que tests existentes de PostflopDecision no rompan (el mixing añade Check donde antes era Bet)
- [ ] Build + 592+ tests pasan

## Verificación Final

- [ ] Ejecutar suite completa: `dotnet test OpenScrape.sln`
- [ ] Verificar que los 3 fixes no interfieren entre sí (BF1 cambia all-in threshold, BF3 cambia frecuencia de bet)
- [ ] Smoke test manual: revisar logs de decisión en escenarios push/fold y c-bet
