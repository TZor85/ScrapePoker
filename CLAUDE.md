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

.NET 10.0 WinForms poker table scraping and decision-making bot. Clean Architecture with CQRS (MediatR).

**Five layers:**

1. **OpenScrape.Domain** — Core entities (`Table`, `Card`, `RegionTableMap`, `GameRound`, `StrategyProfile`), value objects (`Hand`, `Region`, `CardDataOuts`, `StreetThresholds`, `StreetDecision`, `BoardChangeResult`), enums (`BoardPosition`, `HandSituation`, `TablePosition`, `JugadasEnum`), DTOs, mappers. No external dependencies.
2. **OpenScrape.Features** — CQRS use cases via MediatR. Organized by feature: `Table/`, `Cards/`, `ActionScenario/`, `RegionsTableMap/`, `GameRound/`. `Services.cs` registers all use cases.
3. **OpenScrape.Infrastructure** — Marten (PostgreSQL document DB) setup. `Services.cs` configures the document store.
4. **OpenScrape.DecisionMaker** — Poker algorithms and decision services. See "Decision Engine" below.
5. **OpenScrape.App** — WinForms UI and composition root. `Program.cs` wires DI via Host builder. Key services: `OcrService` (Tesseract OCR with caching), `ColorDetectionService`, `ImageCropperService`, `GameLoopStateMachine`, `GameLoggerService`. Forms: `FrmMain` (main window), `FrmOverlay` (table overlay), `FrmDetectionDebug`.

**Data flow:** Screen capture → Image preprocessing (OpenCvSharp/SkiaSharp) → OCR (Tesseract) → Domain model → Decision engine (equity calculation, hand evaluation) → Action recommendation.

## Decision Engine (DecisionMaker)

Entry point: `IPokerCalculator` → `UnifiedPokerCalculator`. Equity pipeline: pot odds → raw equity (Monte Carlo 1000 iterations) → outs/draws → fold equity → EV.

**Algorithms:**
- `HandEvaluator` — Hand strength ranking
- `MonteCarloSimulator` — Postflop equity via simulation
- `PreflopEquityCalculator` — Preflop equity lookup
- `OutsCalculator` — Draw detection and outs counting
- `BoardTextureAnalyzer` — 5-category wetness scoring (Dry <15, SemiDry 15-35, SemiWet 35-60, Wet 60+, Paired) and board change detection (`AnalyzeBoardChange()`) across streets

**PostflopDecisionService — Three decision paths:**
1. **Facing Bet** → Call/Raise/Fold with bet-size penalties (Small+1, Medium+4, Large+8 over FoldBelow; VillainAggro+3)
2. **No Bet** → Check/Bet with board-texture sizing (Dry/Coordinated/Paired bet sizes)
3. **Low Equity** → Semi-bluff (only without facing bet), implied odds, pot odds marginal calls

**Danger card penalty system:**
- Percentage penalties (proportional): FlushComplete = equity×25%, StraightComplete = equity×18%
- Flat penalties: BoardPaired −5, Overcard −3, FlushDraw −5
- FacingBetMultiplier ×1.4 (villain represents completed draw)
- Hero blocker effect: penalty ×0.5 if hero holds danger suit
- NoBet cap: `DangerCompletedDrawNoBetCap=45` (no value bet on completed draw board)
- Danger propagation: turn `_lastBoardChange` carries to river via `CombineBoardChanges()`
- `effectiveEquity = equity - dangerPenalty`, then cap if applicable
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
2. **StreetThresholds** (per situation) — 20 configs (10 Turn + 10 River). Key format: `"{BoardPosition}_{HandSituation}"` (e.g., `"Turn_OpenRaise"`). Contains equity tiers (FoldBelow, ThinValueAbove, ValueAbove, StrongValueAbove), board-texture bet sizes, bluff controls (CanBluff, BluffFrequencyMultiplier, BluffCondition), position handling (ThinValueIPOnly, ThinValueOOPFallback).
3. **Simplified mode** (RaiseOverLimper) — `IsSimplified=true` skips board texture analysis, uses fixed IP/OOP bet sizing.

JSON strategy files in `src/OpenScrape.App/Data/`: `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json`, `tableMap.json`.

## Persistence & Logging

- `GameRound` entity stores hand data; `StreetDecision` value object logs per-street decisions (equity%, action, reason, bet sizing)
- `GameLoggerService` persists to Marten (PostgreSQL) and writes to `tbResume` UI control (Logs tab)
- Log format: `[TURN]`/`[RIVER]` + Equity, DangerLevel, Penalty, EffEquity, Decision
- `LogError()` writes to both tbResume and Console

## Key Enums

- `BoardPosition`: Hand, Flop, Turn, River (not "Street")
- `HandSituation`: OpenRaise, RaiseOverLimper, ThreeBet, OpenRaiseVs3Bet, FourBet, Squeeze, DonkBet, etc.
- `TablePosition`: Early, Middle, CutOff, Button, SmallBlind, BigBlind

## Key Dependencies

- **Marten** — PostgreSQL document database
- **MediatR** — CQRS command/query dispatch
- **Tesseract** — OCR engine (eng.traineddata)
- **OpenCvSharp4 / Emgu.CV / SkiaSharp** — Image processing
- **Ardalis.Result** — Result pattern
- **NUnit** — Testing framework (139 tests, no mocking framework)

## Code Style

- .NET 10.0, nullable reference types enabled, implicit usings enabled
- File-scoped namespaces, records for DTOs/value objects, primary constructors for simple DI
- Allman braces, 4-space indentation, max 120 chars per line
- Private fields: `_camelCase`. Booleans: `is`/`has`/`can`/`should` prefix
- Using groups: System → third-party → OpenScrape.*
- Conventional commits in Spanish
- All DecisionMaker services registered as singletons
