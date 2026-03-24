# Villain Identity Tracking

## MODIFIED Requirements

### Requirement: GetActiveVillainId SHALL prefer alias over seat name

When identifying the active villain for OpponentTracker, the system shall use `player.Alias` (OCR name) when available. If alias is empty, fallback to `OpponentTracker.ResolveName(seatName)` (cached alias from previous hand). Last resort: seat name ("P3").

#### Scenario: Alias available — use alias
- **GIVEN** active villain has Name = "P3" and Alias = "PlayerA"
- **WHEN** GetActiveVillainId is called
- **THEN** result SHALL be "PlayerA"

#### Scenario: Alias empty, cached alias exists — use cached
- **GIVEN** active villain has Name = "P3" and Alias = ""
- **AND** OpponentTracker has cached "P3" → "PlayerA" from previous hand
- **WHEN** GetActiveVillainId is called
- **THEN** result SHALL be "PlayerA"

#### Scenario: No alias, no cache — use seat name
- **GIVEN** active villain has Name = "P3" and Alias = ""
- **AND** OpponentTracker has no cached alias for "P3"
- **WHEN** GetActiveVillainId is called
- **THEN** result SHALL be "P3"

### Requirement: OpponentTracker SHALL cache seat-to-alias mapping

#### Scenario: RegisterSeatAlias stores mapping
- **GIVEN** OpponentTracker has no mapping for "P3"
- **WHEN** RegisterSeatAlias("P3", "PlayerA") is called
- **THEN** ResolveName("P3") SHALL return "PlayerA"

#### Scenario: RegisterSeatAlias migrates existing profile
- **GIVEN** OpponentTracker has profile for "P3" with 10 hands played
- **AND** no profile for "PlayerA"
- **WHEN** RegisterSeatAlias("P3", "PlayerA") is called
- **THEN** profile "PlayerA" SHALL have 10 hands played
- **AND** profile "P3" SHALL no longer exist
- **AND** because stats accumulated under seat name transfer to real name

#### Scenario: RegisterSeatAlias with new player at same seat
- **GIVEN** _seatAliasCache has "P3" → "PlayerA"
- **AND** OpponentTracker has profile for "PlayerA"
- **WHEN** RegisterSeatAlias("P3", "PlayerB") is called
- **THEN** ResolveName("P3") SHALL return "PlayerB"
- **AND** profile "PlayerA" SHALL still exist (different player)
- **AND** profile "PlayerB" SHALL be created fresh

#### Scenario: ResolveName for unknown seat
- **GIVEN** no mapping for "P5"
- **WHEN** ResolveName("P5") is called
- **THEN** result SHALL be null

#### Scenario: Seat alias cache survives across hands
- **GIVEN** hand 1: RegisterSeatAlias("P3", "PlayerA")
- **AND** hand 2: _playerGameState is reset (new hand)
- **WHEN** ResolveName("P3") is called in hand 2
- **THEN** result SHALL be "PlayerA"
- **AND** because _seatAliasCache lives in OpponentTracker (singleton), not in PlayerGameState

#### Scenario: Reset clears seat alias cache
- **GIVEN** _seatAliasCache has "P3" → "PlayerA"
- **WHEN** OpponentTracker.Reset() is called (new session)
- **THEN** ResolveName("P3") SHALL return null
- **AND** because session-level data is cleared on reset
