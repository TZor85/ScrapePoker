# Mejora OCR de Nombres de Jugadores

## Why

El OpponentTracker necesita nombres reales de villanos para acumular estadísticas entre manos y sesiones. Actualmente los nombres se leen con OCR una sola vez por mano, con umbral alto (0.87), sin limpieza post-OCR, sin reintentos con consenso, y `GetActiveVillainId()` usa `player.Name` ("P0", "P1") en vez de `player.Alias` (nombre real). Resultado: el tracking agrupa por seat, no por jugador, y si un nombre falla se pierde toda la mano.

Problemas concretos:
1. **Una lectura por mano** — si la interfaz está cargando al momento de leer, el nombre queda vacío toda la mano.
2. **Umbral 0.87 demasiado alto** — brightness threshold 221/255 descarta colores pastel, grises y amarillos comunes en salas de poker.
3. **Sin limpieza post-OCR** — Tesseract devuelve basura ("P1ayer_A", "...", caracteres sueltos) que se guarda directo.
4. **Sin consenso** — los hand numbers usan 3 lecturas; los nombres usan 1 lectura + 1 fallback.
5. **Tracking por seat** — `GetActiveVillainId()` usa `player.Name` ("P3") en vez de `player.Alias`, así que si un jugador cambia de seat pierde historial.
6. **Sin persistencia entre manos** — nombres se resetean con `_playerGameState`, sin vínculo entre manos.

## What Changes

- `FrmMain.SetAliasVillain()`: Re-leer nombres vacíos en cada iteración del game loop (no solo al inicio de mano).
- `FrmMain.SetTextOCR()`: Umbral reducido a 0.80 para nombres. Lectura con 2 intentos + consenso.
- `FrmMain`: Nuevo helper `CleanOcrPlayerName()` para limpieza post-OCR.
- `FrmMain.GetActiveVillainId()`: Usar `player.Alias` cuando disponible, fallback a `player.Name`.
- `OpponentTracker`: Cachear mapping seat↔alias para resiliencia entre manos.

## Capabilities

### Modified Capabilities
- `ocr-player-names`: Lectura más robusta con umbral adaptativo, consenso y limpieza.
- `opponent-tracking-identity`: Tracking por alias real del jugador (no por seat).
- `name-retry`: Re-lectura de nombres vacíos en cada game loop iteration.

### New Capabilities
- `name-cleanup`: Limpieza post-OCR eliminando caracteres basura, trim, min length.
- `seat-alias-cache`: Mapeo seat→alias persistente durante sesión en OpponentTracker.

## Impact

- **`FrmMain.cs`** — SetAliasVillain retry, SetTextOCR umbral, CleanOcrPlayerName, GetActiveVillainId con alias
- **`OpponentTracker.cs`** — Seat-alias cache
- **Sin nuevas dependencias externas.**
