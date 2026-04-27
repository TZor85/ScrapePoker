# Design: Instrumentation Integration Tests

## Architecture

### Test Structure
```
TelemetryInstrumentationIntegrationTests.cs
├── PokerDecisionFacadeTestBuilder (helper, reused or created)
└── Test methods:
    ├── DecisionCategoriesPresente_WhenPostflopDecision_ThenAllDecisionCategoriesRecorded()
    └── ExactCountPerSubphase_WhenCompleteDecisionCycle_ThenExactCountPerCategory()
```

### Dependencies & Flow
1. **Arrange**: Crear `PokerDecisionFacade` con `InMemoryMetricsCollector` real
2. **Act**: Llamar `EvaluateAsync` con `DecisionRequest` válido
3. **Assert**: Verificar en `_metrics.GetSnapshot()` las categorías esperadas

### Key Classes

| Class | Responsibility |
|-------|----------------|
| `InMemoryMetricsCollector` | Implementación real de `IMetricsCollector` que acumula en memoria |
| `PokerDecisionFacade` | Ejecuta el pipeline completo de decisión |
| `DecisionRequest` | DTO con cartas, board, stacks, etc. |
| `TelemetryCategories` | Constantes con los nombres de categoría |

### Métricas a verificar
```csharp
// En PokerDecisionFacade.EvaluateAsync:
using (_ = _metrics.Measure(TelemetryCategories.DecisionTotal))      // +1
using (_ = _metrics.Measure(TelemetryCategories.DecisionEquity))  // +1
using (_ = _metrics.Measure(TelemetryCategories.DecisionTexture))   // +1
using (_ = _metrics.Measure(TelemetryCategories.DecisionProfile))     // +1
using (_ = _metrics.Measure(TelemetryCategories.DecisionDecisionService)) // +1
using (_ = _metrics.Measure(TelemetryCategories.DecisionSizing)) // +1
// Total esperado: Count == 6 por cada categoría
```

### Test Scenarios

#### Postflop Decision Scenario
```csharp
var request = new DecisionRequest
{
    HeroCards = [Card.AsCorazones, Card.KCorazones],
    CommunityCards = [Card.DosTreboles, Card.QCorazones, Card.JCorazones, Card.NuevePicas],
    PotSize = 100m,
    BetToCall = 20m,
    NumOpponents = 1,
    IsInPosition = true,
    HeroStack = 1000m,
    VillainStack = 1000m,
    HandSituationTag = "FlushDraw",
    Situation = HandSituation.Postflop
};
```

#### Expected Counts per Category
| Category | Expected Count |
|----------|---------------|
| DecisionTotal | 1 |
| DecisionEquity | 1 |
| DecisionTexture | 1 |
| DecisionProfile | 1 |
| DecisionDecisionService | 1 |
| DecisionSizing | 1 |

### Reusable Test Builder
Si no existe `PokerDecisionFacadeTestBuilder`, crear uno simple:
```csharp
public class PokerDecisionFacadeTestBuilder
{
    private IPokerCalculator _calculator = ...;
    private IMetricsCollector _metrics = ...;
    // ... configuración por defecto
    
    public PokerDecisionFacade Build() => new PokerDecisionFacade(...);
}
```

## Consideraciones

### Por qué no testear en unit tests existentes
Los unit tests de `PokerDecisionFacadeTests` mocking el `IMetricsCollector` no verifican que el código real llame a `Measure()`. Un test de integración con collector real es la única forma de detectar regressions silenciosas.

### Por qué no simplemente validar los calls al mock
Un mock puede configurarse para responder a cualquier cosa (`mock.Measure(...).Returns(...)`), pero no nos dice si el código dejó de llamar `Measure()`. El test de integración verifica presencia real.

### Manejo de side effects
Cada test debe crear un nuevo `InMemoryMetricsCollector` limpio o limpiar el estado antes del test, para evitar contaminación entre tests.