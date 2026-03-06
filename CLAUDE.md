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

1. **OpenScrape.Domain** — Core entities (`Table`, `Card`, `RegionTableMap`), value objects (`Hand`, `Region`, `CardDataOuts`), enums (`Positions`, `JugadasEnum`), DTOs, mappers. No external dependencies.
2. **OpenScrape.Features** — CQRS use cases via MediatR. Organized by feature: `Table/`, `Cards/`, `ActionScenario/`, `RegionsTableMap/`. `Services.cs` registers all use cases.
3. **OpenScrape.Infrastructure** — Marten (PostgreSQL document DB) setup. `Services.cs` configures the document store.
4. **OpenScrape.DecisionMaker** — Poker algorithms: `HandEvaluator`, `MonteCarloSimulator`, `OutsCalculator`, `PreflopEquityCalculator`, `EquityCalculatorService`, `BetSizingService`. All registered as singletons. `IPokerCalculator` → `UnifiedPokerCalculator` is the main entry point.
5. **OpenScrape.App** — WinForms UI and composition root. `Program.cs` wires DI via Host builder. Key services: `OcrService` (Tesseract OCR with caching), `ColorDetectionService`, `ImageCropperService`. Forms: `FrmMain` (main window), `FrmOverlay` (table overlay), `FormAction`.

**Data flow:** Screen capture → Image preprocessing (OpenCvSharp/SkiaSharp) → OCR (Tesseract) → Domain model → Decision engine (equity calculation, hand evaluation) → Action recommendation.

## Key Dependencies

- **Marten** — PostgreSQL document database
- **MediatR** — CQRS command/query dispatch
- **Tesseract** — OCR engine (eng.traineddata)
- **OpenCvSharp4 / Emgu.CV / SkiaSharp** — Image processing
- **Ardalis.Result** — Result pattern
- **NUnit** — Testing framework

## Strategy Configuration

JSON files in `src/OpenScrape.App/Data/` define poker strategies: `OpenRaise.json`, `BBvsSB.json`, `ThreeBet.json`, `VsThreeBet.json`, `Squeeze.json`, `tableMap.json`. Poker parameters (bluff frequencies, etc.) are in `appsettings.json`.

## Code Style

- .NET 10.0, nullable reference types enabled, implicit usings enabled
- File-scoped namespaces, records for DTOs/value objects, primary constructors for simple DI
- Allman braces, 4-space indentation, max 120 chars per line
- Private fields: `_camelCase`. Booleans: `is`/`has`/`can`/`should` prefix
- Using groups: System → third-party → OpenScrape.*
- Conventional commits in Spanish
