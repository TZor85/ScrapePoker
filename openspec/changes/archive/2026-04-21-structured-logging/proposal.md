## Why

El logging del proyecto está partido en dos mundos incompatibles. **La mitad "correcta"** ya usa `Microsoft.Extensions.Logging` (`ILogger<T>`) en `GameLoggerService`, `GameLoopCoordinator`, `GameLoopStateMachine`, `UiSyncService` y `UnifiedPokerCalculator` — con mensajes estructurados (`_logger.LogInformation("Sesión iniciada: {SessionId} en {TableName}", sessionId, tableName)`). **La otra mitad "artesanal"** vive en `FrmMain` con tres métodos privados (`LogError`/`LogInformation`/`LogDebug` en líneas 3678/3715/3732) y se usa en **65 sitios**, todos con string interpolation (`$"Error al cargar regiones: {ex.Message}"`). Esos métodos escriben a la vez a `Console.WriteLine` y a `tbResume` (un `TextBox` de la pestaña Logs), con un timestamp `[HH:mm:ss]` pegado a mano.

Además hay 4 `Console.WriteLine` directos sin ningún wrapper en `ScreenReaderService` y `TableLayoutService`.

Problemas concretos del mundo "artesanal":

1. **Sin structured properties**. `LogInformation($"Región actualizada: {_selectedRegion.Name}")` es una cadena opaca — no se puede filtrar por región, agregar por tipo de error, o consumir desde un sink externo. La pestaña de logs del usuario es un string soup.
2. **Sin log level real**. El nombre del método (`LogError` vs `LogInformation`) es la única señal; no hay enum `LogLevel`, ni filtros por nivel, ni posibilidad de silenciar debug en producción.
3. **Sin correlation ID**. Una traza de una mano entera se mezcla con las de otras manos y sesiones. No hay `HandNumber` o `SessionId` asociado a cada línea.
4. **Sink `tbResume` acoplado**. Cualquier servicio que quiera escribir al panel de logs debe conocer `FrmMain` — o duplicar el patrón. Hoy sólo lo hace `FrmMain` consigo mismo.
5. **Inconsistencia con el resto del proyecto**. Los servicios nuevos (post-refactor) ya inyectan `ILogger<T>`. `FrmMain` sigue con su sistema paralelo — cada línea escrita con `LogError(...)` no pasa por los providers registrados del host.

## What Changes

- Añadir un `ILoggerProvider` custom `TextBoxLoggerProvider` + `TextBoxLogger` que enruta mensajes al `tbResume` de `FrmMain` vía `Invoke` cuando cruza hilos. Se registra como provider del host, con filtro de nivel mínimo configurable (`Information` por defecto).
- Configurar el `Host.CreateDefaultBuilder` en `Program.cs` con `ConfigureLogging(builder => { builder.AddTextBoxSink(); builder.AddFilter(...); })` y un plantilla de formato común con timestamp, nivel y categoría (`[HH:mm:ss INF FrmMain] mensaje`).
- Inyectar `ILogger<FrmMain>` en el constructor de `FrmMain`. Eliminar los 3 métodos privados `LogError`/`LogInformation`/`LogDebug`; migrar los 65 call-sites a `_logger.LogError/LogInformation/LogDebug` con **structured properties** (`{Region}` en vez de `{ex.Message}` inline).
- Migrar los 4 `Console.WriteLine` de `ScreenReaderService` y `TableLayoutService` a `ILogger<T>`. Ambos servicios ya lo pueden inyectar.
- Introducir correlation IDs vía scopes: cuando arranca una mano, `_logger.BeginScope(new Dictionary<string, object> { ["HandNumber"] = n, ["SessionId"] = id })` envuelve todo el bloque de logs de esa mano. `TextBoxLogger` y el `ConsoleLogger` por defecto leen las scopes y las muestran en cada línea.
- **BREAKING (interno):** los métodos `LogError`/`LogInformation`/`LogDebug` privados de `FrmMain` se eliminan. Cualquier referencia futura debe pasar por `_logger`.
- No se introduce Serilog en este change — `Microsoft.Extensions.Logging` más el sink custom cubre los requisitos (timestamp, level, structured, correlation, sink múltiple). Si en el futuro se quiere sink de fichero rotativo o formato JSON, se añade Serilog en un change específico con impacto acotado.

Tests: añadir unit tests del `TextBoxLogger` (niveles, scopes, thread-safety del `Invoke`). Los 65 sitios migrados no cambian semántica observable — la suite existente debe seguir verde.

## Capabilities

### New Capabilities

- `textbox-logger-sink`: Sink de `ILogger` que escribe a un `TextBox` de WinForms respetando cross-thread, filtros de nivel y scopes como correlation IDs.

### Modified Capabilities

Ninguna — no hay specs existentes de logging. Este change introduce la capability desde cero.

## Impact

- **Código afectado**:
  - `src/OpenScrape.App/Services/Logging/TextBoxLogger.cs` (nuevo)
  - `src/OpenScrape.App/Services/Logging/TextBoxLoggerProvider.cs` (nuevo)
  - `src/OpenScrape.App/Services/Logging/TextBoxLoggerOptions.cs` (nuevo, configurable desde `appsettings.json`)
  - `src/OpenScrape.App/Services/Logging/TextBoxLoggerExtensions.cs` (nuevo, `AddTextBoxLogger(...)` para `ILoggingBuilder`)
  - `src/OpenScrape.App/Program.cs` — `ConfigureLogging` con el nuevo sink + filtros.
  - `src/OpenScrape.App/Forms/FrmMain.cs` — eliminar 3 métodos locales y migrar 65 call-sites; inyectar `ILogger<FrmMain>`; registrar el `TextBox` destino en el provider tras InitializeComponent.
  - `src/OpenScrape.App/Services/ScreenReaderService.cs` — reemplazar 3 `Console.WriteLine` por `_logger`.
  - `src/OpenScrape.App/Services/TableLayoutService.cs` — reemplazar 1 `Console.WriteLine` por `_logger`.
- **Correlation scopes**: `StartNewHandAsync` y `StartSessionAsync` abren scopes en `GameLoggerService`; `FrmMain` abre scope por cada iteración del game loop.
- **Tests**: nuevo `OpenScrape.App.Tests/TextBoxLoggerTests.cs` (~8-10 tests). Suite actual ~1138 debe seguir verde.
- **Configuración**: sección `"Logging"` de `appsettings.json` soporta filtros estándar (`"Logging": { "LogLevel": { "Default": "Information", "OpenScrape": "Debug" } }`) + subsección nueva `"TextBoxSink": { "MinimumLevel": "Information", "MaxLines": 5000 }`.
- **Dependencias externas**: ninguna nueva (MEL ya está transitivamente en el host).
- **Runtime**: overhead despreciable. Structured logging con MEL evita asignar strings innecesarias cuando el filtro de nivel descarta el evento.
