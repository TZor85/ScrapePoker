# FoldBelow RaiseOverLimper Fix

## MODIFIED Requirements

### Requirement: Turn_RaiseOverLimper FoldBelow SHALL be 25 (not 0)

The `Turn_RaiseOverLimper` configuration shall have `FoldBelow: 25` instead of `FoldBelow: 0`. This ensures the bot folds hands with very low equity on the turn against limpers.

#### Scenario: Turn vs limper with 15% equity — should fold
- **GIVEN** street = Turn, situation = RaiseOverLimper
- **AND** FoldBelow = 25
- **AND** hero equity = 15%
- **WHEN** DetermineAction is called
- **THEN** the action SHALL involve folding or checking (equity < FoldBelow)
- **AND** NOT blindly continuing with 15% equity

#### Scenario: Turn vs limper with 30% equity — should not fold
- **GIVEN** street = Turn, situation = RaiseOverLimper
- **AND** FoldBelow = 25
- **AND** hero equity = 30%
- **WHEN** DetermineAction is called
- **THEN** hero SHALL NOT fold (equity > FoldBelow)

### Requirement: River_RaiseOverLimper FoldBelow SHALL be 30 (not 0)

The `River_RaiseOverLimper` configuration shall have `FoldBelow: 30` instead of `FoldBelow: 0`. River requires slightly higher threshold since it's the final street.

#### Scenario: River vs limper with 20% equity — should fold
- **GIVEN** street = River, situation = RaiseOverLimper
- **AND** FoldBelow = 30
- **AND** hero equity = 20%
- **WHEN** DetermineAction is called
- **THEN** the action SHALL involve folding or checking

#### Scenario: River vs limper with 35% equity — should not fold
- **GIVEN** street = River, situation = RaiseOverLimper
- **AND** FoldBelow = 30
- **AND** hero equity = 35%
- **WHEN** DetermineAction is called
- **THEN** hero SHALL NOT fold

### Requirement: IsSimplified mode preserved

#### Scenario: Both configs remain in simplified mode
- **GIVEN** Turn_RaiseOverLimper and River_RaiseOverLimper updated configs
- **WHEN** IsSimplified is checked
- **THEN** both SHALL have IsSimplified = true
- **AND** simplified bet sizing logic SHALL continue to work
