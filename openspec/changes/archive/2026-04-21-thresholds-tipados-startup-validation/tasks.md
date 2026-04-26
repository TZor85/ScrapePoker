## 1. Auditoría y preparación

- [x] 1.1 Enumerar todas las claves `"{Street}_{Situation}"` presentes en `src/OpenScrape.App/appsettings.json`: **36 entradas — cobertura completa** `{Flop, Turn, River} × {OpenRaise, Call, RaiseOverLimper, ThreeBet, OpenRaiseVs3Bet, OpenRaiseVs3BetAndCall, FourBet, Cold4Bet, Squeeze, VsSqueeze, DonkBet, DonkBetVsOpenRaise}`.
- [x] 1.2 Call-sites actuales de `GetThresholds`: (a) `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs:132` (uso interno propio), (b) tests `OpenScrape.App.Tests/PostflopDecisionServiceTests.cs:297,306,2076`, (c) tests `OpenScrape.App.Tests/StrategyProfileTests.cs:79,90,99,109,118,126,134,168,177`, (d) tests `OpenScrape.App.Tests/DecisionIntegrationTests.cs:55,78,103,129,153,164,206,207`. No hay consumidores de producción fuera de `PostflopDecisionService` mismo.
- [x] 1.3 Lista obligatoria definitiva: las 36 combinaciones cubiertas actualmente por `appsettings.json`. `HandSituation.Call` se incluye como situación postflop válida. `HandSituation.LimpRaise` queda fuera.
- [x] 1.4 No se requieren entradas nuevas — `appsettings.json` ya cubre las 36. Paso ejecutado automáticamente durante 1.1.

## 2. Dominio: ThresholdKey

- [x] 2.1 Crear `src/OpenScrape.Domain/ValueObjects/ThresholdKey.cs` con `public record ThresholdKey(BoardPosition Street, HandSituation Situation)` y validación en constructor (rechaza `Street ∈ {None, Hand}`, `Situation = None`; `Call` es válido).
- [x] 2.2 Implementar `ToString()` override en `ThresholdKey` que devuelve `$"{Street}_{Situation}"`.
- [x] 2.3 Crear `OpenScrape.App.Tests/ThresholdKeyTests.cs` cubriendo: construcción válida, rechazo `Street=None`, rechazo `Street=Hand`, rechazo `Situation=None`, aceptación `Situation=Call`, igualdad por valor, `ToString()` formato.
- [x] 2.4 Ejecutar `dotnet test --filter "FullyQualifiedName~ThresholdKey"` → 8/8 verde.

## 3. DecisionMaker: IThresholdsRegistry

- [x] 3.1 Crear `src/OpenScrape.DecisionMaker/Interfaces/IThresholdsRegistry.cs` con `Get(ThresholdKey)`, `TryGet(ThresholdKey, out StreetThresholds)`, `Contains(ThresholdKey)`, `IReadOnlyCollection<ThresholdKey> Keys`.
- [x] 3.2 Crear `src/OpenScrape.DecisionMaker/Services/ThresholdsRegistry.cs` que implementa la interfaz. Constructor recibe `IOptions<StrategyProfile>`, parsea cada string a `ThresholdKey` mediante split por `_` y `Enum.Parse`, y construye un `Dictionary<ThresholdKey, StreetThresholds>` interno. Lanza `InvalidOperationException` con mensaje explícito si alguna clave string no parsea.
- [x] 3.3 Registrar el singleton en `src/OpenScrape.App/Program.cs` (no existe `AddDecisionMaker` central): `services.AddSingleton<ThresholdsRegistry>()` + forwarding a `IThresholdsRegistry`, inmediatamente después de `services.AddSingleton<StrategyProfileService>()`.
- [x] 3.4 Crear `OpenScrape.App.Tests/ThresholdsRegistryTests.cs` con 11 tests cubriendo: lookup exitoso, `TryGet` miss no lanza, `Get` miss lanza `KeyNotFoundException` con mensaje que incluye la clave string, constructor con 4 tipos de clave inválida lanza `InvalidOperationException`, singleton resuelto por DI retorna misma instancia.
- [x] 3.5 Ejecutar `dotnet test --filter "FullyQualifiedName~ThresholdsRegistry"` → 11/11 verde.

## 4. App: StrategyProfileValidator

- [x] 4.1 Crear `src/OpenScrape.Domain/Exceptions/StrategyProfileValidationException.cs` heredando de `Exception`, expone `IReadOnlyList<string> Errors` y construye mensaje multi-línea a partir de la lista.
- [x] 4.2 Crear `src/OpenScrape.App/Services/StrategyProfileValidator.cs` con método estático `Validate(StrategyProfile profile)`. Acumula errores y lanza `StrategyProfileValidationException` al final si hay alguno.
- [x] 4.3 Parseo de cada clave string — delegado a `ThresholdKey.TryParse` (movido al value object como método estático público por cross-assembly visibility).
- [x] 4.4 Comprobación de cobertura — 12 situaciones × 3 streets = 36 combinaciones obligatorias.
- [x] 4.5 Comprobación de tiers — rango `[0, 100]` y monotonía `FoldBelow ≤ ThinValueAbove ≤ ValueAbove ≤ StrongValueAbove`.
- [x] 4.6 Crear `OpenScrape.App.Tests/StrategyProfileValidatorTests.cs` con 8 tests cubriendo todos los escenarios del spec.
- [x] 4.7 Crear `OpenScrape.App.Tests/StrategyProfileValidatorIntegrationTests.cs` que carga `src/OpenScrape.App/appsettings.json` real vía `ConfigurationBuilder` y verifica que el validador no lanza.
- [x] 4.8 Ejecutar `dotnet test --filter "FullyQualifiedName~StrategyProfileValidator"` → 9/9 verde.

## 5. Integración en arranque

- [x] 5.1 Modificar `src/OpenScrape.App/Program.cs`: tras `var host = builder.Build();`, resolver `IOptions<StrategyProfile>`, llamar a `StrategyProfileValidator.Validate(options.Value)` dentro de un try/catch. Build verde tras el cambio.
- [x] 5.2 En el catch de `StrategyProfileValidationException`: mostrar `MessageBox.Show(ex.Message, "Error de configuración", MessageBoxButtons.OK, MessageBoxIcon.Error)` y luego `Environment.Exit(1)`.
- [ ] 5.3 *(Manual, pendiente del usuario)* Arranque manual: iniciar la app con config correcta y verificar que `FrmMain` se abre sin incidencias.
- [ ] 5.4 *(Manual, pendiente del usuario)* Arranque manual: introducir temporalmente un typo en una clave de `appsettings.json`, iniciar la app, verificar que aparece `MessageBox` con mensaje descriptivo y el proceso termina. Restaurar `appsettings.json`.

## 6. Migración de PostflopDecisionService

- [x] 6.1 Añadir dependencia `IThresholdsRegistry` al constructor de `PostflopDecisionService` (junto a los existentes `IOptions<StrategyProfile>`, `BetSizingService`, `RangePolarizer`).
- [x] 6.2 `GetThresholds` pasa a ser privado y delega en `_thresholdsRegistry.Get(new ThresholdKey(...))`. Eliminado de `IPostflopDecisionService` la exposición pública del método.
- [x] 6.3 Eliminado `Console.WriteLine` de warning y el bloque de fallback hardcoded.
- [x] 6.4 Grep `$"{...}_{...}"` y `Thresholds[` en `src/OpenScrape.DecisionMaker`: cero hits tras la migración.
- [x] 6.5 Suite completa tras la migración: `dotnet test OpenScrape.sln` → **1122/1122 verde**.

## 7. Migración de StrategyProfileService y tests existentes

- [x] 7.1 Eliminar `StrategyProfileService.GetThresholds(BoardPosition, HandSituation)` y el fallback hardcoded.
- [x] 7.2 Para cada call-site encontrado en 1.2, migrar al registry. Los call-sites eran todos en tests; no había consumidores de producción distintos de `PostflopDecisionService` mismo.
- [x] 7.3 `StrategyProfileService` sigue existiendo — expone `Profile` y `GetBluffFrequency`. La limpieza adicional queda fuera del scope de este change.
- [x] 7.4 Migrar `StrategyProfileTests.cs`: añadir `_registry`, reemplazar `_service.GetThresholds(...)` por `GetThresholds(...)` helper que usa el registry. Reescribir `GetThresholds_KeyInexistente_DeberiaLanzarKeyNotFoundException`.
- [x] 7.5 Migrar `PostflopDecisionServiceTests.cs`: renombrar `GetThresholds_Existente_*` y `GetThresholds_NoExistente_*` → `Registry_*` que construyen un registry sin `FillMissingThresholds`. Migrar uso en línea 2077 al registry.
- [x] 7.6 Migrar `DecisionIntegrationTests.cs`: reemplazar `_strategyService` por `_registry` y helper local `GetThresholds`.
- [x] 7.7 Añadir `TestProfileDefaults.FillMissingThresholds()` extension para cubrir las 36 combinaciones en perfiles de test parciales (evita que tests downstream rompan por fallback desaparecido).
- [x] 7.8 Aplicar `FillMissingThresholds()` en 5 setups de test: PostflopDecisionServiceTests, PairClassificationTests, PokerDecisionFacadeTests, StrategyBacktesterTests, RangePolarizerIntegrationTests.
- [x] 7.9 `dotnet build OpenScrape.sln` → 0 errores; `dotnet test OpenScrape.sln` → 1122/1122 verde.

## 8. Verificación final

- [x] 8.1 `dotnet format --verify-no-changes OpenScrape.sln` pasa sin diffs (exit 0).
- [x] 8.2 `dotnet build OpenScrape.sln --configuration Release` → 0 errores.
- [x] 8.3 `dotnet test OpenScrape.sln` → 1122/1122 verde (los 28 nuevos tests de este change + 1094 preexistentes).
- [ ] 8.4 *(Manual, pendiente del usuario)* Arranque manual de la app: overlay se abre, FrmMain se abre, los tiers se pintan correctamente en todas las pestañas.
- [x] 8.5 `openspec validate thresholds-tipados-startup-validation` → "is valid".
- [ ] 8.6 *(Pendiente cuando el usuario pida el commit)* Redactar commit message conventional en castellano (`refactor(decision): introducir ThresholdsRegistry tipado + validación al arranque`).
