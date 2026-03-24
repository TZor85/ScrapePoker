## MODIFIED Requirements

### Requirement: Penalties porcentuales de flush y straight no deben sumarse
`DangerPenaltyCalculator.Calculate()` SHALL aplicar `Math.Max` entre la penalización porcentual de flush completado y straight completado, porque el villano típicamente tiene UNA de las dos manos completadas, no ambas.

#### Scenario: Solo flush completado
- **GIVEN** boardChange.FlushCompleted es true, boardChange.StraightCompleted es false
- **AND** rawEquity es 70, DangerFlushCompletePct es 35
- **WHEN** se calcula la penalización
- **THEN** penalty incluye 70 × 0.35 = 24.5 puntos por flush completado

#### Scenario: Solo straight completado
- **GIVEN** boardChange.FlushCompleted es false, boardChange.StraightCompleted es true
- **AND** rawEquity es 70, DangerStraightCompletePct es 18
- **WHEN** se calcula la penalización
- **THEN** penalty incluye 70 × 0.18 = 12.6 puntos por straight completado

#### Scenario: Flush Y straight completados simultáneamente
- **GIVEN** boardChange.FlushCompleted es true, boardChange.StraightCompleted es true
- **AND** rawEquity es 70, DangerFlushCompletePct es 35, DangerStraightCompletePct es 18
- **WHEN** se calcula la penalización
- **THEN** penalty incluye Math.Max(70 × 0.35, 70 × 0.18) = 24.5 puntos (el mayor, NO la suma)
- **AND** penalty NO es 70 × 0.35 + 70 × 0.18 = 37.1

#### Scenario: Flush draw appeared sin flush completado
- **GIVEN** boardChange.FlushCompleted es false, boardChange.FlushDrawAppeared es true
- **WHEN** se calcula la penalización
- **THEN** penalty incluye DangerFlushDrawPenalty flat (no porcentual)
- **AND** flush draw penalty se suma normalmente a las flat penalties (board paired, overcard)

#### Scenario: Penalties flat siguen sumándose normalmente
- **GIVEN** boardChange.BoardPaired es true, boardChange.OvercardAppeared es true
- **AND** flush/straight penalties porcentuales ya calculadas con Math.Max
- **WHEN** se calcula la penalización total
- **THEN** penalty = Max(flushPct, straightPct) + boardPairedFlat + overcardFlat
