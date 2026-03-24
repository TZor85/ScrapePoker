# Name OCR Robustness

## MODIFIED Requirements

### Requirement: Player names SHALL be read with reduced threshold and consensus

The OCR reading for player names shall use a lower threshold (0.80 max instead of 0.87) and a two-read consensus mechanism to reduce errors.

#### Scenario: Two reads agree — high confidence result
- **GIVEN** region for player name at p3Name
- **AND** read with umbral 0.80 returns "PlayerA"
- **AND** read with inactiveUmbral 0.30 returns "PlayerA"
- **WHEN** ReadPlayerNameOCR is called
- **THEN** result SHALL be "PlayerA"
- **AND** because both reads agree

#### Scenario: Only first read succeeds — use it
- **GIVEN** read with umbral 0.80 returns "PlayerA"
- **AND** read with inactiveUmbral 0.30 returns ""
- **WHEN** ReadPlayerNameOCR is called
- **THEN** result SHALL be "PlayerA"

#### Scenario: Only second read succeeds — use it
- **GIVEN** read with umbral 0.80 returns ""
- **AND** read with inactiveUmbral 0.30 returns "PlayerA"
- **WHEN** ReadPlayerNameOCR is called
- **THEN** result SHALL be "PlayerA"

#### Scenario: Reads disagree — use longer name
- **GIVEN** read with umbral 0.80 returns "Play"
- **AND** read with inactiveUmbral 0.30 returns "PlayerA"
- **WHEN** ReadPlayerNameOCR is called
- **THEN** result SHALL be "PlayerA" (longer = more likely correct)

#### Scenario: Both reads empty — empty result
- **GIVEN** both reads return ""
- **WHEN** ReadPlayerNameOCR is called
- **THEN** result SHALL be ""

### Requirement: Player names with empty alias SHALL be retried each game loop iteration

#### Scenario: Name fails first read, succeeds on retry
- **GIVEN** player P3 is active with Alias = "" (OCR failed on hand start)
- **WHEN** RetryEmptyAliases runs on next game loop iteration
- **AND** ReadPlayerNameOCR returns "PlayerA"
- **THEN** P3.Alias SHALL be set to "PlayerA"
- **AND** RegisterSeatAlias("P3", "PlayerA") SHALL be called

#### Scenario: Name already set — not re-read
- **GIVEN** player P3 has Alias = "PlayerA" (already read)
- **WHEN** RetryEmptyAliases runs
- **THEN** ReadPlayerNameOCR SHALL NOT be called for P3
- **AND** because only empty aliases are retried

#### Scenario: Inactive player — not retried
- **GIVEN** player P3 has Active = false and Alias = ""
- **WHEN** RetryEmptyAliases runs
- **THEN** ReadPlayerNameOCR SHALL NOT be called for P3
