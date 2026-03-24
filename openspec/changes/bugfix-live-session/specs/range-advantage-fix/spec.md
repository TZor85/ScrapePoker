# Range Advantage Fix for Mixed Boards

## MODIFIED Requirements

### Requirement: HasRangeAdvantageOnBoard SHALL correctly identify boards that do NOT favor 3Bet range

In 3Bet pots, the preflop aggressor's range advantage depends on the board texture. Boards with A/K favor the 3Bet range (overpairs, TPTK). Low boards and mixed boards (one high card + low cards) favor the caller's range (pocket pairs, suited connectors).

#### Scenario: T-5-2 in 3Bet pot — NO range advantage
- **GIVEN** flopRanks = [10, 5, 2], situation = ThreeBet, isPreflopAggressor = true
- **WHEN** HasRangeAdvantageOnBoard is called
- **THEN** result SHALL be `false`
- **AND** because T-5-2 is a mixed low board where caller connects with Tx, 55, 22

#### Scenario: A-K-3 in 3Bet pot — YES range advantage
- **GIVEN** flopRanks = [14, 13, 3], situation = ThreeBet, isPreflopAggressor = true
- **WHEN** HasRangeAdvantageOnBoard is called
- **THEN** result SHALL be `true`
- **AND** because A/K present → 3Bet range has TPTK, overpairs

#### Scenario: Q-J-T in 3Bet pot — YES range advantage
- **GIVEN** flopRanks = [12, 11, 10], situation = ThreeBet, isPreflopAggressor = true
- **WHEN** HasRangeAdvantageOnBoard is called
- **THEN** result SHALL be `true`
- **AND** because 2+ high cards (Q, J) → 3Bet range connects well

#### Scenario: 7-5-3 in 3Bet pot — NO range advantage
- **GIVEN** flopRanks = [7, 5, 3], situation = ThreeBet, isPreflopAggressor = true
- **WHEN** HasRangeAdvantageOnBoard is called
- **THEN** result SHALL be `false`
- **AND** because all low → caller has suited connectors, pocket pairs

#### Scenario: K-8-4 in 3Bet pot — YES (King present)
- **GIVEN** flopRanks = [13, 8, 4], situation = ThreeBet, isPreflopAggressor = true
- **WHEN** HasRangeAdvantageOnBoard is called
- **THEN** result SHALL be `true`
- **AND** because King present → 3Bet range has AK, KK, KQs

#### Scenario: J-6-2 in 3Bet pot — NO range advantage
- **GIVEN** flopRanks = [11, 6, 2], situation = ThreeBet, isPreflopAggressor = true
- **AND** highCards = 0 (J=11 < 12), min rank = 2
- **WHEN** HasRangeAdvantageOnBoard is called
- **THEN** result SHALL be `false`
- **AND** because mixed board with no high cards and low min rank
