# OpenScrape.Infrastructure — Preguntas Abiertas

> Lacunas detectadas en la capa de infraestructura que requieren validación humana antes de implementar/migrar.
> Modo de respuesta: `file` (responder editando este archivo, secciones marcadas con `📝 Respuesta:`).
> Cada pregunta tiene un ID estable (`Q-INF-NN`) para referencia desde `tasks.md`, `decisions.md`, `edge-cases.md`.

---

## Q-INF-01 — ¿`IsDevelopment` debe leer `IHostEnvironment` o seguir hardcoded a `true`?

**Contexto:**
- `Program.cs:52` invoca `services.AddDataBase(context.Configuration, true)` con el flag hardcoded.
- `Services.cs:36-39` activa `AutoCreate.All` solo si `IsDevelopment == true`.
- En `dotnet publish ... --configuration Release`, el código sigue creyendo que está en development → Marten intenta crear/modificar el esquema en cada arranque productivo.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Status quo (hardcoded `true`)** | Auto-creación funciona en cualquier entorno; un único dev no necesita configurar nada. | 🔴 Race conditions en deploy multi-instancia; permisos DDL en runtime; no hay protección contra `ALTER TABLE` accidental. |
| **B. Leer `context.HostingEnvironment.IsDevelopment()`** | Producción protegida, dev sin fricción. Patrón estándar de .NET. | Requiere que `DOTNET_ENVIRONMENT` esté configurado correctamente en cada entorno (launchSettings local, variable de entorno en deploy). |
| **C. Leer flag explícito de `appsettings.json`** (`Marten:AutoCreate=true`) | Independiente de `DOTNET_ENVIRONMENT`. Más auditable. | Más superficie de configuración. Una mala edición rompe la app. |

**Preguntas concretas:**
1. ¿La app está pensada para correr alguna vez en producción multi-instancia, o siempre será single-user desktop?
2. Si es single-user, ¿es aceptable mantener `AutoCreate.All` por simplicidad?
3. Si va a producción, ¿quién maneja el esquema (manual, script, Marten CLI)?

**Bloqueo:** T-12 en `tasks.md`. **Decisiones afectadas:** DD-05, DD-07. **Casos extremos relacionados:** EC-03, EC-10.

🔴 **Respuesta:** 

1. Single-user
2. Si
3. El usuario

---

## Q-INF-02 — ¿Connection string committeada en `appsettings.json` debe revertirse a `CHANGE_ME`?

**Contexto:**
- El Scout detectó que `appsettings.json` contiene credenciales reales de Neon committeadas (anomalía registrada en `surface.json`).
- `appsettings.Development.json` está gitignored (verificado con `git check-ignore`) pero contiene la misma cadena.
- El historial de Git ya expuso las credenciales — la rotación del password es necesaria independientemente de qué se haga ahora.
- CLAUDE.md menciona el patrón `CHANGE_ME` pero no se aplica al estado actual del repositorio.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Revertir a `CHANGE_ME` en `appsettings.json`** | Sin secrets en el repo. Pattern correcto. | Onboarding requiere docs claras (clonar + crear `appsettings.Development.json` + rotar password). |
| **B. Mantener committeada** | Onboarding inmediato (clone + run). | 🔴 Vulnerabilidad seria si el repo se hace público o se compromete. Los logs de Git ya tienen el password. |
| **C. Migrar a User Secrets / variables de entorno** | No depende de archivos físicos en el repo. | Requiere documentación de setup. Cambia el contrato de configuración (afecta a futuros consumidores). |

**Preguntas concretas:**
1. ¿La password de Neon ya fue rotada tras el commit con credenciales? Si no → **rotar inmediatamente**.
2. ¿El proyecto es privado/público en GitHub? (`gh repo view --json visibility`)
3. ¿Se acepta el sobrecoste de onboarding (crear `appsettings.Development.json` localmente)?

**Bloqueo:** T-14 en `tasks.md`. **Casos extremos relacionados:** EC-13.

🔴 **Respuesta:**
1. Hay que rotar
2. privado
3. Si

---

## Q-INF-03 — ¿Se introduce guard explícito ante connection string ausente?

**Contexto:**
- `Services.cs:16` usa `configuration.GetConnectionString("DefaultConnection")!` con null-forgiving.
- Si la clave no existe, `null` pasa a `options.Connection(null)` → falla en runtime con stack trace ofuscado (EC-01).
- El cambio es local y de bajo riesgo: 4-5 líneas en `Services.cs`.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Status quo (`!`)** | Cero código adicional. | NRE críptico al primer fallo de configuración. |
| **B. Guard explícito con `InvalidOperationException`** | Mensaje claro al usuario en startup. | 4 líneas adicionales. |
| **C. Health check + fallback in-memory** | UI puede arrancar sin DB y reportar "modo degradado". | Complejidad alta para un proyecto desktop. Overkill. |

**Recomendación implícita:** Opción B (mejora con costo trivial).

**Pregunta concreta:**
1. ¿Hay alguna razón específica para mantener el comportamiento actual (ej. depende de un test que asume NRE)?

**Bloqueo:** T-13 en `tasks.md`. **Casos extremos relacionados:** EC-01.

🔴 **Respuesta:** Opción B

---

## Q-INF-04 — ¿Política de migración del esquema en producción?

**Contexto:**
- Sin auto-create (T-12 Opción B), Marten **no** migrará el esquema en prod.
- No existen scripts SQL versionados en el repo (`grep -r '*.sql'` retorna vacío).
- Si se añade un campo a `HandRecord` y se despliega sin migración, las queries siguen funcionando (Postgres ignora el campo extra durante INSERT/UPDATE de JSONB) **pero** los índices declarados en `Services.cs:22-32` no se crean automáticamente.
- Marten ofrece su propio CLI: `dotnet marten patch` genera scripts diferenciales.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Migración manual con scripts SQL versionados** | Auditoría completa. Compatible con cualquier herramienta DBA. | Boilerplate para cada cambio del modelo. |
| **B. `dotnet marten patch` en CI/CD pre-deploy** | Auto-generado a partir del modelo. | Requiere Marten CLI instalado en pipeline. |
| **C. Auto-create en producción (status quo extendido)** | Cero ceremony. | Race conditions multi-instancia (EC-10). |
| **D. No producción / single-user desktop** | No aplica. | Si el proyecto evoluciona a multi-user en el futuro, retrofittear cuesta más. |

**Preguntas concretas:**
1. ¿La app va a ejecutarse alguna vez fuera del escritorio del autor?
2. ¿Hay roadmap para multi-tenant / SaaS?
3. Si va a producción, ¿qué herramienta de DevOps gestiona los deploys?

**Bloqueo:** TM-01 en `tasks.md`. **Decisiones afectadas:** DD-05. **Casos extremos relacionados:** EC-06, EC-10.

🔴 **Respuesta:**
1. No
2. Si
3. Github

---

## Q-INF-05 — ¿Health check + retry policy ante fallos transitorios de la base?

**Contexto:**
- Neon (cloud-hosted Postgres) puede tener latencia variable y desconexiones transitorias por idle timeout.
- Actualmente, una caída momentánea propaga `NpgsqlException` al consumidor; no hay retry automático.
- `IHealthCheck` no está registrado.
- Para una app desktop con sesión persistente (~1-2 hrs), un retry de 1-3 intentos con backoff cubriría >95% de los fallos transitorios.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Sin retry, sin health check (status quo)** | Cero código. | UX degradada cuando la red parpadea. |
| **B. Retry con Polly en cada `await using`** | Resilencia transparente. | Polly añade dep transitiva. Atrapado en cada consumidor o vía interceptor. |
| **C. Health check + reconexión manual al reintentar acción de usuario** | UI muestra estado degradado, usuario decide reintentar. | Más invasivo en la UI. |
| **D. Marten ya hace retry interno (verificar)** | Sin cambios. | Marten 8.24.0 NO realiza retry sobre `NpgsqlException` por defecto — confirmado por inspección de Marten source. |

**Preguntas concretas:**
1. ¿La base de datos siempre será Neon/cloud (con SSL y latencia variable) o algún despliegue futuro será local?
2. ¿Tolerable que una mano se pierda si la base parpadea durante 5 segundos?
3. ¿Hay budget para añadir `Polly` como dependencia transitiva (~50KB)?

**Bloqueo:** T-15 en `tasks.md` (mejora opcional). **Casos extremos relacionados:** EC-02, EC-08, EC-12.

🔴 **Respuesta:**
1. Puede variar, pero no local
2. Si
3. No, pero es recomendable
---

## Q-INF-06 — ¿Eliminar `Ardalis.Result` del `OpenScrape.Infrastructure.csproj`?

**Contexto:**
- El paquete está declarado pero ningún archivo del módulo lo importa o usa.
- Posiblemente se copió del csproj de `OpenScrape.Features` durante el scaffolding.
- Eliminarlo no rompe nada (verificado con `grep "Ardalis" src/OpenScrape.Infrastructure/`).
- Mantenerlo cuesta ~100KB en el bundle final (despreciable).

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar** | csproj coherente con el código. | Riesgo cero (cambio puramente declarativo). |
| **B. Conservar y empezar a usarlo** | Bootstrap retornaría `Result<IDocumentStore>` con feedback estructurado. | Over-engineering para un método llamado una vez. |
| **C. Conservar sin usar** | Status quo. | Confusión leve para alguien que lea el csproj. |

**Recomendación implícita:** Opción A (limpieza trivial, cero riesgo).

**Pregunta concreta:**
1. ¿Hay algún plan futuro de envolver el bootstrap en `Result<T>`?

**Bloqueo:** T-18 en `tasks.md`. **Decisiones afectadas:** DD-08.

🔴 **Respuesta:**
1. No

---

## Q-INF-07 — ¿Verificar y limpiar índices duplicados sobre `HandRecord`?

**Contexto:**
- `Services.cs:27-32` declara:
  - `Index(x => x.GameSessionId)` (simple)
  - `Index(x => new { x.GameSessionId, x.Timestamp })` (compuesto)
- En Postgres, el índice compuesto cubre por **left-prefix rule** las queries que filtran solo por `GameSessionId`. El índice simple es redundante salvo que existan queries con `ORDER BY` distinto del orden del compuesto.
- Inspección de consumidores actuales (`GameLoggerService.GetHandsForSessionAsync`): usa `Where(GameSessionId).OrderBy(Timestamp)` → cubierto por el compuesto.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar el simple `(GameSessionId)`** | Inserts ~10% más rápidos (un índice menos que mantener). | Si surge una query con `ORDER BY GameSessionId DESC` o sin `Timestamp`, latencia degrada. |
| **B. Conservar ambos** | Cubre cualquier query futura por `GameSessionId`. | Costo extra de mantenimiento del índice. |
| **C. Reemplazar por `(GameSessionId, HeroPosition)` compuesto** | Cubre análisis estadístico por posición + sesión. | Hay que medir si ese análisis es frecuente. |

**Pregunta concreta:**
1. ¿Hay alguna query proyectada sobre `HandRecord.GameSessionId` que **no** ordene por `Timestamp`?

**Bloqueo:** ninguno (decisión cosmética). **Decisiones afectadas:** DD-04.

🔴 **Respuesta:** No que yo recuerde

---

## Q-INF-08 — ¿Vale la pena trazar las queries de Marten con OpenTelemetry?

**Contexto:**
- `OpenTelemetry.Api` ya está cargado transitivamente (con la vulnerabilidad NU1902 silenciada).
- Marten emite traces `Marten.Operation` automáticamente si hay un tracer registrado.
- Sin tracer, los traces se descartan — el overhead es trivial.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Status quo (sin tracer)** | Cero overhead. | Sin observabilidad de DB. |
| **B. Tracer in-memory para diagnóstico ad-hoc** | Útil para debugging cuando el Historial responde lento. | Pequeño overhead. |
| **C. Tracer + exporter (Jaeger/Honeycomb)** | Métricas completas. | Sobre-ingeniería para desktop app single-user. |

**Pregunta concreta:**
1. ¿Hay diagnósticos de performance pendientes que se beneficiarían de traces de Marten?

**Bloqueo:** ninguno (mejora opcional). **Casos extremos relacionados:** EC-08.

🔴 **Respuesta:** No

---

## Q-INF-09 — ¿Documentar formato de connection string para distintos entornos?

**Contexto:**
- Neon (cloud) requiere `SslMode=Require;Trust Server Certificate=true`.
- Postgres local típicamente sin SSL (`SslMode=Disable`).
- El módulo es agnóstico a la cadena, pero el onboarding de un dev nuevo se rompe sin la cadena correcta.

**Implicaciones:**
- Sin documentación: dev nuevo pierde 30+ min adivinando.
- Con documentación: README o `appsettings.json.example` aclara cada entorno.

**Pregunta concreta:**
1. ¿Aceptable añadir un `appsettings.json.example` versionado con ejemplos por entorno?

**Bloqueo:** T-14 (relacionado). **Casos extremos relacionados:** EC-09, EC-13.

🔴 **Respuesta:** No

---

## Q-INF-10 — ¿Validar idempotencia de `AddDataBase` en doble registro?

**Contexto:**
- EC-04 documenta que el comportamiento de Marten 8.24.0 ante doble registro es **no verificado**.
- Tests actuales no cubren ese escenario.
- `AddDataBase` invocado dos veces en test setup compartido podría producir resultados inestables.

**Implicaciones:**
- Si Marten lo soporta naturalmente: añadir test (TT-07) y documentar.
- Si Marten falla: añadir guard `if (services.Any(s => s.ServiceType == typeof(IDocumentStore))) return;` en `AddDataBase`.

**Pregunta concreta:**
1. ¿Hay tests reales que vayan a invocar `AddDataBase` múltiples veces sobre el mismo `IServiceCollection`?

**Bloqueo:** TT-07 en `tasks.md`. **Casos extremos relacionados:** EC-04.

🔴 **Respuesta:** No

---

## Resumen — orden recomendado de respuesta

| Prioridad | ID | Tema | Impacto |
|-----------|-----|------|---------|
| 🔴 P0 | Q-INF-02 | Secrets committeados | Seguridad activa — rotar password ya. |
| 🔴 P1 | Q-INF-01 | `IsDevelopment` hardcoded | Riesgo en deploy productivo. |
| 🔴 P1 | Q-INF-04 | Política migración prod | Bloquea release real. |
| 🟡 P2 | Q-INF-03 | Guard connection string nula | UX/diagnóstico. |
| 🟡 P3 | Q-INF-05 | Health check + retry | Resilencia transitoria. |
| 🟢 P4 | Q-INF-06 | `Ardalis.Result` huérfano | Limpieza. |
| 🟢 P4 | Q-INF-07 | Índice simple redundante | Optimización marginal. |
| 🟢 P4 | Q-INF-08 | Tracing Marten | Mejora opcional. |
| 🟢 P5 | Q-INF-09 | Doc connection string | Onboarding. |
| 🟢 P5 | Q-INF-10 | Test idempotencia | Robustez de tests. |

> **Acción recomendada antes del Revisor:** responder al menos Q-INF-01, Q-INF-02 y Q-INF-04 (las tres 🔴 críticas). Las restantes pueden quedar marcadas como 🟡 para iteración futura.
