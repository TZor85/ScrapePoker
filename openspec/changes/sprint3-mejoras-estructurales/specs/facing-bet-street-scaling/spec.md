# Facing Bet Penalty Street Scaling

## MODIFIED Requirements

### Requirement: Facing bet penalties SHALL scale by street

The facing bet penalty (Small+1, Medium+4, Large+8) shall be multiplied by a street-dependent factor: Flop ×1.0, Turn ×1.15, River ×1.30. This reflects that large bets on later streets represent stronger ranges.

#### Scenario: Flop facing large bet — no scaling
- **GIVEN** hero is facing a bet on the Flop
- **AND** villain bet size is Large (base penalty = 8.0)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** facingBetPenalty SHALL be 8.0 (8.0 × 1.0)

#### Scenario: Turn facing large bet — moderate scaling
- **GIVEN** hero is facing a bet on the Turn
- **AND** villain bet size is Large (base penalty = 8.0)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** facingBetPenalty SHALL be 9.2 (8.0 × 1.15)

#### Scenario: River facing large bet — maximum scaling
- **GIVEN** hero is facing a bet on the River
- **AND** villain bet size is Large (base penalty = 8.0)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** facingBetPenalty SHALL be 10.4 (8.0 × 1.30)

#### Scenario: Turn facing small bet — minimal impact
- **GIVEN** hero is facing a bet on the Turn
- **AND** villain bet size is Small (base penalty = 1.0)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** facingBetPenalty SHALL be 1.15 (1.0 × 1.15)

#### Scenario: River facing medium bet — proportional scaling
- **GIVEN** hero is facing a bet on the River
- **AND** villain bet size is Medium (base penalty = 4.0)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** facingBetPenalty SHALL be 5.2 (4.0 × 1.30)

#### Scenario: ThinValueAbove also scales
- **GIVEN** hero is facing a bet on the River
- **AND** villain bet size is Large (scaled penalty = 10.4)
- **WHEN** DetermineAction calculates adjustedThinValueAbove
- **THEN** adjustedThinValueAbove SHALL increase by 5.2 (10.4 / 2)
