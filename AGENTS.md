# AGENTS.md - Coding Guidelines for OpenScrape Poker Bot

This document provides coding guidelines, build commands, and development practices for the OpenScrape poker bot codebase. Follow these guidelines when making changes to ensure consistency and maintainability.

## Communication Guidelines

- **Language**: All responses must be in Castellano (Spanish). This includes code comments, commit messages, documentation, and any communication related to the project.

## Build Commands

### Full Build
```bash
dotnet build OpenScrape.sln
```

### Release Build
```bash
dotnet build OpenScrape.sln --configuration Release
```

### Clean Build
```bash
dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
```

### Build Specific Project
```bash
dotnet build src/OpenScrape.App/OpenScrape.App.csproj
dotnet build src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj
dotnet build src/OpenScrape.Domain/OpenScrape.Domain.csproj
```

### Publish Application
```bash
dotnet publish src/OpenScrape.App/OpenScrape.App.csproj --configuration Release --runtime win-x64 --self-contained
```

## Test Commands

### Run All Tests
```bash
dotnet test OpenScrape.sln
```

### Run Tests with Coverage
```bash
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --collect:"XPlat Code Coverage"
```

### Run Single Test
```bash
# Run specific test method
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~OpenScrape.App.Tests.Tests.TestHacenEscalera"

# Run all tests in a class
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~OpenScrape.App.Tests.Tests"

# Run tests by name pattern
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "Name~TestHacenEscalera"
```

### Run Tests in Watch Mode
```bash
dotnet watch test test/OpenScrape.App.Tests
```

## Lint/Format Commands

### Format Code
```bash
dotnet format OpenScrape.sln
```

### Check Code Style
```bash
dotnet format --verify-no-changes OpenScrape.sln
```

## Code Style Guidelines

### C# Language Features
- **Target Framework**: .NET 10.0
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **File-scoped Namespaces**: Preferred for new files
- **Records**: Use for immutable value objects and DTOs
- **Pattern Matching**: Use modern C# pattern matching features
- **Primary Constructors**: Use for simple dependency injection

### Formatting
- **Indentation**: 4 spaces
- **Line Length**: Keep under 120 characters
- **Braces**: Allman style (braces on new lines)
- **Blank Lines**: Use sparingly for readability
- **String Interpolation**: Prefer `$""` over `string.Format()`

### Imports and Using Statements
Group using statements by namespace origin:
1. System namespaces
2. Third-party libraries (Marten, MediatR, Emgu.CV, etc.)
3. Project namespaces (OpenScrape.*)

Remove unused imports, use implicit usings where applicable, sort alphabetically within groups.

### Naming Conventions

#### Classes and Types
- **PascalCase**: All class names, interfaces, structs, enums, records
- **Interface Prefix**: `I` (e.g., `ITableRepository`, `IPokerCalculator`)
- **Generic Parameters**: Single uppercase letters (e.g., `TEntity`, `TResult`)
- **Records**: Descriptive names (e.g., `Hand`, `TableDto`)

#### Methods and Properties
- **PascalCase**: All methods and properties
- **Private Fields**: camelCase with underscore (e.g., `_documentStore`)
- **Constants**: PascalCase (e.g., `MaxRetries`)
- **Events**: PascalCase with `EventHandler` suffix

#### Variables and Parameters
- **camelCase**: Local variables and parameters
- **Descriptive Names**: Prefer clarity over brevity
- **Boolean Prefixes**: `is`, `has`, `can`, `should` (e.g., `isValid`, `hasData`)

#### Files and Namespaces
- **File Names**: Match class/record name (e.g., `TableService.cs`)
- **Namespace Structure**: Follow directory structure
- **Partial Classes**: Descriptive suffixes (e.g., `FrmMain.Designer.cs`)

## Architecture Patterns

### Clean Architecture Layers
1. **Domain**: Core business entities, value objects, enums (OpenScrape.Domain)
2. **Features**: Application use cases and CQRS handlers (OpenScrape.Features)
3. **Infrastructure**: External concerns (database, APIs) (OpenScrape.Infrastructure)
4. **App**: UI layer and composition root (OpenScrape.App)
5. **DecisionMaker**: Poker decision algorithms (OpenScrape.DecisionMaker)

### CQRS Pattern
- Use MediatR for command/query separation
- Commands return `Unit` or specific result types
- Queries return DTOs or domain objects
- Use records for request/response objects

### Repository Pattern
- Use Marten for document database operations
- Implement repository interfaces in Infrastructure layer
- Inject repositories via dependency injection

## Error Handling

### Exception Handling
- **Validate Inputs**: Use guard clauses at method starts
- **Custom Exceptions**: Create specific exception types for domain errors
- **Null Checks**: Use null-coalescing (`??`) and null-conditional (`?.`) operators
- **Argument Validation**: Use `ArgumentNullException` for required parameters

### Logging
- Use Microsoft.Extensions.Logging
- Log levels: Trace, Debug, Information, Warning, Error, Critical
- Include contextual information in log messages
- Log exceptions with full stack traces in Error/Critical levels

### Async/Await Patterns
- Use `async`/`await` for all I/O operations
- Return `Task` or `Task<T>` for async methods
- Use `ConfigureAwait(false)` for library code
- Handle cancellation tokens properly

## Dependency Injection

### Service Registration
- Register services in `Program.cs` using Host builder pattern
- Use appropriate lifetimes:
  - **Singleton**: Services instantiated once (calculators, simulators)
  - **Scoped**: Services per request/scope
  - **Transient**: Services instantiated each time

### Constructor Injection
- Prefer constructor injection over property injection
- Validate injected dependencies are not null
- Keep constructors simple and focused
- Use primary constructors for simple cases

### Example Registration
```csharp
// In Program.cs
builder.Services.AddDataBase(context.Configuration, true);
builder.Services.AddUseCases();

// Register equity calculation components
services.AddSingleton<MonteCarloSimulator>();
services.AddSingleton<HandEvaluator>();
services.AddSingleton<OutsCalculator>();
services.AddSingleton<PreflopEquityCalculator>();
services.AddSingleton<EquityCalculatorService>();

// Register unified calculator
services.AddSingleton<IPokerCalculator, UnifiedPokerCalculator>();
```

## File Organization

### Directory Structure
```
src/
├── OpenScrape.App/           # UI and composition root
│   ├── Forms/                # Windows Forms (WinForms)
│   ├── Services/             # Application services
│   ├── Helpers/              # Static helper classes
│   ├── Models/               # UI-specific models
│   └── Program.cs
├── OpenScrape.Domain/        # Core business logic
│   ├── Entities/             # Domain entities
│   ├── ValueObjects/         # Value objects and records
│   ├── Enums/                # Enumerations
│   ├── Dtos/                 # Data transfer objects
│   └── Mappers/              # Mapping utilities
├── OpenScrape.Features/      # Use cases and CQRS handlers
│   ├── Table/                # Table-related features
│   ├── Cards/                # Card-related features
│   └── ActionScenario/       # Action scenario features
├── OpenScrape.Infrastructure/# External dependencies
│   └── Services/             # Infrastructure services
├── OpenScrape.DecisionMaker/ # Poker decision algorithms
│   ├── Services/             # Poker calculation services
│   └── Algorithms/           # Core poker algorithms
└── Tools/                    # Auxiliary tools and benchmarks
    ├── BenchmarkSuite1/
    └── ExtractorTablas/

test/
└── OpenScrape.App.Tests/     # NUnit test suite

docs/resources/              # Static documentation resources
output/samples/              # Sample/generated poker captures and text data
```

### File Naming
- **Use Cases**: `GetAllTables.cs`, `CreateTable.cs`
- **Interfaces**: `ITableRepository.cs`, `IPokerCalculator.cs`
- **DTOs**: `TableDto.cs`, `CardDto.cs`
- **Extensions**: `ServiceCollectionExtensions.cs`
- **Records**: `Hand.cs`, `Region.cs`

### Example Code Style
```csharp
using System.Linq;
using Marten;
using MediatR;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.Domain.ValueObjects;

public record Hand(string Name, bool? Suited, string Action, int Percentage);

public class TableService
{
    private readonly IDocumentStore _documentStore;

    public TableService(IDocumentStore documentStore)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
    }

    public async Task<List<Table>> GetAllTablesAsync()
    {
        using var session = _documentStore.LightweightSession();
        return await session.Query<Table>().ToListAsync();
    }
}
```

### Run Benchmarks
```bash
dotnet run --project src/Tools/BenchmarkSuite1/BenchmarkSuite1.csproj
```

## Debugging Guidelines

- Use Visual Studio Debugger or dotnet debug for breakpoints.
- Log detailed information in Debug mode.
- For OCR issues, enable debug mode in OcrService to save intermediate images.
- Check logs in the application directory for errors.

## Security Best Practices

- **Secrets Management**: Never commit API keys, passwords, or sensitive data. Use environment variables or secure configuration.
- **Input Validation**: Validate all inputs to prevent injection attacks, especially in OCR text processing.
- **Logging**: Avoid logging sensitive information like player names or stack values.
- **Access Control**: Ensure proper permissions for file access and external APIs.
- **Code Reviews**: Require peer reviews for all changes, especially those affecting OCR or decision logic.

## Performance Guidelines

- Use efficient algorithms for poker calculations (e.g., Monte Carlo simulations).
- Cache OCR results where possible to avoid redundant processing.
- Profile performance-critical code using BenchmarkDotNet (see BenchmarkSuite1).
- Minimize allocations in UI update loops.

## OCR Specific Guidelines

- Preprocess images with grayscale and binarization for better accuracy.
- Use multiple thresholds (active/inactive) for robust text extraction.
- Validate numeric results (e.g., stack values) before calculations.
- Handle failures gracefully by falling back to defaults or logging warnings.

## Version Control

- Use Git for source control.
- Follow conventional commit messages in Spanish.
- Create feature branches for new developments.
- Never commit build artifacts or sensitive files.

## Pre-Merge Checkpoints

Antes del merge a develop, ejecutar los checkpoints de verificación:

### Script Automatizado
```powershell
.\scripts\verify-pre-merge.ps1 -SkipSmoke
```

### Checklist Manual
Consultar `docs/pre-merge-checklist.md` para verificación paso a paso.

### Checkpoints
1. **Build Debug**: `dotnet build OpenScrape.sln` → Exit code 0
2. **Build Release**: `dotnet build OpenScrape.sln --configuration Release` → Exit code 0
3. **Format**: `dotnet format --verify-no-changes` → Sin cambios
4. **Tests**: `dotnet test` → ≥ 1173 tests pasando
5. **Warnings**: ≤ 75 warnings
6. **csproj**: Sin cambios en archivos .csproj
7. **Smoke Test**: Manual (8 pasos)

This document should be updated as the codebase evolves. When adding new patterns or changing existing guidelines, update this file accordingly.</content>
<parameter name="filePath">C:\Code\Poker\ScrapePoker\AGENTS.md

---

# Reversa

> Framework de Engenharia Reversa instalado neste projeto.

## Como usar

Digite `reversa` para ativar o Reversa e iniciar ou retomar a análise do projeto.

## Comportamento ao ativar

Quando o usuário digitar `reversa` sozinho em uma mensagem:

1. Ative o skill `reversa` disponível em `.agents/skills/reversa/SKILL.md`
2. Leia o SKILL.md na íntegra e siga exatamente as instruções do Reversa

## Regra não-negociável

Nunca apague, modifique ou sobrescreva arquivos pré-existentes do projeto legado.
O Reversa escreve **apenas** em `.reversa/` e `_reversa_sdd/`.
