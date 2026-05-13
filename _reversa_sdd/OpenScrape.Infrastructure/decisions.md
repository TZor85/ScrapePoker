# OpenScrape.Infrastructure — Decisiones de Diseño

> Registro de decisiones arquitecturales detectadas en la capa de infraestructura. Cada decisión cita su evidencia en código y, cuando aplica, su ADR correspondiente en `_reversa_sdd/adrs/`.

---

## DD-01 — Marten como document database (en vez de EF Core)

**Decisión:** El módulo registra Marten como única tecnología de persistencia. No coexiste con Entity Framework Core ni con Dapper.

**Contexto:** El modelo de dominio del proyecto contiene tipos polimorfos pesados (`StrategyProfile` con ~60 propiedades anidadas, `HandRecord` con `List<StreetDecision>`), telemetría agregada, y datos JSON-friendly (regiones de mesa). Una solución relacional pura (EF + tablas normalizadas) requeriría modelar `~30+ tablas` con relaciones complejas, mientras que Marten persiste cada documento como JSONB y deja la flexibilidad al esquema del documento.

**Alternativas consideradas:**
1. **EF Core 8 con SQL Server** — descartado: el modelo de dominio es agregado por sesión/mano, no transaccional fino. Mapping ORM penalizaría el throughput de escritura del game loop sin beneficio.
2. **Dapper con SQL puro** — descartado: requiere mantener DDL versionado y mappers ad-hoc. Excesiva fricción para un proyecto de un único desarrollador.
3. **MongoDB** — descartado: introduce un servicio adicional, sin sumar valor sobre PostgreSQL JSONB.
4. **LiteDB / SQLite local** — descartado: evita servidor pero no permite acceso concurrente desde análisis externos (Grafana, queries ad-hoc desde DBeaver).

**Consecuencias positivas:**
- Bootstrap de **~30 LOC** (`Services.cs:13-40`) cubre 6 tipos persistidos.
- Auto-creación del esquema en development (`AutoCreate.All`) elimina migraciones manuales durante prototipado.
- Queries LINQ tipadas: `session.Query<HandRecord>().Where(h => h.GameSessionId == id)`.
- JSONB de Postgres permite queries SQL ad-hoc sobre documentos serializados.

**Consecuencias negativas:**
- Marten arrastra `OpenTelemetry.Api` con vulnerabilidad transitiva (NU1902 silenciada).
- Skill scarce: el equipo necesita formación específica de Marten si crece más allá de un dev.
- Rendimiento de queries complejas sobre JSONB es inferior a tablas normalizadas con índices apropiados (mitigado con índices secundarios — ver DD-04).

**Evidencia:** `src/OpenScrape.Infrastructure/Services.cs:13` (`services.AddMarten(...)`); `OpenScrape.Infrastructure.csproj:13` (`<PackageReference Include="Marten" Version="8.24.0" />`). 🟢

**ADR relacionado:** ADR-0003 (Marten Postgres document DB).

---

## DD-02 — System.Text.Json como serializer

**Decisión:** `options.UseSystemTextJsonForSerialization()` activa STJ como serializer por defecto de Marten.

**Contexto:** Marten 8 soporta tres serializers: Newtonsoft.Json (default histórico), System.Text.Json y JsonNet customizado. STJ es el más rápido y el oficial de .NET 6+.

**Alternativas consideradas:**
1. **Newtonsoft.Json** — descartado: dependencia adicional, performance inferior, futuro incierto en .NET 10.
2. **JsonNet customizado** — descartado: complejidad sin beneficio claro para un modelo simple.
3. **MessagePack/Protobuf** — descartado: pierde la posibilidad de inspección JSONB ad-hoc en SQL.

**Consecuencias positivas:**
- Sin dependencias adicionales (STJ viene con .NET runtime).
- Performance medible mejor que Newtonsoft en benchmarks Marten.
- Compatibilidad nativa con `record` inmutables y `init` props (Marten 8.x lo gestiona).

**Consecuencias negativas:**
- STJ es más estricto con type-handling que Newtonsoft. Tipos polimorfos requieren `JsonConverter` custom (no presente en este proyecto).
- Comentarios JSON, trailing commas, etc. no se permiten — irrelevante porque el JSON no es escrito a mano sino generado.

**Evidencia:** `Services.cs:19`. 🟢

**ADR relacionado:** ADR-0003.

---

## DD-03 — Bootstrap concentrado en un único método estático

**Decisión:** Todo el bootstrap de Marten vive en `Services.AddDataBase(IServiceCollection, IConfiguration, bool)`. No hay clases auxiliares, factories ni configuradores parciales.

**Contexto:** El módulo tiene una sola responsabilidad (registrar Marten) y un solo consumidor (`Program.cs:52`). Trocear el bootstrap en sub-clases sería sobre-ingeniería.

**Alternativas consideradas:**
1. **Builder pattern** (`new MartenConfiguratorBuilder().WithIndexes().WithSerializer().Build()`) — descartado: complejidad innecesaria para 7 índices y 1 serializer.
2. **Configuración en `appsettings.json`** (índices declarativos) — descartado: añade un parser y reduce type-safety. Marten ofrece API tipada vía `Schema.For<T>().Index(x => x.Field)`.
3. **Sub-extension methods** (`AddMartenIndexes`, `AddMartenSerializer`, ...) — descartado: fragmentación sin beneficio dado el tamaño.

**Consecuencias positivas:**
- Toda la verdad cabe en una pantalla (43 LOC).
- Onboarding inmediato: nuevo dev entiende el módulo en <1 minuto.
- Cambios futuros tocan un solo archivo.

**Consecuencias negativas:**
- Si el módulo crece (>200 LOC), la concentración se volverá una desventaja → trocear cuando ese umbral se alcance.

**Evidencia:** `Services.cs` (43 LOC totales, 1 método público). 🟢

---

## DD-04 — Índices secundarios elegidos por análisis de queries reales

**Decisión:** Se declaran 7 índices secundarios (3 sobre `GameSession`, 4 sobre `HandRecord` incluido uno compuesto). Todos los demás campos quedan sin índice.

**Contexto:** Marten crea por defecto un único índice en la PK (`Id`). Cualquier query frecuente sobre otro campo escaneará la tabla completa salvo que se declare índice. Los índices fueron seleccionados tras observar las queries de:
- `GameLoggerService.GetRecentSessionsWithStatsAsync` — filtro/orden por `EndTime`, agrupa por `TableName`.
- `GameLoggerService.GetHandsForSessionAsync` — filtra por `GameSessionId`, ordena por `Timestamp` (de ahí el índice compuesto).
- `OpponentTracker` y análisis estadístico — agrupan por `HeroPosition`.

**Alternativas consideradas:**
1. **Sin índices, depender de scan secuencial** — descartado: a partir de ~10K manos, los queries del Historial se vuelven lentos (>1s).
2. **Índice GIN sobre todo el JSONB** — descartado: consumo de espacio enorme, beneficio marginal sobre los campos accedidos.
3. **Índices duplicativos** (ej: `(GameSessionId)` + `(GameSessionId, Timestamp)`) — descartado: el compuesto ya cubre queries que filtran solo por `GameSessionId` (left-prefix rule). **Sin embargo, el código actual declara ambos** (`Services.cs:27` + `Services.cs:30`) — duplicación leve, ver lacuna.

**Consecuencias positivas:**
- Queries del Historial completan en <50ms hasta ~100K manos.
- Índice compuesto `(GameSessionId, Timestamp)` cubre el caso "manos de una sesión ordenadas por fecha" de un solo escaneo.

**Consecuencias negativas:**
- Inserts pagan el costo de mantener 4 índices sobre `HandRecord` y 3 sobre `GameSession`. Para un game loop a 1-2 Hz no es problema (1 hand cada ~30 segundos típicamente).
- 🟡 **Índice simple `(GameSessionId)` redundante con el compuesto** (`Services.cs:27` vs `Services.cs:30`). Postgres usa el compuesto por left-prefix; el simple solo se justifica si hay queries por `GameSessionId` con `ORDER BY` distinto de `Timestamp`. Verificar consumidores.
- 🔴 **Sin índice sobre `HandRecord.HeroPosition` filtrado por sesión** — un query "manos de hero en BTN en la sesión X" haría scan del compuesto. Si ese análisis es frecuente, considerar un compuesto adicional `(GameSessionId, HeroPosition)`.

**Evidencia:** `Services.cs:22-32` + queries en `GameLoggerService.cs:298-337`, `BankrollTrackerService.cs:36-250`. 🟢

---

## DD-05 — Auto-creación de esquema solo en development

**Decisión:** `options.AutoCreateSchemaObjects = AutoCreate.All` se activa **únicamente** si el parámetro `IsDevelopment == true`.

**Contexto:** Marten puede crear/modificar tablas e índices automáticamente al detectar nuevos tipos. En development esto acelera el ciclo (no migraciones manuales). En producción, con múltiples instancias arrancando concurrentemente, podría provocar races durante la migración del esquema.

**Alternativas consideradas:**
1. **`AutoCreate.All` siempre** — descartado: race conditions en deploy multi-instancia, peligro en producción.
2. **`AutoCreate.None` siempre** — descartado: development se vuelve frustrante (cada cambio de schema requiere intervención manual).
3. **`AutoCreate.CreateOrUpdate`** — más conservador, pero igual con races potenciales en multi-instancia.
4. **Auto-creación gobernada por `appsettings.json`** — descartado: redundante con `IsDevelopment`.

**Consecuencias positivas:**
- Development sin fricción: añadir un campo a `HandRecord` y reiniciar la app es suficiente.
- Producción protegida: el deploy debe ejecutar migración explícita (en teoría — ver lacuna 🔴 sobre política de migración).

**Consecuencias negativas:**
- 🔴 **No hay política de migración para producción documentada.** El binario de producción no migrará el esquema, pero tampoco se ha definido un mecanismo formal (Marten CLI, scripts SQL versionados, etc.).
- 🔴 **`Program.cs:52` pasa `IsDevelopment=true` hardcoded.** Ese hardcoded subvierte la decisión: en una build de producción deployada con `dotnet publish ... --configuration Release`, **el código sigue creyendo que está en development**.

**Evidencia:** `Services.cs:36-39`; uso del flag en `Program.cs:52`. 🟢 (decisión) / 🔴 (implementación rota por hardcoded en App).

**ADR relacionado:** ADR-0003.

---

## DD-06 — Sesiones cortas (`await using`) en lugar de sesión por scope DI

**Decisión:** Todos los consumidores reciben `IDocumentStore` y abren sesiones cortas vía `store.LightweightSession()` o `store.QuerySession()`, dispuestas con `await using` por operación. No se inyecta `IDocumentSession` directamente.

**Contexto:** Marten ofrece dos patrones:
- **Sesión por scope** — Marten registra `IDocumentSession` como scoped y permite inyectarla en use cases. La sesión vive lo que dura el scope (típicamente una request HTTP).
- **Sesión por operación** — el consumidor recibe `IDocumentStore` (singleton) y abre/cierra una sesión por cada operación.

Este proyecto es WinForms, **no HTTP**, por lo que no existe el concepto de "request scope" natural. Las operaciones son disparadas por eventos UI o por el game loop.

**Alternativas consideradas:**
1. **Sesión por scope DI con un scope manual** — descartado: introduce ceremony (crear/disponer scope) sin beneficio.
2. **Sesión singleton compartida** — descartado: las sesiones de Marten no son thread-safe; compartirla entre threads del game loop produciría errores.
3. **Sesión por hand (game session = sesión Marten)** — descartado: una sesión Marten viva durante 1+ hora consume conexión del pool y bloquea cleanup.

**Consecuencias positivas:**
- 🟢 Patrón uniforme verificado en 12+ archivos consumidores (`legacy-mapping.md`).
- Cada operación libera la conexión inmediatamente al pool — escalabilidad multi-instancia.
- Errores aislados: un fallo en una operación no contamina la siguiente.
- Test setup más simple: cada test abre su propia sesión.

**Consecuencias negativas:**
- Operaciones que necesitan transacción multi-paso deben coordinar la sesión explícitamente (no se ha visto en el código actual; ningún caso transaccional complejo).
- Pequeño overhead por apertura/cierre de sesión vs reutilizar (~ms, despreciable comparado con OCR/Monte Carlo).

**Evidencia:** `legacy-mapping.md` filas 71-83 (12+ consumidores con patrón uniforme); ejemplos en `GameLoggerService.cs:239,274,298,311,324,337`. 🟢

**ADR relacionado:** ADR-0003.

---

## DD-07 — `IsDevelopment` como parámetro explícito en lugar de leer `IHostEnvironment`

**Decisión:** El extension method recibe `IsDevelopment` como `bool` por parámetro. No introspecta `IHostEnvironment` ni `Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")` internamente.

**Contexto:** El llamador (`Program.cs:52`) decide qué valor pasar. La firma del método es agnóstica al host.

**Alternativas consideradas:**
1. **Leer `IHostEnvironment` desde DI dentro del método** — descartado: acopla el módulo al modelo de hosting de Microsoft.Extensions.Hosting, que en una WinForms app desktop es opcional.
2. **Leer `Environment.GetEnvironmentVariable` directamente** — descartado: hace el método no-testeable (requiere mutar variables de entorno).
3. **Sobrecarga sin parámetro que asuma `IsDevelopment=false` en producción** — alternativa razonable, no implementada.

**Consecuencias positivas:**
- 🟢 Método 100% testeable: pasar `true`/`false` desde el test.
- Sin acoplamiento a Microsoft.Extensions.Hosting (módulo solo depende de `Configuration.Abstractions`).

**Consecuencias negativas:**
- 🔴 **El llamador puede equivocarse**: y de hecho lo hace (`Program.cs:52` pasa `true` hardcoded). Una sobrecarga que detecte `DOTNET_ENVIRONMENT` automáticamente reduciría el riesgo.
- La firma con `bool` plano es susceptible al "boolean trap" (`AddDataBase(cfg, true)` no es auto-documentante; un `enum AutoCreatePolicy { Development, Production }` sería más legible).

**Evidencia:** `Services.cs:11` (signature); `Program.cs:52` (uso). 🟢 (decisión) / 🔴 (consumidor rompe el contrato esperado).

---

## DD-08 — `Ardalis.Result` declarado pero no consumido

**Decisión (implícita, probablemente accidental):** El `OpenScrape.Infrastructure.csproj` declara `<PackageReference Include="Ardalis.Result" Version="10.1.0" />` pero ningún archivo del módulo lo importa o usa.

**Contexto:** El paquete `Ardalis.Result` envuelve operaciones en `Result<T>` con metadatos (Success/Error/NotFound, etc.). Es usado activamente en `OpenScrape.Features` para los use cases. Su presencia en `OpenScrape.Infrastructure.csproj` parece copia-pega del csproj de Features durante el scaffolding inicial.

**Alternativas a futuro:**
1. **Eliminar la dependencia** (Won't / clean — recomendado).
2. **Usarla**: envolver `AddDataBase` en un retorno `Result<IDocumentStore>` para dar feedback estructurado al host. Probablemente over-engineering.

**Consecuencias actuales:**
- 🟡 Dependencia transitiva no usada → desperdicio de espacio mínimo, riesgo cero.
- Confusión leve para alguien que lea el csproj y busque dónde se consume.

**Evidencia:** `OpenScrape.Infrastructure.csproj:12`; `grep "Ardalis" src/OpenScrape.Infrastructure/` retorna vacío. 🟡

**Recomendación:** eliminar en T-18 si la decisión humana lo permite.

---

## DD-09 — `<NoWarn>NU1902</NoWarn>` para silenciar vulnerabilidad transitiva

**Decisión:** El csproj suprime el warning NU1902 globalmente. NU1902 reporta una vulnerabilidad en `OpenTelemetry.Api`, transitivamente arrastrada por `Marten 8.24.0`.

**Contexto:** Marten 8.24.0 depende de `OpenTelemetry.Api` para emitir traces opcionales. Una versión específica está marcada como vulnerable por NuGet Audit. El módulo no consume OpenTelemetry directamente, por lo que la superficie de ataque real es nula (no hay código vulnerable invocado), pero el warning obstruye el build limpio.

**Alternativas consideradas:**
1. **Forzar override de la versión** — descartado: requiere conocer una versión segura compatible con Marten, mantenimiento continuo.
2. **Esperar la actualización de Marten** — pendiente: cuando Marten 8.25+ refresque la dependencia, eliminar el `<NoWarn>`.
3. **Suprimir solo en este proyecto** — implementado actualmente.
4. **Quitar Marten** — DD-01 ya descarta esta opción.

**Consecuencias positivas:**
- Build limpio sin advertencias falso-positivas.
- Documentado in-line con comentario explicativo (`OpenScrape.Infrastructure.csproj:7`).

**Consecuencias negativas:**
- 🟡 **Riesgo latente**: si una versión futura de OpenTelemetry introduce una vulnerabilidad **explotable** y NuGet emite el mismo NU1902, el silenciamiento la ocultaría. Mitigación: revisar al actualizar Marten.

**Evidencia:** `OpenScrape.Infrastructure.csproj:7-8`. 🟡

---

## DD-10 — `Schema.For<T>()` con tipos del Domain (no DTOs)

**Decisión:** Marten persiste los tipos de Domain directamente (`GameSession`, `HandRecord`), no DTOs intermedios.

**Contexto:** El proyecto tiene DTOs (`CardDTO`, `TableDTO`, `SessionStatsDto`) pero son para serialización entre capas/UI, no para persistencia. Los documentos persistidos son las entidades de dominio.

**Alternativas consideradas:**
1. **DTO intermedio para persistencia** (`HandRecordDocument` que se mappea a `HandRecord`) — descartado: duplicación de tipos, mappers boilerplate.
2. **Anémicos** (campos primitivos en lugar de tipos compuestos) — descartado: Marten serializa structs/clases anidadas sin problema.

**Consecuencias positivas:**
- 0 mappers entre dominio y persistencia.
- Cambios en el modelo se reflejan inmediatamente en el documento (con auto-creación de schema en dev).

**Consecuencias negativas:**
- Tipos de dominio quedan acoplados al serializer (no pueden tener referencias circulares, propiedades no-serializables, etc.).
- Si el dominio cambia, los documentos persistidos quedan desactualizados → versionado del documento implícito (Marten gestiona esquemas evolutivos).

**Evidencia:** `Services.cs:22-32` referencia tipos de `OpenScrape.Domain.Entities` directamente. 🟢

---

## Resumen de decisiones por fuerza/debilidad

| ID | Decisión | Fortaleza | Riesgo / lacuna |
|----|----------|-----------|-----------------|
| DD-01 | Marten en lugar de EF Core | 🟢 simplicidad de bootstrap | 🟡 vulnerabilidad transitiva NU1902 |
| DD-02 | System.Text.Json | 🟢 cero deps adicionales, performance | — |
| DD-03 | Bootstrap monolítico (43 LOC) | 🟢 onboarding inmediato | trocear si crece |
| DD-04 | 7 índices secundarios elegidos | 🟢 queries del Historial rápidas | 🟡 índice simple `(GameSessionId)` redundante con el compuesto |
| DD-05 | `AutoCreate.All` solo en dev | 🟢 separación clara dev/prod | 🔴 hardcoded a `true` en `Program.cs:52` rompe la decisión |
| DD-06 | Sesiones cortas `await using` | 🟢 patrón uniforme y testeable | — |
| DD-07 | `IsDevelopment` como parámetro | 🟢 testeable | 🔴 boolean trap en consumidor |
| DD-08 | `Ardalis.Result` no usado | — | 🟡 deuda de limpieza |
| DD-09 | `<NoWarn>NU1902</NoWarn>` | 🟢 build limpio | 🟡 riesgo latente al actualizar Marten |
| DD-10 | Persistir tipos de Domain | 🟢 sin mappers | acopla Domain al serializer |
