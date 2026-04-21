## Why

`PostflopGameContext` (`src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs`, 190 LOC) es una clase mutable con **15 setters públicos**, tres métodos mutadores (`Reset`, `UpdateFlopState`, `UpdateTurnState`) y un método `TrackHeroStackForRebuy` que muta y retorna. El estado se modifica en **44 sitios** del solution (38 en `GameCoordinator`, 6 en `FrmMain`), y cada decisión postflop se toma contra una instancia cuyo contenido puede haber cambiado entre el momento en que se leyó la equity y el momento en que el `PostflopDecisionService` la consume.

El problema va más allá de "preferir records sobre clases": hoy **conviven dos instancias separadas** del contexto — una en `FrmMain._postflopContext = new()` y otra en `GameCoordinator.PostflopContext = new()`. Ambas se mutan en paralelo y nunca se leen la una a la otra. Eso es un bug latente: el estado cross-street del coordinator y el del form divergen, y cualquier decisión que mezcle fuentes lee algo incoherente. Un record inmutable único, con reemplazo explícito de la referencia, hace imposible esa duplicación silenciosa — porque cualquier lector de "el contexto actual" tiene que pasar por un único poseedor.

Tres invariantes rotos que este cambio restaura: (1) una decisión postflop ve un snapshot estable de estado cross-street durante toda su ejecución; (2) la transición de street (flop → turn → river) es una operación atómica que produce un contexto nuevo en vez de una serie de asignaciones parciales que puedan fallar a medias; (3) hay una única fuente de verdad del contexto por mano.

## What Changes

- Convertir `PostflopGameContext` a `public sealed record` con propiedades `init-only`. Todos los `{ get; set; }` pasan a `{ get; init; }`.
- Añadir métodos `With*` no-mutadores que devuelven un contexto nuevo para cada transición: `WithFlopState(bool heroBet, bool villainBet, bool isPreflopAggressor)`, `WithTurnState(bool heroBet, bool villainBet)`, `WithInitialBoardDanger(BoardChangeResult)`, `WithLastBoardChange(BoardChangeResult)`, `WithVillainBetSizes(...)`, `WithAllInFlagged()`, `WithPreviousStreetBet(bool)`.
- Reemplazar `Reset()` (mutación in-place) por factory estática `PostflopGameContext.NewHand()` que devuelve un contexto virgen.
- Reemplazar `TrackHeroStackForRebuy(decimal)` (muta y retorna `decimal`) por `TrackHeroStack(decimal)` que devuelve `(PostflopGameContext NewContext, decimal EffectiveStack)`. `HeroStackPreRebuy` pasa a `init`-only.
- Introducir `IPostflopContextHolder` — un servicio scoped con un único `PostflopGameContext Current` (y método `Update(Func<PostflopGameContext, PostflopGameContext>)`) que reemplaza las dos instancias paralelas de hoy. `GameCoordinator` y `FrmMain` dependen del holder, nunca crean contextos por su cuenta.
- Migrar los 44 call-sites: cada `PostflopContext.X = Y` se convierte en `_holder.Update(ctx => ctx with { X = Y })`. Cada lectura sigue siendo `_holder.Current.X`.
- **BREAKING (interno):** se elimina la superficie mutadora pública. El código que hacía `ctx.HeroBetFlop = true` deja de compilar y debe migrarse al holder.
- Tests: los 6 tests de `PostflopGameContextRebuyTests` se reescriben para la nueva API (`TrackHeroStack` devuelve tupla; `NewHand()` sustituye `Reset()`). Añadir ≥5 tests nuevos de `IPostflopContextHolder` y de la atomicidad de `WithFlopState`/`WithTurnState`.

## Capabilities

### New Capabilities

- `postflop-context-holder`: Poseedor único (scoped) del contexto postflop de la mano en curso, con API `Current` + `Update(...)` y garantía de instancia única por scope de decisión.

### Modified Capabilities

- `postflop-decision-api`: El cuerpo de `PostflopDecisionService` sigue consumiendo un `PostflopGameContext` vía `PostflopDecisionInput` — el contrato externo no cambia. Lo que cambia es que el objeto que se pasa ahora es un record inmutable; cualquier consumidor que mutara el contexto dentro de `DetermineAction` (no debería, pero hoy es sintácticamente posible) deja de compilar.

## Impact

- **Código afectado**:
  - `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs` — conversión a `record` con `init`-only, métodos `With*` nuevos, factory `NewHand()`, tupla en `TrackHeroStack`.
  - `src/OpenScrape.App/Services/IPostflopContextHolder.cs` (nuevo) + implementación `PostflopContextHolder`.
  - `src/OpenScrape.App/Services/GameCoordinator.cs` — migra 38 mutaciones al holder; elimina `public PostflopGameContext PostflopContext { get; }`.
  - `src/OpenScrape.App/Services/IGameCoordinator.cs` — quita (o adapta) la propiedad `PostflopContext`.
  - `src/OpenScrape.App/Forms/FrmMain.cs` — elimina `_postflopContext = new()`, inyecta `IPostflopContextHolder`, migra las 6 mutaciones.
  - `src/OpenScrape.App/Program.cs` — registra `IPostflopContextHolder` como scoped.
- **Tests**: `OpenScrape.App.Tests/PostflopGameContextRebuyTests.cs` se reescribe. Nuevo `PostflopContextHolderTests.cs`. La suite existente (~1122 tests) no debe cambiar comportamiento — ninguna decisión se altera.
- **Runtime**: asignaciones extra de records (copia por calle). Impacto despreciable — el contexto tiene ~15 campos, se crea a lo sumo 4-5 veces por mano. La ganancia de claridad/correctitud compensa sobradamente.
- **Dependencias externas**: ninguna.
