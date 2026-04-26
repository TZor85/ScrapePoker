## ADDED Requirements

### Requirement: TextBoxLogger implementa ILogger sobre un TextBox de WinForms

El proyecto SHALL exponer `TextBoxLogger` que implementa `Microsoft.Extensions.Logging.ILogger`. Recibe en su constructor una categoría (nombre del tipo) y una referencia a un contenedor de opciones (`TextBoxLoggerOptions`) que expone el `TextBox` destino y el nivel mínimo de log. Al escribir, el logger MUST:

- Componer el mensaje siguiendo la plantilla `"[HH:mm:ss LVL Category] <msg>"` donde `LVL` es el nivel a 3 letras (`TRC|DBG|INF|WRN|ERR|CRT`) y `Category` es el último segmento del nombre completo (ej.: `FrmMain`).
- Si el `TextBox` destino no es `null` y no está `IsDisposed`, hacer `Invoke` si el hilo actual no es el hilo del `TextBox` (propiedad `InvokeRequired`). El logger MUST NO bloquear si el form se cierra durante la escritura — capturar y descartar `ObjectDisposedException` / `InvalidOperationException`.
- Respetar `TextBoxLoggerOptions.MaxLines`: si el `TextBox` supera ese número de líneas, retirar las primeras N líneas antes de escribir la nueva (buffer rodante).

#### Scenario: Mensaje Informational se escribe al TextBox con formato

- **GIVEN** un `TextBoxLogger` con categoría `"OpenScrape.App.Forms.FrmMain"` y un `TextBox` `tb` vacío
- **WHEN** se invoca `logger.LogInformation("Región actualizada: {Region}", "BoardZone")`
- **THEN** `tb.Text` contiene una línea que empieza por `"["`, contiene `"INF FrmMain"`, y termina con `"Región actualizada: BoardZone"`

#### Scenario: Mensaje bajo el filtro no se escribe

- **GIVEN** un `TextBoxLogger` con `MinimumLevel = LogLevel.Warning`
- **WHEN** se invoca `logger.LogInformation("ruido")`
- **THEN** `logger.IsEnabled(LogLevel.Information)` devuelve `false`
- **AND** el `TextBox` no se modifica

#### Scenario: TextBox disposed no lanza

- **GIVEN** un `TextBoxLogger` cuyo `TextBox` destino se ha `Dispose()`-eado
- **WHEN** se invoca `logger.LogError(new Exception("boom"), "falló {X}", "y")`
- **THEN** la llamada retorna sin lanzar
- **AND** ningún `Invoke` fallido propaga la excepción

#### Scenario: Rotación de buffer cuando se excede MaxLines

- **GIVEN** un `TextBoxLogger` con `MaxLines = 10` y un `TextBox` con 10 líneas previas
- **WHEN** se invoca `logger.LogInformation("nueva")`
- **THEN** el `TextBox` tiene exactamente 10 líneas
- **AND** la primera línea original ha desaparecido
- **AND** la última línea contiene `"nueva"`

### Requirement: Scopes se propagan al texto renderizado

`TextBoxLogger` SHALL incluir los pares clave-valor de las scopes activas en la línea renderizada. El formato es `" {Key=Value, Key2=Value2}"` justo antes del mensaje. Scopes anidadas acumulan pares; si la misma clave se define en dos scopes anidadas, gana la más interna.

El logger MUST soportar scopes creadas con `ILogger.BeginScope(IEnumerable<KeyValuePair<string, object>>)` y con `ILogger.BeginScope(string messageFormat, object[] args)`; en este último, el mensaje formateado se añade como valor de la clave `Scope`.

#### Scenario: Scope con HandNumber se incluye en el log

- **GIVEN** un `TextBoxLogger` sobre `tb`
- **WHEN** se ejecuta `using (logger.BeginScope(new Dictionary<string, object> { ["HandNumber"] = 42L })) { logger.LogInformation("flop procesado"); }`
- **THEN** la línea escrita en `tb` contiene `"HandNumber=42"` antes del texto `"flop procesado"`

#### Scenario: Scopes anidadas acumulan pares

- **GIVEN** un `TextBoxLogger` con scope externa `SessionId=abc` y scope interna `HandNumber=7`
- **WHEN** se loguea un mensaje desde dentro de la scope interna
- **THEN** la línea renderizada contiene ambos pares `SessionId=abc` y `HandNumber=7`

### Requirement: TextBoxLoggerProvider registra el destino tardíamente

La clase `TextBoxLoggerProvider` SHALL implementar `ILoggerProvider` y permitir **registrar el `TextBox` destino tras la construcción**, porque el provider se construye al arranque del host (antes de que exista `FrmMain`), pero el `TextBox` sólo está disponible después de `InitializeComponent()`.

El provider expone `SetTextBoxTarget(TextBox target)` que puede llamarse una vez tras la construcción. Hasta que no se llame, los loggers creados por el provider acumulan mensajes en un buffer interno acotado (256 líneas) o los descartan silenciosamente (configurable vía `TextBoxLoggerOptions.BufferUntilTargetReady`, default `false` = descartar).

#### Scenario: Provider sin target descarta por defecto

- **GIVEN** un `TextBoxLoggerProvider` con opciones por defecto y sin `SetTextBoxTarget` llamado
- **WHEN** se crea un logger y se emite `logger.LogInformation("temprano")`
- **THEN** no se lanza excepción
- **AND** cuando más tarde se llama `SetTextBoxTarget(tb)`, `tb.Text` NO contiene `"temprano"`

#### Scenario: Provider con buffering flushea al registrar target

- **GIVEN** un `TextBoxLoggerProvider` con `BufferUntilTargetReady = true`
- **AND** se emiten 3 mensajes antes de registrar el target
- **WHEN** se llama `SetTextBoxTarget(tb)`
- **THEN** las 3 líneas buffered aparecen en `tb.Text` en orden de emisión
- **AND** los mensajes emitidos después del flush se escriben directamente sin buffering

### Requirement: TextBoxLoggerExtensions registra el provider en el host

El proyecto SHALL exponer `AddTextBoxLogger(this ILoggingBuilder builder, Action<TextBoxLoggerOptions>? configure = null)` como método de extensión. La llamada dentro de `Host.CreateDefaultBuilder().ConfigureLogging(...)` registra el `TextBoxLoggerProvider` como singleton, de modo que `FrmMain` puede resolverlo desde DI para llamar a `SetTextBoxTarget(tbResume)` tras `InitializeComponent`.

#### Scenario: AddTextBoxLogger registra el provider singleton

- **GIVEN** un `IServiceCollection` al que se llama `AddLogging(lb => lb.AddTextBoxLogger())`
- **WHEN** se resuelve `ILoggerProvider` vía `IEnumerable<ILoggerProvider>`
- **THEN** la colección contiene exactamente una instancia de `TextBoxLoggerProvider`
- **AND** resolver `TextBoxLoggerProvider` directamente devuelve la misma instancia

### Requirement: FrmMain deja de usar LogError/LogInformation/LogDebug locales

`FrmMain.cs` MUST NOT contener métodos privados o estáticos con los nombres `LogError`, `LogInformation`, `LogDebug` que escriban a `Console.WriteLine` y/o `tbResume`. Todos los call-sites existentes SHALL migrar a `_logger.LogError(...)` / `_logger.LogInformation(...)` / `_logger.LogDebug(...)` donde `_logger` es un `ILogger<FrmMain>` inyectado por DI.

Los mensajes migrados MUST usar **structured properties** en la plantilla (por ejemplo: `_logger.LogInformation("Región actualizada: {Region}", _selectedRegion.Name)`) en vez de string interpolation con `$"..."`.

#### Scenario: No quedan métodos locales de logging en FrmMain

- **WHEN** se grepea `private.*void Log(Error|Information|Debug)|static.*void Log(Error|Information|Debug)` en `src/OpenScrape.App/Forms/FrmMain.cs`
- **THEN** cero coincidencias

#### Scenario: Ningún call-site usa interpolación para la plantilla del log

- **WHEN** se grepea `_logger\.Log(Error|Information|Debug|Warning)\(\$"` en `src/OpenScrape.App/`
- **THEN** cero coincidencias
- **AND** los mensajes siguen el patrón `_logger.LogX("mensaje {Property}", value)` con placeholders en la plantilla

### Requirement: Correlation scopes en el game loop

Cuando se inicia una mano nueva vía `GameLoggerService.StartNewHandAsync(...)`, el código SHALL envolver el procesamiento posterior en un scope `_logger.BeginScope(new Dictionary<string, object> { ["HandNumber"] = n, ["SessionId"] = id })`. De forma equivalente, `StartSessionAsync` envuelve en scope `{ ["SessionId"] = id, ["TableName"] = name }`.

Los logs emitidos durante el procesamiento de esa mano o sesión MUST aparecer en el `TextBox` con los pares `HandNumber=n` y `SessionId=id` presentes en la línea.

#### Scenario: Línea de flop tiene HandNumber en la renderización

- **GIVEN** un test de integración que arranca una sesión y una mano con `HandNumber = 123`
- **AND** emite un `_logger.LogInformation("procesando flop")` dentro del scope de mano
- **WHEN** se inspecciona el `TextBox` destino
- **THEN** la línea correspondiente contiene `HandNumber=123`
- **AND** contiene `SessionId=` con el id de la sesión activa
