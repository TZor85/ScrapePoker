## Why

El motor de decisión postflop tiene 5 bugs confirmados que impactan directamente en la rentabilidad:
1. El bot nunca bluffea en boards coordinated IP (condición imposible de cumplir).
2. La penalización por peligro se duplica cuando flush Y straight completan simultáneamente, causando folds de manos ganadoras.
3. El bonus de combo draw se aplica cuando el draw ya se completó, distorsionando el sizing con nuts.
4. El probe bet en river no funciona porque falta un parámetro en la llamada.
5. Varios parámetros de calibración son subóptimos, causando calls marginales excesivos y slow plays inexistentes.

Estos bugs combinados reducen significativamente el Expected Value en situaciones comunes de juego real.

## What Changes

- Se corrige `ShouldBluff()` para que `IPCoordinatedSmallOnly` funcione sin depender de `villainBetSize` (siempre `NoBet` cuando no hay facing bet).
- Se cambia `DangerPenaltyCalculator.Calculate()` para usar `Math.Max` entre flush y straight penalty en vez de sumarlas.
- Se condiciona el combo draw bonus a que hero NO haya completado el draw.
- Se agrega `villainAggressorCheckedPreviousStreet` a la llamada de `DetermineAction` en river.
- Se ajustan 6 parámetros de `StrategyProfile` y 1 de `appsettings.json`.

## Capabilities

### Modified Capabilities

- `bluff-condition-fix`: Corregir `ShouldBluff()` para que la condición `IPCoordinatedSmallOnly` se evalúe sin depender de bet size inexistente.
- `danger-penalty-double-count`: Aplicar `Math.Max` entre flush y straight penalty porcentuales para evitar doble conteo.
- `combo-draw-bonus-completed`: No aplicar combo draw bonus cuando hero ya completó el draw (HandRank >= Straight).
- `river-probe-bet-missing`: Pasar `villainAggressorCheckedPreviousStreet` en la llamada a `DetermineAction` para river.
- `calibration-params`: Ajustar 6 parámetros de calibración en `StrategyProfile` defaults y `appsettings.json`.

## Impact

- **`src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`**: Fix en `ShouldBluff()` (eliminar parámetro `betSize`), fix en combo draw bonus (condición adicional).
- **`src/OpenScrape.DecisionMaker/Services/DangerPenaltyCalculator.cs`**: Cambiar suma por `Math.Max` en penalties porcentuales.
- **`src/OpenScrape.App/Forms/FrmMain.cs`**: Agregar parámetro faltante en llamada river a `DetermineAction`.
- **`src/OpenScrape.Domain/Entities/StrategyProfile.cs`**: Ajustar defaults de 6 parámetros.
- **`src/OpenScrape.App/appsettings.json`**: Actualizar valores de calibración.
- **Tests**: Actualizar `DangerPenaltyCalculatorTests`, `PostflopDecisionServiceTests`, `ImpliedOddsCalculatorTests`. Crear tests nuevos para los fixes.
- **Sin nuevas dependencias externas.**
