## 1. OCR Confidence Scoring

- [x] 1.1 Añadir `Confidence` (float) y `IsHighConfidence` (bool) a `OcrResult`.
- [x] 1.2 Extraer `page.GetMeanConfidence()` en `ExtractTextFromRegionAndDebug`.
- [x] 1.3 En `SetTextOCR`: retry con umbral inactivo cuando `!ocr.IsHighConfidence`, usar resultado de mayor confianza.
- [x] 1.4 En `SetBetValue`: cuando ambos OCR retornan valor, preferir el de mayor confianza.

## 2. Validación Street vs Board Cards

- [x] 2.1 Añadir overload `TryTransition(GameState, int visibleBoardCards)` a `GameLoopStateMachine`.
- [x] 2.2 Validar mínimo de cartas: FlopDetected >= 3, TurnDetected >= 4, RiverDetected >= 5.
- [x] 2.3 Warning si cartas > máximo esperado (posible desfase de state).
- [ ] 2.4 Actualizar llamadas en FrmMain que transicionan a Flop/Turn/River para pasar board card count.

## 3. OpponentTracker Sample Size Granular

- [x] 3.1 Añadir a OpponentProfile: `HasReliableCBetData` (>= 5), `HasReliableAFData` (>= 10), `HasReliableFoldData` (>= 8), `HasReliablePreflopData` (>= 10).
- [x] 3.2 GetAdjustedFoldEquity: usar `HasReliableAFData` en vez de `IsReliable`.
- [x] 3.3 GetFoldToBetPct: usar `HasReliableFoldData` en vez de hardcoded `< 10`.

## 4. Verificación

- [x] 4.1 Build sin errores.
- [x] 4.2 514 tests existentes pasan.
