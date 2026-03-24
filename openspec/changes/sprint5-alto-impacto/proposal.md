# Sprint 5 — Mejoras de Alto Impacto

## Why

El análisis exhaustivo del motor de decisión tras completar los Sprints 1-4 reveló 5 leaks estratégicos de alto impacto que generan pérdidas de EV estimadas en +8-12% ROI:

1. **Overcards ignoradas con draws**: AK en 9-8-7 solo cuenta straight outs, ignorando 6 outs de A/K como overcards. Subestima equity de manos premium en ~15-20 puntos.
2. **Danger penalty plana por street**: Misma penalización en flop (2 calles por venir) que en river (definitivo). Sobre-penaliza river, sub-penaliza flop.
3. **Multiway sin posición**: OOP con 3 oponentes es el spot más costoso del poker; el ajuste plano de +4/oponente no captura la diferencia IP vs OOP.
4. **Coordinated = SemiWet = Wet**: Boards con 60+ wetness (muchos draws) usan el mismo sizing que boards con 35-60 (algunos draws). Sizing ineficiente en ~15% de flops.
5. **Flop sin configs DonkBet**: El donk bet en flop (~10-15% de manos) usa fallback genérico sin calibración.

## What Changes

- `OutsCalculator`: Cuenta overcards siempre, incluso con draws activos, separando "clean overcards" de "overlap outs".
- `DangerPenaltyCalculator`: Multiplicador por street (Flop ×1.3, Turn ×1.0, River ×0.8) en penalizaciones porcentuales.
- `PostflopDecisionService` + `PokerConstants`: Multiway penalty diferenciada IP vs OOP.
- `BoardTextureAnalyzer` + `StreetThresholds` + `PostflopDecisionService`: Nueva categoría "Wet" con sizing propio.
- `appsettings.json`: Nuevas configs `Flop_DonkBet` y `Flop_DonkBetVsOpenRaise`.

## Capabilities

### Modified Capabilities
- `outs-calculation`: Overcards contadas siempre, sin doble-contar outs de straight draw.
- `danger-penalty`: Penalización porcentual escalada por street.
- `multiway-adjustment`: Penalty diferenciada IP (+2/opp) vs OOP (+6/opp). Bluff bloqueado OOP 3+way.
- `board-texture-sizing`: Nueva categoría "Wet" con `WetBoardBetSize` en `StreetThresholds`.
- `flop-donkbet-config`: Configs específicas para donk bet en flop.

## Impact

- **`OutsCalculator.cs`** — S5.1
- **`DangerPenaltyCalculator.cs`** — S5.2
- **`PostflopDecisionService.cs`** — S5.3, S5.4
- **`PokerConstants.cs`** — S5.3
- **`BoardTextureAnalyzer.cs`** — S5.4
- **`StreetThresholds.cs`** — S5.4
- **`StrategyProfile.cs`** — S5.2
- **`appsettings.json`** — S5.4, S5.5
- **Sin nuevas dependencias externas.**
