# Tasks: Refinamiento del Motor de Decisiones (L1-L6)

## L1: Backdoor Outs Calibrados

- [ ] Añadir constantes `BackdoorFlushOuts = 1.5` y `BackdoorStraightOuts = 1.0` a `PokerConstants.cs`
- [ ] Cambiar tipo retorno de `CalculateBackdoorOuts()` de `int` a `double` en `OutsCalculator.cs`
- [ ] Cambiar variable local `backdoorOuts` de `int` a `double` en `Calculate()` (linea 119)
- [ ] Reemplazar `backdoorOuts += 1` por `backdoorOuts += PokerConstants.BackdoorFlushOuts` (linea 207)
- [ ] Reemplazar `backdoorOuts += 1` por `backdoorOuts += PokerConstants.BackdoorStraightOuts` (linea 249)
- [ ] Asegurar que `TotalOuts` final se redondea con `(int)Math.Round()` al sumar backdoorOuts
- [ ] Escribir 8 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan

## L2: Flush Draw Penalty con Blocker

- [ ] Añadir `DangerFlushDrawNutBlockerReduction = 0.50` y `DangerFlushDrawNonNutBlockerReduction = 0.70` a `StrategyProfile.cs`
- [ ] Añadir mismos parametros a `appsettings.json`
- [ ] En `DangerPenaltyCalculator.Calculate()` lineas 61-71: añadir bloque blocker despues de hand rank reduction
- [ ] Verificar que flush completed blocker (lineas 81-93) no se altera
- [ ] Escribir 6 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan

## L3: Danger Penalty Skip Hero Improvement

- [ ] Añadir parametros `heroCompletedFlush` y `heroCompletedStraight` a firma de `DangerPenaltyCalculator.Calculate()`
- [ ] Actualizar wrapper `CalculateDangerPenalty` en `PostflopDecisionService` con nuevos parametros
- [ ] Modificar calculo de `flushCompletePenalty`: skip si `heroCompletedFlush`
- [ ] Modificar calculo de `straightCompletePenalty`: skip si `heroCompletedStraight`
- [ ] En call site de `DetermineAction`: pasar `heroHandRank >= HandRank.Flush` y `heroHandRank >= HandRank.Straight && heroHandRank < HandRank.Flush`
- [ ] Escribir 8 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan

## L4: AF Laplace Smoothing

- [ ] Reemplazar `AggressionFactor` getter con formula `(aggressive + 1) / (passive + 1)` en `OpponentProfile.cs`
- [ ] Reemplazar `AggressionFactorIP` getter (mantener guard `< 5 → -1`)
- [ ] Reemplazar `AggressionFactorOOP` getter (mantener guard `< 5 → -1`)
- [ ] Verificar que `GetTypeForPosition()` no necesita cambios (threshold 1.5 se mantiene)
- [ ] Escribir 9 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan (actualizar valores esperados si aplica)

## L5: Combo Draw Bonus por Textura

- [ ] Añadir 5 parametros `ComboDrawTexture{Dry/SemiDry/SemiWet/Wet/Monotone}` a `StrategyProfile.cs`
- [ ] Añadir mismos parametros a `appsettings.json`
- [ ] En `PostflopDecisionService.DetermineAction()`: reordenar calculo de `simplifiedTexture` ANTES del combo draw bonus
- [ ] Modificar bloque combo draw (linea 162-165): multiplicar bonus por factor de textura via switch
- [ ] Escribir 8 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan

## L6: Multiway OOP Factor Configurable

- [ ] Añadir `MultiwayOOPQuadraticDamping = 0.5` a `StrategyProfile.cs`
- [ ] Añadir parametro a `appsettings.json`
- [ ] Reemplazar `* 0.5` hardcodeado (lineas 260-261) por `* _profile.MultiwayOOPQuadraticDamping`
- [ ] Verificar que IP no se ve afectado
- [ ] Escribir 7 tests unitarios (ver spec)
- [ ] Verificar build y tests existentes pasan

---

## Orden de Implementacion Recomendado

1. **L4** (AF cliff) — cambio aislado en Domain, sin dependencias
2. **L1** (backdoor outs) — cambio aislado en Algorithms, sin dependencias
3. **L6** (multiway OOP) — cambio simple: extraer constante a parametro
4. **L2 + L3** (danger penalties) — mismo archivo, implementar juntos
5. **L5** (combo draw texture) — requiere reordenamiento de logica en DetermineAction

## Verificacion Final

- [ ] `dotnet build OpenScrape.sln`
- [ ] `dotnet test OpenScrape.sln` — todos los tests pasan
- [ ] `dotnet format --verify-no-changes OpenScrape.sln`
- [ ] Backtest A/B con StrategyBacktester comparando antes/despues
