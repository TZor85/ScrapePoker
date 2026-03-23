# Plan de Mejoras — ScrapePoker

Análisis realizado: 2026-03-23

---

## Fase 1: Bugs y Memory Leaks (Estabilidad)

### 1.1 Memory leak OcrResult.Image
- **Problema:** Cada frame del game loop crea 2-3 `OcrResult` con `Bitmap` internos que nunca se liberan (`Dispose`). Acumula ~100+ MB tras 1000 frames.
- **Ubicación:** `FrmMain.cs` — `SetBetValue()`, `SetStackValue()`, `SetHandNumberOCR()`, y múltiples llamadas a `_ocrService.ExtractTextFromRegionAndDebug()`.
- **Fix:** Envolver cada `OcrResult` en `using` o llamar a `.Image?.Dispose()` tras usarlo. Hacer que `OcrResult` implemente `IDisposable`.

### 1.2 _sessionDB (IDocumentSession) nunca se dispone
- **Problema:** `_sessionDB = _dataBase.LightweightSession()` en el constructor mantiene una conexión PostgreSQL abierta toda la vida del formulario, sin `Dispose`.
- **Ubicación:** `FrmMain.cs:205`
- **Fix:** Disponer `_sessionDB` en `FrmMain_FormClosing` o usar `using` por operación. Alternativamente, crear sesiones por uso con `using`.

### 1.3 FormClosing async void sin sincronización
- **Problema:** `SaveSessionAsync()` en `FrmMain_FormClosing` (async void) puede no completarse antes del cierre de la app, perdiendo la última sesión.
- **Ubicación:** `FrmMain.cs:224-232`
- **Fix:** Cancelar el cierre con `e.Cancel = true`, ejecutar save, y luego cerrar programáticamente.

### 1.4 Graphics context sin using en SetBetPlayerBitmap
- **Problema:** `Graphics g` y `Graphics gGray` creados sin `using`, causando GDI+ resource leak.
- **Ubicación:** `FrmMain.cs` — `SetBetPlayerBitmap()`
- **Fix:** Envolver en `using`.

---

## Fase 2: Motor de Decisión (Decisiones incorrectas)

### 2.1 Fallback thresholds hardcodeados silenciosos
- **Problema:** Si falta un threshold en config, usa valores 40/45/55/75 sin log. Puede causar decisiones completamente incorrectas.
- **Ubicación:** `PostflopDecisionService.cs:33-50`
- **Fix:** Loguear warning cuando se usa fallback.

### 2.2 Penalización StraightCompleted sin verificar si hero tiene la escalera
- **Problema:** Reverse implied odds penaliza equity incluso cuando hero completó la escalera.
- **Ubicación:** `PostflopDecisionService.cs:118-120`, `ImpliedOddsCalculator`
- **Fix:** Agregar condición `heroHandRank < HandRank.Straight`.

### 2.3 adjustedThinValueAbove calculado pero no usado en HandleFacingBet
- **Problema:** Se calcula con ajustes por facing bet y agresión, pero HandleFacingBet usa el original sin ajustar.
- **Ubicación:** `PostflopDecisionService.cs:237-346`
- **Fix:** Pasar `adjustedThinValueAbove` como parámetro a HandleFacingBet y usarlo.

### 2.4 Orden de penalizaciones invertido
- **Problema:** El cap de equity en boards peligrosos se aplica DESPUÉS de reverse implied odds, reduciendo menos de lo esperado.
- **Ubicación:** `PostflopDecisionService.cs:111-127`
- **Fix:** Aplicar cap ANTES de reverse implied odds.

---

## Fase 3: Configuración y Validación

### 3.1 Validación incompleta en StrategyProfile.Validate()
- **Problema:** No valida multiplicadores > 0, frecuencias en [0,1], ni penalizaciones <= 100%.
- **Ubicación:** `StrategyProfile.cs:135-167`
- **Fix:** Ampliar Validate() con las reglas faltantes.

### 3.2 SimplifiedIPBet/OOPBet no mapean a StreetThresholds
- **Problema:** JSON tiene 2 campos simplificados pero el modelo requiere 5.
- **Ubicación:** `StreetThresholds.cs:67-71` vs `appsettings.json`
- **Fix:** Unificar nombres o agregar defaults.

### 3.3 Registros DI duplicados
- **Problema:** Mismos servicios registrados por interfaz y por clase concreta.
- **Ubicación:** `Program.cs:52-59`
- **Fix:** Eliminar registros duplicados por clase concreta.

### 3.4 Config huérfana PokerStrategy
- **Problema:** Sección `PokerStrategy` en appsettings nunca se deserializa.
- **Ubicación:** `appsettings.json:16-18`
- **Fix:** Eliminar o migrar a StrategyProfile.

---

## Fase 4: Cobertura de Tests

Servicios sin tests (ordenados por criticidad):
1. `GameLoggerService` (307 líneas) — persistencia de datos
2. `PreflopAnalyzer` — análisis preflop
3. `ImpliedOddsCalculator` — cálculo de implied odds
4. `DangerPenaltyCalculator` — penalizaciones por carta peligrosa
5. `OcrService` (405 líneas) — lectura de pantalla (requiere mocks de Tesseract)

---

## Fase 5: Refinamiento del Motor

### 5.1 Overcards ignorados en manos con gutshot
- `OutsCalculator.cs:78-117`

### 5.2 BetSizing: multiplicador 1.1 heads-up sin justificación
- `BetSizingService.cs:49-52`

### 5.3 Straight draw falso en BoardTextureAnalyzer
- `BoardTextureAnalyzer.cs:224-243`

### 5.4 BetSizing sin factor de street (flop vs river)
### 5.5 Reverse implied odds solo en turn (falta river)
### 5.6 StreetDecision sin campos Reason/BoardTexture/Outs/SPR
### 5.7 Índices Marten insuficientes (compuesto GameSessionId+Timestamp)
