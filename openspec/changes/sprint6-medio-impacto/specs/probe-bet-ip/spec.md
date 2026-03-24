# Probe Bet IP

## MODIFIED Requirements

### Requirement: Probe bet SHALL be allowed when hero is in position

The probe bet condition shall no longer require `!isInPosition`. When hero is IP and the villain aggressor checked the previous street, hero can lead with a probe bet using `ProbeBetIPSize` (default "Bet 1/2").

#### Scenario: Probe bet IP with sizing "Bet 1/2"
- **GIVEN** thresholds.CanProbeBet = true
- **AND** villainAggressorCheckedPreviousStreet = true
- **AND** isInPosition = true
- **AND** !isMultiway
- **AND** equity >= thresholds.ProbeBetMinEquity
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL be a probe bet
- **AND** the sizing SHALL be thresholds.ProbeBetIPSize ("Bet 1/2")
- **AND** the reason SHALL contain "Probe bet IP"

#### Scenario: Probe bet OOP with sizing "Bet 1/3" (unchanged)
- **GIVEN** thresholds.CanProbeBet = true
- **AND** villainAggressorCheckedPreviousStreet = true
- **AND** isInPosition = false
- **AND** !isMultiway
- **AND** equity >= thresholds.ProbeBetMinEquity
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL be a probe bet
- **AND** the sizing SHALL be thresholds.ProbeBetSize ("Bet 1/3")
- **AND** the reason SHALL contain "Probe bet OOP"

#### Scenario: IP probe bet sizing is larger than OOP
- **GIVEN** default ProbeBetIPSize = "Bet 1/2" and ProbeBetSize = "Bet 1/3"
- **WHEN** comparing IP vs OOP probe sizing
- **THEN** IP size SHALL be larger (1/2 > 1/3)
- **AND** because IP allows more aggression with position advantage

#### Scenario: Probe bet still blocked in multiway
- **GIVEN** isMultiway = true
- **AND** isInPosition = true
- **AND** all other probe conditions met
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL NOT be a probe bet
- **AND** because probe bets are less effective multiway

#### Scenario: ProbeBetIPSize default value
- **GIVEN** a StreetThresholds with no explicit ProbeBetIPSize
- **WHEN** the property is accessed
- **THEN** it SHALL default to "Bet 1/2"
