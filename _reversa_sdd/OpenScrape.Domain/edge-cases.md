# OpenScrape.Domain — Casos Extremos

> Casos límite del modelo de dominio detectados en el código legado, con comportamiento esperado y consecuencias si se ignoran.

---

## EC-01 — `Hand.Name` vacío o solo espacios

**Disparador:** `new Hand("", null, "fold", 0)` o `new Hand("   ", null, "fold", 0)`.

**Comportamiento esperado** (🟢 Hand.cs:5-7):
- El init throw `ArgumentException` con mensaje `"El nombre de la mano no puede estar vacío."`
- `paramName == "Name"`.
- La instancia **no** se materializa.

**Consecuencias si se ignora:**
- Diccionarios indexados por `Hand.Name` colisionan con la clave string vacía.
- Logs muestran "Mano: " sin contenido, imposibilitando depuración.
- `VillainRange` que carga JSON con entradas corruptas se silencia y resulta en rangos truncados.

**Cobertura test:** `TT-02` (3 inputs: `""`, `"   "`, `null` cast a string).

---

## EC-02 — `Hand.Percentage` fuera del rango `[0, 100]`

**Disparador:** `new Hand("AKs", true, "raise", 150)` o `new Hand("22", false, "fold", -5)`.

**Comportamiento esperado** (🟢 Hand.cs:9-11):
- Lanza `ArgumentOutOfRangeException` con mensaje que incluye el valor recibido (`"Percentage debe estar entre 0 y 100, valor: 150"`).
- `paramName == "Percentage"`.

**Consecuencias si se ignora:**
- Probabilidades sumando >100% en el agregado del rango → equity Monte Carlo retorna basura.
- `BluffFrequencyMultiplier × Percentage` puede generar `BluffFrequency > 1` (NaN downstream).

**Cobertura test:** `TT-02` (4 inputs: `-1`, `101`, `int.MinValue`, `int.MaxValue`).

---

## EC-03 — `ThresholdKey` con `BoardPosition.None` o `BoardPosition.Hand`

**Disparador:** `new ThresholdKey(BoardPosition.None, HandSituation.OpenRaise)` o `new ThresholdKey(BoardPosition.Hand, HandSituation.OpenRaise)`.

**Comportamiento esperado** (🟢 ThresholdKey.cs:17-20):
- Lanza `ArgumentException` con mensaje que incluye el valor literal del enum y el texto `"Esperado: Flop, Turn o River."`.
- `paramName == "street"`.

**Consecuencias si se ignora:**
- Lookup en `StrategyProfile.Thresholds` con clave `"None_OpenRaise"` o `"Hand_OpenRaise"` retorna `null` o lanza `KeyNotFoundException`.
- En el legado anterior (clave string), había **fallback silencioso a default**, lo que producía decisiones con thresholds incorrectos sin warning. Esta validación previene exactamente eso. (ADR-0008)

**Cobertura test:** `TT-04`.

---

## EC-04 — `ThresholdKey.TryParse` con string mal formado

**Disparadores y respuestas** (🟢 ThresholdKey.cs:36-77):

| Input | Retorno | Error |
|-------|---------|-------|
| `null` | `false` | `"La clave está vacía."` |
| `""` | `false` | `"La clave está vacía."` |
| `"   "` | `false` | `"La clave está vacía."` |
| `"Flop"` (sin `_`) | `false` | `"Formato inválido. Esperado '{Street}_{Situation}'."` |
| `"flop_OpenRaise"` (case-sensitive) | `false` | `"'flop' no es un valor válido de BoardPosition."` |
| `"NoExiste_OpenRaise"` | `false` | `"'NoExiste' no es un valor válido de BoardPosition."` |
| `"Flop_NoExiste"` | `false` | `"'NoExiste' no es un valor válido de HandSituation."` |
| `"None_OpenRaise"` | `false` | `"BoardPosition 'None' no es válido para un ThresholdKey postflop. ..."` (se llama internamente al constructor) |
| `"Flop_OpenRaise"` | `true` | `""` |

**Comportamiento esperado:**
- `TryParse` **nunca** lanza excepción aunque el string sea pathological.
- `key` es `null` cuando retorna `false`.
- `error` no es `null`; es string vacío en caso de éxito.

**Consecuencias si se ignora:**
- Cargar `appsettings.json` con clave malformada `"Tunr_OpenRaise"` (typo) provocaría crash en hot path en lugar de error de configuración detectable en startup.

**Cobertura test:** `TT-05` (todos los inputs de la tabla).

---

## EC-05 — `StrategyProfile.Validate()` con tiers de equity invertidos

**Disparador:** profile con `FoldBelow > ThinValueAbove` o `ThinValueAbove > ValueAbove` o `ValueAbove > StrongValueAbove` (orden roto).

**Comportamiento esperado** (🟢 ADR-0008):
- `Validate()` lanza `StrategyProfileValidationException` enumerando cada tier roto.
- Detecta el problema **en startup** vía `IValidateOptions<StrategyProfile>`, no durante la primera mano.
- Mensaje en castellano lista todas las reglas violadas, no solo la primera.

**Consecuencias si se ignora:**
- Decisión postflop hace `equity > FoldBelow → ¿call o thin value?` con tiers solapados → resultado indefinido.
- `StrategyBacktester` produciría "decisiones distintas" sin causa raíz visible.

**Cobertura test:** `TT-06` (18 reglas, una por test, cada una con perfil mutado en una sola dimensión).

---

## EC-06 — `GameSession.BBPer100` sin manos (`TotalHands == 0`)

**Disparador:** sesión recién abierta, antes de la primera mano completada.

**Comportamiento esperado** (🟢 GameSession.cs:39-41):
- `BBPer100 == 0` exactamente, sin `DivideByZeroException` ni `NaN`.
- La condición es `BigBlind > 0m && TotalHands > 0`; falla short-circuit en el `&&`.

**Consecuencias si se ignora:**
- Dashboard mostraría `NaN` o `Infinity` y rompería el binding de WinForms.
- Métricas exportadas (CSV, telemetría) corromperían reportes downstream.

**Cobertura test:** `TT-07` (sesión vacía) + caso con `BigBlind == 0m` (sesión sin BB configurado).

---

## EC-07 — `GameSession.BigBlind == 0m`

**Disparador:** sesión persistida con BB malformado por bug histórico o BB no configurado.

**Comportamiento esperado** (🟢 GameSession.cs:39):
- `BBPer100 == 0` (short-circuit).
- `IsValid == false` (`BigBlind > 0m` falla, GameSession.cs:49).

**Consecuencias si se ignora:**
- División por cero en `TotalProfit / BigBlind`.
- La sesión no es detectada como inválida y aparece en el historial.

**Cobertura test:** caso adicional en `TT-07`.

---

## EC-08 — `HandRecord.NetProfit` con auto-rebuy

**Disparador:** durante la mano, hero queda short-stack y el sitio aplica auto-rebuy a 100 BB. `HeroStackEnd > HeroStackStart + 50 BB` (umbral de detección, ADR-0013).

**Caso concreto:**
- `HeroStackStart = 100` (en BB), pierde 95 BB, fold con stack 5
- Auto-rebuy a 100 → `HeroStackEnd = 100`, `AutoRebuy = 95`
- `BlindPosted = 1` (BB obligatoria pagada y perdida ese turno)
- `NetProfit = (100 - 100) - 95 + 1 = -94` (correcto: la pérdida real fue 94 BB)

**Comportamiento esperado** (🟢 GameSession.cs:92):
- `NetProfit` excluye el dinero de auto-rebuy (no es ganancia).
- Suma `BlindPosted` para no contar la BB obligatoria como pérdida si hero foldó preflop sin acción voluntaria.

**Consecuencias si se ignora:**
- Sin restar `AutoRebuy`: profit aparente +5 BB cuando en realidad perdió 95.
- Sin sumar `BlindPosted`: hero que foldea en BB (sin acción voluntaria) aparece perdiendo 1 BB cuando lo correcto es 0.

**Cobertura test:** `TT-08` + caso de fold-en-BB-sin-acción.

---

## EC-09 — `OpponentPositionProfile.AggressionFactorIP` con `< 5` muestras

**Disparador:** villain con `TimesAggressiveIP=2`, `TimesPassiveIP=1` (total 3 < 5).

**Comportamiento esperado** (🟢 OpponentProfile.cs:26-28):
- Retorna sentinel `-1` (no `(2+1)/(1+1)=1.5`).
- Consumidor (`PostflopDecisionService`) interpreta `-1` como "datos insuficientes" y aplica fallback al AF agregado o estático.

**Consecuencias si se ignora:**
- Con 1 muestra (Aggressive=1, Passive=0), AF "real" = ∞ (División por cero) o `(1+1)/(0+1)=2.0` (LAG sin justificación).
- El sistema clasificaría villains como LAG/TP con confianza estadística inválida tras pocas observaciones.

**Cobertura test:** `TT-10`.

---

## EC-10 — `OpponentPositionProfile.AggressionFactorIP` con `Passive == 0`

**Disparador:** villain con `TimesAggressiveIP=10`, `TimesPassiveIP=0` (cliff sin Laplace).

**Comportamiento esperado** (🟢 OpponentProfile.cs:27, ADR-0011):
- Laplace smoothing aplica `(10+1)/(0+1) = 11.0`.
- Sin smoothing sería división por cero o NaN (dependiendo del lenguaje); con smoothing se obtiene un AF "casi" 11 que decae a 1.0 si llega un sample passive.

**Consecuencias si se ignora:**
- Crash o NaN propagándose al árbol de decisión.
- Clasificación villain extrema (LAG super-LAG) sin la incertidumbre que debería tener.

**Cobertura test:** `TT-10` (cuarto caso: Aggressive ≥ 5, Passive = 0).

---

## EC-11 — `OpponentProfile.GetTypeForPosition(true)` sin datos IP suficientes

**Disparador:** villain con `OpponentPositionProfile_IP.HandsPlayed < 10`.

**Comportamiento esperado** (🟢 OpponentProfile.cs):
- Fallback al AF agregado (`OpponentProfile.AggressionFactor` con todos los samples).
- Si tampoco el agregado tiene reliability suficiente, retornar `OpponentType.Unknown`.

**Consecuencias si se ignora:**
- Type calculado con 2-3 samples IP es ruido estadístico — produce decisiones falsamente confiadas.

**Cobertura test:** `TT-09` con caso adicional sin datos IP.

---

## EC-12 — `VillainRange` con `OpponentProfile { VPIP=0 }` o `VPIP=100`

**Disparador:** villain extremo (jugador rock con VPIP=0, o jugador maniac con VPIP=100).

**Comportamiento esperado** (🟢 VillainRange.cs):
- VPIP escala el ancho del rango con factor `[0.5×, 2.0×]`.
- VPIP=0 ⇒ factor min `0.5×` (rango colapsado a top hands).
- VPIP=100 ⇒ factor max `2.0×` (rango ensanchado al límite).
- **No** debería colapsar a "ningún hand" ni "todos los hands"; el factor está acotado.

**Consecuencias si se ignora:**
- Sin clamp: VPIP=0 produce rango vacío → equity Monte Carlo lanza excepción al no encontrar hand combos.
- VPIP=100: rango total → MC simula 1326 combos uniformes (no realista).

**Cobertura test:** `TT-11` con casos `VPIP=0`, `VPIP=100`, `VPIP=NaN` (debería rechazar o clampear).

---

## EC-13 — Tipo `record` con igualdad estructural por defecto

**Comportamiento implícito en C#:**
- Records con la **misma firma de campos** y **mismos valores** son `Equals == true` y tienen el mismo `GetHashCode`.
- `Hand("AKs", true, "raise", 85) == Hand("AKs", true, "raise", 85)` retorna `true` aunque sean instancias distintas.

**Riesgo concreto:**
- Si dos objetos `StreetDecision` con campos idénticos se persisten, Marten **podría** deduplicar (depende del configurador). Verificar política en `OpenScrape.Infrastructure`.

**Consecuencias si se ignora:**
- Dos calles registradas con misma equity exacta (caso raro pero posible) podrían ser tratadas como una si Marten usa hashing estructural.

**Cobertura test:** verificar que `GameSession.Hands.Count` crece correctamente al agregar dos `StreetDecision` con valores iguales (en memoria, no persistidos).

---

## EC-14 — `[JsonIgnore]` propiedades calculadas (`TotalHands`, `TotalProfit`, `BBPer100`)

**Disparador:** Marten serializa `GameSession` y omite `Hands`, `TotalHands`, `TotalProfit`, `BBPer100`, `IsValid`.

**Comportamiento esperado** (🟢 GameSession.cs:28-49):
- Las propiedades calculadas se recomputan en memoria al deserializar.
- `Hands` carga desde la colección Marten externa (no embebida).

**Consecuencias si se ignora:**
- Si se quita `[JsonIgnore]` de `Hands`, Marten persistiría las hands embebidas dentro del documento `GameSession`, creando duplicación con la colección `HandRecord` separada.

**Cobertura test:** smoke test de round-trip: persistir `GameSession`, recuperar, agregar manos, recalcular `BBPer100`.

---

## EC-15 — `record sealed` con herencia accidental

**Disparador:** intento de heredar `ThresholdKey`.

**Comportamiento esperado** (🟢 ThresholdKey.cs:10):
- El compilador lanza error CS0509 "cannot derive from sealed type".

**Consecuencias si se ignora:**
- Permitir herencia abre la puerta a subclases que rompan invariantes (ej: subclase que sobrescriba `Equals` y elimine la validación de constructor).

**Cobertura test:** test de compilación que verifique que `class FakeKey : ThresholdKey { }` no compila (negative test, opcional).

---

## EC-16 — `HandRecord.AutoRebuy = 0` con auto-rebuy real ocurrido

**Disparador:** OCR falla al detectar el delta de stack que dispara auto-rebuy → `HandRecord.AutoRebuy` queda en `0` aunque ocurrió.

**Comportamiento esperado:** **NO existe protección** en Domain. Es responsabilidad del `OpenScrape.App.Forms.FrmMain` con `_heroStackPreRebuy` (ADR-0013).

**Consecuencias:**
- `NetProfit = HeroStackEnd - HeroStackStart + BlindPosted` aparenta gran ganancia (+95 BB de auto-rebuy contado como profit).
- Distorsiona BB/100 de la sesión.

**Cobertura test:** N/A en Domain. Cobertura vive en `OpenScrape.App.Tests` para `FrmMain.SetHeroStackUseCase`.

---

## EC-17 — `Card` con `Id` duplicado tras cargar 2× el catálogo

**Disparador:** seed inicial corre dos veces (ej. tras restaurar DB) → 104 cartas en lugar de 52.

**Comportamiento esperado:** **NO existe protección** en Domain. Es responsabilidad de `OpenScrape.Infrastructure` (índice único en Marten).

**Consecuencias:**
- `CardCacheService.GetByName("As")` retorna el primero encontrado, ignorando el duplicado.
- Reconciliar imágenes OCR contra el catálogo puede mapear a un Id distinto al esperado.

**Cobertura test:** N/A en Domain. Cobertura en `OpenScrape.Infrastructure` (constraint en setup Marten).

---

## EC-18 — `StreetDecision` con `BetSize` `null` o `Action` `null`

**Disparador:** decisión Check (no apuesta) → `BetSize` puede ser `null` o `0`.

**Comportamiento esperado:** **NO documentado explícitamente**. La inspección de código sugiere que `StreetDecision` es record con propiedades nullable opcionales (`Reason?`, `BoardTexture?`).

**Consecuencias:**
- Si `BetSize` no es nullable y se asigna `0` en checks, se confunde "check" con "bet de 0".
- 🔴 **Lacuna:** confirmar nullability exacta de `StreetDecision` con el legado.

**Cobertura test:** ver `questions.md` Q-04 (decisión sobre nullability).

---

## EC-19 — Mensaje de error de validación con caracteres no-ASCII (acentos)

**Disparador:** `Hand.Name` vacío produce mensaje "El nombre de la mano no puede estar vacío.".

**Comportamiento esperado:**
- Mensaje usa UTF-8 con acentos correctos (`"vacío"`, no `"vaci­o"`).
- Render correcto en logs y `MessageBox.Show()` de WinForms.

**Consecuencias si se ignora:**
- En sistemas con encoding incorrecto (Windows-1252 sin BOM), se muestran caracteres rotos.
- `appsettings.json` con BOM faltante puede romper el parseo de mensajes localizados.

**Cobertura test:** caso adicional verificando `mensaje.Contains("vacío")` con encoding UTF-8.

---

## EC-20 — `OpponentProfile` accedido desde múltiples threads

**Disparador:** game loop captura UI (thread principal) actualiza contadores; telemetría background los lee.

**Comportamiento esperado:** **NO documentado**. Los `+= 1` en contadores `int` **no** son atómicos en C# si el campo no es `Interlocked`.

**Consecuencias si se ignora:**
- Actualizaciones perdidas (`TimesVPIP++` paralelo: dos hilos ven 5, ambos escriben 6).
- Lecturas inconsistentes (`HandsPlayed` y `TimesVPIP` desincronizados un instante → VPIP > 100% momentáneamente).

**Mitigación documentada:** `_postflopGameContextLock` y `ConcurrentDictionary` viven en `OpenScrape.App.Services.GameCoordinator`. Domain mismo no protege.

**Cobertura test:** stress test multithread (en `App.Tests`, no Domain), 1000 incrementos paralelos sobre `OpponentProfile.TimesVPIP`.

---

## Resumen

20 casos extremos catalogados, distribuidos por origen:

| Categoría | EC | Causa raíz |
|-----------|-----|-----------|
| Validación inline (`record init`) | EC-01, EC-02, EC-03, EC-04 | Constructor lanza |
| Configuración (`StrategyProfile`) | EC-05 | Validación `IValidateOptions` startup |
| Cálculos lazy (`GameSession`) | EC-06, EC-07, EC-08 | Short-circuit + `[JsonIgnore]` |
| `OpponentProfile` | EC-09, EC-10, EC-11, EC-12 | Sentinels + Laplace smoothing |
| Semántica de records | EC-13, EC-14, EC-15 | C# records + `[JsonIgnore]` + `sealed` |
| Lacunas funcionales | EC-16, EC-17, EC-18 | Domain confía en capas superiores |
| Encoding e i18n | EC-19 | UTF-8 |
| Multithreading | EC-20 | No protegido en Domain |

🔴 EC-18 marca una lacuna real (`StreetDecision` nullability) → ver `questions.md`.
