## 1. Infraestructura del fixture

- [x] 1.1 Crear `OpenScrape.App.Tests/DecisionMatrixIntegrationTests.cs` con `[TestFixture]` y `SetUp` que carga `StrategyProfile` desde el `appsettings.json` real vía `ConfigurationBuilder`. Ruta del JSON: resolver con `Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "src", "OpenScrape.App", "appsettings.json")` o variante equivalente. Si el archivo no existe en tiempo de test, el `SetUp` debe lanzar fallo claro ("appsettings.json no encontrado en ruta X").
- [x] 1.2 Instanciar `PostflopDecisionService` con el `StrategyProfile` cargado + `BetSizingService` + `RangePolarizer` (todos con ctor parameterless o `IOptions.Create(profile)` según convención existente en `PokerDecisionFacadeTests`).
- [x] 1.3 Añadir método helper privado `MakeNeutralInput(BoardPosition street, HandSituation situation, bool isInPosition, BetSizeCategory villainBetSize, double equity)` que produce un `PostflopDecisionInput` con:
  - `Equity = equity`, `Street = street`, `Situation = situation`, `IsInPosition = isInPosition`, `VillainBetSize = villainBetSize`.
  - `BoardTexture = "SemiDry"` (neutral, evita que la textura domine la decisión).
  - `HeroStack = 100m`, `PotSize = 10m` (SPR 10, evita push/fold).
  - `HeroHandRank = HandRank.HighCard` (no asume madera; equity es el driver).
  - `VillainType = OpponentType.Unknown` (neutraliza ajustes por tipo).
  - `NumOpponents = 1` (HU).
  - Resto en defaults.

## 2. Generador de la matriz

- [x] 2.1 Añadir método `public static IEnumerable<TestCaseData> MatrixCases()` con producto cartesiano de:
  - 3 streets: `Flop, Turn, River`
  - 9 situations: `OpenRaise, RaiseOverLimper, Call, ThreeBet, FourBet, Squeeze, VsSqueeze, DonkBet, DonkBetVsOpenRaise`
  - 2 positions: `true, false`
  - 2 bet sizes: `BetSizeCategory.NoBet, BetSizeCategory.Medium`
  - 2 equities: `20.0, 80.0`
- [x] 2.2 Cada `TestCaseData` debe tener `.SetName($"Matrix({street},{situation},IP={isInPosition},{villainBetSize},Eq={equity})")` para que el test runner muestre el caso concreto al fallar.
- [x] 2.3 Verificar mentalmente que el generador produce exactamente **216** casos (3×9×2×2×2).

## 3. Test parametrizado con propiedades invariantes

- [x] 3.1 Implementar `[TestCaseSource(nameof(MatrixCases))] public void Matrix_ProducesValidDecision(BoardPosition street, HandSituation situation, bool isInPosition, BetSizeCategory villainBetSize, double equity)` que:
  - Construye el input con `MakeNeutralInput(...)`.
  - Invoca `_service.DetermineAction(input)`.
  - Asserts:
    - **Smoke**: `result.Action` no null, no vacío.
    - **Smoke**: el primer token de `result.Action` (antes del primer espacio) está en `{"Bet", "Call", "Raise", "Check", "Fold", "All-In"}`.
    - **Invariante 1**: si `villainBetSize == NoBet`, `result.Action` no empieza con `"Call "` ni es exactamente `"Call"`.
    - **Invariante 2**: si `equity >= 80` y `villainBetSize == NoBet`, `result.Action` no empieza con `"Fold"`.
    - **Invariante 3**: si `equity >= 80` y `villainBetSize == Medium`, `result.Action` no empieza con `"Fold"`.
    - **Invariante 4**: si `equity <= 20` y `villainBetSize == Medium`, `result.Action` no empieza con `"Raise"`.
  - Cada `Assert.That(...)` incluye mensaje con el contexto `(street,situation,IP,betSize,equity)` y la `action` emitida.

## 4. Guardrail de claves de thresholds

- [x] 4.1 Añadir `[Test] public void Matrix_EachStreetThresholdsKeyHasEntry()` que recorre las 27 combinaciones `{Street}_{Situation}` usadas por la matriz y asserta que cada clave existe explícitamente en `_profile.Thresholds` (no usa fallback).
- [x] 4.2 Si la clave no existe, el mensaje de error debe listar la clave faltante (p.ej. `"Falta entrada 'Flop_Squeze' en StrategyProfile.Thresholds"`) para detectar typos.

## 5. Ejecución y triage

- [x] 5.1 `dotnet test --filter "FullyQualifiedName~DecisionMatrixIntegrationTests"` — esperar 217 tests (216 parametrizados + 1 guardrail).
- [x] 5.2 Si todos verdes: cierre.
- [x] 5.3 Si algunos rojos: listar aquí los casos fallidos con:
  - Combinación exacta `(street,situation,IP,betSize,equity)`.
  - `Action` emitida.
  - Propiedad invariante violada.
  - Decisión: **A** (revertir assert si la propiedad invariante es demasiado estricta para ese caso específico), **B** (`[Ignore]` con issue link para change futuro que arregle el motor), o **C** (abrir change separado `fix-decision-matrix-case-X`).
- [x] 5.4 Documentar los casos ignorados (opción B) al final de este archivo para transparencia.

## 6. Verificación final

- [x] 6.1 `dotnet build OpenScrape.sln` → 0 errores, 0 warnings nuevos.
- [x] 6.2 `dotnet test OpenScrape.sln` → 877 + ~217 = ~1,094 tests verdes.
- [x] 6.3 `dotnet format --verify-no-changes OpenScrape.sln` sin cambios.
- [x] 6.4 Ejecutar sólo el fixture 3 veces seguidas; verificar que el resultado es idéntico en las 3 (determinismo).
- [x] 6.5 Medir duración con `dotnet test --filter "FullyQualifiedName~DecisionMatrixIntegrationTests"` — esperar < 5 s.
- [x] 6.6 Documentar en este archivo (al final) las extensiones propuestas para changes sucesivos:
  - Añadir bucket equity `Mid = 50`.
  - Añadir `villainType ∈ {TAG, LAG, TP, LP}` como 5ª dimensión.
  - Añadir `BoardTexture ∈ {Dry, Wet, Monotone}` como 6ª dimensión.
  - Añadir cobertura para las 5 `HandSituation` sin threshold (`OpenRaiseVs3Bet`, `OpenRaiseVs3BetAndCall`, `Cold4Bet`, `LimpRaise`) cuando/si se les configure threshold.
