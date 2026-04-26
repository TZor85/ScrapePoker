## ADDED Requirements

### Requirement: IActionFormatter enriquece acciones preflop con cantidad en BB

El sistema SHALL proveer un servicio `IActionFormatter` con un método `EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind)` que convierte acciones del tipo `"3Bet x6"` en `"3Bet x6 (15BB)"` anexando el monto total en big blinds. El servicio MUST ser puro (sin estado mutable ni efectos colaterales) y tolerar entradas inválidas sin lanzar excepción.

#### Scenario: Open raise sin apuesta previa del villano

- **GIVEN** `action = "Open Raise x2.4"`, `villains` todos con `Bet = 0`, `bigBlind = 0.5m`
- **WHEN** se invoca `EnrichActionWithBBAmount`
- **THEN** el resultado es `"Open Raise x2.4 (2,4BB)"` (base = big blind)

#### Scenario: 3Bet sobre raise del villano

- **GIVEN** `action = "3Bet x6"`, un villano con `Bet = 2.5m`, `bigBlind = 0.5m`
- **WHEN** se invoca `EnrichActionWithBBAmount`
- **THEN** el resultado es `"3Bet x6 (30BB)"` (6 × 2.5 = 15 = 30 BB)

#### Scenario: Multiplicador inválido o cero

- **GIVEN** `action = "Check"` (sin `x`), o `action = "Raise x0"`, o `action = "Raise xabc"`
- **WHEN** se invoca `EnrichActionWithBBAmount`
- **THEN** el resultado es la cadena original sin modificar

#### Scenario: Big blind no configurado

- **GIVEN** `bigBlind = 0`
- **WHEN** se invoca `EnrichActionWithBBAmount` con cualquier action que contenga `x`
- **THEN** el servicio usa un fallback sensato (0.5) para el cálculo, preservando el contrato actual de `FrmMain` (que hacía `if (bigBlind <= 0) bigBlind = 0.50m`)

### Requirement: PlayerRegionParser extrae números de jugador de strings de región

El sistema SHALL proveer una clase estática `PlayerRegionParser` con un método `GetPlayerNumber(string regionName, string extraText = "")` que extrae el número de jugador embebido en nombres de región del tablemap (p.ej. `"p3bet"` → `3`, `"p11stack"` → `11`). El método MUST retornar `null` para inputs inválidos sin lanzar.

#### Scenario: Nombre de región estándar

- **GIVEN** `regionName = "p3bet"`, `extraText = "bet"`
- **WHEN** se invoca `GetPlayerNumber`
- **THEN** el resultado es `3`

#### Scenario: Número multidígito

- **GIVEN** `regionName = "p11stack"`, `extraText = "stack"`
- **WHEN** se invoca `GetPlayerNumber`
- **THEN** el resultado es `11`

#### Scenario: Nombre sin prefijo esperado

- **GIVEN** `regionName = "dealer_button"`, `extraText = ""`
- **WHEN** se invoca `GetPlayerNumber`
- **THEN** el resultado es `null`

#### Scenario: Input vacío o nulo

- **GIVEN** `regionName` es `""` o `null`
- **WHEN** se invoca `GetPlayerNumber`
- **THEN** el resultado es `null` sin lanzar excepción

### Requirement: IOverlayPositioner calcula la posición del overlay sobre la ventana del casino

El sistema SHALL proveer un servicio `IOverlayPositioner` con un método `Calculate(int windowLeft, int windowRight, int windowBottom, int overlayWidth)` que retorna un `Point` con la posición destino del overlay, centrado horizontalmente sobre la ventana con offset porcentual, y desplazado verticalmente desde el borde inferior según `OverlayConfig.VerticalOffset`. El servicio MUST ser determinístico y reproducir exactamente la aritmética actual de `FrmMain.CalculateOverlayPosition`.

#### Scenario: Ventana estándar sin offset horizontal

- **GIVEN** `OverlayConfig { HorizontalOffsetPercent = 0, VerticalOffset = 100 }`, ventana `left = 0, right = 1920, bottom = 1080`, `overlayWidth = 400`
- **WHEN** se invoca `Calculate`
- **THEN** el resultado es `Point(760, 980)` (center = 960, x = 960 - 200 - 0 = 760; y = 1080 - 100 = 980)

#### Scenario: Offset horizontal del 5%

- **GIVEN** `HorizontalOffsetPercent = 0.05`, ventana `[0, 1920]`, `overlayWidth = 400`
- **WHEN** se invoca `Calculate`
- **THEN** el x es `960 - 200 - 96 = 664`

#### Scenario: Overlay más ancho que la ventana

- **GIVEN** ventana `[0, 100]`, `overlayWidth = 400`
- **WHEN** se invoca `Calculate`
- **THEN** el x es negativo (fuera de la ventana), el servicio NO corrige — queda responsabilidad del caller. El comportamiento actual de `FrmMain` se preserva sin introducir clamping nuevo.

### Requirement: FrmMain delega en los helpers extraídos

Tras la extracción, `FrmMain` MUST NOT contener las implementaciones originales de `EnrichActionWithBBAmount`, `GetPlayerNumber` ni `CalculateOverlayPosition`. Las llamadas existentes desde `FrmMain` SHALL delegar en los servicios inyectados (`IActionFormatter`, `IOverlayPositioner`) o el helper estático (`PlayerRegionParser`).

#### Scenario: Inspección del form tras el refactor

- **GIVEN** el código fuente de `FrmMain.cs` tras aplicar el change
- **WHEN** se busca la firma `private string EnrichActionWithBBAmount` o `private Point CalculateOverlayPosition`
- **THEN** no hay coincidencias
- **AND** `FrmMain` inyecta `IActionFormatter` e `IOverlayPositioner` en su constructor
- **AND** la invocación a `PlayerRegionParser.GetPlayerNumber(...)` sustituye a la llamada local `GetPlayerNumber(...)`

#### Scenario: Suite de tests existente sigue verde

- **WHEN** se ejecuta `dotnet test OpenScrape.sln` tras el refactor
- **THEN** los 852 tests previos siguen pasando
- **AND** los ≥20 tests nuevos de los helpers pasan
- **AND** no aparece ningún test rojo nuevo por regresión
