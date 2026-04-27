# Tasks: Instrumentation Integration Tests

## Task 1: Localizar o crear PokerDecisionFacadeTestBuilder

**Subtarea 1a: Buscar test builder existente**
- Buscar en el proyecto de tests `Pokerdecisionfacade*builder*` o similar
- Si existe, documentar ubicación para reuse

**Subtarea 1b: Si no existe, crear PokerDecisionFacadeTestBuilder**
- Ubicación: `OpenScrape.App.Tests/PokerDecisionFacadeTestBuilder.cs`
- Configurar `IPokerCalculator` mockeado (usa implementations reales de poker)
- Configurar `IMetricsCollector` real (InMemoryMetricsCollector)
- Configurar otros servicios: `IPostflopDecisionService`, `IBetSizingService`, `IBoardTextureAnalyzer`, `IOpponentTracker`
- builder.Build() retorna PokerDecisionFacade configurado

**Archivos:**
- Crear: `OpenScrape.App.Tests/PokerDecisionFacadeTestBuilder.cs`

## Task 2: Test categorías Decision* presentes tras determinar acción

**Test: DecisionCategoriesPresente_WhenPostflopDecision_ThenAllDecisionCategoriesRecorded**
- Arrange: Crear request postflop válido (hero con flush draw, board con cartas comunitarias)
- Arrange: Instanciar InMemoryMetricsCollector y construir PokerDecisionFacade
- Act: Llamar `facade.EvaluateAsync(request)`
- Assert: En el snapshot, todas las 6 categorías Decision* tienen Count > 0
- Assert verificaciones:
  ```csharp
  var snapshot = metrics.GetSnapshot();
  Assert.True(snapshot[TelemetryCategories.DecisionTotal].Count > 0);
  Assert.True(snapshot[TelemetryCategories.DecisionEquity].Count > 0);
  Assert.True(snapshot[TelemetryCategories.DecisionTexture].Count > 0);
  Assert.True(snapshot[TelemetryCategories.DecisionProfile].Count > 0);
  Assert.True(snapshot[TelemetryCategories.DecisionDecisionService].Count > 0);
  Assert.True(snapshot[TelemetryCategories.DecisionSizing].Count > 0);
  ```

**Archivo:**
- Crear: `OpenScrape.App.Tests/Telemetry/TelemetryInstrumentationIntegrationTests.cs`

## Task 3: Test count exacto por subfase

**Test: ExactCountPerSubphase_WhenCompleteDecisionCycle_ThenExactCountPerCategory**
- Arrange: Mismo setup que Task 2
- Act: Ejecutar decisión completa
- Assert: Count exacto de cada subfase
  ```csharp
  var snapshot = metrics.GetSnapshot();
  Assert.Equal(1, snapshot[TelemetryCategories.DecisionTotal].Count);
  Assert.Equal(1, snapshot[TelemetryCategories.DecisionEquity].Count);
  Assert.Equal(1, snapshot[TelemetryCategories.DecisionTexture].Count);
  Assert.Equal(1, snapshot[TelemetryCategories.DecisionProfile].Count);
  Assert.Equal(1, snapshot[TelemetryCategories.DecisionDecisionService].Count);
  Assert.Equal(1, snapshot[TelemetryCategories.DecisionSizing].Count);
  ```
- Assertion key: No comparar DecisionTotal vs suma de subfases, verificar cada categoría independientemente con valor exacto 1

**Archivo:**
- Agregar a: `OpenScrape.App.Tests/Telemetry/TelemetryInstrumentationIntegrationTests.cs`

## Task 4: Build y verification

- Ejecutar `dotnet test OpenScrape.App.Tests` para verificar tests pasan
- Verificar que los 2 tests nuevos aparecen y pasan

**Verification Commands:**
```bash
dotnet test OpenScrape.App.Tests --filter "FullyQualifiedName~TelemetryInstrumentationIntegrationTests"
```

## Dependencies

| Task | Depends on |
|------|-----------|
| Task 2 | Task 1 |
| Task 3 | Task 2 |
| Task 4 | Task 3 |

## Notes

- TelemetryCategories ya defined in src/OpenScrape.App/Telemetry/TelemetryCategories.cs
- InMemoryMetricsCollector should be used for real accumulation (no mock)
- DecisionRequest should provide valid postflop scenario with hero cards, community cards, pot info