## Why

La suite tiene **877 tests** con excelente cobertura de caja blanca sobre el motor de decisiones (228+ tests en `PostflopDecisionServiceTests` que verifican ramas específicas). Sin embargo, **la matriz sistemática `street × situation × position × bet state` no está cubierta**: tests individuales verifican "C-bet con TAG en flop dry" o "Check-raise OOP con combo draw", pero **no hay una red de seguridad que recorra los 108 nodos de la matriz y asegure que cada combinación emite una decisión coherente**.

Concretamente:
- `appsettings.json` declara **27 `StreetThresholds`** (9 `HandSituation` × 3 streets postflop: `Flop`, `Turn`, `River`).
- Cada uno debe comportarse correctamente para `IsInPosition ∈ {true, false}` × `VillainBetSize ∈ {NoBet, Medium}` × `Equity ∈ {Low=20%, Mid=50%, High=80%}` = 108 × 3 = **324 casos**. Acotando a 2 buckets de equity (low/high) = **216 casos**.
- Con el pipeline actual:
  - Un typo que añade un threshold nuevo como `"Flop_Squeze"` (sin `e`) pasa el build — `GetThresholds` cae a fallback silencioso.
  - Un cambio en `PostflopDecisionService` que rompe un solo path (p.ej. `DonkBetVsOpenRaise` en river) no se detecta si ningún test unitario lo ejercita exactamente.
  - Añadir una nueva `HandSituation` sin los 3 thresholds correspondientes no falla nada hasta que se juega en producción.

Este change añade un **matrix coverage test** parametrizado vía `[TestCaseSource]` que recorre los 216 casos, invoca `DetermineAction` con inputs válidos y verifica propiedades direccionales (sin reimplementar la lógica). Rompe la regla "la matriz nunca estuvo auditada" con un coste de mantenimiento bajo: los tests no asumen acciones específicas, sólo propiedades invariantes (p.ej. equity 80% sin facing bet nunca debe devolver `Fold`).

## What Changes

- Añadir `OpenScrape.App.Tests/DecisionMatrixIntegrationTests.cs` con:
  - **`MatrixCases()`** (método `[TestCaseSource]`) que emite un `IEnumerable<TestCaseData>` con todas las combinaciones `(street, situation, isInPosition, villainBetSize, equityBucket)`. Tamaño: **216 casos**.
  - **`Matrix_ProducesValidDecision(...)`** (test parametrizado) que para cada caso:
    1. Construye un `PostflopDecisionInput` mínimo consistente con los inputs.
    2. Invoca `PostflopDecisionService.DetermineAction(input)`.
    3. Verifica **smoke** (no excepción, `Action` no vacío, string de acción en un set conocido: `"Bet", "Call", "Raise", "Check", "Fold", "All-In"`, posiblemente con sizing suffix).
    4. Verifica **invariantes direccionales** (véase detalle en `specs/`): equity alta sin facing bet nunca es `Fold`; equity muy baja facing bet grande nunca es `Raise`.
- Añadir `Matrix_EachStreetThresholdsKeyHasEntry(...)` que verifica que **las 27 claves `{Street}_{Situation}` usadas por el test tienen entrada real en `StrategyProfile.Thresholds`** (sin caer al fallback genérico).
- Añadir `Matrix_NoSilentFallbacksInMainSituations(...)` que con cada `(BoardPosition postflop, HandSituation)` llama a `GetThresholds` y asserta que la clave existe explícitamente en el diccionario cargado desde `appsettings.json`.
- Documentar en `tasks.md` las propiedades invariantes exactas que el test verifica, para que queden disponibles como contrato.

## Capabilities

### New Capabilities

- `decision-matrix-coverage`: garantías de cobertura sistemática del motor de decisiones postflop a nivel de matriz `street × situation × position × bet state × equity bucket`, con aserciones direccionales e independientes de los valores exactos de los thresholds.

### Modified Capabilities

<!-- Ninguna capability existente cambia sus requirements. -->

## Impact

- **Código afectado**:
  - Nuevo: `OpenScrape.App.Tests/DecisionMatrixIntegrationTests.cs` (~400 LOC estimadas, incluye fixture + generador de casos + 3 tests parametrizados).
  - No se modifica código de producción.
  - Opcional: utilidad `TestMakeInputHelper.cs` (ya existe) puede crecer con un método de conveniencia `MakeNeutralInput(street, situation, isInPosition, villainBetSize, equity)` para reducir boilerplate.
- **APIs públicas**: sin cambios.
- **Tests**: 877 actuales → **877 + ~216 nuevos = ~1,093 tests verdes** (si todas las propiedades invariantes se cumplen). Si algún caso rompe, el test falla y señala el bug — ése es el valor.
- **Performance**: `dotnet test` puede pasar de ~2s a ~4s por los 216 tests extra. Aceptable: cada caso construye un `PostflopDecisionInput`, invoca `DetermineAction` (sin MC real, equity se pasa directa), y verifica aserts baratos.
- **Riesgo descubrimiento**: es probable que algunos de los 216 casos **ya estén rotos hoy** (p.ej. `DonkBet` en flop sin fallback correcto para OOP sin facing bet). En tal caso:
  - Si la propiedad invariante es correcta y el motor es incorrecto → fix del motor en otro change.
  - Si el motor es correcto y la propiedad invariante es demasiado estricta → relajar la invariante.
  - **El change NO arregla bugs del motor**; sólo los expone con cobertura.
- **Deuda posterior**: los tests por villainType y por board texture (Dry/Wet/Monotone) siguen pendientes. Esta matriz es el esqueleto; extensiones en changes sucesivos.
