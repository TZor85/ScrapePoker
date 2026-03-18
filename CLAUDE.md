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
5. **OpenScrape.App** — WinForms UI and composition root. `Program.cs` wires DI via Host builder with `DOTNET_ENVIRONMENT` (defaults to `"Development"`). Key services: `OcrService` (Tesseract OCR with bounded caching), `ColorDetectionService`, `ImageCropperService`, `GameLoopStateMachine`, `GameLoggerService`. Forms: `FrmMain` (main window), `FrmOverlay` (table overlay), `FrmDetectionDebug`.

**Data flow:** Screen capture → Image preprocessing (OpenCvSharp/SkiaSharp) → OCR (Tesseract) → Domain model → Decision engine (equity calculation, hand evaluation) → Action recommendation.

**DI pattern:** `FrmMain` is resolved from a scoped `ServiceProvider` (not root) because it depends on scoped use cases. All DecisionMaker services are singletons.

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

**PostflopDecisionService — Five decision paths:**
1. **Facing Bet** → Call/Raise/Fold. Raise only with TwoPair+ (OnePair → call even with high equity). Bet-size penalties (Small+1, Medium+4, Large+8; VillainAggro+3). Agresor vs donk: FoldBelow−5, raise with strong hand. Caller vs cbet: FoldBelow+2.
2. **No Bet** → Check/Bet with board-texture sizing (Dry/Coordinated/Paired). Hand strength relative adjusts thresholds (nuts −4 to −8, vulnerable +2 to +4). Overbet on dry boards as aggressor (1.25x pot).
3. **Check-Raise** → OOP + equity > CheckRaiseThreshold + HandRank >= TwoPair + !heroIsAggressor + !multiway. Returns `IsCheckRaise=true`.
4. **Probe Bet** → Villain aggressor checked previous street + hero OOP + !multiway + equity >= ProbeBetMinEquity → Bet 1/3 (probe). Cross-street state via `_villainAggressorCheckedFlop`.
5. **Low Equity** → Semi-bluff with combo draw sizing (12+ outs on flop → 3/4 pot), implied odds, pot odds marginal calls.

**Additional decision modifiers:**
- `heroIsAggressor` / `heroHandRank` / `heroKickerStrength` — affect raise/call/sizing decisions
- `hasComboDraw` — flush+straight draw gets +6 equity bonus (ComboDrawEquityBonus)
- Board texture per situation — 3bet pot aggressor keeps range advantage on low boards (overpairs)
- Tainted outs — outs that also improve villain discounted ×0.5 (`EffectiveOuts`)

**Danger card penalty system:**
- Percentage penalties (proportional): FlushComplete = equity×25% (requires 4+ same suit on board), StraightComplete = equity×18%
- Flat penalties: BoardPaired −5, Overcard −3, FlushDraw −5 (3 same suit on board)
- FacingBetMultiplier ×1.4 (villain represents completed draw)
- Hero blocker effect: penalty ×0.5 if hero holds danger suit
- NoBet cap: `DangerCompletedDrawNoBetCap=45` (no value bet on completed draw board)
- Danger propagation: turn `_lastBoardChange` carries to river via `CombineBoardChanges()`
- `effectiveEquity = equity - dangerPenalty + comboDrawBonus`, then cap if applicable
- Never folds without facing bet → Check instead

## Game State Machine

`GameLoopStateMachine` — 10 states with enforced valid transitions:

```
WaitingForHand → HandDetected → PreflopAction → FlopDetected → FlopAction →
TurnDetected → TurnAction → RiverDetected → RiverAction → HandComplete → (loop)
```

Properties `IsFlop`, `IsTurn`, `IsRiver` are derived from `CurrentState` (not separate booleans). Includes `ForceState()` for test/debug mode and `MaxOcrRetries` for OCR failure handling.

## Strategy Configuration

Three-tier hierarchy, all via `IOptions<StrategyProfile>` from `appsettings.json`:

1. **StrategyProfile** (global) — Fold equity base/adjustments, bet sizing multipliers (SPR-based, board texture, position), bluff frequencies (flop/turn/river), all 8 danger penalty parameters.
2. **StreetThresholds** (per situation) — 30 configs (10 Flop + 10 Turn + 10 River). Key format: `"{BoardPosition}_{HandSituation}"` (e.g., `"Flop_OpenRaise"`, `"Turn_OpenRaise"`). Contains equity tiers (FoldBelow, ThinValueAbove, ValueAbove, StrongValueAbove), board-texture bet sizes, bluff controls (CanBluff, BluffFrequencyMultiplier, BluffCondition), position handling (ThinValueIPOnly, ThinValueOOPFallback), check-raise (CanCheckRaise, CheckRaiseThreshold), overbet (CanOverbet, OverbetBetSize, OverbetMinEquity), combo draw sizing (ComboDrawBetSize, ComboDrawOutsThreshold), probe bet (CanProbeBet, ProbeBetSize, ProbeBetMinEquity).
3. **Simplified mode** (RaiseOverLimper) — `IsSimplified=true` skips board texture analysis, uses fixed IP/OOP bet sizing.

JSON strategy files in `src/OpenScrape.App/Data/`: `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json`, `tableMap.json`.

## Persistence & Logging

- `GameRound` entity stores hand data; `StreetDecision` value object logs per-street decisions (equity%, action, reason, bet sizing)
- `GameLoggerService` persists to Marten (PostgreSQL) and writes to `tbResume` UI control (Logs tab)
- Log format: structured blocks per street with `═══ [FLOP/TURN/RIVER] ═══` separator, showing: cards (hero + board), pot/bet/stack/SPR, situation/position/opponents, equity pipeline, hand rank, board texture, draws, and final decision with tags ([CHECK-RAISE], [BLUFF], [BARREL])
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
- **NUnit** — Testing framework (266 tests, no mocking framework)

## Code Style

- .NET 10.0, nullable reference types enabled, implicit usings enabled
- File-scoped namespaces, records for DTOs/value objects, primary constructors for simple DI
- Allman braces, 4-space indentation, max 120 chars per line
- Private fields: `_camelCase`. Booleans: `is`/`has`/`can`/`should` prefix
- Using groups: System → third-party → OpenScrape.*
- Conventional commits in Spanish
