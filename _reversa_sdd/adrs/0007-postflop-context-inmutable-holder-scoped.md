# ADR-0007 — `PostflopGameContext` inmutable con `IPostflopContextHolder` scoped

- **Estado:** 🟢 ACEPTADO (vigente). Reemplaza diseño previo (clase mutable con dos instancias divergentes).
- **Fecha:** 2026-04-21 (commit `127a2f5 refactor(decision): immutable PostflopGameContext with scoped holder`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude Sonnet 4.6 (`Co-Authored-By` en commit message)

## Contexto

`PostflopGameContext` mantiene estado cross-street para decisiones postflop: quién apostó en cada calle, sizing tells, board change acumulado, auto-rebuy detection, etc. Originalmente era una **clase mutable** con 15+ setters públicos.

**Bug latente:** `FrmMain._postflopContext` y `GameCoordinator.PostflopContext` eran instancias separadas que **divergían silenciosamente** durante mid-street reprocess (cuando villain raise dispara una segunda vuelta de la misma calle). Una mutación en `GameCoordinator` no se reflejaba en `FrmMain` y viceversa.

Síntoma observable: en spots con villain raise + reprocess de turn, `IsVillainBarreling` o `HeroFloatedFlop` salían inconsistentes con el flujo real.

## Decisión

Convertir `PostflopGameContext` a:

1. **`sealed record` con `init`-only properties.** Reemplaza setters mutables.
2. **`NewHand()` factory** sustituye al antiguo `Reset()`.
3. **Helpers `WithFlopState`, `WithTurnState`, `TrackHeroStack`** generan instancias nuevas con `c with { ... }`. `TrackHeroStack` cambia firma: ya no muta y retorna `decimal`, ahora retorna **tupla** `(PostflopGameContext, decimal)`.
4. **`IPostflopContextHolder` (scoped)** es el único poseedor del contexto durante la mano. `holder.Update(c => c with { ... })` con `lock` interno + `Volatile.Read` garantiza thread safety.
5. **Eliminar `PostflopContext` de la superficie pública** de `IGameCoordinator`. `FrmMain` y `GameCoordinator` ahora **leen el mismo holder** vía DI.

## Alternativas consideradas

1. **Sincronizar las dos instancias mutables manualmente.** Rechazado: cualquier nuevo setter olvidado vuelve a divergir. Mantener invariant a mano es frágil.
2. **Mover `PostflopContext` solo a `GameCoordinator`** y exponerlo como propiedad pública lectura. Rechazado: `FrmMain` también necesita escribir (en el game loop reset). Compartir referencia mutable reproduce el problema.
3. **Singleton root.** Rechazado: vida de mano = vida de scope, no de la app. Singleton mantendría datos entre manos.
4. **Event-sourcing del contexto.** Sobre-ingeniería para el caso. La mano dura segundos; reconstrucción de estado por eventos no aporta.
5. **`record class` con `Update(Action<...>)` mutador.** Híbrido. Rechazado: si el record es mutable internamente, no garantiza inmutabilidad efectiva.

## Consecuencias

**Positivas:**

- **Una única instancia por scope** garantizada por DI. Imposible que dos componentes diverjan.
- **Inmutabilidad** facilita razonamiento: cada lectura ve un snapshot estable. Si quieres cambiar, generas uno nuevo.
- `c with { ... }` es declarativo: lee como "el nuevo contexto es como este pero con esto otro".
- 16 mutaciones del `GameCoordinator` migradas a `holder.Update(c => c with { ... })`. 4 mutaciones del `FrmMain` migradas a `_contextHolder.StartNewHand()`.
- Test suite **sube de 1122 a 1138 verdes** (+16 tests nuevos) sin romper ninguna decisión existente. `DecisionMatrix` 216 cases sin cambios.

**Negativas:**

- **Cada mutación crea un objeto.** Para una mano (~10 mutaciones) son 10 records temporales. Despreciable para GC moderno.
- Sintaxis `c with { ... }` puede ser sorprendente para devs nuevos en C# 9+ records.
- `TrackHeroStack` ahora retorna tupla — los callers deben deconstruirla. Más verbose.

**Implicaciones para una migración:**

- En lenguajes con records inmutables nativos (Kotlin data class copy, Rust struct + builders, F# records) la migración es directa.
- En Python con dataclass `frozen=True` + `dataclasses.replace()` es 1:1.
- En cualquier paradigma, **el patrón "single holder + immutable transitions"** es el invariante a preservar.

## Referencias

- Commit `127a2f5 refactor(decision): immutable PostflopGameContext with scoped holder`.
- Commit `7409bf5 docs(openspec): archive postflop-context-inmutable and sync specs` — archive de la spec OpenSpec.
- `openspec/specs/postflop-context-holder/spec.md` — capability spec.
- `src/OpenScrape.DecisionMaker/Services/PostflopGameContext.cs`.
- `src/OpenScrape.App/Services/IPostflopContextHolder.cs` + `PostflopContextHolder.cs`.
