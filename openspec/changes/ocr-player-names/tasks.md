# Mejora OCR Nombres — Tareas de Implementación

## Tasks

### 1. CleanOcrPlayerName helper
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar método estático `CleanOcrPlayerName(string?)` que: trim, elimina caracteres no alfanuméricos (excepto `_-` y espacio), trim bordes, valida longitud >= 2.
- **Verificación:** `"..Player_A"` → `"Player_A"`, `"|"` → `""`, `"___"` → `""`.

### 2. ReadPlayerNameOCR con consenso
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar método `ReadPlayerNameOCR(int x, int y, int w, int h, double umbral, double inactiveUmbral)`. 2 lecturas (umbral estándar + inactiveUmbral), limpieza con `CleanOcrPlayerName`, consenso (ambas iguales → seguro, sino la más larga).
- **Verificación:** Test manual: lectura con umbral 0.80 y 0.30 produce consenso.

### 3. Actualizar SetAliasVillain para usar ReadPlayerNameOCR
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** En `SetAliasVillain`, reemplazar `SetTextOCR(...)` por `ReadPlayerNameOCR(...)` con umbral reducido (`Math.Min(region.Umbral, 0.80)`). Después de asignar alias, llamar `_opponentTracker.RegisterSeatAlias(player.Name, cleanName)`.
- **Verificación:** Nombres se leen con umbral más permisivo.

### 4. RetryEmptyAliases
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Agregar método `RetryEmptyAliases()` que itera jugadores activos con `Alias` vacío y re-lee su nombre. Llamar en el game loop después de `SetActivePlayer()`, antes de `ProcessTableInfoAsync()`.
- **Verificación:** Si un nombre falla en primera lectura, se recupera en la siguiente iteración.

### 5. GetActiveVillainId con alias real
- **Archivo:** `src/OpenScrape.App/Forms/FrmMain.cs`
- **Acción:** Modificar `GetActiveVillainId()`: preferir `villain.Alias` sobre `villain.Name`. Si alias vacío, usar `_opponentTracker.ResolveName(villain.Name)` como fallback.
- **Verificación:** Con alias "PlayerA" → retorna "PlayerA". Sin alias → retorna seat cacheado o "P3".

### 6. Seat-alias cache en OpponentTracker
- **Archivo:** `src/OpenScrape.DecisionMaker/Services/OpponentTracker.cs`
- **Acción:** Agregar `_seatAliasCache` dictionary. Métodos `RegisterSeatAlias(seatName, alias)` y `ResolveName(seatName)`. En RegisterSeatAlias, migrar perfil de seat a alias si existe.
- **Verificación:** Test: RegisterSeatAlias("P3", "PlayerA") → ResolveName("P3") retorna "PlayerA". Perfil migra de "P3" a "PlayerA".

### 7. Tests
- **Archivo:** `OpenScrape.App.Tests/`
  - Test: CleanOcrPlayerName con varios inputs (basura, válidos, cortos)
  - Test: OpponentTracker RegisterSeatAlias + ResolveName
  - Test: OpponentTracker RegisterSeatAlias migra perfil existente
- **Verificación:** `dotnet test OpenScrape.sln` — todos pasan.

### 8. Build y test final
- **Acción:**
  ```bash
  dotnet clean OpenScrape.sln && dotnet build OpenScrape.sln
  dotnet test OpenScrape.sln
  dotnet format --verify-no-changes OpenScrape.sln
  ```
- **Verificación:** Build OK, 0 tests fallidos, formato correcto.
