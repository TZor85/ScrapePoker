# US-07 — Diagnóstico y debug operativo

> **Historia de troubleshooting.** Cuando algo va mal — OCR lee basura, regions desplazadas, decisiones erráticas, captura sin valor — Pablo o el dev necesitan ver qué está pasando dentro de la app. Esta historia cubre las herramientas de diagnóstico: pestaña Logs, `FrmDetectionDebug`, `DetectionLoggerService`, indicadores en overlay.

---

## 1. Persona

**Pablo** — durante una sesión nota que el bot recomienda "FOLD" cuando claramente debería decir "CALL". Quiere entender por qué.

**Dev del producto** — está ayudando a Pablo remotamente vía screenshare; necesita ver logs estructurados y poder reproducir el problema.

---

## 2. Historia (formato narrativo)

> **Como** Pablo o el dev,
> **quiero** disponer de herramientas que me permitan inspeccionar lo que el bot ve, lee, decide y por qué — incluyendo logs estructurados, snapshots de captura, y un modo debug que detenga el ciclo y muestre detalle —
> **para** diagnosticar problemas de OCR, calibración de regions, decisiones erráticas o crashes silenciosos sin tener que adivinar.

---

## 3. Criterios de aceptación

### CA-01 — Pestaña Logs con filtros

**Dado** que Pablo abre la pestaña Logs durante o después de una sesión,
**Cuando** el `tbResume: TextBox` se llena vía `TextBoxLoggerProvider`,
**Entonces** Pablo ve bloques estructurados:

```
═══ [HAND #1234 — UTC 14:30:12] ═══
[INFO] Position: BTN — Cards: [Ah Kh] — Stack: 100.0 BB
[INFO] Situation: OpenRaise

═══ [FLOP — Qd 7c 2s] ═══
[DEBUG] Equity pipeline: rawEq=42.3 | dangerPenalty=2.1 | comboBonus=0 | effectiveEq=40.2
[DEBUG] Hand rank: OnePair (Aces) | Kicker: Strong (TPTK)
[DEBUG] Board texture: Coordinated (DangerLevel=2)
[DEBUG] Draws: FlushDraw=false | StraightDraw=false | TotalOuts=5
[INFO] Decision: BET 3.5 BB [VALUE-BET]

═══ [TURN — Jh] ═══
...
```

**Y** el ComboBox de filtro permite: `Errors / Warnings / Debug / All`,
**Y** los logs respetan `LogLevel` configurado en `appsettings.json`.

🟢 Confirmado en `FrmMain` Logs tab + DD-17 + ADR-0009.

### CA-02 — `LogError` dual (TextBox + Console)

**Dado** que ocurre un error capturable,
**Cuando** `_logger.LogError(ex, "context message")` ejecuta,
**Entonces** el mensaje se escribe a **ambos** sinks:
- `tbResume` (TextBox UI) vía `TextBoxLoggerProvider`.
- `Console` (visible si la app se lanzó desde `cmd.exe` o Visual Studio).

**Y** Pablo puede ver errores en la UI sin abrir consola; el dev puede ver desde Visual Studio sin abrir la app.

🟢 Confirmado en DD-17 + comentario en CLAUDE.md.

### CA-03 — `LogDebug` solo a Console (low-noise UI)

**Dado** que el bot lee posiciones, dealer, OCR raw cada ciclo,
**Cuando** `_logger.LogDebug(...)` ejecuta para esos eventos verbose,
**Entonces** los mensajes solo van a Console (NO al TextBox de la UI),
**Y** el `tbResume` se mantiene legible para Pablo (logs estructurados de manos, no spam de captura).

🟢 Confirmado en CLAUDE.md y diseño del logger.

### CA-04 — `DetectionLoggerService` para errores estructurados

**Dado** que el `BackgroundWorker` captura una excepción en una iteración (EC-07),
**Cuando** `_detectionLoggerService.LogDetectionError(ex, captureBitmap?)` ejecuta,
**Entonces** se loggea con metadatos extra:
- Estado del state machine (`CurrentState: FlopAction`).
- Tiempo desde último ciclo exitoso.
- Optional: snapshot del bitmap para análisis offline (guardado en `logs/captures/error_<ts>.png`).

🟡 **Inferido:** servicio existe (`DetectionLoggerService.cs`); guardado de bitmap no confirmado.

### CA-05 — `FrmDetectionDebug` modo step-by-step

**Dado** que Pablo presiona `F12` o un botón de debug,
**Cuando** `FrmDetectionDebug` se abre,
**Entonces** se ofrece un modo donde:
1. El `BackgroundWorker` se pausa.
2. Pablo presiona "Capturar ahora".
3. La app captura el bitmap actual y lo muestra en `PictureBox`.
4. Por cada region del `tableMap.json` activo, dibuja un rectángulo overlay sobre el bitmap.
5. Por cada region, ejecuta OCR y muestra el resultado al lado.
6. Pablo puede comparar visualmente: ¿la region está bien posicionada? ¿el OCR lee correctamente?

🟢 Confirmado en `FrmDetectionDebug.cs` + `FrmDetectionDebug.Designer.cs`.

### CA-06 — `ForceState` para test/debug en game loop

**Dado** que un dev quiere reproducir un escenario de "river ya jugado",
**Cuando** invoca `_gameLoopStateMachine.ForceState(GameState.RiverAction)`,
**Entonces** el state machine valida que el estado destino existe en `_validTransitions` (ADR-0012),
- ✅ Si existe: cambia el estado, loggea `LogWarning("ForceState aplicado: {From} → {To}")`.
- ❌ Si no: loggea `LogError("ForceState rechazado: estado inválido {State}")`, NO cambia.

🟢 Confirmado en `GameLoopStateMachine.ForceState` + EC-15.

### CA-07 — Snapshot de game state en logs

**Dado** que se completa un ciclo de captura (exitoso o no),
**Cuando** `LogDebug` registra el snapshot,
**Entonces** se imprimen las claves del `_playerGameState`:
- `HeroCards`, `BoardCards`, `Pot`, `HeroStack`, `VillainBet`.
- `Position`, `Players[].Active/Folded/SitOut`, `Dealer`.
- `Situation` (`OpenRaise`/`ThreeBet`/etc.).

**Y** en error, esto sirve como evidencia para diagnóstico post-mortem.

🟡 **Inferido:** logs verbose existen; estructura específica no se confirmó al detalle.

### CA-08 — Indicador de calle en `FrmOverlay`

**Dado** que el state machine cambia de estado,
**Cuando** `FrmOverlay.UpdateStreetIndicator(street)` ejecuta,
**Entonces** Pablo ve en el overlay un texto de calle activa: `"PRE"` / `"FLOP"` / `"TURN"` / `"RIVER"` / `"–"`,
**Y** el cambio es inmediato (≤200 ms desde transición).

🟢 Confirmado en `FrmOverlay.cs` + design.md `OpenScrape.App` § overlay.

### CA-09 — Indicador de OCR low confidence

**Dado** que `ScreenReaderService` retorna `IsHighConfidence = false` en una lectura crítica,
**Cuando** Pablo está mirando el overlay,
**Entonces** un icono ⚠️ o cambio de color (amarillo) aparece junto al campo afectado en el overlay,
**Y** Pablo puede decidir si confiar en la decisión recomendada o no.

🔴 **Lacuna:** indicador visual de low confidence NO confirmado en código; spec pendiente Q-APP-14.

### CA-10 — Guardado de captura con problema (un-clic)

**Dado** que Pablo nota una decisión rara,
**Cuando** presiona un botón "Reportar este momento" o `Ctrl+Shift+S`,
**Entonces** la app guarda en `logs/captures/manual_<ts>/`:
- `bitmap.png` (captura actual).
- `state.json` (`_playerGameState` serializado).
- `decision.json` (resultado del último `Calculate + DetermineAction`).
- `tableMap.json` (regions activas).
- `appsettings_active.json` (profile activo redactado, sin secretos).

**Y** Pablo puede enviar este bundle al dev por email para reproducir.

🔴 **Lacuna:** funcionalidad de "snapshot bundle" NO confirmada; spec pendiente.

### CA-11 — Logs persistentes a archivo

**Dado** que la app crashea inesperadamente,
**Cuando** Pablo relanza,
**Entonces** los logs de la sesión anterior están disponibles en `logs/app_<date>.log`,
**Y** Pablo puede inspeccionar el log post-mortem sin haberlo capturado en tiempo real.

🔴 **Lacuna:** sink de archivo NO confirmado en código; hoy `TextBoxLoggerProvider` solo escribe a UI + Console (efímero). Spec pendiente Q-APP-21 (logging strategy).

---

## 4. Diagrama de flujo

```
[Sesión activa, Pablo nota anomalía]
         │
         ▼
Pablo abre pestaña Logs
  ├─ TextBoxLoggerProvider flushea bufferizados
  └─ tbResume llena con bloques estructurados
         │
         ▼ [Pablo aplica filtro Errors]
         │
ComboBox filter → solo LogLevel >= Error visible
         │
         ▼ [Pablo identifica error específico]
         │
   ┌─────┴─────┐
   ▼           ▼
[Si OCR malo]  [Si decisión rara]
   │             │
   ▼             ▼
F12 / botón     Ver bloque ═══ [STREET] ═══
"DetectionDebug" detalle equity pipeline:
   │             rawEq, dangerPenalty,
   │             comboBonus, effectiveEq
FrmDetectionDebug  hand rank, kicker
abre              board texture
   │             draws
   ▼             decision tag
PictureBox       │
+ regions overlay
+ OCR output         │
                     ▼
   │              Pablo entiende qué path
   ▼              activó la decisión
Pablo ajusta
regions en
pestaña Tablas
   │
   ▼
UpdateRegionTableMap
+ RegionLookupCache.Initialize
   │
   ▼
[Resume captura, valida con próxima mano]


[Si crash silencioso]
   │
   ▼
DetectionLoggerService.LogDetectionError
  ├─ Log con state machine + metadata
  ├─ [Lacuna: bitmap snapshot]
  └─ tbResume + Console
   │
   ▼
[Pablo reporta al dev con logs/]


[Force State para reproducir]
   │
   ▼
_gameLoopStateMachine.ForceState(targetState)
  ├─ Valida _validTransitions.ContainsKey(target)
  ├─ ✅ aplica + LogWarning
  └─ ❌ rechaza + LogError
   │
   ▼
Dev ejecuta caso específico sin reproducir mano completa
```

---

## 5. Variantes (caminos secundarios)

### V-01 — Buffer de logs saturado pre-`SetTextBoxTarget`
- `BufferCapacity=1000` saturado durante arranque con `LogLevel.Trace` (EC-16, Q-APP-21).
- Logs perdidos.
- Mitigación: aumentar buffer + sink consola siempre activo.

### V-02 — Crash de proceso (no logueable desde dentro)
- `AccessViolationException` en Tesseract (EC-17) → proceso muere instantáneamente.
- Logs in-memory perdidos.
- Mitigación: sink de archivo persistente (CA-11 lacuna).

### V-03 — Logs llenos de errores idénticos (zombie loop EC-07)
- 1000 ciclos consecutivos con mismo `NullReferenceException`.
- `tbResume` saturado con repetición.
- Mitigación: deduplicación + abort tras N consecutivos (Q-APP-13).

### V-04 — `FrmDetectionDebug` con `BackgroundWorker` activo simultáneo
- Race condition leve: capturar mientras BG worker captura.
- Mitigación esperada: `FrmDetectionDebug.OnShow` pausa BG worker (`_executeCapture = false`); `OnClose` lo reactiva.

🟡 **Inferido:** secuencia esperada; implementación específica no confirmada.

### V-05 — Pablo sin acceso a Console (lanzamiento via Explorer)
- App lanzada con doble-clic — no hay consola visible.
- Solo `tbResume` accesible. `LogDebug` invisibles.
- Mitigación: si `LogLevel` se sube a `Debug`, también escribir al TextBox temporalmente.

### V-06 — Dev remoto sin acceso a la máquina
- Pablo describe el problema verbalmente.
- Sin herramienta de captura → diagnóstico ciego.
- Mitigación: snapshot bundle CA-10 (lacuna).

### V-07 — `ForceState` invocado por error en producción
- Dev olvida deshabilitar el handler de `F12` en build Release.
- Pablo presiona accidentalmente → state machine cambia.
- Mitigación: gate por `FeatureFlags.EnableDebugUI` o solo en `DOTNET_ENVIRONMENT=Development`.

### V-08 — Pablo ve indicador OCR low confidence pero ignora
- ⚠️ aparece en overlay, Pablo decide confiar.
- La decisión es errada por OCR malo.
- Comportamiento esperado: educational tooltip "Confianza baja, considere skip".

---

## 6. Métricas de éxito

| Métrica | Valor objetivo | Fuente |
|---------|----------------|--------|
| **Tiempo medio diagnóstico de OCR malo** | <5 minutos con `FrmDetectionDebug` | feedback Pablo |
| **Cobertura de errores logueados** | 100 % de `LogError` aparece en tbResume + Console | review |
| **Logs estructurados parseables** | bloques `═══ [STREET] ═══` consistentes | regex test |
| **Tasa de crashes diagnosticables** | >80 % con sink de archivo (CA-11) | manual |
| **Snapshot bundle generado** | 100 % completo (5 archivos) | test |
| **Falsos positivos del filtro Errors** | 0 (todos los errors son reales) | review |

---

## 7. Riesgos y mitigaciones

| Riesgo | Severidad | Mitigación actual | Mitigación pendiente |
|--------|:---------:|---|---|
| Buffer logger saturado | 🟡 | sink consola paralelo | Aumentar default + archivo (Q-APP-21) |
| Crash de proceso pierde logs | 🔴 | — | Sink archivo persistente (CA-11) |
| Zombie loop con repetición | 🟡 | try/catch loggea cada vez | Deduplicación (Q-APP-13) |
| Race condition `FrmDetectionDebug` + BG worker | 🟡 | — | Pausa explícita |
| Sin Console en lanzamiento Explorer | 🟡 | — | Sink archivo |
| Diagnóstico remoto ciego | 🟡 | — | Snapshot bundle (CA-10) |
| `ForceState` en producción | 🟡 | validación de estado | Gate por feature flag |
| Indicador low confidence ausente | 🟡 | — | UI overlay (Q-APP-14) |
| Bitmap snapshot lacuna | 🟢 | — | Implementar si demand |

---

## 8. Dependencias

**Otras user stories:**
- US-01 / US-03 generan los eventos a diagnosticar.
- US-02 (configuración inicial) genera regions calibradas que `FrmDetectionDebug` valida.

**Specs por unit:**
- `OpenScrape.App/` — `TextBoxLoggerProvider`, `DetectionLoggerService`, `FrmDetectionDebug`, `FrmOverlay` (indicadores), `GameLoopStateMachine.ForceState`, `OcrService`, `ScreenReaderService`.
- `OpenScrape.Domain/` — modelos de evento (no específicos).

**Externo:**
- File system writeable para `logs/` (CA-11, CA-04).
- Console accesible (parcial: solo si lanzado desde cmd o VS).

---

## 9. Definición de "completado"

✅ Esta historia está completa cuando:

- [ ] CA-01 a CA-11 pasan en testing manual.
- [ ] Sink de archivo persistente implementado (CA-11).
- [ ] Snapshot bundle CA-10 generable con un atajo.
- [ ] `FrmDetectionDebug` pausa BG worker correctamente.
- [ ] Indicador low confidence visible en overlay (Q-APP-14).
- [ ] Dev remoto puede reproducir un problema reportado por Pablo en <30 min con bundle.
- [ ] Tests cubren `LogDetectionError`, `ForceState`, `BufferCapacity`.

---

## 10. Notas

- **Diagnóstico = enabler del soporte:** sin estas herramientas, cualquier bug reportado por Pablo es "no reproducible" para el dev. Son menos visibles que features pero crítico para mantenimiento.
- **Logs estructurados son contrato:** los bloques `═══ [STREET] ═══` y los tags `[VALUE-BET]`, `[BLUFF]`, `[BARREL]` son contrato implícito que tooling externo (parsers, dashboards) puede consumir. Documentar el formato.
- **`FrmDetectionDebug` es el feature menos publicitado pero más útil:** Pablo lo usa una vez por sesión, dev lo usa 10 veces por feature. Vale la pena pulir UX.
- **Snapshot bundle = ticket de soporte:** equivalente a "tcpdump" para support. La feature CA-10 es el upgrade más alto-ROI para soporte remoto.
- **Sin sink de archivo, los crashes son ciegos:** CA-11 lacuna es crítica para diagnóstico post-mortem. Recomendación: priorizar.
- **`ForceState` debe quedar dev-only:** confirmar gate por env o feature flag para producción. EC-15 valida estado pero no gate.
- **Privacidad en bundle:** asegurar que `appsettings_active.json` redacta connection string y `EncryptionKey` antes de zipearlo.
