# ADR-0005 — Eliminar ML.NET y volver a un motor de decisión determinístico

- **Estado:** 🟢 ACEPTADO (vigente). Reemplaza decisión previa de "ML.NET para predicción de acciones".
- **Fecha:** 2026-03-16 (commit `2360036 refactor(core): Remove ML.NET, update config & logging`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

A finales de 2026-01 / inicios de 2026-02 se experimentó con un modelo ML para predecir la jugada óptima en escenarios postflop específicos. Commit `9acdcc0 [feature/ML]: Add RolOopHelper for poker action prediction` añadió:

- `Helpers/MLHelper/RolOopHelper.cs` (385 LOC).
- Dependencia `Microsoft.ML`.
- Lógica que delegaba a un modelo entrenado para `RaiseOverLimperFlopAction`.

Tras un tiempo en producción, el approach se abandonó.

## Decisión

Eliminar **toda la lógica ML.NET** y volver a un motor 100% **determinístico y configurable**:

- Sin ML.NET, Nancy ni dependencias relacionadas.
- Pipeline de decisión basado en **algoritmos clásicos de poker**: Monte Carlo equity, hand evaluation por bit-manipulation, outs counting con inclusión-exclusión, board texture analysis.
- Configuración explícita en `StrategyProfile` (~70 parámetros) + `StreetThresholds` (36 entradas: 4 streets × 9 situations).
- Todas las decisiones reproducibles dado el mismo input.

## Alternativas consideradas

1. **Mantener ML.NET y entrenar mejor el modelo.** Rechazado:
   - Dataset reducido (solo manos jugadas por el autor).
   - Coste alto de re-entrenar cuando cambia la calibración.
   - Imposible debuggear "por qué decidió X" — caja negra que el autor no podía afinar manualmente.
   - Versionado del modelo + del código se vuelve un proyecto en sí mismo.
2. **Migrar a un solver GTO existente (PioSolver, GTO+).** Rechazado: licencias caras, no portables a un binario embebido, latencia incompatible con live play (PioSolver tarda minutos por spot).
3. **Mantener ambos motores y elegir runtime.** Rechazado por complejidad: dos pipelines paralelos = dos veces el mantenimiento y los tests.
4. **Aprendizaje por refuerzo (RL) sobre simulaciones.** Considerado mentalmente, descartado: requiere infraestructura de entrenamiento que excede el alcance del proyecto.
5. **Volver al motor previo a ML.NET (statu quo ante).** Aceptado, **iterando** con sprints S5..S22 y refinamientos L1..L6 para ganar precisión sin recurrir a ML.

## Consecuencias

**Positivas:**

- **Determinismo completo:** dado un input, la decisión es siempre la misma. Bug fix del motor = test unitario verde.
- **Tunable a mano:** `appsettings.json` permite al usuario ajustar ~70 parámetros sin recompilar.
- **Backtest reproducible:** `StrategyBacktester` puede replay manos históricas con perfil A vs perfil B y comparar BB/100 esperado.
- **Tests unitarios cubren paths claros** (645+ tests). Cada parámetro tiene cobertura.
- **Eliminación de superficie de ataque** y reducción de tamaño del binario al sacar Microsoft.ML.

**Negativas:**

- **Pérdida de adaptación automática.** El modelo ML potencialmente ajustaba mejor a oponentes específicos sin parámetros explícitos. Compensado parcialmente por `OpponentTracker` + `OpponentType` y los ajustes por tipo (LAG/TAG/LP/TP).
- **70 parámetros en `StrategyProfile` son intimidantes.** Sin documentación, el usuario casual no sabe qué tocar. Mitigación: validación al arrancar (`StrategyProfileValidator`), mensajes de error claros, docstring en cada parámetro.
- **Riesgo de over-fitting al estilo del autor.** Los thresholds están afinados para los stakes y pools donde se testeó. Migrar a otros stakes requiere re-calibración.

**Implicaciones para una migración:**

- En cualquier reimplementación, el motor determinístico debe preservarse 1:1 — son ~3K LOC con 645+ tests que constituyen el "knowhow comprimido" del proyecto.
- El `StrategyProfile` es contractual: los nombres de parámetros deben mantenerse para que `appsettings.json` siga funcionando.
- Si en el futuro se quisiera reintroducir ML, hacerlo como **adjustment layer** (post-procesar la decisión determinística) en lugar de reemplazo total. Permite A/B test sin perder el baseline.

## Referencias

- Commit `2360036 refactor(core): Remove ML.NET, update config & logging` — fecha exacta y diff.
- Commit `9acdcc0 [feature/ML]: Add RolOopHelper for poker action prediction` — el commit ML eliminado.
- `src/OpenScrape.Domain/Entities/StrategyProfile.cs` — los ~70 parámetros que sustituyen al modelo.
- ADR-0006 (Pipeline unificado de equity).
- ADR-0010 (Monte Carlo híbrido).
