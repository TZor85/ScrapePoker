## Context

ScreenReaderService es el punto de entrada para todas las operaciones OCR. El usuario quiere instrumentar métricas de timing sin modificar OcrService para mantener separation of concerns.

## Goals

- Agregar 5 categorías de métricas que reportan datos de timing
- Cada Measure cubre las 2-3 re-lecturas de consenso internas (1 sample por método público)
- Mantener OcrService sin cambios

## Decisions

- Usar IMetricsCollector existente (verificar si existe)
- Wrappear cada método público con using var _ = _metrics.Measure(category)

## Categories

1. OcrCards - timing de ReadCard y ReadFlopCard
2. OcrBets - timing de ReadBetValue
3. OcrStacks - timing de ReadStackValue
4. OcrHandNumber - timing de ReadHandNumber
5. OcrPlayerNames - timing de ReadPlayerName

## Risks

- Sin riesgos - solo agrega medición, no cambia lógica de negocio