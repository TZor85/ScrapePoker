# OpenScrape.Infrastructure — Design Técnico

> Cómo está construida la frontera DI con Marten/PostgreSQL: bootstrap, índices, lifetime, patrones de uso por los consumidores y observabilidad.

---

## Interface

`OpenScrape.Infrastructure` no expone HTTP, RPC ni clases instanciables. Su superficie pública es **un único extension method**:

| Símbolo | Firma | Retorno | Observación |
|---------|-------|---------|-------------|
| `Services.AddDataBase` | `(this IServiceCollection services, IConfiguration configuration, bool IsDevelopment)` | `void` | Registra `IDocumentStore` + sesiones derivadas en el contenedor DI vía `services.AddMarten(...)`. 🟢 (`Services.cs:11`) |

### Servicios resueltos por el contenedor tras invocar `AddDataBase`

| Tipo resuelto | Lifetime | Dónde se usa |
|---------------|----------|--------------|
| `IDocumentStore` | Singleton (gestionado por Marten) | Inyectado en `GameLoggerService`, `CardCacheService`, `BankrollTrackerService`, `FrmMain`, todos los use cases de `OpenScrape.Features` |
| `IDocumentSession` | Scoped (factory de Marten) | Casi nunca usado directo: el patrón estándar es abrir sesiones cortas vía `store.LightweightSession()` |
| `IQuerySession` | Scoped (factory de Marten) | Idem, vía `store.QuerySession()` |

🟢 **CONFIRMADO**: ningún consumidor inyecta `IDocumentSession`/`IQuerySession` directamente; todos inyectan `IDocumentStore` y abren sesiones por operación. Patrón uniforme verificado en 12+ archivos (`legacy-mapping.md`).

### Configuración interna pasada a Marten

| Opción | Valor | Línea |
|--------|-------|------:|
| `Connection(string)` | `configuration.GetConnectionString("DefaultConnection")!` | `Services.cs:16` |
| `UseSystemTextJsonForSerialization()` | activo | `Services.cs:19` |
| `Schema.For<GameSession>().Index(x => x.EndTime)` | activo | `Services.cs:22` |
| `Schema.For<GameSession>().Index(x => x.SessionId)` | activo | `Services.cs:23` |
| `Schema.For<GameSession>().Index(x => x.TableName)` | activo | `Services.cs:24` |
| `Schema.For<HandRecord>().Index(x => x.GameSessionId)` | activo | `Services.cs:27` |
| `Schema.For<HandRecord>().Index(x => x.Timestamp)` | activo | `Services.cs:28` |
| `Schema.For<HandRecord>().Index(x => new { GameSessionId, Timestamp })` | activo (compuesto) | `Services.cs:30` |
| `Schema.For<HandRecord>().Index(x => x.HeroPosition)` | activo | `Services.cs:32` |
| `AutoCreateSchemaObjects = AutoCreate.All` | condicional `if (IsDevelopment)` | `Services.cs:36-39` |

---

## Fluxo Principal

### Fluxo de arranque (host startup)

1. **`Program.cs`** del host App invoca `services.AddDataBase(context.Configuration, true)`. 🟢 (`src/OpenScrape.App/Program.cs:52`)
2. **`Services.AddDataBase`** llama `services.AddMarten(options => { ... })`, configurando dentro del lambda:
   1. Connection string vía `IConfiguration.GetConnectionString("DefaultConnection")`. 🟢 (`Services.cs:16`)
   2. Serializer System.Text.Json. 🟢 (`Services.cs:19`)
   3. **7 índices** secundarios (`GameSession`×3, `HandRecord`×4 incluido compuesto). 🟢
   4. `AutoCreateSchemaObjects = AutoCreate.All` solo si `IsDevelopment == true`. 🟢
3. **Marten** registra `IDocumentStore` como singleton y las sesiones (`IDocumentSession`, `IQuerySession`) como scoped/transient. 🟢 (comportamiento estándar de `AddMarten`, ADR-0003)
4. **Host construye** el `IServiceProvider`. En este punto no se ha tocado PostgreSQL todavía. 🟢
5. **Primer `OpenSession()`/`QuerySession()`** de un consumidor dispara la conexión y, en development, la auto-creación del esquema. 🟢

### Fluxo de uso en una operación de escritura

1. Consumidor recibe `IDocumentStore` por DI (constructor). 🟢
2. Abre sesión corta:
   ```csharp
   await using var session = _documentStore.LightweightSession();
   ```
3. Ejecuta operación: `session.Store(handRecord)`, `session.Insert(...)`, etc.
4. `await session.SaveChangesAsync(cancellationToken)`.
5. `await using` dispone la sesión y libera la conexión al pool. 🟢 (`legacy-mapping.md` filas 71-83)

### Fluxo de uso en una operación de lectura

1. Consumidor recibe `IDocumentStore`. 🟢
2. Abre sesión read-only:
   ```csharp
   await using var session = _documentStore.QuerySession();
   ```
3. Ejecuta query LINQ: `await session.Query<HandRecord>().Where(...).ToListAsync()`.
4. `await using` dispone la sesión.

### Fluxo de creación inicial del esquema (solo development)

1. Primer `LightweightSession().SaveChangesAsync()` con un nuevo tipo (`GameSession`, `HandRecord`, ...).
2. Marten detecta tabla ausente, ejecuta `CREATE TABLE mt_doc_<typename>`.
3. Marten aplica los `Schema.For<T>().Index(...)` declarados → `CREATE INDEX mt_doc_<typename>_idx_<field>`.
4. La sesión persiste el documento.
5. Operaciones siguientes encuentran tablas/índices listos. 🟢 (ADR-0003)

---

## Fluxos Alternativos

- **`IsDevelopment == false`:** `AutoCreate.All` no se aplica; Marten asume esquema pre-existente. Si tablas/índices faltan, las queries lanzan `Npgsql.PostgresException` en runtime. 🟢
- **Connection string nula:** el null-forgiving operator `!` en `Services.cs:16` causa `NullReferenceException` al construir el provider. 🔴 (sin guard explícito; ver `questions.md`)
- **Cadena sintácticamente inválida:** Npgsql lanza `ArgumentException` al primer `OpenSession()`. La construcción del DI no detecta el problema (lazy connect). 🟢
- **`AddDataBase` invocado dos veces:** `AddMarten` interno detecta duplicados y no rompe; el último wins. 🟡 (comportamiento de Marten 8, no testeado en este repo)
- **Vulnerabilidad transitiva `OpenTelemetry.Api`:** `<NoWarn>NU1902</NoWarn>` silencia el warning durante `dotnet restore`/`dotnet build`. La biblioteca vulnerable sigue cargada en memoria pero no se invoca activamente — es overhead de telemetría sin uso. 🟡
- **Migración de esquema en producción:** **fuera del alcance del módulo**. No hay scripts SQL committeados ni integración con FluentMigrator/EF Migrations. La política operativa es manual o vía herramienta externa Marten. 🔴 (ver `questions.md`)
- **Particionado/sharding:** ninguno declarado. Todas las sesiones apuntan a la misma DB. 🟢

---

## Dependências

### NuGet (declaradas en `OpenScrape.Infrastructure.csproj`)

| Paquete | Versión | Uso real | Confianza |
|---------|---------|----------|-----------|
| `Marten` | 8.24.0 | Bootstrap, índices, sesiones | 🟢 |
| `Microsoft.Extensions.Configuration.Abstractions` | 10.0.3 | `IConfiguration.GetConnectionString` | 🟢 |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 10.0.3 | `IServiceCollection`, `services.AddMarten` | 🟢 |
| `Ardalis.Result` | 10.1.0 | **Sin uso en el módulo** — declarada pero no consumida | 🟡 (vestigio, ver lacunas) |

### Transitivas relevantes

- **`Npgsql`** — driver PostgreSQL bajo Marten. Pool, retry, autenticación.
- **`OpenTelemetry.Api`** — arrastrada por Marten. Vulnerabilidad NU1902 silenciada.
- **`JasperFx`** — usado para `using JasperFx;` (`Services.cs:1`) que importa el namespace del enum `AutoCreate`. 🟢

### Project references

| Proyecto referenciado | Por qué | Confianza |
|----------------------|---------|-----------|
| `OpenScrape.Domain` | Tipo-fuerte para `Schema.For<GameSession>()` y `Schema.For<HandRecord>()` | 🟢 |

🟢 **CONFIRMADO**: el módulo **no** referencia `OpenScrape.App`, `OpenScrape.Features` ni `OpenScrape.DecisionMaker`. Cumple la regla de Clean Architecture: Infrastructure depende solo de Domain.

---

## Decisiones de Design Identificadas

| Decisión | Evidencia en el código | Confianza |
|----------|------------------------|-----------|
| Marten como document database por encima de Entity Framework Core | `Services.cs:13` (`AddMarten`), ADR-0003 | 🟢 |
| System.Text.Json como serializer (en vez de Newtonsoft.Json o JsonNet) | `Services.cs:19` | 🟢 |
| Auto-creación de esquema solo en development | `Services.cs:36-39`, ADR-0003 | 🟢 |
| Bootstrap concentrado en un único método estático sin clases auxiliares | `Services.cs` (43 LOC totales) | 🟢 |
| Índices secundarios elegidos por análisis de queries de `GameLoggerService` (Historial) y `OpponentTracker` (HeroPosition) | `Services.cs:22-32`, queries en `GameLoggerService.cs:298-337` | 🟢 |
| Sesiones cortas (`await using`) en cada operación, en vez de sesión compartida por scope DI | `legacy-mapping.md` Consumidores; ADR-0003 | 🟢 |
| `IsDevelopment` como **parámetro explícito** en lugar de leer `IHostEnvironment` dentro del método | `Services.cs:11` (signature) | 🟡 (decisión cuestionable, hardcoded en `Program.cs:52`) |
| `<NoWarn>NU1902</NoWarn>` para silenciar la vulnerabilidad transitiva | `OpenScrape.Infrastructure.csproj:8` | 🟡 |

---

## Estado Interno

El módulo **no mantiene estado propio**. Todo el estado vive en:

1. **Marten `IDocumentStore`** (singleton DI) — pool de conexiones, document registry, schema cache.
2. **PostgreSQL** — datos persistidos en tablas `mt_doc_<typename>`.
3. **Sesiones de corta vida** abiertas por consumidores — viven el tiempo de la operación (`await using`).

No hay caches en memoria internos al módulo; cualquier cache (catálogo de cartas, regiones de mesa) vive en servicios singleton de `OpenScrape.App` (`CardCacheService`, `RegionLookupCache`).

---

## Observabilidade

| Aspecto | Estado actual | Confianza |
|---------|---------------|-----------|
| Logging del bootstrap | Ninguno propio. Marten emite logs internos vía `ILogger` si el host lo configura. | 🟡 |
| Métricas de queries (latencia, número de queries por sesión) | Disponibles vía `IDocumentStore.Diagnostics` pero **no consumidas** por el código actual. | 🟡 |
| Health check de la base de datos | No implementado. La UI no detecta si PostgreSQL está caído hasta que falla una operación. | 🔴 (ver `questions.md`) |
| OpenTelemetry tracing | `OpenTelemetry.Api` cargado transitivamente pero sin configuración activa. Ningún tracer ni exporter registrado. | 🟢 (intencional — overhead innecesario para desktop app) |
| Retry / circuit breaker | No implementado. Cada operación lanza ante fallo y propaga al consumidor. | 🟡 |

> Recomendación: si en el futuro se externaliza PostgreSQL (Neon/cloud), añadir un health check y retry policy para tolerar latencia/desconexiones transitorias.

---

## Riesgos e Lacunas

- 🔴 **Connection string nula no validada.** `Services.cs:16` usa `!` (null-forgiving) sobre `GetConnectionString(...)`. Si la clave falta en `appsettings.json`, falla con `NullReferenceException` durante `BuildServiceProvider()`, sin mensaje accionable.
- 🔴 **`IsDevelopment` hardcoded a `true` en `Program.cs:52`.** Esto significa que **siempre** se intenta auto-crear el esquema, incluso en producción. ¿Es intencional? Si lo es, debe documentarse explícitamente; si no, debe leer `IHostEnvironment.IsDevelopment()`.
- 🔴 **Connection string con credenciales en `appsettings.json` committeado.** Anomalía detectada por el Scout (`surface.json`). Los secrets deben moverse a `appsettings.Development.json` (gitignored) o a un secret store.
- 🔴 **Sin política de migración para producción.** Si se despliega a una base productiva existente sin tablas/índices nuevos, el deploy fallará al primer query. No hay scripts SQL versionados.
- 🟡 **`Ardalis.Result` declarado pero no usado.** Falta limpieza del `.csproj` o introducción de wrappers de bootstrap (poco probable que valga la pena).
- 🟡 **`<NoWarn>NU1902</NoWarn>` aceptable a corto plazo** mientras Marten 8.24.0 arrastra `OpenTelemetry.Api` vulnerable, pero debe revisarse al actualizar Marten.
- 🟡 **Sin tests unitarios del bootstrap.** Marten se prueba indirectamente en tests de integración del repositorio principal, pero un test que verifique los 7 índices declarados no existe.
- 🟡 **Falta health check.** Para una app desktop con sesión persistente, una caída de la DB se manifiesta como excepciones esporádicas en lugar de un estado degradado claro.
- 🟢 **Aislamiento de capas verificado.** Cumple Clean Architecture: solo depende de Domain.
- 🟢 **Patrón `await using` consistente entre los 12+ consumidores.** Sin riesgo de fugas de conexiones ni sesiones de larga vida.
