## Context

El método actual `DetermineP0Position` en `PositionCalculator.cs` filtra玩家的 usando `!p.Empty && !p.SitOut`. Esto excluye el asiento del Hero (P0) cuando está marcado como Empty, causando que el cálculo falle porque requiere ambos asientos (dealer y hero) en la lista activa.

## Goals / Non-Goals

**Goals:**
- Corregir el cálculo de posición cuando faltan jugadores
- Incluir siempre el asiento P0 (Hero) en el cálculo
- Mantener compatibilidad con mesas completas (6-max)

**Non-Goals:**
- No تغيير la lógica de asignación de posições de los villanos
- No introducir nuevas posiciones

## Decisions

- **Incluir P0 siempre en activeSeats**: Añadir una excepción para incluir el asiento 0 (Hero) aunque esté marcado como Empty, ya que representa al jugador local que siempre está presente.

  Alternativa considerada: Verificar si P0 está vacío y agregarlo manualmente al inicio de la lista.

## Risks / Trade-offs

- [Risk] P0 marked as Empty pero otros też están vacíos → **Mitigation**: Se verifica solo P0, no se改变 el comportamiento para otros asientos vacíos