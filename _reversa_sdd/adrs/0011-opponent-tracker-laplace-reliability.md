# ADR-0011 — `OpponentTracker` con sample-size reliability granular y Laplace smoothing

- **Estado:** 🟢 ACEPTADO (vigente)
- **Fecha:** ~2026-03-25 (Sprint 9-10) y refinado el 2026-04-08 (commit `72ade14 fix(domain): Laplace smoothing en AggressionFactor`)
- **Confianza:** 🟢 CONFIRMADO
- **Decisor inferido:** Alberto

## Contexto

Las decisiones del motor postflop se modulan según el tipo del villain (LAG/TAG/LP/TP/Unknown) y según estadísticas como VPIP, PFR, AggressionFactor (AF), Fold-to-CBet, WTSD, etc.

Problemas iniciales:

- **Cliff abrupto del AF:** cuando un villano nunca había hecho call (passive=0), `AF = aggressive / 0 = Infinity` o `AF = aggressive / 1 = aggressive` (si se hardcodeaba +1 al denominador). Resultado: villanos con 2 manos parecían "ultra-agresivos" (AF=∞) o "neutros" (AF=2).
- **Falta de granularidad de fiabilidad:** un único flag "tiene 20 manos" determinaba si usar stats reales o defaults estáticos. Pero algunos stats convergen rápido (CBet con 5 muestras es informativo), otros lentos (WTSD requiere ≥15 manos para no ser ruido).
- **OpponentType binario en VPIP/AF.** El cliff de "20 manos cumplidas → ahora villain es TAG" es brusco.

## Decisión

1. **`OpponentProfile.AggressionFactor`** con **Laplace smoothing** universal: `(aggressive + 1) / (passive + 1)`. Cuando passive=0, AF tiende a `aggressive+1`, no infinito. Con pocos datos AF→1.0 (neutro). Convergencia suave.
2. **Reliability flags granulares por estadística:**
   - `IsReliable`: HandsPlayed ≥ 20 (para `Type` general).
   - `HasReliablePreflopData`: HandsPlayed ≥ 10 (más rápido que IsReliable).
   - `HasReliableCBetData`: TimesCBetOpportunity ≥ 5 && TimesFacedCBet ≥ 5.
   - `HasReliableAFData`: (Bet+Raise+Call) ≥ 10.
   - `HasReliableFoldData`: (Fold+Call+Raise) ≥ 8.
   - `HasReliableWTSDData`: TimesReachedRiver ≥ 15.
   - `HasReliableWSDData`: TimesWentToShowdown ≥ 10.
   - `HasReliableCheckRaiseData`: TimesCheckRaiseOpportunity ≥ 10.
   - `HasReliableDonkBetData`: TimesDonkBetOpportunity ≥ 8.
   - `HasReliableBarrelData`: TimesBarrelOpportunity ≥ 8.
3. **Stats posicionales** (`OpponentPositionProfile`, S22.3): contadores VPIP/PFR/AF separados por `TablePosition` (BTN, EP, SB, BB, etc.). `IsReliable` por posición = HandsPlayed ≥ 10. `GetProfileForPosition()` devuelve perfil sintético con stats de la posición + globales para el resto. Permite distinguir "villain LAG desde BTN, TAG desde EP".
4. **`ConcurrentDictionary<string, OpponentProfile>`** thread-safe en `OpponentTracker`. Hero (P0) **no** se rastrea — solo villains.

## Alternativas consideradas

1. **Stats globales sin reliability flags.** Estado original parcial. Rechazado: crea decisiones basadas en ruido (1-2 manos).
2. **Bayesian update con prior por OpponentType.** Más sofisticado: mantener prior de "villain medio" y actualizar con likelihood. Rechazado por complejidad — Laplace smoothing simple cubre el cliff con suficiente calidad.
3. **Persistir perfiles entre sesiones.** Considerado pero no implementado (Q-FSM-02 abierta). Rechazado por ahora: identidad del jugador requiere identificador robusto (alias OCR es frágil entre temas/skin).
4. **Bucket más fino de tipos** (TAG-loose, TAG-tight, etc., 8+ tipos). Rechazado por complejidad. 4 tipos + Unknown cubren 90% de las decisiones.
5. **AF con Bessel correction** o suavizado por sample-size variable. Rechazado: Laplace simple es comprensible y testeable.

## Consecuencias

**Positivas:**

- **Sin cliff abrupto** en AF. Villanos con 2 manos no se clasifican como "ultra-aggressive". Bug fix L4.
- **Granularidad correcta:** el motor consulta el flag adecuado antes de usar la stat. Si CBet aún no es fiable, fallback a 50% default.
- **AF posicional** captura "el villano juega LAG en CO pero TAG en EP" — relevante para los pools de hoy.
- 21 tests `OpponentTrackerTests` cubren el setup.
- `ConcurrentDictionary` permite tracking desde game loop sin bloqueos.

**Negativas:**

- **Memoria por sesión:** ~10 villains × 80 bytes ≈ irrelevante. Pero si la sesión es larga (1000+ villains rotando), creciente.
- **Sin persistencia → cold start cada sesión.** Cada villano vuelve a ser `Unknown` los primeros 10-20 minutos.
- Los **defaults** (VPIP=50, PFR=15, ...) son **conservadores tirando a fish**. Asumen "villano medio loose". Si el pool cambia (mesa de regs), las defaults son sub-óptimas.

**Implicaciones para una migración:**

- El contrato `OpponentProfile` (28+ propiedades + reliability flags) es **estable**. Cualquier reimplementación debe replicar las defaults y los thresholds de reliability.
- La estrategia de identidad (`PlayerId = nombre OCR`) debería evolucionar a "identidad robusta" — quizá hash de (nombre + initial seat) — pero requiere tracking de sit-out/levantar.
- Persistencia entre sesiones es trivial añadir si Marten ya está disponible: nueva colección `opponent_profiles` con TTL.

## Referencias

- `src/OpenScrape.Domain/Entities/OpponentProfile.cs` (~310 LOC).
- `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs` (274 LOC).
- Commit `72ade14 fix(domain): Laplace smoothing en AggressionFactor — elimina cliff passive=0 (L4)`.
- Memoria proyecto: "S15.3 OpponentTracker sample size granular" y "S22.3 stats posicionales".
- ADR-0010 (Monte Carlo usa villain range adaptativo basado en este tracker).
