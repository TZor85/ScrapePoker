# Proposal: Instrumentation Integration Tests

## Capability
`telemetry-instrumentation` (modified)

## Why
Sin tests de integración, los `Measure(category)` pueden quedar sin invocarse silenciosamente al refactorizar. Necesitamos verificación end-to-end para asegurar que todas las categorías de telemetría del pipeline de decisión se miden correctamente.

## What Changes
2 tests que disparan un ciclo de decisión completo y verifican que las categorías esperadas tienen:
- Count > 0 (presencia)
- Count exacto donde aplique (subfases específicas)

## Impact
- **Archivo nuevo**: `OpenScrape.App.Tests/Telemetry/TelemetryInstrumentationIntegrationTests.cs`
- **Dependencia**: `PokerDecisionFacade` usa 6 categorías de decisión que deben verificarse

## Capabilities involved
- `IMetricsCollector`
- `PokerDecisionFacade.EvaluateAsync`
- `TelemetryCategories`

## Acceptance Criteria

### AC1: Decisión postflop dispara categorías Decision*
- **Dado** un `PokerDecisionFacade` con `IMetricsCollector` real
- **Cuando** se ejecuta `EvaluateAsync` con escenario postflop
- **Entonces** las categorías `DecisionTotal`, `DecisionEquity`, `DecisionTexture`, `DecisionProfile`, `DecisionDecisionService`, `DecisionSizing` aparecen en el snapshot con Count > 0

### AC2: Count exacto por subfase
- **Dado** un ciclo de decisión completo
- **Cuando** se completan todas las subfases (equity, texture, profile, decision, sizing)
- **Entonces** el Count de cada subfase es exacto (no se compara Total vs subfases por granularidad)

### AC3: Reusabilidad
- **Cuando** se crean los tests
- **Entonces** usar `PokerDecisionFacadeTestBuilder` si existe, o crearlo para reutilización futura