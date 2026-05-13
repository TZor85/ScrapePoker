# Mejoras posibles - ScrapePoker

> Generado el 2026-05-12.
> Fuente: analisis Reversa ya cerrado, lectura de specs en `_reversa_sdd/` y contraste puntual con codigo.
> Alcance: oportunidades de mejora tecnicas, funcionales, operativas y de producto. No cambia codigo legado.

---

## Resumen ejecutivo

El proyecto tiene una base funcional fuerte: arquitectura por capas, motor de decision cubierto por muchos tests, telemetria interna, ADRs y specs Reversa completas. Las mejoras principales no son "reescribir todo"; son cerrar decisiones ya detectadas, reducir riesgo operativo y terminar refactors iniciados.

Prioridad recomendada:

1. **P0 - Seguridad/configuracion y bugs de decision**: `IsDevelopment=true`, secrets, guardias de input, `AutoCalibrationService.OldValue`.
2. **P1 - Correctitud poker/metricas**: `BetSize` en lookup, BigBlind real, persistencia de `OpponentProfile`, auto-rebuy y random determinista.
3. **P2 - Arquitectura/mantenibilidad**: cortar `FrmMain`, cortar `PostflopDecisionService`, eliminar placeholders y legacy duplicado.
4. **P3 - Operacion/UX**: carpetas configurables, recursos Tesseract, shutdown robusto, CI, packaging.
5. **P4 - Producto**: coaching real, comparativa humano vs recomendacion, modo replay/backtest, disclaimers y licencias diferidas.

---

## P0 - Criticas

### 1. Cambiar `IsDevelopment=true` hardcoded

- **Evidencia:** `src/OpenScrape.App/Program.cs:52` llama `services.AddDataBase(context.Configuration, true)`.
- **Impacto:** `AutoCreate.All` queda activo siempre en `src/OpenScrape.Infrastructure/Services.cs:36-38`; riesgo en una BD real.
- **Mejora:** pasar `context.HostingEnvironment.IsDevelopment()` o configurar politica `Database:AutoCreate`.
- **Validacion:** test de arranque/config + test unitario de opciones Marten si se abstrae el builder.
- **Esfuerzo:** bajo.

### 2. Validar connection string con error accionable

- **Evidencia:** `Services.cs:16` usa `GetConnectionString("DefaultConnection")!`.
- **Impacto:** si falta config, fallo tardio o poco claro.
- **Mejora:** guard clause y `InvalidOperationException("Connection string 'DefaultConnection' no encontrada.")`.
- **Validacion:** test de extension `AddDataBase` con config vacia.
- **Esfuerzo:** bajo.

### 3. Separar secrets de config distribuible

- **Evidencia:** `appsettings.json` se copia siempre (`CopyToOutputDirectory=Always`) y Reversa detecto credenciales reales.
- **Decision actual:** usuario pidio mantener compatibilidad exacta en reconstruccion, pero sigue siendo deuda de seguridad.
- **Mejora:** `appsettings.json` con placeholders, `appsettings.Development.json` local, User Secrets/DPAPI para instalaciones.
- **Validacion:** build no contiene claves reales; smoke test con config local.
- **Esfuerzo:** medio, por rotacion/compatibilidad.

### 4. DPAPI para licencia/config sensible local

- **Evidencia:** respuesta humana Q-PERM-RBAC-02: la key recordada debe cifrarse con DPAPI.
- **Impacto:** evita texto plano en `Properties/Settings`.
- **Mejora:** wrapper `ISecretProtector` con `ProtectedData.Protect/Unprotect`.
- **Validacion:** roundtrip local + fallback si clave corrupta.
- **Esfuerzo:** bajo/medio.

### 5. Guardias de input en `PostflopDecisionService`

- **Evidencia:** gaps: equity NaN/<0/>100, pot invalido, stack negativo pueden emitir decision absurda.
- **Impacto:** bug silencioso en spots raros de OCR/MC.
- **Mejora:** validar `PostflopDecisionInput` al inicio: equity 0..100, pot >= 0, stack >= 0, pot odds finitos, street coherente.
- **Decision pendiente tecnica:** lanzar `ArgumentException` vs devolver accion segura + log. Recomiendo fail-soft en runtime y fail-fast en tests.
- **Validacion:** tests con NaN, negativos, >100, pot 0.
- **Esfuerzo:** medio.

### 6. Corregir `AutoCalibrationService.OldValue`

- **Evidencia:** `AutoCalibrationService.cs:177,185,193,201` hardcodea 45/40.
- **Impacto:** UI puede mostrar ajustes falsos y erosionar confianza.
- **Mejora:** leer valores actuales desde `StrategyProfile` o un accessor tipado por parametro.
- **Validacion:** test donde profile activo no sea 45/40 y ajuste preserve `OldValue` real.
- **Esfuerzo:** bajo/medio.

### 7. Revisar `EncrypterHelper` con IV fija

- **Evidencia:** `EncrypterHelper.cs:122,134` usa IV de 16 ceros.
- **Decision actual:** mantener compatibilidad exacta.
- **Mejora segura:** soporte dual: leer formato viejo, escribir formato nuevo versionado con IV aleatoria + autenticacion.
- **Validacion:** descifrado legacy + cifrado nuevo + corrupcion detectada.
- **Esfuerzo:** medio.

---

## P1 - Correctitud funcional

### 8. Rehabilitar `BetSize` en lookup preflop

- **Evidencia:** `GetActionScenario.cs:31` comenta `request.BetSize`; JSON contiene `BetSize`.
- **Decision humana:** volver al lookup.
- **Impacto:** hoy puede elegir rangos incorrectos en spots donde el size distingue estrategia.
- **Mejora:** reactivar filtro, normalizar decimals y cubrir fallback cuando `BetSize` sea null.
- **Validacion:** test con dos secuencias iguales salvo `BetSize`.
- **Esfuerzo:** bajo.

### 9. Sustituir `new Random()` por RNG inyectable

- **Evidencia:** `GetActionScenario.GetRandomAction` crea `new Random()`, `ObtainActionHelper` tambien.
- **Impacto:** mezcla no reproducible y riesgo de correlacion si se llama rapido.
- **Mejora:** `IRandomProvider` o `Random.Shared` detras de abstraccion.
- **Validacion:** tests deterministas de seleccion ponderada.
- **Esfuerzo:** bajo.

### 10. Sustituir `Random.Shared` directo en DecisionMaker

- **Evidencia:** `PostflopDecisionService` y `MonteCarloSimulator` usan `Random.Shared`.
- **Impacto:** tests estadisticos/flaky; imposible reproducir una decision exacta.
- **Mejora:** inyectar RNG/seed por sesion; registrar seed en `HandRecord` si decision usa random.
- **Validacion:** replay determinista de mano.
- **Esfuerzo:** medio.

### 11. BigBlind real en `ExploitabilityCalculator`

- **Evidencia:** `ExploitabilityCalculator.cs:93` fija `BigBlind = 1.0`.
- **Decision humana:** tomar BigBlind desde sesion actual.
- **Impacto:** mbb/BB100 y exploitability mal escaladas.
- **Mejora:** parametro por decision o contexto de sesion inyectado, no constante global.
- **Validacion:** mismo spot con BB distinta cambia escala correctamente.
- **Esfuerzo:** bajo/medio.

### 12. Persistir `OpponentProfile`

- **Evidencia:** `OpponentTracker` vive en `ConcurrentDictionary`; decision humana: GUID por alias OCR.
- **Impacto:** el bot pierde aprendizaje al cerrar app.
- **Mejora:** entidad `OpponentAlias { Alias, OpponentId, Room/TableMap?, FirstSeen, LastSeen }` + repositorio.
- **Privacidad:** alias exportado debe anonimizarse como `player1`, `player2`, ...
- **Validacion:** perfil sobrevive reinicio y mergea alias conocido al mismo GUID.
- **Esfuerzo:** medio/alto.

### 13. Persistir/gestionar `ExploitabilityCalculator` records

- **Evidencia:** cola en memoria cap 10K.
- **Impacto:** analisis TopLeaks se pierde entre sesiones.
- **Mejora:** persistir resumen por sesion o `DecisionRecord` agregado, no necesariamente cada decision cruda.
- **Validacion:** dashboard conserva historial tras reinicio.
- **Esfuerzo:** medio.

### 14. Formalizar regla de `AutoRebuy`

- **Evidencia:** respuesta humana: `AutoRebuy = 100 BB - currentStack`.
- **Impacto:** profit/bankroll depende de esta formula.
- **Mejora:** mover formula a dominio, cubrir top-up manual y rebuy parcial.
- **Validacion:** tests con stack 48BB, 60BB, 100BB, top-up manual.
- **Esfuerzo:** bajo/medio.

### 15. Historial no editable como regla explicita

- **Evidencia:** respuesta humana Q-DOM-05.
- **Impacto:** evita UI ambigua y datos inconsistentes.
- **Mejora:** documentar/asegurar controles read-only; si se requiere correccion futura, crear flujo auditado aparte.
- **Validacion:** tests/UI smoke si aplica.
- **Esfuerzo:** bajo.

### 16. Coaching: divergencia humano vs recomendacion

- **Evidencia:** respuesta humana Q-DOM-07: deberia existir.
- **Impacto:** convierte logs en feedback accionable.
- **Mejora:** reporte de desviaciones `RecommendedAction` vs `ActionTaken`, filtro por street/situacion, EV perdido estimado.
- **Validacion:** dataset historico con desviaciones conocidas.
- **Esfuerzo:** medio.

### 17. Watchdog de manos atascadas

- **Evidencia:** respuesta humana Q-FSM-01: no existe y deberia implementarse.
- **Impacto:** evita estado colgado si OCR/ventana falla.
- **Mejora:** timeout por estado en `GameLoopStateMachine` + reset controlado + log.
- **Validacion:** test FSM con tiempo simulado.
- **Esfuerzo:** medio.

### 18. All-in per-player

- **Evidencia:** solo existe `IsAnyoneAllIn` global en contexto; pregunta Reversa marco caso por jugador.
- **Impacto:** decisiones multiway pueden perder informacion de stacks comprometidos.
- **Mejora:** estado all-in por asiento/jugador en `PostflopGameContext`.
- **Validacion:** multiway con un villano all-in y otro con stack.
- **Esfuerzo:** medio.

### 19. Corregir `UpdateRegionTableMap` flags inertes

- **Evidencia:** request trae flags, pero creacion inicial puede dejarlos null/preservar previos.
- **Impacto:** calibracion OCR puede guardar mapa incompleto.
- **Mejora:** distinguir update parcial vs create; en create usar flags del request.
- **Validacion:** test create/update de region con `IsHash/IsColor/IsBoard/IsOnlyNumber`.
- **Esfuerzo:** bajo.

### 20. Mejorar excepciones en `GetActionScenario`

- **Evidencia:** catch envuelve `Exception` base y pierde inner exception/stack.
- **Impacto:** diagnostico pobre de tablas preflop.
- **Mejora:** `throw new InvalidOperationException(..., ex)` o result/error tipado.
- **Validacion:** test confirma `InnerException`.
- **Esfuerzo:** bajo.

---

## P2 - Arquitectura y mantenibilidad

### 21. Cortar `FrmMain` por responsabilidades

- **Evidencia:** archivo mayor: `FrmMain.cs` ~3866 lineas en lectura actual; Reversa lo clasifica como god class.
- **Impacto:** cambios UI/game loop/OCR/persistencia se pisan.
- **Mejora:** separar coordinadores: captura, hand lifecycle, tabs, historial, overlay, calibration UI.
- **Camino seguro:** activar feature flag `UseGameLoopCoordinator`, migrar una ruta, medir paridad.
- **Validacion:** smoke + tests de coordinadores extraidos.
- **Esfuerzo:** alto.

### 22. Terminar cutover a `GameLoopCoordinator`

- **Evidencia:** `FeatureFlags.UseGameLoopCoordinator=false`; `BackgroundWorker1_DoWork` sigue llamando `btnCapture_Click`.
- **Impacto:** doble ruta conceptual.
- **Mejora:** hacer coordinator ruta primaria; dejar BackgroundWorker como legacy temporal o eliminarlo.
- **Validacion:** screenshot/smoke manual, logs de ciclo, pruebas de stop/start.
- **Esfuerzo:** medio/alto.

### 23. Cortar `PostflopDecisionService`

- **Evidencia:** ~1705 lineas; 10+ paths y random mezclado.
- **Impacto:** alto riesgo al tocar una rama.
- **Mejora:** estrategias: `FacingBetStrategy`, `NoBetStrategy`, `CBetStrategy`, `ProbeBetStrategy`, `CheckRaiseStrategy`, `PotControlStrategy`.
- **Validacion:** matriz de decisiones existente + tests de snapshot por estrategia.
- **Esfuerzo:** alto.

### 24. Consolidar entrypoint de motor

- **Evidencia:** conviven `UnifiedPokerCalculator` y `PokerDecisionFacade`.
- **Impacto:** dos formas de decidir/evaluar; confusion en futuras migraciones.
- **Mejora:** elegir una ruta canonica. Recomendacion: `PokerDecisionFacade` como orquestador; `UnifiedPokerCalculator` queda como servicio interno o se retira.
- **Validacion:** tests de paridad antes/despues.
- **Esfuerzo:** medio.

### 25. Mover Marten fuera de `DecisionMaker`

- **Evidencia:** `BankrollTrackerService` inyecta `IDocumentStore` en `OpenScrape.DecisionMaker`.
- **Impacto:** rompe pureza de capa; dificulta tests.
- **Mejora:** interfaz repositorio en dominio/app, implementacion en Infrastructure/App.
- **Validacion:** tests de servicio con repositorio fake.
- **Esfuerzo:** medio.

### 26. Eliminar use cases muertos

- **Evidencia:** `GetAllTables`, `GetAllRegionTableMap`, `GetFlopCards`; decision humana: se eliminan.
- **Impacto:** limpia DI y evita uso accidental.
- **Mejora:** borrar clases/registro/aggregators si no usados.
- **Validacion:** build + grep referencias.
- **Esfuerzo:** bajo.

### 27. Eliminar legacy `HandEvaluator` duplicado

- **Evidencia:** coexiste con `BitHandEvaluator`.
- **Impacto:** ambiguedad y mantenimiento doble.
- **Mejora:** verificar consumidores; borrar o mover a benchmark/reference.
- **Validacion:** tests de evaluacion siguen verdes con `BitHandEvaluator`.
- **Esfuerzo:** bajo.

### 28. Mover tipos nested/retornos a DTOs propios

- **Evidencia:** interfaces acopladas a tipos nested (`EquityResult`, `OutsResult`, `FullEquityAnalysis`).
- **Impacto:** cambios internos rompen contratos.
- **Mejora:** records publicos en `DTOs/` o `ValueObjects/`.
- **Validacion:** API contract tests.
- **Esfuerzo:** bajo/medio.

### 29. Centralizar constantes y thresholds

- **Evidencia:** docs mencionan 80+ hardcodes; `PokerConstants`, `StrategyProfile`, servicios y JSON mezclan valores.
- **Impacto:** calibracion dificil.
- **Mejora:** catalogo tipado por dominio: config, constante tecnica, parametro estrategico.
- **Validacion:** startup validation exhaustiva.
- **Esfuerzo:** medio.

### 30. Normalizar convenciones async/session

- **Evidencia:** mezcla `using`/`await using`; servicios sync con Marten (`BankrollTrackerService`) y async en otros.
- **Impacto:** bloqueo UI y patrones inconsistentes.
- **Mejora:** APIs async para DB; `ConfigureAwait(false)` en librerias si aplica.
- **Validacion:** tests async y UI no bloqueada.
- **Esfuerzo:** medio.

### 31. Reducir `catch (Exception)` genericos

- **Evidencia:** busqueda muestra muchos catches amplios en UI/services.
- **Impacto:** oculta bugs, hace dificil distinguir retry vs fallo fatal.
- **Mejora:** capturar excepciones esperadas, preservar `InnerException`, registrar contexto estructurado.
- **Validacion:** tests de error paths criticos.
- **Esfuerzo:** incremental.

### 32. Limpiar proyectos/archivos vestigio

- **Evidencia:** `OpenScrape.Application`, `OpenScrape.Core`, `MainPage.xaml*`, `obj/` versionados.
- **Impacto:** ruido, falsas rutas de arquitectura.
- **Mejora:** borrar vestigios tras confirmar no referenciados.
- **Validacion:** `dotnet build OpenScrape.sln`, grep referencias.
- **Esfuerzo:** bajo.

---

## P3 - Performance, observabilidad y datos

### 33. Resolver N+1 queries

- **Evidencia:** `GameLoggerService.GetRecentSessionsWithStatsAsync` consulta manos por sesion; `BankrollTrackerService` idem.
- **Impacto:** historial/bankroll degradan con muchas sesiones.
- **Mejora:** batch query por `SessionId IN (...)`, agregados materializados o proyecciones Marten.
- **Validacion:** benchmark con 1K/10K manos.
- **Esfuerzo:** medio.

### 34. Cache OCR por contenido, no identidad

- **Evidencia Reversa:** `OcrService.GetCroppedBitmap` usa `image.GetHashCode()` como parte de cache.
- **Impacto:** stale/miss segun identidad de bitmap.
- **Mejora:** dHash/xxHash de crop o invalidacion por captura.
- **Validacion:** mismo contenido/distinta instancia y distinto contenido/misma ruta.
- **Esfuerzo:** medio.

### 35. Manejar Tesseract corrupto/faltante

- **Evidencia:** specs marcan crash UX con `eng.traineddata`.
- **Impacto:** arranque fragile.
- **Mejora:** validar recurso al inicio, autoextraer, mensaje claro, reparacion.
- **Validacion:** test manual quitando/corrompiendo tessdata.
- **Esfuerzo:** bajo/medio.

### 36. Eliminar duplicado de `eng.traineddata`

- **Evidencia:** csproj embebe dos rutas.
- **Impacto:** binario/output mas pesado y packaging confuso.
- **Mejora:** una fuente canonica; preferible recurso embebido con extraccion a carpeta app data.
- **Validacion:** publish limpio + OCR smoke.
- **Esfuerzo:** bajo.

### 37. Telemetria externa opcional

- **Evidencia:** hay `MetricsCollector`, pero no export APM/OpenTelemetry.
- **Impacto:** dificil diagnosticar fuera de UI/log local.
- **Mejora:** export opcional JSON/CSV/OpenTelemetry local, sin red por defecto.
- **Validacion:** archivo de metricas por sesion.
- **Esfuerzo:** medio.

### 38. Mejorar logs de decision

- **Evidencia:** decision pipeline complejo, logs repartidos.
- **Impacto:** dificil explicar por que una accion fue recomendada.
- **Mejora:** `DecisionTrace` estructurado: inputs, thresholds, ajustes, branch, random seed, accion final.
- **Validacion:** snapshot de trace por caso.
- **Esfuerzo:** medio.

### 39. Sampling/adaptive loop

- **Evidencia:** capture interval fijo 100ms.
- **Impacto:** CPU innecesario cuando no hay accion; latencia no adaptativa.
- **Mejora:** intervalos por estado: idle lento, action-detection rapido, cooldown tras captura.
- **Validacion:** metrica CPU/latencia antes/despues.
- **Esfuerzo:** medio.

### 40. CancellationToken y cierre robusto

- **Evidencia:** `BackgroundWorker1_DoWork` no usa token; `GameLoopCoordinator` ya modela stop async.
- **Impacto:** cierre fragil.
- **Mejora:** unificar stop/start con token, timeout y disposal determinista.
- **Validacion:** tests de stop + cierre app manual.
- **Esfuerzo:** medio.

### 41. Control de memoria de bitmaps

- **Evidencia:** pipeline crea/corta bitmaps intensivamente.
- **Impacto:** leaks/GDI handles si un path no dispone.
- **Mejora:** auditoria `IDisposable`, pooling donde aporte, contador de GDI handles en debug.
- **Validacion:** sesion larga 1h sin crecimiento sostenido.
- **Esfuerzo:** medio.

### 42. Benchmarks de hotspots

- **Evidencia:** existe BenchmarkSuite, pero no ligado a pre-merge.
- **Impacto:** cambios MC/OCR pueden degradar sin aviso.
- **Mejora:** benchmark minimo para `MonteCarloSimulator`, `BitHandEvaluator`, OCR preprocessing, decision service.
- **Validacion:** baseline y umbrales.
- **Esfuerzo:** medio.

---

## P4 - UX, producto y operacion

### 43. Carpeta de recursos configurable

- **Evidencia:** `FormImage`, `LoadTableMapUseCase`, `SaveTableMapUseCase` tienen rutas `C:\Code\...`.
- **Decision humana:** configurable por usuario.
- **Mejora:** `ResourcePathsOptions` + dialog inicial + persistencia en user settings.
- **Validacion:** mover repo/binario a otra ruta y cargar mapas.
- **Esfuerzo:** bajo/medio.

### 44. Filtro de ventana configurable

- **Evidencia:** `FormListApps.cs:32` filtra `"NL H"`.
- **Impacto:** salas/titulos distintos no aparecen.
- **Mejora:** patron configurable por usuario, lista blanca/regex simple, modo mostrar todas.
- **Validacion:** ventanas dummy con titulos variados.
- **Esfuerzo:** bajo.

### 45. Wizard de calibracion/tablemap

- **Evidencia:** usuario confirma soporte por tablemap, no por sala fija.
- **Impacto:** onboarding depende de configuracion manual.
- **Mejora:** flujo guiado: seleccionar sala, capturar referencia, validar regiones, probar OCR, guardar perfil.
- **Validacion:** smoke con mapa nuevo.
- **Esfuerzo:** alto.

### 46. Diagnostico visual de OCR/vision

- **Evidencia:** `FrmDetectionDebug` tiene TODOs de guardado.
- **Impacto:** debug depende de logs y capturas manuales.
- **Mejora:** guardar snapshot de crop, texto OCR, confidence, region id, decision del consenso.
- **Validacion:** reproduccion de fallo OCR desde artifact.
- **Esfuerzo:** medio.

### 47. Historial explicativo

- **Evidencia:** historial existe, pero no edicion; coaching pendiente.
- **Mejora:** vista por mano con "por que": equity, pot odds, textura, perfil villano, thresholds, branch.
- **Validacion:** cada `StreetDecision` muestra trace asociado.
- **Esfuerzo:** medio/alto.

### 48. Disclaimer/TOS

- **Evidencia:** respuesta Q-PERM-TOS-01: no existe y habria que implementarlo.
- **Impacto:** riesgo de uso indebido.
- **Mejora:** disclaimer de uso educativo, responsabilidad del usuario, zona gris de TOS.
- **Validacion:** aparece en primer arranque y queda aceptacion local.
- **Esfuerzo:** bajo.

### 49. Licencias diferidas, pero aisladas

- **Evidencia:** spec abierta `login-sistema-licencias`; respuesta: queda diferida.
- **Mejora:** no mezclar con core ahora; dejar interfaces/feature flag si se implementa.
- **Validacion:** app funciona sin licencia mientras feature desactivada.
- **Esfuerzo:** futuro.

### 50. Backend intermedio futuro

- **Evidencia:** respuesta humana: posible mejora, no actual.
- **Impacto:** necesario si multiusuario o distribucion real.
- **Mejora:** API para licencias/telemetria, nunca connection string compartida.
- **Validacion:** contrato OpenAPI y tests de integracion.
- **Esfuerzo:** alto.

### 51. Aislamiento multiusuario

- **Evidencia:** respuesta Q-PERM-DATA-01: cada usuario solo debe ver sus sesiones.
- **Mejora:** `OwnerLicenseKey`/`OwnerId` en sesiones y filtros obligatorios.
- **Validacion:** tests de query no cruzada.
- **Esfuerzo:** medio/alto.

### 52. Export anonimizado

- **Evidencia:** respuesta Q-PERM-DATA-02.
- **Mejora:** exportador que remapea alias a `player1..N`, conserva posiciones/acciones.
- **Validacion:** no aparece alias OCR real en export.
- **Esfuerzo:** bajo/medio.

### 53. Instalador/publicacion

- **Evidencia:** distribucion actual binario WinForms sin instalador.
- **Mejora:** publish win-x64, carpeta app data, migracion config, prerequisitos OCR incluidos, checksum.
- **Validacion:** instalar en maquina limpia.
- **Esfuerzo:** medio.

---

## Calidad, tests y CI

### 54. CI real

- **Evidencia:** no hay pipeline CI; hay script `scripts/verify-pre-merge.ps1`.
- **Mejora:** GitHub Actions/otro: build Debug, build Release, test, format verify, publicar cobertura.
- **Validacion:** PR bloqueado por pipeline rojo.
- **Esfuerzo:** bajo/medio.

### 55. Cobertura automatizada visible

- **Evidencia:** coverlet existe, pero no hay reporte integrado.
- **Mejora:** `dotnet test --collect:"XPlat Code Coverage"` + ReportGenerator.
- **Validacion:** HTML/markdown de coverage.
- **Esfuerzo:** bajo.

### 56. Tests de App Services y UI coordinators

- **Evidencia:** tests concentrados en DecisionMaker; `FrmMain`, OCR, table layout y persistence menos cubiertos.
- **Mejora:** extraer logic de UI a servicios testeables; tests con bitmaps fixtures.
- **Validacion:** casos OCR/region/tablemap reproducibles.
- **Esfuerzo:** medio/alto.

### 57. Tests de errores de config

- **Evidencia:** errores críticos config: connection string, thresholds, tessdata, paths.
- **Mejora:** suite `ConfigurationValidationTests`.
- **Validacion:** cada config mala falla con mensaje accionable.
- **Esfuerzo:** bajo/medio.

### 58. Contract tests de JSON preflop

- **Evidencia:** 16 JSON son motor preflop real.
- **Mejora:** validar schema, porcentajes suman 100, `Name` coincide con enum description, `BetSize` coherente.
- **Validacion:** test lee todos los JSON.
- **Esfuerzo:** bajo.

### 59. Tests de reproducibilidad random

- **Evidencia:** random en decisiones y MC.
- **Mejora:** seed inyectada y replay exacto.
- **Validacion:** misma seed => misma accion; distinta seed => distribucion esperada.
- **Esfuerzo:** medio.

### 60. Mutation/property tests para evaluador de manos

- **Evidencia:** motor poker critico.
- **Mejora:** property tests de invariantes: ranking monotonicidad, wheel straight, flush, kicker ordering.
- **Validacion:** FsCheck o generador propio.
- **Esfuerzo:** medio.

### 61. Smoke test UI manual documentado/automatizado

- **Evidencia:** checklist manual existe.
- **Mejora:** smoke mínimo con capturas fixtures: arranca, carga tablemap, procesa mano, muestra overlay.
- **Validacion:** script/manual reproducible.
- **Esfuerzo:** medio.

---

## Dependencias y build

### 62. Centralizar versiones NuGet

- **Evidencia:** versiones repetidas en csproj.
- **Mejora:** `Directory.Packages.props`.
- **Validacion:** restore/build.
- **Esfuerzo:** bajo.

### 63. Resolver `NU1902` en vez de suprimirlo indefinidamente

- **Evidencia:** `NoWarn NU1902` en Features/Infrastructure por OpenTelemetry transitivo de Marten.
- **Mejora:** actualizar Marten/OpenTelemetry o documentar excepcion con fecha de revision.
- **Validacion:** `dotnet list package --vulnerable` limpio o waiver controlado.
- **Esfuerzo:** bajo/medio.

### 64. Revisar paquete `Microsoft.NET.Test.Sdk` preview

- **Evidencia:** tests usan `17.14.0-preview-25107-01`.
- **Mejora:** pasar a version estable si disponible.
- **Validacion:** tests descubiertos igual.
- **Esfuerzo:** bajo.

### 65. Revisar plataformas csproj

- **Evidencia:** App declara `AnyCPU;x64;x86;ARM32;ARM64`, pero depende de WinForms, native OCR/OpenCV y runtime win.
- **Mejora:** limitar plataformas soportadas reales, probablemente `x64`.
- **Validacion:** publish win-x64.
- **Esfuerzo:** bajo.

### 66. Limpiar `CopyToOutputDirectory=Always`

- **Evidencia:** `appsettings.json` siempre copiado.
- **Mejora:** separar template vs local, evitar sobrescribir config de usuario en publish/update.
- **Validacion:** update no pisa config existente.
- **Esfuerzo:** bajo/medio.

---

## Seguridad y privacidad

### 67. Rotacion de credenciales

- **Evidencia:** respuesta humana: hay que hacerlo.
- **Mejora:** rotar Neon password, purgar historial si repo se comparte, cambiar config distribuida.
- **Validacion:** credencial vieja no conecta.
- **Esfuerzo:** medio.

### 68. No loggear datos sensibles

- **Evidencia:** OCR puede capturar alias/stacks.
- **Mejora:** sanitizador de logs/exports; nivel Debug opt-in.
- **Validacion:** logs no contienen alias reales si modo privado activo.
- **Esfuerzo:** bajo/medio.

### 69. Cifrado autenticado

- **Evidencia:** AES-CBC sin autenticacion.
- **Mejora:** AES-GCM o CBC+HMAC versionado.
- **Validacion:** tamper detectado.
- **Esfuerzo:** medio.

### 70. Politica de retencion de manos

- **Evidencia:** se persisten manos/sesiones; aliases potencialmente personales.
- **Mejora:** opcion de borrar sesiones antiguas, export anonimo, purge local/remoto.
- **Validacion:** test de borrado por rango/usuario.
- **Esfuerzo:** medio.

---

## Orden recomendado de ejecucion

### Sprint 1 - Riesgo bajo, impacto alto

1. `IsDevelopment` real.
2. Guard de connection string.
3. `BetSize` vuelve al lookup.
4. Eliminar use cases muertos.
5. BigBlind desde sesion actual.
6. Tests de JSON preflop.

### Sprint 2 - Correctitud DecisionMaker

1. `AutoCalibrationService.OldValue` desde profile.
2. Guardias de input.
3. RNG inyectable para decisiones.
4. Decision trace estructurado.

### Sprint 3 - Persistencia y UX

1. `OpponentProfile` con GUID por alias.
2. Carpeta recursos configurable.
3. Filtro ventana configurable.
4. Diagnostico OCR persistible.

### Sprint 4 - Arquitectura

1. Cutover a `GameLoopCoordinator`.
2. Cortar `FrmMain`.
3. Cortar `PostflopDecisionService`.
4. Repositorio para bankroll/persistencia fuera de DecisionMaker.

### Sprint 5 - Operacion

1. CI real.
2. Coverage report.
3. Packaging win-x64.
4. Limpieza de dependencias/vulnerabilidades.

---

## Referencias locales

- `_reversa_sdd/gaps.md`
- `_reversa_sdd/confidence-report.md`
- `_reversa_sdd/architecture.md`
- `_reversa_sdd/traceability/spec-impact-matrix.md`
- `src/OpenScrape.App/Program.cs`
- `src/OpenScrape.Infrastructure/Services.cs`
- `src/OpenScrape.Features/ActionScenario/Get/GetActionScenario.cs`
- `src/OpenScrape.DecisionMaker/Services/AutoCalibrationService.cs`
- `src/OpenScrape.DecisionMaker/Services/ExploitabilityCalculator.cs`
- `src/OpenScrape.App/OpenScrape.App.csproj`
