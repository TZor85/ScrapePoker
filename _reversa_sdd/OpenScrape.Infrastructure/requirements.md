# OpenScrape.Infrastructure — Requisitos

> Capa de infraestructura. Responsable exclusivo del bootstrap de **Marten** (PostgreSQL document database) sobre `IServiceCollection`. No contiene lógica de negocio, repositorios ni servicios de aplicación. **Es la frontera DI con la base de datos.**

---

## Visión General

`OpenScrape.Infrastructure` es la capa más fina del solution: **un solo archivo de código** (`Services.cs`, 43 LOC) que expone el extension method `IServiceCollection.AddDataBase(IConfiguration, bool IsDevelopment)`. Ese método registra `Marten` como `IDocumentStore` singleton, configura el serializer (System.Text.Json), declara los índices de los documentos `GameSession` y `HandRecord`, y, en modo desarrollo, habilita la auto-creación de esquemas (`AutoCreate.All`). Todos los consumidores postflop (UI, telemetría, backtester, opponent tracker, features) abren sesiones cortas (`LightweightSession` o `QuerySession`) sobre ese `IDocumentStore` mediante `await using` por operación. 🟢 (`src/OpenScrape.Infrastructure/Services.cs`)

---

## Responsabilidades

- **Bootstrap único de Marten.** Registra `IDocumentStore` y derivados (`IDocumentSession`, `IQuerySession`) en el contenedor DI. 🟢 (`Services.cs:13`, ADR-0003)
- **Configuración de la connection string.** La extrae de `IConfiguration` mediante `GetConnectionString("DefaultConnection")` y la pasa a `options.Connection(...)`. 🟢 (`Services.cs:16`)
- **Selección del serializer.** `UseSystemTextJsonForSerialization()` para todos los documentos. Implica que entidades/value objects deben ser STJ-friendly (sin ciclos, sin tipos exóticos). 🟢 (`Services.cs:19`)
- **Declaración de índices secundarios.** Crea índices que aceleran las queries más frecuentes:
  - `GameSession`: `EndTime`, `SessionId`, `TableName`. 🟢
  - `HandRecord`: `GameSessionId`, `Timestamp`, `(GameSessionId, Timestamp)` compuesto, `HeroPosition`. 🟢
- **Auto-creación condicional de esquema.** Solo si `IsDevelopment == true` activa `AutoCreate.All`; en producción deja la migración fuera del runtime. 🟢 (`Services.cs:36-39`, ADR-0003)
- **Entrega de `IDocumentStore` como singleton (lifetime de Marten).** Los consumidores abren sesiones cortas con `await using`; nunca mantienen una sesión viva. 🟢 (verificado en 12+ consumidores, `legacy-mapping.md`)
- **No contiene repositorios ni use cases.** El acceso a documentos lo realizan directamente los use cases en `OpenScrape.Features` o servicios singleton/scoped en `OpenScrape.App` y `OpenScrape.DecisionMaker`. 🟢

---

## Reglas de Negocio

### Reglas estructurales

- **Una sola entrada DI: `AddDataBase(...)`.** No hay otros extension methods públicos en el módulo. 🟢 (`Services.cs:11`)
- **`IDocumentStore` es singleton de proceso.** Marten mantiene su propio pool de conexiones; ningún consumidor debe crear uno paralelo. 🟢 (lifetime estándar de `AddMarten`, ADR-0003)
- **Cada operación abre y cierra su propia sesión.** Patrón uniforme `await using var session = store.Lightweight(Query)Session();` en los 12+ consumidores documentados. 🟢 (`legacy-mapping.md` — Consumidores de `IDocumentStore`)
- **El módulo no instancia nada del dominio.** Solo lo referencia tipo-fuertemente para configurar `Schema.For<T>().Index(...)`. 🟢 (`using OpenScrape.Domain.Entities;` `Services.cs:5`)
- **STJ es el único serializer soportado.** No hay configuración alternativa; cualquier tipo persistido debe ser serializable por System.Text.Json. 🟢 (`Services.cs:19`)

### Reglas de configuración

- **`IsDevelopment` es un parámetro explícito, no infiere `DOTNET_ENVIRONMENT`.** El llamador (`Program.cs:52` lo pasa hardcoded a `true`). 🟢 / 🟡 hardcoded (ver lacunas)
- **Connection string proviene **siempre** de `IConfiguration`.** El módulo no acepta una cadena hardcoded ni lee `Environment.GetEnvironmentVariable` directamente. 🟢
- **Si la connection string es `null` el `!` lanzará `NullReferenceException` en runtime.** No hay guard explícito. 🟡 (potencial mejora — ver lacunas)

### Reglas de auto-creación de esquema

- **`AutoCreate.All` solo en development.** En producción la propiedad por defecto de Marten (`AutoCreate.None` o `CreateOrUpdate` según versión) deja al equipo de operaciones controlar las migraciones. 🟢 (ADR-0003)
- **Auto-creación cubre tablas, índices y secuencias.** Marten es responsable; no hay scripts SQL en el repositorio. 🟢

### Reglas marcadas como sospechosas / 🟡 lacunas

- **`Ardalis.Result 10.1.0` está declarado en `OpenScrape.Infrastructure.csproj` pero ningún archivo del módulo lo usa.** Probable copia-pega de `OpenScrape.Features.csproj`. 🟡 (`legacy-mapping.md` línea 54)
- **`<NoWarn>NU1902</NoWarn>` suprime una vulnerabilidad transitiva en `OpenTelemetry.Api` arrastrada por Marten 8.24.0.** El código del módulo no consume OpenTelemetry directamente, pero la advertencia se silencia globalmente. 🟡 (`OpenScrape.Infrastructure.csproj:8`)
- **`IsDevelopment=true` pasado **hardcoded** en `Program.cs:52`.** No respeta `DOTNET_ENVIRONMENT`; ningún path lo cambia a `false`. 🔴 (ver `questions.md`)

---

## Requisitos Funcionales

| ID | Requisito | Prioridad | Criterio de Aceite |
|----|-----------|-----------|---------------------|
| RF-01 | `IServiceCollection.AddDataBase(IConfiguration, bool)` debe ser el único punto de entrada DI del módulo y registrar `IDocumentStore` como singleton. | **Must** | Test que invoque `services.AddDataBase(cfg, true); var sp = services.BuildServiceProvider();` y verifique que `sp.GetRequiredService<IDocumentStore>()` retorna no-null y la misma instancia en dos resoluciones consecutivas. |
| RF-02 | La connection string debe leerse desde `configuration.GetConnectionString("DefaultConnection")`. | **Must** | Test que pase `IConfiguration` con `{ "ConnectionStrings:DefaultConnection": "Host=test" }` y verifique vía reflexión o smoke-test que Marten recibió esa cadena. |
| RF-03 | El serializer registrado debe ser System.Text.Json. | **Must** | Test que serialice y deserialice `GameSession` con caracteres Unicode y propiedades anidadas; verifique round-trip exacto. |
| RF-04 | Deben existir índices secundarios en `GameSession.EndTime`, `GameSession.SessionId`, `GameSession.TableName`. | **Must** | Smoke test con base local: tras invocar `AddDataBase`, consultar `pg_indexes` y verificar 3 índices por tabla `mt_doc_gamesession`. |
| RF-05 | Deben existir índices secundarios en `HandRecord.GameSessionId`, `HandRecord.Timestamp`, índice compuesto `(GameSessionId, Timestamp)`, e índice por `HeroPosition`. | **Must** | Smoke test que verifique 4 índices en `pg_indexes` para la tabla `mt_doc_handrecord`. |
| RF-06 | `AutoCreate.All` debe activarse **solo** si `IsDevelopment == true`. | **Must** | Inspección de configuración Marten: con `IsDevelopment=true` la opción `AutoCreateSchemaObjects` es `All`; con `false` queda en el default. |
| RF-07 | Ningún archivo del módulo debe abrir/mantener una `IDocumentSession` viva fuera del scope DI. | **Must** | Inspección estática: `Services.cs` nunca declara `IDocumentSession` ni invoca `store.LightweightSession()`. |
| RF-08 | El método `AddDataBase` debe ser idempotente respecto al registro de Marten (no debe romper si se invoca dos veces). | **Should** | Test que invoque `AddDataBase` dos veces sobre el mismo `services`; el segundo `BuildServiceProvider().GetRequiredService<IDocumentStore>()` no debe lanzar. |
| RF-09 | El módulo no debe registrar ningún servicio adicional fuera de la cadena Marten. | **Must** | Inspección de `Services.cs`: solo invoca `services.AddMarten(...)`; ningún `services.AddSingleton/Scoped/Transient` adicional. |
| RF-10 | El módulo no debe depender de proyectos diferentes de `OpenScrape.Domain` (tipo-fuerte) y los NuGets declarados. | **Must** | Inspección de `OpenScrape.Infrastructure.csproj`: única `<ProjectReference>` apunta a Domain; sin referencias a App/Features/DecisionMaker. |
| RF-11 | Conectarse a una base inexistente debe diferirse al primer `OpenSession()`, no al `AddDataBase`. | **Should** | Test que invoque `AddDataBase` con cadena inválida pero sintácticamente correcta; verifique que `BuildServiceProvider` no lanza, pero `store.QuerySession().Query<GameSession>().ToList()` sí. |
| RF-12 | El consumidor `Program.cs` debe poder pasar `IsDevelopment` desde un host environment, no hardcoded. | **Could** | Refactor pendiente: ver lacuna 🔴 en `questions.md`. |

---

## Requisitos No Funcionales

| Tipo | Requisito inferido | Evidencia en el código | Confianza |
|------|--------------------|------------------------|-----------|
| Performance | Pool de conexiones gestionado internamente por Marten. No hay configuración explícita de `MinPoolSize`/`MaxPoolSize`. | `Services.cs:13-40` (no override) | 🟡 |
| Performance | Índices secundarios sobre los campos de mayor cardinalidad de query reducen scans. Específicamente índice compuesto `(GameSessionId, Timestamp)` cubre la query "hands de sesión X ordenadas por fecha". | `Services.cs:30` | 🟢 |
| Escalabilidad | `AutoCreate.None` implícito en producción evita race conditions de migración entre instancias. | `Services.cs:36-39`, ADR-0003 | 🟢 |
| Disponibilidad | El módulo no implementa retry, circuit breaker ni health check sobre Marten. La resiliencia depende del cliente Npgsql subyacente. | `Services.cs` (ausencia de configuración de retry) | 🟡 |
| Mantenibilidad | LOC mínimo (43) → superficie de cambio reducida; bootstrap en 1 archivo permite revisión 1-shot. | `Services.cs` | 🟢 |
| Seguridad | Connection string contiene credenciales en texto plano dentro de `appsettings.json`/`appsettings.Development.json`. El módulo es agnóstico a la fuente de la cadena. | `Services.cs:16`, anomalía Scout `surface.json` | 🔴 (ver lacunas) |
| Seguridad | Marten 8.24.0 arrastra `OpenTelemetry.Api` con vulnerabilidad transitiva (`NU1902`); silenciada globalmente vía `<NoWarn>`. | `OpenScrape.Infrastructure.csproj:8` | 🟡 |
| Trazabilidad | El bootstrap no registra logging estructurado del estado de Marten en startup; depende del logger global del host. | `Services.cs` (sin `services.AddLogging` ni hooks) | 🟡 |
| Compatibilidad | TFM `.NET 10.0` → versiones específicas: `Marten 8.24.0`, `Microsoft.Extensions.Configuration.Abstractions 10.0.3`, `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.3`. | `OpenScrape.Infrastructure.csproj:13-15` | 🟢 |
| Internacionalización | El módulo no maneja strings de UI; comentarios in-line están en castellano + inglés mixto (vestigio de scaffolding original Marten). | `Services.cs:14-39` | 🟡 |

> Estos requisitos se inferirían a partir del bootstrap; varios apuntan a deudas conocidas (retry, hardcoded `IsDevelopment`, secrets en config). Validar con operaciones antes de implementarlos.

---

## Criterios de Aceitação

### Bootstrap básico (RF-01 a RF-03)

```gherkin
Dado un IServiceCollection vacío y una IConfiguration con ConnectionStrings:DefaultConnection = "Host=localhost;Database=scrape_test;Username=sp;Password=sp"
Cuando se invoca services.AddDataBase(cfg, true)
Entonces sp.GetRequiredService<IDocumentStore>() retorna una instancia válida
Y dos resoluciones consecutivas devuelven la misma instancia (singleton)
Y store.Options.Serializer().GetType().Name == "SystemTextJsonSerializer"
```

### Índices declarados (RF-04, RF-05)

```gherkin
Dado un Marten arrancado con AddDataBase(cfg, true) y AutoCreateSchemaObjects=All
Cuando se invoca por primera vez await using var s = store.LightweightSession()
Y s.Store(new GameSession { … })
Y await s.SaveChangesAsync()
Entonces existe un índice "mt_doc_gamesession_idx_endtime" en pg_indexes
Y existe un índice compuesto "mt_doc_handrecord_idx_gamesessionid_timestamp"
Y un total de 3 índices secundarios en GameSession y 4 en HandRecord
```

### Auto-creación condicional (RF-06)

```gherkin
Dado IsDevelopment = false
Cuando se invoca AddDataBase(cfg, false)
Entonces options.AutoCreateSchemaObjects no se setea a AutoCreate.All
Y permanece en el default que Marten establece para producción

Dado IsDevelopment = true
Cuando se invoca AddDataBase(cfg, true)
Entonces options.AutoCreateSchemaObjects == AutoCreate.All
```

### Idempotencia (RF-08)

```gherkin
Dado un IServiceCollection
Cuando se invoca services.AddDataBase(cfg, true) dos veces consecutivas
Entonces sp.BuildServiceProvider().GetRequiredService<IDocumentStore>() no lanza
Y la sesión resultante es funcional (no rompe Marten internamente)
```

### Connection string nula (lacuna 🔴)

```gherkin
Dado IConfiguration sin la clave ConnectionStrings:DefaultConnection
Cuando se invoca AddDataBase(cfg, true)
Entonces el comportamiento ESPERADO debe lanzar una excepción descriptiva al construir el provider
Pero el comportamiento ACTUAL es: NullReferenceException debido al null-forgiving operator (!) en Services.cs:16
🔴 LACUNA — validar política con equipo de operaciones
```

---

## Prioridade (MoSCoW)

| Requisito | MoSCoW | Justificación |
|-----------|--------|---------------|
| `AddDataBase` registra `IDocumentStore` (RF-01) | **Must** | Caminho crítico de toda la app: sin Marten, no hay persistencia, ni telemetría, ni opponent tracker, ni backtester. |
| Connection string desde `IConfiguration` (RF-02) | **Must** | Cualquier hardcoding ataría el módulo a un único entorno. |
| System.Text.Json como serializer (RF-03) | **Must** | Otros serializers cambiarían el formato wire de los documentos ya persistidos → migración masiva. |
| Índices secundarios sobre `HandRecord` y `GameSession` (RF-04, RF-05) | **Must** | Sin índices, las queries del Historial y del backtester escalan O(n) sobre el set completo y se vuelven inutilizables a partir de ~50K manos. |
| `AutoCreate.All` solo en dev (RF-06) | **Must** | Habilitar auto-creación en prod entre múltiples instancias provoca races durante el deploy → corrupción del esquema. |
| Inmutabilidad del bootstrap (RF-07, RF-09, RF-10) | **Must** | El módulo es un nodo terminal de la dependencia inversa de Clean Architecture; permitir lógica adicional rompería el principio. |
| Idempotencia de `AddDataBase` (RF-08) | **Should** | Tests que reusan `IServiceCollection` se vuelven brittle si el método rompe en doble registro. |
| Lazy connect (RF-11) | **Should** | Permite arrancar la UI aunque la base esté caída temporalmente; el comportamiento ACTUAL ya es lazy gracias a Marten. |
| Sustituir `IsDevelopment` hardcoded (RF-12) | **Could** | Refactor sencillo (`context.HostingEnvironment.IsDevelopment()`); riesgo: activar migraciones automáticas en producción si no se controla la cadena. |
| Eliminar `Ardalis.Result` del csproj | **Could** | Limpieza estética; no afecta runtime ni build. |
| Wrap del bootstrap en `Result<T>` | **Won't** | Ardalis.Result añade complejidad sin valor para un extension method que invocará el host una vez. |

---

## Rastreabilidade de Código

| Archivo | Función / Tipo | Cobertura |
|---------|----------------|-----------|
| `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj` | proyecto, NuGets, `<NoWarn>NU1902</NoWarn>` | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs` | `static class Services` | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:11` | `AddDataBase(this IServiceCollection, IConfiguration, bool)` | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:13` | `services.AddMarten(...)` bootstrap | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:16` | configuración de `Connection` | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:19` | `UseSystemTextJsonForSerialization()` | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:22-24` | índices `GameSession` | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:27-32` | índices `HandRecord` (incluye compuesto) | 🟢 |
| `src/OpenScrape.Infrastructure/Services.cs:36-39` | `AutoCreate.All` condicional | 🟢 |
| `src/OpenScrape.App/Program.cs:52` | llamador único `AddDataBase(...)` | 🟢 (consumidor externo) |

> Cobertura: **1 archivo `.cs`** documentado, **1 método público** expuesto, **7 índices** declarados. Módulo **mínimo y completo** — apenas LOC para un cambio futuro pero alta criticidad operativa.
