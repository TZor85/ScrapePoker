## Context

GameLoggerService maneja la persistencia de manos. Necesitamos metricar por mano (HandRecord.Telemetry) y por sesión (para persistencia) sin paradoja temporal en last-hand.

## Goals

- HandRecord.Telemetry contiene métricas de la mano al persistir
- Persistence.SaveHand reporta en sesión, no en last-hand
- IMetricsCollector inyectado ya existe como singleton

## Decisions

- StartHand en StartNewHandAsync tras crear _currentHand
- EndHand en FinalizeAndPersistHandAsync ANTES del session.Store
- RecordSessionOnly envuelve el bloque de persistencia

## Implementation

- En StartNewHandAsync (línea ~116): `_metrics.StartHand(handNumber.ToString())`
- En FinalizeAndPersistHandAsync (línea ~210): `_currentHand.Telemetry = _metrics.EndHand()`
- En persistencia (línea ~227): `using var _ = _metrics.RecordSessionOnly("Persistence.SaveHand")`

## Risks

- Sin riesgos - implementación simple de API existente