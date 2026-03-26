## Why

El MonteCarloSimulator usa sampling aleatorio para TODAS las streets con 10.000 iteraciones fijas, lo que introduce error estadístico innecesario en river y turn donde la enumeración exacta es computacionalmente trivial. Además, el fallback a cartas aleatorias cuando el rango del villano está bloqueado corrompe la equity calculada.

## What Changes

1. **Enumeración exacta en river** — Con 5 community cards conocidas, enumera C(45,2)=990 manos posibles del oponente para equity 100% exacta (sin error estadístico).
2. **Enumeración exacta en turn** — Con 4 community cards, enumera 45 posibles river cards × C(44,2) manos del oponente ≈ 42K evaluaciones para equity exacta.
3. **HandScore struct** — Nuevo struct ligero para evaluación de manos en MC que elimina ~80.000 heap allocations por cálculo (no crea listas de Cards ni Kickers).
4. **Iteraciones adaptativas** — Preflop: 30K, Flop: 50K (antes 10K para todo). Error estadístico en flop baja de ±2% a ±0.5%.
5. **Fix villain range fallback** — Cuando TryDrawFromRange falla, la iteración se descarta (skip) en vez de usar cartas aleatorias que corrompen la equity.
6. **Pre-compute totalWeight** — El peso total de los combos del villano se calcula una vez, no en cada iteración.

## Capabilities

### New Capabilities

- `exact-enumeration`: Cálculo de equity determinístico (sin error) para river y turn mediante enumeración exhaustiva de manos posibles.
- `handscore-struct`: Evaluación de manos sin allocations en heap para uso intensivo en simulaciones.

### Modified Capabilities

- `adaptive-iterations`: Iteraciones MC ajustadas por street (30K preflop, 50K flop) para mayor precisión.
- `villain-range-integrity`: Skip de iteraciones bloqueadas en vez de fallback a random.

## Impact

- **`src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs`**: Reescrito con 3 paths (exact river, exact turn, MC flop/preflop).
- **`src/OpenScrape.DecisionMaker/Algorithms/BitHandEvaluator.cs`**: Nuevo método `EvaluateHandScore()`.
- **`src/OpenScrape.DecisionMaker/Algorithms/IHandEvaluator.cs`**: Nuevo `HandScore` struct y método en interfaz.
- **`src/OpenScrape.DecisionMaker/Algorithms/HandEvaluator.cs`**: Implementación delegada de `EvaluateHandScore()`.
