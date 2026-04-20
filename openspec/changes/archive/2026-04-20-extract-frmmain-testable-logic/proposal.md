## Why

`FrmMain.cs` (4,477 LOC) no tiene **ninguna cobertura de tests directa**. La suite de 852 tests verifica servicios abajo (DecisionMaker, helpers, coordinator), pero el form mismo —que concentra el game loop, 30+ event handlers y la coordinación entre servicios— no tiene regresión automatizada. Cualquier cambio en `FrmMain` (refactor, nuevo botón, fix de OCR) sólo se valida con ejecución manual en una sesión real de casino.

La mayor parte de `FrmMain` es UI-acoplada legítimamente (event handlers, control creation), pero hay **islas de lógica pura** que están ahí por proximidad histórica y son perfectamente testables una vez extraídas:

1. **`EnrichActionWithBBAmount(string action)`** — parser + cálculo que convierte `"3Bet x6"` en `"3Bet x6 (15BB)"` usando el big blind y la mayor apuesta del villano. No toca controles, sólo lee `_playerGameState.Players` y `_gameLoggerService.CurrentBigBlind`.
2. **`GetPlayerNumber(string regionName, string extraText)`** — ya `static`. Regex que extrae el nº de jugador de nombres de región tipo `"p3bet"` → `3`.
3. **`CalculateOverlayPosition(User32.RECT windowRect, int overlayWidth)`** — geometría pura que centra el overlay sobre la ventana del casino, con offset configurable.

Extraer estos tres helpers no cambia comportamiento pero rompe la regla "no hay tests de FrmMain" y establece patrón: cada función pura que hoy vive en `FrmMain` puede promocionarse a servicio/helper con test de caja negra.

Fuera de alcance: refactor del game loop (hecho en `refactor-frmmain-coordinators`, pendiente el port final por validación manual), tests de event handlers y tests de flujos end-to-end.

## What Changes

- Extraer **`EnrichActionWithBBAmount`** a un servicio `IActionFormatter` en `src/OpenScrape.App/Services/`. La implementación recibe el bet map de jugadores y el big blind como parámetros del método, no por campo: el servicio queda puro y testable sin mocks.
- Extraer **`GetPlayerNumber`** a una clase estática `PlayerRegionParser` en `src/OpenScrape.App/Helpers/`. Ya era `static private`; sólo cambia de ubicación y visibilidad.
- Extraer **`CalculateOverlayPosition`** a un servicio `IOverlayPositioner` en `src/OpenScrape.App/Services/` que encapsula la dependencia de `OverlayConfig` via `IOptions<OverlayConfig>`. Acepta un `RECT` light (record `Rectangle`) o directamente `User32.RECT` ya que no es más sensible al entorno.
- Reemplazar las 3 llamadas correspondientes en `FrmMain` por llamadas al servicio/helper. `FrmMain` pasa de contener la lógica a sólo orquestar.
- Añadir **≥20 tests unitarios** en `OpenScrape.App.Tests/`:
  - `ActionFormatterTests.cs` — ≥12 tests (open raise sin villano, 3bet sobre raise, bet decimal, multiplier inválido, big blind 0/negativo, etc.).
  - `PlayerRegionParserTests.cs` — ≥6 tests (p1, p11, p3bet, prefijo distinto, string vacío, null).
  - `OverlayPositionerTests.cs` — ≥6 tests (ventana estándar, offset horizontal, ventana grande, overlay más ancho que ventana, offset vertical, porcentaje 0).
- Documentar en `tasks.md` qué métodos de `FrmMain` siguen sin cobertura y por qué (event handlers WinForms, control creation) para encauzar futuros changes.

## Capabilities

### New Capabilities

- `frmmain-helpers`: expone la lógica pura extraída de `FrmMain` (formato de acciones con BB, parser de regiones, posicionamiento del overlay) como servicios/helpers testables independientes del host WinForms.

### Modified Capabilities

<!-- Ninguna capability existente de `openspec/specs/` cambia. -->

## Impact

- **Código afectado**:
  - Nuevos: `src/OpenScrape.App/Services/IActionFormatter.cs` + `ActionFormatter.cs`, `src/OpenScrape.App/Services/IOverlayPositioner.cs` + `OverlayPositioner.cs`, `src/OpenScrape.App/Helpers/PlayerRegionParser.cs`.
  - Modificado: `src/OpenScrape.App/Forms/FrmMain.cs` — 3 métodos eliminados, 3 llamadas redirigidas. Estimado −40/+10 LOC.
  - Modificado: `src/OpenScrape.App/Program.cs` — 2 registros DI.
  - Nuevos: `OpenScrape.App.Tests/ActionFormatterTests.cs`, `PlayerRegionParserTests.cs`, `OverlayPositionerTests.cs`.
- **APIs públicas**: sin cambios externos. Los 3 helpers son `internal` al ejecutable.
- **Tests**: 852 existentes siguen verdes + ≥20 nuevos → objetivo ≥872 verdes.
- **Performance**: neutro. Las rutas críticas (`ProcessPostFlopAsync`, loop de captura) no se tocan.
- **Riesgos**: el método `EnrichActionWithBBAmount` lee la colección `_playerGameState.Players` directamente (concurrente con el game loop). Al moverlo a un servicio que recibe el snapshot como parámetro, puede haber diferencias de timing si hay llamadas simultáneas — no detectado en práctica, pero anotar en diseño.
- **Deuda posterior**: establece el patrón para extraer más helpers en changes futuros (`SetPreflopAggressors`, `CreateLogWithMarkedHands`, `AnalyzeBoardChange`, etc.). No en este scope.
