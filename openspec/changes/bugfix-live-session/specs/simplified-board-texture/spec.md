# Simplified Decision Board Texture

## MODIFIED Requirements

### Requirement: DetermineSimplifiedAction SHALL receive and adapt to board texture

The simplified decision path (RaiseOverLimper, `IsSimplified=true`) shall receive `boardTexture` as a parameter and adapt bet sizing for dangerous board textures (Monotone, Wet).

#### Scenario: RaiseOverLimper on Monotone board — smaller sizing
- **GIVEN** thresholds.IsSimplified = true
- **AND** boardTexture = "Monotone"
- **AND** equity > StrongValueAbove
- **AND** isInPosition = false (OOP)
- **AND** isFacingBet = false
- **WHEN** DetermineSimplifiedAction is called
- **THEN** the bet size SHALL be "Bet 1/4 (Value)"
- **AND** NOT the default SimplifiedOOPStrongBet ("Bet 3/4 (Value)")
- **AND** because monotone boards require smaller sizing to avoid inflating pot

#### Scenario: RaiseOverLimper on Wet board — reduced sizing
- **GIVEN** thresholds.IsSimplified = true
- **AND** boardTexture = "Wet"
- **AND** equity > StrongValueAbove
- **AND** isInPosition = true (IP)
- **WHEN** DetermineSimplifiedAction is called
- **THEN** the bet size SHALL be "Bet 1/3 (Value)"

#### Scenario: RaiseOverLimper on Dry board — default sizing
- **GIVEN** thresholds.IsSimplified = true
- **AND** boardTexture = "Dry"
- **AND** equity > StrongValueAbove
- **AND** isInPosition = true
- **WHEN** DetermineSimplifiedAction is called
- **THEN** the bet size SHALL be SimplifiedIPStrongBet (default)
- **AND** because Dry boards are safe, standard sizing applies

#### Scenario: Facing bet — sizing unchanged regardless of texture
- **GIVEN** thresholds.IsSimplified = true
- **AND** isFacingBet = true
- **AND** any boardTexture
- **WHEN** DetermineSimplifiedAction is called
- **THEN** facing bet actions (Raise/Call/Fold) SHALL NOT change based on texture
- **AND** because facing bet decisions are about hand strength vs opponent's range

#### Scenario: DetermineAction propagates boardTexture to simplified path
- **GIVEN** thresholds.IsSimplified = true
- **WHEN** DetermineAction calls DetermineSimplifiedAction
- **THEN** it SHALL pass `boardTexture` from DetermineAction's parameter
