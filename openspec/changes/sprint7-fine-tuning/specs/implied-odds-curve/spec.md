# Implied Odds Curve Interpolation

## MODIFIED Requirements

### Requirement: Implied odds interpolation SHALL use square root curve instead of linear

The interpolation between SPR shallow threshold (2.0, factor 0.95) and deep threshold (4.0, factor 0.65) shall use `Math.Sqrt(position)` instead of `position` directly. This captures that implied odds increase faster as SPR approaches deep stacks.

#### Scenario: SPR 3.0 — sqrt factor is lower than linear
- **GIVEN** SPR = 3.0, shallow threshold = 2.0, deep threshold = 4.0
- **AND** shallow factor = 0.95, deep factor = 0.65
- **WHEN** CalculateImpliedOddsFactor is called
- **THEN** the sqrt position SHALL be Math.Sqrt(0.5) ≈ 0.707
- **AND** the factor SHALL be ≈ 0.95 + (0.707 × (0.65 - 0.95)) ≈ 0.738
- **AND** this SHALL be less than the linear factor (0.80)

#### Scenario: SPR 2.5 — more significant difference
- **GIVEN** SPR = 2.5 (position = 0.25, sqrt = 0.5)
- **WHEN** CalculateImpliedOddsFactor is called
- **THEN** sqrt factor SHALL be ≈ 0.95 + (0.5 × -0.30) = 0.80
- **AND** linear factor would be 0.95 + (0.25 × -0.30) = 0.875
- **AND** sqrt factor < linear factor (better implied odds captured earlier)

#### Scenario: SPR at shallow threshold (2.0) — no change
- **GIVEN** SPR = 2.0 (exactly at shallow threshold)
- **WHEN** CalculateImpliedOddsFactor is called
- **THEN** factor SHALL be 0.95 (shallow factor, unchanged)
- **AND** because sqrt(0) = 0 = linear(0)

#### Scenario: SPR at deep threshold (4.0) — no change
- **GIVEN** SPR = 4.0 (exactly at deep threshold)
- **WHEN** CalculateImpliedOddsFactor is called
- **THEN** factor SHALL be 0.65 (deep factor, unchanged)
- **AND** because sqrt(1) = 1 = linear(1)

#### Scenario: SPR below shallow — uses shallow factor directly
- **GIVEN** SPR = 1.5 (below shallow threshold 2.0)
- **WHEN** CalculateImpliedOddsFactor is called
- **THEN** factor SHALL be 0.95 (shallow, no interpolation)
