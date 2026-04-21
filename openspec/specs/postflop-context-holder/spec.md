## ADDED Requirements

### Requirement: PostflopGameContext es un record inmutable

`PostflopGameContext` SHALL ser declarado `public sealed record` en `OpenScrape.DecisionMaker.Services`. Todas las propiedades de estado (`VillainBetFlop`, `VillainBetTurn`, `HeroBetFlop`, `HeroBetTurn`, `PreviousStreetWasBet`, `VillainBetSizeFlop`, `VillainBetSizeTurn`, `VillainAggressorCheckedFlop`, `VillainCheckedMiddleStreet`, `HeroFloatedFlop`, `TurnCalledWithFlushDanger`, `IsAnyoneAllIn`, `TurnBetCommitsToRiver`, `InitialBoardDanger`, `LastBoardChange`, `HeroStackPreRebuy`) MUST ser `{ get; init; }`. Las propiedades derivadas (`HeroCheckedAllStreets`, `IsVillainBarreling`) permanecen como `{ get; }` computadas.

El record MUST NO exponer métodos que muten campos en su propio `this`: `Reset()`, `UpdateFlopState`, `UpdateTurnState` y la versión mutadora de `TrackHeroStackForRebuy` MUST eliminarse.

#### Scenario: Compilación rechaza asignación post-construcción

- **WHEN** un call-site intenta `context.HeroBetFlop = true` tras la construcción
- **THEN** el compilador emite un error CS8852 (init-only)
- **AND** el código no compila

#### Scenario: Igualdad por valor

- **GIVEN** `a = PostflopGameContext.NewHand() with { HeroBetFlop = true }`
- **AND** `b = PostflopGameContext.NewHand() with { HeroBetFlop = true }`
- **WHEN** se comparan con `a == b`
- **THEN** el resultado es `true`

### Requirement: Factory NewHand sustituye a Reset

`PostflopGameContext` SHALL exponer un método estático `public static PostflopGameContext NewHand()` que devuelve una nueva instancia con todos los campos en su valor por defecto documentado (flags booleanos a `false`, `BetSizeCategory.NoBet`, `BoardChangeResult.Safe`, `HeroStackPreRebuy = 0`).

`Reset()` MUST eliminarse. Los consumidores que antes llamaban `context.Reset()` ahora obtienen un nuevo contexto vía `NewHand()`.

#### Scenario: NewHand produce un contexto en estado inicial

- **WHEN** se llama `var ctx = PostflopGameContext.NewHand()`
- **THEN** `ctx.HeroBetFlop`, `ctx.HeroBetTurn`, `ctx.VillainBetFlop`, `ctx.VillainBetTurn`, `ctx.PreviousStreetWasBet`, `ctx.IsAnyoneAllIn`, `ctx.TurnBetCommitsToRiver` son `false`
- **AND** `ctx.VillainBetSizeFlop == BetSizeCategory.NoBet`
- **AND** `ctx.HeroStackPreRebuy == 0m`
- **AND** `ctx.InitialBoardDanger.DangerLevel == 0`

### Requirement: Métodos With para transiciones de street

`PostflopGameContext` SHALL exponer métodos de transición no-mutadores que devuelven un contexto nuevo:

- `WithFlopState(bool heroBet, bool villainBet, bool isPreflopAggressor)` — actualiza `HeroBetFlop`, `VillainBetFlop`, `PreviousStreetWasBet`, `VillainAggressorCheckedFlop` en una sola operación.
- `WithTurnState(bool heroBet, bool villainBet)` — actualiza `HeroBetTurn`, `VillainBetTurn`, `PreviousStreetWasBet` y deriva `VillainCheckedMiddleStreet = VillainBetFlop && !villainBet`.

Estos métodos MUST devolver un `PostflopGameContext` nuevo con los campos derivados coherentes; MUST NO mutar el receptor.

#### Scenario: WithFlopState es atómico

- **GIVEN** `var ctx = PostflopGameContext.NewHand()`
- **WHEN** se llama `var next = ctx.WithFlopState(heroBet: true, villainBet: false, isPreflopAggressor: true)`
- **THEN** `next.HeroBetFlop == true`
- **AND** `next.VillainBetFlop == false`
- **AND** `next.PreviousStreetWasBet == true`
- **AND** `next.VillainAggressorCheckedFlop == false` (hero era el agresor preflop)
- **AND** `ctx` sigue sin cambios (inmutable)

#### Scenario: WithTurnState deriva VillainCheckedMiddleStreet

- **GIVEN** `var ctx = PostflopGameContext.NewHand() with { VillainBetFlop = true }`
- **WHEN** se llama `var next = ctx.WithTurnState(heroBet: false, villainBet: false)`
- **THEN** `next.VillainCheckedMiddleStreet == true` (apostó flop, chequeó turn)

### Requirement: TrackHeroStack devuelve tupla sin mutar

`PostflopGameContext` SHALL exponer `public (PostflopGameContext NewContext, decimal EffectiveStack) TrackHeroStack(decimal currentStack)` que sustituye al antiguo `TrackHeroStackForRebuy`. Este método MUST NO mutar el receptor y MUST devolver:

- `EffectiveStack` = el stack que debe usarse para profit tracking (el pre-rebuy si detectó rebuy, el actual si no).
- `NewContext` = un contexto con `HeroStackPreRebuy` actualizado según las mismas reglas que antes (primer registro → registra; incremento ≥50 → conserva pre-rebuy; descenso o incremento <50 → actualiza).

#### Scenario: Primera lectura registra stack

- **GIVEN** `var ctx = PostflopGameContext.NewHand()` (`HeroStackPreRebuy == 0`)
- **WHEN** `var (next, eff) = ctx.TrackHeroStack(80m)`
- **THEN** `eff == 80m` y `next.HeroStackPreRebuy == 80m`
- **AND** `ctx.HeroStackPreRebuy == 0m` (el original no se muta)

#### Scenario: Incremento masivo se interpreta como rebuy

- **GIVEN** `var (ctx, _) = PostflopGameContext.NewHand().TrackHeroStack(20m)`
- **WHEN** `var (next, eff) = ctx.TrackHeroStack(100m)` (salto de +80)
- **THEN** `eff == 20m` (stack pre-rebuy)
- **AND** `next.HeroStackPreRebuy == 20m` (conservado)

#### Scenario: Incremento pequeño actualiza normalmente

- **GIVEN** `var (ctx, _) = PostflopGameContext.NewHand().TrackHeroStack(80m)`
- **WHEN** `var (next, eff) = ctx.TrackHeroStack(90m)` (salto de +10, win de bote)
- **THEN** `eff == 90m`
- **AND** `next.HeroStackPreRebuy == 90m`

### Requirement: IPostflopContextHolder es el único poseedor del contexto durante una mano

La aplicación SHALL exponer la interfaz `IPostflopContextHolder` en `OpenScrape.App.Services` con la siguiente superficie:

- `PostflopGameContext Current { get; }` — snapshot actual.
- `void Update(Func<PostflopGameContext, PostflopGameContext> updater)` — aplica una transformación al contexto y reemplaza `Current` por el resultado.
- `void StartNewHand()` — reemplaza `Current` por `PostflopGameContext.NewHand()`.

La implementación `PostflopContextHolder` MUST registrarse como **scoped** en el contenedor DI. El scope de juego de la aplicación (`CreateAsyncScope` en `Program.cs`) garantiza una sola instancia por sesión. La mutación DEBE ser thread-safe usando `Interlocked.Exchange` o `lock` — el game loop puede invocar `Update` desde hilos distintos.

`GameCoordinator` y `FrmMain` MUST depender del holder via constructor/DI. Ninguno MUST crear instancias propias de `PostflopGameContext` con `new`.

#### Scenario: Current devuelve el contexto tras Update

- **GIVEN** un `PostflopContextHolder` recién construido
- **WHEN** se invoca `holder.Update(ctx => ctx with { HeroBetFlop = true })`
- **THEN** `holder.Current.HeroBetFlop == true`

#### Scenario: StartNewHand reinicia el contexto

- **GIVEN** un holder con `HeroBetFlop = true` y `HeroStackPreRebuy = 100m`
- **WHEN** se invoca `holder.StartNewHand()`
- **THEN** `holder.Current.HeroBetFlop == false`
- **AND** `holder.Current.HeroStackPreRebuy == 0m`

#### Scenario: DI resuelve una sola instancia por scope

- **GIVEN** un `IServiceProvider` con `IPostflopContextHolder` registrado scoped
- **WHEN** se resuelve el holder dos veces desde el mismo scope
- **THEN** ambas resoluciones retornan la misma instancia (referencia idéntica)
- **AND** al resolver desde un scope distinto se obtiene una instancia diferente

### Requirement: GameCoordinator y FrmMain no poseen su propio PostflopGameContext

`GameCoordinator` MUST NO exponer la propiedad `PostflopGameContext PostflopContext { get; }`. `FrmMain` MUST NO tener un campo `_postflopContext` construido con `new`. Ambas dependencias MUST inyectar `IPostflopContextHolder` y leer/mutar el contexto a través de él.

#### Scenario: No hay instanciación directa fuera del holder

- **WHEN** se grepea `new PostflopGameContext(` en `src/OpenScrape.App/`
- **THEN** cero coincidencias (salvo dentro de `PostflopContextHolder` y los tests)

#### Scenario: GameCoordinator.PostflopContext ya no existe

- **WHEN** se inspeccionan los miembros públicos de `IGameCoordinator`
- **THEN** no existe miembro `PostflopContext` de tipo `PostflopGameContext`
