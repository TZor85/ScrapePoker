## Why

`IPostflopDecisionService` expone dos overloads coexistentes de `DetermineAction`: el nuevo `DetermineAction(PostflopDecisionInput)` introducido en `decision-input` (archivado), y el viejo `DetermineAction(double equity, ..., 100+ params)` marcado `[Obsolete]`. Hoy ambos son necesarios porque:

1. El nuevo overload **delega internamente** al viejo envuelto en `#pragma warning disable CS0618` (`PostflopDecisionService.cs:85-108`). El wrapper es el único consumidor legítimo de la semántica antigua.
2. **~247 callers de tests** y **1 caller en producción** (`StrategyBacktester.ReplayDecision`) siguen usando el overload viejo directamente — genera **508 warnings CS0618** por compilación.
3. El método viejo tiene 41 parámetros posicionales; cualquier refactor mecánico en él rompe silenciosamente todos los callers por cambios de orden.

La convivencia multiplica superficie de API, oscurece qué camino está "aprobado", y deja el proyecto con cientos de warnings ruidosos que enmascaran problemas reales nuevos. Además, romper la dependencia interna del nuevo overload hacia el viejo simplifica la lógica: la implementación real vive en un único lugar.

Eliminar el overload obsoleto es un refactor mecánico con cobertura de tests ya existente, de bajo riesgo funcional pero alto valor de higiene de código.

## What Changes

- **BREAKING (interno)** Eliminar el método `DetermineAction(double equity, BoardPosition street, ..., RiverCardType riverCardType)` de `PostflopDecisionService` e `IPostflopDecisionService`. Toda llamada pasa por `DetermineAction(PostflopDecisionInput)`.
- Mover la lógica real de decisión (actualmente en el método viejo) al método que recibe `PostflopDecisionInput`, sin cambios de comportamiento. Eliminar el `#pragma warning disable CS0618` y el wrapping delegation.
- Migrar **1 caller de producción** (`StrategyBacktester.ReplayDecision` en `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs:90`) al nuevo overload construyendo un `PostflopDecisionInput` con los campos disponibles en `StreetDecision`/`HandRecord`.
- Migrar **~247 callers de tests** en tres archivos (`PostflopDecisionServiceTests.cs`, `PairClassificationTests.cs`, `RangePolarizerIntegrationTests.cs`) al nuevo overload. Script asistido: los parámetros nombrados se mapean 1:1 a propiedades `init` de `PostflopDecisionInput`.
- Verificar que los 849/849 tests siguen verdes con comportamiento idéntico (las decisiones emitidas deben ser byte-a-byte iguales salvo ruido MC).
- La compilación del solution debe producir **0 warnings CS0618** al terminar.

## Capabilities

### New Capabilities

- `postflop-decision-api`: documenta el contrato público consolidado de `IPostflopDecisionService` tras eliminar el overload obsoleto. Un único `DetermineAction(PostflopDecisionInput)` es el punto de entrada soportado; el resto de firmas (cálculos auxiliares como `CalculateDangerPenalty`, `CalculateImpliedOddsFactor`, `CalculateReverseImpliedOdds`, `GetThresholds`) permanecen expuestos. Se crea como capability nueva porque la anterior (`decision-input`) está archivada tras `refactoring-arquitectura`.

### Modified Capabilities

<!-- Ninguna capability existente en openspec/specs/ cambia sus requirements. -->


## Impact

- **Código afectado**:
  - `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` — eliminar método obsoleto (~480 LOC del método viejo), mover cuerpo al nuevo overload, eliminar `#pragma`.
  - `src/OpenScrape.DecisionMaker/Interfaces/IPostflopDecisionService.cs` — eliminar declaración del overload obsoleto.
  - `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs:90` — migrar 1 caller al nuevo overload.
  - `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs` — migrar ~228 callers.
  - `OpenScrape.App.Tests/PairClassificationTests.cs` — migrar 8 callers.
  - `OpenScrape.App.Tests/RangePolarizerIntegrationTests.cs` — migrar 11 callers.
- **APIs públicas**: sin cambios externos. `IPostflopDecisionService` es interno al ejecutable; no hay consumidores fuera del repo.
- **Tests**: 849 existentes deben seguir en verde. No se añaden tests funcionales nuevos (el refactor preserva comportamiento). Se añade 1 test dirigido que garantiza que la interfaz solo expone un único `DetermineAction` (guardrail contra regresiones).
- **Warnings**: 508 → 0 warnings `CS0618` en el build del solution.
- **LOC**: `PostflopDecisionService.cs` se reduce ~50 LOC (eliminación del wrapper y firmas duplicadas, el cuerpo se mueve no se duplica). `PostflopDecisionServiceTests.cs` crece ligeramente por la verbosidad del inicializador de `PostflopDecisionInput` (estimado +200 LOC) salvo que se extraiga un helper `MakeInput(...)` — decisión tomada en `design.md`.
- **Riesgos**: re-ordenar los parámetros del cuerpo viejo en su nuevo home podría introducir sutiles bugs si algún parámetro tiene default distinto entre el método viejo y la propiedad del record. Mitigado con suite completa de tests que hoy pasa.
