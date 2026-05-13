## Why

Necesitamos persistir métricas por mano (HandRecord.Telemetry) y medir el coste de persistencia sin paradoja temporal en last-hand. El agregado por mano permite análisis post-fiesta, mientras que session-only evita contaminar la mano actual.

## What Changes

- StartHand en StartNewHandAsync tras crear _currentHand
- En FinalizeAndPersistHandAsync: _currentHand.Telemetry = _metrics.EndHand() ANTES del session.Store
- Persistence.SaveHand con RecordSessionOnly envolviendo el bloque _dbWriteLock.WaitAsync() + Store + SaveChangesAsync

## Capabilities

### New Capabilities

- `telemetry-persistence`: Métricas de persistencia por mano y sesión

## Impact

- Código afectado: `src/OpenScrape.App/Services/GameLoggerService.cs`, `OpenScrape.Domain/Entities/GameSession.cs`