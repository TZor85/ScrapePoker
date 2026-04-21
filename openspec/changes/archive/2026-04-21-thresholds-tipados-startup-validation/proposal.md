## Why

`PostflopDecisionService.GetThresholds` (líneas 38–58) y `StrategyProfileService.GetThresholds` (líneas 27–45) resuelven la configuración por clave string `"{Street}_{Situation}"` contra `StrategyProfile.Thresholds` (`Dictionary<string, StreetThresholds>`). Cuando la clave falta —por typo en `appsettings.json`, por un nuevo valor en los enums `BoardPosition`/`HandSituation`, o por olvido al añadir una situación— la función devuelve un fallback conservador hardcoded y sólo emite un `Console.WriteLine` informativo. El bot entra en producción con decisiones degradadas sin que nadie se entere hasta revisar logs manualmente, y la segunda copia del fallback en `StrategyProfileService` ni siquiera loguea.

Este patrón rompe tres invariantes deseables: (1) la configuración debería fallar rápido al arrancar, no silenciosamente a mitad de una mano; (2) las claves deberían validarse contra el dominio (enums), no aceptarse como texto libre; (3) el fallback debería ser una decisión explícita por situación, no un valor genérico disfrazado de "seguro".

## What Changes

- Introducir `ThresholdKey` — record tipado `(BoardPosition Street, HandSituation Situation)` con validación que rechaza `BoardPosition.None`, `BoardPosition.Hand`, `HandSituation.None` y `HandSituation.Call` (no aplican postflop).
- Introducir `IThresholdsRegistry` + `ThresholdsRegistry` — servicio singleton que encapsula el lookup tipado y expone `TryGet`, `Get` y `Contains` sobre `ThresholdKey`.
- Añadir `StrategyProfileValidator` que corre al startup (`IHostedService` o llamada directa tras `Build()` en `Program.cs`) y comprueba: (a) cada clave de `StrategyProfile.Thresholds` parsea a un `ThresholdKey` válido; (b) existe al menos una entrada por cada combinación `(Flop|Turn|River) × HandSituation postflop soportada`; (c) ningún valor de tiers es incoherente (`FoldBelow <= ThinValueAbove <= ValueAbove <= StrongValueAbove`).
- Reemplazar el acceso por string en `PostflopDecisionService.GetThresholds` y `StrategyProfileService.GetThresholds` por `IThresholdsRegistry.Get(ThresholdKey)`.
- Eliminar los dos bloques de fallback hardcoded. Si falta una clave tras la validación de arranque, lanzar excepción en runtime (garantía de que nunca ocurrirá si el validador pasó).
- **BREAKING** (interno): `StrategyProfile.Thresholds` queda como fuente de configuración cruda; el acceso en tiempo de ejecución pasa exclusivamente por `IThresholdsRegistry`. Usuarios de `_profile.Thresholds[...]` deben migrar.
- Tests: cobertura de `ThresholdKey` (construcción válida/inválida), `ThresholdsRegistry` (hit/miss), `StrategyProfileValidator` (todos los escenarios de error), y verificación de que `appsettings.json` actual pasa la validación.

## Capabilities

### New Capabilities

- `thresholds-registry`: Lookup tipado `(BoardPosition, HandSituation) → StreetThresholds` con garantía de completitud validada al arranque de la aplicación.
- `strategy-profile-validation`: Validación fail-fast del `StrategyProfile` cargado desde `appsettings.json` al iniciar el host, abortando el arranque si faltan claves, hay claves inválidas, o tiers incoherentes.

### Modified Capabilities

- `postflop-decision-api`: `PostflopDecisionService` deja de exponer `GetThresholds(BoardPosition, HandSituation)` con fallback silencioso; ahora delega en `IThresholdsRegistry` y el método público se elimina de la superficie pública (o retorna directamente sin fallback).

## Impact

- **Código afectado**:
  - `src/OpenScrape.Domain/ValueObjects/ThresholdKey.cs` (nuevo)
  - `src/OpenScrape.DecisionMaker/Interfaces/IThresholdsRegistry.cs` (nuevo)
  - `src/OpenScrape.DecisionMaker/Services/ThresholdsRegistry.cs` (nuevo)
  - `src/OpenScrape.App/Services/StrategyProfileValidator.cs` (nuevo)
  - `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs` (líneas 38–58: reemplazar)
  - `src/OpenScrape.App/Services/StrategyProfileService.cs` (líneas 27–45: reemplazar o eliminar método)
  - `src/OpenScrape.App/Program.cs` (registro DI + llamada a validación al arranque)
  - `src/OpenScrape.DecisionMaker/Services.cs` (registro de `IThresholdsRegistry` singleton)
- **Configuración**: `appsettings.json` no cambia de esquema — las claves `"Flop_OpenRaise"`, etc. siguen siendo válidas. Sólo cambia cómo se valida y consume.
- **Tests**: añadir ~15 tests nuevos (ThresholdKey, Registry, Validator). Los 638+ existentes deben seguir verdes; no cambia lógica de decisión.
- **Runtime behaviour**: un arranque con config incompleta ahora falla con mensaje explícito en vez de degradarse silenciosamente. Cero impacto en latencia del hot path (lookup O(1) en diccionario tipado vs O(1) en diccionario string).
- **Dependencias externas**: ninguna.
