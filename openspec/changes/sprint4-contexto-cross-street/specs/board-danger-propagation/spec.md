# Board Danger Propagation from Flop

## ADDED Requirements

### Requirement: BoardTextureAnalyzer SHALL analyze the flop as an initial danger state

A new method `AnalyzeInitialBoard()` shall generate a `BoardChangeResult` representing the base danger of the flop. This is NOT a "change" (no card was added) but an initial state describing what draws are already present.

#### Scenario: 2-tone flop (flush draw present)
- **GIVEN** community cards on flop are Ah Qh 3d (two hearts)
- **WHEN** `AnalyzeInitialBoard([14,12,3], [1,1,2])` is called
- **THEN** `FlushDrawAppeared` SHALL be `true`
- **AND** `CompletedFlushSuit` SHALL be the suit of hearts (1)
- **AND** `FlushCompleted` SHALL be `false` (cannot complete flush with only 3 cards)
- **AND** `DangerLevel` SHALL be >= 1

#### Scenario: Monotone flop (flush already possible)
- **GIVEN** community cards on flop are Ah 5h 9h (three hearts)
- **WHEN** `AnalyzeInitialBoard([14,5,9], [1,1,1])` is called
- **THEN** `FlushDrawAppeared` SHALL be `true`
- **AND** `DangerLevel` SHALL be >= 3 (monotone is very dangerous)

#### Scenario: Rainbow flop (no flush draw)
- **GIVEN** community cards on flop are 2c 7d Ks (all different suits)
- **WHEN** `AnalyzeInitialBoard([2,7,13], [1,2,3])` is called
- **THEN** `FlushDrawAppeared` SHALL be `false`
- **AND** `DangerLevel` SHALL be 0 (if also not connected or paired)

#### Scenario: Paired flop
- **GIVEN** community cards on flop are 8c 8d Ks
- **WHEN** `AnalyzeInitialBoard([8,8,13], [1,2,3])` is called
- **THEN** `BoardPaired` SHALL be `true`
- **AND** `DangerLevel` SHALL be >= 1

#### Scenario: Connected flop (straight draw present)
- **GIVEN** community cards on flop are Jh Td 9c (connected)
- **WHEN** `AnalyzeInitialBoard([11,10,9], [1,2,3])` is called
- **THEN** `DangerLevel` SHALL be >= 1 (straight draw is present)

#### Scenario: Flop never sets FlushCompleted or StraightCompleted
- **GIVEN** any community cards on flop
- **WHEN** `AnalyzeInitialBoard()` is called
- **THEN** `FlushCompleted` SHALL always be `false`
- **AND** `StraightCompleted` SHALL always be `false`
- **AND** because a draw cannot "complete" on the initial deal

### Requirement: Flop initial danger SHALL propagate to turn and river

The `BoardChangeResult` from `AnalyzeInitialBoard()` shall be stored in `PostflopGameContext.InitialBoardDanger` and combined with turn's `BoardChangeResult` via `CombineBoardChanges()`.

#### Scenario: 2-tone flop danger propagates to turn
- **GIVEN** flop was Ah Qh 3d (InitialBoardDanger: FlushDrawAppeared=true, DangerLevel=1)
- **AND** turn card is 5s (turnChange: DangerLevel=0, no new danger)
- **WHEN** `CombineBoardChanges(InitialBoardDanger, turnChange)` is called
- **THEN** the result SHALL have `FlushDrawAppeared=true`
- **AND** `DangerLevel` SHALL be >= 1 (flop danger preserved)

#### Scenario: 2-tone flop + flush completed on turn = accumulated danger
- **GIVEN** flop was Ah Qh 3d (InitialBoardDanger: FlushDrawAppeared=true, DangerLevel=1)
- **AND** turn card is 7h (turnChange: FlushCompleted=true, DangerLevel=4)
- **WHEN** `CombineBoardChanges(InitialBoardDanger, turnChange)` is called
- **THEN** the result SHALL have `FlushDrawAppeared=true` AND `FlushCompleted=true`
- **AND** `DangerLevel` SHALL be >= 5 (1 from flop + 4 from turn completion)

#### Scenario: Rainbow flop does not add turn danger
- **GIVEN** flop was 2c 7d Ks (InitialBoardDanger: DangerLevel=0)
- **AND** turn card is Ah (turnChange: OvercardAppeared=true, DangerLevel=1)
- **WHEN** `CombineBoardChanges(InitialBoardDanger, turnChange)` is called
- **THEN** the result SHALL have `DangerLevel=1` (only turn danger, flop was safe)

#### Scenario: Accumulated danger from flop reaches river
- **GIVEN** flop had InitialBoardDanger with DangerLevel=2
- **AND** turn had turnChange with DangerLevel=1
- **AND** `LastBoardChange` after turn = combined DangerLevel=3
- **AND** river card adds riverChange with DangerLevel=1
- **WHEN** `CombineBoardChanges(LastBoardChange, riverChange)` is called
- **THEN** `DangerLevel` SHALL be 4 (accumulated across all 3 streets)

#### Scenario: InitialBoardDanger resets on new hand
- **GIVEN** a hand has completed with `InitialBoardDanger.DangerLevel = 3`
- **WHEN** `PostflopGameContext.Reset()` is called
- **THEN** `InitialBoardDanger` SHALL be `BoardChangeResult.Safe`
- **AND** `InitialBoardDanger.DangerLevel` SHALL be 0
