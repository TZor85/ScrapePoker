# Villain Stack Extraction

## MODIFIED Requirements

### Requirement: UnifiedPokerCalculator SHALL receive real villain stack from Players

The 4 calls to `UnifiedPokerCalculator.Calculate()` in `FrmMain.cs` shall pass the actual villain stack instead of `villainStack: 0`. The villain stack is extracted as the maximum stack among active opponent players.

#### Scenario: Single active villain with stack 1500
- **GIVEN** `_playerGameState.Players` contains one active player with `Stack = 1500`
- **WHEN** `GetVillainStack()` is called
- **THEN** the result SHALL be `1500`
- **AND** this value SHALL be passed as `villainStack` to `Calculate()`

#### Scenario: Multiple active villains — use max stack
- **GIVEN** `_playerGameState.Players` contains active players with stacks 800, 1200, 500
- **WHEN** `GetVillainStack()` is called
- **THEN** the result SHALL be `1200` (maximum of active villain stacks)

#### Scenario: No active villains — return 0
- **GIVEN** `_playerGameState.Players` has no active players (all folded)
- **WHEN** `GetVillainStack()` is called
- **THEN** the result SHALL be `0`
- **AND** `UnifiedPokerCalculator` SHALL handle `villainStack = 0` gracefully (existing behavior)

#### Scenario: Effective stack SPR calculation works with real villain stack
- **GIVEN** heroStack = 2000, villainStack = 800, potSize = 400
- **WHEN** `UnifiedPokerCalculator` calculates SPR with `villainStack > 0`
- **THEN** effective stack SHALL be `Math.Min(2000, 800) = 800`
- **AND** SPR SHALL be `800 / 400 = 2.0`
- **AND** because the villain's shorter stack limits the effective stack

#### Scenario: Villain stack passed in all 4 call points
- **GIVEN** a hand is being played
- **WHEN** `Calculate()` is called for preflop, flop (turn), turn, or river
- **THEN** `villainStack` SHALL be `GetVillainStack()` (not `0`)
- **AND** specifically at lines ~1265, ~1580, ~1658, ~1935 in `FrmMain.cs`

#### Scenario: GetPotOddsCalculator also receives real villain stack
- **GIVEN** the system calculates pot odds during detection loop
- **WHEN** `Calculate()` is called from `GetPotOddsCalculator` (line ~1935)
- **THEN** `villainStack` SHALL be `GetVillainStack()`
- **AND** because pot odds also benefit from accurate SPR context
