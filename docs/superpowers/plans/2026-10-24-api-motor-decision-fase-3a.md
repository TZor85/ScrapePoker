# API del motor de decisión — Fase 3a (paridad por HTTP) — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Servir el motor de decisión por HTTP (`POST /v1/decisions`, `GET /health`) con un contrato que sólo lleva hechos, y probar paridad exacta contra los 15 fixtures existentes, sin licencias todavía.

**Architecture:** Un proyecto `OpenScrape.Contracts` (`net10.0`, sólo `Domain`) define el `DecisionRequest` crudo y el `DecisionResult` derivado. `OpenScrape.DecisionMaker` consume `Contracts` y traduce crudo→interno con dos factories que encapsulan `UnifiedPokerCalculator`, `BoardTextureAnalyzer` y `PreflopAnalyzer`. `OpenScrape.Api` (Web SDK) expone el motor. `OpenScrape.App` arma el request crudo con `PlayerStateMapper`, elige motor local o HTTP con `RemoteEngine:Enabled` y sigue haciendo logging, tracking, contexto y exploitability con el resultado.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, `System.Text.Json`, NUnit, `WebApplicationFactory`, Marten 9.51 (rangos preflop), `Microsoft.Extensions.Options`.

**Spec:** `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md`

## Global Constraints

- `Contracts`, `DecisionMaker` y `Api` son `net10.0`. `App` sigue `net10.0-windows`. No se retargetea nada.
- `OpenScrape.Contracts` no puede referenciar Marten, ASP.NET, `Features`, `Infrastructure` ni `App`.
- `OpenScrape.App` **sí** puede referenciar `DecisionMaker` durante 3a; la prohibición aplica al terminar la fase 4.
- `RemoteEngine:Enabled` arranca en `false`. Con el flag en `false` el comportamiento es el actual, byte a byte.
- Timeout del cliente HTTP: **1,5 s**. Sin reintentos.
- Base de rutas: `/v1`. Un cambio incompatible crea `/v2`, nunca modifica `/v1`.
- Enums en JSON como `camelCase` string; `Rank`/`Suit` de las cartas como `int` (igual que `PlayerGameState`).
- Comentarios, tests, documentación y mensajes de commit en castellano.
- Sin atribución de IA en commits.
- 0 advertencias en Release: `dotnet build OpenScrape.sln --configuration Release -p:TreatWarningsAsErrors=true`.
- `dotnet format --verify-no-changes` limpio en cada task.
- Cada task cierra con `dotnet test OpenScrape.sln` verde.

## Review Focus

1. **Board incompleto para la calle:** un request `Flop` con 0-2 cartas de board, o `Turn` con 3, devuelve **422** con código `invalid_request`; nunca una decisión.
2. **Cartas duplicadas** entre mano y board devuelven **422** `invalid_request`; el motor no evalúa una mano imposible.
3. **API caída con `RemoteEngine:Enabled=true`:** el loop se detiene y avisa; no cae al motor local ni emite una decisión con equity 0.
4. **`HandSituation` sin escenario en la tabla de rangos:** respuesta explícita (422 `situation_not_found`), no un 500 ni una acción inventada.
5. **Timeout de 1,5 s por Monte Carlo pesado:** se lanza `EngineUnavailableException`; no se devuelve una decisión parcial.

---

## Mapa de archivos

```
src/OpenScrape.Contracts/                 NUEVO (net10.0, ref Domain)
  OpenScrape.Contracts.csproj
  DecisionStreet.cs
  CardRef.cs
  VillainProfile.cs
  DecisionContext.cs
  DecisionRequest.cs
  DecisionResult.cs
  BetSizeCategory.cs
  EngineError.cs

src/OpenScrape.DecisionMaker/
  IDecisionEngine.cs                      MODIFICA: usa los tipos de Contracts
  LocalDecisionEngine.cs                  MODIFICA: usa las factories
  DecisionMakerServiceCollectionExtensions.cs   NUEVO
  DTOs/DecisionRequest.cs                 BORRA (muerto)
  DTOs/DecisionResult.cs                  BORRA (muerto)
  Preflop/PreflopRequestFactory.cs        NUEVO
  Preflop/PreflopSituationNotFoundException.cs   NUEVO
  Postflop/PostflopRequestFactory.cs      NUEVO
  Services/PostflopDecisionService.cs     MODIFICA: pierde la declaración de BetSizeCategory
  OpenScrape.DecisionMaker.csproj         MODIFICA: referencia Contracts

src/OpenScrape.Api/                       NUEVO (Microsoft.NET.Sdk.Web)
  OpenScrape.Api.csproj
  Program.cs
  ApiMarker.cs
  appsettings.json
  Endpoints/DecisionEndpoints.cs
  Endpoints/RequestValidator.cs
  StrategyProfileConfig.cs
  HealthEndpoint.cs (inline en Program.cs)

src/OpenScrape.App/
  Services/PlayerStateMapper.cs           NUEVO
  Services/Http/HttpDecisionEngine.cs     NUEVO
  Services/Http/EngineUnavailableException.cs   NUEVO
  Services/GameCoordinator.cs             MODIFICA: delega en IDecisionEngine
  Services/HandProcessor.cs               MODIFICA: preflop por IDecisionEngine
  Forms/FrmMain.cs                        MODIFICA: preflop por IDecisionEngine
  Program.cs                              MODIFICA: usa AddDecisionMaker + selector de motor
  appsettings.json                        MODIFICA: RemoteEngine

test/OpenScrape.App.Tests/
  Contracts/DecisionContractSerializationTests.cs   NUEVO
  Api/ApiFactory.cs                                 NUEVO (helper)
  Api/JsonConfig.cs                                 NUEVO (opciones JSON compartidas)
  Api/FixtureTables.cs                              NUEVO (helpers de tablas y requests)
  Api/DecisionEndpointTests.cs                      NUEVO
  Api/StrategyProfileDriftTests.cs                  NUEVO
  Api/ParidadHttpTests.cs                           NUEVO (15 fixtures)
  Services/PlayerStateMapperTests.cs                NUEVO
  Services/HttpDecisionEngineTests.cs               NUEVO
  Fixtures/GameCoordinatorFixtureBuilder.cs         MODIFICA: motor inyectable
  LocalDecisionEngineTests.cs                       MODIFICA: nuevo contrato

scripts/verify-pre-merge.ps1              MODIFICA: agregar chequeo de arquitectura
```

---

### Task 1: Proyecto `Contracts` con el contrato crudo

**Files:**
- Create: `src/OpenScrape.Contracts/OpenScrape.Contracts.csproj`
- Create: `src/OpenScrape.Contracts/DecisionStreet.cs`, `CardRef.cs`, `VillainProfile.cs`, `DecisionContext.cs`, `DecisionRequest.cs`, `DecisionResult.cs`, `BetSizeCategory.cs`, `EngineError.cs`
- Modify: `OpenScrape.sln` (alta del proyecto)
- Test: `test/OpenScrape.App.Tests/Contracts/DecisionContractSerializationTests.cs`

**Interfaces:**
- Consumes: `OpenScrape.Domain.Enums.TablePosition`, `HandSituation`; `OpenScrape.Domain.Entities.OpponentType`.
- Produces:
  - `enum DecisionStreet { Preflop = 0, Flop = 1, Turn = 2, River = 3 }`
  - `readonly record struct CardRef(int Rank, int Suit)`
  - `enum BetSizeCategory { NoBet, Underbet, Small, Medium, Large }` (se muda desde `PostflopDecisionService.cs`, mismos miembros y mismo orden)
  - `sealed record VillainProfile { string VillainId = "Unknown"; OpponentType Type; double FoldToBetPct = -1; double AdjustedFoldEquity = -1; int HandsPlayed; int TimesVoluntarilyPutMoneyIn; }`
  - `sealed record DecisionContext { bool PreviousStreetBet; bool VillainAggressorCheckedPreviousStreet; bool VillainBarreling; bool HeroFloatedFlop; BetSizeCategory VillainBetSizeFlop; BetSizeCategory VillainBetSizeTurn; bool VillainCheckedMiddleStreet; bool TurnCalledWithFlushDanger; bool HeroCheckedAllStreets; bool IsAnyoneAllIn; bool IsDonkBet; }`
  - `sealed record DecisionRequest` con `required DecisionStreet Street`, `required CardRef HeroCard1`, `required CardRef HeroCard2`, `IReadOnlyList<CardRef> Board = []`, `decimal PotSize`, `decimal BetToCall`, `decimal HeroStack`, `decimal VillainStack`, `bool IsInPosition`, `int NumOpponents = 1`, `TablePosition HeroPosition`, `TablePosition VillainPosition = None`, `HandSituation HandSituation`, `bool IsSecondAction`, `bool HeroWasPreflopAggressor`, `bool IsHeroAggressor`, `VillainProfile? VillainProfile`, `DecisionContext Context = new()`, `int? MonteCarloIterations`
  - `sealed record DecisionResult` con `required string Action`, `double Equity`, `double PotOdds`, `double? BetSize`, `string? Reason`, `string? BoardTexture`, `bool IsBluff`, `bool IsBarrel`, `bool IsCheckRaise`, `bool IsFloating`, `double FoldEquity`, `int TotalOuts`, `double EffectiveOuts`, `IReadOnlyList<string> DrawTypes = []`, `bool HasComboDraw`, `string? HeroHandRank`, `string? PairClassification`, `string? KickerStrength`
  - `sealed record EngineError(string Code, string Message)`

- [ ] **Step 1: Crear el proyecto y darlo de alta**

```powershell
dotnet new classlib -n OpenScrape.Contracts -o src/OpenScrape.Contracts -f net10.0
Remove-Item src/OpenScrape.Contracts/Class1.cs
dotnet sln OpenScrape.sln add src/OpenScrape.Contracts/OpenScrape.Contracts.csproj
dotnet add src/OpenScrape.Contracts/OpenScrape.Contracts.csproj reference src/OpenScrape.Domain/OpenScrape.Domain.csproj
```

- [ ] **Step 2: Escribir el test de serialización (rojo)**

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenScrape.Contracts;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Contracts;

[TestFixture]
public class DecisionContractSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Test]
    public void Serialize_RequestPreflop_UsaPropiedadesCamelCaseYEnumsComoTexto()
    {
        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            PotSize = 3m,
            HeroStack = 100m,
            HeroPosition = TablePosition.Button,
            HandSituation = HandSituation.None
        };

        var json = JsonSerializer.Serialize(request, Options);

        Assert.That(json, Does.Contain("\"street\":\"preflop\""));
        Assert.That(json, Does.Contain("\"handSituation\":\"none\""));
        Assert.That(json, Does.Contain("\"heroCard1\":{\"rank\":"));
    }

    [Test]
    public void RoundTrip_ResultConDetalle_ConservaTodasLasPropiedades()
    {
        var original = new DecisionResult
        {
            Action = "Bet 33%",
            Equity = 54.2,
            PotOdds = 25.0,
            BetSize = 12.5,
            Reason = "top pair",
            BoardTexture = "Dry",
            IsBluff = false,
            IsBarrel = true,
            DrawTypes = ["Flush Draw"],
            EffectiveOuts = 9,
            HeroHandRank = "OnePair",
            KickerStrength = "Strong",
            PairClassification = "TopPair"
        };

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<DecisionResult>(json, Options);

        Assert.That(restored!.Action, Is.EqualTo(original.Action));
        Assert.That(restored.Equity, Is.EqualTo(original.Equity));
        Assert.That(restored.BetSize, Is.EqualTo(original.BetSize));
        Assert.That(restored.DrawTypes, Is.EquivalentTo(original.DrawTypes));
        Assert.That(restored.HeroHandRank, Is.EqualTo(original.HeroHandRank));
    }

    [Test]
    public void Deserialize_RequestSinBoard_DejaListaVacia()
    {
        const string json = """
        { "street": "flop", "heroCard1": {"rank": 14, "suit": 3},
          "heroCard2": {"rank": 13, "suit": 3}, "potSize": 6, "heroStack": 94 }
        """;

        var request = JsonSerializer.Deserialize<DecisionRequest>(json, Options);

        Assert.That(request!.Board, Is.Empty);
        Assert.That(request.Context, Is.Not.Null);
    }
}
```

- [ ] **Step 3: Correr y verificar que falla**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~DecisionContractSerializationTests"`
Expected: FAIL de compilación con `CS0246: DecisionRequest no se encontró`.

- [ ] **Step 4: Crear los ocho archivos**

`DecisionStreet.cs`, `CardRef.cs`, `VillainProfile.cs`, `DecisionContext.cs`, `DecisionRequest.cs`, `DecisionResult.cs`, `BetSizeCategory.cs`, `EngineError.cs`, todos en `namespace OpenScrape.Contracts;`. `BetSizeCategory` se copia del que hoy vive en `Services/PostflopDecisionService.cs` con los mismos cinco miembros y el mismo orden. Cada tipo lleva un comentario de una línea explicando si es un hecho (lo manda el cliente) o un derivado (lo calcula el servidor).

- [ ] **Step 5: Correr y verificar verde**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~DecisionContractSerializationTests"`
Expected: PASS, 3/3.

- [ ] **Step 6: Verificar el aislamiento del proyecto**

Run: `Select-String -Path src/OpenScrape.Contracts/OpenScrape.Contracts.csproj -Pattern 'Marten|AspNetCore|ProjectReference'`
Expected: la única línea `ProjectReference` es `OpenScrape.Domain.csproj`; sin Marten ni AspNetCore.

- [ ] **Step 7: Commit**

```bash
git add src/OpenScrape.Contracts OpenScrape.sln test/OpenScrape.App.Tests/Contracts
git commit -m "feat(contracts): definir el contrato crudo de decisiones"
```

---

### Task 2: `DecisionMaker` consume `Contracts` y traduce crudo→interno

**Files:**
- Modify: `src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj`
- Modify: `src/OpenScrape.DecisionMaker/IDecisionEngine.cs` (queda sólo la interfaz)
- Modify: `src/OpenScrape.DecisionMaker/LocalDecisionEngine.cs`
- Modify: `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` (borra `BetSizeCategory`)
- Delete: `src/OpenScrape.DecisionMaker/DTOs/DecisionRequest.cs`, `src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs`
- Create: `src/OpenScrape.DecisionMaker/Preflop/PreflopRequestFactory.cs`
- Create: `src/OpenScrape.DecisionMaker/Postflop/PostflopRequestFactory.cs`
- Create: `src/OpenScrape.DecisionMaker/DecisionMakerServiceCollectionExtensions.cs`
- Modify: `src/OpenScrape.App/Services/GameLoopResult.cs:25` (comentario `cref`)
- Modify: `test/OpenScrape.App.Tests/LocalDecisionEngineTests.cs`
- Test: `test/OpenScrape.App.Tests/Contract/RequestFactoryTests.cs`

**Interfaces:**
- Consumes: `Contracts.DecisionRequest`/`DecisionResult`/`CardRef`/`BetSizeCategory`/`DecisionStreet`; `UnifiedPokerCalculator.Calculate(...)`; `IBoardTextureAnalyzer.Analyze(List<CardDataOuts>)`; `IPreflopAnalyzer.CalculateCbetAdjustment(...)`/`DetectDonkBet(...)`/`CategorizeOpponentBet(...)`; `SetPreflopActionUseCase.Execute(SetPreflopActionUseCaseRequest)`.
- Produces:
  - `IDecisionEngine` con `Task<Contracts.DecisionResult> DecideAsync(Contracts.DecisionRequest request, CancellationToken cancellationToken = default)`
  - `PreflopRequestFactory.Create(Contracts.DecisionRequest, decimal potSize) -> SetPreflopActionUseCaseRequest`
  - `PostflopRequestFactory.Create(Contracts.DecisionRequest) -> PostflopEngineRequest`
  - `IServiceCollection AddDecisionMaker(this IServiceCollection services)`

**Notas de implementación obligatorias:**
- Antes de escribir las factories, leer `test/OpenScrape.App.Tests/Fixtures/PreflopTestCaseBuilder.cs` y `test/fixtures/decisions/preflop/open-raise-aks.json` para replicar el armado de `Players`, `ResponseAction` y `PlayerGameState` que hoy usan los fixtures. No inventar campos.
- Para `PostflopRequestFactory`, la fuente es la composición que hoy vive en `GameCoordinator.DetermineFlopAction` (líneas 342-480), `DetermineTurnAction` (481-631) y `DetermineRiverAction` (632-...): **se copia tal cual**, cambiando sólo el origen de cada valor (ahora viene del `DecisionRequest` crudo o de `UnifiedPokerCalculator`, no del coordinador). El coordinador sigue intacto hasta la Task 5; la duplicación es deliberada y se elimina ahí.

- [ ] **Step 1: Escribir el test de la factory preflop (rojo)**

```csharp
using OpenScrape.Contracts;
using OpenScrape.DecisionMaker.Preflop;
using OpenScrape.Domain.Enums;

namespace OpenScrape.App.Tests.Contract;

[TestFixture]
public class RequestFactoryTests
{
    [Test]
    public void Create_PreflopAksEnButton_ArmaElEstadoConLasCartasYLaPosicion()
    {
        var request = new DecisionRequest
        {
            Street = DecisionStreet.Preflop,
            HeroCard1 = new CardRef((int)Rank.Ace, (int)Suit.Spades),
            HeroCard2 = new CardRef((int)Rank.King, (int)Suit.Spades),
            PotSize = 3m,
            BetToCall = 3m,
            HeroStack = 100m,
            VillainStack = 100m,
            HeroPosition = TablePosition.Button,
            VillainPosition = TablePosition.BigBlind,
            IsInPosition = true,
            HandSituation = HandSituation.None
        };

        var internalRequest = new PreflopRequestFactory().Create(request, request.PotSize);

        Assert.That(internalRequest.PlayerState.HoleCard1Rank, Is.EqualTo((int)Rank.Ace));
        Assert.That(internalRequest.PlayerState.HoleCard1Suit, Is.EqualTo((int)Suit.Spades));
        Assert.That(internalRequest.PlayerState.Position, Is.EqualTo(TablePosition.Button));
        Assert.That(internalRequest.PlayerState.Players, Has.Count.EqualTo(1));
        Assert.That(internalRequest.PlayerState.Players[0].Bet, Is.EqualTo(3m));
        Assert.That(internalRequest.ResponseAction.IsSecondAction, Is.False);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~RequestFactoryTests"`
Expected: FAIL con `CS0246: PreflopRequestFactory no se encontró`.

- [ ] **Step 3: Agregar la referencia y borrar los DTO muertos**

```powershell
dotnet add src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj reference src/OpenScrape.Contracts/OpenScrape.Contracts.csproj
git rm src/OpenScrape.DecisionMaker/DTOs/DecisionRequest.cs src/OpenScrape.DecisionMaker/DTOs/DecisionResult.cs
```

Agregar en `OpenScrape.DecisionMaker.csproj`: `<ItemGroup><Using Include="OpenScrape.Contracts" /></ItemGroup>`.
Quitar de `Services/PostflopDecisionService.cs` la línea `public enum BetSizeCategory { ... }`.
En `src/OpenScrape.App/Services/GameLoopResult.cs:25` cambiar `<see cref="DecisionResult.RecommendedAction"/>` por `` `<see cref="DecisionResult.Action"/>` `` y quitar el `using OpenScrape.DecisionMaker.DTOs;` de `GameLoopResult.cs`, `FrmMain.cs` y `GameCoordinator.cs`.

- [ ] **Step 4: Implementar `PreflopRequestFactory`**

`Create(DecisionRequest request, decimal potSize)` devuelve `SetPreflopActionUseCaseRequest` con:
- `PlayerState` = `new PlayerGameState { HoleCard1Rank = request.HeroCard1.Rank, HoleCard1Suit = request.HeroCard1.Suit, HoleCard2Rank = request.HeroCard2.Rank, HoleCard2Suit = request.HeroCard2.Suit, HoleCard1Face = ((Rank)request.HeroCard1.Rank).ToString(), HoleCard2Face = ((Rank)request.HeroCard2.Rank).ToString(), Position = request.HeroPosition, PotSize = potSize, CurrentBet = request.BetToCall, HeroStack = request.HeroStack, IsInPosition = request.IsInPosition, HandSituation = request.HandSituation, Players = [ ...villano... ] }`
- villano: `new Player { Name = "Villain", Active = true, Bet = request.BetToCall, Stack = request.VillainStack, Position = request.VillainPosition, WasPreflopAggressor = request.HeroWasPreflopAggressor is false }`
- `ResponseAction = new ResponseAction { HandSituation = request.HandSituation, IsSecondAction = request.IsSecondAction }`
- `PreflopHeroPosition = new()`

- [ ] **Step 5: Implementar `PostflopRequestFactory`**

`Create(DecisionRequest request)` calcula, en este orden:
1. `playerHand` = dos `CardDataOuts((Suit)rank, (Rank)suit)`; `communityCards` = `request.Board`.
2. `result = calculator.Calculate(playerHand, communityCards, request.PotSize, request.BetToCall, request.NumOpponents, request.MonteCarloIterations, request.IsInPosition, request.HeroStack, request.VillainStack, request.HandSituation.ToString(), request.VillainPosition, opponentProfile)`.
3. `texture = analyzer.Analyze(communityCards)`.
4. `boardChange` = `street == Flop ? analyzer.AnalyzeInitialBoard(communityCards) : analyzer.AnalyzeBoardChange(previousBoard, últimaCarta)`; `previousBoard` = `communityCards.Take(3)` para Turn y `Take(4)` para River.
5. `cbetAdjustment` = en Flop, `preflopAnalyzer.CalculateCbetAdjustment(request.HeroWasPreflopAggressor, hasRangeAdvantage, texture, request.IsInPosition, request.NumOpponents, profile)`, con `hasRangeAdvantage = preflopAnalyzer.HasRangeAdvantageOnBoard(flopRanks, texture, request.HeroWasPreflopAggressor, request.HandSituation)`; en Turn/River, `0`.
6. Devuelve `PostflopEngineRequest` con `State` armado como en la Task 2 Step 4 (más `BoardCards`), `Result = result`, `Street` mapeado, `Situation`, `BoardTexture = texture.ToString()`, `VillainBetSize = preflopAnalyzer.CategorizeOpponentBet(request.BetToCall, request.PotSize)`, `VillainShowedAggression = request.BetToCall > 0`, `BoardChange`, `IsDonkBet = request.Context.IsDonkBet`, `CbetAdjustment`, `FoldEquity = request.VillainProfile?.AdjustedFoldEquity ?? result.FoldEquity`, `VillainType = request.VillainProfile?.Type ?? OpponentType.Unknown`, `VillainFoldToBetPct = request.VillainProfile?.FoldToBetPct ?? -1`, `HeroIsAggressor = request.IsHeroAggressor`, `HasFlushDraw = result.DrawTypes.Contains("Flush Draw")`, `NumOpponents`, y los once flags de `request.Context`.
7. Si `request.Street == Preflop` lanza `ArgumentException`. El chequeo de situación sin escenario (Review Focus 4) vive en `PreflopRequestFactory`: si `IPreflopRangeStore.GetAsync(situación)` devuelve `null`, lanza `PreflopSituationNotFoundException(string situation)` — tipo nuevo en `src/OpenScrape.DecisionMaker/Preflop/PreflopSituationNotFoundException.cs`.

- [ ] **Step 6: Recablear `IDecisionEngine` y `LocalDecisionEngine`**

`IDecisionEngine.cs` queda con la interfaz sola, apuntando a `Contracts.DecisionRequest`/`DecisionResult`.
`LocalDecisionEngine(PreflopRequestFactory preflopFactory, PostflopRequestFactory postflopFactory, SetPreflopActionUseCase preflop, PostflopDecisionEngine postflop)`. En preflop: `Create` → `Execute` → `new DecisionResult { Action = response.ResponseAction.Action ?? "" }`. En postflop: `Create` → `Decide` → mapear `PostflopDecisionEngineResult` a `DecisionResult` (incluye `Equity`, `PotOdds`, `TotalOuts`, `EffectiveOuts`, `DrawTypes`, `HeroHandRank.ToString()`, `PairClassification.ToString()`, `KickerStrength.ToString()`, `FoldEquity`, `BetSize` desde `result.Decision`).

- [ ] **Step 7: Crear la extensión de DI**

`DecisionMakerServiceCollectionExtensions.AddDecisionMaker` registra: `IRandomProvider → SystemRandomProvider` (singleton), `MonteCarloSimulator`, `OutsCalculator`, `PreflopEquityCalculator`, `IHandEvaluator → BitHandEvaluator`, `IBoardTextureAnalyzer → BoardTextureAnalyzer`, `IPreflopAnalyzer → PreflopAnalyzer`, `UnifiedPokerCalculator` + `IPokerCalculator`, `BetSizingService`, `IRangePolarizer → RangePolarizer`, `IThresholdsRegistry → ThresholdsRegistry`, `IPostflopDecisionService → PostflopDecisionService`, `PostflopDecisionEngine`, `GetActionScenario`, `ActionScenarioUseCases`, `ISetPreflopActionUseCase → SetPreflopActionUseCase`, alias concreto por factory, ambas factories, `IDecisionEngine → LocalDecisionEngine`. Todo singleton salvo lo que dependa de scoped (preflop y engine compuesto: scoped).

- [ ] **Step 8: Ajustar el test existente de la puerta**

En `LocalDecisionEngineTests.cs`: reemplazar `DecisionRequest { Street = DecisionStreet.Preflop, Preflop = ... }` por el contrato crudo (`Street`, `HeroCard1`, `HeroCard2`, `HeroPosition`, `PotSize`, `HeroStack`) y construir el engine con las factories reales. El assert sigue siendo `Is.EqualTo("Raise")` con RNG 30.

- [ ] **Step 9: Correr y verificar**

Run: `dotnet test OpenScrape.sln`
Expected: PASS. El total baja en los tests que dependían de los DTO borrados (verificar que ningún test los usaba: `git grep "DecisionMaker.DTOs"` debe devolver vacío).

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "refactor(decisionmaker): consumir el contrato crudo y traducirlo al motor"
```

---

### Task 3: `OpenScrape.Api` con `/v1/decisions` y `/health`

**Files:**
- Create: `src/OpenScrape.Api/OpenScrape.Api.csproj`
- Create: `src/OpenScrape.Api/Program.cs`, `ApiMarker.cs`, `appsettings.json`
- Create: `src/OpenScrape.Api/Endpoints/DecisionEndpoints.cs`, `StrategyProfileConfig.cs`
- Modify: `OpenScrape.sln`
- Test: `test/OpenScrape.App.Tests/Api/DecisionEndpointTests.cs`, `test/OpenScrape.App.Tests/Api/ApiFactory.cs`
- Modify: `test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj` (referencias y `Microsoft.AspNetCore.Mvc.Testing`)

**Interfaces:**
- Consumes: `AddDecisionMaker()`, `AddUseCases()` (Features), `AddDataBase(configuration, true)` (Infrastructure), `IDecisionEngine`, `IPreflopRangeStore`.
- Produces: `POST /v1/decisions` → 200 `DecisionResult` | 400 `EngineError` | 422 `EngineError`; `GET /health` → 200 `{ "status": "ok" }`; `public sealed class ApiMarker` para `WebApplicationFactory<ApiMarker>`.

- [ ] **Step 1: Escribir el test de integración (rojo)**

`ApiFactory.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenScrape.Api;
using OpenScrape.Domain.Dtos;
using OpenScrape.Domain.Entities;
using OpenScrape.Domain.Interfaces;

namespace OpenScrape.App.Tests.Api;

/// <summary>Host en proceso con la tabla preflop y el RNG fijos del fixture.</summary>
internal sealed class ApiFactory(TableDTO table, int randomNumber, int monteCarloSeed = 12345)
    : WebApplicationFactory<ApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Marten", "Host=localhost;Database=test");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPreflopRangeStore>();
            services.AddSingleton<IPreflopRangeStore>(new InMemoryPreflopRangeStore(table));
            services.RemoveAll<IRandomProvider>();
            services.AddSingleton<IRandomProvider>(new FixedRandomProvider(randomNumber));
            services.RemoveAll<MonteCarloSimulator>();
            services.AddSingleton(new MonteCarloSimulator(monteCarloSeed));
        });
    }
}
```

`DecisionEndpointTests.cs` (mínimo, preflop AKs):```csharp
[Test]
public async Task PostDecisions_PreflopAks_DevuelveRaise()
{
    using var factory = new ApiFactory(FixtureTables.OpenRaiseAks(), randomNumber: 30);
    using var client = factory.CreateClient();

    var response = await client.PostAsJsonAsync("/v1/decisions", BuildAksRequest());
    var result = await response.Content.ReadFromJsonAsync<DecisionResult>(JsonConfig.Options);

    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    Assert.That(result!.Action, Is.EqualTo("Raise"));
}

[Test]
public async Task PostDecisions_FlopConDosCartas_Devuelve422()
{
    var request = BuildAksRequest() with { Street = DecisionStreet.Flop, Board = [new CardRef(14, 2), new CardRef(13, 2)] };
    ...
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    Assert.That(error!.Code, Is.EqualTo("invalid_request"));
}

[Test]
public async Task GetHealth_SinBaseDeDatos_DevuelveOk()
{
    ...
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
}
```

`FixtureTables` y `JsonConfig` son helpers de test: el primero lee `test/fixtures/decisions/preflop/open-raise-aks.json` y devuelve su `Table` (`OpenRaiseAks()`), el segundo expone las mismas `JsonSerializerOptions` que la API. `BuildAksRequest()` arma el `DecisionRequest` crudo con A♠K♠ en Button.

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~DecisionEndpointTests"`
Expected: FAIL con `CS0246: ApiMarker no se encontró`.

- [ ] **Step 3: Crear el proyecto Api**

```powershell
dotnet new web -n OpenScrape.Api -o src/OpenScrape.Api -f net10.0
dotnet sln OpenScrape.sln add src/OpenScrape.Api/OpenScrape.Api.csproj
dotnet add src/OpenScrape.Api/OpenScrape.Api.csproj reference src/OpenScrape.Contracts src/OpenScrape.DecisionMaker src/OpenScrape.Features src/OpenScrape.Infrastructure
dotnet add test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj reference src/OpenScrape.Api src/OpenScrape.Contracts
dotnet add test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.3
```

- [ ] **Step 4: Escribir `Program.cs`**

```csharp
using OpenScrape.Api;
using OpenScrape.Api.Endpoints;
using OpenScrape.DecisionMaker;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDecisionMaker();
builder.Services.AddUseCases();
builder.Services.AddDataBase(builder.Configuration, true);
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapDecisionEndpoints();

app.Run();

public sealed class ApiMarker;
```

`DecisionEndpoints.MapDecisionEndpoints`:
```csharp
group.MapPost("/decisions", async (DecisionRequest request, IDecisionEngine engine, CancellationToken ct) =>
{
    var invalid = RequestValidator.Validate(request);
    if (invalid is not null) return Results.UnprocessableEntity(new EngineError("invalid_request", invalid));
    try
    {
        return Results.Ok(await engine.DecideAsync(request, ct));
    }
    catch (PreflopSituationNotFoundException ex)
    {
        return Results.UnprocessableEntity(new EngineError("situation_not_found", ex.Message));
    }
});
```
ruta del grupo: `/v1`.

`RequestValidator.Validate` devuelve `null` o un mensaje: board con cantidad distinta a 0/3/4/5 según calle; cartas duplicadas entre mano y board; `PotSize < 0`; `HeroStack < 0`.

`PreflopSituationNotFoundException` se lanza desde `PreflopRequestFactory` si `IPreflopRangeStore.GetAsync` devuelve `null` (Task 2: agregar el tipo y el chequeo).

- [ ] **Step 5: Configurar el perfil**

`appsettings.json` con `StrategyProfile` copiado de `test/fixtures/decisions/strategy-profile.json` (mismos valores, mismo nombre de propiedades que `StrategyProfile`). `StrategyProfileConfig.AddApiStrategyProfile(services, configuration)` hace `services.Configure<StrategyProfile>(configuration.GetSection("StrategyProfile"))` y valida con `StrategyProfileValidator` al arrancar (falla el host si el perfil es inválido).

Agregar el test de deriva: `Api/StrategyProfileDriftTests.cs` compara el `StrategyProfile` bindeado de `appsettings.json` contra el JSON de fixtures y falla si difieren.

- [ ] **Step 6: Implementar el doble en memoria y el RNG fijo**

`InMemoryPreflopRangeStore(TableDTO table)` implementa `IPreflopRangeStore.GetAsync` devolviendo la tabla. `FixedRandomProvider(int number)` implementa `IRandomProvider` devolviendo siempre `number`. Ambos en `test/OpenScrape.App.Tests/Api/` (código de test).

- [ ] **Step 7: Correr y verificar verde**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~DecisionEndpointTests|FullyQualifiedName~StrategyProfileDriftTests"`
Expected: PASS.

- [ ] **Step 8: Verificar el host real**

Run: `dotnet run --project src/OpenScrape.Api/OpenScrape.Api.csproj --no-build` en background, luego `curl http://localhost:5000/health`
Expected: `{"status":"ok"}`. Cerrar el proceso.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(api): exponer el motor por http con health y validacion de entrada"
```

---

### Task 4: `PlayerStateMapper` y `HttpDecisionEngine` en el cliente

**Files:**
- Create: `src/OpenScrape.App/Services/PlayerStateMapper.cs`
- Create: `src/OpenScrape.App/Services/Http/HttpDecisionEngine.cs`, `EngineUnavailableException.cs`
- Modify: `src/OpenScrape.App/Program.cs`, `appsettings.json`
- Test: `test/OpenScrape.App.Tests/Services/PlayerStateMapperTests.cs`, `test/OpenScrape.App.Tests/Services/HttpDecisionEngineTests.cs`

**Interfaces:**
- Consumes: `Contracts.DecisionRequest`/`DecisionResult`, `PlayerGameState`, `DecisionContext`.
- Produces:
  - `PlayerStateMapper.MapPreflop(PlayerGameState state, bool isSecondAction, VillainProfile? villain) -> DecisionRequest`
  - `PlayerStateMapper.MapPostflop(PlayerGameState state, DecisionStreet street, decimal maxBet, DecisionContext context, VillainProfile? villain, bool heroIsAggressor) -> DecisionRequest`
  - `HttpDecisionEngine(HttpClient client, ILogger<HttpDecisionEngine> logger) : IDecisionEngine`
  - `sealed class EngineUnavailableException(string message, Exception? inner = null) : Exception`
  - `DecisionEngineSelector` en `Program.cs`: con `RemoteEngine:Enabled=false` registra `LocalDecisionEngine`; con `true` registra `HttpDecisionEngine` con `BaseAddress` de `Api:BaseUrl`.

- [ ] **Step 1: Escribir el test del mapper (rojo)**

```csharp
[Test]
public void MapPostflop_EstadoConBoardDeTres_ArmaElRequestCrudo()
{
    var state = PostflopState.WithFlopSet();   // helper del propio test
    var request = PlayerStateMapper.MapPostflop(state, DecisionStreet.Flop, 6m, new DecisionContext(), villain: null, heroIsAggressor: true);

    Assert.That(request.Street, Is.EqualTo(DecisionStreet.Flop));
    Assert.That(request.Board, Has.Count.EqualTo(3));
    Assert.That(request.PotSize, Is.EqualTo(state.PotSize));
    Assert.That(request.IsInPosition, Is.EqualTo(state.IsInPosition));
    Assert.That(request.HeroWasPreflopAggressor, Is.True);
}

[Test]
public void MapPostflop_NoIncluyeEquityNiOuts()
{
    var request = PlayerStateMapper.MapPostflop(...);
    var json = JsonSerializer.Serialize(request, JsonConfig.Options);
    Assert.That(json, Does.Not.Contain("equity"));
    Assert.That(json, Does.Not.Contain("totalOuts"));
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PlayerStateMapperTests"`
Expected: FAIL con `CS0103: PlayerStateMapper no existe`.

- [ ] **Step 3: Implementar `PlayerStateMapper`**

Traduce `PlayerGameState` → `DecisionRequest`: cartas desde `HoleCard1Rank/Suit` y `HoleCard2Rank/Suit`; `Board` desde `BoardCards` (`new CardRef(card.Force, card.Suit)`); `PotSize`, `HeroStack`, `IsInPosition`, `Position`, `HandSituation`; `BetToCall = maxBet`; `VillainStack` desde el primer `Player` activo distinto del héroe (`0` si no hay); `VillainPosition` desde ese jugador; `NumOpponents` = activos menos uno; `Context` tal cual. **No** calcula equity, outs, textura ni pot odds.

- [ ] **Step 4: Implementar `HttpDecisionEngine`**

`HttpClient` tipado con `BaseAddress = Api:BaseUrl` y `Timeout = TimeSpan.FromSeconds(1.5)`. `DecideAsync` hace `PostAsJsonAsync("/v1/decisions", request, ct)`; si `IsSuccessStatusCode` deserializa `DecisionResult`; si 400/422 lee `EngineError` y lanza `ArgumentException`; si hay `TaskCanceledException`/`HttpRequestException`/5xx lanza `EngineUnavailableException`. Sin reintentos. En 3a no adjunta token (3b lo agrega).

- [ ] **Step 5: Escribir el test del cliente (rojo→verde)**

`HttpDecisionEngineTests`: con `ApiFactory` en proceso y `new HttpClient { BaseAddress = factory.Server.BaseAddress }`; un request AKs devuelve `Raise`; con `BaseAddress` a un puerto cerrado lanza `EngineUnavailableException` en menos de 2 s.

- [ ] **Step 6: Agregar el flag y el selector**

`appsettings.json`: `"RemoteEngine": { "Enabled": false }, "Api": { "BaseUrl": "http://localhost:5000" }`.
En `Program.cs`, extraer el registro del motor a un método que lea la configuración y registre uno u otro. Con `false`, el comportamiento debe ser idéntico al actual: los tests existentes son la prueba.

- [ ] **Step 7: Correr la suite completa**

Run: `dotnet test OpenScrape.sln`
Expected: PASS, mismo total que al cerrar la Task 3.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(app): traducir el estado de mesa al contrato y agregar el cliente http"
```

---

### Task 5: Recablear el coordinador y los llamadores preflop a `IDecisionEngine`

**Files:**
- Modify: `src/OpenScrape.App/Services/GameCoordinator.cs`
- Modify: `src/OpenScrape.App/Services/HandProcessor.cs:27,47,142`
- Modify: `src/OpenScrape.App/Forms/FrmMain.cs:137,231,1072`
- Modify: `test/OpenScrape.App.Tests/Fixtures/GameCoordinatorFixtureBuilder.cs`
- Modify: `test/OpenScrape.App.Tests/Fixtures/PostflopDecisionRunner.cs`
- Test: `test/OpenScrape.App.Tests/Api/ParidadHttpTests.cs`

**Interfaces:**
- Consumes: `IDecisionEngine`, `PlayerStateMapper`, `ApiFactory`.
- Produces: `GameCoordinatorFixtureBuilder.Create(StrategyProfile profile, IGameLoggerService logger, IDecisionEngine? engine = null)`; con `engine == null` construye `LocalDecisionEngine` con las factories reales.

**Notas de implementación obligatorias:**
- `DetermineFlopAction`/`DetermineTurnAction`/`DetermineRiverAction` conservan **todo** su código de efectos (tracking, contexto cross-street, exploitability, logging) y reemplazan únicamente la composición del request y la llamada al motor por `PlayerStateMapper.MapPostflop` + `await _decisionEngine.DecideAsync` + `ToCalculationResult(result)`.
- `ToCalculationResult` es un mapeo privado `Contracts.DecisionResult → PokerCalculationResult` para que `FlopResult`/`TurnResult`/`RiverResult` y `FrmMain` sigan funcionando sin cambios.
- Cuando el motor lanza `EngineUnavailableException`, el coordinador **propaga** la excepción; el loop (`HandProcessor`) la captura, detiene el ciclo y escribe el motivo en el log. No se emite decisión.
- Preflop: `HandProcessor` y `FrmMain` reemplazan `_setPreflopActionUseCase.Execute(...)` por `_decisionEngine.DecideAsync(PlayerStateMapper.MapPreflop(...))`.
- Si `IGameCoordinator` declara los tres `Determine*Action`, la interfaz también pasa a `Task<GameDecisionResult>`; actualizar `GameLoopStateMachine`/`HandProcessor`/`FrmMain` para `await`. `PostflopDecisionRunner.RunStreet` pasa a `async Task<PostflopDecisionSnapshot>` y sus llamadores a `await`.

- [ ] **Step 1: Hacer inyectable el motor en el builder de fixtures**

Agregar el parámetro `IDecisionEngine? engine = null` a `Create`; el coordinador lo recibe por ctor. Si es `null`, construir `LocalDecisionEngine` con `PreflopRequestFactory`, `PostflopRequestFactory` (que necesita `UnifiedPokerCalculator` con la semilla del fixture), `SetPreflopActionUseCase` y `PostflopDecisionEngine` — igual que hoy arma el pipeline, pero por la puerta.

- [ ] **Step 2: Correr el replay postflop y verificar que sigue verde antes de tocar el coordinador**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~PostflopFixtureReplayTests"`
Expected: PASS 1/1. Este es el punto de control: el motor inyectado reproduce el camino actual.

- [ ] **Step 3: Recablear el coordinador**

En los tres métodos, reemplazar la construcción de `PostflopEngineRequest` por:
```csharp
var request = PlayerStateMapper.MapPostflop(state, DecisionStreet.Flop, betToCall, context, villain, heroIsAggressor);
var result = await _decisionEngine.DecideAsync(request, cancellationToken);
var decision = new PostflopDecisionResult(result.Action, result.Reason, result.IsBluff, result.IsBarrel, result.IsCheckRaise, result.IsFloating);
effectiveEquity = result.Equity;
```
Los tres métodos pasan a `async Task<GameDecisionResult>`; ajustar los llamadores (`HandProcessor`, `FrmMain`) para `await`. `_postflopEngine` y `_postflopDecisionService` se quitan del coordinador si ya no se usan; `_exploitabilityCalculator`, `_opponentTracker`, `_contextHolder`, `_gameLoopStateMachine`, `_gameLoggerService` se quedan.

- [ ] **Step 4: Recablear preflop**

`HandProcessor` y `FrmMain` reciben `IDecisionEngine` en vez de `ISetPreflopActionUseCase`. En el punto de llamada: `var result = await _decisionEngine.DecideAsync(PlayerStateMapper.MapPreflop(state, isSecondAction, villainProfile));` y seguir usando `result.Action` igual que antes `response.ResponseAction.Action`.

- [ ] **Step 5: Escribir el test de paridad 15/15 (rojo→verde)**

`ParidadHttpTests` recorre los 15 fixtures. Para cada uno:
- Preflop: `ApiFactory(fixture.Table, fixture.RandomNumber)`; request armado con `PlayerStateMapper.MapPreflop` desde el `PlayerState` del fixture; assert `Action == ExpectedAction`.
- Postflop: `ApiFactory(FixtureTables.Empty(), randomNumber: 0, monteCarloSeed: fixture.MonteCarloSeed)`; correr `PostflopDecisionRunner.RunStreet` con el coordinador construido sobre `HttpDecisionEngine` apuntando al host en proceso; assert `Action == ExpectedAction`, `|Equity − ExpectedEquity| <= 0.1`, y si el fixture trae `ExpectedBoardTexture`/`ExpectedIsBluff`/`ExpectedIsBarrel`/`ExpectedIsCheckRaise`, compararlos.

Un fixture que no coincide debe fallar con el `Id` en el mensaje.

- [ ] **Step 6: Correr la paridad y la suite**

Run: `dotnet test test/OpenScrape.App.Tests/OpenScrape.App.Tests.csproj --filter "FullyQualifiedName~ParidadHttpTests"`
Expected: PASS 15/15.
Run: `dotnet test OpenScrape.sln`
Expected: PASS.

- [ ] **Step 7: Verificar el arranque del cliente con ambos motores**

Con `RemoteEngine:Enabled=false`: arrancar `OpenScrape.App.exe`, confirmar `RUNNING`, cerrar.
Con `RemoteEngine:Enabled=true` y la API levantada: arrancar ambos, confirmar que el cliente no crashea.
Con `RemoteEngine:Enabled=true` y la API apagada: arrancar el cliente, confirmar que muestra el error de motor no disponible y **no** decide.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(app): decidir a traves de la puerta unica y probar paridad http"
```

---

### Task 6: Guardas de arquitectura y gate

**Files:**
- Create: `test/OpenScrape.App.Tests/Architecture/ContractsIsolationTests.cs`
- Modify: `scripts/verify-pre-merge.ps1`
- Modify: `docs/superpowers/specs/2026-10-23-api-motor-decision-design.md` (marcar 3a como completada)

**Interfaces:** ninguna nueva.

- [ ] **Step 1: Escribir el test de aislamiento (rojo→verde)**

```csharp
[Test]
public void Contracts_NoReferenciaMartenNiAspNetCore()
{
    var csproj = File.ReadAllText(FindProject("src/OpenScrape.Contracts/OpenScrape.Contracts.csproj"));
    Assert.That(csproj, Does.Not.Contain("Marten"));
    Assert.That(csproj, Does.Not.Contain("AspNetCore"));
}

[Test]
public void Contracts_SoloReferenciaDomain()
{
    var references = Regex.Matches(csproj, @"ProjectReference Include=""([^""]+)""")
        .Select(m => Path.GetFileNameWithoutExtension(m.Groups[1].Value));
    Assert.That(references, Is.EqualTo(new[] { "OpenScrape.Domain" }));
}
```

- [ ] **Step 2: Agregar el checkpoint al gate**

En `scripts/verify-pre-merge.ps1`, después del checkpoint de fixtures: correr `dotnet test --filter "FullyQualifiedName~Architecture"` y fallar si no pasa.

- [ ] **Step 3: Correr el gate completo**

Run: `.\scripts\verify-pre-merge.ps1 -SkipSmoke`
Expected: `ALL CHECKPOINTS PASSED`, con `preflop: 8 | postflop: 7`.

- [ ] **Step 4: Actualizar la spec**

En `### Fase 3`, marcar `3a — Paridad` como `Estado: completada` con el commit de cierre y el total de tests.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(arquitectura): blindar el aislamiento de contracts y cerrar la fase 3a"
```

---

## Self-review del plan

- **Cobertura de la spec (3a):** `Contracts` (T1), `DecisionMaker` sin DTO muertos y con factories (T2), `Api` con `/v1/decisions` y `/health` (T3), cliente HTTP tras el flag (T4), recableado del coordinador y preflop (T5), paridad 15/15 (T5), guardas de arquitectura y gate (T6). Licencias, JWT, cuotas, `LicenseCli` y `FormLicense` quedan explícitamente fuera: son 3b.
- **Fuera de alcance declarado:** `MartenOpponentProfileStore` mantiene Marten dentro de `DecisionMaker`; se muda con `IOpponentTracker` a `App` en la fase 4. `App` sigue referenciando `DecisionMaker` durante 3a.
- **Consistencia de tipos:** `DecisionStreet`, `CardRef`, `DecisionRequest`, `DecisionResult`, `BetSizeCategory`, `VillainProfile`, `DecisionContext` se definen en T1 y se consumen sin cambios en T2-T6. `PostflopEngineRequest` y `SetPreflopActionUseCaseRequest` nunca cruzan el contrato.
- **Proporción:** el plan define decisiones (archivos, firmas, valores, tests) y deja los cuerpos al ejecutor, salvo la validación de entrada y el orden de cálculo de la factory postflop, que son decisiones de diseño.
