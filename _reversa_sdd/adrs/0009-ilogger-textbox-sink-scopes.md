# ADR-0009 — `Microsoft.Extensions.Logging` con sink TextBox y correlation scopes

- **Estado:** 🟢 ACEPTADO (vigente). Reemplaza método artesanal `LogError`/`LogInformation`/`LogDebug` en `FrmMain`.
- **Fecha:** 2026-04-21 (commit `b5a5dfa refactor(logging): migrate to ILogger with TextBox sink and scopes`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude Sonnet 4.6

## Contexto

Antes del refactor, `FrmMain` tenía métodos privados `LogError`, `LogInformation`, `LogDebug` que escribían a la vez a `Console.WriteLine` y al `tbResume` TextBox usando string interpolation `$"..."`. Otros servicios (`ScreenReaderService`, `TableLayoutService`) tenían sus propios helpers locales o llamaban a `Console.WriteLine` directamente.

Problemas:

- 65 call-sites con interpolación → coste de format incluso si el log estaba filtrado.
- Sin nivel de filtrado: `Debug` siempre escribía a Console aunque el usuario no quisiera ver.
- Sin **correlation**: imposible saber a qué `SessionId` o `HandNumber` pertenecía un mensaje.
- Sin **categoría/template estructurado**: imposible filtrar por componente o exportar a Seq/ELK.
- Cross-thread: `tbResume.AppendText` desde el game loop background → `InvalidOperationException` si no se sincroniza.

## Decisión

Migrar **todo el logging** a `Microsoft.Extensions.Logging`:

1. **Pipeline estándar** vía `ConfigureLogging(lb => lb.AddTextBoxLogger())` en `Program.cs`. Niveles configurables en `appsettings.json:Logging`.
2. **Sink custom `TextBoxLogger` + `TextBoxLoggerProvider`**:
   - Renderiza al `tbResume` con formato `[HH:mm:ss LVL Cat] {scope=...} message`.
   - Late target registration: `SetTextBoxTarget(tbResume)` se llama en `FrmMain` tras `InitializeComponent`. Antes de eso, los logs se buffearían (con rotación).
   - Cross-thread invoke seguro vía `ISynchronizeInvoke.Invoke`.
   - Configurable: nivel mínimo, formato, capacidad de buffer.
3. **Correlation scopes** en `GameLoggerService`:
   - `StartSessionAsync` abre scope `{SessionId, TableName}`.
   - `StartNewHandAsync` abre scope `{HandNumber}`.
   - Disposed al cambiar sesión / finalizar mano.
4. **Migración de 65 call-sites** en `FrmMain` + 2 en `ScreenReaderService` + 15 en `TableLayoutService` + 3 en `Console.WriteLine` residuales.
5. **Eliminación** de `LogError/LogInformation/LogDebug` privados y `AppendLog` de `FrmMain`.
6. Ningún `Console.WriteLine` en `src/OpenScrape.App/` tras el refactor (hooks: el commit message lo verifica).

## Alternativas consideradas

1. **Serilog directamente.** Excelente librería pero añade dependencia que no se necesita: el sink TextBox y los scopes son trivialmente implementables sobre `ILogger`. Si en el futuro se quiere Serilog, se añade un provider sin tocar call-sites.
2. **Log4Net / NLog.** Mismas razones. Además son ecosistemas más antiguos y menos integrados con `Microsoft.Extensions.Hosting`.
3. **Mantener helpers artesanales pero estructurarlos.** Rechazado: sigue sin levels, sin scopes, sin pipeline estándar. Reinventa la rueda.
4. **Solo Console + parsing externo.** Rechazado: el `tbResume` es parte de la UX (pestaña Logs). Se necesita renderizar en proceso.
5. **Logging asíncrono con ChannelWriter.** Buffer interno pero síncrono al render del TextBox sería suficiente. Considerado para un futuro si la cadencia se incrementa, pero el commit `495cf7e refactor/core: BitHandEvaluator, LRU cache, async logging` ya introdujo async donde dolía.

## Consecuencias

**Positivas:**

- **Logs estructurados:** template `_logger.LogInformation("Hero stack: {Stack} BB", heroStackBB)` permite filtrado y exportación.
- **Correlation visible:** un log de motor puede ir acompañado de `{SessionId=abc, TableName=Pala1, HandNumber=42}` automáticamente.
- **Levels:** el usuario ajusta `Microsoft` vs `OpenScrape.App` independientemente.
- **9 tests** cubren TextBoxLogger (levels, scopes, buffering, rotation, DI). Suite 1147/1147 verde (+9 tests vs 1138 previas).
- **Cross-thread safe** con `ISynchronizeInvoke`.
- Permite enviar logs adicionales a archivo/Seq sin tocar código (solo añadir provider).

**Negativas:**

- **Performance:** `LogInformation("X: {Y}", value)` evalúa el template solo si pasa el filtro, pero existen ~65 call-sites — cualquier provider lento (file con flush) impactará. Mitigado: TextBox es in-process.
- **TextBox tiene rotación con capacidad finita.** Si el usuario juega 10K manos y todos los logs están al máximo nivel, la pestaña Logs trunca los más antiguos. **Decisión deliberada** para evitar OOM.
- Migrar 65 call-sites con regex/AST fue costoso. Si el patrón cambia (templates con propiedades complejas), retrabajo.

**Implicaciones para una migración:**

- En cualquier stack moderna existe equivalente: SLF4J + scopes (Java/Kotlin), structlog (Python), tracing (Rust), pino (Node).
- El **contrato del scope** es lo importante: `{SessionId, TableName}` y `{HandNumber}` deben mantenerse para correlación analítica.

## Referencias

- Commit `b5a5dfa refactor(logging): migrate to ILogger with TextBox sink and scopes`.
- `src/OpenScrape.App/Services/Logging/TextBoxLogger.cs` + `Provider.cs` + `Options.cs` + `Extensions.cs`.
- `openspec/specs/textbox-logger-sink/spec.md`.
- `src/OpenScrape.App/Services/GameLoggerService.cs` — uso de scopes.
