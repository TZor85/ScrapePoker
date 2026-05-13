# OpenScrape.Infrastructure — Tareas de Implementación

> Secuencia de tareas para reimplementar la frontera DI con Marten/PostgreSQL a partir del legado, con rastreabilidad línea a línea. **Módulo deliberadamente mínimo (43 LOC, 1 archivo)**: la mayoría de las tareas son de hardening y observabilidad, no de funcionalidad nueva.

---

## Pré-requisitos

- [ ] .NET 10 SDK instalado.
- [ ] PostgreSQL 14+ disponible (local para desarrollo, gestionado para prod). Marten 8.x exige extensiones `pg_trgm` y `plv8` no son requeridas; sí requiere permiso `CREATE TABLE/INDEX` para auto-creación en development.
- [ ] `OpenScrape.Domain` ya compilable (provee `GameSession`, `HandRecord`, etc.).
- [ ] Decisiones humanas pendientes (ver `questions.md`):
  - [ ] ¿`IsDevelopment` debe leer `IHostEnvironment` o seguir hardcoded a `true`?
  - [ ] ¿La connection string sale a un secret store o `User Secrets` (placeholder en `appsettings.json` con `CHANGE_ME`)?
  - [ ] ¿Se introduce health check + retry policy?
  - [ ] ¿Se elimina `Ardalis.Result` del csproj?
  - [ ] ¿Política de migración en producción (manual / automatizada / declarativa)?

---

## Tareas

> Cada tarea referencia el archivo legado de origen y su número de línea cuando aplica.

### Bloque A — Estructura del proyecto

- [ ] **T-01** Crear `OpenScrape.Infrastructure.csproj` con `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` y `<NoWarn>$(NoWarn);NU1902</NoWarn>` (vulnerabilidad transitiva de Marten/OpenTelemetry).
  - Origen en el legado: `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj:1-22`
  - Critério de pronto: `dotnet build` pasa sin warnings (excepto los suprimidos).
  - Confianza: 🟢

- [ ] **T-02** Declarar las dependencias NuGet: `Marten 8.24.0`, `Microsoft.Extensions.Configuration.Abstractions 10.0.3`, `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.3`. **Decidir** si se conserva `Ardalis.Result 10.1.0` (no usado en este módulo).
  - Origen en el legado: `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj:11-16`
  - Critério de pronto: `dotnet restore` exitoso. Si se descarta `Ardalis.Result`, registrar en `discard_log.md` y verificar que ningún archivo del módulo lo usa (`grep "Ardalis"` vacío).
  - Confianza: 🟢 (Marten + MEC) / 🟡 (Ardalis.Result — decisión humana)

- [ ] **T-03** Declarar la `<ProjectReference>` exclusiva a `OpenScrape.Domain`. Verificar que **no** existe referencia a App/Features/DecisionMaker.
  - Origen en el legado: `src/OpenScrape.Infrastructure/OpenScrape.Infrastructure.csproj:18-20`
  - Critério de pronto: `dotnet list reference` muestra una sola entrada hacia `OpenScrape.Domain`.
  - Confianza: 🟢

### Bloque B — Bootstrap Marten (núcleo del módulo)

- [ ] **T-04** Crear `Services.cs` con `namespace OpenScrape.Infrastructure;` y `public static class Services`.
  - Origen en el legado: `src/OpenScrape.Infrastructure/Services.cs:7-9`
  - Critério de pronto: namespace correcto, clase `static` declarada.
  - Confianza: 🟢

- [ ] **T-05** Implementar el extension method `public static void AddDataBase(this IServiceCollection services, IConfiguration configuration, bool IsDevelopment)`.
  - Origen en el legado: `src/OpenScrape.Infrastructure/Services.cs:11`
  - Critério de pronto: la firma es exactamente esta (mismos tipos, mismo nombre `IsDevelopment` con C mayúscula). Modificarla rompería `Program.cs:52`.
  - Confianza: 🟢

- [ ] **T-06** Dentro de `AddDataBase`, llamar `services.AddMarten(options => { ... })`.
  - Origen en el legado: `Services.cs:13`
  - Critério de pronto: tras invocar `AddDataBase`, `sp.GetRequiredService<IDocumentStore>()` retorna no-null y la misma instancia entre dos resoluciones.
  - Confianza: 🟢

- [ ] **T-07** Configurar `options.Connection(configuration.GetConnectionString("DefaultConnection")!)`. **DECISIÓN PENDIENTE** (🔴): ¿conservar el null-forgiving `!` o introducir guard explícito que lance `InvalidOperationException("Connection string 'DefaultConnection' no encontrada.")`?
  - Origen en el legado: `Services.cs:16`
  - Critério de pronto: con cadena válida en `IConfiguration`, Marten arranca; con cadena ausente, el comportamiento es **predecible y diagnosticable** (no NRE silencioso).
  - Confianza: 🟢 (comportamiento legacy) / 🔴 (decisión de hardening)

- [ ] **T-08** Configurar `options.UseSystemTextJsonForSerialization()`.
  - Origen en el legado: `Services.cs:19`
  - Critério de pronto: `store.Options.Serializer().GetType().Name` contiene `"SystemTextJson"`. Test de round-trip serialice/deserialice un `GameSession` con caracteres Unicode.
  - Confianza: 🟢

- [ ] **T-09** Declarar los **3 índices** sobre `GameSession`:
  ```csharp
  options.Schema.For<GameSession>().Index(x => x.EndTime);
  options.Schema.For<GameSession>().Index(x => x.SessionId);
  options.Schema.For<GameSession>().Index(x => x.TableName);
  ```
  - Origen en el legado: `Services.cs:22-24`
  - Critério de pronto: tras primer `SaveChangesAsync`, `pg_indexes` muestra 3 índices secundarios sobre `mt_doc_gamesession`.
  - Confianza: 🟢

- [ ] **T-10** Declarar los **4 índices** sobre `HandRecord` (incluye uno compuesto):
  ```csharp
  options.Schema.For<HandRecord>().Index(x => x.GameSessionId);
  options.Schema.For<HandRecord>().Index(x => x.Timestamp);
  options.Schema.For<HandRecord>().Index(x => new { x.GameSessionId, x.Timestamp });
  options.Schema.For<HandRecord>().Index(x => x.HeroPosition);
  ```
  - Origen en el legado: `Services.cs:27-32`
  - Critério de pronto: `pg_indexes` muestra 4 índices sobre `mt_doc_handrecord`, uno de ellos compuesto.
  - Confianza: 🟢

- [ ] **T-11** Configurar la auto-creación condicional:
  ```csharp
  if (IsDevelopment)
  {
      options.AutoCreateSchemaObjects = AutoCreate.All;
  }
  ```
  - Origen en el legado: `Services.cs:36-39`
  - Critério de pronto: con `IsDevelopment=false`, la propiedad **no** se setea a `All` (queda en el default que Marten provee). Test verifica con `store.Options.AutoCreateSchemaObjects`.
  - Confianza: 🟢

### Bloque C — Hardening (decisiones 🔴)

- [ ] **T-12** Decidir y documentar la fuente de `IsDevelopment`:
  - **Opción A (status quo, frágil):** mantener `Program.cs:52` con `services.AddDataBase(context.Configuration, true);` hardcoded.
  - **Opción B (recomendada):** `services.AddDataBase(context.Configuration, context.HostingEnvironment.IsDevelopment());`.
  - Origen en el legado: `src/OpenScrape.App/Program.cs:52`
  - Critério de pronto: decisión registrada en `decisions.md`; si se elige Opción B, validar que `DOTNET_ENVIRONMENT` está configurado correctamente en `launchSettings.json` y en deploy targets.
  - Confianza: 🔴 (decisión humana pendiente)

- [ ] **T-13** **(Si se elige hardening de connection string)** Sustituir `GetConnectionString(...)!` por:
  ```csharp
  var cs = configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' no encontrada en IConfiguration.");
  options.Connection(cs);
  ```
  - Origen en el legado: `Services.cs:16` (caso problemático)
  - Critério de pronto: arrancar con `IConfiguration` vacío produce `InvalidOperationException` con mensaje claro, no `NullReferenceException`.
  - Confianza: 🟡 (mejora opcional, decisión humana)

- [ ] **T-14** **(Si se elige sanear secrets)** Reemplazar la connection string committeada en `appsettings.json` por placeholder `CHANGE_ME` y mover credenciales reales a `appsettings.Development.json` (gitignored) o User Secrets.
  - Origen en el legado: `src/OpenScrape.App/appsettings.json:3` (anomalía Scout)
  - Critério de pronto: `git log --all -p appsettings.json` no expone credenciales reales en futuros commits; `dotnet user-secrets` o `appsettings.Development.json` se usan para desarrollo local.
  - Confianza: 🔴 (decisión + remediación de seguridad)

- [ ] **T-15** **(Opcional, futuro)** Añadir health check de PostgreSQL: `services.AddHealthChecks().AddNpgSql(connectionString)`.
  - Origen en el legado: ninguno (mejora propuesta)
  - Critério de pronto: endpoint `/health` (si se expone) o llamada explícita en arranque reporta el estado de la DB.
  - Confianza: 🟡 (mejora opcional)

- [ ] **T-16** **(Opcional, futuro)** Considerar wrapping del bootstrap en `Result<IDocumentStore>` usando `Ardalis.Result` (ya declarado pero no usado), si se decide dar feedback estructurado al host.
  - Origen en el legado: `OpenScrape.Infrastructure.csproj:12` (paquete declarado, sin uso)
  - Critério de pronto: decisión documentada. **Recomendación:** descartar (Won't), mantener `void` simple.
  - Confianza: 🔴 (decisión humana)

### Bloque D — Pulido

- [ ] **T-17** Limpiar comentarios in-line de `Services.cs` (mezcla castellano/inglés del scaffolding original Marten). Estandarizar al `doc_language=Español`.
  - Origen en el legado: `Services.cs:14-39`
  - Critério de pronto: comentarios homogéneos; sin TODOs huérfanos.
  - Confianza: 🟢

- [ ] **T-18** **(Si se descarta `Ardalis.Result`)** Eliminar el `<PackageReference Include="Ardalis.Result" .../>` del csproj.
  - Origen en el legado: `OpenScrape.Infrastructure.csproj:12`
  - Critério de pronto: `dotnet restore` y `dotnet build` siguen pasando; `grep -r "Ardalis"` en el módulo retorna vacío.
  - Confianza: 🟢

---

## Tareas de Test

- [ ] **TT-01** Test del happy path: `services.AddDataBase(cfg, true)` registra `IDocumentStore` resoluble como singleton (RF-01).
- [ ] **TT-02** Test de configuración: `options.Connection` recibe la cadena de `IConfiguration.GetConnectionString("DefaultConnection")` (RF-02). Usar reflexión sobre `StoreOptions` o un test de integración con cadena conocida.
- [ ] **TT-03** Test de serializer: round-trip de `GameSession` con texto Unicode preserva todos los caracteres (RF-03).
- [ ] **TT-04** **Test de integración** contra Postgres local: tras `AddDataBase` + `SaveChangesAsync` con un `GameSession` y un `HandRecord`, query a `pg_indexes` muestra los 7 índices declarados (RF-04, RF-05).
- [ ] **TT-05** Test condicional: con `IsDevelopment=false`, `store.Options.AutoCreateSchemaObjects` no es `AutoCreate.All` (RF-06).
- [ ] **TT-06** Test de aislamiento: inspección estática de `Services.cs` confirma que ningún `services.AddSingleton/Scoped/Transient` se invoca aparte de `AddMarten` (RF-07, RF-09).
- [ ] **TT-07** Test de doble registro: `services.AddDataBase(cfg, true)` invocado dos veces no rompe el contenedor (RF-08).
- [ ] **TT-08** Test de connection string ausente: con `IConfiguration` que no contiene `DefaultConnection`, el comportamiento esperado tras T-13 es `InvalidOperationException` (no `NullReferenceException`).
- [ ] **TT-09** Test de aislamiento de capas: `OpenScrape.Infrastructure` se compila sin acceso a `OpenScrape.App`/`OpenScrape.Features`/`OpenScrape.DecisionMaker` (RF-10).

---

## Tareas de Migración de Datos

> El módulo no realiza migraciones programáticas. La política operativa actual depende de `AutoCreate.All` en development y de la **ausencia de migración explícita** en producción (🔴 lacuna conocida).

- [ ] **TM-01** Documentar la política de despliegue para producción: ¿migración manual con scripts dump-restore? ¿Marten CLI (`dotnet marten --apply`)? ¿Migración automatizada en CI/CD pre-deploy?
  - Origen en el legado: ninguno (lacuna explícita)
  - Critério de pronto: decisión documentada en `decisions.md` o ADR adicional.
  - Confianza: 🔴

- [ ] **TM-02** **(Si se persiste `AutoCreate.All` en prod intencionalmente)** Documentar el riesgo de race conditions cuando múltiples instancias arrancan en paralelo. Mitigación: feature flag para habilitar solo en una instancia "líder".
  - Origen en el legado: `Services.cs:36-39` + `Program.cs:52` hardcoded
  - Critério de pronto: ADR explícito sobre la decisión.
  - Confianza: 🔴

---

## Ordem Sugerida

1. **T-01 a T-03** (proyecto y referencias). Base sin la cual nada compila.
2. **T-04 a T-11** (bootstrap funcional). Reproduce el comportamiento legacy 1:1.
3. **TT-01 a TT-09** (tests). Fija el contrato antes de modificar.
4. **T-12 a T-14** (hardening de configuración). Decisiones humanas que pueden tocar `Program.cs` y `appsettings.json` — **no son bloqueantes** del build.
5. **T-15, T-16** (mejoras opcionales). Solo si el equipo decide invertir.
6. **T-17, T-18** (limpieza). Última fase, sin riesgo.
7. **TM-01, TM-02** (estrategia de migración prod). Pre-requisito para deploy real.

**Bloqueos cruzados:**
- T-05 (extension method) requiere T-01 (csproj).
- T-09/T-10 (índices) requieren T-08 (serializer) declarado primero, por convención del DSL Marten.
- T-12 (refactor `IsDevelopment`) toca `Program.cs`, fuera del módulo — coordinar con la unit `OpenScrape.App`.
- TT-04 requiere PostgreSQL accesible en CI; si no, marcar como `[Category("Integration")]` y excluir del run unitario.

---

## Lacunas Pendentes (🔴)

1. **¿`IsDevelopment` lee `IHostEnvironment` o sigue hardcoded a `true`?** (T-12) — Impacto: si se ejecuta en producción con hardcoded `true`, Marten intentará migrar el esquema en cada arranque, con riesgo de races entre instancias.
2. **¿La connection string committeada en `appsettings.json` debe revertirse a placeholder `CHANGE_ME`?** (T-14) — Impacto: secrets en repositorio histórico (anomalía Scout pendiente de remediación).
3. **¿Se introduce guard explícito ante connection string ausente?** (T-13) — Impacto: error operativo más diagnosticable, ningún cambio de comportamiento para casos normales.
4. **¿Política de migración de esquema en producción?** (TM-01) — Impacto crítico para deploy real. Sin esta decisión, no se puede liberar la app a entornos productivos seguros.
5. **¿Health check + retry policy?** (T-15) — Impacto: tolerancia a caídas transitorias de la base; útil si Postgres es externo (Neon/cloud).
6. **¿Eliminar `Ardalis.Result` del csproj?** (T-18) — Limpieza estética, sin riesgo.
