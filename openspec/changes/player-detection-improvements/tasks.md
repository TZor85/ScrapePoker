# Mejora Detección de Jugadores — Tareas de Implementación

## Tasks

### 1. Nuevo campo HasFolded en Player
- **Archivo:** `src/OpenScrape.App/Entities/Player.cs`
- **Acción:** Agregar `public bool HasFolded { get; set; }`.
- **Verificación:** Compila.

### 2. Fix SetEmptyPlayer — no marcar SitOut
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `SetEmptyPlayer()`, eliminar `player.SitOut = true` cuando se detecta asiento vacío. Solo marcar `player.Empty = true`.
- **Verificación:** Player vacío tiene Empty=true, SitOut=false.

### 3. Fix SetSitOutPlayer — no marcar Empty
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `SetSitOutPlayer()`, eliminar `player.Empty = true` cuando se detecta "SIT". Solo marcar `player.SitOut = true`.
- **Verificación:** Player sitout tiene SitOut=true, Empty=false.

### 4. IsColorMatch helper con tolerancia
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar método estático `IsColorMatch(int actualB, int[] expectedValues, int tolerance = 5)` que retorna true si cualquier expected value está dentro de ±tolerance del actual. Reemplazar comparaciones directas `_colorPlaying.Contains(color.B)` y `_colorEmpty.Contains(color.B)` por `IsColorMatch()`.
- **Verificación:** B=18 con expected [17] y tolerancia 5 → true. B=25 → false.

### 5. DetectFoldedPlayers
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Nuevo método que en postflop verifica cada villano activo no-hero: si su región de playing ya no muestra el color activo → `HasFolded=true`, `Active=false`. Llamar en `ProcessPostFlopAsync` antes de procesar cada street.
- **Verificación:** Villano que foldea → Active=false en siguiente street.

### 6. RefreshPlayerStates
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Nuevo método que re-evalúa Empty por color para jugadores no-hero. Si cambió de no-empty a empty → desactivar. Llamar en el else branch del game loop junto a `SetActivePlayer()`.
- **Verificación:** Jugador que se va mid-session detectado como Empty.

### 7. ValidatePlayerStates
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Nuevo método de validación cruzada: si !Active && !Empty && !SitOut && alias vacío && stack=0 && bet=0 → marcar Empty. Llamar después de `RetryEmptyAliases()`.
- **Verificación:** Jugador sin datos detectado como Empty.

### 8. Integrar en game loop
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:**
  - else branch: agregar `RefreshPlayerStates()` después de `SetActivePlayer()`
  - `ProcessPostFlopAsync`: agregar `DetectFoldedPlayers()` antes de cada ProcessStreetAsync
  - Después de `RetryEmptyAliases()`: agregar `ValidatePlayerStates()`
- **Verificación:** Flujo completo funciona sin errores.

### 9. Revisar dependientes de Empty/SitOut
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Buscar todos los usos de `player.Empty` y `player.SitOut` para verificar que la separación no rompe lógica existente. En particular: `SetDealerPlayer`, `SetVillainPosition`, `numOpponents` calculations.
- **Verificación:** Tests existentes pasan. Dealer detection sigue funcionando.

### 10. Tests
- **Archivo:** `OpenScrape.App.Tests/`
  - Test: IsColorMatch con tolerancia (B=18, expected [17], tol 5 → true)
  - Test: IsColorMatch fuera de tolerancia (B=25, expected [17], tol 5 → false)
  - Test: CleanOcrPlayerName edge cases (ya cubierto)
- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 11. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
