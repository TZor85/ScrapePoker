# Opponent Action Recording in Game Loop

## ADDED Requirements

### Requirement: FrmMain SHALL record opponent actions via OpponentTracker

The game loop shall call `OpponentTracker.Record*()` methods when opponent actions are detected, building a statistical profile of each villain over time.

#### Scenario: New hand records HandPlayed for all active villains
- **GIVEN** a new hand is detected
- **AND** there are 2 active villains ("PlayerA", "PlayerB")
- **WHEN** DetectNewHand processes the new hand
- **THEN** `RecordHandPlayed("PlayerA")` SHALL be called
- **AND** `RecordHandPlayed("PlayerB")` SHALL be called

#### Scenario: Villain raise preflop records VPIP and PFR
- **GIVEN** villain "PlayerA" has bet > BigBlind in preflop
- **WHEN** preflop situation is detected
- **THEN** `RecordVPIP("PlayerA")` SHALL be called
- **AND** if bet > 2× BigBlind, `RecordPFR("PlayerA")` SHALL also be called

#### Scenario: Villain bet on flop records PostflopAction.Bet
- **GIVEN** villain "PlayerA" bets on the flop (maxBet > 0)
- **WHEN** DetermineFlopActionUnified processes the flop
- **THEN** `RecordPostflopAction("PlayerA", PostflopAction.Bet)` SHALL be called

#### Scenario: Preflop aggressor bets flop records c-bet
- **GIVEN** villain "PlayerA" was the preflop aggressor
- **AND** villain bets on the flop
- **WHEN** DetermineFlopActionUnified processes the flop
- **THEN** `RecordCBetOpportunity("PlayerA", didCBet: true)` SHALL be called

#### Scenario: Preflop aggressor checks flop records missed c-bet
- **GIVEN** villain "PlayerA" was the preflop aggressor
- **AND** villain does NOT bet on the flop (maxBet = 0)
- **WHEN** DetermineFlopActionUnified processes the flop
- **THEN** `RecordCBetOpportunity("PlayerA", didCBet: false)` SHALL be called

#### Scenario: Profile becomes reliable after 20 hands
- **GIVEN** "PlayerA" has played 19 hands (IsReliable = false)
- **WHEN** the 20th `RecordHandPlayed("PlayerA")` is called
- **THEN** `GetProfile("PlayerA").IsReliable` SHALL be true
- **AND** `GetProfile("PlayerA").Type` SHALL return a concrete type (TAG/LAG/TP/LP)

#### Scenario: Villain with no name is not tracked
- **GIVEN** a player with `Name = null` or empty
- **WHEN** GetActiveVillainId is called
- **THEN** it SHALL NOT record actions for that player
