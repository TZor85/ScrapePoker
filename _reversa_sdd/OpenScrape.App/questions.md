# OpenScrape.App — Preguntas Abiertas

> Lacunas detectadas en la capa App (composition root, UI, captura/OCR, game loop, telemetría, persistencia orquestada) que requieren validación humana antes de implementar/migrar/refactorizar.
> Modo de respuesta: `file` (responder editando este archivo, secciones marcadas con `🔴 Respuesta:`).
> Cada pregunta tiene un ID estable (`Q-APP-NN`) para referencia desde `tasks.md`, `decisions.md`, `edge-cases.md`.
> **Algunas preguntas están parcialmente cubiertas en `_reversa_sdd/questions.md` raíz** (Q-DOM-10 secretos, Q-FSM-01 watchdog, Q-FSM-02 persistencia OpponentProfile). Aquí se duplican solo cuando hay implicación específica de implementación de la App.

---

## Q-APP-01 — `appsettings.json` con credenciales reales: ¿rotar + purgar histórico Git?

**Contexto:**
- 🔴 **Anomalía DD-18 / Scout `inventory.md`:** `src/OpenScrape.App/appsettings.json` (~31 KB, versionado) contiene **connection string PostgreSQL real** y **`EncryptionKey` real** del usuario.
- `appsettings.Development.json` (gitignored) se introdujo posteriormente para mover secretos, pero **el legacy nunca se purgó del histórico Git**.
- Cualquiera con acceso al repo o al histórico ve credenciales válidas hasta la fecha de rotación.
- ADR-0018 (`Config Environment Development Secrets Gitignored`) mandata `appsettings.Development.json` como única fuente de secretos — **contradicho por la realidad del repo**.
- Q-DOM-10 raíz pregunta el modelo comercial; **antes de cualquier distribución** (incluso interna) hay que decidir.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Rotar credenciales + purgar histórico con `git filter-repo`** | Compromiso completamente cerrado. Permite open-source. | Reescritura de SHAs, rebase forzado, equipo debe re-clonar. Pérdida de blame en commits afectados. |
| **B. Rotar credenciales + dejar histórico** | Bajo riesgo si las credenciales viejas dejan de funcionar. | Atacante histórico ve credenciales pasadas (pueden haber filtrado en backups). |
| **C. Mover a User Secrets / Azure Key Vault / etc. + reescribir `Program.cs:43-50`** | Patrón estándar .NET. Cero secretos en repo. | Refactor de configuración + onboarding más complejo. |
| **D. Mantener** | Cero esfuerzo. | Riesgo activo de seguridad. |

**Preguntas concretas:**
1. ¿La BD apuntada por las credenciales versionadas tiene datos reales del usuario o es disposable? (severidad)
2. ¿Hay backups del repo en sistemas no controlados (CI cache, Docker layers cache)?
3. ¿Existe apetito para `git filter-repo` o el equipo prefiere rotar y vivir con el histórico?
4. ¿Se planea distribuir el binario fuera de la máquina del usuario? (cambia urgencia)

**Bloqueo:** T-09 (decisión sobre `EncrypterHelper`), Pré-requisito § configuración. **Decisiones afectadas:** DD-18 (anomalía activa). **Casos extremos:** EC-03.

🔴 **Respuesta:**

> Si, rota y purgar Git
> 1. Datos reales
> 2. No
> 3. No existe
> 4. No

---

## Q-APP-02 — `EncrypterHelper` con IV fija de 16 ceros: ¿reescribir o eliminar?

**Contexto:**
- 🔴 **Anomalía DD-19:** `Helpers/EncrypterHelper.cs:1-145` usa AES-CBC con `Key = SHA256(secret)` y `IV = new byte[16]` (16 ceros) reusada entre todos los cifrados con la misma clave.
- Vulnerabilidad conocida: dos plaintexts con prefijo común producen ciphertexts con prefijo común — ataque de prefijo conocido.
- Sin autenticación (no GCM) → atacante puede modificar ciphertext sin detectarse.
- **Estado de uso desconocido:** posible código muerto post-Marten; no se localizó consumidor activo en el grafo de DI ni en `FrmMain`.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Reescribir con IV aleatoria por cifrado, prefijar IV en ciphertext (16 bytes)** | Patrón seguro estándar. Round-trip transparente. | Ciphertexts cambian de tamaño y formato — incompatible con datos existentes cifrados con la versión legacy. |
| **B. Migrar a `AesGcm` (autenticado)** | Más seguro (autentica + cifra). | Igual incompatibilidad + nuevo formato. |
| **C. Eliminar el helper** (si confirma código muerto) | -145 LOC, supresión de vector de ataque. | Hay que verificar que nadie consume y migrar los datos cifrados existentes (si hay). |
| **D. Mantener** | Cero esfuerzo. | Vulnerabilidad latente. |

**Preguntas concretas:**
1. ¿Hay datos cifrados con `EncrypterHelper.Encrypt` persistidos en disco/BD/configuración? (Marten, archivos JSON, registry)
2. ¿Algún test invoca `EncrypterHelper.Encrypt`/`Decrypt` directamente?
3. Si hay datos cifrados existentes, ¿son recuperables (regenerables) o requieren migración?

**Bloqueo:** T-09. **Decisiones afectadas:** DD-19.

🔴 **Respuesta:**

> Migrar a AesGcm
> 1. Quizás archivos json
> 2. No lo recuerdo
> 3. Requieren migración

---

## Q-APP-03 — `PokerDecisionFacade` registrado pero sin consumidores: ¿eliminar o completar cutover?

**Contexto:**
- 🟡 **Anomalía DD-12:** `Program.cs:118-119` registra `IPokerDecisionFacade → PokerDecisionFacade` como scoped, **pero ningún consumidor lo inyecta**.
- `FrmMain.btnCapture_Click → ProcessPostFlopAsync` consume directamente `IPokerCalculator` + `IPostflopDecisionService`.
- La telemetría granular planificada del facade ("5 fases medidas" — equity → texture → profile → decision → sizing) **no se ejecuta en producción**.
- 231 LOC sin consumidor activo, riesgo de bit-rot y confusión documental (CLAUDE.md menciona el facade como "punto único" — falso).

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Eliminar `PokerDecisionFacade.cs` + registro DI + cualquier referencia** | -231 LOC. Documentación coherente. | Pérdida del trabajo invertido en su construcción. |
| **B. Completar cutover Fase 2 — migrar `GameCoordinator/FrmMain` a consumir el facade** | Pipeline centralizado con telemetría granular. Coherente con ADR-0006. | Refactor de `GameCoordinator` (793 LOC). Posible regresión de timing/orden. |
| **C. Mantener registrado, marcar `[Obsolete]` con plan de eliminación** | Visibilidad de la deuda. | Sigue siendo código muerto. |

**Preguntas concretas:**
1. ¿Hubo razón explícita para no completar el cutover Fase 2? (motivo histórico)
2. ¿La telemetría granular del facade aporta valor que la actual (`GameCoordinator` directo) no cubre?
3. ¿Hay capacidad de un sprint dedicado al cutover, o se prefiere eliminar la deuda?

**Bloqueo:** T-89/T-90 en `tasks.md` (composition root). **Decisiones afectadas:** DD-12. **ADRs afectados:** ADR-0006 § Pipeline Unificado (parcialmente contradicho).

🔴 **Respuesta:**

> Eliminar
> 1. No
> 2. No lo se
> 3. Eliminamos la deuda

---

## Q-APP-04 — `GameLoopCoordinator` con `FeatureFlags.UseGameLoopCoordinator=false`: ¿activar o eliminar?

**Contexto:**
- 🟡 **Anomalía DD-04:** `Services/GameLoopCoordinator.cs` (213 LOC) implementa el game loop moderno con `PeriodicTimer + IAsyncDisposable + CancellationToken`, **pero está apagado** detrás de `FeatureFlags.UseGameLoopCoordinator`.
- Cuando el flag está en `false` (estado actual), `RunOnceAsync` retorna `GameLoopResult { Empty = true }` por tick — código muerto en producción.
- El game loop real vive en `FrmMain.BackgroundWorker1_DoWork:2737-2897` (`BackgroundWorker` clásico).
- Migración Fase 4 prevista pero nunca completada.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Activar el flag y migrar `FrmMain` para consumir `GameLoopCoordinator`** | Cancelación cooperativa, async/await first-class, `IAsyncDisposable`. | Refactor del UI dispatch (`Invoke`/`BeginInvoke`). Validación exhaustiva del comportamiento idéntico a `BackgroundWorker`. |
| **B. Eliminar `GameLoopCoordinator` + `FeatureFlags.UseGameLoopCoordinator`** | -213 LOC. `BackgroundWorker` battle-tested permanece. | Pérdida del camino moderno; bloquea mejoras futuras. |
| **C. Mantener (estado actual)** | Cero riesgo. | Deuda visible; bit-rot del coordinator. |

**Preguntas concretas:**
1. ¿Hay apetito para Fase 4 cutover en los próximos 2-3 sprints?
2. ¿Tests existentes (TT-XX) cubren equivalencia funcional `BackgroundWorker` vs `GameLoopCoordinator`?
3. ¿Hay alguna característica deseable (ej. cancelación al cerrar mesa) que requiera el coordinator?

**Bloqueo:** T-91/T-92. **Decisiones afectadas:** DD-04. **Decisión coordinada con:** Q-APP-13 (zombie loop).

🔴 **Respuesta:**

> Eliminar
> 1. No
> 2. No lo sé
> 3. Ese es buen ejemplo, cancelar al cerrar mesa

---

## Q-APP-05 — `FormImage` con path hardcoded `C:\Code\Poker\...\Games`: ¿hacer configurable?

**Contexto:**
- 🔴 **Anomalía:** `Forms/FormImage.cs` referencia un directorio físico hardcoded (`C:\Code\Poker\ScrapePoker\Games` o similar).
- Cualquier máquina que NO sea la del usuario original: el formulario falla silenciosamente al abrir o lanza `DirectoryNotFoundException`.
- Bloquea onboarding de developers nuevos y cualquier distribución.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Mover a `appsettings.json` → `Paths.GamesFolder`** | Configurable por máquina. | Debe definirse default razonable (ej. `%LOCALAPPDATA%\OpenScrape\Games`). |
| **B. Resolver relativo a `AppContext.BaseDirectory + "Games"`** | Cero configuración. | Si `Games/` no se distribuye, el form falla igual. |
| **C. Mantener hardcoded** | Cero esfuerzo. | Funciona solo en la máquina original. |
| **D. Eliminar `FormImage` si es código muerto** (preview/debug abandonado) | -N LOC. | Verificar nadie lo invoca. |

**Preguntas concretas:**
1. ¿Qué uso real tiene `FormImage`? (debug, preview, exploración manual de capturas)
2. ¿Hay archivos en `Games/` que sean parte del proyecto (committed) o son outputs de captura (gitignored)?
3. ¿Se considera código muerto?

**Bloqueo:** T-XX (formulario `FormImage`). **Pré-requisito:** mencionado en `tasks.md`.

🔴 **Respuesta:**

> Hay que hacer la ruta configurable
> 1. Debug
> 2. gitignored
> 3. No

---

## Q-APP-06 — `eng.traineddata` duplicada (embebida + output): ¿consolidar?

**Contexto:**
- 🟡 **Anomalía:** El archivo Tesseract `eng.traineddata` se distribuye DOS veces:
  - `Resources/tessdata/eng.traineddata` con `<EmbeddedResource>` (embebido en el .exe).
  - `tessdata/eng.traineddata` con `<None Update><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` (archivo en output).
- ~30 MB duplicados en cada build.
- `OcrService` consume desde el path output; el recurso embebido nunca se extrae.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Mantener solo `tessdata/` output** | -30 MB del .exe. | Distribución requiere shipping de la carpeta `tessdata/`. |
| **B. Mantener solo embebido + extraer a temp en arranque** | .exe self-contained sin carpeta external. | Lógica de extracción + path resolution + cleanup. ~50 LOC. |
| **C. Mantener ambos** (estado actual) | Robustez si una falla. | 30 MB duplicados. |

**Preguntas concretas:**
1. ¿Cómo se distribuye la app? (`dotnet publish` self-contained, MSI installer, copia manual)
2. ¿Hay escenario donde `tessdata/` se borre por accidente y el embebido sirva como fallback?
3. ¿El tamaño del .exe importa? (marginal en desktop, importante en cualquier auto-update)

**Bloqueo:** T-03. **Pré-requisito:** mencionado en `tasks.md`.

🔴 **Respuesta:**

> Si, hay que consolidarlo
> 1. actualmente, copia manual, hay que mejorarlo
> 2. No
> 3. No es importante

---

## Q-APP-07 — `FrmMain` (4 502 LOC, god-class): ¿split por pestañas en partial classes?

**Contexto:**
- 🟡 **Anomalía:** `Forms/FrmMain.cs` tiene 4 502 LOC tras reducir desde 5 977 (refactoring Fase 1-8 ya completado).
- Contiene la totalidad del game loop, manejo UI de 5 pestañas (Juego/Config/Tablas/Logs/Historial), persistencia orquestada, telemetría display, calibración auto, backtest A/B.
- Costoso de navegar y entender; tests de regresión escasos sobre la totalidad del flow.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Split en partial classes por pestaña** (`FrmMain.Game.cs`, `FrmMain.Config.cs`, …) | LOC por archivo razonable (<1000). Onboarding viable. | Refactor mecánico pero sensible — los métodos cross-pestaña (event handlers, state shared) requieren cuidado. |
| **B. Extraer pestañas como UserControls** (`UC_Game`, `UC_Config`, …) con DI propio | Encapsulación real. Cada UC testeable. | Refactor mayor. Comunicación cross-control requiere event bus o mediator. |
| **C. Continuar el refactor extracting servicios** (siguiendo el patrón ya iniciado: `GameCoordinator`, `ScreenReaderService`, `TableLayoutService`) | Coherente con la dirección actual. | Cada extracción reduce ~200-500 LOC, pero `FrmMain` sigue siendo el orchestrator. |
| **D. Mantener** | Cero riesgo. | Mantenibilidad sigue degradándose. |

**Preguntas concretas:**
1. ¿Qué bloque de los 5 (Juego/Config/Tablas/Logs/Historial) genera más fricción al editar?
2. ¿Hay capacidad para un sprint dedicado (riesgo de regresión visual + binding events)?
3. ¿Existe matriz de tests UI (manual o automatizada) que cubra los 5 tabs?

**Bloqueo:** T-XX (refactor). **Decisiones afectadas:** DD-01 (FrmMain scope). **Pré-requisito:** mencionado en `tasks.md`.

🔴 **Respuesta:**

> Si, hagamos split por pestañas
> 1. Config
> 2. Si
> 3. No

---

## Q-APP-08 — `FormListApps` filtro `"NL H"` hardcoded: ¿hacer configurable?

**Contexto:**
- 🟡 **Anomalía:** `Forms/FormListApps` filtra ventanas visibles cuyo título contenga literalmente `"NL H"` (Hold'em No Limit en español/inglés). Constante hardcoded.
- Otras variantes (`"NL Hold'em"`, `"NLHE"`, `"PLO"`, `"PL Omaha"`, etc.) NO se listan aunque sean ventanas válidas del cliente.
- Q-DOM-02 raíz pregunta qué salas se soportan; este filtro es un gate operacional.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Mover el filtro a `appsettings.json` → `WindowFilter.IncludePatterns: ["NL H", "PLO", …]`** | Configurable sin recompilar. Soporta multi-formato. | Documentar el setting en README. |
| **B. UI con checkboxes de tipos de juego soportados** | Auto-discoverable. | Requiere reload de la lista al cambiar checkboxes. |
| **C. Eliminar el filtro y mostrar todas las ventanas visibles** | Máxima libertad. | Saturación visual con Visual Studio, browsers, etc. |
| **D. Mantener** | Cero esfuerzo. | Solo NLHE soportado oficialmente. |

**Preguntas concretas:**
1. ¿El bot soporta hoy en día algo distinto a NLHE? (motor de decisión, regions del table map)
2. ¿La filosofía del producto es "NLHE only" o "multi-formato futuro"?
3. ¿Qué patrones de título usan las salas concretas que hoy operan? (depende de Q-DOM-02)

**Bloqueo:** T-XX (formulario `FormListApps`).

🔴 **Respuesta:**

> Hay que hacerlo configurable
> 1. No, solo NLHE
> 2. NLHE only
> 3. "NL H"

---

## Q-APP-09 — Hot reload de `StrategyProfile`: ¿documentar como inválido o implementar reconstrucción?

**Contexto:**
- 🟡 **EC-04:** `IConfiguration` con hot reload activado (default `Host.CreateDefaultBuilder`) propaga cambios en `appsettings.json` mientras la app corre.
- `StrategyProfileValidator.Validate` solo se ejecuta UNA vez en `Program.Main` antes de `Application.Run`.
- `ThresholdsRegistry` (singleton) **no observa cambios** post-construcción → cambios silenciosamente ignorados.
- Usuario edita JSON esperando aplicación; cree que aplicó pero el motor sigue con thresholds viejos.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Suscribirse a `IOptionsMonitor<StrategyProfile>.OnChange` + re-validar + reconstruir `ThresholdsRegistry`** | Hot reload real. UX intuitiva. | Concurrencia: lecturas in-flight ven thresholds inconsistentes. Requires lock o double-buffering. |
| **B. Documentar que cambios requieren reinicio + deshabilitar hot reload** (`reloadOnChange: false` en `Program.cs:43-50`) | Comportamiento explícito y honesto. | UX menos amigable — usuario debe reiniciar. |
| **C. UI con botón "Recargar profile"** que valida y reconstruye en demanda | Control explícito + cero race conditions. | Requiere UI nueva en Config tab. |
| **D. Mantener (estado actual)** | Cero esfuerzo. | Bug latente — usuario cree que aplicó pero motor ignora. |

**Preguntas concretas:**
1. ¿Cuántas veces el usuario edita el profile en una sesión típica? (frecuencia → urgencia)
2. ¿La auto-calibración (Q-DM-06) escribe al JSON o usa otro mecanismo?
3. ¿El profile es propiedad del usuario o del producto? (afecta UX esperada)

**Bloqueo:** T-XX (orquestación de configuración). **Decisiones afectadas:** DD-03 (fail-fast). **Casos extremos:** EC-04.

🔴 **Respuesta:**

> Implementar reconstruccion
> 1. Una vez dejado bien, muy poco
> 2. Creo que al json
> 3. Del usuario

---

## Q-APP-10 — Multi-monitor con DPI distinto: ¿per-monitor DPI awareness?

**Contexto:**
- 🟡 **EC-10:** `User32.SetProcessDPIAware()` se invoca al inicio del proceso, NO por monitor.
- Si el usuario mueve el cliente entre monitores con DPI distinto (100 % vs 150 %), `CoordinateScaler` (calibrado en monitor original) NO sabe que ahora estamos en otro.
- Las regiones de `tableMap.json` no matchean el bitmap del nuevo monitor → OCR falla silenciosamente leyendo áreas equivocadas.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. `SetProcessDPIAwarenessV2(PerMonitorAwareV2)` + listener `WM_DPICHANGED` + reinicializar `CoordinateScaler`** | DPI cambia transparentemente. | P-Invoke adicional. Tests en multi-monitor reales (difíciles de automatizar). |
| **B. Detectar monitor en cada captura y aplicar factor por monitor** | Granular. | Overhead pequeño por captura (~1-2 µs). |
| **C. Documentar que el cliente debe quedarse en el monitor donde se calibró** | Cero código. | UX limitada — usuario no puede mover el cliente. |
| **D. Mantener (estado actual)** | Cero esfuerzo. | Falla silenciosa en multi-monitor. |

**Preguntas concretas:**
1. ¿El usuario opera en single-monitor o multi-monitor habitualmente?
2. ¿Hay reportes históricos de OCR fallando tras mover el cliente?
3. ¿Q-APP-09 (hot reload) y este se resuelven juntos como pack de "sensibilidad a entorno cambiante"?

**Bloqueo:** T-XX (capture pipeline). **Casos extremos:** EC-10.

🔴 **Respuesta:**

> 1. Single-monitor
> 2. No
> 3. No lo tengo claro

---

## Q-APP-11 — Validación de `BoardCards` con duplicados: ¿App o DecisionMaker?

**Contexto:**
- 🟡 **EC-11:** OCR ruidoso puede leer `[As, Kd, As]` (As duplicado por dHash collision o región mal calibrada).
- `BitHandEvaluator` evalúa con cartas repetidas → ranking incoherente (FullHouse falso).
- `MonteCarloSimulator.TryDrawFromRange` puede marcar `IsReliable = false` (con `BlockedComboUnreliableThreshold`) pero la decisión ya se ejecutó.
- **Hoy no hay validación** ni en App ni en DecisionMaker.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Validar en App** (en `GetCardsFlop/Turn/RiverUseCase` o en el caller de `_pokerCalculator.Calculate`) → si duplicado, abortar ciclo + log | Detección temprana, antes de invocar el motor. | Cada caller debe validar — fácil olvidar. |
| **B. Validar en DecisionMaker** (`UnifiedPokerCalculator.Calculate` lanza `ArgumentException` si `communityCards.Distinct().Count() != communityCards.Count`) | Una sola defensa, motor garantiza inputs limpios. | App debe handle excepción → log + skip ciclo. |
| **C. Validar en ambos** (defense in depth) | Máxima robustez. | Duplicación de lógica. |
| **D. Mantener (sin validación)** | Cero esfuerzo. | Decisión 🔴 basada en mano evaluada incorrectamente. |

**Preguntas concretas:**
1. ¿La filosofía es "fail loudly cuanto antes" o "tolerar inputs dudosos"? (alinear con Q-DM-09)
2. ¿`MonteCarloSimulator.IsReliable=false` se exhibe en UI o se ignora?
3. Si se aborta el ciclo, ¿se cuenta como mano "no decidida" en métricas o se descarta silenciosamente?

**Bloqueo:** T-XX (`GetCardsFlopUseCase` callers). **Casos extremos:** EC-11. **Decisión coordinada con:** Q-DM-09.

🔴 **Respuesta:**

> La opcion B
> 1. tolerar
> 2. Se exhibe
> 3. Se descarta

---

## Q-APP-12 — Marten outage durante persistencia: ¿cola de fallback en disco?

**Contexto:**
- 🟡 **EC-09:** PostgreSQL crashea durante `await _gameLoggerService.FinalizeAndPersistHandAsync()`.
- Catch loggea error pero NO propaga; mano se pierde silenciosamente.
- `_sessionTotalHands` (Interlocked) divergente del recuento real persistido.
- Sesiones largas (4-6 h) con outage transitorio de DB → pérdida de datos críticos para coaching.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Cola de manos pendientes en disco (file-based) + retry al volver la BD** | Cero pérdida en outage transitorio. | Lógica de queue + dedup + cleanup tras persist exitoso. ~150 LOC. |
| **B. Notificar al usuario con `MessageBox`/indicador UI cuando falla persist** | Usuario consciente. | UX intrusiva; modal interrumpe juego. |
| **C. Indicador discreto en status bar** ("⚠️ N manos sin persistir") | UX consistente. | Necesita reconectar y re-intentar. |
| **D. Mantener (estado actual)** | Cero esfuerzo. | Pérdida silenciosa. |

**Preguntas concretas:**
1. ¿La BD del usuario corre local o remota? Si local, outage es raro (servicio Windows reiniciado).
2. ¿El coaching depende de datasets completos o tolera lagunas?
3. ¿Existe acceso al filesystem fiable (HDD lleno, AV bloqueando escritura)?

**Bloqueo:** T-XX (`GameLoggerService`). **Casos extremos:** EC-09.

🔴 **Respuesta:**

> Opción C
> 1. remota
> 2. datasets completos
> 3. Si

---

## Q-APP-13 — `BackgroundWorker` zombie loop: ¿contador de fallos consecutivos + abort?

**Contexto:**
- 🟡 **EC-07:** `BackgroundWorker1_DoWork:2737-2897` envuelve cada iteración en try/catch — si una iteración lanza, loggea y `continue`.
- Sin contador de fallos consecutivos: el loop puede iterar 1000 veces fallando idénticamente.
- Logs llenos de errores sin escalación; CPU consumida sin valor.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Contador `_consecutiveFailures`; tras `MaxConsecutiveFailures=10`, abortar BG worker + `MessageBox` al usuario** | Loop muere ruidosamente. UX accionable. | Necesita reset del contador en cada éxito. |
| **B. Backoff exponencial entre iteraciones tras N fallos** (100 ms → 500 ms → 2 s → …) | Reduce CPU sin abortar. | Si el problema es transitorio, recupera; si permanente, sigue iterando. |
| **C. Combinar A + B** (backoff hasta umbral, luego abort) | Tolerante + robusto. | Más complejo. |
| **D. Mantener (estado actual)** | Cero esfuerzo. | Loop zombie posible. |

**Preguntas concretas:**
1. ¿Cuál es el umbral de "fallos sin recuperación posible"? (10 ciclos × 100 ms = 1 s; 100 ciclos = 10 s)
2. Si se aborta, ¿la app permanece abierta para que el usuario reseleccione mesa, o se cierra?
3. ¿Hay relación con Q-APP-04 (cutover a `GameLoopCoordinator`)? Allí la cancelación cooperativa es nativa.

**Bloqueo:** T-XX (game loop hardening). **Casos extremos:** EC-07. **Decisión coordinada con:** Q-APP-04.

🔴 **Respuesta:**

> Opcion B
1. 15 ciclos x 100 ms
2. Mensaje de error y se mantiene abierta
3. No lo tengo claro

---

## Q-APP-14 — OCR low confidence cascade: ¿cache last-known en lugar de "best effort"?

**Contexto:**
- 🟡 **EC-05:** `ScreenReaderService.ReadXxx` ejecuta 3 lecturas con preprocesamientos variados.
- Si las 3 retornan strings distintas (sin mayoría), retorna la primera no-vacía con `IsHighConfidence = false`.
- Game loop continúa con valor "best effort" — si es `0` para un bet o stack, motor decide con datos incoherentes.
- Caso típico: animation overlay durante cambio de turno.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Si confianza promedio < 0.70, retornar valor anterior cacheado (last-known)** + skip ciclo en preflop si es la primera lectura sin cache | Decisiones nunca basadas en "best effort" malo. | Requires cache last-known per region — ~10-15 fields × per-table state. |
| **B. Skip ciclo completo si alguna lectura clave es low confidence** | Conservador — no decidir vs decidir mal. | Pierde reactividad — el ciclo siguiente puede capturar bien. |
| **C. Incrementar `_failedOcrCount`; tras `MaxOcrRetries=2` consecutivas, abortar el ciclo y esperar** (estado parcialmente implementado) | Coherente con `MaxOcrRetries`. | Latencia adicional en spots reactivos. |
| **D. Mantener (estado actual)** | Cero esfuerzo. | Decisiones erradas en transiciones. |

**Preguntas concretas:**
1. ¿Cómo se invalida el last-known cache entre manos? (cada `StartNewHand`)
2. ¿Test logs muestran qué % de ciclos hoy tienen `IsHighConfidence=false`?
3. ¿La decisión `Skip ciclo` deja al usuario sin recommendation visible o se mantiene la última?

**Bloqueo:** T-XX (`ScreenReaderService`). **Casos extremos:** EC-05.

🔴 **Respuesta:**

> Las respuestas no las sé al 100%, decide tú la mejor opción

---

## Q-APP-15 — Tesseract crash con bitmap region out-of-bounds: ¿validación previa?

**Contexto:**
- 🔴 **EC-17:** `CoordinateScaler` puede producir region `(x, y, w, h) = (0, 0, 99999, 99999)` por bug de cálculo (`tableMap.json` corrupto, valores extremos).
- `Bitmap.Clone(rect, format)` lanza `OutOfMemoryException` paradójica si excede el bitmap.
- Tesseract puede crashear con `AccessViolationException` → **proceso muere**, sesión se pierde.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. `OcrService.ExtractTextFromRegionAsync` valida `(x + w) <= image.Width && (y + h) <= image.Height`** + retorna string vacío si no | Defensa local — Tesseract nunca recibe región inválida. | Solo cubre el caller específico; otros callsites pueden caer. |
| **B. Validación en `CoordinateScaler.ScaleRegion`** — clamp + log warning si overflow | Previene en origen. | Region clamped puede ocultar bugs upstream del table map. |
| **C. Combinar A + B** | Defense in depth. | Duplicación. |
| **D. Mantener** | Cero esfuerzo. | Crash de proceso latente. |

**Preguntas concretas:**
1. ¿Hay tests sobre `tableMap.json` corruptos (valores negativos, gigantes, NaN)?
2. ¿`tableMap.json` se valida en `LoadTableMapUseCase`? (rangos físicos razonables)
3. ¿Severidad real? (crash de proceso es 🔴, pero requiere `tableMap.json` corrupto — escenario raro)

**Bloqueo:** T-XX (`OcrService`). **Casos extremos:** EC-17.

🔴 **Respuesta:**

> Hay que hacer validación
> 1. No
> 2. No
> 3. Si

---

## Q-APP-16 — `RegionLookupCache.Initialize` no thread-safe en reload: ¿reasignación atómica?

**Contexto:**
- 🟡 **EC-21:** Usuario aplica nuevo `tableMap.json` en pestaña Tablas mientras `BackgroundWorker` está iterando.
- `RegionLookupCache.Initialize(newMaps)` reconstruye `Dictionary` interno.
- `Dictionary` no es thread-safe — reader puede ver estado inconsistente y lanzar `InvalidOperationException`.
- Captura por `BackgroundWorker.try/catch` (EC-07) — recupera al ciclo siguiente, pero error visible en logs.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Cambiar a `ConcurrentDictionary` con `Clear` + `TryAdd`** | Thread-safe nativo. | `Clear` no es atómico — readers pueden ver estado parcial vacío. |
| **B. Construir nuevo `Dictionary` aparte y reasignar atómicamente con `Volatile.Write`** | Reasignación atómica; readers ven nuevo o viejo, nunca parcial. | Pequeño refactor — campo `_regions` debe ser `volatile`. |
| **C. Pausar `BackgroundWorker` durante reload** (`_executeCapture = false; await; reload; = true`) | Cero races. | Flicker visual si reload es lento. |
| **D. Mantener** | Cero esfuerzo. | Excepción transitoria en logs (recuperada). |

**Preguntas concretas:**
1. ¿Reload de `tableMap.json` es operación frecuente o esporádica?
2. ¿La excepción transitoria afecta la UX de algún modo perceptible?
3. ¿La opción B se debería extender al `CardCacheService` (singleton lazy análogo)?

**Bloqueo:** T-XX (`RegionLookupCache`). **Casos extremos:** EC-21.

🔴 **Respuesta:**

> Opcion C
> 1. No, esporádica
> 2. Aparentemente no
> 3. Si

---

## Q-APP-17 — Equity cache: ¿LRU real o aceptar FIFO?

**Contexto:**
- 🟡 **DD-09 / EC-13:** `UnifiedPokerCalculator._equityCache` es `ConcurrentDictionary<string, double>` con `EquityCacheMaxSize=2048`. **No es LRU** — al saturarse, `TryAdd` retorna `false` y nuevas equities NO se cachean.
- Sesiones largas (>10 K manos) con muchos boards únicos: cache miss del 100 % en nuevos boards.
- Performance degradada (no bug, solo penalty de latencia ~30-100 ms por cálculo).

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Migrar a `LruCache<string, double>` thread-safe** | Hit ratio óptimo en sesiones largas. | Contention en `LinkedList` interno — ventaja sobre FIFO <5 % en hit ratio para este patrón. |
| **B. Cleanup periódico al alcanzar tope** (descarta 25 % oldest) | Mantiene FIFO simple, recupera espacio. | Heurística — no LRU real. |
| **C. Aumentar `EquityCacheMaxSize` a 8192 o 16384** | Capacidad cubre sesiones largas reales. | Memoria 4×-8× — aceptable (8 KB → 64 KB). |
| **D. Mantener (estado actual)** | Cero esfuerzo. | Performance degrada en sesiones largas. |

**Preguntas concretas:**
1. ¿Hay perfiles de sesión real con >2048 boards únicos? (instrumentar telemetría)
2. ¿La diferencia de latencia (30-100 ms) impacta UX o es invisible?
3. ¿`MemoryCache` con expiración temporal es semánticamente errado (equity es invariante) o útil como guardrail?

**Bloqueo:** T-XX (`UnifiedPokerCalculator`). **Decisiones afectadas:** DD-09. **Casos extremos:** EC-13.

🔴 **Respuesta:**

> Opcion A
> 1. Actualmente no, pero los habrá
> 2. invisible
> 3. Memorycahe

---

## Q-APP-18 — Cliente de poker cerrado durante captura: ¿detección proactiva?

**Contexto:**
- 🟡 **EC-01:** Usuario cierra ventana del cliente; `_handle: IntPtr` apunta a ventana destruida.
- `User32.GetWindowRect` retorna `false` o rect inválido. `PrintWindow` retorna `false`.
- Pipeline continúa con bitmap inválido; OCR produce strings vacíos; `Players=[]`; loop iter sin valor.
- **No hay log explícito** "ventana cerrada".

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. `User32.IsWindow(_handle)` antes de `GetWindowRect`** + auto-abrir `FormListApps` si false | Detección proactiva con UX accionable. | Comportamiento no-silencioso — el formulario aparece sin que el usuario lo invoque. |
| **B. Validación post-captura** (`if (image == null \|\| image.Width == 0) { LogWarning + skip }`) | Ciclo no-op explícito. | Usuario no sabe que reseleccionar. |
| **C. Combinar A + B**: validación silenciosa primero, prompt al usuario tras N ciclos consecutivos | Robusto + no intrusivo. | Más complejo. |
| **D. Mantener** | Cero esfuerzo. | App "colgada" desde el punto de vista del usuario. |

**Preguntas concretas:**
1. ¿El usuario cierra/reabre el cliente durante una sesión, o es excepcional?
2. ¿Hay valor en pause-and-prompt (modal) o es preferible silent-skip?
3. ¿`IsWindow` se comporta correctamente con minimized windows (no destroyed)?

**Bloqueo:** T-XX (capture pipeline). **Casos extremos:** EC-01.

🔴 **Respuesta:**

> Opcion C
> 1. Es excepcional
> 2. silent-skip
> 3. Si

---

## Q-APP-19 — Tesseract `eng.traineddata` corrupta o incompatible: ¿auto-recovery desde recurso embebido?

**Contexto:**
- 🔴 **EC-02:** Versión vieja/corrupta de `eng.traineddata` en `tessdata/` → `TesseractException: Unsupported Tesseract data version` en arranque.
- Catch en `OcrService:55-58` solo `Debug.WriteLine` y re-throw.
- `Host.Build()` propaga la excepción → usuario ve `MessageBox` genérico de WinForms con stack trace.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Catch específico en `OcrService` que sobrescribe `tessdata/eng.traineddata` con la versión embebida** + retry una vez | Auto-recovery sin intervención del usuario. | Requiere mantener traineddata embebida (Q-APP-06). Posible loop si embebida también está mal. |
| **B. Validación de versión previa** (parsear primer N bytes de la traineddata) | Detección temprana. | Frágil — formato Tesseract puede cambiar. |
| **C. Catch que muestra `MessageBox` con instrucciones accionables** ("Borre `tessdata/eng.traineddata` y relance") | UX clara. | Usuario debe seguir instrucciones. |
| **D. Mantener (estado actual)** | Cero esfuerzo. | UX pésima en escenario raro pero crítico. |

**Preguntas concretas:**
1. ¿Existe escenario real de traineddata corrupta? (filesystem error, Tesseract version mismatch tras update)
2. ¿La opción A es coherente con la decisión de Q-APP-06 (consolidar embebida + output)?
3. ¿Auto-overwrite de archivos en disco viola alguna política?

**Bloqueo:** T-XX (`OcrService`). **Casos extremos:** EC-02. **Decisión coordinada con:** Q-APP-06.

🔴 **Respuesta:**

> Si, hay que hacer auto-recovery
> 1. No existe
> 2. Si
> 3. No

---

## Q-APP-20 — `_currentHand` con `Result=Unknown` al cerrar app durante mano activa: ¿persistir o descartar?

**Contexto:**
- 🟢 **EC-08:** Usuario hace `Alt+F4` durante el flop. `_currentHand != null`.
- `FrmMain_FormClosing:304` llama `SaveSessionAsync()` que persiste la mano parcial con `Result = Unknown`.
- 💡 **Comportamiento actual es correcto** (cleanup explícito), pero la decisión de qué hacer con manos `Unknown` en queries históricas es abierta.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Persistir como `Unknown` y excluir de stats `BBPer100`** (estado actual) | Cero pérdida de info; stats limpias. | Manos `Unknown` aparecen en Historial sin profit calculable. |
| **B. Persistir como `Unknown` y incluir con `Profit = 0`** | Stats reflejan número total de manos. | Diluye `BBPer100` con manos no-conclusas. |
| **C. Descartar (no persistir)** | Stats puras. | Pierde información parcial (street decisions ya tomadas son útiles para review). |
| **D. Persistir con flag separado** (`IsAbandoned: bool`) | Granular — UI puede filtrar. | Schema migration. |

**Preguntas concretas:**
1. ¿Hay valor en revisar manos parcialmente jugadas (decisiones tomadas hasta el cierre)?
2. ¿`Result=Unknown` se distingue visualmente en pestaña Historial?
3. ¿Las stats `BBPer100` excluyen `Unknown` hoy? (verificar `GameSession.BBPer100` formula)

**Bloqueo:** N/A (decisión de comportamiento). **Casos extremos:** EC-08. **Coordinada con:** Q-HR-01 raíz.

🔴 **Respuesta:**

> Descartar
> 1. No
> 2. No
> 3. Si, deben excluirlas

---

## Q-APP-21 — `TextBoxLoggerProvider.BufferCapacity` saturado pre-`SetTextBoxTarget`: ¿upgrade default?

**Contexto:**
- 🟡 **EC-16:** `Host.Build()` y la fase pre-`FrmMain` pueden emitir más de `BufferCapacity=1000` logs si `LogLevel.Trace` está activado.
- Saturación → drop FIFO; logs perdidos.
- Sink de consola permanece activo en paralelo (mitigación parcial).

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Aumentar default a `BufferCapacity=5000`** | Cubre escenarios con `Trace` sin pérdida. | Memoria pre-startup ~500 KB (despreciable). |
| **B. Documentar que `Trace` en arranque puede saturar; default queda 1000** | Sin cambios. | Diagnóstico incompleto si los logs perdidos son los relevantes. |
| **C. Sink de consola siempre activo + log unificado a archivo** (Serilog/NLog) | Diagnóstico completo independiente del TextBox. | Nueva dependencia. |
| **D. Mantener** | Cero esfuerzo. | Saturación posible en debug. |

**Preguntas concretas:**
1. ¿`MinimumLevel` default es `Information` o `Trace`?
2. ¿Hay escenario donde `BufferCapacity=1000` se ha saturado en producción?
3. ¿Q-DOM-XX (logging strategy) preferida: TextBox-only, archivo-only, ambos?

**Bloqueo:** T-XX (`TextBoxLoggerProvider`). **Casos extremos:** EC-16.

🔴 **Respuesta:**

> Opcion A
> 1. Information
> 2. No
> 3. Ambos

---

## Q-APP-22 — Heads-up con `_previousSBPlayerName == _previousBBPlayerName`: ¿defensa adicional?

**Contexto:**
- 🟡 **EC-18:** En heads-up con OCR errático, `previousSB == previousBB` (mismo nombre detectado dos veces).
- Indicadores 6 y 7 de `DetectNewHand` se rompen (compara `currentSB != previousSB` y `currentBB != previousBB` — ambos pueden activarse incorrectamente).
- Falsos positivos / negativos en detección de nueva mano.

**Implicaciones por cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. Validación previa**: `if (previousSB == previousBB && previousSB != null) skip indicators 6,7` | Robustez con OCR errático. | Sutil — solo afecta heads-up, casos raros. |
| **B. Reforzar OCR de nombres en heads-up** (3-read consensus por nombre) | Causa raíz atacada. | Cost OCR adicional por nombre. |
| **C. Aumentar peso de los otros 5 indicadores** (votación con quorum) | Tolera 1-2 indicadores rotos. | Threshold de quorum a calibrar. |
| **D. Mantener** | Cero esfuerzo. | Errores en heads-up con OCR malo. |

**Preguntas concretas:**
1. ¿Heads-up es escenario operativo común o raro? (depende de Q-DOM-03 stakes objetivo)
2. ¿Hay logs reales con `previousSB == previousBB`? (frecuencia)
3. ¿Q-APP-14 (OCR low confidence) cubre parcialmente este caso?

**Bloqueo:** T-XX (`DetectNewHand`). **Casos extremos:** EC-18. **Decisión coordinada con:** Q-APP-14.

🔴 **Respuesta:**

> Opción A
> 1. raro
> 2. No 
> 3. Aparentemente

---

## Resumen

| ID | Bloqueo (tasks.md) | Severidad | Estado |
|----|--------------------|-----------|--------|
| Q-APP-01 — Credenciales en `appsettings.json` | T-09, Pré-req config | 🔴 | abierta (compromiso seguridad) |
| Q-APP-02 — `EncrypterHelper` IV fija | T-09 | 🔴 | abierta (vulnerabilidad cripto) |
| Q-APP-03 — `PokerDecisionFacade` muerto | T-89/T-90 | 🟡 | abierta (deuda arquitectural) |
| Q-APP-04 — `GameLoopCoordinator` apagado | T-91/T-92 | 🟡 | abierta (cutover Fase 4 pendiente) |
| Q-APP-05 — `FormImage` path hardcoded | T-XX | 🔴 | abierta (bloquea distribución) |
| Q-APP-06 — `eng.traineddata` duplicada | T-03 | 🟡 | abierta (deuda build) |
| Q-APP-07 — `FrmMain` god-class 4502 LOC | T-XX | 🟡 | abierta (refactor Fase 7) |
| Q-APP-08 — Filtro `"NL H"` hardcoded | T-XX | 🟡 | abierta (config) |
| Q-APP-09 — Hot reload `StrategyProfile` | T-XX | 🟡 | abierta (UX vs complejidad) |
| Q-APP-10 — Multi-monitor DPI distinto | T-XX | 🟡 | abierta (entorno cambiante) |
| Q-APP-11 — Validación `BoardCards` duplicados | T-XX | 🟡 | abierta (App vs DM) |
| Q-APP-12 — Marten outage durante persist | T-XX | 🟡 | abierta (pérdida silenciosa) |
| Q-APP-13 — `BackgroundWorker` zombie loop | T-XX | 🟡 | abierta (game loop hardening) |
| Q-APP-14 — OCR low confidence cascade | T-XX | 🟡 | abierta (decisiones erradas) |
| Q-APP-15 — Tesseract crash region OOB | T-XX | 🔴 | abierta (crash proceso) |
| Q-APP-16 — `RegionLookupCache` reload no thread-safe | T-XX | 🟡 | abierta (race condition) |
| Q-APP-17 — Equity cache LRU vs FIFO | T-XX | 🟡 | abierta (perf sesión larga) |
| Q-APP-18 — Cliente cerrado durante captura | T-XX | 🟡 | abierta (UX) |
| Q-APP-19 — Traineddata corrupta auto-recovery | T-XX | 🔴 | abierta (UX arranque) |
| Q-APP-20 — Manos `Unknown` al cerrar app | N/A | 🟢 | abierta (decisión comportamental) |
| Q-APP-21 — `TextBoxLoggerProvider` saturación | T-XX | 🟡 | abierta (logging) |
| Q-APP-22 — Heads-up con `previousSB == previousBB` | T-XX | 🟡 | abierta (OCR edge) |

**Distribución de severidad:**
- 🔴 Críticas (5): Q-APP-01 (credenciales), Q-APP-02 (cripto), Q-APP-05 (path hardcoded), Q-APP-15 (crash proceso), Q-APP-19 (traineddata UX).
- 🟡 Importantes (16): refactors, robustez, UX.
- 🟢 Confirmaciones (1): Q-APP-20 (decisión de comportamiento).

**Decisiones coordinadas:**
- Q-APP-04 ↔ Q-APP-13 (game loop coordinator vs zombie loop).
- Q-APP-06 ↔ Q-APP-19 (traineddata embebida + auto-recovery).
- Q-APP-11 ↔ Q-DM-09 (validación input — App o DecisionMaker).
- Q-APP-14 ↔ Q-APP-22 (OCR low confidence + heads-up edge).
- Q-APP-20 ↔ Q-HR-01 raíz (manos `Unknown`).

**Bloqueos cruzados con preguntas raíz:**
- Q-DOM-02 (salas soportadas) ↔ Q-APP-08 (filtro `NL H`).
- Q-DOM-10 (connection string) ↔ Q-APP-01 (credenciales).
- Q-FSM-02 (persistencia OpponentProfile) ↔ Q-DM-08.

---

## Cómo continuar

Cuando hayas respondido (todas o un subset):

- **Si respondes todo:** dime "preguntas APP respondidas" → el Reviewer integra y cierra el bloque.
- **Si respondes solo algunas:** dime cuáles dejas abiertas para una segunda iteración.
- **Las 5 críticas (🔴)** deberían priorizarse — bloquean distribución, seguridad o robustez de proceso.
