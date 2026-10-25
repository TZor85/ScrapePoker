# Fase 4a — Retirar el motor del cliente: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `OpenScrape.App` deja de referenciar `OpenScrape.DecisionMaker`: toda decisión y todo cálculo de equity viajan por HTTP, y el publish del cliente no contiene `OpenScrape.DecisionMaker.dll`.

**Architecture:** El cliente conserva sólo lo que es estado suyo (tracking de villanos, contexto cross-street, exploitability, logging y backtesting) movido a `App/Services`. Los cálculos que hoy hace `IPokerCalculator` en `FrmMain` se reemplazan por los valores que ya devuelve `Contracts.DecisionResult`. El flag `RemoteEngine` desaparece: sólo queda `HttpDecisionEngine`.

**Tech Stack:** .NET 10, WinForms, ASP.NET Core (tests), NUnit.

**Spec:** `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md` (`### Fase 4`).

## Global Constraints

- Castellano en comentarios, tests, commits y docs. Sin atribución de IA.
- `OpenScrape.App.csproj` no puede contener `ProjectReference` a `DecisionMaker` al terminar.
- El publish del cliente no puede contener `OpenScrape.DecisionMaker.dll`.
- `Features` **sigue** referenciada: sacarla es la fase 4b.
- Sin cambios de comportamiento observable: la decisión y el overlay muestran lo mismo que hoy.
- Cada task cierra con `dotnet test OpenScrape.sln` verde y `dotnet format --verify-no-changes` limpio.
- 0 advertencias en Release con `-p:TreatWarningsAsErrors=true`.

## Review Focus

1. **Equity del overlay**: con el cálculo local borrado, ¿el overlay muestra la equity del servidor o queda en 0? Un jugador lo nota en la primera mano.
2. **Preflop sin cálculo local**: el `_pokerCalculator.Calculate` de preflop alimentaba el log y el sizing; debe salir del `DecisionResult`.
3. **Tracking movido**: `OpponentTracker` y `ExploitabilityCalculator` mudados de proyecto no pueden cambiar su estado entre calles (misma instancia por scope).
4. **Servidor caído**: sin motor remoto el loop se detiene; ya cubierto, pero el retiro del flag no debe reintroducir un fallback.
5. **Publish limpio**: un `PackageReference` transitivo no basta; el test debe mirar el directorio de publish, no sólo el csproj.

---

### Task 1: Inventario congelado de dependencias del motor

**Files:**
- Modify: `docs/superpowers/plans/2026-10-25-api-motor-decision-fase-4a.md` (tabla al final)
- Read: `src/OpenScrape.App/Forms/FrmMain.cs:1396,1531,1620,1794,1971`

**Interfaces:** ninguna nueva.

- [ ] **Step 1: Documentar cada uso del motor**

Dos tablas al final del plan. **(a) Cálculos**: para cada una de las 5 llamadas a `_pokerCalculator.Calculate`, calle, campos de `PokerCalculationResult` que se usan después, si alimentan decisión, overlay, log o sizing, y qué campo de `Contracts.DecisionResult` lo reemplaza. **(b) Tipos**: cada archivo de `src/OpenScrape.App` que importa `OpenScrape.DecisionMaker`, con su veredicto `mover a App`, `consumir del contrato` o `borrar`, y el motivo. Si un campo no existe en el contrato, anotarlo como hueco y **no** avanzar a la Task 4 hasta resolverlo.

- [ ] **Step 2: Commit del inventario**

```bash
git add docs/superpowers/plans/2026-10-25-api-motor-decision-fase-4a.md
git commit -m "docs(plans): inventariar los calculos locales del cliente"
```

---

### Task 2: Mover el estado del cliente fuera del motor

**Files:**
- Move: `src/OpenScrape.DecisionMaker/Interfaces/IOpponentTracker.cs` → `src/OpenScrape.App/Services/Tracking/IOpponentTracker.cs`
- Move: `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs` → `src/OpenScrape.App/Services/Tracking/OpponentTracker.cs`
- Move: `src/OpenScrape.DecisionMaker/Interfaces/IOpponentProfileStore.cs` → `src/OpenScrape.App/Services/Tracking/IOpponentProfileStore.cs`
- Move: `src/OpenScrape.DecisionMaker/Services/MartenOpponentProfileStore.cs` → `src/OpenScrape.App/Services/Tracking/MartenOpponentProfileStore.cs`
- Move: `src/OpenScrape.DecisionMaker/Interfaces/IExploitabilityCalculator.cs` → `src/OpenScrape.App/Services/Exploitability/IExploitabilityCalculator.cs`
- Move: `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs` → `src/OpenScrape.App/Services/Exploitability/ExploitabilityCalculator.cs`
- Move: `src/OpenScrape.DecisionMaker/Interfaces/IStrategyBacktester.cs` → `src/OpenScrape.App/Services/Backtesting/IStrategyBacktester.cs`
- Move: `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs` → `src/OpenScrape.App/Services/Backtesting/StrategyBacktester.cs`
- Modify: usings y namespaces; `Program.cs`; tests que los referencian.

**Interfaces:**
- Consumes: nada nuevo.
- Produces: los mismos tipos con namespace `OpenScrape.App.Services.Tracking`, `.Exploitability`, `.Backtesting`.

- [ ] **Step 1: `git mv` de los 8 archivos y cambiar el namespace**

Un `git mv` no cambia namespaces: editarlos en cada archivo. Si `StrategyBacktester` usa `IPostflopDecisionService`, dejarlo como dependencia inyectada: se resuelve más adelante.

- [ ] **Step 2: Compilar y corregir usings**

Run: `dotnet build OpenScrape.sln`
Expected: 0 errores. Los errores de namespace faltante se arreglan agregando el `using` nuevo en `Program.cs`, `GameCoordinator`, `HandProcessor`, `FrmMain`, `HistorialTabControl`, `BankrollTabControl` y los tests.

- [ ] **Step 3: Verificar suite**

Run: `dotnet test OpenScrape.sln` → mismo total que al empezar (1379).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(app): mover el estado del cliente fuera del motor"
```

---

### Task 3: Quitar del cliente los analizadores y el backtester

**Files:**
- Modify: `src/OpenScrape.App/Services/GameCoordinator.cs` (c-bet, danger penalty, `IsPreflopAggressor`)
- Modify: `src/OpenScrape.App/Forms/Controls/HistorialTabControl.cs`, `BankrollTabControl.cs` (según el veredicto de la Task 1)
- Create: `src/OpenScrape.App/Services/Analysis/PreflopSituation.cs`

**Interfaces:**
- Consumes: `HandSituation` (Domain), `Contracts.DecisionResult`.
- Produces: `static bool PreflopSituation.IsAggressor(HandSituation situation)`.

**Por qué no se mueven**: `PreflopAnalyzer`, `DangerPenaltyCalculator` y `IPostflopDecisionService` son parte del motor y los usa el servidor (`PostflopRequestFactory`, `PostflopDecisionService`). Moverlos a `App` rompería la API; el cliente tiene que dejar de necesitarlos.

- [ ] **Step 1: Reemplazar `PreflopAnalyzer.IsPreflopAggressor` por una función local**

`PreflopSituation.IsAggressor` replica la tabla de situaciones que ya usa el motor (`OpenRaise`, `ThreeBet`, `Squeeze`, `FourBet`, `Cold4Bet`, `RaiseOverLimper` → verdadero; el resto falso). Test de tabla en `test/OpenScrape.App.Tests/Services/PreflopSituationTests.cs`, una fila por situación.

- [ ] **Step 2: Sacar el c-bet y el danger penalty de la telemetría**

`GameCoordinator` usa `CalculateCbetAdjustment` sólo para el texto del log y `CalculateDangerPenalty` sólo para el `DecisionRecord` de exploitability. Borrar ambas llamadas y sus variables; el log pasa a mostrar la equity que devolvió el motor, sin desglose de ajuste.

- [ ] **Step 3: Resolver el backtester según la Task 1**

Si el inventario confirma que `StrategyBacktester` sólo alimenta pestañas de historia, se borra del cliente y sus tests. Si alimenta una feature viva, se mueve a `src/Tools/` como herramienta de escritorio y se saca del `App`. **No** se mueve con el motor adentro.

- [ ] **Step 4: Verificar y commitear**

Run: `dotnet test OpenScrape.sln` → verde.
Run: `dotnet build OpenScrape.sln` → 0 errores.

```bash
git add -A
git commit -m "refactor(app): dejar de usar los analizadores del motor"
```

---

### Task 4: Consumir la decisión en vez de calcular equity

**Files:**
- Modify: `src/OpenScrape.App/Forms/FrmMain.cs` (5 sitios inventariados)
- Modify: `src/OpenScrape.App/Services/GameCoordinator.cs` (si algún cálculo queda muerto)

**Interfaces:**
- Consumes: `Contracts.DecisionResult` (`Equity`, `PotOdds`, `TotalOuts`, `EffectiveOuts`, `BetSize`, `BoardTexture`, `Reason`, flags).
- Produces: overlay y log con los mismos valores que hoy.

- [ ] **Step 1: Reemplazar cada sitio según el inventario**

Para cada una de las 5 llamadas: borrar `_pokerCalculator.Calculate(...)` y usar la decisión de esa calle. Si el sitio necesita el resultado **antes** de decidir (preflop), mover la asignación después del `DecideAsync`. No inventar valores: lo que el contrato no traiga, se quita del overlay.

- [ ] **Step 2: Borrar el campo y el parámetro del ctor**

Quitar `_pokerCalculator`, el parámetro `IPokerCalculator pokerCalculator` de `FrmMain` y su registro en `Program.cs`.

- [ ] **Step 3: Verificar comportamiento**

Run: `dotnet test OpenScrape.sln` → verde.
Smoke: arrancar `OpenScrape.App.exe`, abrir el overlay y confirmar que muestra acción, equity y pot odds con el motor local apagado (flag eliminado en la Task 5; hacer el smoke después).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(app): consumir la decision del motor en la interfaz"
```

---

### Task 5: Eliminar el camino local y el flag

**Files:**
- Delete: `src/OpenScrape.App/Services/Http/../` no; ver lista exacta abajo.
- Modify: `src/OpenScrape.App/Program.cs`
- Modify: `src/OpenScrape.App/OpenScrape.App.csproj`
- Modify: `src/OpenScrape.App/appsettings.json`
- Test: `test/OpenScrape.App.Tests/Architecture/AppEngineIsolationTests.cs`

**Interfaces:**
- Consumes: `HttpDecisionEngine`.
- Produces: `IDecisionEngine` registrado siempre contra `HttpDecisionEngine`.

- [ ] **Step 1: Test de arquitectura (rojo)**

```csharp
// Mismos helpers FindProject/FindDir que ContractsIsolationTests.
[Test]
public void App_NoReferenciaDecisionMaker()
{
    var csproj = File.ReadAllText(FindProject("src/OpenScrape.App/OpenScrape.App.csproj"));
    Assert.That(csproj, Does.Not.Contain("OpenScrape.DecisionMaker"));
}

[Test]
public void App_NoUsaLocalDecisionEngine()
{
    var files = Directory.GetFiles(FindDir("src/OpenScrape.App"), "*.cs", SearchOption.AllDirectories);
    Assert.That(files.Select(File.ReadAllText).Any(text => text.Contains("LocalDecisionEngine")), Is.False);
}
```

- [ ] **Step 2: Correr y verificar rojo** → `App_NoReferenciaDecisionMaker` falla.

- [ ] **Step 3: Quitar el flag y el registro local**

En `Program.cs`: borrar `remoteEngineEnabled`, el `else` con `LocalDecisionEngine` y las factories `PreflopRequestFactory`/`PostflopRequestFactory` si sólo las usaba ese camino. Registrar `IDecisionEngine → HttpDecisionEngine` siempre. En `appsettings.json`: borrar `RemoteEngine` y dejar `Api:BaseUrl`.

- [ ] **Step 4: Quitar la referencia del csproj y los global usings**

Borrar `<ProjectReference Include="..\OpenScrape.DecisionMaker\...">` y los `<Using Include="OpenScrape.DecisionMaker.Postflop|Preflop">`. Compilar y corregir lo que quede: cada error es un tipo del motor que todavía se usa y debe haberse movido en las Tasks 2-4.

Run: `dotnet build OpenScrape.sln` → 0 errores.

- [ ] **Step 5: Verificar**

Run: `dotnet test OpenScrape.sln` → verde, incluidos los dos tests nuevos.
Run: `dotnet publish src/OpenScrape.App/OpenScrape.App.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish-check`
Expected: `Test-Path artifacts/publish-check/OpenScrape.DecisionMaker.dll` → `False`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(app): retirar el motor local del cliente"
```

---

### Task 6: Cierre de 4a

**Files:**
- Modify: `scripts/verify-pre-merge.ps1` (checkpoint nuevo)
- Modify: `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md` (4a completada)

- [ ] **Step 1: Checkpoint de publish en el gate**

Después del checkpoint `Arquitectura de Contracts`, publicar en un directorio temporal y fallar si existe `OpenScrape.DecisionMaker.dll`. Limpiar el directorio al terminar.

- [ ] **Step 2: Gate completo**

Run: `.\scripts\verify-pre-merge.ps1 -SkipSmoke`
Expected: `ALL CHECKPOINTS PASSED`, `preflop: 8 | postflop: 7`.

- [ ] **Step 3: Marcar 4a en la spec y commitear**

```bash
git add -A
git commit -m "chore(app): cerrar la fase 4a con el publish sin motor"
```

---

## Inventario de cálculos locales (Task 1)

### Tabla 1 — Sitios de cálculo local

El plan estimaba cinco sitios; el código tiene **seis** (el sexto, vivo, está en
`UiUpdateService`). Se listan todos, incluidos los muertos, porque un método
muerto que calcula equity sigue arrastrando el motor al binario.

| Sitio | Calle | Campos usados | Alimenta | Reemplazo en `DecisionResult` |
|---|---|---|---|---|
| `FrmMain.cs:1396` | Flop | `EquityPercentage`, `PotOddsPercentage`, `ExpectedValue`, `RecommendedAction`, `TotalOuts`, `FoldEquity` | `_flopResult` → overlay (`UpdateWithPotOdds`) y log `LogStreetDecision`; `FoldEquity` viaja como `AdjustedFoldEquity` del request | `Equity`, `PotOdds`, `BetSize`, `Action`, `TotalOuts`, `FoldEquity`, `Reason` |
| `FrmMain.cs:1531` | Turn | ídem flop | `_turnResult` → overlay y log dentro del coordinador | ídem |
| `FrmMain.cs:1620` | River | ídem flop | `_riverResult` → overlay y log dentro del coordinador | ídem |
| `FrmMain.cs:1794` | Preflop | `EquityPercentage` | `UpdateResumeTextForPreflop(PokerCalculationResult)` — **muerto**: su único caller (`UpdateUIWithResults`, L1735) no tiene callers | `Equity` (se agrega al mapeo preflop del servidor) |
| `FrmMain.cs:1969` | — | `GetPotOddsCalculator()` | **muerto**, sin callers | borrar |
| `UiUpdateService.cs:103` | Preflop | `EquityPercentage` (+ ajuste por posición ±3 en el cliente) | **vivo**: texto de resume (`Equity: X%`), llamado desde `FrmMain:718` | `Equity` (servidor) + ajuste por posición, que es presentación y se queda en App |

Notas del inventario:

- El flujo vivo de preflop (`FrmMain:567`) crea `new PokerCalculationResult()`
  vacío y lo pasa a `UiUpdateService`; por eso el resume muestra hoy
  `PotOdds: 0%` y `Should Call: False`. Se mantiene igual.
- `HandProcessor` guarda `IPokerCalculator`, `IBetSizingService` y
  `IOpponentTracker` y **no los usa** (sólo asignación en el ctor): se borran.
- `GameCoordinator` ya decide por `IDecisionEngine`; los valores locales de
  equity, c-bet, danger penalty, textura y board change alimentan sólo
  telemetría, explotabilidad, logs y el `AdjustedFoldEquity` del request.

### Tabla 2 — Veredicto por tipo del motor usado en App

| Tipo(s) | Archivos | Uso en App | Veredicto |
|---|---|---|---|
| `IOpponentTracker`, `OpponentTracker`, `IOpponentProfileStore`, `MartenOpponentProfileStore` | `DecisionMaker/Services`, `Interfaces` | FrmMain, GameCoordinator, HandProcessor, TableLayoutService | **Mover a App** (`Services/Tracking`) — Task 2 |
| `IExploitabilityCalculator`, `ExploitabilityCalculator`, `DecisionRecord` | `DecisionMaker/Services` | FrmMain (panel), GameCoordinator | **Mover a App** (`Services/Exploitability`) — Task 2 |
| `IBankrollTrackerService`, `BankrollTrackerService` | `DecisionMaker/Interfaces`, `Services` | FrmMain, BankrollTabControl | **Mover a App** (`Services/Bankroll`) — Task 2 |
| `IAutoCalibrationService`, `AutoCalibrationService`, `CalibrationResult`, `ParameterAdjustment`, `CalibrationPreview` | `DecisionMaker/Interfaces`, `Services` | FrmMain (botón de calibración) | **Mover a App** (`Services/Calibration`) — Task 2 |
| `PostflopGameContext` | `DecisionMaker/Services` | `IPostflopContextHolder`, `PostflopContextHolder`, GameCoordinator | **Mover a App** (`Services/State`), sin `InitialBoardDanger`, `LastBoardChange` ni `CombineBoardChanges` — Task 2/3 |
| `IBoardTextureAnalyzer`, `BoardTextureAnalyzer`, `BoardTextureResult`, `BoardTextureCategory`, `BoardChangeResult`, `TurnBoardTexture`, `RiverBoardTexture`, `RiverCardType` | `DecisionMaker/Algorithms` | FrmMain, GameCoordinator, FrmOverlay, IFrmOverlay | **Consumir del contrato**: `BoardTexture` (string), `BoardWetnessScore` (nuevo); se borra el análisis local. `TurnBoardTexture`/`RiverBoardTexture`/`BoardChangeResult` desaparecen con la telemetría que los usaba |
| `IBetSizingService`, `BetSizingService` | `DecisionMaker/Interfaces`, `Services` | GameCoordinator (`AdjustBetSize`, **muerto**), HandProcessor (sin uso) | **Consumir del contrato**: `BetSize`; se borra el uso local y el método muerto |
| `IPostflopDecisionService`, `DangerPenaltyCalculator` | `DecisionMaker/Interfaces`, `Services` | GameCoordinator (danger penalty: sólo log/telemetría), FrmMain (campo sin uso), HistorialTabControl | **Borrar el uso en App**; el motor sigue usándolos del lado servidor |
| `IPreflopAnalyzer`, `PreflopAnalyzer` | `DecisionMaker/Services` | GameCoordinator: `IsPreflopAggressor`, `DetectDonkBet`, `HasRangeAdvantageOnBoard`, `CalculateCbetAdjustment` | **Reemplazar por lógica local de situación** (`PreflopSituation.IsAggressor`, `DetectDonkBet`) — Task 3. Range advantage y c-bet no se reemplazan: su resultado ya se descartaba (`doorResult.Equity` lo pisa) |
| `IPokerCalculator`, `UnifiedPokerCalculator`, `PokerCalculationResult`, `CalculationFailedException` | `DecisionMaker/Postflop` | FrmMain, GameCoordinator, FrmOverlay, IFrmOverlay, IGameCoordinator, IUiUpdateService, UiUpdateService | **Borrar**: se reemplaza por `Contracts.DecisionResult` — Task 4 |
| `IStrategyBacktester`, `StrategyBacktester`, `BacktestResult`, `DecisionDivergence` | `DecisionMaker/Interfaces`, `Services` | HistorialTabControl (`Backtest A/B`) | **Borrar**: replayed manos contra `IPostflopDecisionService` local; sin motor no puede correr. El botón queda deshabilitado con aviso — Task 3 |
| `ActionScenarioUseCases`, `GetActionScenario`, `ActionScenarioRequest`, `ISetPreflopActionUseCase`, `SetPreflopActionUseCase`, 10 use cases de acción, `UserHandHelper` | `DecisionMaker/Preflop` | `ActionScenarioUseCases`: campo y parámetro de FrmMain **sin uso**; `GetActionScenario` y `SetPreflopActionUseCase`: sólo registro en `Program.cs` | **Borrar del cliente**: preflop ya decide por `IDecisionEngine` (`FrmMain:1071`) |
| `IDecisionEngine`, `LocalDecisionEngine` | `DecisionMaker` | FrmMain, GameCoordinator, HandProcessor, HttpDecisionEngine | **Consumir del contrato** vía `HttpDecisionEngine`; `LocalDecisionEngine` no viaja al cliente |

### Huecos del contrato y resolución

`DecisionResult` no tiene `Street`, `BoardWetnessScore`, `EVWithFoldEquity`,
`ExpectedValue` ni `ShouldCall`.

| Hueco | Resolución |
|---|---|
| `Street` | No viaja: el cliente conoce la calle y la pasa al overlay. Sin cambio de contrato |
| `BoardWetnessScore` | **Agregar** `double? BoardWetnessScore` al contrato y propagarlo desde `BoardTextureResult.WetnessScore` (el servidor ya lo calcula en `PostflopRequestFactory`). El overlay lo muestra y colorea por categoría |
| `EVWithFoldEquity` | **Agregar** `double? EVWithFoldEquity` al contrato; el servidor ya lo tiene en `PokerCalculationResult` |
| `ExpectedValue` | No se agrega: el log usa `EVWithFoldEquity` |
| `ShouldCall` | Se deriva en el cliente con la misma fórmula del motor (`equity >= potOdds`) para conservar el texto del resume; hoy siempre da `False` porque llega un resultado vacío |
| Equity preflop | El servidor hoy devuelve sólo `Action`. **Agregar** `Equity` al mapeo preflop de `LocalDecisionEngine` con `PreflopEquityCalculator.GetEquity(...) * 100`, el mismo cálculo que usa `UnifiedPokerCalculator` (L233). Sin esto el resume pierde el número y el overlay queda sin equity si alguna calle llegara sin ella |
| `AdjustedFoldEquity` del villano | Sin equity local no hay base que ajustar por tipo. **Ruling**: el cliente manda `AdjustedFoldEquity = -1` y el `PostflopRequestFactory` aplica el multiplicador por `VillainProfile.Type` sobre su propio `FoldEquity` (LP 1.25, TP 1.10, TAG 0.85, LAG 0.70), replicando `OpponentTracker.GetAdjustedFoldEquity`. Preserva las decisiones de los fixtures |
