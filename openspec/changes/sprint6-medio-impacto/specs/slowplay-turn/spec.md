# Slowplay Extended to Turn

## MODIFIED Requirements

### Requirement: Slowplay SHALL be allowed on the Turn with additional conditions

The slowplay check (currently Flop only) shall be extended to the Turn. On the Turn, slowplay requires the same base conditions (Dry board, ThreeOfAKind+, not aggressor, not multiway) plus the villain must be LAG or Unknown (aggressive opponents are more likely to bet if hero checks).

#### Scenario: Slowplay turn with ThreeOfAKind on Dry board vs LAG
- **GIVEN** street = Turn
- **AND** boardTexture = "Dry"
- **AND** heroHandRank = ThreeOfAKind
- **AND** equity >= SlowPlayMinEquity (72%)
- **AND** !isMultiway
- **AND** !heroIsAggressor
- **AND** villainType = LAG
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL be "Check" with reason containing "Slow play" and "turn"

#### Scenario: Slowplay turn vs Unknown villain (default)
- **GIVEN** street = Turn
- **AND** boardTexture = "Dry"
- **AND** heroHandRank = FullHouse
- **AND** equity >= SlowPlayMinEquity
- **AND** villainType = Unknown
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL be "Check" with reason containing "Slow play"
- **AND** because Unknown villains may still bet, making slowplay viable

#### Scenario: No slowplay turn vs TP (nit)
- **GIVEN** street = Turn
- **AND** boardTexture = "Dry"
- **AND** heroHandRank = ThreeOfAKind
- **AND** equity >= SlowPlayMinEquity
- **AND** villainType = TP (nit — rarely bets without nuts)
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL NOT be a slowplay check
- **AND** because nits won't bet, so checking loses value

#### Scenario: No slowplay turn vs TAG
- **GIVEN** street = Turn
- **AND** all slowplay conditions met
- **AND** villainType = TAG
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL NOT be a slowplay check
- **AND** because TAG may not bet enough to justify slowplay

#### Scenario: Flop slowplay unchanged (no villainType required)
- **GIVEN** street = Flop
- **AND** boardTexture = "Dry"
- **AND** heroHandRank = ThreeOfAKind
- **AND** equity >= SlowPlayMinEquity
- **AND** !heroIsAggressor, !isMultiway
- **AND** any villainType (including TP)
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL be "Check" with slowplay
- **AND** because flop slowplay does NOT require specific villainType

#### Scenario: No slowplay on river (never)
- **GIVEN** street = River
- **AND** all other slowplay conditions met
- **AND** villainType = LAG
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL NOT be a slowplay check
- **AND** because river is the last street — always bet for value with nuts

#### Scenario: No slowplay turn on Coordinated board
- **GIVEN** street = Turn
- **AND** boardTexture = "Coordinated"
- **AND** heroHandRank = ThreeOfAKind
- **AND** villainType = LAG
- **WHEN** HandleNoBet evaluates the hand
- **THEN** the result SHALL NOT be a slowplay check
- **AND** because coordinated boards have too many draws to give free cards
