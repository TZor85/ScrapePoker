# Dependencias — ScrapePoker

> Generado por el **Scout** del Reversa el 2026-05-04.
> Fuente: archivos `*.csproj` (NuGet PackageReference). Sin lock file central.

## Stack base

- **.NET 10.0** (todos los proyectos productivos y tests).
- **.NET 8.0** (solo `Extractor/ExtractorTablas` — herramienta auxiliar aislada).
- **Package manager:** NuGet.

## Dependencias por proyecto

### `src/OpenScrape.App/OpenScrape.App.csproj` (WinExe, net10.0-windows)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Marten` | 8.24.0 | Document DB sobre PostgreSQL. |
| `Microsoft.Extensions.DependencyInjection` | 10.0.3 | DI container. |
| `Microsoft.Extensions.Hosting` | 10.0.3 | Generic host (Hosting + Logging + Configuration). |
| `OpenCvSharp4` | 4.10.0.20241108 | Procesamiento de imagen para preprocesamiento OCR. |
| `OpenCvSharp4.runtime.win` | 4.10.0.20241108 | Native runtime Windows. |
| `SkiaSharp` | 3.119.2 | Imagen 2D adicional. |
| `Tesseract` | 5.2.0 | OCR engine. |
| `Tesseract.Drawing` | 5.2.0 | Adaptador OCR ↔ `System.Drawing`. |

**ProjectReferences:** `OpenScrape.DecisionMaker`, `OpenScrape.Domain`, `OpenScrape.Features`, `OpenScrape.Infrastructure`.

**Recursos embebidos:** `Resources/tessdata/eng.traineddata` y `tessdata/eng.traineddata` (duplicado, uno con `CopyToOutputDirectory=PreserveNewest`).

### `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` (Library, net10.0)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Marten` | 8.24.0 | (Acceso al store para BankrollTrackerService.) |
| `Microsoft.Extensions.Logging.Abstractions` | 10.0.3 | Logging plug-in. |
| `Microsoft.Extensions.Options` | 10.0.3 | `IOptions<StrategyProfile>`. |

**ProjectReferences:** `OpenScrape.Domain`.
**InternalsVisibleTo:** `OpenScrape.App.Tests`.

### `src/OpenScrape.Domain/OpenScrape.Domain.csproj` (Library, net10.0)

Sin paquetes NuGet — solo Domain puro.

**InternalsVisibleTo:** `OpenScrape.App.Tests`.

### `src/OpenScrape.Features/OpenScrape.Features.csproj` (Library, net10.0)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Ardalis.Result` | 10.1.0 | Result pattern para use cases. |
| `Marten` | 8.24.0 | Sesiones de documento dentro de los use cases. |

**ProjectReferences:** `OpenScrape.Domain`.
**Suprime warning:** `NU1902` (vulnerability transitiva de `OpenTelemetry.Api` arrastrada por Marten).

### `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj` (Library, net10.0)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Ardalis.Result` | 10.1.0 | Result pattern. |
| `Marten` | 8.24.0 | Setup del document store. |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.3 | Lectura de connection string. |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.3 | Extension methods `AddDataBase`. |

**ProjectReferences:** `OpenScrape.Domain`.
**Suprime warning:** `NU1902`.

### `OpenScrape.App.Tests/OpenScrape.App.Tests.csproj` (Tests, net10.0-windows)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Microsoft.Extensions.Options` | 10.0.3 | Inyección de StrategyProfile en tests. |
| `Microsoft.NET.Test.Sdk` | 17.14.0-preview-25107-01 | Test runner. |
| `NUnit` | 4.3.2 | Framework de tests. |
| `NUnit3TestAdapter` | 5.0.0 | Adaptador VS / `dotnet test`. |
| `NUnit.Analyzers` | 4.6.0 | Roslyn analyzers (private). |
| `coverlet.collector` | 6.0.4 | Cobertura de código (private). |

**ProjectReferences:** `OpenScrape.App`, `OpenScrape.DecisionMaker`, `OpenScrape.Domain`.

### `BenchmarkSuite1/BenchmarkSuite1.csproj` (Exe, net10.0)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `BenchmarkDotNet` | 0.15.2 | Microbenchmarks. |
| `Microsoft.VisualStudio.DiagnosticsHub.BenchmarkDotNetDiagnosers` | 18.0.36421.1 | Diagnostics Hub para perfilado. |

**Sin ProjectReferences declarados** (la suite incluye su propio código autocontenido o referencias dinámicas — verificar si se cargan ensamblados de OpenScrape).

### `Extractor/ExtractorTablas/ExtractorTablas.csproj` (Exe, net8.0)

| Paquete | Versión | Propósito |
|---------|---------|-----------|
| `Newtonsoft.Json` | 13.0.3 | Serialización JSON. |

> 🟡 Único proyecto que usa `Newtonsoft.Json` (vs `System.Text.Json` en el resto) y único en `.NET 8.0`. Probable utilidad legacy desacoplada del bot principal.

## Pirámide de dependencias entre proyectos

```
                 ┌────────────────────────┐
                 │     OpenScrape.App     │
                 │   (WinExe, net10-win)  │
                 └───┬──────────┬─────┬───┘
                     │          │     │
       ┌─────────────┘          │     └────────────┐
       ▼                        ▼                  ▼
 OpenScrape.Features    OpenScrape.DecisionMaker  OpenScrape.Infrastructure
       │                        │                  │
       └────────┬───────────────┴──────────────────┘
                ▼
         OpenScrape.Domain
```

`Domain` no depende de nadie. `App` depende de las 4 capas. `Features`, `DecisionMaker` e `Infrastructure` solo dependen de `Domain`. La regla de Clean Architecture se respeta.

## Notas técnicas

- **Marten 8.24.0** se usa en App, DecisionMaker, Features e Infrastructure — fuerte acoplamiento de la capa de persistencia distribuido en varias capas (no centralizado en Infrastructure). El Archaeologist debe profundizar en este reparto.
- **`AllowUnsafeBlocks=true`** en `OpenScrape.App` (probable necesidad para LockBits / pixel sampling de OCR — coherente con la memoria de optimizaciones).
- **`Platforms=AnyCPU;x64;x86;ARM32;ARM64`** en `OpenScrape.App` — declarado multiplataforma de CPU pero `net10.0-windows` lo limita a Windows.
- **Tesseract `eng.traineddata`** embebido como `EmbeddedResource` y duplicado en `Resources/tessdata/` y `tessdata/`.
- **OpenTelemetry.Api** entra como dependencia transitiva de Marten — silenciado con `NoWarn=NU1902` por vulnerabilidad reportada.
- **No hay paquetes para HTTP, gRPC, REST clients ni WebSocket** — el bot no se comunica con APIs externas (al menos no por dependencias declaradas; el Archaeologist puede confirmar si hay HTTP raw).
