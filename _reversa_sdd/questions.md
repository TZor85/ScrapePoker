# Preguntas para validar — ScrapePoker

> Lacunas 🔴 generadas por los agentes del Reversa. Tu turno: escribe la respuesta debajo de cada pregunta.
>
> Cuando termines, avísame en chat ("preguntas respondidas") y el Reviewer integrará tus respuestas en las specs.

---

## Bloque DOM — Dominio de producto (de `domain.md` §7)

### Q-DOM-01 — Modelo comercial / audiencia

¿Cuál es el público objetivo y modelo comercial de ScrapePoker? ¿Uso personal del propietario, distribución a un círculo cerrado, o producto comercial abierto?

**Respuesta:**

> No tiene un uso comercial, es de uso personal y con fines educativos

---

### Q-DOM-02 — Salas de poker soportadas

¿Qué salas de poker están **oficialmente** soportadas / calibradas? ¿Hay alguna lista de mesas testadas? Ayuda a no dar falsa promesa de portabilidad.

**Respuesta:**

> Están soportadas todas, ya que es el usuario quien realiza el "tablemap" de cada sala

---

### Q-DOM-03 — Stakes objetivo

Los parámetros del motor (especialmente las defaults conservadoras de oponentes y `BigBlind=0.50m`, `BuyInMax=2.0m`) sugieren calibración para micro-stakes. ¿El motor está calibrado para NL2-NL10 (micro), NL25-NL100 (low), o stakes mayores?

**Respuesta:**

> Actualmente para micro-stakes (NL2-NL10)

---

### Q-DOM-04 — Trigger de Risk-of-Ruin

`BankrollTrackerService` calcula `RiskOfRuin` con varianza de muestra y `RiskOfRuinThreshold=0.05`. ¿Quién/qué decide cuándo subir o bajar de stake? No vi trigger automático en código.

**Respuesta:**

> El usuario

---

### Q-DOM-05 — Edición de manos en Historial

¿La pestaña `FrmHistorial` permite editar manos (corregir `ActionTaken` a posteriori cuando el bot se equivocó al detectar la acción)? El UI existe pero no validé los handlers.

**Respuesta:**

> No, es solo un historial, no se debe permitir editar manos

---

### Q-DOM-06 — Legacy AutoIt / automatización pasada

`Helpers/AutoIt` y commits prehistóricos (`3db442c Capture auto`, `7562772 Add autoit to show form action`) sugieren que en algún momento hubo automatización del clic. ¿Se eliminó deliberadamente, o queda dormido por si vuelve?

**Respuesta:**

> Está dormido a la espera de termina de implementar

---

### Q-DOM-07 — Coaching / divergencia bot vs humano

`StreetDecision` registra `RecommendedAction` (motor) y `ActionTaken` (humano). ¿Hay reporte/UI que muestre cuándo el humano se desvía del motor? ¿Es para coaching o para detectar bugs del motor?

**Respuesta:**

> No hay motor y al tratarse de coaching, debería existir

---

### Q-DOM-08 — Prioridad del sistema de licencias

La spec `openspec/changes/login-sistema-licencias/` está abierta desde hace meses. ¿Está priorizada para próxima versión o queda diferida?

**Respuesta:**

> Queda diferida

---

### Q-DOM-09 — Rol de `Extractor/ExtractorTablas/`

El proyecto auxiliar (.NET 8.0, Newtonsoft.Json) en `Extractor/ExtractorTablas/`. ¿Qué papel juega? ¿Genera los `*.json` de tablas preflop a partir de capturas / herramientas externas (PokerSnowie, etc.)?

**Respuesta:**

> Actualmente no tiene función

---

### Q-DOM-10 — Connection string en `appsettings.json`

Anomalía detectada por el Scout: `appsettings.json` (committed) contiene una connection string PostgreSQL real (Neon) y una `Encrypter.Key` real. CLAUDE.md declara que deberían ser `CHANGE_ME`. ¿Es olvido de rotación o decisión consciente (todos los binarios distribuidos comparten BD)?

**Respuesta:**

> Decisión consciente

---

## Bloque FSM — Máquinas de estado (de `state-machines.md`)

### Q-FSM-01 — Watchdog de manos atascadas

¿El sistema resetea automáticamente si una mano se queda en `*Action` durante demasiado tiempo (timeout)? No vi watchdog en `GameLoopStateMachine`.

**Respuesta:**

> No se resetea, debería implementarse

---

### Q-FSM-02 — Persistencia de OpponentProfile

¿Los `OpponentProfile` se persisten en BD para mantener historial cross-sesión, o se resetean al cerrar la app? El campo `PlayerId = nombre` sugiere que se podría persistir como Marten document, pero no encontré config.

**Respuesta:**

> Actualmente no tiene uso, pero se deberían persistir

---

### Q-FSM-LIC-01 — Validación offline de licencia

Spec `login-sistema-licencias` dice "validación online en cada arranque". ¿Qué hacer si la BD no es accesible (sin internet) y la licencia no es Admin? ¿Modo offline temporal con `LastValidation`?

**Respuesta:**

> Esta funcionalidad permanece aparcada, pero no debería poder usarse directamente sin conexión

---

### Q-PS-01 — Estado AllIn por jugador

¿Hay estado intermedio "AllIn" para un jugador que comprometió todo su stack pero sigue en la mano? Veo `IsAnyoneAllIn` global en `PostflopGameContext`, pero ¿se rastrea per-jugador?

**Respuesta:**

> Si, un jugador puede ir AllIn pero no comprometer todo su stack, por lo que podría seguir jugando

---

### Q-HR-01 — Manos persistidas como Unknown

¿Hay manos que se persisten con `Result=Unknown` permanentemente (OCR del resultado falló)? Si sí, ¿se reportan al usuario para corrección manual desde `FrmHistorial`?

**Respuesta:**

> No

---

## Bloque PERM — Permisos y seguridad (de `permissions.md`)

### Q-PERM-AV-01 — Falsos positivos antivirus

El binario podría dispararse como falso positivo de AV (BitBlt + window enumeration + posible AutoIt legacy). ¿Hay alguna mitigación (firma de código, EV cert)? ¿O simplemente se documenta el riesgo al usuario?

**Respuesta:**

> Actualmente se documenta el riesgo al usuario

---

### Q-PERM-TOS-01 — TOS de las salas

Las salas de poker prohíben "bots". El sistema solo recomienda (no actúa), pero es zona gris. ¿Hay disclaimer / EULA al usuario?

**Respuesta:**

> No, habría que implementarlo

---

### Q-PERM-CRED-01 — Rotación de credenciales

La connection string Neon vive en `appsettings.json` actual. **Riesgo activo:** quien tenga el binario tiene acceso a la BD. ¿Plan para rotar y mover a `Development.json`?

**Respuesta:**

> Si, hay que hacerlo

---

### Q-PERM-CRED-02 — Rotación Encrypter.Key

`Encrypter.Key` también está en `appsettings.json` real. Si su único uso es ofuscar screenshots, ¿se puede rotar / mover sin romper datos antiguos?

**Respuesta:**

> Habría que rotarlo sin que nada se rompa

---

### Q-PERM-CRED-03 — Backend intermedio

¿Has considerado un backend intermedio (REST API) en vez de connection string compartida directa? Necesario si el producto va multi-tenant.

**Respuesta:**

> Actualmente no, pero es una posible mejora

---

### Q-PERM-RBAC-01 — Gestión de licencias

¿La gestión de licencias (crear, alargar, desactivar) se hará por SQL directo en Neon, o habrá panel web?

**Respuesta:**

> Ahora mismo, directo por SQL

---

### Q-PERM-RBAC-02 — DPAPI para `Properties/Settings`

"Recordar licencia" guarda key en `Properties/Settings` texto plano. ¿Debería encriptarse con DPAPI (`ProtectedData.Protect`)?

**Respuesta:**

> Si, debería encriptarse

---

### Q-PERM-DATA-01 — Aislamiento entre usuarios

Hoy las sesiones de un usuario son visibles para otro con la misma conn string. ¿El plan multi-tenant futuro filtrará por `OwnerLicenseKey` (cambio breaking)?

**Respuesta:**

> Si, cada usuario solo debería ver sus sesiones

---

### Q-PERM-DATA-02 — GDPR / nombres de oponentes

Las manos pueden contener alias OCR de oponentes (potencialmente reales). ¿Se anonimiza algo si se exporta? ¿Implicaciones GDPR?

**Respuesta:**

> Si, deberían quedarse como "player1", "player2"... etc

---

## Bloque ADR — Detalles de decisiones

### Q-ADR-AR-01 — Cómo se rellena `HandRecord.AutoRebuy`

ADR-0013: el threshold 50 BB detecta el rebuy pero no su **monto exacto**. ¿Cómo se rellena `HandRecord.AutoRebuy`? ¿Diff `currentStack - HeroStackPreRebuy` cuando se detecta? Validar que no sobrestima.

**Respuesta:**

> El autorebuy es 100 BB - currentStack, eso nos dice el monto que se ha rellenado

---

## Cómo continuar

Cuando hayas respondido (todas o un subset):

- **Si respondes todo:** dime "preguntas respondidas" → el Reviewer integra y cierra el bloque.
- **Si respondes solo algunas:** dime qué bloques quieres dejar abiertos por ahora; el Reviewer integra lo respondido y deja las restantes para una segunda iteración.

No hace falta responder en orden ni en una sola sesión.

---

## Bloque REVIEW — Pendientes detectados por el Revisor (2026-05-12)

> Las preguntas anteriores ya tienen respuesta humana. Este bloque contiene decisiones que siguen abiertas tras cruzar units, matrices y codigo.

### Q-REV-01 — Politica real de `IsDevelopment=true`

**Contexto:** `src/OpenScrape.App/Program.cs:52` llama `services.AddDataBase(context.Configuration, true)` y `src/OpenScrape.Infrastructure/Services.cs:36-38` activa `AutoCreate.All`.
**Spec afectada:** `_reversa_sdd/OpenScrape.Infrastructure/{requirements,design,tasks}.md`
**Pregunta:** Debe mantenerse `AutoCreate.All` siempre, o debe cambiar a `context.HostingEnvironment.IsDevelopment()`?
**Impacto:** Define si la reconstruccion preserva comportamiento 1:1 o corrige riesgo productivo.

✅ Respondida

**Respuesta:** cambiar

---

### Q-REV-02 — Migracion de credenciales y cifrado

**Contexto:** `appsettings.json` contiene credenciales reales; `EncrypterHelper` usa IV fija.
**Spec afectada:** `_reversa_sdd/OpenScrape.App/{requirements,design,tasks}.md`
**Pregunta:** Para reconstruccion, quieres mantener compatibilidad exacta con config/ciphertext existente, o romper compatibilidad y migrar a secrets + IV aleatoria/versionada?
**Impacto:** Cambia formato de config, bootstrap, migracion y tests de compatibilidad.

✅ Respondida

**Respuesta:** Mantener

---

### Q-REV-03 — Persistencia de perfiles de oponente

**Contexto:** `OpponentTracker` usa `ConcurrentDictionary` in-memory; usuario confirmo que deberia persistirse.
**Spec afectada:** `_reversa_sdd/OpenScrape.Domain/*`, `_reversa_sdd/OpenScrape.DecisionMaker/*`, `_reversa_sdd/OpenScrape.Infrastructure/*`
**Pregunta:** La persistencia de `OpponentProfile` debe ser por alias OCR, por sala+alias, por licencia+alias, o por otro identificador?
**Impacto:** Define modelo de datos, privacidad, merge de perfiles y comportamiento cross-sesion.

✅ Respondida

**Respuesta:** hay que asignar un id (guid) a cada alias y usar ese id

---

### Q-REV-04 — Canon de tablas preflop y `BetSize`

**Contexto:** `GetActionScenario.cs:31` comenta el filtro `BetSize`; JSON de tablas contiene `BetSize` en varios spots.
**Spec afectada:** `_reversa_sdd/OpenScrape.Features/{requirements,design,tasks}.md`
**Pregunta:** `BetSize` debe volver al lookup, eliminarse del contrato, o quedar reemplazado por `IsGreater`?
**Impacto:** Afecta exactitud preflop y compatibilidad con JSON existentes.

✅ Respondida

**Respuesta:** Volver al lookup

---

### Q-REV-05 — Dead code de use cases

**Contexto:** `GetAllTables`, `GetAllRegionTableMap` y `GetFlopCards` existen/estan registrados pero no implementan flujo util.
**Spec afectada:** `_reversa_sdd/OpenScrape.Features/{design,tasks}.md`
**Pregunta:** En reconstruccion se eliminan estos use cases, se completan, o se mantienen como placeholders por compatibilidad?
**Impacto:** Define alcance real de Features y limpieza de DI.

✅ Respondida

**Respuesta:** Se eliminan

---

### Q-REV-06 — Fuente canonica de BigBlind

**Contexto:** dominio tiene `GameSession.BigBlind`; `ExploitabilityCalculator` fija `BigBlind = 1.0`.
**Spec afectada:** `_reversa_sdd/OpenScrape.DecisionMaker/{requirements,design,tasks}.md`
**Pregunta:** `ExploitabilityCalculator` debe recibir BigBlind desde sesion actual, `StrategyProfile`, config global, o parametro por decision?
**Impacto:** Corrige calculo de mbb/BB100 y evita metricas sesgadas.

✅ Respondida

**Respuesta:** desde sesion actual

---

### Q-REV-07 — Paths absolutos y recursos

**Contexto:** `FormImage.cs` y use cases de tablemap apuntan a `C:\Code\Poker\ScrapePoker\resources`; `eng.traineddata` esta duplicado.
**Spec afectada:** `_reversa_sdd/OpenScrape.App/{requirements,design,tasks}.md`
**Pregunta:** Quieres una carpeta configurable por usuario, una carpeta relativa al ejecutable, o eliminar esas funciones legacy?
**Impacto:** Define packaging, portabilidad y migracion de tablemaps/resources.

✅ Respondida

**Respuesta:** configurable por el usuario
