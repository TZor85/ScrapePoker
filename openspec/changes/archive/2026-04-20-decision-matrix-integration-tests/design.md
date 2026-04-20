## Context

Estado actual del motor de decisiones:
- `PostflopDecisionService` tiene 8+ ramas de decisión (FacingBet, CheckRaise, FloatExit, ProbeBet, PotControl, DelayedValue, LowEquity, NoBet, Cbet mixing).
- Suite de tests: 877 verdes, con **228+ tests** en `PostflopDecisionServiceTests` cubriendo escenarios concretos por rama.
- `DecisionIntegrationTests` ya existe (8 tests) pero verifica escenarios específicos, no una matriz.
- `StrategyProfile.Thresholds` cargado desde `appsettings.json` contiene 27 entradas: 9 `HandSituation` × 3 streets (`Flop`, `Turn`, `River`). Las `HandSituation` con threshold son: `OpenRaise`, `RaiseOverLimper`, `Call`, `ThreeBet`, `FourBet`, `Squeeze`, `VsSqueeze`, `DonkBet`, `DonkBetVsOpenRaise`. Las `HandSituation` restantes (`None`, `OpenRaiseVs3Bet`, `OpenRaiseVs3BetAndCall`, `Cold4Bet`, `LimpRaise`) no tienen threshold postflop configurado y caen al fallback genérico — se excluyen de la matriz.

El valor esperado del change: red de seguridad que detecta dos tipos de bug silencioso:
1. **Regresión dentro de una rama** — un refactor que rompe DonkBet en river pero sólo en OOP facing bet; test específico no existe, test unitario genérico no ejercita la combinación.
2. **Drift de configuración** — un typo que añade `"Flop_Squeze"` pasa el build porque `GetThresholds` cae al fallback; la matriz lo detecta comparando las claves esperadas con el diccionario cargado.

Restricciones:
- **No** modificar código de producción.
- **No** depender de Monte Carlo real (varianza no determinística); la equity se pasa como parámetro directo.
- **No** reimplementar la lógica del motor en los tests (violaría el principio del test black-box).
- Ejecución rápida: <5s para los 216 tests.

## Goals / Non-Goals

**Goals:**
- Cubrir sistemáticamente los 108 nodos `(3 streets × 9 situations × 2 positions × 2 bet states)` × 2 equity buckets = 216 casos.
- Propiedades invariantes direccionales que no dependan de valores exactos de thresholds.
- Guardrail que previene typos en claves de `StrategyProfile.Thresholds`.
- Mensaje de error informativo para cada caso fallido.

**Non-Goals:**
- **NO** cubrir combinaciones con villain profile detallado (TAG/LAG/TP/LP) — producto explota, se deja para follow-up.
- **NO** cubrir board texture (Dry/SemiDry/SemiWet/Wet/Monotone) en la matriz base — follow-up.
- **NO** arreglar bugs del motor que el test exponga. Si algún caso falla, se reporta en un change separado.
- **NO** asserts sobre el sizing específico (p.ej. `"Bet 1/2"` vs `"Bet 2/3"`) — frágil ante ajustes de thresholds. Sólo se verifica el tipo de acción (primera palabra).
- **NO** crear generadores de casos programáticos complejos (property-based testing, fuzzing) — queda para cambios futuros.

## Decisions

### D1: `[TestCaseSource]` con `IEnumerable<TestCaseData>` es el mecanismo

NUnit ofrece dos formas de parametrizar: `[TestCase(a, b, c)]` inline o `[TestCaseSource(nameof(source))]`. La matriz tiene 216 casos; inline sería inmanejable. El método source genera los casos con bucles anidados:

```csharp
public static IEnumerable<TestCaseData> MatrixCases()
{
    var streets = new[] { BoardPosition.Flop, BoardPosition.Turn, BoardPosition.River };
    var situations = new[] { HandSituation.OpenRaise, HandSituation.RaiseOverLimper, ... };
    var positions = new[] { true, false };
    var betSizes = new[] { BetSizeCategory.NoBet, BetSizeCategory.Medium };
    var equities = new[] { 20.0, 80.0 };

    foreach (var s in streets)
      foreach (var h in situations)
        foreach (var p in positions)
          foreach (var b in betSizes)
            foreach (var e in equities)
              yield return new TestCaseData(s, h, p, b, e)
                  .SetName($"Matrix({s},{h},IP={p},{b},Eq={e})");
}
```

**Alternativa rechazada**: generar `[Test]` separados con nombres explícitos. 216 métodos es inviable.

### D2: Equity se pasa directamente, MC no se invoca

Pasar equity numérica a `DetermineAction` es trivial — el método acepta `input.Equity = 80`. **No** se construyen cartas hero/board reales. Esto hace los tests:
- **Deterministas** (sin varianza de MC).
- **Rápidos** (sin simulaciones de 30K iteraciones).
- **Focalizados** en el motor, no en la pipeline upstream.

**Trade-off**: no testea la interacción con MC. Eso ya está cubierto en `MonteCarloSimulatorTests` y `EquityCalculatorServiceTests`. La separación de responsabilidades es intencional.

### D3: Propiedades invariantes se eligen por no depender de thresholds

Se escogen 4 propiedades que son consecuencias **matemáticas**, no reglas configurables:
1. No-bet ⇒ no-Call (no hay nada que callear).
2. Equity 80% sin facing bet ⇒ no-Fold (trivial, no hay coste).
3. Equity 80% facing bet ⇒ no-Fold (negativo en EV foldear equity alta facing bet Medium).
4. Equity 20% facing bet ⇒ no-Raise (negativo en EV raise con 20%, y el fold equity default del input neutral no compensa).

Estas propiedades son verdad para **cualquier valor razonable** de `FoldBelow` / `ThinValueAbove` / `StrongValueAbove`. Un motor correcto las cumple sin importar el tuning.

**Descartadas**: propiedades como "equity 80% ⇒ emite exactamente `"Bet 1/2"`" — depende del sizing específico, frágil.

### D4: `BetSizeCategory.Medium` representa facing bet; `NoBet` representa no facing bet

`BetSizeCategory ∈ {NoBet, Underbet, Small, Medium, Large}`. Para la matriz base se usan solo `NoBet` y `Medium` (representativo de facing bet estándar). Extensión a los 5 valores queda como follow-up; inflaría la matriz a 540 casos.

### D5: 9 situations con threshold, 5 excluidas

Incluidas (tienen `{Street}_{Situation}` en `appsettings.json`): `OpenRaise`, `RaiseOverLimper`, `Call`, `ThreeBet`, `FourBet`, `Squeeze`, `VsSqueeze`, `DonkBet`, `DonkBetVsOpenRaise`.

Excluidas: `None` (sentinel), `OpenRaiseVs3Bet`, `OpenRaiseVs3BetAndCall`, `Cold4Bet`, `LimpRaise`. No tienen threshold configurado; probar con ellas es probar el fallback genérico, no el motor. Se documenta como deuda para futuros changes si alguien añade thresholds.

### D6: Guardrail de claves existente como test separado

`Matrix_EachStreetThresholdsKeyHasEntry` es un test que **no** depende del `TestCaseSource`. Es un test simple que recorre las 27 combinaciones esperadas y verifica que `_profile.Thresholds.ContainsKey($"{street}_{situation}")`. Si alguien renombra una clave en `appsettings.json` o crea un typo, este test falla inmediatamente.

### D7: Reutilizar `StrategyProfile` cargado desde `appsettings.json` real

El test fixture carga el `StrategyProfile` real desde `appsettings.json` (no un mock). Si la configuración del repo drifta, el test refleja el estado real. Para conseguirlo, el fixture construye un `ConfigurationBuilder` con `AddJsonFile("appsettings.json", optional: false)` apuntando al fichero del proyecto `OpenScrape.App`.

**Alternativa rechazada**: usar un `StrategyProfile` construido en código con valores inventados. Pierde la capacidad de detectar drift de config.

### D8: Mensajes de error construidos con `Assert.That(..., Is...).WithMessage(...)`

Cada assert incluye contexto:

```csharp
Assert.That(action, Does.Not.StartWith("Fold"),
    $"Case ({street},{situation},IP={isInPosition},{villainBetSize},Eq={equity}): " +
    $"equity alta no-facing bet debe evitar Fold pero emitió '{action}'.");
```

NUnit muestra el mensaje en stdout al fallar. Permite diagnóstico sin re-ejecutar un caso concreto.

## Risks / Trade-offs

- **Riesgo: el test expone bugs reales del motor actual**. Estimación: 0-5 casos probablemente fallen (p.ej. un path de `DonkBetVsOpenRaise` en river OOP raro). Si ocurre:
  - Opción A: revertir el assert ofensivo si la propiedad invariante estaba mal formulada.
  - Opción B: `[TestCase(...)] [Ignore("Regresión conocida, ver change X")]` hasta que se arregle.
  - Opción C: arreglar el motor — **fuera del scope de este change**, se crea nuevo change.
  → **Mitigación**: el change se aplica con los 216 casos; si fallan, se documenta cada uno en `tasks.md` como "descubrimiento" y se decide (A/B/C) caso por caso.
- **Riesgo: los 216 casos ralentizan `dotnet test`** de ~2s a ~4s. Aceptable.
- **Riesgo: `ConfigurationBuilder` no encuentra `appsettings.json` en tiempo de test**. Los tests se ejecutan con cwd diferente del proyecto. **Mitigación**: construir la ruta absoluta con `Path.Combine(TestContext.CurrentContext.TestDirectory, ...)` o usar `Directory.GetCurrentDirectory()` + `../../../../src/OpenScrape.App/appsettings.json`. Probar ambos caminos en la fase de implementación.
- **Trade-off: la matriz excluye 5 `HandSituation` sin threshold**. Si alguien añade threshold para `OpenRaiseVs3Bet` en un change futuro, debe ampliar la matriz manualmente. Documentado en `tasks.md`.
- **Trade-off: las propiedades invariantes son "3 reglas direccionales"** — no cubren todos los casos de bug posibles. El valor es proporcional al número de bugs que detectan; no es coverage absoluto. Aceptable.

## Migration Plan

1. Crear el fixture `DecisionMatrixIntegrationTests.cs` con:
   - `SetUp` que carga `StrategyProfile` desde `appsettings.json` real.
   - Helper `MakeNeutralInput(...)` que produce un `PostflopDecisionInput` con defaults conservadores (sin combo draw, sin blockers, villain type Unknown, etc.).
   - `MatrixCases()` que yields los 216 `TestCaseData`.
   - `Matrix_ProducesValidDecision(...)` con los 4 asserts invariantes.
   - `Matrix_EachStreetThresholdsKeyHasEntry(...)` como test separado.
2. Ejecutar `dotnet test --filter DecisionMatrixIntegrationTests`. Si todo verde: cierre. Si hay rojos:
   - Listar en `tasks.md` cada caso fallido con action emitida + propiedad violada.
   - Decidir A/B/C (revertir assert, ignorar con issue, o abrir change para fix del motor).
3. Commit como refactor/test.

**Rollback**: revertir el commit. El change sólo añade tests, cero impacto en producción.

## Open Questions

- ¿Añadir el bucket de equity `Mid = 50.0` al matriz base? **Inclinación**: no — el caso es ambiguo (puede legítimamente dar Call, Bet o Fold según textura). Los dos buckets extremos (20, 80) generan más señal por caso.
- ¿Parametrizar también `HandRank` base? **Inclinación**: no en este change — se pasa `HighCard` en todos los casos. El equity se manipula directo.
- ¿El test debe tirar `Assert.Pass()` si los 216 pasan, o es suficiente con `[TestCaseSource]` sin cuerpo adicional? **Resuelto**: NUnit cuenta 216 tests verdes, uno por `TestCaseData`, no hace falta assert de cierre.
- ¿Extender el guardrail de claves a **todas las claves en `_profile.Thresholds`** (no solo las 27 esperadas)? **Inclinación**: no en este change — podría haber claves adicionales experimentales. Se limita a las 27 usadas por la matriz.
