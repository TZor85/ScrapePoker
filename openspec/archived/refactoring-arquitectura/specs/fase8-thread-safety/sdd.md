# SDD — Fase 8: Thread Safety

## 1. Propósito

Corregir accesos no sincronizados a estado mutable compartido entre hilos (UI thread vs BackgroundWorker).

## 2. Cambios

| Clase | Campo | Fix |
|-------|-------|-----|
| OpponentTracker | `_profiles` | `Dictionary` → `ConcurrentDictionary` |
| OpponentTracker | `_seatAliasCache` | `Dictionary` → `ConcurrentDictionary` |
| AutoCalibrationService | `_decisionsSinceLastCalibration` | `++` → `Interlocked.Increment` |
| FrmMain | `_executeCapture` | `bool` → `volatile bool` |
| FrmMain | `_backgroundExecute` | `bool` → `volatile bool` |
| GameLoggerService | `_sessionTotalHands` | `++` → `Interlocked.Increment` |
| GameLoggerService | DB writes | `SemaphoreSlim(1,1)` para serializar |

## 3. Decisiones

- `ConcurrentDictionary` es drop-in para los patrones de acceso de OpponentTracker
- `volatile` es suficiente para flags booleanos de coordinación
- `SemaphoreSlim` es compatible con async/await (a diferencia de `lock`)
- `_newHand` y `_newTableHand` son UI-thread-only → no requieren cambio
