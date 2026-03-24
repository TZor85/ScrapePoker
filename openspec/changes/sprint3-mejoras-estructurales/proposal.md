# Sprint 3 — Mejoras Estructurales del Motor de Decisión

## Why

El motor de decisión postflop tiene 6 debilidades estructurales que generan pérdidas de EV estimadas en +5-8% ROI:

1. **Facing bet penalty plano**: Las penalizaciones por tamaño de apuesta del villano (Small+1, Medium+4, Large+8) son idénticas en flop, turn y river. En flop, una large bet es común (rango amplio, c-bets); en river, una large bet indica rango fuerte (más value). El bot foldea demasiado en flop y paga demasiado en river.

2. **Bluff catch solo en river**: `HandleLowEquity` solo permite bluff catch en river. En turn, facing small bet con OnePair+ y equity marginal, hero debería poder call para ver river barato. Se pierden spots +EV.

3. **Floating IP sin draw real**: La condición `totalOuts >= 4` para floating incluye overcards débiles sin draw legítimo. Hero float-calls con manos como 9h2d en AcKcQc porque tiene "4 outs" (overcards), pero sin ningún draw real — un float -EV.

4. **Bluffs sin verificación de fold equity**: `ShouldBluff()` decide bluffear basado solo en frecuencia aleatoria, sin verificar si el bluff es matemáticamente rentable. Contra calling stations, esto genera bluffs -EV sistemáticos.

5. **Board monotone sin sizing propio**: Boards con 3+ cartas del mismo palo se clasifican como "Dry" o "Coordinated" sin sizing específico. El c-bet en board monotone debería ser menor (1/4 pot) porque el flush draw es muy probable para el villano.

6. **Equity preflop siempre vs random**: La equity preflop se calcula contra rango aleatorio en todas las situaciones. En 3Bet/4Bet, el rango del villano es mucho más estrecho y la equity debería reflejar eso.

## What Changes

Se implementan 6 mejoras independientes en el motor de decisión:

- Facing bet penalty escalado por street (×1.0 flop, ×1.15 turn, ×1.3 river).
- Bluff catch extendido al turn con condiciones más estrictas que en river.
- Floating IP exige draw real (flush draw, combo draw, o 6+ outs).
- Verificación de fold equity mínima antes de ejecutar bluffs.
- Nuevo case "Monotone" en el switch de board texture con sizing 1/4 pot.
- Equity preflop contra VillainRange filtrado por HandSituation en pots 3Bet+.

## Capabilities

### Modified Capabilities

- `facing-bet-penalty`: Penalización por bet size escalada por street (flop ×1.0, turn ×1.15, river ×1.3).
- `bluff-catch`: Extendido de solo river a turn+river, con condiciones más estrictas en turn.
- `floating-ip`: Requiere draw real (flush draw, combo draw, o totalOuts >= 6) además de totalOuts >= 4.
- `bluff-decision`: Verificación de fold equity mínima (`foldEquity >= betFraction / (1 + betFraction)`) antes de bluffear.
- `board-texture-sizing`: Nuevo case "Monotone" con sizing "Bet 1/4" en HandleNoBet.
- `preflop-equity`: Calcula contra VillainRange cuando existe rango definido para la HandSituation.

## Impact

- **`PostflopDecisionService.cs`**: S2.3 (facing bet scaling), S3.1 (bluff catch turn), S3.2 (floating IP), S3.3 (fold equity check), S3.4 (monotone sizing).
- **`PokerConstants.cs`**: Nuevas constantes de street multiplier.
- **`StrategyProfile.cs`**: Nuevos parámetros `FacingBetTurnMultiplier`, `FacingBetRiverMultiplier`, `BluffCatchTurnEquityMultiplier`, `FloatingIPMinOuts`, `MonotoneBoardBetSize`.
- **`StreetThresholds.cs`**: Nuevo campo `MonotoneBoardBetSize`.
- **`UnifiedPokerCalculator.cs`**: Pasar `handSituation` al calculador preflop.
- **`PreflopEquityCalculator.cs`**: Soporte para equity vs VillainRange.
- **Sin nuevas dependencias externas.**
