## Context

Estado tras `refactor-frmmain-coordinators`:
- `FrmMain.cs` = **4,477 LOC**, 30 dependencias DI, mezcla event handlers (legítimamente UI) con helpers de lógica pura (históricamente convenientes por estar cerca de los consumidores).
- El refactor del game loop principal (port de `btnCapture_Click` al coordinator) quedó diferido porque requiere validación con screenshots reales de casino.
- Tests existentes: **852 verdes**, pero cero sobre `FrmMain`.

Tres métodos concretos identificados como extracción segura:

1. **`EnrichActionWithBBAmount(string action)`** (líneas 113-147, ~35 LOC) — parser + aritmética. Lee `_playerGameState.Players` (lista de `Player`) y `_gameLoggerService.CurrentBigBlind`. Output: string enriquecido.
2. **`GetPlayerNumber(string regionName, string extraText = "")`** (líneas 152-159, ~8 LOC, ya `static`) — regex `^p(\d+){extraText}`. Puro.
3. **`CalculateOverlayPosition(User32.RECT, int)`** (líneas 2669-2677, 9 LOC) — geometría con `_overlayConfig.HorizontalOffsetPercent` y `VerticalOffset`.

Callers conocidos:
- `EnrichActionWithBBAmount`: 2 llamadas (`FrmMain.cs:1860`, `:1960`).
- `GetPlayerNumber`: 1 llamada (`FrmMain.cs:2101`) + 1 interna en un loop de regiones.
- `CalculateOverlayPosition`: 2 llamadas (`btnWindow_Click` en 2 puntos, líneas 2692 y 2712).

Restricciones: no tocar event handlers, no redibujar la UI, 0 cambios visibles al usuario. Mantener el mismo parsing y aritmética que hoy para que los tests existentes no cambien su comportamiento observable.

## Goals / Non-Goals

**Goals:**
- Extraer los 3 helpers a servicios/clase estática en ubicaciones convencionales (`Services/`, `Helpers/`).
- Añadir ≥20 tests unitarios que cubran los 3 helpers.
- Eliminar las implementaciones originales de `FrmMain` y redirigir las llamadas.
- 0 regresiones en los 852 tests existentes.
- Establecer precedente replicable: cada helper extraído sigue el mismo patrón (servicio + interfaz + tests o clase estática + tests).

**Non-Goals:**
- **NO** tocar event handlers ni `ProcessPostFlopAsync`.
- **NO** mover `SetPreflopAggressors`, `AnalyzeBoardChange`, `CreateLogWithMarkedHands` ni otros helpers más pesados — se dejan para changes posteriores.
- **NO** introducir framework de mocks (Moq, NSubstitute) — seguir con fakes manuales como el resto del repo.
- **NO** cambiar la lógica de parsing ni aritmética — extracción pura, comportamiento 1:1.
- **NO** reducir la dependencia count de `FrmMain` significativamente (se añaden 2 nuevas, pero `GetPlayerNumber` es `static` y no suma dep).

## Decisions

### D1: `EnrichActionWithBBAmount` pasa a `IActionFormatter` con inputs explícitos

La firma original `EnrichActionWithBBAmount(string action)` lee estado del form. El nuevo método es puro:

```csharp
public interface IActionFormatter
{
    string EnrichActionWithBBAmount(string action, IEnumerable<Player> villains, decimal bigBlind);
}
```

`FrmMain` compone la llamada: `_actionFormatter.EnrichActionWithBBAmount(action, _playerGameState.Players.Where(p => p.Name != "P0"), _gameLoggerService.CurrentBigBlind)`.

**Ventaja**: tests no necesitan mock de `PlayerGameState` ni `GameLoggerService` — pasan listas y decimales directos.

**Alternativa rechazada**: inyectar `PlayerGameState` y `GameLoggerService` al servicio. Introduce acoplamiento innecesario; además `PlayerGameState` es mutable y no hay forma limpia de hacerlo inmutable aquí.

### D2: `GetPlayerNumber` pasa a `PlayerRegionParser` estático

Ya era `static`. No hay estado que inyectar. Se mueve tal cual a `src/OpenScrape.App/Helpers/PlayerRegionParser.cs`:

```csharp
public static class PlayerRegionParser
{
    public static int? GetPlayerNumber(string? regionName, string extraText = "") { ... }
}
```

**Ventaja**: llamadas desde `FrmMain` quedan como `PlayerRegionParser.GetPlayerNumber(...)`. No toca DI.

**Alternativa rechazada**: hacerlo método de instancia en un servicio. No añade valor (no hay config ni estado) y complica el llamado.

### D3: `CalculateOverlayPosition` pasa a `IOverlayPositioner`

Depende de `OverlayConfig`, que es un `IOptions<OverlayConfig>`:

```csharp
public interface IOverlayPositioner
{
    Point Calculate(int windowLeft, int windowRight, int windowBottom, int overlayWidth);
}
```

La implementación toma `IOptions<OverlayConfig>` en el ctor. El método acepta coordenadas como `int` (no `User32.RECT`) para que el test no dependa del tipo interop.

**Alternativa rechazada**: aceptar `User32.RECT` directamente. Filtra detalle interop al contrato. La conversión en el caller es trivial (`windowRect.left, windowRect.right, windowRect.bottom`).

### D4: No introducir `using static` para `PlayerRegionParser`

Las llamadas desde `FrmMain` quedan calificadas (`PlayerRegionParser.GetPlayerNumber(...)`). Esto aumenta 1 línea vs. `using static`, pero hace el origen explícito en el cuerpo del form. Para el código del form, explicitud > concisión.

### D5: Tests ubicados en raíz de `OpenScrape.App.Tests/`

Siguiendo la convención actual (todos los `*Tests.cs` están en raíz sin subdirectorios), los 3 archivos nuevos van en la raíz. No se introduce estructura de subdirectorios en esta change.

### D6: Comportamiento preservado exactamente — cero cambios observables

Cada extracción es un copy-paste del código original renombrando referencias a estado (`_playerGameState.Players` → `villains` parámetro, `_overlayConfig.X` → `_config.Value.X`). Los tests verifican el mismo output que el código original produciría.

### D7: Guardrail de cobertura acumulable

Los tests siguen el patrón existente: `[TestFixture]` NUnit, assertions `Assert.That(...)`. No se añade atributo de coverage mínimo ni gates de CI — la expansión del ratio coverage se hace en changes sucesivos, cada uno sumando helpers al pool testable.

## Risks / Trade-offs

- **Riesgo: concurrencia en `_playerGameState.Players`**. El método original lee `_playerGameState.Players.Where(...)` sin lock — si el game loop muta la colección durante la evaluación, hay posible `InvalidOperationException`. El nuevo servicio recibe `IEnumerable<Player>` y el caller es responsable de pasar un snapshot. **Mitigación**: en `FrmMain`, los 2 callers pasan `.ToList()` antes de invocar el servicio. Los tests verifican con listas estáticas.
- **Riesgo: localización decimal (`,` vs `.`)**. El test del `EnrichActionWithBBAmount` assertaba originalmente en cultura invariante, pero el método usa `CultureInfo.InvariantCulture` para el parseo y el output usa la cultura por defecto en `$"{totalBB}BB"` (decimal). El output puede ser `"2,4BB"` en es-ES o `"2.4BB"` en en-US. **Mitigación**: los tests aceptan ambos formatos (`Is.EqualTo("2,4BB").Or.EqualTo("2.4BB")`) — consistente con `UiSyncServiceTests` que ya aplica el patrón.
- **Trade-off: el diff de `FrmMain` es pequeño (~40 LOC menos) y la LOC total casi no baja**. La ganancia no es "FrmMain más pequeño" sino "FrmMain más testable indirectamente". Se acepta.
- **Trade-off: tests no cubren las rutas UI que invocan al helper** (p.ej. `_frmOverlay.UpdateAction(EnrichActionWithBBAmount(...))`). Siguen sin test. La cobertura baja del UI no es evitable sin UI integration framework y está fuera de scope.
- **Riesgo: añadir 2 dependencias al ctor de `FrmMain` (30 → 32)**. Queremos eventualmente bajar, no subir. Documentado en `tasks.md` como deuda a consolidar (posible facade en futuro).

## Migration Plan

1. **Crear helpers** en sus ubicaciones finales con implementación migrada del original. Aún no modificar `FrmMain`.
2. **Añadir tests** y verificar que pasan con la nueva implementación extraída (los tests demuestran que el helper funciona aislado).
3. **Migrar callers en `FrmMain`**: inyectar `IActionFormatter` y `IOverlayPositioner`; reemplazar llamadas; eliminar los 3 métodos originales de `FrmMain`.
4. **Registrar en DI** en `Program.cs`.
5. **Build + test** → 852 + ≥20 = ≥872 verdes, 0 regresiones.
6. **Format + commit**.

**Rollback**: revertir el commit. Los helpers son aditivos; `FrmMain` se restablece a su estado anterior con los 3 métodos.

## Open Questions

- ¿Debe `IActionFormatter` tomar un `IReadOnlyList<Player>` en lugar de `IEnumerable<Player>` para comunicar que es un snapshot? **Inclinación**: `IEnumerable` para no forzar materialización en el caller si ya lo tiene.
- ¿Mover `ActionFormatterTests` y compañía a un subdirectorio `OpenScrape.App.Tests/Helpers/` para organización? **Inclinación**: no — el resto de tests están en raíz, mantener consistencia.
- ¿`OverlayPositioner` debe clampar coordenadas negativas? **Resuelto en spec**: no; preservar comportamiento actual.
