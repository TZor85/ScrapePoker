# Villain Bet Size Tracking

## ADDED Requirements

### Requirement: PostflopGameContext SHALL store villain bet size per street

`PostflopGameContext` shall store `VillainBetSizeFlop` and `VillainBetSizeTurn` as `BetSizeCategory` values in addition to the existing boolean fields. These fields shall be updated in `FrmMain` when the opponent's bet is detected, and reset to `NoBet` on new hand.

#### Scenario: Flop bet size stored in context
- **GIVEN** villain bets on the Flop
- **AND** `GetOpponentBetSize(maxBet, potSize)` returns `BetSizeCategory.Medium`
- **WHEN** `FrmMain` updates the postflop context after flop action
- **THEN** `_postflopContext.VillainBetSizeFlop` SHALL be `BetSizeCategory.Medium`
- **AND** `_postflopContext.VillainBetFlop` SHALL be `true` (existing behavior preserved)

#### Scenario: Turn bet size stored in context
- **GIVEN** villain bets on the Turn
- **AND** `GetOpponentBetSize(maxBet, potSize)` returns `BetSizeCategory.Large`
- **WHEN** `FrmMain` updates the postflop context after turn action
- **THEN** `_postflopContext.VillainBetSizeTurn` SHALL be `BetSizeCategory.Large`

#### Scenario: No bet on flop leaves NoBet
- **GIVEN** villain does not bet on the Flop (maxBet = 0)
- **WHEN** `FrmMain` updates the postflop context after flop action
- **THEN** `_postflopContext.VillainBetSizeFlop` SHALL be `BetSizeCategory.NoBet`

#### Scenario: Reset clears bet size fields
- **GIVEN** a hand has completed and context stores `VillainBetSizeFlop = Large`
- **WHEN** `PostflopGameContext.Reset()` is called for a new hand
- **THEN** `VillainBetSizeFlop` SHALL be `BetSizeCategory.NoBet`
- **AND** `VillainBetSizeTurn` SHALL be `BetSizeCategory.NoBet`

### Requirement: PostflopDecisionService SHALL detect villain sizing tells

When the villain escalates bet size between streets (e.g., Small on flop → Large on turn), the system shall apply an additional `VillainSizingEscalationPenalty` to `adjustedFoldBelow`. This reflects that escalating bet sizes indicate a stronger range.

#### Scenario: Villain escalates Small → Large on turn
- **GIVEN** hero is facing a bet on the Turn
- **AND** `villainBetSizeFlop = BetSizeCategory.Small`
- **AND** `villainBetSize = BetSizeCategory.Large` (current turn bet)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** `adjustedFoldBelow` SHALL increase by `VillainSizingEscalationPenalty` (default 4.0)
- **AND** this penalty is IN ADDITION TO the existing facing bet penalty and street scaling

#### Scenario: Villain escalates Medium → Large on river
- **GIVEN** hero is facing a bet on the River
- **AND** `villainBetSizeTurn = BetSizeCategory.Medium`
- **AND** `villainBetSize = BetSizeCategory.Large` (current river bet)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** `adjustedFoldBelow` SHALL increase by `VillainSizingEscalationPenalty`

#### Scenario: Villain maintains same size — no penalty
- **GIVEN** hero is facing a bet on the Turn
- **AND** `villainBetSizeFlop = BetSizeCategory.Large`
- **AND** `villainBetSize = BetSizeCategory.Large` (same size)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** `adjustedFoldBelow` SHALL NOT increase by `VillainSizingEscalationPenalty`

#### Scenario: Villain de-escalates — no penalty
- **GIVEN** hero is facing a bet on the Turn
- **AND** `villainBetSizeFlop = BetSizeCategory.Large`
- **AND** `villainBetSize = BetSizeCategory.Small` (de-escalated)
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** `adjustedFoldBelow` SHALL NOT increase by `VillainSizingEscalationPenalty`

#### Scenario: No previous street bet — no penalty
- **GIVEN** hero is facing a bet on the Turn
- **AND** `villainBetSizeFlop = BetSizeCategory.NoBet` (villain checked flop)
- **AND** `villainBetSize = BetSizeCategory.Large`
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** `adjustedFoldBelow` SHALL NOT increase by `VillainSizingEscalationPenalty`
- **AND** because there is no previous reference to compare against

#### Scenario: Flop has no sizing tell detection
- **GIVEN** hero is facing a bet on the Flop
- **AND** any `villainBetSize`
- **WHEN** DetermineAction calculates adjustedFoldBelow
- **THEN** `adjustedFoldBelow` SHALL NOT include sizing tell penalty
- **AND** because there is no previous street to compare against on flop
