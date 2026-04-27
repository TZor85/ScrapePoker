## Context

TableLayoutService maneja la detección del dealer y la asignación de posiciones de jugadores. Necesitamos metricar estas operações para identificar cuellos de botella.

## Goals

- Agregar categoría LayoutDealer para timing de detección de dealer
- Agregar categoría LayoutPositions para timing de asignación de posiciones
- Mantener IMetricsCollector inyección consistente

## Decisions

- Usar IMetricsCollector existente (ya registrado como singleton)
- Wrappear método SetDealerPlayer y método de asignación de posiciones

## Categories

1. LayoutDealer - timing de detección del dealer (SetDealerPlayer)
2. LayoutPositions - timing de asignación de posiciones

## Risks

- Sin riesgos - solo agrega medición