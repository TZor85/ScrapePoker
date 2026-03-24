# Monotone Board Sizing

## ADDED Requirements

### Requirement: Monotone boards SHALL have specific bet sizing

When the board texture is "Monotone" (3+ cards of the same suit), HandleNoBet shall use `thresholds.MonotoneBoardBetSize` (default "Bet 1/4") instead of falling through to DryBoardBetSize. Smaller sizing is correct because flush draws are very likely in villain's range on monotone boards.

#### Scenario: NoBet on Monotone board uses 1/4 pot sizing
- **GIVEN** hero is not facing a bet
- **AND** boardTexture = "Monotone"
- **AND** equity > ThinValueAbove
- **WHEN** HandleNoBet determines baseBetThreshold
- **THEN** baseBetThreshold SHALL be thresholds.MonotoneBoardBetSize ("Bet 1/4")

#### Scenario: NoBet on Dry board unchanged
- **GIVEN** hero is not facing a bet
- **AND** boardTexture = "Dry"
- **WHEN** HandleNoBet determines baseBetThreshold
- **THEN** baseBetThreshold SHALL be thresholds.DryBoardBetSize (unchanged behavior)

#### Scenario: NoBet on Coordinated board unchanged
- **GIVEN** hero is not facing a bet
- **AND** boardTexture = "Coordinated"
- **WHEN** HandleNoBet determines baseBetThreshold
- **THEN** baseBetThreshold SHALL be thresholds.CoordinatedBoardBetSize (unchanged behavior)

#### Scenario: MonotoneBoardBetSize default value
- **GIVEN** a StreetThresholds with no explicit MonotoneBoardBetSize
- **WHEN** the property is accessed
- **THEN** it SHALL default to "Bet 1/4"

### Requirement: BoardTextureAnalyzer SHALL classify monotone boards as "Monotone"

When 3 or more community cards share the same suit, the board texture category shall be "Monotone" rather than a wetness-based classification.

#### Scenario: Three cards same suit on flop
- **GIVEN** community cards are Ah 5h 9h (three hearts)
- **WHEN** BoardTextureAnalyzer.Analyze is called
- **THEN** Category SHALL be "Monotone"

#### Scenario: Four cards same suit on turn
- **GIVEN** community cards are Ah 5h 9h 2h (four hearts)
- **WHEN** BoardTextureAnalyzer.Analyze is called
- **THEN** Category SHALL be "Monotone"

#### Scenario: Two-tone board is NOT monotone
- **GIVEN** community cards are Ah 5h 9c (two hearts, one club)
- **WHEN** BoardTextureAnalyzer.Analyze is called
- **THEN** Category SHALL NOT be "Monotone"
