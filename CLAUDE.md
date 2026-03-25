# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Language

All responses, code comments, commit messages, and documentation must be in **Castellano (Spanish)**.

## Build & Test Commands

```bash
# Build
dotnet build OpenScrape.sln
dotnet build OpenScrape.sln --configuration Release
dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln

# Tests
dotnet test OpenScrape.sln
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj

# Single test by name
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "Name~TestHacenEscalera"

# Tests with coverage
dotnet test OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --collect:"XPlat Code Coverage"

# Format
dotnet format OpenScrape.sln
dotnet format --verify-no-changes OpenScrape.sln

# Benchmarks
dotnet run --project BenchmarkSuite1/BenchmarkSuite1.csproj

# Publish
dotnet publish src/OpenScrape.App/OpenScrape.App.csproj --configuration Release --runtime win-x64 --self-contained
```

## Architecture

.NET 10.0 WinForms poker table scraping and decision-making bot. Clean Architecture with scoped use cases (no MediatR pipeline).

**Five layers:**

1. **OpenScrape.Domain** — Core entities (`Table`, `Card`, `RegionTableMap`, `GameRound`, `StrategyProfile`), value objects (`Hand`, `Region`, `CardDataOuts`, `StreetThresholds`, `StreetDecision`, `BoardChangeResult`), enums (`BoardPosition`, `HandSituation`, `TablePosition`, `JugadasEnum`), DTOs, mappers. No external dependencies.
2. **OpenScrape.Features** — Scoped use cases organized by feature: `Table/`, `Cards/`, `ActionScenario/`, `RegionsTableMap/`, `GameRound/`. `Services.cs` registers all use cases. Uses `Ardalis.Result` for return types.
3. **OpenScrape.Infrastructure** — Marten (PostgreSQL document DB) setup. `Services.cs` configures the document store.
4. **OpenScrape.DecisionMaker** — Poker algorithms and decision services. See "Decision Engine" below.
5. **OpenScrape.App** — WinForms UI and composition root. `Program.cs` wires DI via Host builder with `DOTNET_ENVIRONMENT` (defaults to `"Development"`). Key services: `OcrService` (Tesseract OCR with bounded caching), `ColorDetectionService`, `ImageCropperService`, `GameLoopStateMachine`, `GameLoggerService`. Forms: `FrmMain` (main window, 5 tabs: Juego/Config/Tablas/Logs/Historial), `FrmOverlay` (table overlay with 9 rows + action panel + street indicator), `FrmHandDetail` (hand history popup with colored RichTextBox), `FrmDetectionDebug`.

**Data flow:** Screen capture → Image preprocessing (OpenCvSharp/SkiaSharp) → OCR (Tesseract) → Domain model → Decision engine (equity calculation, hand evaluation) → Action recommendation.

**DI pattern:** `FrmMain` is resolved from a scoped `ServiceProvider` (not root) because it depends on scoped use cases. All DecisionMaker services are singletons. Algorithms use forwarding pattern (concrete + interface factory → single shared instance). Database sessions use `using` per operation (no long-lived sessions).

## Configuration & Secrets

- **`appsettings.json`** — Contains strategy config, thresholds, and placeholder credentials (`CHANGE_ME`). Safe to commit.
- **`appsettings.Development.json`** — Contains real database credentials and encryption key. Gitignored, never committed.
- **`launchSettings.json`** — Sets `DOTNET_ENVIRONMENT=Development` for Visual Studio launches.
- Environment variable `DOTNET_ENVIRONMENT` controls which appsettings override file is loaded. Defaults to `"Development"` for this desktop app.

## Decision Engine (DecisionMaker)

Entry point: `IPokerCalculator` → `UnifiedPokerCalculator`. Equity pipeline: pot odds → raw equity (Monte Carlo 1000 iterations) → outs/draws (with tainted outs + combo draw detection) → hand evaluation (HandRank + KickerStrength) → fold equity → EV.

**Algorithms:**
- `HandEvaluator` — Hand strength ranking
- `MonteCarloSimulator` — Postflop equity via simulation
- `PreflopEquityCalculator` — Preflop equity lookup
- `OutsCalculator` — Draw detection, outs counting, tainted outs, combo draw detection
- `BoardTextureAnalyzer` — 5-category wetness scoring (Dry <15, SemiDry 15-35, SemiWet 35-60, Wet 60+, Paired) and board change detection (`AnalyzeBoardChange()`) across streets

**PostflopDecisionService — Eight+ decision paths:**
1. **Facing Bet** → Call/Raise/Fold. Raise only with TwoPair+ (OnePair → call, OnePair no raise en board con flush posible sin blocker). Underbet (< 15% pot, penalty 0) → raise con equity buena. Bet-size penalties (Underbet 0, Small+1, Medium+4, Large+8; VillainAggro+3). Agresor vs donk: FoldBelow−5, raise with strong hand (cross-street: HeroBetFlop/Turn = agresor). Caller vs cbet: FoldBelow+2. Pot commitment: SPR < 0.5 + EV(call) > 0 → Call.
2. **No Bet** → Check/Bet with board-texture sizing (Dry/Coordinated/Paired/Monotone/Wet). Hand strength relative adjusts thresholds. Overbet on dry boards (flop/turn: aggressor); river: TwoPair+ NUTS en **cualquier textura**. River merged sizing: OnePair → ReduceBetSize, TwoPair+ → normal/polarizado. River danger board (3+ same suit sin blocker) → sizing reducido. Turn vulnerability sizing: OnePair en Wet/Coordinated → ReduceBetSize. Card removal: heroBlocksTopCard → value sizing mayor. Bet sizing adjusted by SPR. Double barrel condicionado al runout (bad runout → check).
3. **Check-Raise** → OOP: TwoPair+ O draws fuertes (combo draw/flush draw 9+ outs, CheckRaiseDrawMinEquity=40). IP: TwoPair+ trap en board no Wet/Monotone. !heroIsAggressor + !multiway. Active on all streets.
4. **Float Exit** → heroFloatedFlop + turn + villain check + !multiway → Bet 1/2 (float exit).
5. **Probe Bet** → Villain aggressor checked previous street + !multiway + equity >= ProbeBetMinEquity. IP: Bet 1/2, OOP: Bet 1/3.
6. **Pot Control** → Turn equity 40-55% + Coordinated/Wet/Monotone + !heroIsAggressor → check-back.
7. **Delayed Value** → River + HeroCheckedAllStreets + OnePair TopPair+ → Bet 1/3 (delayed value).
8. **Low Equity** → Semi-bluff con fold equity check (breakevenFE ajustado por draw equity). Bluff puro con fold equity ≥ breakeven. Pot odds marginales. Bluff catching con ajuste por: villainType (LAG ×0.80, TP ×1.20), runout (brick ×0.85, scare ×1.15), card removal (heroBlocksTopCard ×0.90), blocker bonus (×0.85).
9. **Randomización** → Equity dentro de ±3% de ThinValueAbove → 30% check (anti-exploit).

**Additional decision modifiers:**
- `heroIsAggressor` — cross-street: incluye HeroBetFlop/HeroBetTurn (no solo preflop)
- `heroKickerStrength` — TPTK (Strong) → sizing mayor, TPWK (Weak) OOP → check
- `heroBlocksTopCard` — hero tiene carta que matchea top board card → bluff catch ×0.90, value sizing mayor
- `hasComboDraw` — flush+straight draw gets +6 equity bonus, only if HandRank < Straight
- `villainBarreling` + `villainCheckedMiddleStreet` — bet-bet vs bet-check-bet differentiation
- Range narrowing — villain apostó en 2+ calles → FoldBelow +3/calle (rango más estrecho)
- SPR push/fold — SPR < 2: EV(allin) explícito. SPR > 4: FoldBelow+3. Only turn/river.
- Reverse implied odds — turn/river facing bet, OnePair/TwoPair, draw-heavy board, blocker reduction
- Board texture per situation — 3bet pot: range advantage con 1+ carta alta (Q, K, A)
- Tainted outs — flush draw ×0.7, sin flush ×0.3
- Cross-street state — `PostflopGameContext`: VillainBet/HeroBet per street, VillainBetSize, FloatedFlop, TurnCalledWithFlushDanger, HeroCheckedAllStreets
- VillainRange — ajustado por posición villain (EP ×0.7, BTN ×1.3) y HandSituation
- Fold equity — stats reales OpponentTracker (GetFoldToBetPct) o fallback multipliers estáticos
- Implied odds — ajustadas por numOpponents (OOP multiway peor, IP con draw mejor)
- DonkBet detection — cross-street: HeroBetFlop/Turn activa donk bet en turn/river

**Danger card penalty system:**
- Percentage penalties (proportional, Math.Max not sum): FlushComplete = equity×35%, StraightComplete = equity×18%.
- FlushDraw (3 same suit): equity × 8% × streetMultiplier (proporcional, no flat). Reducido con mano fuerte (OnePair ×0.75, TwoPair+ ×0.5).
- Flat penalties: BoardPaired −5, Overcard −3.
- Street multipliers: Flop ×1.3, Turn ×1.0, River ×0.8.
- FacingBetMultiplier ×1.4.
- Hero blocker effect granular: nut ×0.35, non-nut ×0.55, board4flush ×0.7.
- NoBet cap: `DangerCompletedDrawNoBetCap=45` (skipped if hero has completed draw).
- `dangerousFlushBoard`: flop solo monotone (DangerLevel >= 3), turn/river FlushDrawAppeared. Afecta raise decisions (OnePair no raise).
- Turn-river plan: `TurnCalledWithFlushDanger` → river check si flush completa.
- `effectiveEquity = equity - dangerPenalty + comboDrawBonus`, then cap, then `- reverseImpliedPenalty`.
- Never folds without facing bet → Check instead.

## Game State Machine

`GameLoopStateMachine` — 10 states with enforced valid transitions:

```
WaitingForHand → HandDetected → PreflopAction → FlopDetected → FlopAction →
TurnDetected → TurnAction → RiverDetected → RiverAction → HandComplete → (loop)
```

Properties `IsFlop`, `IsTurn`, `IsRiver` cover both `*Detected` and `*Action` states (e.g., `IsFlop` = `FlopDetected || FlopAction`). `FrmMain` uses these combined properties for street detection, while `ProcessPostFlopAsync` checks `CurrentState` directly for `*Detected` to avoid re-processing.

**Street detection in real play:**
- **Flop**: `ShouldCaptureFlop` in detection loop → `TryTransition(FlopDetected)`
- **Turn/River**: `IsBoardCardVisible("Card4"/"Card5")` in `ProcessPostFlopAsync` checks board card regions via image hash comparison (>80% confidence threshold) to detect new street cards. When in `FlopAction`/`TurnAction` and next card is visible → transitions to `TurnDetected`/`RiverDetected`. If no new card → reprocesses current street with updated bet info (villain raise scenario).
- **Dealer detection**: `SetDealerPlayer()` retries on each loop iteration while `Position == None`, allowing detection even if first capture misses the dealer button.

Includes `ForceState()` for test/debug mode and `MaxOcrRetries` for OCR failure handling.

## Strategy Configuration

Three-tier hierarchy, all via `IOptions<StrategyProfile>` from `appsettings.json`:

1. **StrategyProfile** (global) — Fold equity base/adjustments, bet sizing multipliers (SPR-based, board texture, position), bluff frequencies (flop/turn/river), all 8 danger penalty parameters, barrel detection penalties, SPR push/fold thresholds, reverse implied odds penalties, bluff catch multiplier, combo draw equity bonus.
2. **StreetThresholds** (per situation) — 30+ configs (10 Flop + 12 Turn + 12 River). Key format: `"{BoardPosition}_{HandSituation}"` (e.g., `"Flop_OpenRaise"`, `"Turn_OpenRaise"`). Contains equity tiers (FoldBelow, ThinValueAbove, ValueAbove, StrongValueAbove), board-texture bet sizes, bluff controls (CanBluff, BluffFrequencyMultiplier, BluffCondition), position handling (ThinValueIPOnly, ThinValueOOPFallback), check-raise (CanCheckRaise, CheckRaiseThreshold), overbet (CanOverbet, OverbetBetSize, OverbetMinEquity), combo draw sizing (ComboDrawBetSize, ComboDrawOutsThreshold), probe bet (CanProbeBet, ProbeBetSize, ProbeBetMinEquity), double barrel (CanDoubleBarrel).
3. **Simplified mode** (RaiseOverLimper) — `IsSimplified=true` skips board texture analysis, uses fixed IP/OOP bet sizing.

JSON strategy files in `src/OpenScrape.App/Data/`: `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json`, `tableMap.json`.

## Persistence & Logging

- **`GameSession`** — Marten document, one per table session. Contains SessionId, TableName, StartTime, EndTime, BigBlind. Keeps last 20 `HandRecord` in memory; older hands already persisted individually. Computed properties: TotalHands, TotalProfit, BBPer100.
- **`HandRecord`** — Marten document, one per hand. FK `GameSessionId` → `GameSession.Id`. Stores hero cards, position, stack start/end, board cards (flop/turn/river), `List<StreetDecision>`, result (`HandResult`: Won/Lost/Push/Unknown), situation, opponents count.
- **`StreetDecision`** — Record (value object) with: Street, EquityPercent, PotOddsPercent, ExpectedValue, RecommendedAction, ActionTaken, PotSizeAtDecision, BetSize, Situation, IsInPosition, and optional: Reason, BoardTexture, TotalOuts, SPR.
- **`GameLoggerService`** — Manages session/hand lifecycle: `StartSessionAsync` → `StartNewHandAsync` → `LogStreetDecision` → `EndHand` → `SaveSessionAsync`. Persists to Marten (PostgreSQL). Query methods: `GetRecentSessionsWithStatsAsync`, `GetHandsForSessionAsync`. Note: `EndHand` must receive the hero stack **before** `PlayerGameState` is reset (use `prevHeroStack`).
- **Historial tab** — `dgvSessions` (sessions with stats) + `dgvSessionHands` (hands per session). Double-click on a hand opens `FrmHandDetail` popup with colored Hand History.
- **Logs tab** — `tbResume` TextBox with structured blocks per street (`═══ [FLOP/TURN/RIVER] ═══`), showing equity pipeline, hand rank, board texture, draws, and final decision with tags ([CHECK-RAISE], [BLUFF], [BARREL]).
- `LogError()` writes to both tbResume and Console; `LogDebug()` writes only to Console (dealer, positions, OCR readings)

## Key Enums

- `BoardPosition`: Hand, Flop, Turn, River (not "Street")
- `HandSituation`: OpenRaise, RaiseOverLimper, ThreeBet, OpenRaiseVs3Bet, FourBet, Squeeze, DonkBet, DonkBetVsOpenRaise, etc.
- `TablePosition`: Early, Middle, CutOff, Button, SmallBlind, BigBlind
- `HandRank`: HighCard, OnePair, TwoPair, ThreeOfAKind, Straight, Flush, FullHouse, FourOfAKind, StraightFlush, RoyalFlush
- `KickerStrength`: None, Weak, Medium, Strong (top pair kicker classification)

## Key Dependencies

- **Marten 8.24.0** — PostgreSQL document database
- **Tesseract** — OCR engine (eng.traineddata)
- **OpenCvSharp4 / SkiaSharp** — Image processing
- **Ardalis.Result** — Result pattern (used in Features layer)
- **NUnit** — Testing framework (392+ tests, no mocking framework)

## Code Style

- .NET 10.0, nullable reference types enabled, implicit usings enabled
- File-scoped namespaces, records for DTOs/value objects, primary constructors for simple DI
- Allman braces, 4-space indentation, max 120 chars per line
- Private fields: `_camelCase`. Booleans: `is`/`has`/`can`/`should` prefix
- Using groups: System → third-party → OpenScrape.*
- Conventional commits in Spanish

## Specifications (openspec/)

Spec-driven development via `openspec/changes/`. Each change has: `proposal.md` (why/what/capabilities/impact), `design.md` (layout, data flow, DTOs), `tasks.md` (implementation steps), and `specs/*/spec.md` (BDD-style requirements with scenarios). Current specs: `login-sistema-licencias` (license system), `historial-sesiones-manos` (session/hand history tab), `bugfix-decision-engine` (5 bugfixes: bluff condition, danger penalty Math.Max, combo draw bonus guard, river probe bet, calibration).
