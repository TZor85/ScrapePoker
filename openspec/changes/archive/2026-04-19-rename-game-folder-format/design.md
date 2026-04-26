## Context

El código actual en `FrmMain.cs` genera nombres de carpetas usando `DateOnly.ToString()` que produce formato `YYYY_MM_DD`. Las carpetas existentes en `resources/Games` usam formato `AAAAMMDD_Game`. El cambio es simple y local a una clase.

## Goals / Non-Goals

**Goals:**
- Unificar formato de nombres de carpetas a `AAAAMMDD_Game`
- Mantener compatibilidad con carpetas existentes

**Non-Goals:**
- No requiere migración de carpetas existentes
- No introduce nuevas features ni cambios en la lógica de negocio

## Decisions

- **Usar `DateTime.Now.ToString("yyyyMMdd")`**: Alternativa más simple que `DateOnly.ToString().Replace("/", "_")`
  - Alternativa considerada: Usar formatting de `DateOnly` però requiere más código
  - Ventaja: Más conciso y claro

## Risks / Trade-offs

- [Risk] Carpeta del día actual no será encontrada si usuario cambió de fecha → **Mitigation**: El código usa `DateTime.Now`, se actualiza automáticamente al día siguiente