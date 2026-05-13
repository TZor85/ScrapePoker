# OpenScrape.DecisionMaker — Preguntas Abiertas

> Lacunas detectadas en el motor de decisión que requieren validación humana antes de implementar/migrar/refactorizar.
> Modo de respuesta: `file` (responder editando este archivo, secciones marcadas con `📝 Respuesta:`).
> Cada pregunta tiene un ID estable (`Q-DM-NN`) para referencia desde `tasks.md`, `decisions.md`, `edge-cases.md`.
> **Algunas preguntas ya están respondidas en el `questions.md` raíz** (Q-FSM-01, Q-FSM-02, etc.). Aquí se duplican solo las que tienen implicación específica de implementación de DecisionMaker.

---

## Q-DM-01 — ¿`HandEvaluator` legacy se elimina o queda?

**Contexto:**
- `Algorithms/HandEvaluator.cs` (206 LOC) coexiste con `Algorithms/BitHandEvaluator.cs` (515 LOC).
- El legacy `EvaluateHandScore` delega a `new BitHandEvaluator()` por llamada; el resto de su superficie (`EvaluateBestHand`) sigue siendo brute-force `C(7,5)=21`.
- Sin justificación documentada para mantener ambos.
- Posibles consumidores del legacy: tests legacy, paths antiguos no migrados, uso defensivo "por si acaso".

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar `HandEvaluator.cs` y redirigir tests al bit-evaluator** | -206 LOC muerto, fuente única de verdad. | Riesgo de regresión si algún test depende de outputs específicos del legacy. |
| **B. Mantener legacy hasta validar que BitHandEvaluator cubre 100%** | Cero riesgo. | Código muerto a la vista; confusión de devs nuevos. |
| **C. Mover legacy a `OpenScrape.App.Tests` como código de test** | Fuera del módulo productivo. | Rompe la regla de no tocar código legacy en módulos productivos. |

**Preguntas concretas:**
1. ¿Existe algún test que llame `HandEvaluator.EvaluateBestHand` directamente (no `BitHandEvaluator`)?
2. ¿La diferencia de output entre ambos es 0 en todos los inputs probables?
3. ¿Hay cobertura de tests que valide ambos producen el mismo resultado?

**Bloqueo:** T-29 en `tasks.md`. **Decisiones afectadas:** DD-12. **Casos extremos:** ninguno directo.

🔴 **Respuesta:**

> Opción A
> 1. No lo recuerdo
> 2. Si
> 3. No lo recuerdo

---

## Q-DM-02 — ¿`PreflopEquityCalculator` recibe interfaz `IPreflopEquityCalculator`?

**Contexto:**
- `Algorithms/PreflopEquityCalculator.cs` no implementa interfaz.
- Es consumido directamente por `EquityCalculatorService`.
- No es testeable con mock; hay que instanciar la clase real.
- El resto de algoritmos numéricos sí tienen interface (`IBitHandEvaluator`/`IHandEvaluator`, `IMonteCarloSimulator`, `IOutsCalculator`, `IBoardTextureAnalyzer`).

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Crear `IPreflopEquityCalculator` y registrar en DI** | Consistencia con resto de algoritmos. Mocks limpios para tests de `EquityCalculatorService`. | Refactor + nuevo archivo de interface. |
| **B. Mantener sin interfaz** | Cero refactor. | Inconsistencia, no testeable con mocks. |
| **C. Convertir a estático** | Es una tabla de lookup pura, no requiere estado. | Imposibilita mocking; consumidor (`EquityCalculatorService`) acoplado a la implementación concreta. |

**Preguntas concretas:**
1. ¿Hay tests existentes de `EquityCalculatorService.CalculateFullEquity` con `community = []` que se beneficien de mock? (TT-09)
2. ¿La tabla de 169 manos heads-up cambia entre estrategias (ej. solver outputs distintos para distintos formatos), o es invariante?
3. ¿Vale la pena la interfaz si nunca se va a sustituir?

**Bloqueo:** T-28 en `tasks.md`. **Decisiones afectadas:** DD-02 (consistencia de inversión).

🔴 **Respuesta:**

> Opcion A
> 1. No lo recuerdo
> 2. No
> 3. Si

---

## Q-DM-03 — ¿`PreflopAnalyzer` se queda estático o se elimina la interfaz trivial?

**Contexto:**
- `Services/PreflopAnalyzer.cs` (162 LOC) define métodos estáticos: `IsPreflopAggressor`, `HasRangeAdvantageOnBoard`, `CategorizeOpponentBet`, `DetectDonkBet`, `CalculateCbetAdjustment`.
- También define `IPreflopAnalyzer` con métodos no-estáticos que **delegan trivialmente** a la versión estática.
- Patrón anómalo: `static` + `interface` con impl que es solo `=> StaticVersion(args)`.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar interface `IPreflopAnalyzer` + dejar solo estático** | -29 LOC, cero indirección. | Imposibilita mocking de `PreflopAnalyzer` en tests del motor. |
| **B. Eliminar versión estática + dejar solo `IPreflopAnalyzer` con impl real** | Consistente con el patrón del resto del módulo. | Posible coste de allocation extra (no significativo si singleton). |
| **C. Mantener ambos (estado actual)** | Cero refactor. | Dualidad confusa; consumidores eligen arbitrariamente entre static y DI. |

**Preguntas concretas:**
1. ¿Hay consumidores que llamen `PreflopAnalyzer.IsPreflopAggressor(...)` directamente (sin DI)?
2. ¿Algún test mockea `IPreflopAnalyzer` o todos usan la implementación real?
3. ¿La pureza de los métodos justifica `static` o el patrón uniforme justifica DI?

**Bloqueo:** T-36 en `tasks.md`. **Decisiones afectadas:** DD-02.

🔴 **Respuesta:**

> Opcion A
> 1. No lo recuerdo
> 2. Creo que no
> 3. Creo que justifica

---

## Q-DM-04 — ¿Tipos `nested` (`EquityResult`, `OutsResult`, `FullEquityAnalysis`) se mueven a `DTOs/`?

**Contexto:**
- `Interfaces/IMonteCarloSimulator.cs` referencia `MonteCarloSimulator.EquityResult` (definido como `nested` en la implementación).
- `Interfaces/IOutsCalculator.cs` referencia `OutsCalculator.OutsResult` (idem).
- `Interfaces/IEquityCalculatorService.cs` referencia `EquityCalculatorService.FullEquityAnalysis` (idem).
- Acopla la interfaz a la implementación; mocks deben referenciar la implementación.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Mover a `DTOs/EquityResult.cs`, `OutsResult.cs`, `FullEquityAnalysis.cs`** | Mocks limpios. Interface verdaderamente independiente. | Refactor de 3 tipos + actualizar imports en consumidores. Posible coste de re-test. |
| **B. Mantener `nested`** | Cero refactor. | Mocks acoplados; tests de App posible se benefician de la limpieza pero no urgente. |
| **C. Re-tipear con genéricos (`IEquityCalculator<TResult>`)** | Máxima abstracción. | Over-engineering para 3 tipos. |

**Preguntas concretas:**
1. ¿Hay actualmente tests que mockeen las 3 interfaces problemáticas o todos usan implementaciones reales?
2. ¿El refactor introduce algún breaking change para `OpenScrape.App`?
3. ¿Hay otros tipos `nested` ocultos que deban moverse al mismo tiempo?

**Bloqueo:** T-83 en `tasks.md`. **Decisiones afectadas:** DD-09.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-05 — ¿`PostflopDecisionService` se refactoriza por extracción de paths?

**Contexto:**
- `Services/PostflopDecisionService.cs` (1893 LOC) viola SRP fuerte.
- Contiene: 10+ paths de decisión (facing-bet/no-bet/check-raise/float-exit/probe/pot-control/delayed-value/c-bet/bluff/randomización), `Random.Shared.NextDouble()` directo en 11 puntos, `goto skipBluffCatch` en 2 puntos, pot commitment block duplicado en 2 lugares, bloque `if` vacío en `:1198-1203`.
- 50+ tests cubren el comportamiento; refactor preserva contrato pero implica cambios significativos.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Refactor: extraer cada path a clase dedicada (`FacingBetPath`, `NoBetPath`, `BluffCatchPath`, …)** | LOC por archivo razonable (<300). Tests más enfocados. Onboarding de devs nuevos viable. | Refactor significativo (1-2 sprints). Riesgo de regresión sutil. |
| **B. Refactor parcial: solo extraer pot commitment + bluff catch + eliminar `goto`** | Reduce 50% de las anomalías más visibles con menor riesgo. | El servicio sigue siendo grande. |
| **C. Mantener (estado actual)** | Cero riesgo. | Mantenibilidad degrada con cada nueva regla. |

**Preguntas concretas:**
1. ¿El proyecto tiene apetito de refactor mayor o prefiere "no romper lo que funciona"?
2. ¿Hay un dueño dispuesto a liderar el refactor?
3. ¿Hay tests de regresión suficientes (matriz 216 casos) para detectar drift sutil?

**Bloqueo:** T-58, T-59, T-55 (parciales). **Decisiones afectadas:** DD-11, DD-13. **Casos extremos:** EC-07, EC-08, EC-13.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-06 — ¿`AutoCalibrationService.PreviewAndApply` se corrige (BUG OldValue hardcoded)?

**Contexto:**
- 🔴 **BUG conocido**: `Services/AutoCalibrationService.cs:174-208` tiene `OldValue` literales (`45`, `40`) en lugar de leer del `StrategyProfile` activo.
- La UI muestra calibraciones incorrectas si el usuario tiene profile distinto a los defaults históricos.
- Q-DOM-07 (questions.md raíz) confirma que el coaching es relevante → este bug afecta directamente la confianza en la herramienta.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Inyectar `IOptionsMonitor<StrategyProfile>` y leer `OldValue` en cada llamada** | Calibración correcta. Coherencia con resto del módulo. | Refactor del servicio + nuevo test. ~1 día. |
| **B. Pasar el profile como parámetro de `PreviewAndApply`** | Equivalente, más explícito. | Acopla cada caller. |
| **C. Deshabilitar feature hasta que se corrija** | Evita confundir usuarios. | Pierde coaching value que ya está parcialmente funcional. |
| **D. Mantener** | Cero esfuerzo. | Bug erosiona confianza; calibraciones acumulativas drift. |

**Preguntas concretas:**
1. ¿Cuántos usuarios usan actualmente la auto-calibración? (decisión de prioridad)
2. ¿Hay datos para validar el output esperado tras el fix? (test acceptance)
3. ¿La calibración se aplica via JSON file write, o vía hot-reload de `IOptionsMonitor`?

**Bloqueo:** T-75. **Decisiones afectadas:** DD-13. **Casos extremos:** EC-09.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-07 — ¿`ExploitabilityCalculator` parametriza `BigBlind`?

**Contexto:**
- 🟡 **Anomalía**: `Services/ExploitabilityCalculator.cs:93, 322-348` tiene `BigBlind = 1.0` hardcoded.
- Reportes en BB/100 escalan incorrectamente para stakes ≠ NL1.
- `StrategyAnalyzerService` consume los mismos números → cascade de errores en pestaña Estadísticas.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Inyectar `IOptions<StrategyProfile>` y leer `BigBlind`** | Cálculo correcto, escala con el stake del usuario. | ~2 horas. |
| **B. Pasar `bigBlind` como parámetro de `RecordDecision`** | Más explícito, menor acoplamiento. | Cada caller debe pasarlo. |
| **C. Mantener hardcoded** | Sin cambios. | Errores 10× en NL10, 5× en NL5, etc. |

**Preguntas concretas:**
1. ¿El usuario opera en un solo stake o varía? Q-DOM-03 confirma micro-stakes → BigBlind ∈ {0.02, 0.05, 0.10}.
2. ¿La pestaña Estadísticas se considera "ground truth" para el usuario? Si sí, el bug genera decisiones de coaching erróneas.

**Bloqueo:** T-74. **Decisiones afectadas:** DD-14. **Casos extremos:** EC-10.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-08 — ¿`OpponentTracker` se persiste cross-sesión? (cf. Q-FSM-02)

**Contexto:**
- Pregunta global ya respondida en `questions.md` raíz (Q-FSM-02): **"Actualmente no tiene uso, pero se deberían persistir"**.
- Implicación específica de DecisionMaker: cómo persistir.
- Opciones técnicas:
  - Marten document `OpponentProfile` con `Id = playerId` (alias OCR).
  - JSON file local por sesión.
  - SQLite local.

**Preguntas concretas:**
1. ¿Identidad del jugador es estable cross-sesión? El alias OCR de la sala puede cambiar entre sesiones (cambio de nick) → un `playerId` Marten requeriría reconciliación humana.
2. ¿Privacidad/GDPR? Q-PERM-DATA-02 dice "anonimizar a player1, player2..." → ¿cómo se reconcilian profiles si los IDs son anónimos?
3. ¿Cuándo se persiste? Al cerrar la app, al cerrar la sesión, en cada update?
4. ¿Cuánto tiempo se mantiene un profile sin actividad? (TTL).

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Marten document persistido al `EndSession`** | Coherente con `GameSession`/`HandRecord`. Query histórica natural. | Schema cambia; migración. Reconciliación de IDs anónimos compleja. |
| **B. JSON local por jugador, anonimizado** | Sin BD, simple. | No queryable; backups manuales. |
| **C. Mantener en memoria (estado actual)** | Sin cambios. | Profile se pierde en cada restart. |

**Bloqueo:** T-68 en `tasks.md`. **Decisiones afectadas:** DD-05.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-09 — ¿`DetermineAction` valida input (equity ∈ [0,100], NaN, etc.)? (cf. Q-FSM-01)

**Contexto:**
- Q-FSM-01 ya respondido en raíz: **"No se resetea, debería implementarse"** (sobre watchdog general).
- Pregunta específica: cómo manejar `equity = NaN`, `equity < 0`, `equity > 100`, `pot = 0`, `villainBet < 0`, `numOpponents = 0`.
- Hoy NO hay validación; comportamiento es indefinido (ver EC-01, EC-02, EC-16).

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Lanzar `ArgumentException` al detectar input inválido** | Detección temprana; el game loop ve el error y puede log + skip. | Refactor de callers para handle exception. |
| **B. Devolver `Action = Check` con tag `[INPUT-INVALID]`** | App no se rompe; comportamiento conservador. | Bug upstream se oculta más tiempo. |
| **C. Log + clamp** (clamp equity a [0, 100], pot a max(1, pot), etc.) | Robustez máxima. | Difícil saber qué clamps son seguros para todos los paths. |

**Preguntas concretas:**
1. ¿Qué prefiere el usuario: app que falla rápido o app que tolera input dudoso?
2. ¿Hay ya algún log que detecte estos casos en el caller (`UnifiedPokerCalculator`)?
3. ¿La validación es responsabilidad del facade (App) o del motor (DecisionMaker)?

**Bloqueo:** T-84. **Decisiones afectadas:** ninguna formal. **Casos extremos:** EC-01, EC-02, EC-16.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-10 — ¿Inyectar `IRandomProvider` para testabilidad y reproducibilidad?

**Contexto:**
- DD-11 captura: `Random.Shared.NextDouble()` se usa directamente en 11 puntos de `PostflopDecisionService`.
- Tests sobre randomización son estadísticos (1000 ejecuciones, ratio ±5%) — lentos y flakey.
- No hay forma de reproducir una decisión específica para debug.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. `IRandomProvider` inyectable (singleton en producción, seeded en tests)** | Tests reproducibles + debug de spots específicos posible. | Refactor de 11 callsites. |
| **B. Mantener `Random.Shared`** | Cero cambios. | Tests estadísticos eternos. |
| **C. Inyectar solo en paths de mixing (c-bet/check-raise/randomización), no en otros** | Granularidad. | Inconsistencia. |

**Preguntas concretas:**
1. ¿Hay valor real en reproducir una mano específica para debug, o el corpus de tests cubre el comportamiento esperado?
2. ¿Qué scope para el `IRandomProvider`: singleton (compartido) o transient (uno por mano)?
3. ¿La interfaz expone solo `NextDouble()` o más operaciones?

**Bloqueo:** T-56. **Decisiones afectadas:** DD-11.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-11 — ¿`EquityCalculatorService.RecommendedAction` mueve thresholds a `StrategyProfile`?

**Contexto:**
- 🟡 **Anomalía**: `Services/EquityCalculatorService.cs` genera una `RecommendedAction` heurística con thresholds **hardcoded** (`equity > 70 → "raise"`, `equity > 50 → "call"`, etc.).
- El resto del motor (PostflopDecisionService) usa `StreetThresholds` configurables.
- La recomendación de `EquityCalculatorService` se muestra en la UI como "preview" antes de la decisión final → si los números no coinciden, el usuario se confunde.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Mover thresholds a `StrategyProfile.PreviewThresholds`** | Consistencia. UI alineada con motor. | Nueva sección en JSON. |
| **B. Eliminar `RecommendedAction` (que la UI use el resultado de `PostflopDecisionService` directamente)** | Una fuente de verdad. | Refactor de UI; coste según acoplamiento. |
| **C. Mantener** | Sin cambios. | Inconsistencia visible. |

**Preguntas concretas:**
1. ¿La UI usa `RecommendedAction` para algo distinto a la decisión final del motor?
2. ¿Es un placeholder que precedió al motor postflop completo y nunca se eliminó?

**Bloqueo:** T-44. **Decisiones afectadas:** ninguna formal.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-12 — ¿Eliminar bloque `if` vacío en `PostflopDecisionService.cs:1198-1203`?

**Contexto:**
- 🔴 Bloque `if (...) { }` sin lógica dentro.
- No hay log ni anotación de cuándo se entra.
- Posible:
  - **Debug residual** que el dev olvidó eliminar.
  - **Path no terminado** que dejó alguien para implementación futura.
  - **Placeholder** para una optimización planeada que nunca llegó.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar el bloque** | -6 LOC. | Si era path no terminado, se pierde la nota visible. |
| **B. Mantener con comentario `// TODO: implementar caso X`** | Visibilidad de la deuda. | Sigue siendo código muerto. |
| **C. Implementar la lógica que faltaba** | Cierra la deuda. | Requiere saber qué condición evaluaba. |

**Preguntas concretas:**
1. ¿Git blame indica autor + intención del commit que introdujo el bloque?
2. ¿Hay algún issue/ticket que mencione este path?
3. ¿La condición del `if` (sin entrar al cuerpo) tiene side effects que se pierden si se elimina?

**Bloqueo:** T-58. **Casos extremos:** EC-08.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Q-DM-13 — ¿`obj/Debug/net8.0/` y `obj/Debug/net9.0/` se eliminan del repo?

**Contexto:**
- Repo versiona `obj/Debug/net8.0/`, `obj/Debug/net9.0/`, `obj/Debug/net10.0/` (mismo en Release).
- csproj declara solo `net10.0`.
- Restos de migraciones de target framework no limpiados.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Limpiar carpetas legacy + añadir `obj/` a `.gitignore`** | Repo más pequeño + buena práctica. | Single commit. |
| **B. Mantener** | Sin cambios. | Repo más grande, confusión para devs nuevos. |

**Preguntas concretas:**
1. ¿Hay alguna razón específica para versionar `obj/`? (poco común)
2. ¿`.gitignore` ya excluye otros `obj/` y este es excepción, o nadie lo excluyó?

**Bloqueo:** T-82. **Decisiones afectadas:** DD-15.

🔴 **Respuesta:**

> _(escribe aquí)_

---

## Resumen

| ID | Bloqueo (tasks.md) | Severidad | Estado |
|----|--------------------|-----------|--------|
| Q-DM-01 — `HandEvaluator` legacy | T-29 | 🟡 | abierta |
| Q-DM-02 — `IPreflopEquityCalculator` | T-28 | 🟡 | abierta |
| Q-DM-03 — `PreflopAnalyzer` static vs interface | T-36 | 🟡 | abierta |
| Q-DM-04 — Tipos `nested` a `DTOs/` | T-83 | 🟡 | abierta |
| Q-DM-05 — Refactor `PostflopDecisionService` | T-58/T-59/T-55 | 🟡 | abierta |
| Q-DM-06 — Fix `AutoCalibration` BUG | T-75 | 🔴 | abierta (BUG activo) |
| Q-DM-07 — Parametrizar `BigBlind` en `Exploitability` | T-74 | 🟡 | abierta |
| Q-DM-08 — Persistir `OpponentTracker` | T-68 | 🟡 | abierta (Q-FSM-02 ya respondida globalmente: sí) |
| Q-DM-09 — Validación input `DetermineAction` | T-84 | 🔴 | abierta (Q-FSM-01 ya respondida globalmente: sí) |
| Q-DM-10 — `IRandomProvider` | T-56 | 🟡 | abierta |
| Q-DM-11 — `RecommendedAction` thresholds | T-44 | 🟡 | abierta |
| Q-DM-12 — Bloque `if` vacío `:1198-1203` | T-58 | 🔴 | abierta |
| Q-DM-13 — Limpiar `obj/` legacy | T-82 | 🟢 | abierta (cosmético) |

**3 preguntas con severidad 🔴** son bugs/gaps activos:
- Q-DM-06 (AutoCalibration OldValue),
- Q-DM-09 (validación input DetermineAction),
- Q-DM-12 (bloque if vacío).

Las demás (🟡) son refactors de calidad sin urgencia operativa.

---

## Cómo continuar

Cuando hayas respondido (todas o un subset):

- **Si respondes todo:** dime "preguntas DM respondidas" → el Reviewer integra y cierra el bloque.
- **Si respondes solo algunas:** dime cuáles dejas abiertas para una segunda iteración.
