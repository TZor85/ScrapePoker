## Why

El código actual genera nombres de carpetas con formato `Game_YYYY_MM_DD` (ej. `Game_2026_04_19`), pero las carpetas ya existentes en `resources/Games` siguen el formato `AAAAMMDD_Game` (ej. `20260419_Game`). Esta inconsistencia dificulta la organización y ordenación cronológica de las sesiones de juego.

## What Changes

- Modificar el formato de nombres de carpetas en `FrmMain.cs` de `Game_YYYY_MM_DD` a `AAAAMMDD_Game`
- Actualizar dos ubicaciones donde se genera el path de carpeta:
  - Línea ~1780: Guardado de resume
  - Línea ~2487: Captura de screenshots

## Capabilities

### New Capabilities

No se introducen nuevas capabilities.

### Modified Capabilities

- `game-session-storage`: Corrección del formato de nombres de carpetas para mantener consistencia con el estándar existente en `resources/Games`

## Impact

- Código afectado: `src/OpenScrape.App/Forms/FrmMain.cs` (2 ubicaciones)
- Sin cambios en APIs ni dependencias externas
- Compatible con carpetas existentes (el cambio es solo de formato de nomenclatura)