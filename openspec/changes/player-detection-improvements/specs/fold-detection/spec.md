# Fold Detection Mid-Hand

## ADDED Requirements

### Requirement: Villains who fold mid-hand SHALL be detected and marked inactive

A new `DetectFoldedPlayers()` method shall check active non-hero villains for loss of playing color during postflop streets. Folded players are marked `HasFolded=true` and `Active=false`, reducing `numOpponents` for subsequent streets.

#### Scenario: Villain folds on flop — inactive on turn
- **GIVEN** P3 was Active=true with playing color on flop
- **AND** on turn, P3's playing region no longer shows playing color
- **WHEN** DetectFoldedPlayers runs before ProcessTurnAsync
- **THEN** P3.HasFolded SHALL be `true`
- **AND** P3.Active SHALL be `false`
- **AND** numOpponents SHALL decrease by 1

#### Scenario: Villain still active — no change
- **GIVEN** P3 was Active=true
- **AND** P3's playing region still shows playing color
- **WHEN** DetectFoldedPlayers runs
- **THEN** P3.HasFolded SHALL remain `false`
- **AND** P3.Active SHALL remain `true`

#### Scenario: Hero (P0) never checked for fold
- **GIVEN** P0 is always the hero
- **WHEN** DetectFoldedPlayers runs
- **THEN** P0 SHALL NOT be checked for fold
- **AND** P0.Active SHALL remain `true`

#### Scenario: Already folded player not re-checked
- **GIVEN** P3.HasFolded = true (folded on flop)
- **WHEN** DetectFoldedPlayers runs on turn
- **THEN** P3 SHALL NOT be re-checked
- **AND** because folded players stay folded for the rest of the hand

#### Scenario: Preflop — no fold detection
- **GIVEN** game state is PreflopAction
- **WHEN** DetectFoldedPlayers runs
- **THEN** no players SHALL be checked
- **AND** because fold detection only applies postflop

#### Scenario: Multiple villains fold
- **GIVEN** P2, P3, P4 were all Active=true
- **AND** P2 and P4 no longer show playing color
- **WHEN** DetectFoldedPlayers runs
- **THEN** P2.HasFolded and P4.HasFolded SHALL be `true`
- **AND** P3.Active SHALL remain `true`
- **AND** numOpponents SHALL be 1 (only P3)

### Requirement: HasFolded SHALL reset on new hand

#### Scenario: New hand resets fold status
- **GIVEN** P3.HasFolded = true from previous hand
- **WHEN** new hand is detected and PlayerGameState is reset
- **THEN** P3.HasFolded SHALL be `false`
- **AND** because fold is per-hand state
