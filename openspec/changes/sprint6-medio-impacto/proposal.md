# Sprint 6 — Mejoras de Medio Impacto

## Why

Tras completar el Sprint 5 (alto impacto), quedan 5 optimizaciones de medio impacto que refinan la lectura del oponente, las penalizaciones por manos vulnerables y la extracción de valor en spots actualmente perdidos:

1. **Bet-Check-Bet ≠ Barrel**: `villainBarreling` aplica +5 FoldBelow cuando el villano apuesta 2+ calles, pero no distingue bet-bet consecutivo (barrel real, rango fuerte) de bet-check-bet (draw fallido que reintenta river, rango más débil). La penalización se aplica incorrectamente en ~8% de manos river.

2. **Reverse implied odds sin blockers**: La penalización de 7.0/4.0 se aplica sin verificar si hero bloquea el draw del villano. Si hero tiene A♠ con flush draw en spades en el board, reduce combinaciones del villano → penalización debería ser menor.

3. **Hero blocker plano (0.5×)**: La reducción por blocker es siempre 50%, sin distinguir nut blocker (As del palo) de non-nut blocker (5s del palo). El nut blocker elimina más combinaciones del villano.

4. **Probe bet solo OOP**: Solo permite probe bet cuando hero está OOP (`!isInPosition`). Pero IP probe bet es muy rentable — liderar cuando el oponente checkeó permite definir la mano y extraer valor.

5. **Slowplay solo flop**: Solo permite slowplay en flop con ThreeOfAKind+. En turn con nuts en board seco contra villano agresivo, slowplay induciría bet en river. El spot es menos frecuente pero el value perdido es significativo.

## What Changes

- `PostflopGameContext`: Nuevo flag `VillainCheckedMiddleStreet` para detectar patrón bet-check-bet.
- `PostflopDecisionService`: Penalización diferenciada barrel vs bet-check-bet. Probe bet permitido IP. Slowplay extendida a turn.
- `ImpliedOddsCalculator`: Recibe `heroBlocksDangerSuit` y reduce penalización cuando hero bloquea.
- `DangerPenaltyCalculator`: Blocker granular — nut blocker ×0.35, non-nut ×0.55, board con 4+ del palo ×0.7.
- `StrategyProfile`: Nuevos parámetros para bet-check-bet penalty, nut/non-nut blocker reduction, probe bet IP.
- `StreetThresholds`: Nuevo campo `ProbeBetIPSize`.

## Capabilities

### Modified Capabilities
- `barrel-detection`: Distingue bet-bet (barrel, +5 FoldBelow) de bet-check-bet (reactivation, +2).
- `reverse-implied-odds`: Reduce penalización cuando hero bloquea el palo del draw.
- `blocker-effect`: Nut blocker ×0.35, non-nut ×0.55, board 4+ flush ×0.7.
- `probe-bet`: Extendido a IP con sizing configurable.
- `slow-play`: Extendido a turn con condición de OpponentType LAG.

## Impact

- **`PostflopGameContext.cs`** — S6.1 (VillainCheckedMiddleStreet)
- **`PostflopDecisionService.cs`** — S6.1, S6.4, S6.5
- **`ImpliedOddsCalculator.cs`** — S6.2
- **`DangerPenaltyCalculator.cs`** — S6.3
- **`StrategyProfile.cs`** — S6.1, S6.2, S6.3
- **`StreetThresholds.cs`** — S6.4
- **`FrmMain.cs`** — S6.1 (actualizar contexto), S6.2 (propagar blocker)
- **Sin nuevas dependencias externas.**
