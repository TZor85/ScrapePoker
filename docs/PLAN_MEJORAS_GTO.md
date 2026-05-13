# Plan de Mejoras GTO — OpenScrape

**Fecha**: 2026-04-05  
**Análisis basado en**: TexasSolver (bupticybee/TexasSolver) + Estado actual del codebase  
**Restricciones**: ≤7 segundos por decisión, velocidad clave, sin backtesting

---

## Contexto

### Estado Actual del Motor de Decisión
- **Método**: Heurísticas paramétricas (rule-based) en `PostflopDecisionService`
- **Equity**: Monte Carlo con VillainRange (preflop + postflop)
- **Bet Sizing**: Multiplicadores fijos por street
- **Rangos**: Estáticos por posición (StrategyProfile)

### Referencia: TexasSolver
- Implementa CFR (Counterfactual Regret Minimization)
- Calcula Nash equilibrium real
- Mide exploitabilidad en mbb/hand
- Optimiza bet sizes automáticamente

### Gap Identificado
| Aspecto | TexasSolver | OpenScrape Actual |
|---------|-------------|-------------------|
| Exploitability | ✅ Mide | ❌ No implementado |
| Range polarization | ✅ Dinámico | ❌ Estático |
| Bet sizing | ✅ Múltiples sizes | ✅ Fijo (mejorable) |
| Iteraciones MC | ✅ Optimizado | ✅ Parcial |

---

## Mejoras Propuestas (4 items)

---

### Mejora 1: Exploitability Calculator

**Objetivo**: Medir qué tan "GTO" es la estrategia actual contra un oponente exploitador.

**Ubicación**: Nuevo archivo `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs`

**Implementación**:

```csharp
public class ExploitabilityCalculator
{
    // Calcular best response vs estrategia actual
    // Output: mbb/hand (milli-big-blinds per hand)
    
    // Método principal:
    // 1. Para cada spot posible, calcular "best response" teórico
    // 2. Comparar con decisión actual del bot
    // 3. Sumar diferencia esperada
    
    // Thresholds:
    // < 10 mbb/hand → suficientemente GTO
    // 10-50 mbb/hand → ajustes menores necesarios
    // > 50 mbb/hand → leaks significativos
}
```

**Archivos afectados**:
- Nuevo: `ExploitabilityCalculator.cs`

**Tests**: Unit tests para los principales escenarios de decisión.

**Tiempo estimado**: 2-3 horas

---

### Mejora 2: Range Polarization Logic

**Objetivo**: Adaptar rangos según board texture y posición (IP vs OOP).

**Ubicación**: Nuevo archivo `src/OpenScrape.DecisionMaker/Services/RangePolarizer.cs`

**Implementación**:

```csharp
public enum RangeType
{
    Linear,      // Range merged / value-heavy
    Polarized,   // Polarizado: nuts + bluffs
    Condensed    // Tight, pocas manos medias
}

public class RangePolarizer
{
    public RangeType GetOptimalRangeType(
        BoardTextureCategory texture,
        bool isInPosition,
        SPR spr,
        Street street)
    {
        // Dry board + IP + flop → Polarized (más bluffs)
        // Wet board → Linear (más calls)
        // Low SPR (<3) → Condensed
        // OOP → siempre más Linear (menos bluffs)
    }
}
```

**Integración**:
- Añadir `RangePolarizer` como dependencia de `PostflopDecisionService`
- Usar en `DetermineAction` para ajustar umbrales de bluff/value

**Archivos afectados**:
- Nuevo: `RangePolarizer.cs`
- Modificar: `PostflopDecisionService.cs` (inyectar y usar)

**Tests**: Tests para board texture + posición → RangeType correcto.

**Tiempo estimado**: 4-6 horas

---

### Mejora 3: Optimización de Iteraciones Monte Carlo

**Objetivo**: Reducir latencia manteniendo precisión aceptable.

**Ubicación**: `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`

**Cambios**:

1. **Reducir iterations en spots obvios**:
   - Equity > 80% o < 20%: 250 iteraciones (antes 500)
   - Equity > 65% o < 35%: 500 iteraciones (antes 750)
   - Default: 2000 (antes 10000) — ajustar según benchmarks

2. **Early termination**:
   - Añadir confidence interval check
   - Si std dev < 2% → terminar antes

3. **Cache postflop más agresivo**:
   - Aumentar `_EquityCacheMaxSize` de 512 a 2048
   - Usar LRU eviction strategy

**Archivos afectados**:
- Modificar: `UnifiedPokerCalculator.cs` (líneas ~261-270, ~58-59)
- Modificar: `MonteCarloSimulator.cs` (early termination opcional)

**Tests**: Verificar que decisiones no cambien con fewer iterations.

**Tiempo estimado**: 1 hora

---

### Mejora 4: Multi-Profile Bet Sizing

**Objetivo**: Bet sizes dinámicos según situación (no solo multiplicadores fijos).

**Ubicación**: `src/OpenScrape.DecisionMaker/Services/BetSizingService.cs` (existente, expandir)

**Cambios**:

```csharp
// Nuevos métodos en BetSizingService:

public List<BetSizingOption> GetValueBetSizes(
    double equity,
    SPR spr,
    BoardTextureCategory texture,
    bool isInPosition,
    bool isMultiway);

public List<BetSizingOption> GetBluffSizes(
    double foldEquity,
    SPR spr,
    BoardTextureCategory texture,
    bool isInPosition);

public double GetThinValueThreshold(
    BoardTextureCategory texture,
    SPR spr,
    bool isInPosition);
```

**Lógica**:
- **Value bets**: 3-4 tamaños (small, medium, large, overbet)
- **Bluffs**: Más pequeños en wet boards, más grandes en dry
- **IP**: Puede bet más pequeño (induce calls)
- **OOP**: Necesita más protección → bet más grande
- **Thin value**: Ajustar threshold según textura

**Archivos afectados**:
- Modificar: `BetSizingService.cs`
- Modificar: `PostflopDecisionService.cs` (usar nuevos métodos)

**Tests**: Tests de integración verificando bet sizes por situación.

**Tiempo estimado**: 3-4 horas

---

## Dependencias Entre Mejoras

```
Mejora 1 (Exploitability)    → Independiente
Mejora 3 (MC Optimization)    → Independiente
Mejora 2 (Range Polarization) → Depende de: 1 (para medir impacto)
Mejora 4 (Bet Sizing)        → Depende de: 2 (range → sizing)
```

**Orden sugerido**:
1. **Mejora 3** (rápida, baja latencia)
2. **Mejora 1** (métricas, validación)
3. **Mejora 2** (lógica principal)
4. **Mejora 4** (completa el ciclo)

---

## Tests a Crear/Actualizar

| Mejora | Test File | Coverage |
|--------|-----------|----------|
| 1 | `ExploitabilityCalculatorTests.cs` | 10+ casos |
| 2 | `RangePolarizerTests.cs` | 15+ escenarios |
| 3 | `UnifiedPokerCalculatorTests.cs` | Verificar no-regresión |
| 4 | `BetSizingServiceTests.cs` | 20+ situaciones |

---

## Métricas de Éxito

| Métrica | Target |
|---------|--------|
| Latencia decisión | ≤7 segundos |
| Exploitability inicial | < 100 mbb/hand (línea base) |
| Iteraciones MC promedio | < 1000 por decisión |
| Bet size coverage | ≥3 sizes por spot |

---

## Archivos del Proyecto

### Archivos a Crear
- `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs`
- `src/OpenScrape.DecisionMaker/Services/RangePolarizer.cs`
- `test/OpenScrape.App.Tests/ExploitabilityCalculatorTests.cs`
- `test/OpenScrape.App.Tests/RangePolarizerTests.cs`

### Archivos a Modificar
- `src/OpenScrape.App/Aplication/UseCases/UnifiedPokerCalculator.cs`
- `src/OpenScrape.DecisionMaker/Services/PostflopDecisionService.cs`
- `src/OpenScrape.DecisionMaker/Services/BetSizingService.cs`
- `test/OpenScrape.App.Tests/UnifiedPokerCalculatorTests.cs` (actualizar)

---

## Resumen

| # | Mejora | Impacto | Esfuerzo | Tiempo Est. | Orden |
|---|--------|---------|----------|-------------|-------|
| 1 | Exploitability Calculator | Alto | Bajo | ~2-3 hrs | 2° |
| 2 | Range Polarization Logic | Alto | Medio | ~4-6 hrs | 3° |
| 3 | MC Optimization | Medio | Bajo | ~1 hr | 1° |
| 4 | Multi-Profile Bet Sizing | Medio | Medio | ~3-4 hrs | 4° |

**Total**: ~10-15 horas de desarrollo + tests

**Objetivo final**: Mejorar precisión GTO manteniendo latencia ≤7s.
