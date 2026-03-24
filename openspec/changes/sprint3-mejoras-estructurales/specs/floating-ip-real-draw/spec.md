# Floating IP Requires Real Draw

## MODIFIED Requirements

### Requirement: Floating IP SHALL require a real draw, not just overcards

The floating IP condition shall require `hasFlushDraw || hasComboDraw || totalOuts >= FloatingIPMinOuts` in addition to the existing `totalOuts >= 4`. This prevents -EV floats with hands that only have overcard outs and no draw potential.

#### Scenario: Float rejected — overcards only (4 outs, no draw)
- **GIVEN** hero is in position on the Flop
- **AND** villain bet size is Small
- **AND** heroHandRank = HighCard
- **AND** totalOuts = 4 (two overcards)
- **AND** hasFlushDraw = false
- **AND** hasComboDraw = false
- **AND** equity is within FloatingIPMinEquity..FloatingIPMaxEquity
- **WHEN** HandleFacingBet evaluates the hand
- **THEN** the result SHALL NOT be a Float IP call

#### Scenario: Float accepted — flush draw present
- **GIVEN** hero is in position on the Flop
- **AND** villain bet size is Small
- **AND** heroHandRank = HighCard
- **AND** totalOuts = 9 (flush draw)
- **AND** hasFlushDraw = true
- **AND** equity is within FloatingIPMinEquity..FloatingIPMaxEquity
- **WHEN** HandleFacingBet evaluates the hand
- **THEN** the result SHALL be "Call" with reason containing "Float IP"

#### Scenario: Float accepted — combo draw present
- **GIVEN** hero is in position on the Flop
- **AND** villain bet size is Small
- **AND** heroHandRank = HighCard
- **AND** totalOuts = 15 (flush draw + OESD)
- **AND** hasComboDraw = true
- **AND** equity is within FloatingIPMinEquity..FloatingIPMaxEquity
- **WHEN** HandleFacingBet evaluates the hand
- **THEN** the result SHALL be "Call" with reason containing "Float IP"

#### Scenario: Float accepted — 6+ outs without named draw
- **GIVEN** hero is in position on the Flop
- **AND** villain bet size is Small
- **AND** heroHandRank = HighCard
- **AND** totalOuts = 8 (OESD)
- **AND** hasFlushDraw = false
- **AND** hasComboDraw = false
- **AND** equity is within FloatingIPMinEquity..FloatingIPMaxEquity
- **WHEN** HandleFacingBet evaluates the hand
- **THEN** the result SHALL be "Call" with reason containing "Float IP"
- **AND** because totalOuts (8) >= FloatingIPMinOuts (6)

#### Scenario: Float rejected — OOP (existing condition preserved)
- **GIVEN** hero is NOT in position
- **AND** all other floating conditions are met including hasRealDraw
- **WHEN** HandleFacingBet evaluates the hand
- **THEN** the result SHALL NOT be a Float IP call
