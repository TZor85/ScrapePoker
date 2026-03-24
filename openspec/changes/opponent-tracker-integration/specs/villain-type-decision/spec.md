# Villain Type in Decision Making

## MODIFIED Requirements

### Requirement: DetermineAction SHALL receive real villain type from OpponentTracker

The three calls to `DetermineAction` (flop, turn, river) shall pass the actual `villainType` from `OpponentTracker.GetProfile()` instead of `OpponentType.Unknown`. Only reliable profiles (20+ hands) shall be used; otherwise fallback to Unknown.

#### Scenario: LAG villain detected — adjustments applied
- **GIVEN** villain "PlayerA" has 25 hands played
- **AND** VPIP = 45%, AggressionFactor = 2.5 → Type = LAG
- **WHEN** DetermineAction is called with `villainType: OpponentType.LAG`
- **THEN** adjustedFoldBelow SHALL decrease by 5.0 (facing bet)
- **AND** adjustedThinValueAbove SHALL decrease by 3.0
- **AND** because LAG bluffs often → call more to trap

#### Scenario: TP (nit) villain detected — tighter play
- **GIVEN** villain "PlayerA" has 30 hands played
- **AND** VPIP = 15%, AggressionFactor = 0.8 → Type = TP
- **WHEN** DetermineAction is called with `villainType: OpponentType.TP` facing bet
- **THEN** adjustedFoldBelow SHALL increase by 3.0
- **AND** because nit bets = strong hand → fold more marginals

#### Scenario: LP (fish) villain — exploit with wider range
- **GIVEN** villain "PlayerA" has 20 hands played
- **AND** VPIP = 55%, AggressionFactor = 0.6 → Type = LP
- **WHEN** DetermineAction is called with `villainType: OpponentType.LP`
- **THEN** adjustedFoldBelow SHALL decrease by 4.0
- **AND** because fish plays too many hands → value bet thinner

#### Scenario: Unreliable profile (< 20 hands) — uses Unknown
- **GIVEN** villain "PlayerA" has 15 hands played (IsReliable = false)
- **WHEN** villainType is determined for DetermineAction
- **THEN** villainType SHALL be `OpponentType.Unknown`
- **AND** no opponent-specific adjustments SHALL be applied

#### Scenario: Fold equity adjusted by opponent type
- **GIVEN** villain "PlayerA" is LAG (reliable profile)
- **AND** base fold equity = 30%
- **WHEN** GetAdjustedFoldEquity("PlayerA", 30) is called
- **THEN** adjusted fold equity SHALL be 30 × 0.70 = 21%
- **AND** this adjusted value SHALL be passed to DetermineAction as `foldEquity`

#### Scenario: LP villain increases fold equity
- **GIVEN** villain "PlayerA" is LP (fish)
- **AND** base fold equity = 30%
- **WHEN** GetAdjustedFoldEquity("PlayerA", 30) is called
- **THEN** adjusted fold equity SHALL be 30 × 1.25 = 37.5%
