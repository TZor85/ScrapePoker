# Pot Size Reprocess Fix

## MODIFIED Requirements

### Requirement: Pot and bet values SHALL be updated before re-processing a street

When `ProcessPostFlopAsync` detects that the current street has not changed (no new card) but needs re-processing (e.g., villain raised), `SetPotValue()` and `SetBetValues()` shall be called before invoking `ProcessTurnAsync()` or `ProcessRiverAsync()`.

#### Scenario: Turn re-processed after villain raise — pot updated
- **GIVEN** game loop is in TurnAction state
- **AND** no new card is detected (still 4 community cards)
- **AND** villain raised (maxBet changed since last processing)
- **WHEN** ProcessPostFlopAsync re-processes the turn
- **THEN** SetPotValue() SHALL be called before ProcessTurnAsync()
- **AND** _playerGameState.PotSize SHALL be > 0
- **AND** SPR SHALL be calculated correctly (not 0.0)

#### Scenario: River re-processed — pot updated
- **GIVEN** game loop is in RiverAction state
- **AND** no state change detected
- **WHEN** ProcessPostFlopAsync re-processes the river
- **THEN** SetPotValue() SHALL be called before ProcessRiverAsync()

#### Scenario: Normal turn processing — pot already updated
- **GIVEN** game loop transitions from FlopAction to TurnDetected
- **AND** new card detected
- **WHEN** ProcessTurnAsync is called normally
- **THEN** existing pot update flow SHALL continue to work (no regression)

### Requirement: numOpponents SHALL use Math.Max(1, ...) in all streets

#### Scenario: Turn with 0 active players detected
- **GIVEN** _playerGameState.Players.Count(p => p.Active) = 0
- **WHEN** numOpponents is calculated in ProcessTurnAsync
- **THEN** numOpponents SHALL be Math.Max(1, 0 - 1) = 1
- **AND** NOT -1 (which would cause errors)

#### Scenario: River with 1 active player
- **GIVEN** _playerGameState.Players.Count(p => p.Active) = 1
- **WHEN** numOpponents is calculated in ProcessRiverAsync
- **THEN** numOpponents SHALL be Math.Max(1, 1 - 1) = 1
- **AND** NOT 0

#### Scenario: Flop already uses Math.Max — no change
- **GIVEN** flop numOpponents calculation
- **WHEN** verified
- **THEN** it SHALL already have Math.Max(1, ...) (existing correct behavior)
