# Fold Equity Check for Pure Bluffs

## MODIFIED Requirements

### Requirement: Pure bluffs SHALL verify fold equity before executing

When `ShouldBluff()` returns true, the system shall verify that `foldEquity >= breakevenFoldEquity` before returning a bluff action. The breakeven fold equity is `betFraction / (1 + betFraction)` where `betFraction` is derived from `thresholds.BluffBetSize`. If fold equity is insufficient, the bluff shall not execute and the hand falls through to the check/fold path.

#### Scenario: Bluff with sufficient fold equity (Bet 1/3, 40% FE)
- **GIVEN** it is not facing a bet
- **AND** not multiway
- **AND** thresholds.CanBluff = true
- **AND** ShouldBluff returns true
- **AND** thresholds.BluffBetSize = "Bet 1/3" (betFraction = 0.33, breakeven = 25%)
- **AND** foldEquity = 40%
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL be a Bluff action
- **AND** the reason SHALL contain fold equity information

#### Scenario: Bluff rejected — insufficient fold equity (Bet 1/2, 20% FE)
- **GIVEN** it is not facing a bet
- **AND** not multiway
- **AND** thresholds.CanBluff = true
- **AND** ShouldBluff returns true
- **AND** thresholds.BluffBetSize = "Bet 1/2" (betFraction = 0.50, breakeven = 33%)
- **AND** foldEquity = 20%
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL NOT be a Bluff
- **AND** the hand SHALL fall through to the check path

#### Scenario: Bluff rejected — zero fold equity (calling station)
- **GIVEN** it is not facing a bet
- **AND** not multiway
- **AND** thresholds.CanBluff = true
- **AND** ShouldBluff returns true
- **AND** foldEquity = 5%
- **AND** thresholds.BluffBetSize = "Bet 1/3" (breakeven = 25%)
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL be "Check" (not a bluff)

#### Scenario: Bluff at exact breakeven — still profitable
- **GIVEN** it is not facing a bet
- **AND** thresholds.BluffBetSize = "Bet 1/2" (breakeven = 33.33%)
- **AND** foldEquity = 34%
- **AND** ShouldBluff returns true
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL be a Bluff (fold equity >= breakeven)

#### Scenario: Small bluff size requires less fold equity
- **GIVEN** it is not facing a bet
- **AND** thresholds.BluffBetSize = "Bet 1/4" (betFraction = 0.25, breakeven = 20%)
- **AND** foldEquity = 22%
- **AND** ShouldBluff returns true
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL be a Bluff
