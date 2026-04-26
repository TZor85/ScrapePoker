## Context

El logging convive en dos regímenes. La generación nueva (`GameLoggerService`, `GameLoopCoordinator`, `GameLoopStateMachine`, `UiSyncService`, `UnifiedPokerCalculator`) usa `ILogger<T>` de `Microsoft.Extensions.Logging` — correctamente, con placeholders estructurados (`{SessionId}`, `{HandNumber}`). La generación antigua vive en `FrmMain.cs` con tres métodos privados que componen strings a mano y escriben a `Console.WriteLine` y al `TextBox tbResume` del form. 65 call-sites usan esos métodos, principalmente con `$"..."` (interpolación) — lo que anula cualquier posibilidad de structured filtering río abajo. Hay además 4 `Console.WriteLine` bare metal en `ScreenReaderService` y `TableLayoutService`.

Efectos prácticos: (a) el panel de logs de la UI (`tbResume`) contiene strings opacos sin nivel ni categoría; (b) los logs de la UI no pasan por el pipeline de `Host.CreateDefaultBuilder` — si el usuario configura un sink de fichero más adelante, las 65 líneas se le escapan; (c) si el hilo de captura imprime mid-hand, no hay cómo distinguir a qué mano corresponde.

Stakeholders: el desarrollador único. El usuario final mira el `tbResume` ocasionalmente para diagnosticar capturas fallidas; cualquier cambio visible tiene que **no empeorar** esa experiencia.

## Goals / Non-Goals

**Goals:**

- Eliminar los tres métodos privados de `FrmMain` y sus 65 call-sites; migrar a `ILogger<FrmMain>` con structured properties.
- Eliminar `Console.WriteLine` de `ScreenReaderService` y `TableLayoutService`; usar `ILogger<T>`.
- Añadir un sink custom (`TextBoxLogger`) que conecta el pipeline estándar de `ILoggingBuilder` con `tbResume`, respetando thread-safety (`Invoke`) y filtros de nivel.
- Incorporar scopes para correlation (`HandNumber`, `SessionId`, `TableName`) que aparezcan en cada línea renderizada.
- Preservar la experiencia de usuario: `tbResume` sigue mostrando logs legibles con timestamp.

**Non-Goals:**

- Introducir Serilog. `Microsoft.Extensions.Logging` cubre todos los requisitos (structured, scopes, filtros, múltiples sinks). Serilog se puede añadir más tarde como `ILoggerProvider` adicional sin tocar los call-sites.
- Persistencia de logs en fichero. Si se desea, se añade `AddFile(...)` o Serilog en un change posterior; este trabajo no lo excluye arquitectónicamente.
- Cambiar la pestaña "Logs" de la UI. Sigue siendo un `TextBox` con scroll — sólo el contenido cambia (plantilla con nivel + categoría + scope).
- Migrar los servicios que ya usan `ILogger<T>` correctamente. No se tocan.

## Decisions

### 1. MEL-puro (no Serilog)

**Decisión:** usar `Microsoft.Extensions.Logging` con un `ILoggerProvider` custom (`TextBoxLoggerProvider`). No añadir paquetes Serilog.

**Alternativas consideradas:**

- *Serilog con `UseSerilog()` + sink WinForms*: ofrece sinks listos para fichero rotativo, JSON, seq, Graylog... pero añade 3-5 paquetes NuGet y un segundo vocabulario (`Log.Information(...)` vs `_logger.LogInformation(...)`). El 90% del valor viene del structured logging, que MEL ya ofrece.
- *Mantener el `LogError/LogInformation` local y añadirles structured properties*: no se integra con el pipeline del host; los servicios nuevos seguirían en otro mundo.

**Rationale:** MEL es suficiente, ya es transitivo, y mantiene un vocabulario único. Si se necesita fichero rotativo luego, añadir `Serilog` como provider tarda 10 minutos y no toca call-sites.

### 2. Registro tardío del `TextBox` destino

**Decisión:** el `TextBoxLoggerProvider` se construye al arranque del host (antes de que exista `FrmMain`) pero expone `SetTextBoxTarget(TextBox)` que `FrmMain` llama en el constructor, tras `InitializeComponent()`. Hasta ese momento, los logs emitidos por los loggers creados por el provider se **descartan** silenciosamente (default) o se **bufferizan** en un circular buffer de 256 entradas (configurable).

**Alternativas consideradas:**

- *Lazy factory que retrasa la construcción del provider hasta que el form exista*: complica el registro DI y rompe el invariante "el host tiene el pipeline listo al startup".
- *Pasar el `TextBox` al provider vía `IOptionsMonitor` con actualización viva*: sobra — el target se asigna una sola vez.

**Rationale:** descartar por defecto es lo que hace hoy el sistema (antes de que `FrmMain` cargue, cualquier log se pierde). El buffer opcional es útil para diagnosticar fallos de arranque del propio form.

### 3. Plantilla de formato `[HH:mm:ss LVL Category] mensaje`

**Decisión:** cada línea empieza con `[HH:mm:ss INF FrmMain] ` (3 letras para el nivel, categoría simplificada a su último segmento). Si hay scopes activas, se insertan entre corchetes tras la cabecera: `[HH:mm:ss INF FrmMain] {HandNumber=42, SessionId=abc} procesando flop`.

**Alternativas consideradas:**

- *JSON en cada línea*: ilegible en un `TextBox`; fuerza al usuario a procesar fuera.
- *Una columna por campo (timestamp / level / hand / mensaje) con tabulación*: el `TextBox` no es un `DataGridView`; la alineación con proportional fonts no funciona.

**Rationale:** El formato es el mismo que usa el provider `ConsoleLogger` de MEL para su output, adaptado al TextBox. Es el balance entre legibilidad humana (scroll rápido) y parsabilidad (grep sobre exportación).

### 4. Correlation IDs vía scopes (`BeginScope`)

**Decisión:** los puntos de entrada de sesión/mano abren scopes con los IDs relevantes. Cualquier log emitido dentro del scope los hereda automáticamente. El `TextBoxLogger` los incorpora en la renderización.

- `GameLoggerService.StartSessionAsync` → scope con `SessionId`, `TableName`.
- `GameLoggerService.StartNewHandAsync` → scope con `HandNumber` (anida sobre SessionId).
- `FrmMain` — el bucle de captura de una iteración abre un scope `IterationId` si queremos; por ahora queda fuera de alcance.

**Alternativas consideradas:**

- *Pasar `HandNumber` como parámetro a cada `LogInformation`*: ruido en todos los call-sites, olvidable.
- *`AsyncLocal<Scope>` manual fuera de MEL*: duplica infraestructura ya provista.

**Rationale:** scopes son el mecanismo nativo de MEL para correlation IDs. Zero overhead cuando no hay providers interesados; los providers que sí consumen scopes (nuestro `TextBoxLogger`) los leen en el sitio de render.

### 5. Migración mecánica call-site a call-site

**Decisión:** los 65 call-sites de `FrmMain` se migran por búsqueda-y-reemplazo guiada. Regla:

- `LogError($"msg {x}")` → `_logger.LogError("msg {X}", x)`
- `LogError($"msg {ex.Message}", ex)` → `_logger.LogError(ex, "msg")` (el mensaje de la excepción lo añade el framework).
- `LogInformation($"..."`) → `_logger.LogInformation(...)` con placeholders.
- `LogDebug(...)` → `_logger.LogDebug(...)`.

**Alternativas consideradas:**

- *Mantener los métodos como wrappers de `_logger`*: preserva zero cambios en call-sites pero las plantillas quedan como strings interpoladas — no ganamos structured logging.
- *Análisis Roslyn auto-migrador*: los 65 sitios son manejables con revisión manual.

**Rationale:** el único valor real del cambio está en convertir las interpolaciones en templates con placeholders. Hacerlo a mano garantiza nombres de propiedades sensatos (`{Region}`, `{Umbral}`, `{ExceptionMessage}`).

## Risks / Trade-offs

- **Riesgo:** alguna migración cambia accidentalmente el mensaje que ve el usuario en `tbResume`, rompiendo una expectativa visual. → **Mitigación:** plantilla fija `[HH:mm:ss LVL Category] mensaje` preserva el mismo shape; el scope añade información sin eliminar la existente. Spot-check manual al arrancar la app tras la migración.
- **Riesgo:** si `FrmMain` se cierra mientras el hilo de captura emite un log, el `Invoke` al `TextBox` disposed puede lanzar. → **Mitigación:** el `TextBoxLogger` captura `ObjectDisposedException` / `InvalidOperationException` y descarta silenciosamente. Test dedicado.
- **Trade-off:** usar scopes requiere disciplina en los puntos de entrada (sesión/mano). Si se olvida un `BeginScope`, los logs pierden correlation. Aceptable porque los puntos de entrada son 2-3 y están bien identificados.
- **Trade-off:** la plantilla fija puede no gustar para logs largos. Aceptable; el usuario puede copiar-y-pegar el texto y procesar externamente, o filtrar por nivel vía `appsettings.json`.
- **Riesgo:** pérdida de logs emitidos durante el arranque del host antes de que `FrmMain` registre el target. → **Mitigación:** `BufferUntilTargetReady = true` (opcional) guarda hasta 256 líneas; por defecto se descartan (como hoy). Decisión consciente: preferimos simplicidad al arranque.

## Migration Plan

1. **Paso 1 — Infraestructura del sink (no call-sites)**: crear `TextBoxLoggerOptions`, `TextBoxLogger`, `TextBoxLoggerProvider`, `TextBoxLoggerExtensions`. Registrar en `Program.cs` vía `ConfigureLogging(lb => lb.AddTextBoxLogger())`. Tests unitarios. La app compila y arranca; el provider existe pero aún no tiene target ni consumidores.
2. **Paso 2 — `FrmMain` bootstrapping**: inyectar `ILogger<FrmMain>` y `TextBoxLoggerProvider`. Tras `InitializeComponent()` llamar `_textBoxLoggerProvider.SetTextBoxTarget(tbResume)`. El `_logger` ya funciona pero nadie lo usa todavía.
3. **Paso 3 — Migración de call-sites `FrmMain`**: 65 migraciones. Se pueden agrupar por región del fichero si se prefieren varios commits pequeños. Al terminar, eliminar los 3 métodos privados `LogError/LogInformation/LogDebug`.
4. **Paso 4 — Migración `ScreenReaderService` + `TableLayoutService`**: inyectar `ILogger<T>` (ambos ya son servicios DI), reemplazar 4 `Console.WriteLine` por `_logger.LogX`.
5. **Paso 5 — Scopes de correlation**: envolver los puntos de entrada en `GameLoggerService` (`StartSessionAsync`, `StartNewHandAsync`) con `BeginScope`. El resto del código dentro de esos flujos hereda los IDs.
6. **Paso 6 — Config**: añadir sección `"Logging": { "TextBoxSink": { ... } }` al `appsettings.json` (defaults conservadores). Revisar que `"Logging": { "LogLevel": { ... } }` está acorde con lo que queremos ver en producción.
7. **Paso 7 — Verificación**: build + suite completa + arranque manual. Comprobar que `tbResume` muestra las mismas líneas que antes (con niveles + scope), que `Console` las duplica, y que cerrar el form no provoca excepciones.

**Rollback:** cada paso es un commit; revertir cualquiera restaura el régimen previo. La infraestructura del paso 1 se puede dejar viva aunque se reviertan los pasos 3-4; no tiene efectos secundarios.

## Open Questions

- ¿El usuario quiere mantener un sink de fichero rotativo para diagnóstico post-mortem? **Fuera de scope** para este change. Si sí, abrir uno nuevo con Serilog.File sink.
- ¿Hay que aplicar rate-limiting al `TextBoxLogger` para no saturar la UI si un bug emite miles de líneas? Respuesta provisional: **no** — `MaxLines` con buffer rodante limita el uso de memoria; si la UI se pone lenta, se revisa entonces.
- ¿Scopes de iteración del game loop (`IterationId`)? Queda como mejora opcional para después — no es parte del scope mínimo.
