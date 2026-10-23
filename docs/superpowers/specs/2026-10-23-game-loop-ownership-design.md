# Game Loop Ownership — Diseño de migración

> Documento de diseño. El plan ejecutable de la primera fase vive en
> `docs/superpowers/plans/2026-10-23-game-loop-ownership-fase-a.md`.

## Problema

La propiedad del pipeline de captura está partida en dos loops que conviven:

| Pieza | Rol actual |
|---|---|
| `DetectionLoopService` | Pollea cada 100–200 ms, detecta turno por color, emite `HeroTurnDetected` |
| `FrmMain.SubscribeDetectionEvents` | Convierte ese evento en `btnCapture_Click` (captura + OCR + decisión + UI + logging) |
| `GameLoopCoordinator` | Loop propio cada 100 ms con watchdog, single-flight y lifecycle limpio. No tiene trabajo real |
| `HandProcessor` | Tick processor que solo cubre preflop. Postflop devuelve `Empty` |
| `FeatureFlags.UseGameLoopCoordinator` | `false`: `IGameLoopTickProcessor` se resuelve a `EmptyGameLoopTickProcessor` |

Consecuencia: el coordinador está terminado pero inerte, y el pipeline real vive
dentro de un formulario de 2500 líneas, sin tests.

Aclaración importante: **hoy no hay un problema de performance activo**. El flag
está en `false`, así que no existe doble captura. La motivación es consolidar el
dueño del pipeline y hacerlo testeable, no apagar un incendio.

## Decisión

Migración strangler en tres fases. Ninguna fase requiere big-bang y ninguna deja
la aplicación sin funcionar.

### Fase A — Extraer el gate de turno (`ITurnGate`)

Sacar el latch `_executeCapture` de `DetectionLoopService` a un componente con
contrato propio y tests deterministas.

- Comportamiento idéntico, cero cambio visible.
- Es prerequisito de B y C: la decisión "es mi turno y no lo procesé todavía"
  tiene que poder consultarse desde fuera del loop de píxeles.

**Salida:** `ITurnGate` registrado en DI, usado por `DetectionLoopService`, con
tests que fijan la semántica del latch.

### Fase B — El coordinador pasa a disparar el pipeline caro

El detector sigue detectando; el coordinador pasa a ser el único ejecutor del
pipeline caro.

- `IGameLoopCoordinator` gana `RequestTick()`: una señal (no un timer) que
  despierta una iteración.
- `DetectionLoopService` deja de invocar el path de captura y llama
  `RequestTick()` cuando el gate abre.
- `FrmMain` registra su pipeline en un `DelegatingTickProcessor` (adaptador
  temporal): `Func<Bitmap, CancellationToken, Task<GameLoopResult>>`.
- El bitmap ya capturado por el detector se pasa al pipeline: una captura por
  turno, no dos.
- `btnCapture_Click` queda solo para el botón manual y el modo test.

**Salida:** un solo dueño del pipeline caro, watchdog y single-flight activos,
sin decisiones duplicadas. `UseGameLoopCoordinator` pasa a `true` por defecto.

**Criterio de abandono:** si con el flag en `true` y el adaptador no hay una
mejora medible (latencia de ciclo y capturas por mano en `IMetricsCollector`),
no se ejecuta C. El adaptador se documenta como deuda y la fase se revierte.

### Fase C — Extraer el pipeline del formulario

Recién acá se toca lo caro: mover `btnCapture_Click`, `ProcessTableInfoAsync`,
`ProcessPreflopAsync` y `ProcessPostFlopAsync` a servicios con contrato.

- Extracción por calle (preflop → flop → turn → river), cada una con paridad
  verificada contra el path vigente.
- `UiUpdateService` absorbe la proyección a UI que hoy hace `btnCapture_Click`.
- `DelegatingTickProcessor` se reemplaza por un tick processor real.
- `DetectionLoopService` se retira: su detección de píxeles se pliega al tick.

**Salida:** `FrmMain` sin lógica de pipeline, `HandProcessor` con cobertura
preflop + postflop, `DetectionLoopService` eliminado.

Cada calle extraída debe producir la misma acción sobre la misma captura. La
paridad se verifica con las capturas reales de `output/samples/grab/`.

## Restricciones no negociables

1. **Un solo pipeline activo.** Nunca dos caminos ejecutando decisión sobre el
   mismo turno: duplica `GameLoggerService` (manos repetidas) y escrituras a la
   base.
2. **Una captura por turno.** El detector ya captura para leer 10 píxeles; el
   pipeline no vuelve a capturar.
3. **Paridad del latch.** Se vuelve a detectar turno solo después de que el
   color de acción salga del rango.
4. **Sin big-bang.** Cada fase deja `develop` verde y la app usable.
5. **El modo test (`cbTest`) y el marcado (`cbMark`) sobreviven** hasta que C
   los reubique.

## Riesgos

| Riesgo | Fase | Mitigación |
|---|---|---|
| Latch que no se limpia → manos perdidas | A | Tests de la secuencia rango → fuera de rango → rango |
| Carrera entre el loop, `StartAsync` y `Reset` | A | `_latched` volatile; `Reset` solo desde `StartAsync` y tests |
| Watchdog de 15 s contra OCR lento | B | `WatchdogTimeoutMs` configurable; telemetría antes de activar el flag |
| Decisión duplicada durante el corte | B | Un solo disparador: el detector deja de llamar al pipeline |
| Regresión de postflop al retirar el legacy | C | Paridad por calle con capturas reales |

## Estrategia de paridad

- Detección: los tests existentes de `PerformEnhancedDetection` siguen pasando
  sin cambios; se agregan los del gate.
- Pipeline: replay de `output/samples/grab/*.png` (4 capturas reales) sobre el
  camino vigente y el nuevo, comparando `ResponseAction`.
- Métricas: `IMetricsCollector` ya expone `CaptureScreenshot` y `CycleTotal`;
  son la evidencia de B, no una impresión.
