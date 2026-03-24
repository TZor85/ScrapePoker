# Bet-Check-Bet vs Barrel Detection

## MODIFIED Requirements

### Requirement: PostflopGameContext SHALL track villain checked middle street

When the villain bet on the flop but did NOT bet on the turn, `VillainCheckedMiddleStreet` shall be set to `true`. This indicates a bet-check-bet pattern if the villain bets again on the river.

#### Scenario: Villain bet flop, checked turn → flag set
- **GIVEN** `VillainBetFlop = true`
- **AND** villain does not bet on the turn (`villainBet = false`)
- **WHEN** `UpdateTurnState` is called
- **THEN** `VillainCheckedMiddleStreet` SHALL be `true`

#### Scenario: Villain bet flop AND turn → flag not set
- **GIVEN** `VillainBetFlop = true`
- **AND** villain bets on the turn (`villainBet = true`)
- **WHEN** `UpdateTurnState` is called
- **THEN** `VillainCheckedMiddleStreet` SHALL be `false`

#### Scenario: Villain did not bet flop → flag not set
- **GIVEN** `VillainBetFlop = false`
- **AND** villain does not bet on the turn
- **WHEN** `UpdateTurnState` is called
- **THEN** `VillainCheckedMiddleStreet` SHALL be `false`

#### Scenario: Flag resets on new hand
- **GIVEN** `VillainCheckedMiddleStreet = true` from previous hand
- **WHEN** `Reset()` is called
- **THEN** `VillainCheckedMiddleStreet` SHALL be `false`

### Requirement: PostflopDecisionService SHALL apply different penalties for barrel vs bet-check-bet

When facing a bet with `villainBarreling = true`, the penalty shall differ based on whether it's a consecutive barrel or a bet-check-bet reactivation.

#### Scenario: Consecutive barrel (bet-bet) applies full penalty
- **GIVEN** `villainBarreling = true`
- **AND** `villainCheckedMiddleStreet = false`
- **AND** hero is facing a bet
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** adjustedFoldBelow SHALL increase by `VillainBarrelFoldIncrease` (5.0)
- **AND** adjustedThinValueAbove SHALL increase by `VillainBarrelThinValueIncrease` (3.0)

#### Scenario: Bet-check-bet applies reduced penalty
- **GIVEN** `villainBarreling = true`
- **AND** `villainCheckedMiddleStreet = true`
- **AND** hero is facing a bet on river
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** adjustedFoldBelow SHALL increase by `VillainBetCheckBetPenalty` (2.0)
- **AND** adjustedThinValueAbove SHALL NOT increase (villain likely weak)

#### Scenario: Bet-check-bet penalty is lower than barrel penalty
- **GIVEN** same equity and bet size
- **WHEN** comparing barrel vs bet-check-bet adjustedFoldBelow
- **THEN** bet-check-bet penalty (2.0) SHALL be less than barrel penalty (5.0)
