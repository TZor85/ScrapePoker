# Hero Blocker Granular

## MODIFIED Requirements

### Requirement: DangerPenaltyCalculator SHALL differentiate nut blocker from non-nut blocker

When hero blocks the danger suit, the penalty reduction shall depend on whether hero has the nut blocker (Ace of the completed suit) or a non-nut blocker (lower card of that suit). Nut blockers eliminate the strongest villain combo.

#### Scenario: Nut blocker (Ace of danger suit)
- **GIVEN** heroBlocksDangerSuit = true
- **AND** heroHasNutBlocker = true (hero has Ace of the completed flush suit)
- **AND** boardChange.FlushCompleted = true
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** penalty SHALL be multiplied by `DangerNutBlockerReduction` (0.35)
- **AND** because Ace removes the nut flush from villain's range

#### Scenario: Non-nut blocker (lower card of danger suit)
- **GIVEN** heroBlocksDangerSuit = true
- **AND** heroHasNutBlocker = false (hero has 5s of the completed flush suit, not Ace)
- **AND** boardChange.FlushCompleted = true
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** penalty SHALL be multiplied by `DangerNonNutBlockerReduction` (0.55)

#### Scenario: Nut blocker reduction is stronger than non-nut
- **GIVEN** same equity and board change
- **WHEN** comparing nut blocker vs non-nut blocker penalties
- **THEN** nut blocker penalty SHALL be less than non-nut blocker penalty
- **AND** DangerNutBlockerReduction (0.35) < DangerNonNutBlockerReduction (0.55)

#### Scenario: No blocker — uses existing reduction (backward compatible)
- **GIVEN** heroBlocksDangerSuit = false
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** no blocker reduction SHALL be applied
- **AND** existing behavior preserved

#### Scenario: Board with 4+ cards of flush suit — blocker less relevant
- **GIVEN** heroBlocksDangerSuit = true
- **AND** board has 4+ cards of the completed flush suit
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** penalty SHALL be multiplied by `DangerBlockerBoard4FlushReduction` (0.7)
- **AND** this overrides nut/non-nut distinction
- **AND** because with 4 suited cards on board, villain almost certainly has flush

#### Scenario: heroHasNutBlocker defaults to false
- **GIVEN** heroHasNutBlocker parameter is not passed (default)
- **AND** heroBlocksDangerSuit = true
- **WHEN** DangerPenaltyCalculator.Calculate is called
- **THEN** penalty SHALL use `DangerNonNutBlockerReduction` (0.55) as fallback
