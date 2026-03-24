# Empty and SitOut State Separation

## MODIFIED Requirements

### Requirement: Empty and SitOut SHALL be independent states

An empty seat (no player) and a sitout player (seated but not playing) are distinct states. SetEmptyPlayer shall NOT set SitOut, and SetSitOutPlayer shall NOT set Empty.

#### Scenario: Empty seat detected — only Empty is true
- **GIVEN** color detection matches empty color for seat P3
- **WHEN** SetEmptyPlayer processes P3
- **THEN** P3.Empty SHALL be `true`
- **AND** P3.SitOut SHALL be `false`
- **AND** because empty seat means no player is seated

#### Scenario: SitOut detected — only SitOut is true
- **GIVEN** OCR detects text containing "SIT" for seat P3
- **AND** P3 is not Empty and not Active
- **WHEN** SetSitOutPlayer processes P3
- **THEN** P3.SitOut SHALL be `true`
- **AND** P3.Empty SHALL be `false`
- **AND** because sitout player is seated and may return

#### Scenario: Active player — neither Empty nor SitOut
- **GIVEN** color detection matches playing color for seat P3
- **WHEN** SetActivePlayer processes P3
- **THEN** P3.Active SHALL be `true`
- **AND** P3.Empty SHALL be `false`
- **AND** P3.SitOut SHALL be `false`

#### Scenario: Player goes from Active to SitOut mid-session
- **GIVEN** P3 was Active=true in previous iteration
- **AND** now OCR detects "SIT" and playing color is absent
- **WHEN** SetSitOutPlayer and SetActivePlayer process P3
- **THEN** P3.SitOut SHALL be `true`
- **AND** P3.Active SHALL be `false`
- **AND** P3.Empty SHALL be `false`
- **AND** because player is still seated, just not playing

#### Scenario: Existing code filtering Empty OR SitOut still works
- **GIVEN** code uses `player.Empty || player.SitOut` to exclude non-players
- **WHEN** filtering for position assignment
- **THEN** both Empty seats AND SitOut players SHALL be excluded
- **AND** no regression in SetDealerPlayer or SetVillainPosition
