# Bluff Catch Turn

## ADDED Requirements

### Requirement: Bluff catching SHALL be available on the Turn with stricter conditions

HandleLowEquity shall allow bluff catching on the Turn in addition to the River. Turn bluff catch requires stricter conditions: only Small bet, MiddlePair or better, and a higher equity multiplier (0.90 vs 0.75 for River).

#### Scenario: Turn bluff catch with MiddlePair and small bet
- **GIVEN** street is Turn
- **AND** hero has OnePair with pairClassification = MiddlePair
- **AND** villain bet size is Small
- **AND** equity >= FoldBelow × BluffCatchTurnEquityMultiplier (0.90)
- **AND** pairClassification != BoardPaired
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL be "Call" with reason containing "bluff catch turn"

#### Scenario: Turn bluff catch rejected — BottomPair
- **GIVEN** street is Turn
- **AND** hero has OnePair with pairClassification = BottomPair
- **AND** villain bet size is Small
- **AND** equity >= FoldBelow × BluffCatchTurnEquityMultiplier
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL NOT be a bluff catch call (BottomPair too risky with 1 street remaining)

#### Scenario: Turn bluff catch rejected — Medium bet
- **GIVEN** street is Turn
- **AND** hero has OnePair with pairClassification = MiddlePair
- **AND** villain bet size is Medium
- **AND** equity >= FoldBelow × BluffCatchTurnEquityMultiplier
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL NOT be a bluff catch call (only Small bet allowed on Turn)

#### Scenario: Turn bluff catch rejected — equity too low
- **GIVEN** street is Turn
- **AND** hero has OnePair with pairClassification = TopPair
- **AND** villain bet size is Small
- **AND** equity < FoldBelow × BluffCatchTurnEquityMultiplier (0.90)
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL NOT be a bluff catch call

#### Scenario: River bluff catch unchanged
- **GIVEN** street is River
- **AND** hero has OnePair with pairClassification = BottomPair
- **AND** villain bet size is Small
- **AND** equity >= FoldBelow × BluffCatchFoldBelowMultiplier (0.75)
- **AND** !hasBlocker
- **WHEN** HandleLowEquity evaluates the hand
- **THEN** the result SHALL be "Call" with bluffCatchThreshold × 1.15 (BottomPair without blocker)
- **AND** the existing River bluff catch logic SHALL remain unchanged

#### Scenario: BluffCatchTurnEquityMultiplier validation
- **GIVEN** a StrategyProfile with BluffCatchTurnEquityMultiplier = 0
- **WHEN** Validate() is called
- **THEN** errors SHALL contain a message about BluffCatchTurnEquityMultiplier being out of range (0, 1]
