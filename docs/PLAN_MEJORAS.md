# Plan de Mejoras — ScrapePoker

Análisis realizado: 2026-03-23
Todas las fases completadas: 2026-03-23

---

## Fase 1: Bugs y Memory Leaks (Estabilidad) ✅

### 1.1 Memory leak OcrResult.Image ✅
- **Problema:** Cada frame del game loop crea 2-3 `OcrResult` con `Bitmap` internos que nunca se liberan (`Dispose`). Acumula ~100+ MB tras 1000 frames.
- **Fix:** `OcrResult` implementa `IDisposable`. Todos los callsites en `SetBetValue`, `SetStackValue`, `SetHandNumberOCR`, `SetTextOCR`, `btnTestTexto_Click` ahora usan `using` o `.Dispose()` explícito.

### 1.2 _sessionDB (IDocumentSession) nunca se dispone ✅
- **Problema:** `_sessionDB = _dataBase.LightweightSession()` en el constructor mantiene una conexión PostgreSQL abierta toda la vida del formulario.
- **Fix:** Eliminado campo `_sessionDB`. `FrmMain_Load` crea `await using var sessionDB` local. `LoadRegionTableMapAsync` tiene overload sin parámetro que crea su propia sesión.

### 1.3 FormClosing async void sin sincronización ✅
- **Problema:** `SaveSessionAsync()` en `FrmMain_FormClosing` (async void) puede no completarse antes del cierre de la app.
- **Fix:** Cancela cierre con `e.Cancel = true`, espera `SaveSessionAsync()`, y cierra programáticamente con flag `_isClosing`.

### 1.4 Graphics context sin using ✅
- **Problema:** `_papel = CreateGraphics()` sin `using` en `UpdateRegionDisplay()`.
- **Fix:** Envuelto en `using`.

---

## Fase 2: Motor de Decisión (Decisiones incorrectas) ✅

### 2.1 Fallback thresholds hardcodeados silenciosos ✅
- **Problema:** Si falta un threshold en config, usa valores 40/45/55/75 sin log.
- **Fix:** `GetThresholds()` imprime `[WARNING]` a Console con la key faltante.

### 2.2 Penalización en boards peligrosos sin verificar hero ✅
- **Problema:** Cap `DangerCompletedDrawNoBetCap` se aplicaba incluso cuando hero tenía la escalera/flush completada.
- **Fix:** Condición `heroHasCompletedDraw`: si hero tiene `HandRank >= Straight` con `StraightCompleted`, o `HandRank >= Flush` con `FlushCompleted`, no se aplica el cap.

### 2.3 adjustedThinValueAbove no usado en HandleFacingBet — No era bug ✅
- **Verificado:** `HandleFacingBet` ya recibe y usa `adjustedThinValueAbove` (línea 311). Las otras ocurrencias en `HandleNoBet` y `DetermineSimplifiedAction` usan `thresholds.ThinValueAbove` correctamente (sin facing bet).

### 2.4 Orden de penalizaciones invertido ✅
- **Problema:** Cap de equity en boards peligrosos se aplicaba DESPUÉS de reverse implied odds.
- **Fix:** Cap se aplica ANTES de reverse implied odds para ser más conservador en boards peligrosos.

---

## Fase 3: Configuración y Validación ✅

### 3.1 Validación incompleta en StrategyProfile.Validate() ✅
- **Fix:** +8 validaciones: `DangerFlush/StraightCompletePct` [0,100], bluff frequencies [0,1], `BluffCatchFoldBelowMultiplier` (0,1], bet sizing multipliers > 0, implied odds factors (0,1], coherencia SPR thresholds.

### 3.2 SimplifiedIPBet/OOPBet no mapean a StreetThresholds ✅
- **Problema:** `Flop_RaiseOverLimper` tenía `SimplifiedIPBet`/`SimplifiedOOPBet` (campos inexistentes).
- **Fix:** Reemplazados por los 5 campos correctos: `SimplifiedIPStrongBet`, `SimplifiedIPThinBet`, `SimplifiedOOPStrongBet`, `SimplifiedOOPValueBet`, `SimplifiedOOPThinBet`.

### 3.3 Registros DI duplicados ✅
- **Problema:** Doble registro (interfaz + clase concreta = 2 instancias).
- **Fix:** Forwarding pattern: clase concreta + factory de interfaz = 1 instancia compartida.

### 3.4 Config huérfana PokerStrategy ✅
- **Fix:** Eliminada sección `PokerStrategy` de appsettings (valores ya en `StrategyProfile`). Eliminados campos `_flopBluffFrequency`/`_turnBluffFrequency` de FrmMain (nunca se usaban tras asignarse).

---

## Fase 4: Cobertura de Tests ✅

Tests añadidos (63 nuevos, total: 385):
- **ImpliedOddsCalculatorTests** (18): implied odds factor (river, stack/pot=0, SPR deep/shallow/medio, IP, flush draw, flop vs turn, rango) + reverse implied odds (no-turn, no-bet, null, hero fuerte, flush draw, pair classification)
- **DangerPenaltyCalculatorTests** (11): DangerLevel 0, flush/straight completed, flush draw, board paired, overcard, facing bet multiplier, hero blocks, combinaciones
- **PreflopAnalyzerTests** (34): IsPreflopAggressor (10 cases), HasRangeAdvantageOnBoard (7 scenarios), CalculateCbetAdjustment (5), DetectDonkBet (4), CategorizeOpponentBet (8 cases)

Pendientes (requieren mocking de dependencias externas):
- `GameLoggerService` — requiere mock de IDocumentStore
- `OcrService` — requiere mock de Tesseract

---

## Fase 5: Refinamiento del Motor ✅

### 5.1 Overcards con gutshot — No era bug ✅
- **Verificado:** `straightCompletingRanks.Count >= 2` (OESD) ya permite overcards con gutshot (count=1). Mejorado comentario.

### 5.2 BetSizing: multiplicador 1.1 heads-up eliminado ✅
- **Fix:** Eliminado `adjustedSize *= 1.1` para `numOpponents == 1`. Sin justificación teórica, causaba soberapuestas.

### 5.3 Straight draw en BoardTextureAnalyzer — Algoritmo correcto ✅
- **Verificado:** Gap ≤ 4 para 3 cartas = ventana de 5 ranks. Agregado `OrderBy` explícito y documentación del criterio.

### 5.4 BetSizing con factor de street ✅
- **Fix:** Agregado `BoardPosition street` a `CalculateDynamicBetSize`. Factores: Flop ×0.90, Turn ×1.0, River ×1.10. Retrocompatible.

### 5.5 Reverse implied odds en river ✅
- **Fix:** Extendido de solo turn a turn + river. River aplica penalización ×0.6 (reducida porque no hay más cartas peligrosas). Test existente actualizado.

### 5.6 StreetDecision con campos adicionales ✅
- **Fix:** +4 campos opcionales: `Reason`, `BoardTexture`, `TotalOuts`, `SPR`. 3 callsites actualizados. `FrmHandDetail` muestra los datos adicionales.

### 5.7 Índices Marten insuficientes ✅
- **Fix:** +3 índices: `GameSession.TableName`, compuesto `HandRecord(GameSessionId, Timestamp)`, `HandRecord.HeroPosition`.
