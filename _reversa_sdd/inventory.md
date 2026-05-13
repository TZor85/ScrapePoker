# Inventario — ScrapePoker

> Generado por el **Scout** del Reversa el 2026-05-04.
> Fase: `reconhecimento`. Nivel: `completo`.

## 1. Visión general

**ScrapePoker / OpenScrape** es un bot de escritorio (.NET 10 WinForms, Windows-only) para mesas de póker. Captura pantalla, hace OCR + reconocimiento de imagen sobre la ventana de la sala de póker, modela el estado de la mesa y emite recomendaciones de acción a través de un overlay y de un motor de decisión propio.

- **Arquitectura:** Clean Architecture en 5 capas (Domain, Features, Infrastructure, DecisionMaker, App).
- **Persistencia:** Marten 8.24 + PostgreSQL (Neon DB en producción, document store).
- **OCR:** Tesseract 5.2 + preprocesamiento con OpenCvSharp4 4.10 / SkiaSharp 3.119.
- **DI / hosting:** `Microsoft.Extensions.Hosting` 10.0 con `DOTNET_ENVIRONMENT` por defecto `Development`.
- **Testing:** NUnit 4.3.2 (50 archivos de test, según memoria interna del proyecto rondan los 638-1171 tests).
- **Benchmarks:** BenchmarkDotNet 0.15.

## 2. Solutions y proyectos

### Solutions
| Archivo | Contenido |
|---------|-----------|
| `OpenScrape.sln` | Solution principal con todos los proyectos productivos + tests. |
| `OpenScrape.App.sln` | Solution reducida solo con `OpenScrape.App` (probable conveniencia para abrir UI sin compilar el resto). |

### Proyectos (`*.csproj`)
| Proyecto | Tipo | Target | Rol |
|----------|------|--------|-----|
| `src/OpenScrape.App` | WinExe | `net10.0-windows` | Composition root, WinForms UI, OCR, captura, game loop, telemetría. |
| `src/OpenScrape.DecisionMaker` | Library | `net10.0` | Motor de decisión (equity, MC, hand evaluator, postflop, preflop, opponent tracking). |
| `src/OpenScrape.Domain` | Library | `net10.0` | Entidades, value objects, enums, mappers, exceptions. |
| `src/OpenScrape.Features` | Library | `net10.0` | Use cases scoped (Table, Cards, ActionScenario, GameRound, RegionsTableMap). |
| `src/OpenScrape.Infrastructure` | Library | `net10.0` | Setup de Marten / PostgreSQL. |
| `OpenScrape.App.Tests` | Tests | `net10.0-windows` | Suite NUnit. |
| `BenchmarkSuite1` | Exe | `net10.0` | Benchmarks BenchmarkDotNet. |
| `Extractor/ExtractorTablas` | Exe | `net8.0` | Herramienta auxiliar de extracción (Newtonsoft.Json). Aislada del resto. |

> 🟡 **Atípico:** existen carpetas `src/OpenScrape.Application/` y `src/OpenScrape.Core/` que solo conservan `bin/` u `obj/` — sin `.csproj` ni `.cs`. Probables vestigios de una refactorización anterior. No están referenciados por ninguna solution actual.

## 3. Conteo de archivos por extensión

| Extensión | Archivos (excluyendo bin/obj/.git/.reversa/_reversa_sdd/.vs/.idea/.opencode/.agents/.claude) |
|-----------|-----:|
| `.cs` | 282 |
| `.md` | 162 |
| `.json` | 22 |
| `.xaml` | 1 (vacío, `MainPage.xaml` legacy) |

Lenguaje primario: **C#**.

## 4. Estructura de carpetas (3 niveles relevantes)

```
ScrapePoker/
├── BenchmarkSuite1/                    Benchmarks BenchmarkDotNet
├── Extractor/ExtractorTablas/          Herramienta auxiliar (.NET 8)
├── OpenScrape.App.Tests/               NUnit suite (50 archivos)
│   └── Telemetry/                      Tests de telemetría e2e
├── docs/                               Documentación interna (planes y guías)
│   └── superpowers/                    Plans/specs de superpowers (skill externo)
├── openspec/                           Spec-driven development (capabilities + scenarios)
│   ├── archived/                       9+ specs ya implementadas
│   └── changes/                        Specs en curso o pendientes
├── resources/                          Recursos varios
├── scripts/                            verify-pre-merge.ps1
├── src/
│   ├── OpenScrape.App/                 Composition root + UI
│   │   ├── Aplication/                 Use cases (UseCases/, UseCases/Actions/)
│   │   ├── Configuration/              GameLoopOptions, FeatureFlags
│   │   ├── Data/                       Estrategias JSON + tableMap.json
│   │   ├── Entities/                   Player, PlayerGameState, BoardTextures
│   │   ├── Forms/                      FrmMain, FrmOverlay, FrmHandDetail, FrmDetectionDebug, …
│   │   ├── Helpers/                    Encrypter, Coord scaler, capture helpers
│   │   ├── Interfaces/                 IAddImage
│   │   ├── Models/                     BestHandResult, NormalizedCard, Region
│   │   ├── Properties/                 launchSettings, Settings, Resources
│   │   ├── Resources/tessdata/         eng.traineddata (Tesseract)
│   │   ├── Services/                   38 servicios (OCR, table layout, game coord, etc.)
│   │   │   └── Logging/                TextBoxLogger
│   │   ├── Telemetry/                  Histogram, IMetricsCollector, MetricsCollector
│   │   ├── appsettings.json            Strategy profile, OCR, DB connection
│   │   └── appsettings.Development.json (gitignored)
│   ├── OpenScrape.DecisionMaker/
│   │   ├── Algorithms/                 BitHandEvaluator, MonteCarloSimulator, OutsCalculator, BoardTexture, PreflopEquity
│   │   ├── DTOs/                       DecisionRequest/Result, PostflopDecisionInput
│   │   ├── Interfaces/                 13 interfaces de servicios
│   │   ├── Services/                   PostflopDecision, OpponentTracker, BetSizing, Backtester, etc.
│   │   └── PokerConstants.cs
│   ├── OpenScrape.Domain/
│   │   ├── Dtos/, Entities/, Enums/, Exceptions/, Mappers/, ValueObjects/
│   ├── OpenScrape.Features/
│   │   ├── ActionScenario/Get/
│   │   ├── Cards/{GetAll, GetFlop}/
│   │   ├── GameRound/
│   │   ├── RegionsTableMap/{GetAll, Update}/
│   │   ├── Table/{Get, GetAll}/
│   │   └── Services.cs                 Registro DI de use cases
│   └── OpenScrape.Infrastructure/
│       └── Services.cs                 Marten setup + índices
├── .github/                            Prompts y skills (no GitHub Actions)
└── .reversa/, .agents/, .claude/, .opencode/   Frameworks de agentes (skills, configs)
```

## 5. Entry points

| Punto de entrada | Ubicación | Tipo |
|------------------|-----------|------|
| Main app (WinForms) | `src/OpenScrape.App/Program.cs` (`[STAThread] static void Main`) | `app_entry` |
| FrmMain (ventana principal) | `src/OpenScrape.App/Forms/FrmMain.cs` | `ui_main_form` |
| FrmOverlay (overlay sobre la mesa) | `src/OpenScrape.App/Forms/FrmOverlay.cs` | `ui_overlay` |
| FrmHandDetail (popup historial) | `src/OpenScrape.App/Forms/FrmHandDetail.cs` | `ui_dialog` |
| FrmDetectionDebug | `src/OpenScrape.App/Forms/FrmDetectionDebug.cs` | `ui_debug` |
| FormAction / FormImage / FormListApps | `src/OpenScrape.App/Forms/Form*.cs` | `ui_dialog` |
| Benchmarks | `BenchmarkSuite1/` (Exe) | `benchmark_entry` |
| Extractor de tablas | `Extractor/ExtractorTablas/` (Exe, .NET 8) | `cli_tool` |

## 6. Configuración

| Archivo | Contenido | Versionado |
|---------|-----------|-----------|
| `src/OpenScrape.App/appsettings.json` | Connection string PostgreSQL (Neon), encrypter key, table maps, `GameLoop`, `Features` flags, `StrategyProfile` completo (~30 thresholds postflop), `OverlayConfig`, `Logging` | ✅ committed |
| `src/OpenScrape.App/appsettings.Development.json` | Override de credenciales reales | ❌ gitignored |
| `src/OpenScrape.App/Properties/launchSettings.json` | `DOTNET_ENVIRONMENT=Development` | ✅ committed |
| `src/OpenScrape.App/Data/*.json` (17 archivos) | Estrategias por situación: BBvsSB, OpenRaise, ThreeBet, FourBet, Cold4Bet, Squeeze, RaiseOverLimpers, RaiseVsSbLimp, VsSqueeze, VsThreeBet, VsThreeBetAndCall, Cartas2, RegionToTest, Regiones, Regiones3, Revision, **tableMap.json** | ✅ committed |
| `opencode.json` | Config de OpenCode (CLI) | ✅ committed |
| `.gitignore`, `LICENSE`, `README.md` | Standard repo files | ✅ committed |

> 🔴 **Lacuna detectada (fuera del alcance del Scout):** `appsettings.json` contiene una connection string con credenciales reales (Neon DB, password en claro) y una `Encrypter.Key` real. El `CLAUDE.md` declara que estos valores deberían ser `CHANGE_ME` y que las credenciales reales viven solo en `appsettings.Development.json` (gitignored). Discrepancia entre lo declarado y el estado real del repo. Lo registro aquí para que el Reviewer lo confirme con el usuario en la fase final.

## 7. CI/CD

- **No hay GitHub Actions ni pipeline tradicional.** El directorio `.github/` solo aloja prompts y skills definitions de OpenSpec.
- **Pre-merge checklist manual:**
  - `scripts/verify-pre-merge.ps1` — script PowerShell de validación antes de fusionar.
  - `docs/pre-merge-checklist.md` — documento del proceso.
- **Asistente upgrade .NET:** carpeta `.github/upgrades/scenarios/new-dotnet-version_75fdf3/` con plan, tareas y log de ejecución (asistente Copilot, ya consumido).
- **No hay Dockerfile ni docker-compose.**

## 8. Banco de datos (superficial)

Análisis detallado lo hará el agente `reversa-data-master`. Hints superficiales:

- **Tipo:** PostgreSQL (Marten document store).
- **Provider:** Neon (cloud, `eu-west-2`).
- **Setup:** `src/OpenScrape.Infrastructure/Services.cs` → `AddDataBase()` con `AutoCreateSchemaObjects = AutoCreate.All` en Development.
- **Documentos registrados:**
  - `GameSession` (índices: `EndTime`, `SessionId`, `TableName`).
  - `HandRecord` (índices: `GameSessionId`, `Timestamp`, compuesto `GameSessionId+Timestamp`, `HeroPosition`).
- **Sin migrations DDL** — esquema gestionado en runtime por Marten.

## 9. Tests

- **Framework:** NUnit 4.3.2 (`Microsoft.NET.Test.Sdk` 17.14.0-preview, `NUnit3TestAdapter` 5.0.0, `NUnit.Analyzers` 4.6.0).
- **Cobertura:** `coverlet.collector` 6.0.4 (XPlat Code Coverage).
- **Conteo de archivos:** 50 archivos de test en `OpenScrape.App.Tests/` (incluyendo `Telemetry/`).
- **Áreas cubiertas:** decision engine (postflop, preflop, equity, MC, danger penalty, implied odds), strategy profile y validador, opponent tracker, exploitability, autocalibración, range polarizer, board texture, game loop coordinator y state machine, position calculator, telemetría, UI sync, action formatter, overlay positioner, bankroll tracker, strategy backtester.

## 10. Dependencias críticas

Detalle completo en `dependencies.md`. Resumen:

| Categoría | Paquete | Versión |
|-----------|---------|---------|
| ORM document | `Marten` | 8.24.0 |
| OCR | `Tesseract`, `Tesseract.Drawing` | 5.2.0 |
| Imagen | `OpenCvSharp4`, `OpenCvSharp4.runtime.win` | 4.10.0.20241108 |
| Imagen | `SkiaSharp` | 3.119.2 |
| DI / Hosting | `Microsoft.Extensions.{Hosting, DependencyInjection, Options, Configuration.Abstractions, Logging.Abstractions}` | 10.0.3 |
| Result pattern | `Ardalis.Result` | 10.1.0 |
| Test | `NUnit`, `NUnit3TestAdapter`, `NUnit.Analyzers` | 4.3.2 / 5.0.0 / 4.6.0 |
| Coverage | `coverlet.collector` | 6.0.4 |
| Bench | `BenchmarkDotNet` | 0.15.2 |
| JSON (Extractor) | `Newtonsoft.Json` | 13.0.3 |

## 11. Documentación interna existente

- **`CLAUDE.md`** (19 KB) — guía completa del repo para Claude Code: build/test commands, arquitectura por capas, motor de decisión (10+ paths postflop), state machine de game loop, configuración de estrategia, persistencia y logging, enums y dependencias clave. Excelente contexto para los agentes posteriores.
- **`AGENTS.md`** (12 KB) — guía paralela para otros agentes.
- **`openspec/`** — spec-driven development con capabilities + scenarios. ~9 specs archivadas + cambios en curso (`bankroll-dashboard`, `login-sistema-licencias`).
- **`docs/`** — `PLAN_IMPLEMENTACION.md`, `PLAN_MEJORAS*.md`, `pre-merge-checklist.md`, `logs.txt`, `superpowers/{plans,specs}/`.
- **`game.txt`** (178 KB) y **`tablemap.txt`** (9 KB) — datos brutos auxiliares.

## 12. Sugerencia de organización de specs

| Granularidad | Sugerida |
|--------------|----------|
| **`module`** | ✅ |

**Razón:** `src/` está organizado en 5 proyectos top-level con responsabilidad clara y aislada (`OpenScrape.App`, `OpenScrape.DecisionMaker`, `OpenScrape.Domain`, `OpenScrape.Features`, `OpenScrape.Infrastructure`). Cada proyecto = 1 unit de spec, lo que mantiene 1:1 con la unidad de despliegue/compilación. Si el usuario prefiere mayor granularidad para el motor de decisión, puede elegir `hybrid` en el menú.

**Señales detectadas:**
- 5 proyectos `.csproj` con responsabilidades técnicamente separadas (Clean Architecture).
- `Features/` ya pre-organizado por sub-features (`Table`, `Cards`, `ActionScenario`, `GameRound`, `RegionsTableMap`).
- Ausencia de roteamento centralizado, controllers REST, specs Gherkin/E2E (descarta `endpoint`, `use-case`).

## 13. Artefactos generados

- `_reversa_sdd/inventory.md` (este archivo)
- `_reversa_sdd/dependencies.md`
- `.reversa/context/surface.json` (consumido por agentes posteriores)
