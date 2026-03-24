# Wet Board Sizing

## MODIFIED Requirements

### Requirement: Boards with Wet category SHALL map to "Wet" (not "Coordinated")

`SimplifiedTexture` shall distinguish between SemiWet boards (35-60 wetness → "Coordinated") and Wet boards (60+ wetness → "Wet"). Wet boards have more draws and require smaller sizing to avoid inflating the pot against ranges with real equity.

#### Scenario: Wet board (60+ wetness) returns "Wet"
- **GIVEN** a board with WetnessScore >= 60
- **AND** Category = BoardTextureCategory.Wet
- **AND** IsMonotone = false, IsPaired = false
- **WHEN** SimplifiedTexture is evaluated
- **THEN** result SHALL be "Wet"

#### Scenario: SemiWet board remains "Coordinated"
- **GIVEN** a board with WetnessScore between 35 and 60
- **AND** Category = BoardTextureCategory.SemiWet
- **WHEN** SimplifiedTexture is evaluated
- **THEN** result SHALL be "Coordinated" (unchanged)

#### Scenario: Dry board unchanged
- **GIVEN** a board with WetnessScore < 15
- **AND** Category = BoardTextureCategory.Dry
- **WHEN** SimplifiedTexture is evaluated
- **THEN** result SHALL be "Dry" (unchanged)

#### Scenario: Monotone takes priority over Wet
- **GIVEN** a board with IsMonotone = true
- **AND** Category = BoardTextureCategory.Wet
- **WHEN** SimplifiedTexture is evaluated
- **THEN** result SHALL be "Monotone" (monotone priority preserved)

### Requirement: HandleNoBet SHALL use WetBoardBetSize for Wet boards

When the board texture is "Wet", the base bet size shall use `thresholds.WetBoardBetSize` (default "Bet 1/3") instead of `CoordinatedBoardBetSize`.

#### Scenario: NoBet on Wet board uses 1/3 pot sizing
- **GIVEN** hero is not facing a bet
- **AND** boardTexture = "Wet"
- **AND** equity > ThinValueAbove
- **WHEN** HandleNoBet determines baseBetThreshold
- **THEN** baseBetThreshold SHALL be thresholds.WetBoardBetSize ("Bet 1/3")

#### Scenario: NoBet on Coordinated board unchanged
- **GIVEN** hero is not facing a bet
- **AND** boardTexture = "Coordinated" (SemiWet)
- **WHEN** HandleNoBet determines baseBetThreshold
- **THEN** baseBetThreshold SHALL be thresholds.CoordinatedBoardBetSize (unchanged)

#### Scenario: WetBoardBetSize default value
- **GIVEN** a StreetThresholds with no explicit WetBoardBetSize
- **WHEN** the property is accessed
- **THEN** it SHALL default to "Bet 1/3"
