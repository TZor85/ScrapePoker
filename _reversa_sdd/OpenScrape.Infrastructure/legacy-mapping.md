# Mapeo a archivos legacy — `OpenScrape.Infrastructure`

> Generado por el Arqueólogo del Reversa.
> Lista los archivos del legado que componen este módulo, con referencia directa a rutas y líneas.

---

## Estructura física

| Archivo | Tipo | LOC | Notas |
|---------|------|----:|-------|
| `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj` | proyecto | 22 | `<TargetFramework>net10.0</TargetFramework>`, `<NoWarn>$(NoWarn);NU1902</NoWarn>` |
| `src/OpenScrape.Infrastructure/Services.cs` | clase static | 43 | único archivo de código del módulo |

---

## Mapeo por responsabilidad

### Configuración Marten

- **Bootstrap único:** `src/OpenScrape.Infrastructure/Services.cs:13` → `services.AddMarten(options => …)`
- **Connection string:** `src/OpenScrape.Infrastructure/Services.cs:16` → `options.Connection(configuration.GetConnectionString("DefaultConnection")!)`
- **Serializer:** `src/OpenScrape.Infrastructure/Services.cs:19` → `options.UseSystemTextJsonForSerialization()`
- **Auto-creación de esquema:** `src/OpenScrape.Infrastructure/Services.cs:36-39` → `if (IsDevelopment) options.AutoCreateSchemaObjects = AutoCreate.All`

### Índices declarados

| Documento | Campo | Línea |
|-----------|-------|------:|
| `GameSession` | `EndTime` | `Services.cs:22` |
| `GameSession` | `SessionId` | `Services.cs:23` |
| `GameSession` | `TableName` | `Services.cs:24` |
| `HandRecord` | `GameSessionId` | `Services.cs:27` |
| `HandRecord` | `Timestamp` | `Services.cs:28` |
| `HandRecord` | `(GameSessionId, Timestamp)` compuesto | `Services.cs:30` |
| `HandRecord` | `HeroPosition` | `Services.cs:32` |

---

## Dependencias entre archivos

```
OpenScrape.Infrastructure.csproj
  ├─ Marten 8.24.0 (NuGet)
  ├─ Microsoft.Extensions.Configuration.Abstractions 10.0.3 (NuGet)
  ├─ Microsoft.Extensions.DependencyInjection.Abstractions 10.0.3 (NuGet)
  ├─ Ardalis.Result 10.1.0 (NuGet, no usado en Services.cs — declarado pero sin consumidor en este módulo)
  └─ ProjectReference → ../OpenScrape.Domain/OpenScrape.Domain.csproj
       └─ usado por Services.cs:5 → using OpenScrape.Domain.Entities;
            ├─ GameSession  (declarado en Domain/Entities/GameSession.cs)
            └─ HandRecord   (declarado en Domain/Entities/GameSession.cs:56)
```

🟡 **INFERIDO**: `Ardalis.Result 10.1.0` está declarado en el csproj pero `Services.cs` no lo usa. Posible vestigio de un esfuerzo anterior por envolver el bootstrap en `Result<T>` o herencia accidental al replicar csproj de `Features`. Validar si conviene removerlo.

---

## Llamadores externos

| Archivo | Línea | Llamada |
|---------|------:|---------|
| `src/OpenScrape.App/Program.cs` | 52 | `services.AddDataBase(context.Configuration, true);` |
| `AGENTS.md` (documentación) | 187 | referencia textual al patrón |
| `_reversa_sdd/inventory.md` | 144 | inventario del Scout |
| `_reversa_sdd/dependencies.md` | 65 | nota sobre `Microsoft.Extensions.DependencyInjection.Abstractions` |

---

## Consumidores de `IDocumentStore` (registrado por este módulo)

| Archivo | Líneas | Tipo de sesión |
|---------|--------|----------------|
| `src/OpenScrape.App/Services/GameLoggerService.cs` | 239, 274 | `LightweightSession` (escritura) |
| `src/OpenScrape.App/Services/GameLoggerService.cs` | 298, 311, 324, 337 | `QuerySession` (lectura) |
| `src/OpenScrape.App/Services/CardCacheService.cs` | 33 | `LightweightSession` (carga inicial) |
| `src/OpenScrape.App/Forms/FrmMain.cs` | 360, 383 | `LightweightSession` (escritura) |
| `src/OpenScrape.DecisionMaker/Services/BankrollTrackerService.cs` | 36, 250 | `QuerySession` (lectura) |
| `src/OpenScrape.Features/Cards/GetAll/GetAllCards.cs` | varias | `LightweightSession` |
| `src/OpenScrape.Features/Table/GetAll/GetAllTables.cs` | varias | `LightweightSession` |
| `src/OpenScrape.Features/Table/Get/GetTable.cs` | varias | `LightweightSession` |
| `src/OpenScrape.Features/RegionsTableMap/Update/UpdateRegionTableMap.cs` | 19 | `LightweightSession` |
| `src/OpenScrape.Features/RegionsTableMap/GetAll/GetAllRegionTableMap.cs` | 16 | comentado/legacy |
| `src/OpenScrape.Features/GameRound/GetRecentGameRounds.cs` | varias | `LightweightSession` |

🟢 **CONFIRMADO**: ningún consumidor mantiene la sesión viva más allá de un `await using` por operación. Patrón uniforme en todo el codebase.

---

## Configuración relacionada (fuera del módulo)

| Archivo | Sección | Notas |
|---------|---------|-------|
| `src/OpenScrape.App/appsettings.json:3` | `ConnectionStrings:DefaultConnection` | 🔴 contiene credenciales reales committeadas (anomalía Scout, ver `surface.json`) |
| `src/OpenScrape.App/appsettings.Development.json:3` | `ConnectionStrings:DefaultConnection` | Misma cadena; gitignored en `.gitignore` pero validado con `git check-ignore` |
| `src/OpenScrape.App/Properties/launchSettings.json` | `DOTNET_ENVIRONMENT=Development` | Solo afecta a host, no al flag hardcoded de `Program.cs:52` |

---

## Pendientes / lacunas para el Detective

- [ ] Validar si la cadena de Neon en `appsettings.json` debe ser revertida a placeholder `CHANGE_ME` (ver CLAUDE.md "Configuration & Secrets").
- [ ] Confirmar si el hardcoding de `IsDevelopment=true` en `Program.cs:52` es intencional (¿blockea migraciones automáticas en prod?).
- [ ] Decidir si la dependencia `Ardalis.Result` debe eliminarse del csproj (no usada).
- [ ] Verificar si `NU1902` puede resolverse actualizando Marten a una release que ya no arrastre la versión vulnerable de `OpenTelemetry.Api`.
