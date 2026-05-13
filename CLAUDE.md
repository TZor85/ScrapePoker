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
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj

# Single test by name
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "Name~TestHacenEscalera"

# Tests with coverage
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --collect:"XPlat Code Coverage"

# Format
dotnet format OpenScrape.sln
dotnet format --verify-no-changes OpenScrape.sln

# Benchmarks
dotnet run --project src/Tools/BenchmarkSuite1/BenchmarkSuite1.csproj

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
5. **OpenScrape.App** — WinForms UI and composition root. `Program.cs` wires DI via Host builder with `DOTNET_ENVIRONMENT` (defaults to `"Development"`). Key services: `OcrService` (Tesseract OCR with confidence scoring + bounded caching), `ScreenReaderService` (multi-read consensus, preprocessing, OCR normalization), `TableLayoutService` (dealer detection, player states, position assignment, alias reading), `GameCoordinator` (decision pipeline, helpers, board analysis), `ColorDetectionService`, `ImageCropperService`, `GameLoopStateMachine` (with board card validation), `GameLoggerService`, `RegionLookupCache` (O(1) region lookups), `CardCacheService` (singleton lazy card loading). Forms: `FrmMain` (main window, 5 tabs: Juego/Config/Tablas/Logs/Historial with Backtest A/B button), `FrmOverlay` (table overlay with 9 rows + action panel + street indicator), `FrmHandDetail` (hand history popup with colored RichTextBox), `FrmDetectionDebug`.

**Data flow:** Screen capture → Image preprocessing (OpenCvSharp/SkiaSharp) → ScreenReaderService (OCR + normalization) → TableLayoutService (player detection) → Domain model → GameCoordinator → Decision engine (equity calculation, hand evaluation) → Action recommendation.

**DI pattern:** `FrmMain` is resolved from a scoped `ServiceProvider` (not root) because it depends on scoped use cases. All DecisionMaker services are singletons. Algorithms use forwarding pattern (concrete + interface factory → single shared instance). Database sessions use `await using` per operation (no long-lived sessions). Extracted services: `ScreenReaderService` (singleton, OCR reads), `TableLayoutService` (scoped, player/dealer/position state), `GameCoordinator` (scoped, decision pipeline). `FrmMain` passes `Image` from UI controls to services as parameter. Singletons: `RegionLookupCache` (O(1) region lookups), `CardCacheService` (lazy card loading, thread-safe).

## Configuration & Secrets

- **`appsettings.json`** — Contains strategy config, thresholds, and placeholder credentials (`CHANGE_ME`). Safe to commit.
- **`appsettings.Development.json`** — Contains real database credentials and encryption key. Gitignored, never committed.
- **`launchSettings.json`** — Sets `DOTNET_ENVIRONMENT=Development` for Visual Studio launches.
- Environment variable `DOTNET_ENVIRONMENT` controls which appsettings override file is loaded. Defaults to `"Development"` for this desktop app.

## Decision Engine (DecisionMaker)

Entry point: `IPokerCalculator` → `UnifiedPokerCalculator`. Equity pipeline: pot odds → raw equity (Monte Carlo/exact enumeration with adaptive villain ranges) → outs/draws (with tainted outs + combo draw detection) → hand evaluation (HandRank + KickerStrength) → fold equity → EV. `IPokerCalculator.Calculate` accepts optional `OpponentProfile` to adapt villain ranges based on observed VPIP/3Bet%.

**Algorithms:**
- `BitHandEvaluator` — Hand strength ranking via bit-manipulation (zero-alloc, stackalloc). `EvaluateHandScore()` returns lightweight `HandScore` struct for MC.
- `MonteCarloSimulator` — Equity calculation: exact enumeration on river (C(45,2)=990) and turn (45×C(44,2)≈42K), MC simulation on flop (50K iters) and preflop (30K). `HandScore` struct eliminates heap allocations. Villain range weighted selection with skip-on-block (20 retry attempts, `SkippedSimulations` tracking).
- `PreflopEquityCalculator` — Preflop equity lookup
- `OutsCalculator` — Draw detection, outs counting, tainted outs, combo draw detection
- `BoardTextureAnalyzer` — 5-category wetness scoring (Dry <15, SemiDry 15-35, SemiWet 35-60, Wet 60+, Paired) and board change detection (`AnalyzeBoardChange()`) across streets
- `StrategyBacktester` — Replays historical hands against current engine, compares decisions, estimates BB/100 impact.

**PostflopDecisionService — Eight+ decision paths:**
1. **Facing Bet** → Call/Raise/Fold. Raise only with TwoPair+ (OnePair → call, OnePair no raise en board con flush posible sin blocker). Underbet (< 15% pot, penalty 0) → raise con equity buena. Bet-size penalties (Underbet 0, Small+1, Medium+4, Large+8; VillainAggro+3). Agresor vs donk: FoldBelow−5, raise with strong hand (cross-street: HeroBetFlop/Turn = agresor). Caller vs cbet: FoldBelow+2. Pot commitment: SPR < 0.5 + EV(call) > 0 → Call.
2. **No Bet** → Check/Bet with board-texture sizing (Dry/Coordinated/Paired/Monotone/Wet). Hand strength relative adjusts thresholds. Overbet on dry boards (flop/turn: aggressor); river: TwoPair+ NUTS en **cualquier textura**. River merged sizing: OnePair → ReduceBetSize, TwoPair+ → normal/polarizado. River danger board (3+ same suit sin blocker) → sizing reducido. Turn vulnerability sizing: OnePair en Wet/Coordinated → ReduceBetSize. Card removal: heroBlocksTopCard → value sizing mayor. Bet sizing adjusted by SPR. Double barrel condicionado al runout (bad runout → check).
3. **Check-Raise** → OOP: TwoPair+ O draws fuertes (combo draw/flush draw 9+ outs, CheckRaiseDrawMinEquity=40). IP: TwoPair+ trap en board no Wet/Monotone. !heroIsAggressor + !multiway. Active on all streets.
4. **Float Exit** → heroFloatedFlop + turn + villain check + !multiway → Bet 1/2 (float exit).
5. **Probe Bet** → Villain aggressor checked previous street + !multiway + equity >= ProbeBetMinEquity. IP: Bet 1/2, OOP: Bet 1/3.
6. **Pot Control** → Turn equity 40-55% + Coordinated/Wet/Monotone + !heroIsAggressor → check-back.
7. **Delayed Value** → River + HeroCheckedAllStreets + OnePair TopPair+ → Bet 1/3 (delayed value).
8. **Low Equity** → Semi-bluff con fold equity check (breakevenFE ajustado por draw equity). Bluff puro con fold equity ≥ breakeven. Pot odds marginales. Bluff catching con ajuste por: villainType (LAG ×0.80, TP ×1.20), runout (brick ×0.85, scare ×1.15), card removal (heroBlocksTopCard ×0.90), blocker bonus (×0.85).
9. **Randomización** → Equity dentro de ±3% de ThinValueAbove → check adaptativo por villainType (LAG 85%, LP 80%, TAG 60%, TP 55%, Unknown 70%).
10. **C-Bet** → Agresor preflop con equity baja (< FoldBelow dentro de -15) → c-bet a frecuencia propia (Flop 65%, Turn 45%, River 30%), prioridad sobre bluff genérico. C-bet mixing: agresor con equity media [FoldBelow, ThinValueAbove) chequea a frecuencia (1-cbetFreq) para proteger checking range (solo HU).

**Additional decision modifiers:**
- `heroIsAggressor` — cross-street: incluye HeroBetFlop/HeroBetTurn (no solo preflop)
- `heroKickerStrength` — TPTK (Strong) → sizing mayor + FoldBelow -3 facing bet, TPWK (Weak) OOP → check + FoldBelow +2 facing bet
- `heroBlocksTopCard` — hero tiene carta que matchea top board card → bluff catch ×0.90, value sizing mayor
- `hasComboDraw` — flush+straight draw gets +6 equity bonus, only if HandRank < Straight
- `villainBarreling` + `villainCheckedMiddleStreet` — bet-bet vs bet-check-bet differentiation
- Range narrowing — villain apostó en 2+ calles → FoldBelow +3/calle (rango más estrecho)
- SPR push/fold — Interpolación suave: factor = 1 - spr/threshold (no buckets discretos). isPushFold solo con SPR < 1.0. `CalculateAllinEV`: `equity×(pot+stack) - (1-equity)×stack`.
- Reverse implied odds — turn/river facing bet, OnePair/TwoPair, draw-heavy board, blocker reduction. Desactivado si villain all-in.
- Board texture per situation — 3bet pot: range advantage con 1+ carta alta (Q, K, A)
- Tainted outs — flush draw ×0.7, sin flush ×0.3
- 3-bet/4-bet pot adjustment — ThreeBet/Squeeze: FoldBelow +5, ThinValue +3. FourBet: FoldBelow +8, ThinValue +5.
- Multiway penalty — IP: lineal. OOP: cuadrático (n² × penalty × 0.5). Street multiplier: turn ×1.2, river ×1.4.
- Float exit abort — Verifica runout antes de bet. Bad runout (overcard/flush/straight complete) → Check.
- All-in detection — `IsAnyoneAllIn` → foldEquity=0, reverseImplied=0.
- Check-raise SPR guard — SPR < 1.5 + equity < 60% → skip check-raise.
- effectiveEquity floor — Math.Max(0, effectiveEquity) tras penalizaciones.
- Cross-street state — `PostflopGameContext`: VillainBet/HeroBet per street, VillainBetSize, FloatedFlop, TurnCalledWithFlushDanger, HeroCheckedAllStreets, IsAnyoneAllIn. Reset incondicional al detectar nueva mano.
- VillainRange — ajustado por posición villain (EP ×0.7, BTN ×1.3), HandSituation, y OpponentProfile (VPIP escala ancho [0.5x-2.0x], 3Bet% ajusta rangos 3bet)
- OpponentProfile — AF por posición (AggressionFactorIP/OOP), `GetTypeForPosition(bool villainIsIP)`, stat-specific reliability (`HasReliableCBetData`≥5, `HasReliableAFData`≥10, `HasReliableFoldData`≥8)
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
- `effectiveEquity = equity - dangerPenalty + comboDrawBonus`, then cap, then `- reverseImpliedPenalty`, then `Math.Max(0, ...)`.
- Never folds without facing bet → Check instead.
- Auto-rebuy detection: `_heroStackPreRebuy` tracks stack during active hand, ignores sudden increases from auto-rebuy to 100BB.

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

Includes `ForceState()` for test/debug mode (validates state exists in transition map), `MaxOcrRetries` for OCR failure handling, `TryTransition(state, visibleBoardCards)` overload that validates card count vs expected street (FlopDetected≥3, TurnDetected≥4, RiverDetected≥5), and `lock(_stateLock)` for thread-safe state transitions.

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
- **NUnit** — Testing framework (645+ tests, no mocking framework)

## Code Style

- .NET 10.0, nullable reference types enabled, implicit usings enabled
- File-scoped namespaces, records for DTOs/value objects, primary constructors for simple DI
- Allman braces, 4-space indentation, max 120 chars per line
- Private fields: `_camelCase`. Booleans: `is`/`has`/`can`/`should` prefix
- Using groups: System → third-party → OpenScrape.*
- Conventional commits in Spanish

## Specifications (openspec/)

Spec-driven development via `openspec/changes/`. Each change has: `proposal.md` (why/what/capabilities/impact), `design.md` (layout, data flow, DTOs), `tasks.md` (implementation steps), and `specs/*/spec.md` (BDD-style requirements with scenarios). Pending specs: `login-sistema-licencias` (license system), `bankroll-dashboard` (bankroll tracking UI). Archived specs (9) in `openspec/archived/`.


---

# Reversa

> Framework de Engenharia Reversa instalado neste projeto.

## Como usar

Digite `/reversa` para ativar o Reversa e iniciar ou retomar a análise do projeto.

## Comportamento ao ativar

Quando o usuário digitar `/reversa` ou a palavra `reversa` sozinha em uma mensagem:

1. Ative o skill `reversa` disponível em `.claude/skills/reversa/SKILL.md`
2. Se não encontrar em `.claude/skills/`, tente `.agents/skills/reversa/SKILL.md`
3. Leia o SKILL.md na íntegra e siga exatamente as instruções do Reversa

## Regra não-negociável

Nunca apague, modifique ou sobrescreva arquivos pré-existentes do projeto legado.
O Reversa escreve **apenas** em `.reversa/` e `_reversa_sdd/`.
