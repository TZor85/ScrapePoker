# US-02 — Configuración inicial: arranque, selección de ventana, calibración

> **Historia de onboarding.** Cubre lo que ocurre desde que Pablo lanza el `.exe` por primera vez (o tras un cambio de cliente/tableMap) hasta que el bot está listo para empezar a jugar. Sin este flujo, US-01 no puede ejecutarse: faltan regions, profile válido o ventana seleccionada.

---

## 1. Persona

**Pablo** — mismo que US-01, pero en estado pre-juego.
- Acaba de instalar la app o ha recibido una actualización de `tableMap.json`.
- Tiene el cliente de poker abierto pero NO está jugando todavía.
- Necesita validar que todo está en orden antes de comprometerse a una sesión de 6 h.

---

## 2. Historia (formato narrativo)

> **Como** Pablo,
> **quiero** que el bot arranque, valide su configuración, me deje seleccionar la ventana del cliente y cargar el `tableMap.json` correcto antes de iniciar la sesión,
> **para** evitar empezar a jugar con regions mal calibradas o thresholds incoherentes que arruinen la sesión.

---

## 3. Criterios de aceptación

### CA-01 — Arranque de la app y validación de variables de entorno

**Dado** que Pablo hace doble clic en `OpenScrape.App.exe` (o lanza desde Visual Studio con perfil `OpenScrape.App`),
**Cuando** `Program.Main` ejecuta:
1. `Host.CreateApplicationBuilder` con `DOTNET_ENVIRONMENT=Development`,
2. Carga `appsettings.json` + `appsettings.Development.json` (override),
3. `ConfigureServices` registra ~50 servicios (singletons, scoped, transient con forwarding pattern),
4. `host.Build()`,
**Entonces** ningún `IConfiguration` lanza, todos los `IOptions<T>` se resuelven sin errores,
**Y** la app NO levanta `FrmMain` todavía — antes ejecuta validación de profile.

🟢 Confirmado en `Program.cs:40-180` + DD-01.

### CA-02 — Validación fail-fast de `StrategyProfile`

**Dado** que `host.Build()` completa,
**Cuando** `Program.Main` resuelve `IOptions<StrategyProfile>` y llama `StrategyProfileValidator.Validate(profile)`,
**Entonces:**
- ✅ Si pasa: `Application.Run(form)` se ejecuta normalmente.
- ❌ Si falla: `MessageBox.Show(ex.Message)` muestra la lista completa de errores acumulados (claves faltantes, tiers desordenados, valores fuera de rango), seguido de `Environment.Exit(1)` SIN levantar `FrmMain`.

🟢 Confirmado en DD-03 + ADR-0008 + `StrategyProfileValidator.cs`.

**Negativo:** Pablo edita `appsettings.json` con typo (`"Flop_OpenRase"` en vez de `"Flop_OpenRaise"`); al lanzar, ve `MessageBox` con: *"Falta clave Flop_OpenRaise en StrategyProfile.StreetThresholds"*. Cierra, corrige, relanza.

### CA-03 — Resolución de `FrmMain` desde scope async

**Dado** que la validación pasa,
**Cuando** `Program.Main`:
1. `await using var scope = host.Services.CreateAsyncScope()`,
2. `var form = scope.ServiceProvider.GetRequiredService<FrmMain>()`,
3. `Application.Run(form)`,
**Entonces** `FrmMain` se construye con sus 13 dependencias scoped (`GameCoordinator`, `TableLayoutService`, `GameLoggerService`, `PostflopContextHolder`, etc.),
**Y** al cerrar (`Application.Run` retorna), el scope se dispone con `await scope.DisposeAsync().AsTask().GetAwaiter().GetResult()` invocando los `IAsyncDisposable` (incluyendo `GameLoopCoordinator`).

🟢 Confirmado en DD-01 + ADR-0014.

### CA-04 — Splash inicial con pestañas vacías

**Dado** que `FrmMain` se levanta,
**Cuando** Pablo ve la ventana principal,
**Entonces** observa 5 pestañas: **Juego** | **Config** | **Tablas** | **Logs** | **Historial**.
- Pestaña Juego: TextBox de logs vacío (`tbResume`); botones "Iniciar captura" deshabilitado, "Seleccionar ventana" habilitado.
- Pestaña Config: campos del profile populated (read-only en MVP).
- Pestaña Tablas: dropdown de `tableMap.json` cargados desde Marten + DataGridView con regions actuales.
- Pestaña Logs: ComboBox de filtros (Errors / Debug / All).
- Pestaña Historial: dgvSessions vacío hasta primera sesión guardada.

🟢 Confirmado en `FrmMain.Designer.cs` + design.md § Forms.

### CA-05 — Selección de ventana del cliente vía `FormListApps`

**Dado** que Pablo presiona "Seleccionar ventana",
**Cuando** se abre `FormListApps`,
**Entonces** ve una lista filtrada de ventanas visibles cuyo título contenga `"NL H"` (filtro hardcoded, Q-APP-08),
**Y** al hacer doble clic sobre una entrada, el formulario cierra y `FrmMain._handle: IntPtr` queda con el handle seleccionado,
**Y** el botón "Iniciar captura" se habilita.

🟢 Confirmado en `FormListApps.cs` + DD-01.

**Variante:** si NO hay ventanas con `"NL H"` (ej. cliente cerrado o título distinto), la lista aparece vacía. Pablo tiene que abrir el cliente o ajustar el filtro (decisión Q-APP-08 abierta).

### CA-06 — Carga de `tableMap.json` y populación de `RegionLookupCache`

**Dado** que Pablo está en pestaña Tablas y selecciona un `tableMap.json` del dropdown,
**Cuando** `LoadTableMapUseCase.HandleAsync(mapName)` ejecuta:
1. Marten query: obtiene el `RegionTableMap` con `Name=mapName` (cached vía `CardCacheService` analog),
2. `RegionLookupCache.Initialize(map.Regions)` pre-construye el `Dictionary<regionName, Region>` con `OrdinalIgnoreCase`,
3. `FrmMain` redibuja la pestaña Tablas con las nuevas regions visibles en el DataGridView,
**Entonces** Pablo ve filas con: `Name`, `PosX`, `PosY`, `Width`, `Height`, `Color`,
**Y** las consultas posteriores `regionLookupCache.GetRegion(mapId, regionName)` retornan en O(1).

🟢 Confirmado en DD-14 + `LoadTableMapUseCase.cs` + `RegionLookupCache.cs`.

### CA-07 — Pre-carga lazy de cartas (`CardCacheService`)

**Dado** que un `UseCase` solicita cartas (`GetCardsFlopUseCase`) por primera vez,
**Cuando** `CardCacheService.GetAllAsync` se invoca,
**Entonces** el servicio toma el `_loadLock: SemaphoreSlim`, ejecuta una query Marten retornando 52 cartas, las cachea en `_cards: List<Card>` y libera el lock,
**Y** las llamadas siguientes retornan el cache (sin Marten).

🟢 Confirmado en DD-14 + ADR-0016.

### CA-08 — Coordenadas escaladas vía `CoordinateScaler`

**Dado** que el bot captura por primera vez (`btnCapture_Click`),
**Cuando** se invoca `CoordinateScaler.Initialize(refW: image.Width, refH: image.Height)`,
**Entonces** el scaler queda inicializado one-shot (lock interno para idempotencia),
**Y** las llamadas posteriores `ScaleRegion(posX, posY, w, h, currentW, currentH)` retornan coordenadas con `scale = (currentW/refW + currentH/refH)/2.0`,
**Y** si el cliente NO cambia de tamaño en la sesión, las regions del `tableMap.json` se aplican al pixel.

🟢 Confirmado en `CoordinateScaler.cs` + ADR-0016.

### CA-09 — Calibración manual de regions (pestaña Tablas)

**Dado** que Pablo encuentra que el bot lee mal el slot `Card1` (OCR retorna basura),
**Cuando** Pablo:
1. Va a pestaña Tablas,
2. Selecciona la fila `Card1` en el DataGridView,
3. Modifica `PosX`, `PosY`, `Width` o `Height` directamente,
4. Presiona "Guardar",
**Entonces** `UpdateRegionTableMapUseCase` persiste los cambios en Marten,
**Y** `RegionLookupCache.Initialize(updatedMaps)` se invoca para refrescar el dict in-memory,
**Y** la próxima captura usa las nuevas coordenadas.

🟢 Confirmado en `UpdateRegionTableMap.cs` + R-XX.

**Limitación EC-21:** si el `BackgroundWorker` está activo durante el reload, hay race condition leve (Q-APP-16).

### CA-10 — Logs de arranque visibles en pestaña Logs

**Dado** que durante `Host.Build()` los servicios emiten logs (DI graph, validación, primera carga),
**Cuando** `FrmMain.OnLoad` invoca `TextBoxLoggerProvider.SetTextBoxTarget(tbResume)`,
**Entonces** todos los logs bufferizados (hasta `BufferCapacity=1000`) se flushean al TextBox en orden FIFO,
**Y** Pablo puede ver desde el primer momento qué servicios cargaron, qué regions se inicializaron, y cualquier warning de validación menor (que no bloqueó arranque).

🟢 Confirmado en DD-17 + ADR-0009.

---

## 4. Diagrama de flujo (onboarding)

```
Pablo abre cliente de poker (precondición)
         │
         ▼
Pablo lanza OpenScrape.App.exe
         │
         ▼
┌─── Program.Main ───────────────────────────────┐
│                                                │
│  Host.CreateApplicationBuilder                 │
│  → DOTNET_ENVIRONMENT=Development              │
│                                                │
│  IConfiguration:                               │
│  ├─ appsettings.json (versionado, anomalía)    │
│  └─ appsettings.Development.json (override)    │
│                                                │
│  ConfigureServices                             │
│  ├─ AddDataBase (Marten singleton)             │
│  ├─ Forwarding pattern: 18 pares conc+iface    │
│  ├─ FrmMain como Transient                     │
│  └─ ~50 servicios registrados                  │
│                                                │
│  host.Build()                                  │
│                                                │
│  IOptions<StrategyProfile>                     │
│  StrategyProfileValidator.Validate             │
│  ├─ FAIL → MessageBox + Environment.Exit(1)    │
│  └─ OK   ▼                                     │
│                                                │
│  CreateAsyncScope                              │
│  → form = sp.GetRequiredService<FrmMain>()     │
│                                                │
│  Application.Run(form)                         │
│                                                │
└────────────────────────────────────────────────┘
         │
         ▼
FrmMain.OnLoad
  ├─ TextBoxLoggerProvider.SetTextBoxTarget(tbResume)
  │  → flush 1000 logs buffered
  ├─ Cargar dropdown de tableMaps (Marten)
  └─ pestaña Juego visible (botón "Iniciar captura" disabled)
         │
         ▼
Pablo presiona "Seleccionar ventana"
  ├─ FormListApps abre
  ├─ EnumWindows + filtro "NL H"
  ├─ Pablo doble-clic en ventana del cliente
  └─ _handle = IntPtr seleccionado
         │
         ▼
[Opcional: Pablo va a pestaña Tablas]
  ├─ Selecciona tableMap.json
  ├─ LoadTableMapUseCase.HandleAsync
  │  → RegionLookupCache.Initialize
  ├─ DataGridView muestra regions
  ├─ [Opcional: editar PosX/Y/W/H + Guardar]
  └─ UpdateRegionTableMapUseCase persiste
         │
         ▼
Botón "Iniciar captura" enabled
         │
         ▼
[Pablo entra a US-01: Captura → Decisión]
```

---

## 5. Variantes (caminos secundarios)

### V-01 — Primera vez en máquina nueva sin `appsettings.Development.json`
- `appsettings.json` versionado tiene credenciales del usuario original (anomalía DD-18).
- Al cargar: `IConfiguration` carga las credenciales versionadas → riesgo de escritura en BD producción ajena.
- Comportamiento esperado tras Q-APP-01 resuelta: rotar credenciales + fail-fast con mensaje accionable.
- 🔴 **Estado actual:** silencioso. Pablo NO sabe que está apuntando a BD ajena.

### V-02 — `tableMap.json` corrupto con coordenadas extremas
- `Region.PosX = 99999` → `CoordinateScaler.ScaleRegion` produce región fuera del bitmap.
- `OcrService.ExtractTextFromRegionAsync` con region OOB → `OutOfMemoryException` o crash de proceso (EC-17).
- Comportamiento esperado tras Q-APP-15: validación bounds + clamp + log.

### V-03 — Pablo aplica nuevo `tableMap.json` con BG worker activo
- Race condition en `RegionLookupCache.Initialize` (EC-21, Q-APP-16).
- Comportamiento actual: excepción transitoria capturada por BG worker; recupera al ciclo siguiente.

### V-04 — Cliente con título distinto al filtro `"NL H"`
- `FormListApps` muestra lista vacía.
- Pablo no entiende por qué su cliente no aparece.
- Mitigación pendiente: hacer filtro configurable (Q-APP-08) o añadir tooltip explicativo.

### V-05 — Profile con tier inválido (`FoldBelow=80, ThinValueAbove=70`)
- `StrategyProfileValidator.Validate` detecta orden incoherente.
- `MessageBox` muestra: *"Tier inválido en Flop_OpenRaise: FoldBelow (80) >= ThinValueAbove (70)"*.
- Pablo abre `appsettings.json` con notepad, corrige, relanza.

### V-06 — Tesseract `eng.traineddata` corrupta
- `OcrService` constructor lanza `TesseractException`.
- `Host.Build()` propaga → `MessageBox` genérico de WinForms con stack trace (UX pésima, EC-02, Q-APP-19).
- Pendiente: auto-recovery desde recurso embebido o catch específico.

### V-07 — `appsettings.json` editado mientras la app corre
- Hot reload propaga el cambio a `IOptions`, pero `ThresholdsRegistry` (singleton) NO observa.
- Cambio silenciosamente ignorado (EC-04, Q-APP-09).
- Pablo cree que aplicó pero motor sigue con thresholds viejos.

---

## 6. Métricas de éxito

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Tiempo desde doble clic hasta `FrmMain` visible** | <3 s | manual |
| **Tiempo de validación de profile** | <100 ms | log |
| **Tasa de arranques exitosos** (sin `Environment.Exit`) | 99 % en máquinas configuradas | telemetría EventLog |
| **Tiempo de carga de `tableMap.json`** (cache miss) | <500 ms | `MetricsCollector` (no instrumentado actualmente) |
| **Errores de bounds en regions** | 0 por sesión | log + EC-17 |

---

## 7. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| Credenciales hardcoded ajenas | 🔴 | — | Rotar + purgar Git (Q-APP-01 ✅ decidido) |
| `EncrypterHelper` IV fija | 🔴 | — | Migrar a AesGcm (Q-APP-02 ✅ decidido) |
| `tableMap.json` corrupto bounds | 🔴 | — | Validación + clamp (Q-APP-15) |
| Reload race condition | 🟡 | BG worker try/catch | Reasignación atómica (Q-APP-16) |
| Filtro `"NL H"` excluye clientes válidos | 🟡 | — | Hacer configurable (Q-APP-08) |
| Profile inválido sin onboarding | 🟡 | Fail-fast con MessageBox | UX más amigable (editor inline) |
| Multi-monitor DPI distinto | 🟡 | DPI aware proceso completo | Per-monitor DPI (Q-APP-10) |
| Tesseract data corrupta | 🔴 | — | Auto-recovery embebida (Q-APP-19) |

---

## 8. Dependencias

**Otras user stories:**
- US-01 depende de US-02 — sin onboarding completado, US-01 no puede iniciar.
- US-03 (sesión de juego) depende de US-02.

**Specs por unit:**
- `OpenScrape.App/` — `Program.cs`, `FrmMain.OnLoad`, `FormListApps`, `RegionLookupCache`, `CoordinateScaler`, `StrategyProfileValidator`, `TextBoxLoggerProvider`.
- `OpenScrape.Features/` — `LoadTableMapUseCase`, `UpdateRegionTableMapUseCase`, `GetAllRegionTableMap`.
- `OpenScrape.Domain/` — `RegionTableMap`, `Region`, `StrategyProfile`, `StreetThresholds`.
- `OpenScrape.Infrastructure/` — Marten setup.

**Externo:**
- Cliente de poker abierto (precondición de CA-05).
- PostgreSQL accesible (precondición de CA-06).
- `Resources/tessdata/eng.traineddata` válido (precondición de CA-07).
- `appsettings.Development.json` con credenciales correctas en máquina destino (precondición de CA-01).

---

## 9. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] CA-01 a CA-10 pasan en testing manual.
- [ ] Una máquina nueva (sin estado previo) ejecuta exitosamente: instalar → lanzar → seleccionar ventana → cargar tableMap → estar listo para captura, en <2 minutos.
- [ ] Profile inválido produce `MessageBox` con mensaje accionable (no stack trace genérico).
- [ ] `tableMap.json` con valores extremos no causa crash de proceso (Q-APP-15 implementada).
- [ ] Filtro de ventanas configurable o documentado (Q-APP-08).
- [ ] Credenciales rotadas y movidas fuera de `appsettings.json` (Q-APP-01 ✅).

---

## 10. Notas

- **Posición en el funnel del producto:** US-02 es **friction-bearing** — todo lo que falle aquí bloquea conversión a usuario activo. Pablo aún no tiene "skin in the game" y abandonará si la UX de onboarding es mala.
- **Onboarding distribuido:** algunas decisiones se persisten cross-sesión (`tableMap.json` en Marten, `appsettings.Development.json` en disco), otras son volátiles (selección de ventana, scope DI). Documentar el ciclo de vida de cada artefacto.
- **Tras decisiones Q-APP-01/02 confirmadas por Pablo:** rotar credenciales + purgar histórico Git + migrar EncrypterHelper a AesGcm son tareas pre-distribución obligatorias.
- **Doc gap:** no hay un README de onboarding paso-a-paso para developers nuevos. Mitigación: añadir `docs/onboarding.md` cuando el equipo crezca.
