# Player State Refresh and Validation

## ADDED Requirements

### Requirement: Empty/SitOut SHALL be refreshed in each game loop iteration

The game loop shall call `RefreshPlayerStates()` in the else branch (when `needsInitialization=false`) to detect players who leave or go sitout mid-session.

#### Scenario: Player leaves mid-session — detected as Empty
- **GIVEN** P3 was Active=true with non-empty color
- **AND** next iteration, P3's empty region matches empty color
- **WHEN** RefreshPlayerStates runs
- **THEN** P3.Empty SHALL be `true`
- **AND** P3.Active SHALL be `false`

#### Scenario: Player still present — no change
- **GIVEN** P3 was Active=true
- **AND** P3's empty region does NOT match empty color
- **WHEN** RefreshPlayerStates runs
- **THEN** P3.Empty SHALL remain `false`

#### Scenario: Hero (P0) not refreshed
- **GIVEN** P0 is the hero
- **WHEN** RefreshPlayerStates runs
- **THEN** P0 SHALL NOT be checked for empty/sitout
- **AND** because hero is always present

### Requirement: ValidatePlayerStates SHALL cross-validate using multiple signals

When a player has no alias, stack=0, bet=0, and is not active, it shall be inferred as Empty.

#### Scenario: Ghost player with no data — inferred Empty
- **GIVEN** P3.Active = false
- **AND** P3.Empty = false, P3.SitOut = false
- **AND** P3.Alias = "", P3.Stack = 0, P3.Bet = 0
- **WHEN** ValidatePlayerStates runs
- **THEN** P3.Empty SHALL be `true`
- **AND** because all signals indicate no player at this seat

#### Scenario: Player with stack but inactive — not inferred Empty
- **GIVEN** P3.Active = false
- **AND** P3.Stack = 50 (has chips)
- **WHEN** ValidatePlayerStates runs
- **THEN** P3.Empty SHALL NOT be changed to `true`
- **AND** because having a stack suggests player is present (possibly sitout)

#### Scenario: Active player with zero stack — warning only
- **GIVEN** P3.Active = true
- **AND** P3.Stack = 0, P3.Bet = 0
- **WHEN** ValidatePlayerStates runs
- **THEN** P3 SHALL NOT be marked Empty
- **AND** a debug warning SHALL be logged
- **AND** because active color is a stronger signal than zero stack

#### Scenario: ValidatePlayerStates runs after RetryEmptyAliases
- **GIVEN** the game loop sequence
- **WHEN** execution order is checked
- **THEN** ValidatePlayerStates SHALL run AFTER RetryEmptyAliases
- **AND** because alias retry may fill in data that prevents false Empty inference
