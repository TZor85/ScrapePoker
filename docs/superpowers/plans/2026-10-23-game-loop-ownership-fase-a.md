# Game Loop Ownership — Fase A (TurnGate) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extraer el latch de detección de turno de `DetectionLoopService` a un componente con contrato propio (`ITurnGate`) y tests deterministas, sin cambiar el comportamiento observable.

**Architecture:** El latch `_executeCapture` (que evita re-disparar la captura mientras el color de acción siga en rango) se mueve a `TurnGate`, un objeto puro sin dependencias. `DetectionLoopService` lo recibe por constructor y delega en él tanto el cálculo de `ShouldCapture`/`ShouldCaptureFlop` como el seteo y la limpieza del latch. El loop de píxeles y la emisión de eventos no cambian.

**Tech Stack:** C# / .NET 10, WinForms, Microsoft.Extensions.DependencyInjection, NUnit 4.3.2 (sin librería de mocks: fakes a mano).

**Spec:** `docs/superpowers/specs/2026-10-23-game-loop-ownership-design.md`

## Global Constraints

- Target framework `net10.0`; nada de paquetes nuevos.
- 0 advertencias: el build con `-p:TreatWarningsAsErrors=true` debe pasar.
- `dotnet format --verify-no-changes` limpio.
- Comentarios y mensajes en castellano; identificadores en inglés.
- Sin `Co-Authored-By` ni atribución de IA en los commits.
- Al terminar: `.\scripts\verify-pre-merge.ps1 -SkipSmoke` en verde.
- No tocar `HandProcessor`, `GameLoopCoordinator`, `FrmMain` ni el feature flag en esta fase.

## Review Focus

1. **Latch que no se limpia:** si el héroe hace dos acciones seguidas sin que el color salga del rango, la segunda no se detecta y se pierde la mano. Test: secuencia rango → fuera de rango → rango.
2. **Latch que se limpia de más:** si se limpia con el color todavía en rango, se dispara captura en cada poll (10/s). Test: dos evaluates consecutivos en rango devuelven una sola captura.
3. **Flop depende del turno:** `ShouldCaptureFlop` solo puede ser verdadero si `ShouldCaptureTurn` también lo es. Test: flop visible con latch activo no captura.
4. **Reset al arrancar:** `StartAsync` debe dejar el gate abierto, si no una sesión nueva hereda el latch de la anterior. Test: gate latcheado antes de `StartAsync` queda limpio después.
5. **Hilo:** `IsLatched` se lee desde tests y desde `FrmMain` mientras el loop escribe. `_latched` debe ser `volatile`.

---

## Estructura de archivos

**Nuevos:**
- `src/OpenScrape.App/Services/ITurnGate.cs` — contrato + `TurnGateDecision`.
- `src/OpenScrape.App/Services/TurnGate.cs` — implementación del latch.
- `test/OpenScrape.App.Tests/TurnGateTests.cs` — 6 tests de semántica.

**Modificados:**
- `src/OpenScrape.App/Services/DetectionLoopService.cs` — usa `ITurnGate`; se elimina `_executeCapture`.
- `src/OpenScrape.App/Program.cs` — registro `ITurnGate`.
- `test/OpenScrape.App.Tests/DetectionLoopServiceTests.cs` — helper `CreateService` recibe el gate; 2 tests nuevos de wiring.

---

### Task 1: TurnGate

**Files:**
- Create: `src/OpenScrape.App/Services/ITurnGate.cs`
- Create: `src/OpenScrape.App/Services/TurnGate.cs`
- Test: `test/OpenScrape.App.Tests/TurnGateTests.cs`

**Interfaces:**
- Consumes: nada.
- Produces:
```csharp
namespace OpenScrape.App.Services;

public readonly record struct TurnGateDecision(bool ShouldCaptureTurn, bool ShouldCaptureFlop);

public interface ITurnGate
{
    bool IsLatched { get; }
    TurnGateDecision Evaluate(bool isActionColorInRange, bool isFlopVisible);
    void Reset();
    void ForceLatched(bool value); // solo para tests y replay
}
```

- [ ] **Step 1: Escribir los tests que fallan**

```csharp
using OpenScrape.App.Services;

namespace OpenScrape.App.Tests;

[TestFixture]
public class TurnGateTests
{
    [Test]
    public void Evaluate_ColorEnRango_SinLatch_DevuelveCaptureTurn()
    {
        var gate = new TurnGate();

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.True);
            Assert.That(decision.ShouldCaptureFlop, Is.False);
            Assert.That(gate.IsLatched, Is.True);
        });
    }

    [Test]
    public void Evaluate_ColorEnRangoConLatch_NoDevuelveCaptureTurn()
    {
        var gate = new TurnGate();
        gate.ForceLatched(true);

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.False);
            Assert.That(gate.IsLatched, Is.True);
        });
    }

    [Test]
    public void Evaluate_ColorFueraDeRango_LimpiaElLatch()
    {
        var gate = new TurnGate();
        gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        gate.Evaluate(isActionColorInRange: false, isFlopVisible: false);
        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.True, "vuelve a detectar al reentrar en rango");
            Assert.That(gate.IsLatched, Is.True);
        });
    }

    [Test]
    public void Evaluate_FlopVisibleYTurnDetectado_DevuelveCaptureFlop()
    {
        var gate = new TurnGate();

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: true);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.True);
            Assert.That(decision.ShouldCaptureFlop, Is.True);
        });
    }

    [Test]
    public void Evaluate_FlopVisiblePeroTurnLatcheado_NoDevuelveCaptureFlop()
    {
        var gate = new TurnGate();
        gate.ForceLatched(true);

        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: true);

        Assert.Multiple(() =>
        {
            Assert.That(decision.ShouldCaptureTurn, Is.False);
            Assert.That(decision.ShouldCaptureFlop, Is.False);
        });
    }

    [Test]
    public void Reset_LimpiaElLatch_YPermiteVolverADetectar()
    {
        var gate = new TurnGate();
        gate.ForceLatched(true);

        gate.Reset();
        var decision = gate.Evaluate(isActionColorInRange: true, isFlopVisible: false);

        Assert.Multiple(() =>
        {
            Assert.That(gate.IsLatched, Is.True);
            Assert.That(decision.ShouldCaptureTurn, Is.True);
        });
    }
}
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

```powershell
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~TurnGateTests"
```
Esperado: FAIL de compilación, `TurnGate` no existe.

- [ ] **Step 3: Implementar `TurnGate`**

`TurnGate.cs`: campo `private volatile bool _latched;`. `Evaluate` calcula primero
y latchea después, en este orden exacto (paridad con el loop actual):

```csharp
public TurnGateDecision Evaluate(bool isActionColorInRange, bool isFlopVisible)
{
    bool shouldCaptureTurn = isActionColorInRange && !_latched;
    bool shouldCaptureFlop = shouldCaptureTurn && isFlopVisible;

    if (shouldCaptureTurn)
        _latched = true;
    else if (!isActionColorInRange)
        _latched = false;

    return new TurnGateDecision(shouldCaptureTurn, shouldCaptureFlop);
}
```

`Reset()` asigna `false`; `ForceLatched(bool value)` asigna `value`.
Comentario de clase: explica que es el latch extraído de
`DetectionLoopService._executeCapture` y por qué existe (no re-disparar mientras
el color siga en rango). Sin dependencias de logging: es lógica pura.

- [ ] **Step 4: Correr los tests y verificar que pasan**

```powershell
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~TurnGateTests"
```
Esperado: PASS, 6 tests.

- [ ] **Step 5: Commit**

```bash
git add src/OpenScrape.App/Services/ITurnGate.cs src/OpenScrape.App/Services/TurnGate.cs test/OpenScrape.App.Tests/TurnGateTests.cs
git commit -m "feat(services): extraer el latch de turno a TurnGate"
```

---

### Task 2: DetectionLoopService delega en ITurnGate

**Files:**
- Modify: `src/OpenScrape.App/Services/DetectionLoopService.cs`
- Modify: `src/OpenScrape.App/Program.cs:191`
- Test: `test/OpenScrape.App.Tests/DetectionLoopServiceTests.cs`

**Interfaces:**
- Consumes: `ITurnGate`, `TurnGateDecision`, `TurnGate` (Task 1).
- Produces: `DetectionLoopService(GetWindowsScreenUseCase, RegionLookupCache, DetectionLoggerService, ICoordinateScaler, ITurnGate, ILogger<DetectionLoopService>)`.

- [ ] **Step 1: Escribir los tests que fallan**

En `DetectionLoopServiceTests.cs`, agregar al final del fixture:

```csharp
[Test]
public void SetExecuteCaptureForTest_Delega_En_El_TurnGate()
{
    var gate = new TurnGate();
    var service = CreateService(turnGate: gate);

    service.SetExecuteCaptureForTest(true);

    Assert.That(gate.IsLatched, Is.True);
}

[Test]
public async Task StartAsync_Limpia_El_Latch_Del_TurnGate()
{
    var gate = new TurnGate();
    gate.ForceLatched(true);
    var service = CreateService(turnGate: gate);
    using var cts = new CancellationTokenSource();

    await service.StartAsync(IntPtr.Zero, () => true, cts.Token);
    await service.StopAsync();

    Assert.That(gate.IsLatched, Is.False);
}
```

Y actualizar el helper existente (línea ~416) para aceptar el gate:

```csharp
private static DetectionLoopService CreateService(
    RegionLookupCache? regionCache = null,
    ITurnGate? turnGate = null)
{
    // ...construcción existente...
    return new DetectionLoopService(
        // ...argumentos existentes...,
        turnGate ?? new TurnGate());
}
```

- [ ] **Step 2: Correr los tests y verificar que fallan**

```powershell
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~DetectionLoopServiceTests"
```
Esperado: FAIL de compilación, el constructor no acepta `ITurnGate`.

- [ ] **Step 3: Implementar la delegación**

1. Campo `private readonly ITurnGate _turnGate;` y parámetro de constructor con
   `?? throw new ArgumentNullException(nameof(turnGate))`.
2. Eliminar el campo `_executeCapture`.
3. En `PerformEnhancedDetection`, calcular `isFlopVisible` antes y reemplazar el
   cálculo por:
```csharp
var gateDecision = _turnGate.Evaluate(isActionColorInRange, isFlopVisible);
bool shouldCapture = gateDecision.ShouldCaptureTurn;
bool shouldCaptureFlop = gateDecision.ShouldCaptureFlop;
```
4. En el bloque `catch` (fallback `GetPixel`), la misma delegación con
   `actionColor.B == 24` y `flopColor.B == 255`.
5. Borrar el seteo posterior al evento en `DetectionLoop`:
```csharp
// Prevenir re-detección hasta que el color salga del rango
_executeCapture = true;
// Reset del flag de ejecución
if (!detectionResult.IsActionColorInRange)
    _executeCapture = false;
```
   El gate ya latcheó dentro de `Evaluate`.
6. En `StartAsync`, reemplazar `_executeCapture = false;` por `_turnGate.Reset();`.
7. `SetExecuteCaptureForTest(bool value)` pasa a delegar: `_turnGate.ForceLatched(value);`.
8. En `Program.cs`, junto al registro de `DetectionLoopService`:
```csharp
services.AddSingleton<ITurnGate, TurnGate>();
```

- [ ] **Step 4: Correr la suite completa y verificar**

```powershell
dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj
```
Esperado: PASS. Los tests existentes de `PerformEnhancedDetection` (incluido el
que usa `SetExecuteCaptureForTest`) siguen pasando sin tocarlos: son la prueba
de paridad.

- [ ] **Step 5: Verificar gates del repo**

```powershell
dotnet build OpenScrape.sln --configuration Release --no-incremental -p:TreatWarningsAsErrors=true -warnaserror:"NU1901;NU1902;NU1903;NU1904"
dotnet format OpenScrape.sln --verify-no-changes
```
Esperado: exit code 0 en ambos.

- [ ] **Step 6: Commit**

```bash
git add src/OpenScrape.App/Services/DetectionLoopService.cs src/OpenScrape.App/Program.cs test/OpenScrape.App.Tests/DetectionLoopServiceTests.cs
git commit -m "refactor(detection): detectar turno a traves de ITurnGate"
```

---

## Handoff

Al terminar Task 2, la Fase A está cerrada: `ITurnGate` es consultable desde
fuera del loop y la Fase B puede apoyarse en él. No se activa ningún flag ni se
toca `FrmMain` en esta fase.
