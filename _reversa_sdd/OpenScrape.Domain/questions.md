# OpenScrape.Domain — Preguntas Abiertas

> Lacunas detectadas en la capa de dominio que requieren validación humana antes de implementar/migrar.
> Modo de respuesta: `file` (responder editando este archivo, secciones marcadas con `📝 Respuesta:`).
> Cada pregunta tiene un ID estable (`Q-DOM-NN`) para referencia desde tasks/edge-cases.

---

## Q-DOM-01 — ¿`OpponentProfile` debería persistirse entre sesiones?

**Contexto:**
- El código legado mantiene `OpponentProfile` solo en memoria, sin colección Marten dedicada.
- `OpponentTracker` acumula 30+ contadores durante la sesión (VPIP, PFR, AF posicional, c-bet, donk, check-raise, showdown, barrel).
- Al cerrar la app, todo el tracking se pierde.

**Implicaciones de cada opción:**

| Opción | Beneficio | Costo |
|--------|-----------|-------|
| **A. No persistir (status quo)** | Privacy: sin retención de datos por jugador. Simplicidad. | Cada sesión empieza con villains "Unknown". Hands necesarios para reliability se reacumulan. |
| **B. Persistir por `PlayerId`** | Tracking acumulativo entre sesiones del mismo villain. Reliability más rápida. | Requiere identificador estable de jugador (alias OCR ≠ ID único, vulnerable a alias-changing). Implicaciones legales/ToS de algunos sites. |
| **C. Persistir solo agregados (sin PlayerId)** | Stats globales del pool. | Pierde la utilidad principal (clasificación de villain específico). |

**Preguntas concretas:**
1. ¿Hay un `PlayerId` estable detectable por OCR (alias + foto + asiento), o el alias cambia frecuentemente?
2. ¿Las ToS del sitio de poker permiten retener stats por jugador?
3. ¿El usuario quiere ver stats acumulativas o prefiere "fresh start" cada sesión?

**Bloqueo:** T-27 en `tasks.md` (definir si añadir `IDocumentStore.For<OpponentProfile>()`).

🔴 **Respuesta:**
Opcion B

1. No existe un playerId, pero hay que crearlo
2. Si
3. Acumulativas

---

## Q-DOM-02 — ¿`AutoRebuy` distingue de top-up manual?

**Contexto:**
- `HandRecord.AutoRebuy` (decimal) almacena el monto detectado de auto-rebuy durante la mano.
- `NetProfit = (HSE - HSS) - AutoRebuy + BlindPosted` lo resta para no contar dinero propio como ganancia.
- ADR-0013 documenta el threshold de detección (50 BB) basado en heurística de stack jump.

**Pregunta:** Si el sitio permite "Top-up" o "Add chips" manual durante la mano (no automático), ¿debería distinguirse de auto-rebuy?

**Casos a definir:**

| Acción | Detección hoy | ¿Cómo debe contar? |
|--------|---------------|---------------------|
| Auto-rebuy a 100 BB tras quedar short | ✅ Stack jump > 50 BB | Resta de profit |
| Top-up manual de 30 BB durante mano | ❌ No detectado (jump < 50 BB) | ¿Resta o no? |
| Recompra entre manos (sit-out → re-entry) | ❌ No detectado | ¿Resta o sesión nueva? |
| Cashout parcial | ❌ No detectado (jump negativo) | ¿Suma a profit o ignora? |

**Implicaciones:**
- Si todo top-up cuenta como rebuy: `BB/100` puede ser pesimista en sites con micro top-ups frecuentes.
- Si solo auto-rebuy cuenta: usuarios que hacen top-up manual no ven impacto en stats.

🔴 **Respuesta:** No es necesario diferenciar entre auto-rebuy y top-up manual

---

## Q-DOM-03 — All-in posted blind: ¿`BlindPosted` cubre el caso?

**Contexto:**
- `HandRecord.BlindPosted` registra la BB obligatoria pagada en la mano.
- `NetProfit = (HSE - HSS) - AutoRebuy + BlindPosted` la suma para neutralizar el costo administrativo.

**Caso límite:**
- Hero llega a la mano con stack=0.5 BB.
- Es BB y se paga la ciega completa: hero queda all-in con 0.5 BB.
- HSE = 0 (lost), HSS = 0.5, BlindPosted = 0.5.
- `NetProfit = (0 - 0.5) + 0.5 = 0` → **no cuenta como pérdida**.

**Pero:** hero realmente perdió 0.5 BB; era dinero propio que entró voluntariamente al pot vía BB obligatoria + posible decisión de no foldear.

**Pregunta:** ¿`BlindPosted` debe cubrir solo la parte fold-en-BB-sin-acción, o también el escenario all-in en BB?

**Opciones:**

| Opción | Fórmula | Comportamiento all-in BB |
|--------|---------|--------------------------|
| **A. Status quo** | `+ BlindPosted` siempre | All-in con BB ⇒ NetProfit=0 (subcuenta pérdida) |
| **B. Solo si hero foldó** | `+ BlindPosted * (1 if fold else 0)` | All-in BB ⇒ NetProfit=-0.5 (correcto) |
| **C. Solo si fue check-fold preflop** | requiere campo nuevo `WasVoluntaryAction` | Más preciso pero más complejo |

🔴 **Respuesta:** No puede ocurrir este caso debido al auto-rebuy, nunca el Hero tendrá 0.5 BB

---

## Q-DOM-04 — `StreetDecision`: nullability de `BetSize`, `Action`, campos opcionales

**Contexto:**
- `StreetDecision` es record con campos `BetSize`, `Action`, `Reason?`, `BoardTexture?`, `TotalOuts?`, `SPR?`.
- En decisión "Check" (no apuesta), no hay bet size. ¿Es `0` o `null`?

**Inspección (parcial) del legado:**
- El record acepta `decimal BetSize` (no nullable) en muchas firmas — implica `0` para "no bet".
- Pero `Reason`, `BoardTexture`, etc. sí son nullable.

**Pregunta:** ¿Es correcto interpretar `BetSize=0m` como "check"? ¿O hay manos donde `BetSize=0m` significa "bet 0" (raro pero posible en algunos formatos)?

**Riesgo:**
- Si `BetSize=0` se confunde con "bet de 0", historiales pueden tener manos legítimas con apuesta cero (foldear hace que el cálculo de pot quede en cero).
- Auditor que lee CSV exportado vería `bet_size=0,action=Check` como "check" pero `bet_size=0,action=Fold` como "fold" — semántica del enum.

**Decisión propuesta:** documentar contrato:
- `BetSize == 0m` ⇒ no hay apuesta (Check, Fold, Call de free showdown)
- `BetSize > 0m` ⇒ apuesta voluntaria (Bet, Raise)

¿Está bien o debería haber más distinción?

🔴 **Respuesta:** Si, un Betsize=0m se puede interpretar como "check", no existe una apuesta BetSize = 0m

---

## Q-DOM-05 — Tipos legacy 🟡: ¿descartar, congelar o conservar?

**Contexto:** 5 tipos identificados como candidatos legacy por el Archaeologist:

| Tipo | Archivo | Problema |
|------|---------|----------|
| `Styles` | `Enums/Styles.cs` | Solo 3 valores (`Default/Agresive/Pasive`), typo "Agresive" |
| `HeroHand` | `Enums/Positions.cs:51` | En español, paralelo a `HandRank` (en inglés) |
| `GameSituation` | `Enums/Positions.cs:69` | Paralelo a `HandSituation` con `[Description]` extra |
| `ActionsResponse` | `Enums/ActionsResponse.cs` | Clase POCO mal categorizada en `Enums/` |
| `ListRegions` | `Enums/ListRegions.cs` | Static class con 41 regiones hardcoded; reemplazado por `RegionTableMap` + `RegionLookupCache` |

**Pregunta:** ¿Cuál es el tratamiento por tipo?

**Opciones:**

| Tipo | A. Eliminar | B. `[Obsolete]` | C. Mover a `Legacy/` namespace | D. Conservar como está |
|------|:-----------:|:---------------:|:------------------------------:|:----------------------:|
| `Styles` | ✓ recomendado si nadie lo usa | | | |
| `HeroHand` | | ✓ recomendado (deprecar y migrar) | | |
| `GameSituation` | | ✓ recomendado | | |
| `ActionsResponse` | | | ✓ recomendado (mover fuera de `Enums/`) | |
| `ListRegions` | ✓ recomendado | | | |

**Verificación necesaria antes de eliminar:** grep en `Features`, `DecisionMaker`, `App` por referencias.

**Bloqueo:** T-05 en `tasks.md`, `discard_log.md` del Curator.

🔴 **Respuesta:** Puedes proceder con las opciones recomendadas

---

## Q-DOM-06 — `VillainRange ↔ OpponentProfile`: ¿romper acoplamiento de capa?

**Contexto:**
- `VillainRange` vive en `ValueObjects/` pero depende de `OpponentProfile` (en `Entities/`).
- En Clean Architecture, value objects no deberían depender de entidades. El acoplamiento es inverso.

**Opciones:**

| Opción | Descripción | Impacto |
|--------|-------------|---------|
| **A. Status quo** | `VillainRange` queda en `ValueObjects/`, depende de `Entities/OpponentProfile` | Conserva código actual; documentar como tech debt |
| **B. Mover `VillainRange` a `Entities/`** | Reconocer que es algo más que VO puro | Cambia namespace, requiere actualizar usings en `Features` y `DecisionMaker` |
| **C. Introducir puerto `IOpponentProfile`** | `VillainRange` depende de la abstracción | Más Clean, más boilerplate (interfaz + implementación) |
| **D. Crear capa `Domain.Strategy/`** | `VillainRange` + `StreetThresholds` + `OpponentProfile` viven en módulo separado | Refactor mayor, justificable solo si se planea expandir el dominio strategy |

**Observación:** `HandStrenght.cs` también declara `using OpenScrape.Domain.Entities;` pero **no** lo usa (import muerto). Eliminar al menos eso.

**Bloqueo:** T-15 en `tasks.md`.

🟡 **Respuesta:** Vamos con la opción D

---

## Q-DOM-07 — `StrategyProfile` mutabilidad post-validate: ¿congelar?

**Contexto:**
- `StrategyProfile` tiene ~60 propiedades públicas con setters mutables.
- Tras `Validate()` en startup, no hay protección contra mutaciones posteriores.
- DD-12 documenta el riesgo.

**Pregunta:** ¿Implementar el patrón "snapshot" — clonar a un tipo `StrategyProfileSnapshot` (record sealed) tras Validate y entregarlo a consumidores?

**Trade-offs:**

| Aspecto | Status quo (mutable) | Con snapshot inmutable |
|---------|---------------------|------------------------|
| Compatibilidad Marten | ✅ Directa | ⚠️ Snapshot no se persiste (solo el mutable) |
| `IOptionsMonitor` reload | ✅ Directo | ⚠️ Reload requiere re-snapshot |
| Seguridad runtime | ❌ Sin protección | ✅ Imposible mutar el snapshot |
| Refactor required | 0 | ~5-8 archivos (snapshot + binding + DI) |

🟡 **Respuesta:** Si, me parece bien la recomendación

---

## Q-DOM-08 — `StrategyProfileValidationException`: ¿enriquecer con lista estructurada?

**Contexto:**
- La excepción actual probablemente solo lleva `Message` (string).
- Si en el futuro se quiere mostrar las reglas violadas en UI (ej. dialog "Strategy inválido"), formatear desde el string es frágil.

**Pregunta:** ¿Añadir `IReadOnlyList<string> ViolatedRules` (o `IReadOnlyList<ValidationFailure>` con código + mensaje + path) a la excepción?

**Costo:** ~10 líneas en `StrategyProfileValidationException`. Cero impacto en consumidores actuales (que usan `ex.Message`).

**Beneficio:**
- UI puede iterar reglas y mostrar como lista.
- Telemetría puede agrupar errores por código.
- Tests más precisos: assert que regla específica se viola en lugar de string-match.

🟡 **Respuesta:** Si

---

## Q-DOM-09 — `HandStrenght.cs`: ¿corregir typo en filename?

**Contexto:**
- `ValueObjects/HandStrenght.cs` (con typo "Strenght") contiene `class HandStrength` (sin typo).
- Renombrar el archivo a `HandStrength.cs` requiere actualizar referencias en csproj (si las hay) y posiblemente git history.

**Opciones:**
1. **Conservar typo** — minimiza churn, pero filename sigue siendo inconsistente.
2. **Renombrar archivo** — limpia, pero introduce un commit de pure-rename.
3. **Renombrar y mantener alias `HandStrenght.cs` con `using HandStrenght = HandStrength`** — overkill.

**Bloqueo:** T-13.

🟡 **Respuesta:** Si

---

## Q-DOM-10 — `OverlayConfig`: ¿persistir o solo runtime?

**Contexto:**
- `Entities/OverlayConfig.cs` modela configuración de overlay UI (posición, opacidad, tamaño).
- No marcado claramente como Marten document en `legacy-mapping.md`.
- ¿Es persistente (recordar la posición del overlay entre sesiones) o solo runtime?

**Implicaciones:**
- Si persistente: necesita colección Marten + UI para editarlo.
- Si solo runtime: settings vienen de `appsettings.json` y no hay UI de edición.

**Bloqueo:** T-31 (firma exacta del tipo a verificar).

🟡 **Respuesta:** runtime

---

## Q-DOM-11 — `Card.Image` o `Card.ImageData`: ¿binario embebido o referencia?

**Contexto:**
- El catálogo de 52 `Card` se carga al startup (singleton `CardCacheService`).
- Cada Card debe tener una imagen para comparación OCR.
- ¿Esa imagen vive como `byte[]` en el documento Marten o como path/URI?

**Opciones:**

| Opción | Tamaño documento | Carga inicial | Portabilidad DB |
|--------|------------------|---------------|-----------------|
| **A. `byte[] ImageData`** | 5-20 KB por carta × 52 = 260 KB-1 MB en DB | 1 query Marten carga todo | DB autosuficiente |
| **B. `string ImagePath`** | <1 KB por carta | Lectura adicional desde filesystem | Requiere acceso a file system congruente |
| **C. Hash + path** | <1 KB | Validación de hash + lectura | Mejor para detectar drift |

**Bloqueo:** T-17 (firma de `Card`).

🟡 **Respuesta:** Opción C

---

## Resumen

| ID | Confianza | Tipo | Bloquea |
|------|:---------:|------|---------|
| Q-DOM-01 | 🔴 | Funcional (persistencia OpponentProfile) | T-27 |
| Q-DOM-02 | 🔴 | Funcional (top-up manual) | T-21 |
| Q-DOM-03 | 🔴 | Funcional (all-in BB) | T-21 |
| Q-DOM-04 | 🔴 | Contrato (`BetSize == 0`) | T-09 |
| Q-DOM-05 | 🔴 | Limpieza (5 tipos legacy) | T-05, Curator |
| Q-DOM-06 | 🟡 | Arquitectura (acoplamiento VO ↔ Entity) | T-15 |
| Q-DOM-07 | 🟡 | Diseño (mutabilidad StrategyProfile) | — |
| Q-DOM-08 | 🟡 | Diseño (excepción estructurada) | — |
| Q-DOM-09 | 🟡 | Cosmética (typo filename) | T-13 |
| Q-DOM-10 | 🟡 | Funcional (persistencia OverlayConfig) | T-31 |
| Q-DOM-11 | 🟡 | Diseño (Card image storage) | T-17 |

**4 preguntas críticas** (🔴) bloquean migración: persistencia OpponentProfile, top-up manual, all-in BB, semántica `BetSize=0`.

**5 preguntas estructurales** (🟡) son trade-offs de diseño que se pueden resolver post-migración.

Una vez respondidas, marcar la sección con `📝 Respuesta:` antes de cada `🔴`/`🟡`. El Reviewer leerá este archivo en la fase de Revisión.
