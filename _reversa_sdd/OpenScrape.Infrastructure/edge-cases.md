# OpenScrape.Infrastructure — Casos Extremos

> Casos límite de la frontera DI con Marten/PostgreSQL detectados en el código y la configuración actual, con comportamiento esperado, comportamiento real y consecuencias si se ignoran.

---

## EC-01 — `IConfiguration` sin `ConnectionStrings:DefaultConnection`

**Disparador:** Arrancar la app con un `appsettings.json` que omita la sección `ConnectionStrings` o cuya clave `DefaultConnection` sea `null`/string vacía.

**Comportamiento real** (🔴 `Services.cs:16`):
- `configuration.GetConnectionString("DefaultConnection")` retorna `null`.
- El operador `!` (null-forgiving) le indica al compilador que ignore el null pero **no genera ningún check en runtime**.
- `options.Connection(null)` se invoca → Marten 8 internamente lanza `ArgumentNullException` o `NpgsqlException` con mensaje crípticamente envuelto en `BuildServiceProvider()` o en el primer `OpenSession()`.

**Comportamiento esperado (tras hardening T-13):**
- `InvalidOperationException("Connection string 'DefaultConnection' no encontrada en IConfiguration.")` lanzado durante `services.AddDataBase(...)`.

**Consecuencias si se ignora:**
- Stack trace ofuscado en startup → tiempo perdido investigando.
- En entornos donde el usuario edita `appsettings.json` manualmente, error humano se manifiesta como crash sin pista clara.
- Telemetría/logs pre-Marten no reportan la fase del fallo.

**Cobertura test:** `TT-08` (input: `IConfiguration` mock vacío).

---

## EC-02 — Connection string sintácticamente correcta pero apuntando a host inexistente

**Disparador:** `Host=127.0.0.1;Database=does_not_exist;Username=fake;Password=fake` con servicio Postgres no levantado.

**Comportamiento real** (🟢 lazy connect):
- `services.AddDataBase` y `BuildServiceProvider()` **completan sin error** (Marten difiere la conexión).
- El primer consumidor que invoca `store.LightweightSession()` o `store.QuerySession()` recibe `Npgsql.NpgsqlException` ("No connection could be made because the target machine actively refused it" o similar).
- En `FrmMain.OnLoad`, esto puede propagarse al `await session.SaveChangesAsync()` y romper el inicio de sesión.

**Comportamiento esperado:** debería existir un health check que reporte el estado degradado **antes** de que el game loop arranque, dando feedback claro al usuario en la UI ("Base de datos no disponible — historial deshabilitado").

**Consecuencias si se ignora:**
- App arranca, OCR funciona, primera mano "se pierde" silenciosamente al persistir.
- Stack trace aparece en `Console.WriteLine`/log, pero la UI no lo refleja.

**Cobertura test:** `TT-04` parcial (caso negativo). **Lacuna 🟡**: añadir health check (T-15).

---

## EC-03 — `IsDevelopment=true` hardcoded en producción (deuda actual)

**Disparador:** Build de producción ejecutándose con `Program.cs:52` pasando `true` literalmente.

**Comportamiento real** (🔴 `Program.cs:52`):
- Marten cree estar en development.
- `AutoCreateSchemaObjects = AutoCreate.All` se aplica.
- En el primer `SaveChangesAsync`, Marten ejecuta `CREATE TABLE IF NOT EXISTS ...` y `CREATE INDEX ...` contra la base productiva.
- Si dos instancias de la app arrancan en paralelo → race en el DDL.
- Si la base no permite `CREATE TABLE` al usuario configurado → fallo en el primer write.

**Comportamiento esperado** (tras T-12 Opción B):
- `services.AddDataBase(context.Configuration, context.HostingEnvironment.IsDevelopment())` lee la fuente correcta.
- `AutoCreate.None` se aplica, Marten asume esquema pre-existente.

**Consecuencias si se ignora:**
- Despliegue de v2 con un nuevo campo en `HandRecord` ⇒ Marten intentará `ALTER TABLE` automáticamente. Sin backup ni revisión, riesgo de pérdida de datos.
- Auditoría de seguridad ve "permisos DDL en runtime" como red flag.

**Cobertura test:** ninguna actualmente. **Pendiente**: integration test con `IsDevelopment=false` que verifique que `store.Options.AutoCreateSchemaObjects != AutoCreate.All`.

---

## EC-04 — `AddDataBase` invocado dos veces sobre el mismo `IServiceCollection`

**Disparador:**
```csharp
services.AddDataBase(cfg, true);
services.AddDataBase(cfg, true);  // por error de copy-paste o en test setup
```

**Comportamiento real** (🟡 sin verificación):
- Marten 8 internamente registra `IDocumentStore` como singleton vía `services.AddSingleton<IDocumentStore, DocumentStore>` (o factory equivalente).
- DI .NET permite múltiples registros del mismo tipo; el último wins durante la resolución de **un solo servicio**, pero `GetServices<IDocumentStore>()` retornaría dos.
- `BuildServiceProvider()` no lanza, pero el comportamiento es indeterminado si Marten registra **múltiples índices duplicados** internamente.

**Comportamiento esperado:** la operación debería ser idempotente. **Lacuna 🟡**: validar con un test (TT-07) cuál es el comportamiento exacto de Marten 8.24.0.

**Consecuencias si se ignora:**
- Logs de Marten potencialmente duplicados (uno por registro).
- Test setup que invoca `AddDataBase` en un fixture compartido podría producir resultados inconsistentes entre tests.

**Cobertura test:** `TT-07` (verificación explícita).

---

## EC-05 — Connection string con caracteres especiales en password (escaping)

**Disparador:** `Password=p@ss;w=rd` (incluye `;` que delimita campos en la connection string).

**Comportamiento real** (🟢 delegado a Npgsql):
- Npgsql parsea la cadena buscando separadores `;`. Sin escaping, `Password=p@ss` y `w=rd` se interpretan como dos pares.
- Resultado: autenticación falla con error críptico ("password authentication failed").

**Comportamiento esperado:**
- Si las credenciales contienen `;`, deben usarse comillas: `Password="p@ss;w=rd"`.
- Documentar el formato en `appsettings.json` como comentario o en el README.

**Consecuencias si se ignora:**
- Falla intermitente cuando el usuario rota el password a uno con `;` o `=`.
- Sin logging diferenciador entre "password incorrecto" y "password mal escapado".

**Cobertura test:** ninguna en este módulo (responsabilidad de Npgsql). **Documentar** en `requirements.md` RNF de internacionalización/configuración.

---

## EC-06 — Tabla `mt_doc_handrecord` sin índices declarados (esquema legacy)

**Disparador:** Despliegue contra una DB existente que tiene la tabla `mt_doc_handrecord` pero sin los índices declarados en `Services.cs:27-32` (porque la creó una versión anterior del binario).

**Comportamiento real** (🟢 con `IsDevelopment=true`):
- Auto-create detecta los índices faltantes y los crea con `CREATE INDEX IF NOT EXISTS`.
- Posible bloqueo de la tabla durante la creación (índices pequeños son rápidos).

**Comportamiento real** (🟢 con `IsDevelopment=false`):
- Marten **no** crea los índices.
- Queries posteriores ejecutan `Seq Scan` y degradan rendimiento.
- `EXPLAIN ANALYZE` mostraría el problema.

**Consecuencias si se ignora:**
- Latencia del Historial pasa de <50ms a >500ms con ~50K manos.
- Sin alertas operacionales que lo detecten.

**Cobertura test:** TT-04 verifica los 7 índices presentes; no cubre el caso de migración entre versiones. **Lacuna 🔴**: política de migración no formalizada (TM-01).

---

## EC-07 — Marten serializer rechaza un tipo polimórfico en `HandRecord`

**Disparador:** Si en el futuro `HandRecord.Decisions` cambia de `List<StreetDecision>` a `List<IStreetDecision>` (interface).

**Comportamiento real** (🟡 STJ estricto):
- System.Text.Json no serializa polimorfismo por defecto sin `JsonDerivedType` o un `JsonConverter` custom.
- Round-trip pierde la subclase concreta (deserializa al tipo base sin discriminator).

**Comportamiento esperado:** documentar restricciones del serializer; si se requiere polimorfismo, registrar `JsonConverter` en `options.UseSystemTextJsonForSerialization(jsonOptions => jsonOptions.Converters.Add(...))`.

**Consecuencias si se ignora:**
- Evolución silenciosa del modelo rompe la persistencia sin error visible (datos guardados sin la subclase, queries leen base genérica).
- Detectado solo cuando un consumidor falla al cast a la subclase esperada.

**Cobertura test:** ninguna actualmente. **Lacuna 🟡**: documentar en RNF.

---

## EC-08 — Pool de conexiones agotado bajo carga sostenida

**Disparador:** Game loop a alta frecuencia (1-2 Hz) con cada operación abriendo una nueva sesión durante varias horas; o telemetría de fondo escribiendo en paralelo.

**Comportamiento real** (🟢 gestionado por Npgsql):
- Default Npgsql pool size: `MaxPoolSize=100`, `Timeout=15s`.
- Sesiones cortas (`await using`) liberan al pool < 10ms en operaciones normales → muy lejos del cap.
- Si por error un consumidor olvida el `await using`, la conexión queda viva → tras 100 olvidos (o 100 leaks), `OpenSession()` lanza `NpgsqlException("The connection pool has been exhausted")`.

**Comportamiento esperado:**
- Pattern verificado en el legado: 12+ consumidores documentados usan `await using` correctamente (`legacy-mapping.md`).
- 🟡 Si un nuevo consumidor olvida `await using`, no hay safeguard.

**Consecuencias si se ignora:**
- App degradada gradualmente: las primeras 100 operaciones pasan, luego todo falla con timeout.
- Detección requiere monitoring de `pg_stat_activity` (no implementado).

**Cobertura test:** ninguna específica al pool. **Mitigación**: code review obligatorio + analyzer custom que detecte uso de `IDocumentStore` sin `await using`.

---

## EC-09 — Connection string con SSL requirement no soportado por servidor

**Disparador:** Cadena con `SslMode=Require;Trust Server Certificate=true` apuntando a un Postgres local sin SSL configurado.

**Comportamiento real** (🟢 delegado a Npgsql):
- Npgsql lanza `NpgsqlException("The server requires SSL...")` o equivalente al primer `OpenSession()`.
- El módulo no lo intercepta — propaga al consumidor.

**Comportamiento esperado:** documentar la cadena válida para cada entorno (local, Neon cloud, etc.). El proyecto actualmente apunta a Neon (cloud-hosted), que **siempre** requiere SSL.

**Consecuencias si se ignora:**
- Pruebas locales con Postgres sin SSL fallan con error críptico para alguien que asume el modo cloud.

**Cobertura test:** ninguna. **Documentación**: añadir ejemplos de connection string por entorno en README.

---

## EC-10 — Concurrencia: dos instancias arrancando con `AutoCreate.All`

**Disparador:** Despliegue blue/green o rolling con dos procesos creando el esquema en paralelo.

**Comportamiento real** (🔴 race condition):
- Ambos procesos detectan tablas faltantes y lanzan `CREATE TABLE IF NOT EXISTS`.
- PostgreSQL serializa los DDL pero ambos invocan `CREATE INDEX IF NOT EXISTS`.
- Marten internamente versiona el esquema; conflicto en una tabla "leader-follower" interna puede dejar el esquema en estado inconsistente.

**Comportamiento esperado:**
- En producción, **solo una instancia** debería tener permisos DDL, o el deploy debería ejecutar la migración antes del rollout.
- Status quo (`IsDevelopment=true` hardcoded en `Program.cs:52`) no protege contra este escenario.

**Consecuencias si se ignora:**
- Deploy puede dejar la base en estado parcial → próximas instancias fallan al arrancar.
- Mitigación: locks distribuidos, leader election. **Recomendación**: no auto-crear en prod (T-12 Opción B).

**Cobertura test:** integration test multi-proceso (no implementado).

---

## EC-11 — `Schema.For<T>().Index(x => new { x.A, x.B })` con uno de los campos `null`

**Disparador:** Documentos `HandRecord` antiguos donde `GameSessionId` o `Timestamp` puedan ser `null`/default por migraciones incompletas.

**Comportamiento real** (🟢 Postgres):
- Índices en Postgres soportan valores `NULL` (los almacena al final por defecto).
- Las queries `WHERE GameSessionId IS NOT NULL` los excluyen.
- Si un consumidor filtra por `GameSessionId == someGuid` y el documento tiene `null`, simplemente no aparece.

**Comportamiento esperado:** los tipos en Domain no permiten `null` en campos críticos (`HandRecord.GameSessionId` es `Guid`, no `Guid?`). Si la deserialización STJ encuentra ausencia, asigna default `Guid.Empty` (silenciosamente).

**Consecuencias si se ignora:**
- Hands con `GameSessionId == Guid.Empty` aparecen como una "sesión virtual" en queries que no filtren.
- Sin alertas: las queries del Historial no muestran inconsistencias.

**Cobertura test:** ninguna. **Mitigación**: validar en `GameLoggerService.SaveHandRecord` que `GameSessionId != Guid.Empty` antes de persistir.

---

## EC-12 — Cancelación cooperativa: `CancellationToken` cancelado durante `SaveChangesAsync`

**Disparador:** `FrmMain.OnFormClosing` cancela el `CancellationTokenSource` global mientras un consumidor está en medio de un `await session.SaveChangesAsync(token)`.

**Comportamiento real** (🟢 Marten honra el token):
- `SaveChangesAsync(token)` propaga `OperationCanceledException`.
- El consumidor debe atraparlo en `try/catch` y registrar la mano como "no guardada" en el log (o reintentar al reabrir).

**Comportamiento esperado:** el módulo no maneja la cancelación; delega al consumidor. **Riesgo 🟡**: si el consumidor no atrapa, la excepción propaga al thread principal y puede crashear la UI.

**Consecuencias si se ignora:**
- Pérdida silenciosa de la última mano al cerrar la app abruptamente.
- Si el consumidor crashea, el `await using` sigue disponiendo la sesión → no leaks de conexión.

**Cobertura test:** revisar consumidores en `OpenScrape.App` (responsabilidad de esa unit, no de Infrastructure).

---

## EC-13 — `appsettings.Development.json` ausente con secret esperado

**Disparador:** Clonar el repo en una máquina nueva sin `appsettings.Development.json` (gitignored) y ejecutar la app.

**Comportamiento real** (🟢 fallback al `appsettings.json` base):
- `IConfiguration` lee solo `appsettings.json`.
- Connection string en `appsettings.json` actualmente contiene credenciales reales (anomalía Scout) → la app conecta sin problema.
- Si se aplica T-14 (placeholder `CHANGE_ME`), la cadena es inválida → EC-02.

**Comportamiento esperado** (tras T-14):
- Mensaje claro al usuario indicando que debe crear `appsettings.Development.json` con sus credenciales.
- Documentación en README explicando el setup.

**Consecuencias si se ignora:**
- Onboarding de nuevos devs frustrante.
- Fuga histórica de credenciales en commits pasados (anomalía Scout pendiente).

**Cobertura test:** ninguna en este módulo.

---

## EC-14 — Marten encuentra documentos con tipo no registrado (deserialización)

**Disparador:** Renombrar `HandRecord` a `HandHistoryRecord` sin migrar la tabla.

**Comportamiento real** (🟡 STJ + Marten):
- Marten guarda el FQN del tipo serializado en metadata interna.
- Al renombrar, los documentos antiguos tienen un tipo que ya no existe.
- Marten 8 puede tolerarlo si el campo `mt_doc_type` coincide con la tabla, pero requiere `Schema.For<HandHistoryRecord>().DocumentAlias("handrecord")` para puentear el rename.

**Comportamiento esperado:** documentar el patrón de rename (alias) para evolución del modelo.

**Consecuencias si se ignora:**
- Tras un rename, queries silenciosamente retornan listas vacías porque el tipo no matchea.
- Datos no perdidos (siguen en JSONB) pero invisibles al ORM.

**Cobertura test:** ninguna.

---

## EC-15 — Tamaño del documento `GameSession` excede el límite práctico

**Disparador:** Una sesión muy larga acumula 20 `HandRecord` en `GameSession.Hands` (en memoria) más telemetría agregada — el documento serializado podría superar varias decenas de KB.

**Comportamiento real** (🟢 actualmente):
- `GameSession.Hands` está marcado `[JsonIgnore]` → **no se persiste con la sesión**. La lista vive solo en memoria, los `HandRecord` están en su propia colección con FK.
- `GameSession` persistido típicamente ocupa <2 KB.

**Comportamiento esperado:** verificar que `[JsonIgnore]` siga aplicado en futuras versiones; si se elimina por error, los documentos crecerán sin control.

**Consecuencias si se ignora:**
- Documentos `GameSession` >100 KB degradan performance de queries (Postgres carga el JSONB completo).
- Sin métricas, el problema crece silenciosamente.

**Cobertura test:** ninguna específica al tamaño. **Mitigación**: tests del dominio (`OpenScrape.Domain/edge-cases.md` EC-08) ya cubren la separación lógica.

---

## Resumen — riesgos por categoría

| Categoría | Casos | Severidad |
|-----------|------:|-----------|
| Configuración / secrets | EC-01, EC-05, EC-09, EC-13 | 🔴 alta (impacto en startup y seguridad) |
| Auto-creación / migración | EC-03, EC-06, EC-10 | 🔴 crítica para producción |
| Pool / concurrencia | EC-04, EC-08, EC-12 | 🟡 media (depende de uso correcto del consumidor) |
| Serialización / esquema | EC-02, EC-07, EC-11, EC-14, EC-15 | 🟡 media (latente, sin alertas) |

> 4 casos 🔴 alimentan directamente las lacunas de `questions.md` (T-12, T-13, T-14, TM-01).
