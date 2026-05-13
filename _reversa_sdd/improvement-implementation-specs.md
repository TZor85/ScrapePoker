# Especificaciones de implementacion de mejoras - ScrapePoker

> Generado el 2026-05-13 a partir de `_reversa_sdd/improvement-opportunities.md`, `_reversa_sdd/gaps.md` y `_reversa_sdd/confidence-report.md`.
> Alcance: convertir oportunidades de mejora en especificaciones ejecutables. No modifica codigo legado.

---

## Principios de ejecucion

- Priorizar primero riesgo de seguridad, configuracion y bugs confirmados.
- Mantener compatibilidad funcional del bot salvo donde la mejora indique lo contrario.
- Toda mejora debe tener prueba automatizada o smoke test documentado.
- Cambios en `OpenScrape.DecisionMaker` deben preservar paridad de la matriz de decisiones existente.
- Datos sensibles, aliases OCR y stacks no deben aparecer en logs/exports salvo modo debug explicito.
- Ninguna mejora debe introducir dependencias desde capas inferiores hacia `OpenScrape.App`.

---

## Roadmap recomendado

| Sprint | Objetivo | Specs |
|--------|----------|-------|
| 1 | Riesgo bajo, impacto alto | SPEC-IMP-01, SPEC-IMP-02 |
| 2 | Correctitud DecisionMaker | SPEC-IMP-03 |
| 3 | Persistencia, privacidad y recursos | SPEC-IMP-04, SPEC-IMP-05 |
| 4 | Arquitectura y loop de juego | SPEC-IMP-06, SPEC-IMP-07, SPEC-IMP-11 |
| 5 | Operacion, CI, performance y producto | SPEC-IMP-08, SPEC-IMP-09, SPEC-IMP-10 |
| Futuro | Licencias/backend | SPEC-IMP-12 |

---

## SPEC-IMP-01 - Seguridad y configuracion critica

### Trazabilidad

Mejoras: 1, 2, 3, 4, 7, 48, 57, 67, 68, 69, 70.  
Gaps relacionados: credenciales reales, `IsDevelopment=true`, connection string sin guard, IV fija, datos sensibles en logs.

### Objetivo

Evitar que la aplicacion arranque en modo peligroso o exponga secretos/datos sensibles por defecto, manteniendo una ruta compatible para datos/config legacy.

### Requisitos funcionales

- RF-01: `Program.cs` debe pasar una politica real de entorno a `AddDataBase`: `context.HostingEnvironment.IsDevelopment()` o una opcion tipada equivalente.
- RF-02: `AddDataBase` debe fallar rapido si falta `ConnectionStrings:DefaultConnection`, con mensaje accionable.
- RF-03: `appsettings.json` distribuible no debe contener credenciales reales. Secrets locales deben cargarse desde `appsettings.Development.json`, variables de entorno, User Secrets o mecanismo equivalente.
- RF-04: La configuracion sensible recordada localmente debe cifrarse mediante `ISecretProtector`.
- RF-05: `ISecretProtector` debe soportar lectura legacy si existe cifrado anterior con IV fija y escritura nueva versionada con cifrado autenticado.
- RF-06: Primer arranque debe mostrar/registrar aceptacion de disclaimer de uso educativo/TOS antes de habilitar automatismos sensibles.
- RF-07: Debe existir sanitizador de logs/export que remapee aliases o los oculte en modo privado.
- RF-08: Debe existir politica de retencion/purge de sesiones por rango de fechas y por propietario.
- RF-09: Debe existir suite `ConfigurationValidationTests` para connection string, thresholds, tessdata y paths.

### Diseno

- Crear `DatabaseOptions` con:
  - `AutoCreatePolicy`: `None`, `CreateOrUpdateInDevelopment`, `All`.
  - `RequireConnectionString`: `true` por defecto.
- Cambiar `AddDataBase(IConfiguration configuration, bool isDevelopment)` hacia sobrecarga tipada o mantener sobrecarga actual como adaptador.
- Crear `ISecretProtector` en capa apropiada y `DpapiSecretProtector` en App/Infrastructure Windows-only.
- Crear formato cifrado versionado:
  - `v1:` legacy compatible, lectura solamente.
  - `v2:` AES-GCM o DPAPI, escritura por defecto.
- Crear `SensitiveDataSanitizer` reutilizable por logging/export.

### Criterios de aceptacion

- CA-01: En entorno no development, Marten no usa `AutoCreate.All`.
- CA-02: Sin connection string, el arranque falla con `InvalidOperationException("Connection string 'DefaultConnection' no encontrada.")` o mensaje equivalente.
- CA-03: Build/publish no incluye password real en `appsettings.json`.
- CA-04: Valor cifrado legacy se puede leer; valor guardado nuevo no usa IV fija.
- CA-05: Tampering del secreto nuevo se detecta y no devuelve texto corrupto silencioso.
- CA-06: Logs/export en modo privado no contienen alias OCR reales.
- CA-07: Purge borra sesiones/manos del rango seleccionado y no afecta otros propietarios.
- CA-08: Cada error critico de configuracion falla con mensaje accionable.

### Tests

- `DatabaseConfigurationTests`: entorno dev/prod cambia `AutoCreate`.
- `DatabaseConfigurationTests`: config vacia falla con mensaje accionable.
- `SecretProtectorTests`: roundtrip nuevo, lectura legacy, corrupcion detectada.
- `SensitiveDataSanitizerTests`: alias/stacks quedan anonimizados.
- `ConfigurationValidationTests`: connection string, thresholds, tessdata y paths invalidos.
- Smoke: publish limpio + arranque con config local.

### Tareas

- [ ] Extraer opciones de base de datos.
- [ ] Cambiar `Program.cs` para no hardcodear `true`.
- [ ] Agregar guard de connection string.
- [ ] Separar config distribuible/local.
- [ ] Implementar `ISecretProtector`.
- [ ] Migrar lectura/escritura sensible a wrapper.
- [ ] Agregar disclaimer primer arranque.
- [ ] Implementar sanitizador y retencion.
- [ ] Agregar `ConfigurationValidationTests`.
- [ ] Rotar credenciales actuales fuera del repo si se va a compartir.

---

## SPEC-IMP-02 - Correctitud preflop y use cases Features

### Trazabilidad

Mejoras: 8, 9, 19, 20, 26, 58.  
Gaps relacionados: `BetSize` comentado, use cases placeholder, flags de `UpdateRegionTableMap`, excepciones pobres.

### Objetivo

Recuperar lookup preflop por `BetSize`, eliminar rutas muertas y hacer que los errores de datos sean diagnosticables.

### Requisitos funcionales

- RF-01: `GetActionScenario` debe filtrar por `BetSize` cuando request y datos lo incluyan.
- RF-02: `BetSize` debe normalizarse antes de comparar para evitar fallos por precision decimal.
- RF-03: Si `BetSize` viene `null`, se debe usar fallback explicito documentado.
- RF-04: Seleccion aleatoria ponderada de acciones debe usar RNG inyectable o abstraccion testeable.
- RF-05: Errores de lectura/parseo de JSON deben preservar `InnerException`.
- RF-06: `GetAllTables`, `GetAllRegionTableMap` y `GetFlopCards` deben eliminarse si no tienen consumidores productivos.
- RF-07: `UpdateRegionTableMap` debe distinguir create vs update parcial. En create debe persistir flags del request.
- RF-08: JSON preflop debe validarse por contrato: schema, `BetSize`, porcentajes y nombres de accion.

### Diseno

- Crear `IRandomProvider` minimal:
  - `int Next(int maxValue)`
  - `double NextDouble()`
- Comparador `BetSize`:
  - Redondeo configurable a 2 decimales o tolerancia `0.01`.
  - Decision debe quedar en test.
- Excepciones:
  - `throw new InvalidOperationException($"No se pudo cargar escenario preflop '{path}'.", ex);`
- Contract tests leen todos los JSON en carpeta de escenarios.

### Criterios de aceptacion

- CA-01: Dos escenarios iguales salvo `BetSize` devuelven acciones distintas cuando corresponde.
- CA-02: Mismo seed/RNG fake produce misma seleccion ponderada.
- CA-03: Fallo de JSON conserva `InnerException`.
- CA-04: Build no contiene referencias a use cases eliminados.
- CA-05: Creacion inicial de region guarda `IsHash/IsColor/IsBoard/IsOnlyNumber`.
- CA-06: Todos los JSON preflop pasan contrato.

### Tests

- `GetActionScenarioTests.BetSizeParticipaEnLookup`.
- `GetActionScenarioTests.BetSizeNullUsaFallback`.
- `GetActionScenarioTests.RandomPonderadoEsDeterministaConProviderFake`.
- `RegionTableMapTests.CreatePersisteFlags`.
- `PreflopJsonContractTests`.

### Tareas

- [ ] Localizar consumidores de placeholders y eliminarlos.
- [ ] Reactivar filtro `BetSize`.
- [ ] Normalizar comparacion decimal.
- [ ] Introducir RNG inyectable en Features.
- [ ] Mejorar wrapping de excepciones.
- [ ] Corregir create/update de flags.
- [ ] Agregar contract tests JSON.

---

## SPEC-IMP-03 - Hardening de DecisionMaker

### Trazabilidad

Mejoras: 5, 6, 10, 11, 14, 17, 18, 38, 59.  
Gaps relacionados: input sin guardias, `AutoCalibrationService.OldValue`, `Random.Shared`, `BigBlind=1.0`, auto-rebuy, watchdog, all-in global.

### Objetivo

Hacer decisiones reproducibles, trazables y resistentes a errores OCR/MC sin emitir decisiones absurdas.

### Requisitos funcionales

- RF-01: `PostflopDecisionInput` debe validarse antes de decidir.
- RF-02: Valores invalidos (`NaN`, infinito, equity fuera de 0..100, pot negativo, stack negativo) no deben generar decision agresiva.
- RF-03: En runtime UI, input invalido debe producir accion segura (`Check` si no facing bet, `Fold` o `Call` segun politica conservadora si facing bet) y log de warning estructurado.
- RF-04: En tests/servicios puros, debe existir modo fail-fast con excepcion.
- RF-05: `AutoCalibrationService` debe leer `OldValue` real desde `StrategyProfile`.
- RF-06: `ExploitabilityCalculator` debe recibir BigBlind desde sesion/contexto, nunca constante global.
- RF-07: Randomizacion de DecisionMaker y Monte Carlo debe poder reproducirse por seed.
- RF-08: Si una decision usa random, el seed/roll debe quedar en `DecisionTrace`.
- RF-09: Formula de auto-rebuy debe ser regla de dominio: `100 BB - currentStack`, con tratamiento de top-up manual.
- RF-10: FSM/game loop debe tener watchdog por estado y reset controlado.
- RF-11: `PostflopGameContext` debe distinguir all-in por jugador/asiento, no solo `IsAnyoneAllIn`.
- RF-12: Cada decision debe poder emitir `DecisionTrace` con inputs, thresholds, branch, ajustes, seed y accion final.

### Diseno

- Crear `PostflopDecisionInputValidator`.
- Crear `DecisionSafetyPolicy`:
  - `FailFast`
  - `FailSoftConservative`
- Crear `IRandomProvider` compartido o adaptar el de SPEC-IMP-02.
- Crear `DecisionTrace` record:
  - `DecisionId`, `HandId`, `Street`, `InputSnapshot`, `Thresholds`, `Adjustments`, `Branch`, `RandomSeed`, `RandomRoll`, `FinalAction`, `Warnings`.
- Cambiar `ExploitabilityCalculator.RecordDecision` para recibir `bigBlind` o `SessionContext`.
- Extender context all-in:
  - `IReadOnlyDictionary<int, PlayerAllInState> AllInBySeat`.

### Criterios de aceptacion

- CA-01: Equity `NaN` no produce bet/raise.
- CA-02: Pot/stack negativos quedan bloqueados por validacion.
- CA-03: Profile activo con valores no 45/40 produce `OldValue` exacto.
- CA-04: Misma seed produce misma accion y mismo trace.
- CA-05: Misma mano con BB distinta escala mbb/BB100 correctamente.
- CA-06: Stack 48BB recomienda rebuy 52BB; stack 60BB recomienda 40BB; stack 100BB recomienda 0.
- CA-07: Watchdog resetea mano atascada y deja log.
- CA-08: Multiway con un villano all-in y otro activo conserva fold equity solo contra jugador activo.

### Tests

- `PostflopDecisionInputValidationTests`.
- `AutoCalibrationServiceTests.OldValueSaleDelProfile`.
- `ExploitabilityCalculatorTests.BigBlindDesdeSesion`.
- `RandomReproducibilityTests`.
- `AutoRebuyDomainTests`.
- `GameLoopWatchdogTests` con reloj fake.
- `AllInPerPlayerTests`.
- Snapshot tests de `DecisionTrace`.

### Tareas

- [ ] Implementar validador y politica fail-soft/fail-fast.
- [ ] Reemplazar `Random.Shared` por provider/seed.
- [ ] Propagar seed a trace.
- [ ] Corregir `OldValue`.
- [ ] Parametrizar BigBlind.
- [ ] Formalizar auto-rebuy en dominio.
- [ ] Agregar watchdog con reloj fake.
- [ ] Modelar all-in por asiento.
- [ ] Crear `DecisionTrace`.

---

## SPEC-IMP-04 - Persistencia de oponentes, historial explotable y privacidad

### Trazabilidad

Mejoras: 12, 13, 51, 52.

### Objetivo

Persistir aprendizaje de villanos y analitica entre sesiones sin exponer aliases reales.

### Requisitos funcionales

- RF-01: Cada alias OCR debe mapearse a un `OpponentId` GUID estable.
- RF-02: `OpponentProfile` debe sobrevivir reinicio de aplicacion.
- RF-03: Debe existir entidad `OpponentAlias` con `Alias`, `OpponentId`, `FirstSeen`, `LastSeen`, y opcionalmente sala/mesa.
- RF-04: Export anonimo debe remapear aliases a `player1`, `player2`, etc. de forma estable dentro del export.
- RF-05: Sesiones/manos deben incluir `OwnerId` o equivalente cuando exista multiusuario/licencia.
- RF-06: Queries de historial deben filtrar por propietario si el campo existe.
- RF-07: `ExploitabilityCalculator` debe persistir resumen por sesion o registros agregados, no necesariamente cada decision cruda.

### Diseno

- Dominio:
  - `OpponentAlias`
  - `OpponentProfileSnapshot`
  - `DecisionLeakSummary`
- Interfaces:
  - `IOpponentProfileRepository`
  - `IExploitabilitySummaryRepository`
- Infraestructura Marten implementa repositorios.
- `OpponentTracker` queda como cache/servicio in-memory con hydration al inicio.

### Criterios de aceptacion

- CA-01: Alias conocido antes de cerrar app conserva mismo GUID tras reinicio.
- CA-02: Alias nuevo crea GUID nuevo.
- CA-03: Export no contiene string de alias real.
- CA-04: Dos propietarios no ven sesiones cruzadas.
- CA-05: TopLeaks conserva historial tras reinicio.

### Tests

- `OpponentProfilePersistenceTests`.
- `OpponentAliasMappingTests`.
- `AnonymousExportTests`.
- `OwnerIsolationQueryTests`.
- `ExploitabilitySummaryPersistenceTests`.

### Tareas

- [ ] Definir entidades/records de persistencia.
- [ ] Crear repositorios e implementacion Marten.
- [ ] Hidratar `OpponentTracker` al inicio.
- [ ] Persistir cambios al cierre o por batch.
- [ ] Crear exportador anonimo.
- [ ] Agregar filtros por propietario.
- [ ] Persistir resumen de leaks.

---

## SPEC-IMP-05 - Recursos, OCR y deteccion de ventanas

### Trazabilidad

Mejoras: 35, 36, 43, 44, 46.

### Objetivo

Hacer que recursos/tablemaps/OCR funcionen fuera de la ruta de desarrollo y que fallos visuales sean reproducibles.

### Requisitos funcionales

- RF-01: Carpeta de recursos debe ser configurable por usuario.
- RF-02: No debe existir dependencia a `C:\Code\...` en runtime.
- RF-03: Filtro de ventana debe ser configurable por patron simple, regex o modo "mostrar todas".
- RF-04: `eng.traineddata` debe tener una unica fuente canonica.
- RF-05: Al arranque se debe validar tessdata; si falta o esta corrupto, mostrar mensaje claro y accion de reparacion.
- RF-06: Diagnostico OCR debe poder guardar snapshot con crop, region id, texto OCR, confidence y decision del consenso.

### Diseno

- Crear `ResourcePathsOptions`:
  - `BaseResourcesPath`
  - `TableMapsPath`
  - `TessDataPath`
  - `DiagnosticsPath`
- Crear `WindowDiscoveryOptions`:
  - `TitlePattern`
  - `UseRegex`
  - `ShowAll`
- Crear `OcrDiagnosticsArtifact`.
- Packaging: recurso embebido o archivo distribuido, pero solo una fuente.

### Criterios de aceptacion

- CA-01: App publicada en otra carpeta carga tablemaps desde ruta configurada.
- CA-02: Sin tessdata, app no crashea sin mensaje.
- CA-03: Archivo duplicado de `eng.traineddata` desaparece del publish.
- CA-04: Ventanas con titulo distinto a `"NL H"` pueden aparecer por config.
- CA-05: Falla OCR genera artifact reproducible.

### Tests

- `ResourcePathsOptionsTests`.
- `WindowDiscoveryFilterTests`.
- Smoke manual: mover binario a otra ruta.
- Smoke manual: borrar/corromper tessdata.
- `OcrDiagnosticsArtifactTests`.

### Tareas

- [ ] Localizar rutas absolutas.
- [ ] Introducir opciones de recursos.
- [ ] Crear dialog/config inicial.
- [ ] Parametrizar filtro de ventana.
- [ ] Unificar `eng.traineddata`.
- [ ] Agregar validacion/reparacion OCR.
- [ ] Implementar artifacts de diagnostico.

---

## SPEC-IMP-06 - Game loop, FrmMain y robustez UI

### Trazabilidad

Mejoras: 21, 22, 39, 40, 41, 56, 61.

### Objetivo

Reducir riesgo de `FrmMain`, hacer `GameLoopCoordinator` ruta primaria y estabilizar start/stop/captura.

### Requisitos funcionales

- RF-01: `GameLoopCoordinator` debe ser la ruta primaria bajo feature flag activable.
- RF-02: `BackgroundWorker1_DoWork` debe quedar legacy temporal o eliminarse tras paridad.
- RF-03: Loop debe usar `CancellationToken`, timeout y disposal determinista.
- RF-04: Intervalo de captura debe ser adaptativo por estado: idle, action-detection, cooldown.
- RF-05: Bitmaps/crops deben auditarse para `Dispose`.
- RF-06: Debe existir contador/debug de GDI handles o metrica equivalente en sesion larga.
- RF-07: Logica extraida de UI debe tener tests sin WinForms cuando sea posible.
- RF-08: Smoke UI debe cubrir arranque, carga tablemap, procesamiento de mano y overlay.

### Diseno

- Extracciones sugeridas:
  - `CaptureCoordinator`
  - `HandLifecycleCoordinator`
  - `OverlayCoordinator`
  - `CalibrationUiController`
  - `HistoryTabController`
- Migracion por fases:
  - Fase A: coordinator en paralelo con flag.
  - Fase B: coordinator primario.
  - Fase C: borrar ruta legacy.

### Criterios de aceptacion

- CA-01: Start/stop repetido 20 veces no deja tareas vivas.
- CA-02: Cierre de app cancela loop en timeout definido.
- CA-03: CPU baja en idle frente a intervalo fijo 100ms.
- CA-04: Sesion larga 1h no muestra crecimiento sostenido de GDI handles.
- CA-05: Smoke UI documentado pasa.

### Tests

- `GameLoopCoordinatorStartStopTests`.
- `AdaptiveCaptureIntervalTests`.
- `BitmapDisposalAudit` o smoke monitorizado.
- Tests de coordinadores extraidos.
- Smoke manual/automatizado con fixtures.

### Tareas

- [ ] Activar flag en entorno controlado.
- [ ] Extraer start/stop con token.
- [ ] Implementar intervalos adaptativos.
- [ ] Auditar `IDisposable`.
- [ ] Crear coordinadores fuera de `FrmMain`.
- [ ] Documentar/automatizar smoke UI.
- [ ] Retirar ruta legacy cuando haya paridad.

---

## SPEC-IMP-07 - Modularizacion del motor postflop y contratos publicos

### Trazabilidad

Mejoras: 23, 24, 28, 29, 30, 31.

### Objetivo

Dividir servicios grandes sin alterar comportamiento, estabilizar contratos y centralizar parametros.

### Requisitos funcionales

- RF-01: `PostflopDecisionService` debe dividirse por estrategias/rutas manteniendo mismo resultado.
- RF-02: Debe existir un entrypoint canonico de decision.
- RF-03: Tipos nested publicos deben moverse a DTOs/ValueObjects cuando formen parte de contratos.
- RF-04: Constantes/thresholds deben clasificarse como constante tecnica, parametro estrategico o config operativa.
- RF-05: Acceso DB debe ser async y no bloquear UI.
- RF-06: `catch (Exception)` generico debe reducirse en paths criticos y preservar contexto/inner exception.

### Diseno

- Estrategias candidatas:
  - `FacingBetStrategy`
  - `NoBetStrategy`
  - `CBetStrategy`
  - `ProbeBetStrategy`
  - `CheckRaiseStrategy`
  - `PotControlStrategy`
  - `BluffCatchStrategy`
- Entrypoint recomendado: `PokerDecisionFacade` como orquestador; `UnifiedPokerCalculator` queda interno o adaptador.
- Crear `DecisionParameterCatalog`.

### Criterios de aceptacion

- CA-01: Matriz de decisiones existente pasa sin cambios no esperados.
- CA-02: Snapshot de decision antes/despues coincide para corpus base.
- CA-03: Consumidores usan un entrypoint canonico.
- CA-04: DTOs publicos no dependen de nested types.
- CA-05: No hay catches genericos sin log/contexto en paths modificados.

### Tests

- Golden master/snapshot de decisiones.
- Contract tests DTO.
- Tests por estrategia extraida.
- Analyzer/grep para references del entrypoint antiguo.

### Tareas

- [ ] Crear corpus golden master.
- [ ] Definir entrypoint canonico.
- [ ] Extraer estrategias una por una.
- [ ] Mover nested DTOs.
- [ ] Crear catalogo de parametros.
- [ ] Convertir DB sync a async donde aplique.
- [ ] Reducir catches genericos.

---

## SPEC-IMP-08 - Performance, observabilidad y benchmarks

### Trazabilidad

Mejoras: 33, 34, 37, 42, 60.

### Objetivo

Evitar degradacion invisible en historiales, OCR y motor numerico.

### Requisitos funcionales

- RF-01: Consultas de historial/bankroll no deben hacer N+1 por sesion.
- RF-02: Cache OCR debe basarse en contenido o invalidacion de captura, no identidad de objeto.
- RF-03: Telemetria externa debe ser opcional y local por defecto.
- RF-04: Benchmarks minimos deben cubrir MC, evaluador, OCR preprocessing y decision service.
- RF-05: Evaluador de manos debe tener property/mutation tests para invariantes criticas.

### Diseno

- Reemplazar N+1 por:
  - batch query `SessionId IN (...)`,
  - proyecciones Marten,
  - o agregados materializados.
- OCR cache:
  - `dHash`, `xxHash` o fingerprint de crop.
- Telemetria:
  - JSON/CSV por sesion.
  - OpenTelemetry solo opt-in, sin red por defecto.

### Criterios de aceptacion

- CA-01: Historial con 10K manos no ejecuta consulta por sesion.
- CA-02: Misma imagen en distinta instancia produce cache hit.
- CA-03: Distinto contenido no usa resultado stale.
- CA-04: BenchmarkSuite tiene baseline y umbrales documentados.
- CA-05: Export telemetria no envia red por defecto.
- CA-06: Invariantes de ranking, wheel straight, flush y kicker ordering sobreviven tests generativos.

### Tests

- Benchmark 1K/10K manos.
- `OcrCacheFingerprintTests`.
- Tests de export local.
- BenchmarkDotNet para hotspots.
- Property tests con FsCheck o generador propio para evaluador de manos.

### Tareas

- [ ] Medir queries actuales.
- [ ] Implementar batch/proyeccion.
- [ ] Cambiar clave cache OCR.
- [ ] Crear export telemetria local.
- [ ] Agregar benchmarks y baseline.
- [ ] Agregar property/mutation tests de evaluador.

---

## SPEC-IMP-09 - UX, coaching e historial explicativo

### Trazabilidad

Mejoras: 15, 16, 45, 47.

### Objetivo

Convertir decisiones/historial en feedback util sin permitir edicion inconsistente de manos.

### Requisitos funcionales

- RF-01: Historial de manos debe ser read-only.
- RF-02: Si se implementa correccion futura, debe ser flujo auditado aparte.
- RF-03: Vista coaching debe comparar `RecommendedAction` vs `ActionTaken`.
- RF-04: Coaching debe filtrar por street, situacion, posicion y perfil villano.
- RF-05: Debe estimar EV perdido cuando exista dato suficiente.
- RF-06: Vista por mano debe explicar equity, pot odds, textura, perfil, thresholds, branch y accion final.
- RF-07: Wizard de calibracion/tablemap debe guiar seleccion de sala, captura, validacion de regiones, prueba OCR y guardado.

### Diseno

- Reutilizar `DecisionTrace` de SPEC-IMP-03.
- Crear `CoachingDivergenceReport`.
- Crear `CalibrationWizardStateMachine`.

### Criterios de aceptacion

- CA-01: Usuario no puede editar mano desde historial normal.
- CA-02: Dataset con divergencias conocidas produce reporte correcto.
- CA-03: Mano historica muestra "por que" con trace asociado.
- CA-04: Wizard genera tablemap usable con OCR smoke.

### Tests

- `HistoryReadOnlyTests`.
- `CoachingDivergenceReportTests`.
- Snapshot de vista explicativa.
- Smoke wizard con fixture.

### Tareas

- [ ] Bloquear edicion de historial.
- [ ] Crear reporte divergencias.
- [ ] Conectar `DecisionTrace` a historial.
- [ ] Disenar wizard por estados.
- [ ] Agregar filtros de coaching.

---

## SPEC-IMP-10 - CI, dependencias, build y packaging

### Trazabilidad

Mejoras: 53, 54, 55, 62, 63, 64, 65, 66.

### Objetivo

Hacer verificaciones reproducibles y publicar binario Windows sin pisar config del usuario.

### Requisitos funcionales

- RF-01: CI debe ejecutar build Debug, build Release, tests, format verify y coverage.
- RF-02: Coverage debe generar reporte visible.
- RF-03: Versiones NuGet deben centralizarse en `Directory.Packages.props`.
- RF-04: Vulnerabilidades `NU1902` deben resolverse o tener waiver fechado.
- RF-05: `Microsoft.NET.Test.Sdk` debe pasar a version estable si disponible.
- RF-06: Plataformas soportadas deben reflejar runtime real, probablemente `x64/win-x64`.
- RF-07: Publish/update no debe sobrescribir config local del usuario.
- RF-08: Instalador/publicacion debe incluir prerequisitos OCR, carpeta app data y checksum.

### Diseno

- Pipeline:
  - `dotnet build OpenScrape.sln`
  - `dotnet build OpenScrape.sln --configuration Release`
  - `dotnet format --verify-no-changes OpenScrape.sln`
  - `dotnet test OpenScrape.sln --collect:"XPlat Code Coverage"`
- Packaging:
  - template config separado de config usuario.
  - app data para recursos mutables.

### Criterios de aceptacion

- CA-01: PR/push falla si build/test/format falla.
- CA-02: Reporte coverage queda como artifact.
- CA-03: `dotnet restore` usa versiones centralizadas.
- CA-04: `dotnet list package --vulnerable` limpio o waiver documentado.
- CA-05: Publicacion win-x64 arranca en maquina limpia.
- CA-06: Update conserva config usuario.

### Tests

- Ejecucion local de `scripts/verify-pre-merge.ps1 -SkipSmoke`.
- CI en rama.
- Smoke maquina limpia.

### Tareas

- [ ] Crear pipeline CI.
- [ ] Integrar coverage/report.
- [ ] Migrar a `Directory.Packages.props`.
- [ ] Revisar vulnerabilidades.
- [ ] Estabilizar Test SDK.
- [ ] Limitar plataformas reales.
- [ ] Ajustar copy config.
- [ ] Crear flujo publish/instalador.

---

## SPEC-IMP-11 - Limpieza arquitectural y vestigios

### Trazabilidad

Mejoras: 25, 27, 32.

### Objetivo

Quitar acoplamientos y restos de refactor que confunden la arquitectura.

### Requisitos funcionales

- RF-01: `OpenScrape.DecisionMaker` no debe depender de Marten directamente para bankroll.
- RF-02: `BankrollTrackerService` debe consumir repositorio/interfaz testeable.
- RF-03: `HandEvaluator` legacy debe eliminarse o moverse a benchmark/reference si no tiene consumidores reales.
- RF-04: Proyectos/archivos vestigio (`OpenScrape.Application`, `OpenScrape.Core`, `MainPage.xaml*`, `obj/` versionados) deben eliminarse si no estan referenciados.

### Diseno

- Crear `IBankrollRepository`.
- Implementacion en Infrastructure/App.
- `BitHandEvaluator` queda evaluador canonico.
- Limpieza solo tras grep/build.

### Criterios de aceptacion

- CA-01: `OpenScrape.DecisionMaker.csproj` no referencia Marten si la extraccion se completa.
- CA-02: Tests de bankroll usan repositorio fake.
- CA-03: Evaluacion de manos sigue pasando con `BitHandEvaluator`.
- CA-04: Build solution pasa tras borrar vestigios.

### Tests

- `BankrollTrackerServiceTests` con fake.
- Tests existentes de evaluador de manos.
- `dotnet build OpenScrape.sln`.
- `rg` sin referencias a vestigios borrados.

### Tareas

- [ ] Buscar referencias reales.
- [ ] Introducir repositorio bankroll.
- [ ] Mover implementacion Marten fuera de DecisionMaker.
- [ ] Eliminar/mover `HandEvaluator`.
- [ ] Borrar vestigios no referenciados.
- [ ] Ejecutar build completo.

---

## SPEC-IMP-12 - Licencias y backend futuro

### Trazabilidad

Mejoras: 49, 50.

### Objetivo

Mantener licencias/backend fuera del core actual, pero dejar limites claros para implementacion futura.

### Requisitos funcionales

- RF-01: Core debe funcionar sin licencia mientras feature flag este desactivado.
- RF-02: Cualquier licencia futura debe aislarse detras de interfaz.
- RF-03: Backend futuro no debe compartir connection string directo con cliente.
- RF-04: API futura debe exponer contratos versionados y testeables.

### Diseno

- Feature flag `LicensingEnabled`.
- Interfaces:
  - `ILicenseVerifier`
  - `ILicenseStateStore`
  - `IRemoteTelemetryClient`
- Backend futuro con OpenAPI y tests de contrato.

### Criterios de aceptacion

- CA-01: Con `LicensingEnabled=false`, app arranca y opera igual.
- CA-02: Con `LicensingEnabled=true` y licencia invalida, feature bloqueada de forma explicita.
- CA-03: No hay connection string remoto en cliente.

### Tests

- `LicensingFeatureFlagTests`.
- Contract tests futuros OpenAPI.

### Tareas

- [ ] No mezclar licencias con refactors actuales.
- [ ] Definir interfaces si aparece implementacion.
- [ ] Mantener flag apagado por defecto.

---

## Matriz de trazabilidad mejora -> spec

| Mejora | Spec |
|--------|------|
| 1-4, 7, 48, 57, 67-70 | SPEC-IMP-01 |
| 8-9, 19-20, 26, 58 | SPEC-IMP-02 |
| 5-6, 10-11, 14, 17-18, 38, 59 | SPEC-IMP-03 |
| 12-13, 51-52 | SPEC-IMP-04 |
| 35-36, 43-44, 46 | SPEC-IMP-05 |
| 21-22, 39-41, 56, 61 | SPEC-IMP-06 |
| 23-24, 28-31 | SPEC-IMP-07 |
| 33-34, 37, 42, 60 | SPEC-IMP-08 |
| 15-16, 45, 47 | SPEC-IMP-09 |
| 53-55, 62-66 | SPEC-IMP-10 |
| 25, 27, 32 | SPEC-IMP-11 |
| 49-50 | SPEC-IMP-12 |

---

## Definition of Done global

- [ ] Spec afectada marcada como implementada en changelog/PR.
- [ ] Tests automatizados nuevos o actualizados.
- [ ] `dotnet build OpenScrape.sln` pasa.
- [ ] `dotnet test OpenScrape.sln` pasa o se documenta bloqueo externo.
- [ ] `dotnet format --verify-no-changes OpenScrape.sln` pasa.
- [ ] No se introducen secretos reales.
- [ ] No se reducen garantias de privacidad.
- [ ] Se actualiza documentacion Reversa si cambia contrato funcional.
