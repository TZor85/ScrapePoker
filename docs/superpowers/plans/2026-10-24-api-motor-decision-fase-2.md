# API del motor de decisión — Fase 2: motor local unificado · Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mover preflop y postflop al mismo proyecto de motor y dejar una única puerta `IDecisionEngine`, sin cambiar una sola decisión.

**Architecture:** Es una mudanza, no un rediseño. Los tipos de estado pasan a `Domain`, la calculadora y los dos caminos de decisión pasan a `DecisionMaker`, y `GameCoordinator` queda en `App` como adaptador que conserva contexto, tracking, exploitability y logging. Los 15 fixtures de la fase 1 son el gate: si una decisión cambia, el replay falla.

**Tech Stack:** .NET 10, NUnit 4.3.2, MediatR (sólo en los use cases que se mueven), Microsoft.Extensions.DependencyInjection.

**Spec:** `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md`

**Rama:** se ejecuta sobre `feature/api-engine-fase-1`, que ya contiene la fase 1 (11 commits).

## Global Constraints

- **No se cambia la lógica del motor.** Sólo se mueve de proyecto y se le inyectan dependencias. Si un movimiento obliga a reescribir una condición, es una señal de que el tipo no está bien ubicado: se resuelve moviendo el tipo, no la lógica.
- `OpenScrape.Domain` no referencia `App`, `Features`, `Infrastructure` ni `DecisionMaker`.
- `OpenScrape.DecisionMaker` referencia `Domain` y **no** referencia `App` ni WinForms.
- Comentarios y documentación en castellano; tests con el estilo del repo.
- 0 advertencias en Release con `-p:TreatWarningsAsErrors=true`.
- `dotnet format --verify-no-changes` limpio.
- Después de cada task: `dotnet test OpenScrape.sln` verde y los fixtures en el mismo número (8 preflop, 7 postflop).
- Al terminar: `.\scripts\verify-pre-merge.ps1 -SkipSmoke` en PASS.
- Sin atribución de IA en los commits.

## Review Focus

Modos de falla que la mudanza puede introducir y que los fixtures no cubren:

1. **Namespace duplicado.** Mover un tipo sin borrar el original deja dos `PlayerGameState` y el compilador elige uno por `using`. Se verifica que el archivo viejo no exista.
2. **DI resuelta por tipo concreto.** `Program.cs` registra `GameLoggerService` y `StrategyProfileService` como concretos; al mover un consumidor hay que registrar la interfaz o el host falla al arrancar, no al compilar.
3. **`InternalsVisibleTo` perdido.** `GameCoordinator.ClampEquity` es `internal` y el test lo ve por el atributo del csproj. Al mover tipos entre proyectos, el atributo tiene que viajar.
4. **Dependencia circular.** `Domain` no puede necesitar nada de `DecisionMaker`. Si al mover un tipo aparece esa necesidad, el tipo pertenece a `DecisionMaker`, no a `Domain`.
5. **Publish.** `OpenScrape.App` sigue publicando con el motor incluido en esta fase; el publish sin motor es criterio de la fase 4. No se toca el pipeline de publish acá.

---

### Task 1: Mover el estado de mesa a `Domain`

**Files:**
- Move: `src/OpenScrape.App/Entities/PlayerGameState.cs` → `src/OpenScrape.Domain/Entities/PlayerGameState.cs`
- Move: `src/OpenScrape.App/Entities/Player.cs` → `src/OpenScrape.Domain/Entities/Player.cs`
- Modify: `src/OpenScrape.App/OpenScrape.App.csproj` (global using)
- Modify: `test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj` (global using)
- Test: la suite completa y los fixtures existentes

**Interfaces:**
- Consumes: `BoardPosition`, `HandSituation`, `TablePosition`, `Rank`, `Suit` (ya en `Domain`).
- Produces: `OpenScrape.Domain.Entities.PlayerGameState`, `Player`, `BoardData`, `ResponseAction`, con la misma forma pública que hoy.

- [ ] **Step 1: Verificar el punto de partida**

Run: `dotnet test OpenScrape.sln`
Expected: 1330 pasando, 0 fallos.

- [ ] **Step 2: Mover los archivos y cambiar el namespace**

```bash
git mv src/OpenScrape.App/Entities/PlayerGameState.cs src/OpenScrape.Domain/Entities/PlayerGameState.cs
git mv src/OpenScrape.App/Entities/Player.cs src/OpenScrape.Domain/Entities/Player.cs
```

En ambos archivos: `namespace OpenScrape.App.Entities;` → `namespace OpenScrape.Domain.Entities;`.
No se toca ninguna propiedad, ningún método ni el `Clone()`.

- [ ] **Step 3: Resolver los usings con global usings**

En `src/OpenScrape.App/OpenScrape.App.csproj` y en `test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj`:

```xml
<ItemGroup>
  <Using Include="OpenScrape.Domain.Entities" />
</ItemGroup>
```

Si `OpenScrape.App.Entities` queda vacío, borrar el directorio.

- [ ] **Step 4: Compilar y arreglar sólo lo que el compilador marque**

Run: `dotnet build OpenScrape.sln`
Expected: 0 errores. Los errores esperables son `using` redundantes o tipos homónimos; no se cambia lógica.

- [ ] **Step 5: Verificar paridad**

Run: `dotnet test OpenScrape.sln`
Expected: 1330 pasando.

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~FixtureReplayTests"`
Expected: 2/2 pasan (preflop y postflop).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(domain): mover el estado de mesa al dominio"
```

---

### Task 2: Mover `UnifiedPokerCalculator` al motor

**Files:**
- Move: `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs` → `src/OpenScrape.DecisionMaker/Postflop/UnifiedPokerCalculator.cs`
- Modify: `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` (referencias que falten)
- Modify: `src/OpenScrape.App/Program.cs` (registro)
- Modify: los `using` de los consumidores (`FrmMain`, `GameCoordinator`, tests)

**Interfaces:**
- Consumes: `MonteCarloSimulator`, `OutsCalculator`, `PreflopEquityCalculator`, `IHandEvaluator`, `BoardTextureAnalyzer`, `StrategyProfile`, `CardDataOuts`.
- Produces: `OpenScrape.DecisionMaker.Postflop.UnifiedPokerCalculator` y `PokerCalculationResult` (viaja en el mismo archivo) con las mismas firmas.

- [ ] **Step 1: Mover el archivo y el namespace**

```bash
git mv src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs src/OpenScrape.DecisionMaker/Postflop/UnifiedPokerCalculator.cs
```

`namespace OpenScrape.App.Aplication.UseCases;` → `namespace OpenScrape.DecisionMaker.Postflop;`.
Verificar que `PokerCalculationResult` esté declarado en ese archivo; si está en otro, moverlo también al mismo namespace.

- [ ] **Step 2: Agregar el global using en App y Tests**

```xml
<Using Include="OpenScrape.DecisionMaker.Postflop" />
```

- [ ] **Step 3: Compilar y corregir referencias**

Run: `dotnet build OpenScrape.sln`
Expected: 0 errores. Si `DecisionMaker` no puede ver algún tipo, el tipo está mal ubicado: se resuelve moviéndolo a `Domain`, no duplicándolo.

- [ ] **Step 4: Verificar el registro de DI**

Run: `dotnet run --project src/OpenScrape.App/OpenScrape.App.csproj --no-build`
Expected: la ventana abre sin `InvalidOperationException` de DI. Cerrar.

Si el host no arranca por un registro que quedó apuntando al tipo viejo, se corrige el registro en `Program.cs`.

- [ ] **Step 5: Verificar paridad**

Run: `dotnet test OpenScrape.sln`
Expected: 1330 pasando, incluidos los 7 fixtures postflop.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(decisionmaker): mover la calculadora de equity al motor"
```

---

### Task 3: Mover el preflop completo al motor

**Files:**
- Move: `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs` → `src/OpenScrape.DecisionMaker/Preflop/GetActionScenario.cs`
- Move: `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs` → `src/OpenScrape.DecisionMaker/Preflop/ActionScenarioUseCases.cs`
- Move: `src/OpenScrape.App/Aplication/UseCases/Actions/*.cs` → `src/OpenScrape.DecisionMaker/Preflop/`
- Move: `src/OpenScrape.App/Helpers/UserHandHelper.cs` → `src/OpenScrape.DecisionMaker/Preflop/UserHandHelper.cs`
- Move: `src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs`, `ISetPreflopActionUseCase.cs` → `src/OpenScrape.DecisionMaker/Preflop/`
- Modify: `src/OpenScrape.App/Program.cs`, `src/OpenScrape.Features/Services.cs`
- Test: los 8 fixtures preflop

**Interfaces:**
- Consumes: `IPreflopRangeStore`, `IRandomProvider` (Domain), `TableDTO`, `PlayerActionSequence`, `Hand`.
- Produces: `OpenScrape.DecisionMaker.Preflop.SetPreflopActionUseCase` con `Task<SetPreflopActionUseCaseResponse> Execute(SetPreflopActionUseCaseRequest)`.

- [ ] **Step 1: Verificar el punto de partida preflop**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopFixtureReplayTests"`
Expected: 1/1 pasa.

- [ ] **Step 2: Mover los archivos conservando la estructura**

```bash
mkdir src/OpenScrape.DecisionMaker/Preflop
git mv src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs src/OpenScrape.DecisionMaker/Preflop/
git mv src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs src/OpenScrape.DecisionMaker/Preflop/
git mv src/OpenScrape.App/Aplication/UseCases/Actions/*.cs src/OpenScrape.DecisionMaker/Preflop/
git mv src/OpenScrape.App/Helpers/UserHandHelper.cs src/OpenScrape.DecisionMaker/Preflop/
git mv src/OpenScrape.App/Aplication/SetPreflopActionUseCase.cs src/OpenScrape.DecisionMaker/Preflop/
git mv src/OpenScrape.App/Aplication/ISetPreflopActionUseCase.cs src/OpenScrape.DecisionMaker/Preflop/
```

`namespace` → `OpenScrape.DecisionMaker.Preflop` en todos.

- [ ] **Step 3: Resolver `ActionScenarioRequest`**

Si `ActionScenarioRequest` vive en `Features/ActionScenario` y sólo lo usan tipos movidos, se mueve con ellos. Si lo usa `Infrastructure` o la UI, se queda y se cambia el `using`. Verificar con:

Run: `Select-String -Path src\**\*.cs -Pattern 'ActionScenarioRequest' | Select-Object -ExpandProperty Path | Sort-Object -Unique`

- [ ] **Step 4: Ajustar DI**

`Program.cs` y `Features/Services.cs`: registrar los use cases movidos con su namespace nuevo. `MartenPreflopRangeStore` se queda en `Features` (implementa la interfaz de `Domain`) y no se mueve.

- [ ] **Step 5: Compilar y corregir**

Run: `dotnet build OpenScrape.sln`
Expected: 0 errores.

- [ ] **Step 6: Verificar paridad preflop**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopFixtureReplayTests"`
Expected: 1/1 pasa, con los 8 escenarios dentro del test.

Run: `dotnet test OpenScrape.sln`
Expected: 1330 pasando.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(decisionmaker): mover el motor preflop al proyecto de decision"
```

---

### Task 4: Extraer `PostflopDecisionEngine` del `GameCoordinator`

**Files:**
- Create: `src/OpenScrape.DecisionMaker/Postflop/PostflopDecisionEngine.cs`
- Create: `src/OpenScrape.DecisionMaker/Postflop/PostflopDecisionEngineResult.cs`
- Modify: `src/OpenScrape.App/Services/GameCoordinator.cs` (delega)
- Test: `test/OpenScrape.App.Tests/Fixtures/PostflopFixtureReplayTests.cs` (sin cambios: es el gate)

**Interfaces:**
- Consumes: `PlayerGameState`, `PokerCalculationResult`, `PostflopDecisionService`, `BoardTextureAnalyzer`, `IBetSizingService`, `StrategyProfile`.
- Produces:
  - `public sealed record PostflopEngineRequest(PlayerGameState State, PokerCalculationResult Result, BoardPosition Street, PostflopGameContext Context, double? DangerPenalty = null);`
  - `public sealed record PostflopDecisionEngineResult(string Action, string? Reason, bool IsBluff, bool IsBarrel, bool IsCheckRaise, string BoardTexture, BoardChangeResult? BoardChange);`
  - `public PostflopDecisionEngineResult Decide(PostflopEngineRequest request);`

- [ ] **Step 1: Verificar el gate antes de tocar**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopFixtureReplayTests"`
Expected: 1/1 pasa (7 escenarios).

- [ ] **Step 2: Escribir el test que fija la nueva puerta**

```csharp
[Test]
public void Decide_FlopSet_CoincideConElCoordinador()
{
    var fixture = PostflopFixtureStore.LoadAll().Single(f => f.Id == "flop-set-in-position");
    var profile = StrategyProfileFixture.LoadOrCreate();
    var engine = GameCoordinatorFixtureBuilder.CreateEngine(profile);
    var calculo = PostflopDecisionRunner.CalculateOnly(
        fixture.State, fixture.MaxBet, fixture.MonteCarloSeed);

    var desdeEngine = engine.Decide(new PostflopEngineRequest(
        fixture.State, calculo, BoardPosition.Flop, PostflopGameContext.Empty));
    var desdeCoordinador = PostflopDecisionRunner.Run(
        profile, fixture.State, fixture.MaxBet, fixture.MonteCarloSeed);

    Assert.That(desdeEngine.Action, Is.EqualTo(desdeCoordinador.Action));
}
```

`CalculateOnly` se agrega a `PostflopDecisionRunner` en este step: mismo `Calculate` que ya usa `RunStreet`, sin coordinador.

- [ ] **Step 3: Correr y ver el fallo**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopDecisionEngineTests"`
Expected: FAIL de compilación — `PostflopDecisionEngine` no existe.

- [ ] **Step 4: Extraer el cuerpo de `DetermineFlopAction` al engine**

Mover al engine, sin reescribir condiciones:
- construcción del `PostflopDecisionInput` (hoy `GameCoordinator.cs:374-401`);
- cálculo de `effectiveEquity` con `ClampEquity`;
- `PreflopAnalyzer.CalculateCbetAdjustment`, `DetectDonkBet` y `AnalyzeBoardChange`.

El engine **no** actualiza `_contextHolder`, **no** llama a `_gameLoggerService`, **no** registra exploitability y **no** trackea villanos: eso queda en el coordinador.

- [ ] **Step 5: Hacer que el coordinador delegue**

`DetermineFlopAction` arma el `PostflopEngineRequest`, llama `_engine.Decide(...)`, y conserva el resto: contexto, logging, exploitability, tracking y el `LogText`. El `GameDecisionResult` se arma desde el resultado del engine.

- [ ] **Step 6: Verificar paridad postflop**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopFixtureReplayTests"`
Expected: 1/1 pasa.

Run: `dotnet test OpenScrape.sln`
Expected: 1330 pasando.

- [ ] **Step 7: Repetir para turn y river**

Mismos pasos para `DetermineTurnAction` y `DetermineRiverAction`. Cada uno con su verificación de paridad. Si turn y river comparten helpers con flop, se mueven una sola vez.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(decisionmaker): extraer el motor postflop del coordinador"
```

---

### Task 5: `IDecisionEngine` como puerta única local

**Files:**
- Create: `src/OpenScrape.DecisionMaker/IDecisionEngine.cs`
- Create: `src/OpenScrape.DecisionMaker/LocalDecisionEngine.cs`
- Modify: `src/OpenScrape.App/Program.cs`
- Test: `test/OpenScrape.App.Tests/LocalDecisionEngineTests.cs`

**Interfaces:**
- Consumes: `SetPreflopActionUseCase`, `PostflopDecisionEngine`.
- Produces:
  - `public interface IDecisionEngine { Task<DecisionResult> DecideAsync(DecisionRequest request, CancellationToken cancellationToken = default); }`
  - `DecisionRequest` con `Street`, `PlayerGameState`, y el `PostflopGameContext` serializable.
  - `DecisionResult` con `Action`, `Equity`, `Reason`, `BoardTexture`, flags y `LogText`.

- [ ] **Step 1: Escribir el test de la puerta**

```csharp
[Test]
public async Task DecideAsync_PreflopYDosTramos_RespetaElNumeroAleatorio()
{
    var engine = LocalDecisionEngineFixture.Create(randomNumber: 30);

    var result = await engine.DecideAsync(LocalDecisionEngineFixture.PreflopRequest());

    Assert.That(result.Action, Is.EqualTo("Raise"));
}
```

- [ ] **Step 2: Correr y ver el fallo**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~LocalDecisionEngineTests"`
Expected: FAIL — `IDecisionEngine` no existe.

- [ ] **Step 3: Implementar el contrato y el engine local**

`LocalDecisionEngine` despacha por `request.Street`: `Preflop` → `SetPreflopActionUseCase`; `Flop`/`Turn`/`River` → `PostflopDecisionEngine`.

- [ ] **Step 4: Registrar y consumir**

`Program.cs`: `services.AddSingleton<IDecisionEngine, LocalDecisionEngine>();`.
`GameCoordinator` y `SetPreflopActionUseCase` **no** se cambian en esta task: la puerta existe y está probada; el cableado del cliente a la puerta es la fase 3.

- [ ] **Step 5: Verificar**

Run: `dotnet test OpenScrape.sln`
Expected: 1331 pasando.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(decisionmaker): exponer una puerta unica para el motor"
```

---

### Task 6: Borrar la facade muerta y cerrar el gate

**Files:**
- Delete: `src/OpenScrape.App/Services/PokerDecisionFacade.cs`
- Delete: `src/OpenScrape.App/Services/IPokerDecisionFacade.cs`
- Modify: `src/OpenScrape.App/Program.cs` (quitar el registro)
- Modify: `test/OpenScrape.App.Tests/PokerDecisionFacadeTests.cs` (borrar si sólo prueba la facade)

**Interfaces:** ninguna nueva.

- [ ] **Step 1: Confirmar que nadie la usa**

Run: `Select-String -Path src\**\*.cs,test\**\*.cs -Pattern 'IPokerDecisionFacade|PokerDecisionFacade' | Select-Object -ExpandProperty Path | Sort-Object -Unique`

Expected: sólo la definición, el registro en `Program.cs` y su test.

- [ ] **Step 2: Borrar**

```bash
git rm src/OpenScrape.App/Services/PokerDecisionFacade.cs
git rm src/OpenScrape.App/Services/IPokerDecisionFacade.cs
git rm test/OpenScrape.App.Tests/PokerDecisionFacadeTests.cs
```

Quitar el registro de `Program.cs`.

- [ ] **Step 3: Compilar**

Run: `dotnet build OpenScrape.sln`
Expected: 0 errores.

- [ ] **Step 4: Gate completo**

Run: `.\scripts\verify-pre-merge.ps1 -SkipSmoke`
Expected: todos en PASS, `Fixtures de paridad` con `preflop: 8 | postflop: 7`, `ALL CHECKPOINTS PASSED`.

- [ ] **Step 5: Verificar que la lógica de decisión no quedó en App**

Run: `Select-String -Path src\OpenScrape.App\**\*.cs -Pattern 'PostflopDecisionInput|GetRandomAction|CalculateCbetAdjustment'`

Expected: sin resultados. Si aparece alguno, esa lógica no se movió.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "chore(services): eliminar la facade de decision sin uso"
```
