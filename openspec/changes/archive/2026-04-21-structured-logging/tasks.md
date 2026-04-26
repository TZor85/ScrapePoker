## 1. Auditoría

- [x] 1.1 Inventario final: FrmMain 65 call-sites (38 Error + 21 Info + 6 Debug); ScreenReader 9 refs; TableLayout 16 refs. Total ~90 migraciones + 3 `Console.WriteLine` adicionales descubiertos durante la migración.
- [x] 1.2 Propiedades estructurables identificadas: `{Region}`, `{Umbral}/{Inact}`, `{Pot}`, `{HandNumber}`, `{SessionId}`, `{Raw}/{Corrected}`, `{Width}/{Height}`, `{Position}`, `{Attempt}`, `{Previous}/{Current}`. Excepciones migradas a firma `_logger.LogError(ex, "...")`.

## 2. Infraestructura del sink

- [x] 2.1 `TextBoxLoggerOptions.cs` creado con MinimumLevel, MaxLines, BufferUntilTargetReady, BufferCapacity.
- [x] 2.2 `TextBoxLogger.cs` implementa `ILogger`, filtra por `MinimumLevel`, delega scopes al provider.
- [x] 2.3 Renderizado con plantilla `[HH:mm:ss LVL Cat] {scope=...} msg | Exception: ...`. Cross-thread con `BeginInvoke`. Captura `ObjectDisposedException`/`InvalidOperationException`.
- [x] 2.4 Buffer rodante: si `tb.Lines.Length >= MaxLines`, mantiene `MaxLines-1` anteriores y añade la nueva.
- [x] 2.5 `TextBoxLoggerProvider.cs` implementa `ILoggerProvider + ISupportExternalScope`, cache de loggers por categoría, `SetTextBoxTarget` única.
- [x] 2.6 `ConcurrentQueue` con recorte a `BufferCapacity`. Flush al registrar target si `BufferUntilTargetReady`.
- [x] 2.7 `TextBoxLoggerExtensions.cs` con `AddTextBoxLogger` que registra el provider singleton + `RegisterProviderOptions` para binding.

## 3. Tests del sink

- [x] 3.1 Creado `TextBoxLoggerTests.cs` con 9 tests cubriendo niveles, scopes, buffering, rotación, thread-safety y DI.
- [x] 3.2 `dotnet test --filter "FullyQualifiedName~TextBoxLogger"` → **9/9 verde**.

## 4. Registro en Program.cs

- [x] 4.1 `ConfigureLogging(lb => lb.AddTextBoxLogger())` añadido al host builder.
- [x] 4.2 Sección `"Logging": { "TextBox": { ... } }` añadida a `appsettings.json` con MinimumLevel Information, MaxLines 5000, BufferUntilTargetReady false.
- [x] 4.3 Binding automático vía `RegisterProviderOptions<TextBoxLoggerOptions, TextBoxLoggerProvider>` y alias `[ProviderAlias("TextBox")]`.
- [x] 4.4 Build verde.

## 5. Bootstrapping en FrmMain

- [x] 5.1 Constructor inyecta `ILogger<FrmMain> logger` + `TextBoxLoggerProvider textBoxLoggerProvider`.
- [x] 5.2 Tras `InitializeComponent()`: `_textBoxLoggerProvider.SetTextBoxTarget(tbResume)`.
- [x] 5.3 Build verde.

## 6. Migración de call-sites en FrmMain

- [x] 6.1 65 call-sites migrados a `_logger.LogError/LogInformation/LogDebug` con structured properties.
- [x] 6.2 Todas las interpolaciones `$"..."` convertidas a templates con placeholders.
- [x] 6.3 Excepciones migradas a `_logger.LogError(ex, "...")` — mensaje y stack trace incluidos por framework.
- [x] 6.4 Eliminados los 3 métodos privados (`LogError`, `LogInformation`, `LogDebug`) y el helper `AppendLog`.
- [x] 6.5 Grep `private.*void Log(Error|Information|Debug)|static.*void Log(Error|Debug)` → cero en `src/OpenScrape.App/`.
- [x] 6.6 Build verde + suite 1147/1147 verde.

## 7. Migración ScreenReaderService + TableLayoutService

- [x] 7.1 `ScreenReaderService`: inyecta `ILogger<ScreenReaderService>`. 2 `Console.WriteLine` inline + 6 call-sites del helper local migrados; helper eliminado.
- [x] 7.2 `TableLayoutService`: inyecta `ILogger<TableLayoutService>`. 15 call-sites del helper local migrados (algunos reclasificados: `LogError` para exceptions, `LogWarning` para advertencias); helper eliminado.
- [x] 7.3 Además: 3 `Console.WriteLine` adicionales en `FrmMain` (líneas 2164, 4132, 4177) que no estaban en el audit inicial también migrados.
- [x] 7.4 Grep `Console\.WriteLine` en `src/OpenScrape.App/` → **cero**.

## 8. Scopes de correlation

- [x] 8.1 `GameLoggerService.StartSessionAsync` abre scope `{SessionId, TableName}` almacenado en `_sessionScope`. Se dispone al iniciar otra sesión.
- [x] 8.2 `StartNewHandAsync` abre scope `{HandNumber}` anidado, disposed en `FinalizeAndPersistHandAsync`.
- [x] 8.3 Los logs emitidos por `_logger` dentro de estos scopes heredan los IDs automáticamente. Test de integración manual queda pendiente del usuario.

## 9. Verificación final

- [x] 9.1 `dotnet build OpenScrape.sln` → 0 errores.
- [x] 9.2 `dotnet build OpenScrape.sln --configuration Release` → 0 errores.
- [x] 9.3 `dotnet test OpenScrape.sln` → **1147/1147 verde** (+9 tests nuevos de TextBoxLogger vs 1138 anterior).
- [x] 9.4 `dotnet format --verify-no-changes OpenScrape.sln` → exit 0.
- [ ] 9.5 *(Manual, pendiente del usuario)* Arranque manual: pestaña Logs muestra `[HH:mm:ss INF FrmMain] ...`, al iniciar mano aparecen `HandNumber=n, SessionId=...` en logs siguientes, cerrar app no lanza.
- [x] 9.6 `openspec validate structured-logging` → "is valid".
- [ ] 9.7 *(Pendiente cuando el usuario pida el commit)* Commit conventional: `refactor(logging): migrate FrmMain and services to ILogger with TextBox sink and scopes`.
