# API del motor de decisión — Fase 1: caracterización · Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Congelar el comportamiento actual de las decisiones preflop y postflop en fixtures reproducibles, antes de mover una sola línea del motor.

**Architecture:** Se introducen dos costuras mínimas para poder instanciar el motor sin Postgres (`IPreflopRangeStore` e `IGameLoggerService`). Con esas costuras, dos generadores explícitos producen fixtures desde el código actual y un replay en la suite normal los compara. Los fixtures pasan a ser el contrato de paridad de las fases 2, 3 y 4.

**Tech Stack:** .NET 10, NUnit 4.3.2, `System.Text.Json`, Marten (sólo en el generador preflop).

**Spec:** `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md`

## Global Constraints

- Target `net10.0`; `OpenScrape.App` sigue siendo `net10.0-windows`.
- Comentarios, nombres de tests y documentación en castellano.
- Nombres de tests con underscores y estilo del repo (`Metodo_Escenario_Resultado`).
- `Assert.That` / `Assert.Multiple`; sin librerías de mocks: dobles escritos a mano.
- 0 advertencias en Release con `-p:TreatWarningsAsErrors=true`.
- `dotnet format OpenScrape.sln --verify-no-changes` sin cambios.
- Ningún commit con atribución de IA ni `Co-Authored-By`.
- Los generadores de fixtures se marcan `[Explicit]`: no corren en CI.
- El replay corre en la suite normal y no toca Postgres.
- Al terminar: `.\scripts\verify-pre-merge.ps1 -SkipSmoke` en PASS.

## Review Focus

Estos son los modos de falla que los tests de las tasks no ejercitan y que más probablemente rompan la paridad:

1. **RNG sin fijar.** La equity postflop sale de Monte Carlo y la acción preflop de una mezcla ponderada. Sin semilla y sin número aleatorio fijos, el fixture cambia en cada corrida y la paridad es una ilusión.
2. **Tablas que no suman 100.** `GetActionScenario.GetRandomAction` lanza si los porcentajes de una mano no suman 100. Un fixture generado contra una tabla incompleta falla en el replay, no en la generación.
3. **Cultura decimal.** Guardar `decimal`/`double` con la cultura del sistema rompe en otra máquina (`,` contra `.`). Serializar siempre con `InvariantCulture`.
4. **Estado compartido del coordinator.** El `GameCoordinator` guarda resultados por calle y contexto cross-street. Reusar la misma instancia entre fixtures contamina la decisión siguiente.
5. **Datos personales en fixtures.** Los alias de jugadores no deben quedar commiteados. Usar `P0..P5` en los estados de entrada.

---

### Task 1: Costuras de testeo

**Files:**
- Create: `src/OpenScrape.Domain/Interfaces/IPreflopRangeStore.cs`
- Create: `src/OpenScrape.Features/ActionScenario/MartenPreflopRangeStore.cs`
- Create: `src/OpenScrape.App/Services/IGameLoggerService.cs`
- Modify: `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs:11-18`
- Modify: `src/OpenScrape.Features/ActionScenario/ActionScenarioUseCases.cs`
- Modify: `src/OpenScrape.App/Services/GameCoordinator.cs:26,36-56`
- Modify: `src/OpenScrape.App/Services/GameLoggerService.cs:1` (implementa la interfaz)
- Modify: `src/OpenScrape.App/Program.cs` (registro del store y de la interfaz)
- Test: `test/OpenScrape.App.Tests/PreflopRangeStoreTests.cs`

**Interfaces:**
- Consumes: `GetTable.ExecuteAsync(string)` → `Result<TableDTO?>`; `GameLoggerService` actual.
- Produces:
  - `public interface IPreflopRangeStore { Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default); }`
  - `public interface IGameLoggerService` con la superficie pública que usa `GameCoordinator`.
  - `GetActionScenario(IPreflopRangeStore rangeStore, IRandomProvider? randomProvider = null)`.

- [ ] **Step 1: Escribir el test que fija la costura**

```csharp
// test/OpenScrape.App.Tests/PreflopRangeStoreTests.cs
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Enums;
using OpenScrape.Domain.Interfaces;
using OpenScrape.Features.ActionScenario;
using OpenScrape.Features.ActionScenario.Get;

[TestFixture]
public class PreflopRangeStoreTests
{
    private sealed class FakeRangeStore : IPreflopRangeStore
    {
        private readonly TableDTO? _table;
        public string? LastSituation { get; private set; }

        public FakeRangeStore(TableDTO? table) => _table = table;

        public Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default)
        {
            LastSituation = situation;
            return Task.FromResult(_table);
        }
    }

    [Test]
    public async Task ExecuteAsync_ConTablaEnElStore_DevuelveLaAccionDeLaTabla()
    {
        var table = new TableDTO
        {
            Positions =
            [
                new PlayerActionSequence
                {
                    HeroPosition = "Button",
                    Hands = [new Hand("AK", true, "Raise", 100)]
                }
            ]
        };
        var store = new FakeRangeStore(table);
        var sut = new GetActionScenario(store);

        var action = await sut.ExecuteAsync(
            GameSituation.OpenRaise,
            new ActionScenarioRequest { HeroPosition = TablePosition.Button, HandName = "AK", Suited = true });

        Assert.Multiple(() =>
        {
            Assert.That(action, Is.EqualTo("Raise"));
            Assert.That(store.LastSituation, Is.EqualTo(GameSituation.OpenRaise.GetDescription()));
        });
    }

    [Test]
    public void ExecuteAsync_SinTabla_Lanza()
    {
        var sut = new GetActionScenario(new FakeRangeStore(null));

        var ex = Assert.ThrowsAsync<Exception>(async () => await sut.ExecuteAsync(
            OpenScrape.Domain.Enums.GameSituation.OpenRaise,
            new ActionScenarioRequest()));

        Assert.That(ex!.Message, Does.Contain("Table not found"));
    }
}
```

- [ ] **Step 2: Correr el test y verificar que falla**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopRangeStoreTests"`
Expected: FAIL de compilación — `IPreflopRangeStore` no existe y `GetActionScenario` no tiene ese constructor.

- [ ] **Step 3: Crear `IPreflopRangeStore`**

```csharp
// src/OpenScrape.Domain/Interfaces/IPreflopRangeStore.cs
namespace OpenScrape.Domain.Interfaces;

using OpenScrape.Domain.Dtos;

/// <summary>Acceso a la tabla de rangos de una situación preflop.</summary>
public interface IPreflopRangeStore
{
    Task<TableDTO?> GetAsync(string situation, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Implementar el adaptador Marten y cambiar el constructor de `GetActionScenario`**

`MartenPreflopRangeStore(GetTable getTable)` devuelve `result.IsSuccess ? result.Value : null`.
`GetActionScenario` reemplaza `TableUseCases tableUseCases` por `IPreflopRangeStore rangeStore` y su `ExecuteAsync` llama `await rangeStore.GetAsync(situation.GetDescription())`. La lógica de `SelectMatchingHands` y `GetRandomAction` no se toca.

- [ ] **Step 5: Extraer `IGameLoggerService`**

`GameCoordinator` pasa a depender de `IGameLoggerService`; `GameLoggerService` la implementa sin cambios de comportamiento. La interfaz declara sólo los métodos que `GameCoordinator` invoca (`UpdateBoard`, `LogStreetDecision`, `UpdateSituation`, y los que aparezcan al compilar).

- [ ] **Step 6: Registrar en DI**

```csharp
services.AddScoped<GetTable>();
services.AddScoped<IPreflopRangeStore, MartenPreflopRangeStore>();
services.AddScoped<GameLoggerService>();
services.AddScoped<IGameLoggerService>(sp => sp.GetRequiredService<GameLoggerService>());
```

- [ ] **Step 7: Verificar GREEN y la suite completa**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopRangeStoreTests"`
Expected: 2/2 pasan.

Run: `dotnet test OpenScrape.sln`
Expected: 1322 pasando, 0 fallos.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(features): exponer la tabla de rangos y el logger como interfaces"
```

---

### Task 2: Builder y fixtures postflop

**Files:**
- Create: `test/OpenScrape.App.Tests/Fixtures/GameCoordinatorFixtureBuilder.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PostflopFixture.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PostflopFixtureGeneratorTests.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PostflopFixtureReplayTests.cs`
- Create: `test/fixtures/decisions/postflop/*.json` (generados, luego commiteados)

**Interfaces:**
- Consumes: `GameCoordinator` con `IGameLoggerService`; `UnifiedPokerCalculator`; `MonteCarloSimulator(int sessionSeed)`.
- Produces:
  - `internal sealed class GameCoordinatorFixtureBuilder` con `Create(int monteCarloSeed)` y `CreateCalculator(int monteCarloSeed)`.
  - `internal sealed record PostflopFixture(string Id, string Street, PlayerGameState State, decimal MaxBet, string ExpectedAction, double ExpectedEquity, int MonteCarloSeed)`.
  - `internal static class PostflopFixtureStore` con `Save`/`LoadAll` sobre `test/fixtures/decisions/postflop`.

- [ ] **Step 1: Escribir el replay (falla porque no hay fixtures)**

```csharp
[TestFixture]
public class PostflopFixtureReplayTests
{
    [Test]
    public void Replay_CadaFixture_CoincideConLaAccionEsperada()
    {
        var fixtures = PostflopFixtureStore.LoadAll();
        Assert.That(fixtures, Is.Not.Empty, "Faltan fixtures postflop; correr el generador [Explicit].");

        Assert.Multiple(() =>
        {
            foreach (var fixture in fixtures)
            {
                var state = fixture.State;
                var coordinator = GameCoordinatorFixtureBuilder.Create(fixture.MonteCarloSeed);
                var calculator = GameCoordinatorFixtureBuilder.CreateCalculator(fixture.MonteCarloSeed);
                var myCards = new List<CardDataOuts>
                {
                    new((Suit)state.HoleCard1Suit, (Rank)state.HoleCard1Rank),
                    new((Suit)state.HoleCard2Suit, (Rank)state.HoleCard2Rank)
                };
                var communityCards = state.BoardCards
                    .Where(b => b.Position != BoardPosition.Hand)
                    .OrderBy(b => b.Location)
                    .Select(b => new CardDataOuts((Suit)b.Suit, (Rank)b.Force))
                    .ToList();

                var result = calculator.Calculate(
                    myCards,
                    communityCards,
                    state.PotSize,
                    fixture.MaxBet,
                    isInPosition: state.IsInPosition,
                    heroStack: state.HeroStack,
                    villainStack: coordinator.GetVillainStack(state),
                    handSituation: state.HandSituation.ToString(),
                    opponentProfile: null);

                coordinator.SetFlopResult(result);
                var decision = coordinator.DetermineFlopAction(state);

                Assert.That(decision.Action, Is.EqualTo(fixture.ExpectedAction), fixture.Id);
                Assert.That(result.EquityPercentage, Is.EqualTo(fixture.ExpectedEquity).Within(0.1), fixture.Id);
            }
        });
    }
}
```

- [ ] **Step 2: Correr el replay y verificar el fallo esperado**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopFixtureReplayTests"`
Expected: FAIL — `PostflopFixtureStore` no existe.

- [ ] **Step 3: Implementar el builder**

Construye las implementaciones reales con `Options.Create(new StrategyProfile())`:
`MonteCarloSimulator(seed)`, `BitHandEvaluator`, `OutsCalculator`, `PreflopEquityCalculator`,
`BoardTextureAnalyzer`, `BetSizingService`, `RangePolarizer`, `ThresholdsRegistry`,
`PostflopDecisionService(..., new FixedRandomProvider(1))`, `OpponentTracker(null)`,
`ExploitabilityCalculator()`, `GameLoopStateMachine`, `IGameLoggerService` falso,
`StrategyProfileService`, `PostflopContextHolder`.
Cada llamada a `Create` devuelve una instancia nueva: nunca se reusa entre fixtures.

- [ ] **Step 4: Implementar el generador `[Explicit]` con seis escenarios**

Escenarios mínimos: flop con set en posición, flop con flush draw fuera de posición,
flop con aire y villano agresor, turn con top pair, river con bluff catcher, river con
segunda pareja. Cada escenario usa alias `P0..P5`. Escribe un JSON por escenario.

- [ ] **Step 5: Generar los fixtures y verificar el replay**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopFixtureGeneratorTests" -- NUnit.ExplicitMode=Include`
Expected: 6 JSON creados en `test/fixtures/decisions/postflop/`.

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopFixtureReplayTests"`
Expected: 1/1 pasa.

- [ ] **Step 6: Probar que el fixture detecta un cambio**

Cambiar temporalmente una comparación a un valor distinto, correr el replay y confirmar que falla.
Revertir. Un fixture que no puede fallar no prueba nada.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "test(fixtures): congelar la decision postflop actual"
```

---

### Task 3: Fixtures preflop y snapshot de tablas

**Files:**
- Create: `test/OpenScrape.App.Tests/Fixtures/PreflopFixture.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PreflopFixtureStore.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PreflopTestCaseBuilder.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PreflopFixtureGeneratorTests.cs`
- Create: `test/OpenScrape.App.Tests/Fixtures/PreflopFixtureReplayTests.cs`
- Create: `test/fixtures/decisions/preflop/*.json` (generados, luego commiteados)

**Interfaces:**
- Consumes: `SetPreflopActionUseCase`, los diez use cases, `GetActionScenario` con `IPreflopRangeStore`, `TableDTO`.
- Produces:
  - `internal sealed record PreflopFixture(string Id, SetPreflopActionUseCaseRequest Request, TableDTO Table, int RandomNumber, string ExpectedAction)`.
  - `internal sealed class FixedRandomProvider(int value) : OpenScrape.Domain.Interfaces.IRandomProvider`.
  - `internal sealed class FixedRangeStore(TableDTO table) : IPreflopRangeStore`.

- [ ] **Step 1: Escribir el replay**

```csharp
[Test]
public async Task Replay_CadaFixture_DevuelveLaAccionEsperada()
{
    Assert.Multiple(() =>
    {
        foreach (var fixture in PreflopFixtureStore.LoadAll())
        {
            var store = new FixedRangeStore(fixture.Table);
            var random = new FixedRandomProvider(fixture.RandomNumber);
            var sut = PreflopTestCaseBuilder.Create(store, random);

            var response = sut.Execute(fixture.Request).GetAwaiter().GetResult();

            Assert.That(response.ResponseAction.Action, Is.EqualTo(fixture.ExpectedAction), fixture.Id);
        }
    });
}
```

- [ ] **Step 2: Correr y verificar el fallo esperado**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopFixtureReplayTests"`
Expected: FAIL — `PreflopFixtureStore` no existe.

- [ ] **Step 3: Implementar el builder y el generador `[Explicit]`**

Escenarios mínimos: open raise con `AKs`, open raise con `72o`, raise over limper,
3-bet con `QQ`, cold 4-bet con `AKo`, squeeze con `TT`, y una segunda acción
(`IsSecondAction = true`). El generador corre contra Marten, obtiene el `TableDTO`
usado y lo guarda dentro del fixture: el replay no vuelve a tocar la base.

- [ ] **Step 4: Generar y verificar el replay**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopFixtureGeneratorTests" -- NUnit.ExplicitMode=Include`
Expected: 7 JSON en `test/fixtures/decisions/preflop/`, sin excepción de porcentajes.

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PreflopFixtureReplayTests"`
Expected: 1/1 pasa.

- [ ] **Step 5: Verificar que el replay detecta un cambio de RNG**

Cambiar el `RandomNumber` de un fixture a otro valor que caiga en una acción distinta,
correr el replay y confirmar que falla. Revertir.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "test(fixtures): congelar la decision preflop actual"
```

---

### Task 4: Gate de paridad en la suite

**Files:**
- Modify: `test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj` (copiar `test/fixtures/**` al output)
- Modify: `scripts/verify-pre-merge.ps1` (checkpoint informativo con el conteo de fixtures)

**Interfaces:**
- Consumes: los dos replays de las tasks 2 y 3.
- Produces: un checkpoint que falla si un fixture desaparece o no coincide.

- [ ] **Step 1: Copiar los fixtures al output de los tests**

```xml
<ItemGroup>
  <None Include="..\..\test\fixtures\**\*.json" LinkBase="fixtures" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

- [ ] **Step 2: Correr la suite completa**

Run: `dotnet test OpenScrape.sln`
Expected: 1322 + 2 tests de replay = 1324 pasando, 0 fallos.

- [ ] **Step 3: Agregar el checkpoint informativo**

`verify-pre-merge.ps1` imprime cuántos fixtures preflop y postflop encontró y falla si el
conteo es cero; si quedan menos de los generados, lo reporta como `INFO`.

- [ ] **Step 4: Correr el gate completo**

Run: `.\scripts\verify-pre-merge.ps1 -SkipSmoke`
Expected: todos los checkpoints en PASS, `csproj diff` en INFO, `ALL CHECKPOINTS PASSED`.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "ci(tests): bloquear el merge si la paridad de fixtures falla"
```
