## 1. Preparación y auditoría

- [x] 1.1 Baseline de callers: `PostflopDecisionServiceTests.cs`=299 (228 viejos + 71 nuevos), `PairClassificationTests.cs`=8, `RangePolarizerIntegrationTests.cs`=11, `StrategyBacktester.cs`=1.
- [x] 1.2 Fallback del wrapper: solo **uno** — `effectiveOuts: input.EffectiveOuts > 0 ? input.EffectiveOuts : input.TotalOuts` (línea 106). `riverCardType` se pasa tal cual. Preservar en el nuevo cuerpo como variable local al inicio.
- [x] 1.3 Baseline de build/tests: 508 warnings CS0618 + 849/849 tests verdes (verificado previamente).

## 2. Refactor del servicio

- [x] 2.1 En `PostflopDecisionService.cs`: mover el cuerpo completo del método viejo `DetermineAction(double equity, ..., RiverCardType riverCardType)` al método `DetermineAction(PostflopDecisionInput input)`. Renombrar todas las referencias locales a `input.*` (p.ej. `equity` → `input.Equity`, `street` → `input.Street`).
- [x] 2.2 Preservar los fallbacks auditados en 1.2 dentro del cuerpo nuevo (como variables locales al inicio del método si aplica).
- [x] 2.3 Eliminar el método viejo `DetermineAction(double equity, ...)` de `PostflopDecisionService.cs`. Eliminar `#pragma warning disable CS0618` y `#pragma warning restore CS0618`.
- [x] 2.4 Eliminar la declaración `[Obsolete]` del método viejo en `IPostflopDecisionService.cs`.
- [x] 2.5 `dotnet build src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` — debe compilar 0 errores (los tests y App todavía no compilan porque usan el overload viejo; esperado).

## 3. Migración del caller de producción

- [x] 3.1 En `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs:90`, reemplazar la llamada con parámetros nombrados por `DetermineAction(new PostflopDecisionInput { Equity = original.EquityPercent, Street = original.Street, Situation = original.Situation, BoardTexture = original.BoardTexture ?? "Dry", IsInPosition = original.IsInPosition, VillainBetSize = ..., PotOdds = original.PotOddsPercent, TotalOuts = original.TotalOuts, HeroStack = hand.HeroStackStart, PotSize = original.PotSizeAtDecision, NumOpponents = Math.Max(1, hand.NumOpponents) })`.
- [x] 3.2 `dotnet build src/OpenScrape.DecisionMaker/OpenScrape.DecisionMaker.csproj` verde.

## 4. Migración de tests: helpers MakeInput

- [x] 4.1 En `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs`: añadir método privado estático `MakeInput(double equity, BoardPosition street, HandSituation situation, string boardTexture, bool isInPosition, BetSizeCategory villainBetSize, double potOdds = 0, int totalOuts = 0, ...)` que construya `PostflopDecisionInput`. La firma debe replicar el orden y defaults de los parámetros del overload eliminado para minimizar diff.
- [x] 4.2 En `OpenScrape.App.Tests/PairClassificationTests.cs`: añadir el mismo helper `MakeInput` (o reutilizar si se decide extraer a `TestHelpers`).
- [x] 4.3 En `OpenScrape.App.Tests/RangePolarizerIntegrationTests.cs`: añadir el mismo helper `MakeInput`.

## 5. Migración de callers en tests

- [x] 5.1 En `PostflopDecisionServiceTests.cs`: reemplazar cada llamada `DetermineAction(<named-args>)` (callers del overload viejo, NO los que ya usan `new PostflopDecisionInput`) por `DetermineAction(MakeInput(<named-args>))`. ~228 callers.
- [x] 5.2 En `PairClassificationTests.cs`: mismo patrón, 8 callers.
- [x] 5.3 En `RangePolarizerIntegrationTests.cs`: mismo patrón, 11 callers.
- [x] 5.4 `dotnet build OpenScrape.sln` — debe compilar 0 errores y 0 warnings CS0618.

## 6. Guardrail contra regresión

- [x] 6.1 Crear `OpenScrape.App.Tests/PostflopDecisionApiContractTests.cs` con un único `[Test]` `IPostflopDecisionService_ExposesSingleDetermineAction()` que:
  - Obtiene `typeof(IPostflopDecisionService).GetMethods()`.
  - Asserta `methods.Count(m => m.Name == "DetermineAction") == 1`.
  - Asserta que su único parámetro es de tipo `PostflopDecisionInput` y retorna `PostflopDecisionResult`.
  - Asserta `methods.Any(m => m.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false).Length > 0) == false`.

## 7. Verificación final

- [x] 7.1 `dotnet build OpenScrape.sln` → 0 errores y 0 warnings CS0618 (baseline era 508).
- [x] 7.2 `dotnet test OpenScrape.sln` → 850/850 verdes (849 existentes + 1 guardrail).
- [x] 7.3 `dotnet format --verify-no-changes OpenScrape.sln` sin cambios.
- [x] 7.4 `grep -r "CS0618" src/ OpenScrape.App.Tests/` → sin resultados (ni pragmas ni warnings asociados).
- [x] 7.5 `grep -n "DetermineAction(" src/ | wc -l` debería coincidir con el número esperado (1 por GameCoordinator × 3 + 1 StrategyBacktester + 1 definición en interfaz + 1 implementación = 6).
- [x] 7.6 Actualizar `CLAUDE.md` si hace referencia al overload obsoleto (revisar con `grep -n "DetermineAction" CLAUDE.md`).
