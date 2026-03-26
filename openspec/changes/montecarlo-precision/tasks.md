## 1. HandScore struct

- [x] 1.1 Crear `HandScore` readonly struct en `IHandEvaluator.cs` con `CompositeScore` (long), `HandRank`, `CompareTo()` y `BuildComposite()`.
- [x] 1.2 Implementar `EvaluateHandScore()` en `BitHandEvaluator.cs` con misma lógica de bit-manipulation pero retornando struct sin listas.
- [x] 1.3 Añadir delegación en `HandEvaluator.cs` legacy.
- [x] 1.4 Añadir `EvaluateHandScore` a interfaz `IHandEvaluator`.

## 2. Enumeración exacta river

- [x] 2.1 Implementar `ExactEnumerationRiver()` que enumera C(remaining,2) manos sin rango.
- [x] 2.2 Implementar path con rango: iterar combos ponderados del villano.
- [x] 2.3 Routing en `CalculateEquity()`: si communityCards.Count == 5 → exact.

## 3. Enumeración exacta turn

- [x] 3.1 Implementar `ExactEnumerationTurn()` que enumera river × C(remaining-1,2).
- [x] 3.2 Implementar path con rango: river × combos ponderados.
- [x] 3.3 Routing en `CalculateEquity()`: si communityCards.Count == 4 → exact.

## 4. MC optimizado flop/preflop

- [x] 4.1 `GetAdaptiveIterations()`: Preflop 30K, Flop 50K.
- [x] 4.2 `RunSingleSimulation` usa `EvaluateHandScore()` (struct) en vez de `EvaluateBestHand()`.
- [x] 4.3 ThreadLocal buffer para mano del oponente (elimina allocation por iteración).

## 5. Fix villain range

- [x] 5.1 `TryDrawFromRange` recibe `totalWeight` pre-computado.
- [x] 5.2 Cuando falla → retorna `skipped: true` en vez de usar cartas aleatorias.
- [x] 5.3 `effectiveCount = simulationCount - totalSkipped` para calcular equity solo sobre iteraciones válidas.

## 6. Verificación

- [x] 6.1 Build sin errores.
- [x] 6.2 504 tests existentes pasan.
- [ ] 6.3 Añadir tests de enumeración exacta y HandScore.
