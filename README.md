# ScrapePoker

Asistente de póker para Windows: captura la ventana del cliente, lee la mesa por OCR y
detección de píxeles, calcula equity y muestra la acción sugerida en un overlay.

> Automatizar un cliente de póker puede violar sus términos de servicio.
> Este proyecto es de uso educativo y personal, bajo tu propia responsabilidad.

## Stack

| Componente | Tecnología |
|---|---|
| Runtime | .NET 10 (`net10.0-windows`) |
| UI | WinForms |
| Visión | OpenCvSharp4 + Tesseract 5 (`eng.traineddata` embebido) |
| Persistencia | Marten 8 sobre PostgreSQL (probado con Neon) |
| Tests | NUnit 4 + coverlet |
| Benchmarks | BenchmarkDotNet |
| CI | GitHub Actions |

## Requisitos

- Windows 10 u 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Una base PostgreSQL accesible (local o Neon)

No hace falta instalar Tesseract: el modelo de lenguaje viene embebido y se extrae solo
al iniciar la aplicación.

## Configuración

`src/OpenScrape.App/appsettings.json` está versionado **sin secretos**: la connection
string y la clave de cifrado quedan vacías.

Los valores locales van en `src/OpenScrape.App/appsettings.Development.json`, que está
en `.gitignore` y excluido del publish:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  },
  "Encrypter": {
    "Key": "..."
  }
}
```

> La aplicación **no arranca** sin `DefaultConnection`: `AddDataBase` lanza
> `InvalidOperationException` con un mensaje accionable.

El resto de la configuración (perfil de estrategia, umbrales por situación, overlay,
logging) vive en `appsettings.json` y se puede ajustar sin recompilar.

## Build y ejecución

```bash
dotnet build OpenScrape.sln
dotnet run --project src/OpenScrape.App/OpenScrape.App.csproj
```

Publish self-contained:

```bash
dotnet publish src/OpenScrape.App/OpenScrape.App.csproj --configuration Release --runtime win-x64 --self-contained
```

## Tests

```bash
dotnet test OpenScrape.sln

# Una clase
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~OpenScrape.App.Tests.Tests"

# Un test
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "Name~TestHacenEscalera"
```

## Estructura

```
src/
├── OpenScrape.App/            UI (WinForms), composition root, captura y servicios
├── OpenScrape.Domain/         Entidades, value objects, enums, DTOs
├── OpenScrape.Features/       Casos de uso y handlers
├── OpenScrape.Infrastructure/ PostgreSQL, repositorios, servicios externos
├── OpenScrape.DecisionMaker/  Equity, evaluador de manos, decisión preflop/postflop
└── Tools/                     Benchmarks y utilidades
test/
└── OpenScrape.App.Tests/      Suite NUnit
```

## Cómo funciona

1. **Window**: seleccionás la ventana del cliente de póker; se crea el overlay encima.
2. **Detección**: `DetectionLoopService` muestrea píxeles cada ~100 ms para saber cuándo
   es tu turno (color del botón de acción + flop visible).
3. **Lectura**: se captura la ventana y se extraen cartas, stack, bote y apuestas por OCR.
4. **Decisión**: equity por Monte Carlo + evaluación de mano + perfil de estrategia.
5. **Salida**: overlay con acción sugerida, equity y pot odds; la mano se persiste en
   PostgreSQL.

## Estado y deuda técnica conocida

- `Features.UseGameLoopCoordinator` debe quedar en `false`. El `GameLoopCoordinator`
  todavía no tiene gate de turno: con el flag activo, `HandProcessor` captura y hace OCR
  cada 100 ms y duplica el pipeline legacy.
- `OpenScrape.DecisionMaker` referencia Marten y usa `GetAwaiter().GetResult()` en la
  persistencia de perfiles (sync-over-async). La persistencia debería vivir en
  `OpenScrape.Infrastructure`.
- Conviven dos motores preflop: los use cases de `OpenScrape.App` (escenarios en Marten)
  y `PreflopAnalyzer` en `DecisionMaker`.
- La suite de tests concentra la matriz de decisión postflop en una sola clase de ~5.900
  líneas.

## Contribuir

- Commits convencionales en español.
- Antes del merge: `.\scripts\verify-pre-merge.ps1 -SkipSmoke` y el checklist de
  `docs/pre-merge-checklist.md`.
- Convenciones de código y arquitectura en `AGENTS.md`.
