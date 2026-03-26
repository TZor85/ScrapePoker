## ADDED Requirements

### Requirement: Acceso directo al buffer de píxeles en detección
El método `PerformEnhancedDetection` SHALL usar `Bitmap.LockBits()` con `ImageLockMode.ReadOnly` para acceder al buffer de píxeles en lugar de `Bitmap.GetPixel()`.

#### Scenario: Lectura de 9 píxeles con LockBits
- **GIVEN** un bitmap capturado de la pantalla y coordenadas escaladas de la región de acción
- **WHEN** se ejecuta `PerformEnhancedDetection`
- **THEN** el bitmap se bloquea una sola vez con `LockBits`, se leen los 9 píxeles (1 principal + 8 offsets) directamente del puntero `Scan0`, y se desbloquea con `UnlockBits`

#### Scenario: Bounds checking preservado
- **GIVEN** un píxel con offset que cae fuera de los límites del bitmap
- **WHEN** se calcula la posición `(scaledAction.X + dx, scaledAction.Y + dy)`
- **THEN** el píxel se descarta sin leer del buffer (misma lógica de bounds que antes)

#### Scenario: Resultados idénticos al método anterior
- **GIVEN** un bitmap con los mismos datos de píxeles
- **WHEN** se leen los valores RGB con LockBits vs GetPixel
- **THEN** los valores `avgB`, `avgR`, `avgG` calculados son idénticos

### Requirement: Helper de lectura de píxel
SHALL existir un método helper que encapsule la lectura de un píxel desde el buffer raw, recibiendo el puntero base, stride, coordenadas y bytes por píxel.

#### Scenario: Lectura correcta en formato 24bpp
- **GIVEN** un bitmap con `PixelFormat.Format24bppRgb`
- **WHEN** se lee el píxel en `(x, y)` con el helper
- **THEN** devuelve los bytes en orden BGR del buffer (B = offset+0, G = offset+1, R = offset+2)

#### Scenario: Lectura correcta en formato 32bpp
- **GIVEN** un bitmap con `PixelFormat.Format32bppArgb`
- **WHEN** se lee el píxel en `(x, y)` con el helper
- **THEN** devuelve los bytes en orden BGRA (B = offset+0, G = offset+1, R = offset+2, ignora A)

## MODIFIED Requirements

### Requirement: Detección de color de acción mantiene comportamiento
La detección de `isActionColorInRange` y `shouldCapture` SHALL producir los mismos resultados que antes de la optimización, usando los mismos valores TARGET_B=24 y TOLERANCE=3.
