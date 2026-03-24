# Reverse Implied Odds with Blockers

## MODIFIED Requirements

### Requirement: CalculateReverseImpliedOdds SHALL reduce penalty when hero blocks danger suit

When hero holds a card of the danger suit (flush draw suit on board), the reverse implied odds penalty shall be reduced because the villain has fewer flush combinations.

#### Scenario: Hero blocks flush draw suit — penalty reduced
- **GIVEN** boardChange.FlushDrawAppeared = true
- **AND** hero does NOT have a flush draw (hasFlushDraw = false)
- **AND** heroBlocksDangerSuit = true
- **AND** heroHandRank = OnePair (eligible for reverse implied penalty)
- **AND** street = Turn, isFacingBet = true
- **WHEN** CalculateReverseImpliedOdds is called
- **THEN** penalty SHALL be multiplied by `ReverseImpliedBlockerReduction` (0.5)
- **AND** final penalty SHALL be less than without blocker

#### Scenario: Hero does not block — full penalty
- **GIVEN** boardChange.FlushDrawAppeared = true
- **AND** heroBlocksDangerSuit = false
- **AND** heroHandRank = OnePair
- **AND** street = Turn, isFacingBet = true
- **WHEN** CalculateReverseImpliedOdds is called
- **THEN** penalty SHALL NOT be reduced by blocker
- **AND** penalty SHALL be `ReverseImpliedFlushDrawPenalty` × PairClassification multiplier

#### Scenario: Hero blocks but has flush draw — no reverse implied penalty
- **GIVEN** boardChange.FlushDrawAppeared = true
- **AND** heroBlocksDangerSuit = true
- **AND** hasFlushDraw = true (hero IS drawing to flush)
- **WHEN** CalculateReverseImpliedOdds is called
- **THEN** flush draw penalty SHALL NOT be applied
- **AND** because hero having the flush draw means improving, not losing

#### Scenario: No board danger — no penalty regardless of blocker
- **GIVEN** boardChange.FlushDrawAppeared = false
- **AND** boardChange.DangerLevel < 2
- **AND** heroBlocksDangerSuit = true
- **WHEN** CalculateReverseImpliedOdds is called
- **THEN** penalty SHALL be 0
- **AND** blocker is irrelevant when no danger exists

#### Scenario: Blocker reduction applies before river reduction
- **GIVEN** heroBlocksDangerSuit = true
- **AND** street = River
- **AND** base penalty = 7.0 (flush draw)
- **WHEN** CalculateReverseImpliedOdds is called
- **THEN** penalty SHALL be 7.0 × PairMultiplier × BlockerReduction(0.5) × RiverReduction(0.6)
