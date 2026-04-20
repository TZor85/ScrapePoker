## 1. Extracción de PlayerRegionParser (helper estático, más simple)

- [x] 1.1 Crear `src/OpenScrape.App/Helpers/PlayerRegionParser.cs` con `public static class PlayerRegionParser` y método `public static int? GetPlayerNumber(string? regionName, string extraText = "")` copiando la lógica regex actual de `FrmMain.GetPlayerNumber` (líneas 152-159).
- [x] 1.2 Crear `OpenScrape.App.Tests/PlayerRegionParserTests.cs` con ≥6 tests: nombre estándar (`"p3bet"` → 3), número multi-dígito (`"p11stack"` → 11), extraText vacío (`"p5"` → 5), prefijo distinto (`"dealer_button"` → null), string vacío (`""` → null), string null (`null` → null).
- [x] 1.3 En `FrmMain.cs`, eliminar la definición `private static int? GetPlayerNumber(...)` y reemplazar las llamadas internas (`GetPlayerNumber(region.Name, "bet")`) por `PlayerRegionParser.GetPlayerNumber(region.Name, "bet")`. Añadir `using OpenScrape.App.Helpers;` si no está.
- [x] 1.4 `dotnet build` + `dotnet test --filter PlayerRegionParserTests` verdes.

## 2. Extracción de IActionFormatter (servicio con snapshot de villanos)

- [x] 2.1 Crear `src/OpenScrape.App/Services/IActionFormatter.cs` con `string EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind)`.
- [x] 2.2 Crear `src/OpenScrape.App/Services/ActionFormatter.cs` implementando la lógica actual (regex `x`, extracción del multiplicador con `InvariantCulture`, `maxVillainBet` vs `bigBlind`, fallback `bigBlind <= 0 → 0.5m`, `Math.Round(totalBet/bigBlind, 1)`, formato `"{action} ({totalBB}BB)"`). Copia literal sin cambios de comportamiento.
- [x] 2.3 Registrar `services.AddSingleton<IActionFormatter, ActionFormatter>()` en `Program.cs` (singleton: servicio sin estado).
- [x] 2.4 En `FrmMain.cs`, inyectar `IActionFormatter _actionFormatter` (ctor); eliminar `private string EnrichActionWithBBAmount(string action)`; en los 2 call sites (líneas 1860 y 1960) pasar `_actionFormatter.EnrichActionWithBBAmount(action, _playerGameState.Players.Where(p => p.Name != "P0").ToList(), _gameLoggerService.CurrentBigBlind)`.
- [x] 2.5 Crear `OpenScrape.App.Tests/ActionFormatterTests.cs` con ≥12 tests:
  - Open raise sin villano (base = BB)
  - 3Bet sobre raise del villano (base = maxVillainBet)
  - Multiplicador decimal (`x2.4`)
  - Sin `x` en action → sin cambio
  - `x0` → sin cambio
  - `xabc` → sin cambio
  - `bigBlind = 0` → fallback 0.5
  - `bigBlind` negativo → fallback 0.5
  - Lista de villanos vacía
  - Múltiples villanos, toma el mayor
  - Bet decimal no-redondo (verifica `Math.Round(..., 1)`)
  - Null action → null o "" (comportamiento del original)
- [x] 2.6 `dotnet build` + `dotnet test --filter ActionFormatterTests` verdes.

## 3. Extracción de IOverlayPositioner

- [x] 3.1 Crear `src/OpenScrape.App/Services/IOverlayPositioner.cs` con `Point Calculate(int windowLeft, int windowRight, int windowBottom, int overlayWidth)`.
- [x] 3.2 Crear `src/OpenScrape.App/Services/OverlayPositioner.cs` que recibe `IOptions<OverlayConfig>` en ctor e implementa la aritmética actual de `FrmMain.CalculateOverlayPosition`.
- [x] 3.3 Registrar `services.AddSingleton<IOverlayPositioner, OverlayPositioner>()` en `Program.cs`.
- [x] 3.4 En `FrmMain.cs`, inyectar `IOverlayPositioner _overlayPositioner`; eliminar `private Point CalculateOverlayPosition(...)`; reemplazar los 2 callers (`btnWindow_Click` líneas 2692, 2712) por `_overlayPositioner.Calculate(windowRect.left, windowRect.right, windowRect.bottom, _frmOverlay.Size.Width)`.
- [x] 3.5 Crear `OpenScrape.App.Tests/OverlayPositionerTests.cs` con ≥6 tests:
  - Ventana 1920x1080, offset percent 0, overlay 400 → `Point(760, 980)` (con VerticalOffset 100).
  - Offset horizontal 5% → x ajustado.
  - Offset vertical 200 → y ajustado.
  - Ventana [0, 100], overlay 400 → x negativo (sin clamping).
  - `HorizontalOffsetPercent = 0` no altera x.
  - Ventana desplazada (left > 0) → x correcto con offset.
- [x] 3.6 `dotnet build` + `dotnet test --filter OverlayPositionerTests` verdes.

## 4. Verificación final

- [x] 4.1 `dotnet build OpenScrape.sln` → 0 errores, 0 warnings nuevos.
- [x] 4.2 `dotnet test OpenScrape.sln` → 852 + ≥24 nuevos = ≥876 verdes.
- [x] 4.3 `dotnet format --verify-no-changes OpenScrape.sln` sin cambios.
- [x] 4.4 `grep -n "EnrichActionWithBBAmount\|private.*GetPlayerNumber\|CalculateOverlayPosition" src/OpenScrape.App/Forms/FrmMain.cs` no debe mostrar las definiciones originales (sólo llamadas a los servicios si aplica).
- [x] 4.5 `wc -l src/OpenScrape.App/Forms/FrmMain.cs` — baseline 4,477; esperado ~4,437 (−40 LOC aprox).
- [x] 4.6 Documentar en este mismo archivo (al final) qué otros helpers quedaron identificados como candidatos para futuros changes:
  - `SetPreflopAggressors()` (línea 1194, ~25 LOC) — lógica de detección de agresor preflop.
  - `AnalyzeBoardChange(List<BoardData>, int)` (línea 1375, delega en coordinator — probablemente trivial).
  - `CreateLogWithMarkedHands()` (línea 2359) — file I/O.
  - `GetRegionFromNode(TreeNode)` (línea 595) — parsea tags de TreeNode (requiere abstracción).
