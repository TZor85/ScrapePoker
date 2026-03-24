# Multiway Position Awareness

## MODIFIED Requirements

### Requirement: Multiway adjustments SHALL differentiate IP vs OOP

The multiway penalty shall use different constants for in-position (IP) and out-of-position (OOP). OOP multiway is significantly more vulnerable than IP multiway. Additionally, pure bluffs shall be blocked when hero is OOP in multiway pots.

#### Scenario: Multiway IP — lower penalty
- **GIVEN** numOpponents = 3 (extraOpponents = 2)
- **AND** isInPosition = true
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** multiway penalty SHALL be 2 × MultiwayFoldBelowIP (2.0) = 4.0
- **AND** adjustedThinValueAbove penalty SHALL be 2 × MultiwayThinValueIP (2.0) = 4.0

#### Scenario: Multiway OOP — higher penalty
- **GIVEN** numOpponents = 3 (extraOpponents = 2)
- **AND** isInPosition = false
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** multiway penalty SHALL be 2 × MultiwayFoldBelowOOP (6.0) = 12.0
- **AND** adjustedThinValueAbove penalty SHALL be 2 × MultiwayThinValueOOP (4.0) = 8.0

#### Scenario: Multiway OOP penalty significantly larger than IP
- **GIVEN** same numOpponents
- **WHEN** comparing OOP vs IP multiway adjustments
- **THEN** OOP adjustedFoldBelow SHALL be at least 2× IP adjustedFoldBelow

#### Scenario: Heads-up — no multiway adjustment
- **GIVEN** numOpponents = 1 (not multiway)
- **WHEN** DetermineAction calculates adjustments
- **THEN** no multiway penalty SHALL be applied (existing behavior preserved)

### Requirement: Pure bluffs SHALL be blocked in multiway OOP pots

When hero is out of position in a multiway pot (3+ players), pure bluffs are extremely -EV because multiple opponents reduce fold equity.

#### Scenario: No bluff multiway OOP
- **GIVEN** isMultiway = true (numOpponents >= 2)
- **AND** isInPosition = false
- **AND** thresholds.CanBluff = true
- **AND** equity < FoldBelow (low equity path)
- **AND** not facing a bet
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL NOT be a bluff
- **AND** the result SHALL be "Check"

#### Scenario: Bluff allowed multiway IP
- **GIVEN** isMultiway = true
- **AND** isInPosition = true
- **AND** thresholds.CanBluff = true
- **AND** ShouldBluff returns true
- **AND** foldEquity >= breakeven threshold
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result MAY be a bluff (existing conditions apply)
