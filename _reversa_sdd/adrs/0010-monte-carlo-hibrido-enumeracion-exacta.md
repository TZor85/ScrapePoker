# ADR-0010 — Monte Carlo híbrido: enumeración exacta turn/river, MC adaptativo flop/preflop

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-26 (memoria `Monte Carlo Precision` completado)
- **Confianza:** 🟢 CONFIRMADO en código y memoria del proyecto
- **Decisor inferido:** Alberto

## Contexto

El cálculo de equity es el cuello de botella más caro del game loop (~50-200ms por iteración cuando el motor decide). Originalmente se usaba **Monte Carlo simulation con iteraciones fijas** (10K) para todas las streets:

- Preflop: 10K iteraciones MC. Acceptable.
- Flop: 10K iteraciones MC. Error ~+-1.5%.
- Turn: 10K iteraciones MC. Error ~+-1.0%. **Pero el espacio combinatorial es C(45,2)=990 — totalmente enumerable.**
- River: 10K iteraciones MC. Error ~+-0.7%. **Espacio C(45,2)=990 también enumerable.**

Hacer MC en turn/river era **gastar tiempo en aleatoriedad cuando el resultado exacto es alcanzable**.

## Decisión

Pipeline híbrido en `MonteCarloSimulator`:

1. **River → enumeración exacta** de C(45,2)=990 manos del villain. Equity 100% determinística.
2. **Turn → enumeración exacta**: 45 rivers × C(44,2) opponent hands ≈ 42K evaluaciones. Determinística.
3. **Flop → MC con 50K iteraciones** (subido de 10K). Error ~+-0.5%.
4. **Preflop → MC con 30K iteraciones**. Error similar.
5. **`HandScore` struct** (zero allocation) para evaluación rápida en hot path.
6. **`ThreadLocal<>`** deck/buffers para evitar synchronization en MC paralelizado.
7. **Cache preflop** (LRU 512 entries) para hands repetidas.
8. **Skip-on-block en villain range:** si la mano sampleada bloquea cartas conocidas, se descarta sin random fallback (que corrompía equity). Tracking de `SkippedSimulations` para detectar fiabilidad baja.
9. **Pre-compute totalWeight** del villain range una vez, no por iteración.

## Alternativas consideradas

1. **Mantener MC fijo 10K en todas las streets.** Rechazado: turn/river son enumerables y la precisión gana ~1%.
2. **MC con 100K iteraciones para máxima precisión.** Rechazado: latencia inaceptable (>500ms).
3. **Lookup table preflop completo (169 manos × 169 manos).** Considerado para preflop, descartado en favor de MC + cache LRU porque incluir villain ranges parametrizados expande la tabla a millones de entradas.
4. **GPU compute (CUDA / Vulkan compute).** Sobre-ingeniería. La latencia con CPU + ThreadLocal + struct HandScore es <50ms en flop.
5. **Equity solver externo (PioSolver invocado vía pipe).** Latencia de minutos. Inviable para live play.

## Consecuencias

**Positivas:**

- **Determinismo en turn/river.** Mismo input → siempre misma equity. Bug detection trivial.
- **Precisión flop ~+-0.5%** con 50K iteraciones (antes +-1.5% con 10K).
- **`HandScore` struct elimina allocs** en el hot path del MC. Reducción de pressure GC.
- **Skip-on-block correcto:** antes, bloquear una mano del villain hacía un random sustituto que corrompía la equity (todas las manos iguales en peso). Ahora se skipea limpiamente.
- Tests `MonteCarloTests` (19) + `HandScoreTests` (4) + `EquityCalculatorTests` (7) = 30 tests cubren el pipeline.

**Negativas:**

- **Memory bandwidth en turn:** 42K evaluaciones requieren leer 5×42K cartas + comparar. Mitigado por `BitHandEvaluator` zero-alloc.
- Diferentes paths de código por street añaden complejidad de mantenimiento. Mitigado: cada path tiene tests específicos.
- **Cache preflop (LRU 512)** es heurística. Si el usuario juega muchas hand combos distintas, el hit rate puede bajar. **No medido en producción.**

**Implicaciones para una migración:**

- En un stack que soporte SIMD (Rust con `std::simd`, C++ con AVX2), la enumeración turn/river puede acelerar 4-8x. Atractivo para futuro.
- En lenguajes sin struct value types (Python, JavaScript), `HandScore` requiere alternativas (NumPy arrays para batch eval) — la performance gap puede ser 10x+.
- El **contrato del MC** es claro: dado `(heroCards, communityCards, villainRange, numOpponents)`, retorna `EquityResult { Equity, SkippedSimulations }`. Reimplementable en cualquier lenguaje.

## Referencias

- `src/OpenScrape.DecisionMaker/Algorithms/MonteCarloSimulator.cs` (654 LOC).
- `src/OpenScrape.DecisionMaker/Algorithms/BitHandEvaluator.cs` (515 LOC) y `IHandEvaluator.HandScore` struct.
- Memoria del proyecto: "Monte Carlo Precision ✅ (2026-03-26)".
- ADR-0011 (OpponentTracker para villain range adaptativo).
