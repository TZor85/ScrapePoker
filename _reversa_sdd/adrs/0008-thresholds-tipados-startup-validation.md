# ADR-0008 — `ThresholdsRegistry` tipado con startup validation, sin fallback hardcoded

- **Estado:** 🟢 ACEPTADO (vigente). Reemplaza dos fallbacks silenciosos previos.
- **Fecha:** 2026-04-21 (commit `3aa4c06 refactor(decision): typed thresholds registry with startup validation`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto + Claude Sonnet 4.6

## Contexto

`StreetThresholds` (fold below, thin value above, value above, strong value above, etc.) son consultados ~36 veces por mano: 4 streets × 9 hand situations + extras. Originalmente se almacenaban en un `Dictionary<string, StreetThresholds>` con clave `"{Street}_{HandSituation}"` (ej: `"Flop_OpenRaise"`).

Dos fallbacks silenciosos enmascaraban errores de configuración:

1. `StrategyProfileService.GetThresholds(...)` con un fallback hardcoded a defaults internos si la clave no existía.
2. `IPostflopDecisionService.GetThresholds(...)` exponía un fallback genérico también.

**Resultado:** un typo en `appsettings.json` (`Flop_Squeze` en vez de `Flop_Squeeze`) o una `HandSituation` añadida sin entrada correspondiente quedaba **invisible** — la app arrancaba, jugaba con thresholds genéricos por debajo del nivel óptimo y nadie se enteraba.

## Decisión

1. Crear **`ThresholdKey(BoardPosition Street, HandSituation Situation)`** value object con constructor que valida `Street ∈ {Flop, Turn, River}` y `Situation ≠ None`. `TryParse(string)` para back-compat con strings legacy.
2. Crear **`IThresholdsRegistry` (singleton)** con API `Get(ThresholdKey)`, `TryGet`, `Contains`, `Keys`. **Sin fallback** — `Get` lanza si la clave no existe.
3. Crear **`StrategyProfileValidator`** que en arranque verifica:
   - Las **36 combinaciones requeridas** (`{Flop,Turn,River} × {OpenRaise, ThreeBet, ...}`) están presentes.
   - Cada `StreetThresholds` cumple `FoldBelow < ThinValueAbove < ValueAbove < StrongValueAbove`.
4. Si `Validate()` retorna errores → lanzar `StrategyProfileValidationException` con lista acumulada → MessageBox → exit. **Fail-fast.**
5. Eliminar `GetThresholds` con fallback de `IPostflopDecisionService`. `PostflopDecisionService` ahora inyecta `IThresholdsRegistry` y consulta directo.
6. Switch del scope `Main` a `CreateAsyncScope` para soportar servicios scoped `IAsyncDisposable`.

## Alternativas consideradas

1. **Mantener fallback hardcoded.** Estado original. Rechazado: enmascara typos y oculta degradación silenciosa.
2. **Generar las 36 entradas en código (sin `appsettings.json`).** Rechazado: anula la flexibilidad de tunear sin recompilar — uno de los puntos clave del producto.
3. **Lazy generation con override.** "Si falta, crea una con defaults". Rechazado por la misma razón: oculta typos.
4. **Validación lazy en primera lectura.** Mejor que fallback silencioso, pero **más tarde**: el usuario juega varias manos antes de que se dispare. Rechazado en favor de fail-fast en arranque.
5. **Mantener clave string + parser en el lookup.** Rechazado: sin tipo fuerte, los typos siguen siendo sintácticos válidos.

## Consecuencias

**Positivas:**

- **Misconfiguraciones fallan en arranque** con mensaje accionable: "Falta clave: Flop_Squeeze en Thresholds. Existe: Flop_Squeze (¿typo?)".
- 28 tests nuevos cubren `ThresholdKey`, `ThresholdsRegistry`, `Validator` y la integración. Suite 1122/1122 verde tras refactor.
- API tipada elimina ~40% de strings hardcoded en el motor.
- `Contains` permite a tests preguntar coverage sin acoplar a una clave específica.

**Negativas:**

- **Cambios al `StrategyProfile.Thresholds` son ahora breaking** entre mayor versions. Mitigado con la spec `thresholds-registry/spec.md` documentando el contrato.
- Carga de `appsettings.json` lige­ramente más lenta (validación de 36 entradas). Despreciable.
- Si en el futuro se añade una nueva `HandSituation`, **falla arranque** hasta añadir su entrada en las 3 streets. Es el comportamiento deseado pero puede sorprender.

**Implicaciones para una migración:**

- Cualquier reimplementación debe mantener la **clave compuesta tipada** y la **validación al arrancar**. Sin esto, se reintroduce el bug.
- La especificación `openspec/specs/strategy-profile-validation/spec.md` describe el contrato.

## Referencias

- Commit `3aa4c06 refactor(decision): typed thresholds registry with startup validation`.
- `src/OpenScrape.DecisionMaker/Services/ThresholdsRegistry.cs`.
- `src/OpenScrape.Domain/ValueObjects/ThresholdKey.cs`.
- `openspec/specs/thresholds-registry/spec.md`.
- `openspec/specs/strategy-profile-validation/spec.md`.
- `openspec/specs/postflop-decision-api/spec.md` — refleja el cambio de superficie pública.
