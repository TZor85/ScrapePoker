# Name OCR Cleanup

## ADDED Requirements

### Requirement: CleanOcrPlayerName SHALL sanitize OCR output for player names

A static method shall clean raw OCR text: trim whitespace, remove non-alphanumeric characters (except `_`, `-`, space), trim leading/trailing underscores and dashes, and reject names shorter than 2 characters.

#### Scenario: Valid name passes through
- **GIVEN** rawName = "PlayerA"
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "PlayerA"

#### Scenario: Name with leading dots/special chars
- **GIVEN** rawName = "..Player_A"
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "Player_A"

#### Scenario: Single character rejected
- **GIVEN** rawName = "|"
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "" (empty — too short)

#### Scenario: Only underscores rejected
- **GIVEN** rawName = "___"
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "" (empty — only border chars)

#### Scenario: Whitespace trimmed
- **GIVEN** rawName = " Hero123 "
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "Hero123"

#### Scenario: Name with dashes and underscores preserved
- **GIVEN** rawName = "P3-name_1"
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "P3-name_1"

#### Scenario: Null or empty input
- **GIVEN** rawName = null or ""
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be ""

#### Scenario: Numbers only valid
- **GIVEN** rawName = "12345"
- **WHEN** CleanOcrPlayerName is called
- **THEN** result SHALL be "12345" (valid — 5 chars, all alphanumeric)
