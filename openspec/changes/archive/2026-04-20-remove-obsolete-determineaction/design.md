## Context

Estado actual de `PostflopDecisionService`:

- **Archivo**: `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` (1,936 LOC)
- **Dos overloads públicos de `DetermineAction`**:
  - Nuevo (líneas 83-109): recibe `PostflopDecisionInput`, es thin wrapper que delega al viejo via `#pragma warning disable CS0618`.
  - Viejo (líneas 114-596+): recibe 41 parámetros posicionales, marcado `[Obsolete]`. Contiene el algoritmo real (~480 LOC de lógica de decisión: facing bet, check-raise, float exit, probe bet, pot control, delayed value, low equity, randomización, c-bet).
- **Interfaz `IPostflopDecisionService`** declara ambos.
- **508 warnings `CS0618`** en compilación del solution, principalmente desde tests.

Callers actuales del overload viejo:
- `PostflopDecisionServiceTests.cs`: **~228 callers** (tests de decisiones postflop comprehensivos, cada uno con 10-25 parámetros nombrados).
- `PairClassificationTests.cs`: **8 callers**.
- `RangePolarizerIntegrationTests.cs`: **11 callers**.
- `StrategyBacktester.ReplayDecision` en `src/OpenScrape.DecisionMaker/Services/StrategyBacktester.cs:90`: **1 caller** de producción (con 10 parámetros nombrados).
- **Total**: ~248 callers.

`PostflopDecisionInput` (record con ~40 propiedades, 6 `required`) ya existe en `src/OpenScrape.DecisionMaker/DTOs/PostflopDecisionInput.cs` y mapea 1:1 los parámetros del overload viejo. La migración es mecánica: `DetermineAction(equity: X, street: Y, ...)` → `DetermineAction(new PostflopDecisionInput { Equity = X, Street = Y, ... })`.

Restricciones: `OpenScrape.DecisionMaker` no puede referenciar `OpenScrape.App`; `PostflopDecisionInput` está en DecisionMaker así que es accesible desde todos los consumers. No hay SDK ni paquete externo que dependa de la firma vieja.

## Goals / Non-Goals

**Goals:**
- Eliminar el overload obsoleto del código y de la interfaz.
- Mover el cuerpo real al método que acepta `PostflopDecisionInput` (sin duplicación).
- Quitar `#pragma warning disable CS0618` y el `restore`.
- Migrar el único caller de producción (`StrategyBacktester`) y los ~247 de tests.
- Build con **0 warnings CS0618** en el solution.
- 849/849 tests siguen verdes sin cambios funcionales.

**Non-Goals:**
- **NO** cambiar la semántica de decisión ni ajustar thresholds.
- **NO** refactorizar el algoritmo de decisión en sí (los 8+ decision paths permanecen intactos).
- **NO** introducir nuevas reglas de decisión, tests nuevos ni cobertura adicional más allá del guardrail mínimo.
- **NO** tocar `PostflopDecisionInput` (su contrato es estable; solo se consume).
- **NO** cambiar la signatura de `PostflopDecisionResult`.

## Decisions

### D1: Mover el cuerpo al método que acepta `PostflopDecisionInput`

El cuerpo actual de ~480 LOC vive en el método viejo. Opciones:

- **(a)** Dejar el cuerpo donde está y cambiar la firma del método viejo a `DetermineAction(PostflopDecisionInput input)`, renombrar parámetros internos.
- **(b)** Mover el cuerpo al método nuevo (que hoy delega) y eliminar el viejo.

**Elección: (b)**. Conceptualmente el método "nuevo" es el público soportado; queda natural que contenga la lógica. Cambio de diff más grande pero único commit coherente. Permite renombrar variables internas (`equity`, `street`, ...) a `input.Equity`, `input.Street`, ... en un paso.

**Alternativa considerada**: mantener el viejo con un nombre `DetermineActionCore` privado. Añade superficie innecesaria; rechazado.

### D2: Helper `MakeInput(...)` por fixture

Migrar 247 callers sin helper implica copy-paste masivo del patrón `new PostflopDecisionInput { Equity = X, Street = Y, Situation = Z, ... }`. Los tests existentes ya usan parámetros nombrados; mantener legibilidad es crítico.

**Elección**: introducir por fixture un método privado estático `MakeInput(double equity, BoardPosition street, HandSituation situation, ..., params con defaults...)` que construya el record. Los tests pasan de:

```csharp
DetermineAction(equity: 50, street: BoardPosition.Flop, situation: HandSituation.OpenRaise,
                boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet)
```

A:

```csharp
DetermineAction(MakeInput(equity: 50, street: BoardPosition.Flop, situation: HandSituation.OpenRaise,
                          boardTexture: "Dry", isInPosition: true, villainBetSize: BetSizeCategory.NoBet))
```

**Ventaja**: diff mínimo por caller (solo envolver en `MakeInput(...)` y cambiar llamada). La firma de `MakeInput` replica la del overload viejo con los mismos nombres, así el search-and-replace es seguro.

**Alternativa**: inicializadores de objeto directos (`new PostflopDecisionInput { ... }`). Más verboso y menos compatible con los nombres de parámetro actuales (`equity` vs `Equity`). Rechazado como default.

### D3: Estrategia de migración de tests: asistida por regex, validada por build

El patrón a transformar es estable. Plan:

1. **Fase 2** — introducir `MakeInput` helper en cada fixture afectado.
2. **Fase 3** — reemplazo asistido: script/regex que localiza `DetermineAction\(` y lo envuelve en `MakeInput(`, ajustando el cierre de paréntesis. La ambigüedad (algunos callers ya usan el nuevo overload) se resuelve por contenido: si el primer argumento es `new PostflopDecisionInput` u otra expresión no-nombrada que retorna el record, saltar.
3. **Fase 4** — compilar. Cualquier ambigüedad residual aparece como error de compilación y se corrige a mano.
4. **Fase 5** — ejecutar tests. Si algún test falla con mensaje de mismatch de defaults (p.ej. un parámetro que el viejo no pasaba y el record defaultea a otro valor), corregir el default en `MakeInput` o el caller.

**Alternativa rechazada**: migración manual una a una. ~247 callers × 1 min = 4h de trabajo repetitivo con alta probabilidad de errores de typo. La vía asistida es más segura y ~10× más rápida.

### D4: `StrategyBacktester.ReplayDecision` se migra directamente sin helper

El caller de producción es uno solo. El helper `MakeInput` solo se añade a los fixtures de test. `StrategyBacktester` usa `new PostflopDecisionInput { ... }` directamente para no introducir un helper de un solo uso en el código productivo.

### D5: Guardrail de regresión: test que verifica el contrato vía reflection

Añadir **un** test nuevo `PostflopDecisionApiContractTests.IPostflopDecisionService_ExposesSingleDetermineAction()` que:

1. Obtiene todos los miembros de `IPostflopDecisionService` vía reflection.
2. Asserta que exactamente uno se llama `DetermineAction`.
3. Asserta que su firma es `(PostflopDecisionInput) → PostflopDecisionResult`.
4. Asserta que ningún miembro tiene `ObsoleteAttribute`.

Previene futura reintroducción accidental del overload obsoleto.

### D6: Orden de cambios en el mismo PR

Todo el refactor va en un solo PR / commit atómico:
- Split del cuerpo del método viejo y eliminación.
- Migración del caller de producción.
- Migración de los 247 tests.
- Guardrail.

Opción descartada: PR por fase. Partir la migración genera un estado intermedio con el `#pragma` aún en vigor y parte de los tests migrados — inútil para revisión. Prefiero un único commit verificable por `dotnet build` + `dotnet test`.

## Risks / Trade-offs

- **Riesgo: algún parámetro del overload viejo tiene default distinto al `PostflopDecisionInput`**. Ejemplo: `effectiveOuts = 0` en el viejo vs `EffectiveOuts = 0` default en el record; pero el wrapper actual pasa `effectiveOuts: input.EffectiveOuts > 0 ? input.EffectiveOuts : input.TotalOuts`. Al eliminar el wrapper, esa lógica de fallback desaparece.
  → **Mitigación**: auditar el wrapper (`PostflopDecisionService.cs:106`) antes de eliminarlo. El fallback `EffectiveOuts > 0 ? EffectiveOuts : TotalOuts` se mueve al cuerpo del método nuevo (o a `PostflopDecisionInput` como propiedad calculada). Documentar en `tasks.md`.
- **Riesgo: tests ocultos que prueban el comportamiento obsoleto explícitamente**. Ej: un test que verifica que `[Obsolete]` está aplicado.
  → **Mitigación**: el guardrail (D5) lo detecta invertido. Si existe, se borra como parte del refactor.
- **Trade-off: el helper `MakeInput` en fixtures introduce código auxiliar**. Ganancia en legibilidad y diff estable compensa. Alternativamente podría ponerse un helper compartido en una base class común de tests, pero los fixtures actuales no la tienen.
- **Riesgo: regex de migración produce falsos positivos que compilan pero cambian semántica**. Muy improbable dado el patrón restringido, pero el test suite es el safety net.
- **Coste de revisión del PR**: ~248 callers migrados. El diff es grande pero extremadamente repetitivo. Revisión efectiva: mirar el helper `MakeInput`, 3 o 4 callers migrados como muestra, y confiar en el build + tests para el resto.

## Migration Plan

1. **Auditar el wrapper actual** (`PostflopDecisionService.cs:85-108`) y listar cualquier transformación de datos que hace al delegar (sabemos de `effectiveOuts` fallback). Preservarla en el cuerpo nuevo.
2. **Mover el cuerpo** del método viejo al método `DetermineAction(PostflopDecisionInput)`. Renombrar todas las referencias locales a `input.*`. Eliminar `#pragma`.
3. **Eliminar** la declaración obsoleta de `IPostflopDecisionService` y el método obsoleto de `PostflopDecisionService`.
4. **Migrar `StrategyBacktester.ReplayDecision`** construyendo `new PostflopDecisionInput { Equity = ..., Street = ..., ... }`.
5. **Añadir `MakeInput` helper** a los tres fixtures afectados.
6. **Migración asistida de tests** (regex + build + revisión).
7. **Añadir guardrail** `PostflopDecisionApiContractTests`.
8. **Ejecutar `dotnet format`** para limpieza final.
9. **`dotnet build`** → 0 warnings CS0618.
10. **`dotnet test`** → 849 + 1 nuevo = 850 tests verdes.

**Rollback**: si los tests fallan tras la migración de forma no trivial, revertir el commit y migrar en batches más pequeños (p.ej. un fixture por commit).

## Open Questions

- ¿El fallback `EffectiveOuts > 0 ? EffectiveOuts : TotalOuts` debe quedar como lógica del cuerpo del método o como propiedad calculada `PostflopDecisionInput.EffectiveOutsOrTotal`? **Inclinación**: mover al cuerpo del método para no crecer `PostflopDecisionInput` (la lógica de fallback es del algoritmo, no del dato).
- ¿`MakeInput` debe vivir en cada fixture o en una clase `TestHelpers.PostflopInputs` compartida? **Inclinación**: por fixture al principio (aislamiento); si aparece duplicación evidente, extraer a helper compartido como follow-up.
- ¿Vale la pena tocar los tests del `decision-engine-v2` archivado que pudieran seguir compilando por InternalsVisibleTo? **Resuelto**: no aplica, el archivo está archivado sin código vivo.
