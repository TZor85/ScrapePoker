# Danger Penalty Street Scaling

## MODIFIED Requirements

### Requirement: Percentage-based danger penalties SHALL scale by street

The `DangerFlushCompletePct` and `DangerStraightCompletePct` penalties shall be multiplied by a street-dependent factor. Flop has more future variance (2 cards to come), so penalties are higher. River is final, so penalties are lower. Flat penalties (BoardPaired, Overcard, FlushDraw) are NOT scaled.

#### Scenario: Flush complete on flop — higher penalty
- **GIVEN** hero equity = 60%
- **AND** boardChange.FlushCompleted = true
- **AND** street = Flop
- **AND** DangerFlushCompletePct = 35, DangerPenaltyFlopMultiplier = 1.3
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** flush penalty SHALL be 60 × 0.35 × 1.3 = 27.3
- **AND** penalty is higher than without street scaling (60 × 0.35 = 21.0)

#### Scenario: Flush complete on turn — base penalty
- **GIVEN** hero equity = 60%
- **AND** boardChange.FlushCompleted = true
- **AND** street = Turn
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** flush penalty SHALL be 60 × 0.35 × 1.0 = 21.0

#### Scenario: Flush complete on river — lower penalty
- **GIVEN** hero equity = 60%
- **AND** boardChange.FlushCompleted = true
- **AND** street = River
- **AND** DangerPenaltyRiverMultiplier = 0.8
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** flush penalty SHALL be 60 × 0.35 × 0.8 = 16.8

#### Scenario: Straight complete follows same scaling
- **GIVEN** hero equity = 70%
- **AND** boardChange.StraightCompleted = true
- **AND** street = Flop, DangerStraightCompletePct = 18
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** straight penalty SHALL be 70 × 0.18 × 1.3 = 16.38

#### Scenario: Flat penalties NOT scaled by street
- **GIVEN** boardChange.BoardPaired = true
- **AND** street = Flop
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** BoardPaired penalty SHALL be DangerBoardPairedPenalty (5.0) unchanged
- **AND** no street multiplier applied to flat penalties

#### Scenario: DangerPenaltyCalculator.Calculate receives street parameter
- **GIVEN** DangerPenaltyCalculator.Calculate is called
- **WHEN** the method signature is checked
- **THEN** it SHALL accept `BoardPosition street` as a parameter
- **AND** PostflopDecisionService SHALL propagate street to the calculator
