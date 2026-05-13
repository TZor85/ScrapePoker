# ADR-0014 — `FrmMain` resuelto desde scope (no root) por dependencias scoped

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-19 (commit `8c94e22 refactor/decision: Modularize postflop engine & services`)
- **Confianza:** 🟢 CONFIRMADO en `Program.cs` y CLAUDE.md
- **Decisor inferido:** Alberto

## Contexto

`FrmMain` necesita inyectar:

- **Scoped use cases** del `OpenScrape.Features` (ej: `GetActionScenarioUseCase`, `LoadTableMapUseCase`). Cada operación necesita un scope independiente con su `IDocumentSession` Marten.
- **Singletons:** `RegionLookupCache`, `CardCacheService`, `OcrService`, `IThresholdsRegistry`.
- **Scoped:** `GameCoordinator`, `IPostflopContextHolder`.
- **Singletons del DecisionMaker:** todos los algoritmos (`IBitHandEvaluator`, `IMonteCarloSimulator`, etc.).

Si se resuelve `FrmMain` desde el contenedor root, los servicios scoped que dependen de él (use cases) **lanzan excepción** en runtime: "Cannot resolve scoped service 'X' from root provider".

## Decisión

1. **Resolver `FrmMain` desde un scope creado explícitamente** en `Program.Main`. El refactor (commit `3aa4c06`) cambió a `CreateAsyncScope` para soportar `IAsyncDisposable`.
2. **Todos los use cases scoped** se registran con `services.AddScoped<...>()` en `OpenScrape.Features.Services`.
3. **`SetPreflopActionUseCase` Singleton → Scoped** (bug fix DI memoria proyecto): dependía de scoped `ActionScenarioUseCases` y registraba como singleton. El contenedor lo capturaba como singleton y los scoped internos compartían la primera instancia → estado roto entre manos. Cambio a scoped resolvió.
4. **Servicios concretos de App** (`OcrService`, `RegionLookupCache`, `CardCacheService`) son **singletons** por diseño: cache de regiones / cartas comparte estado entre manos.
5. **Scoped pero stateful por mano:** `GameCoordinator`, `IPostflopContextHolder`. Reinicia state al iniciar nueva mano.
6. **Marten sessions usan `await using`** por operación, no long-lived. (`eaf5643 refactor(core): Fix memory leaks in OCR and DB sessions`).

## Alternativas consideradas

1. **Convertir todo a singleton.** Rechazado: rompería la coherencia transaccional de Marten (un `IDocumentSession` por scope) y mezclaría state entre manos para servicios scoped.
2. **Convertir use cases a transient.** Rechazado: cada use case crearía un nuevo `IDocumentSession` por inyección, multiplicando connections sin necesidad.
3. **Usar `IServiceScopeFactory` ad-hoc en `FrmMain`.** Rechazado: el ciclo de vida del form es la sesión de juego completa; un único scope alineado con el form es lo natural.
4. **DI manual sin contenedor (poor man's DI).** Rechazado: 30+ dependencias, demasiado boilerplate.
5. **Resolver `FrmMain` desde root y marcar use cases como singletons.** Rechazado: rompe el contrato de Marten y la spec arquitectónica.

## Consecuencias

**Positivas:**

- **Coherencia transaccional Marten:** un scope = una operación = una `IDocumentSession`.
- **Aislamiento entre manos** para servicios stateful por mano.
- **Cero instancias `new`** en `FrmMain` post-refactor (commit `0db7272 docs:`). Todo inyectado.
- `CreateAsyncScope` permite `IAsyncDisposable` para servicios que mantienen connections.

**Negativas:**

- **Acoplamiento del lifecycle del form al scope.** Cuando `FrmMain.FormClosing` se dispara, hay que disponer el scope manualmente. Riesgo de leak si se olvida.
- **Bug histórico:** `SetPreflopActionUseCase` registrado como Singleton dependiendo de Scoped fue un bug DI sutil (memoria proyecto: "Bugfix DI"). Ese tipo de errores reaparece si se añaden nuevos use cases sin cuidado.
- `FrmMain` con **30+ dependencias** en constructor — síntoma de "God object". El refactor (commit `343ad2a refactor(frmmain): extract pure helpers for test coverage`) saca pure helpers a servicios pequeños, pero `FrmMain.cs` sigue ≈ 4,244 LOC.

**Implicaciones para una migración:**

- Cualquier framework moderno (ASP.NET Core, Spring, NestJS) tiene scopes equivalentes. Migrar el patrón es directo.
- En frameworks sin DI, hay que reimplementar el ciclo manualmente — tedioso pero factible.
- La **deuda técnica** del `FrmMain` god object es independiente de esta decisión y persiste.

## Referencias

- `src/OpenScrape.App/Program.cs` — composition root.
- Commit `8c94e22 refactor/decision: Modularize postflop engine & services` — primer DI completo.
- Commit `3aa4c06` — switch a `CreateAsyncScope`.
- Memoria proyecto: "Bugfix DI: SetPreflopActionUseCase Singleton→Scoped".
- CLAUDE.md sección "DI pattern".
- ADR-0001 (Clean Architecture).
- ADR-0007 (`IPostflopContextHolder` scoped).
