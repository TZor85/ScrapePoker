# Mejora OCR Nombres — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `FrmMain.cs` | Retry nombres vacíos, CleanOcrPlayerName, GetActiveVillainId con alias, umbral reducido |
| `OpponentTracker.cs` | Seat-alias cache (RegisterSeatAlias, ResolveName) |

---

## Mejora 1 — Re-leer nombres vacíos en cada iteración

### Problema
`SetAliasVillain()` se llama solo en `InitializePlayersAsync()` (inicio de mano nueva). Si falla, el nombre queda vacío toda la mano.

### Después
Llamar a un método `RetryEmptyAliases()` en cada iteración del game loop, después de SetActivePlayer. Solo re-lee los jugadores activos que tienen `Alias` vacío:

```csharp
private void RetryEmptyAliases()
{
    var nameRegions = _regionsTableMap?.FirstOrDefault(x => x.Id == "Names");
    if (nameRegions?.Regions == null) return;

    foreach (var player in _playerGameState.Players.Where(p => p.Active && string.IsNullOrEmpty(p.Alias)))
    {
        var regionName = $"p{player.ValuePosition}Name";
        var region = nameRegions.Regions.FirstOrDefault(r => r.Name == regionName);
        if (region == null) continue;

        var scaled = ScaleRegion(region);
        var rawName = SetTextOCR(scaled.X, scaled.Y, scaled.Width, scaled.Height,
            region.Umbral, region.InactiveUmbral, region.IsOnlyNumber);
        var cleanName = CleanOcrPlayerName(rawName);

        if (!string.IsNullOrEmpty(cleanName))
        {
            player.Alias = cleanName;
            _opponentTracker.RegisterSeatAlias(player.Name!, cleanName);
        }
    }
}
```

Punto de inserción en game loop: después de `SetActivePlayer()` y antes de `ProcessTableInfoAsync()`.

---

## Mejora 2 — Umbral reducido + lectura con consenso

### Problema
Umbral 0.87 (brightness 221) descarta colores comunes de nombres en salas de poker (grises 180-200, amarillos 200-210).

### Después — SetTextOCR para nombres
Crear un overload o adaptar el flujo para nombres: 2 lecturas con umbrales diferentes + consenso.

```csharp
private string ReadPlayerNameOCR(int x, int y, int w, int h, double umbral, double inactiveUmbral)
{
    // Lectura 1: umbral estándar (0.80)
    string read1;
    using (var ocrResult1 = _ocrService.ExtractTextFromRegionAndDebug(
        _formImage.pbImage.Image, x, y, w, h, umbral, false))
    {
        read1 = CleanOcrPlayerName(ocrResult1.Text);
    }

    // Lectura 2: umbral bajo (inactive, ~0.30) para capturar más colores
    string read2;
    using (var ocrResult2 = _ocrService.ExtractTextFromRegionAndDebug(
        _formImage.pbImage.Image, x, y, w, h, inactiveUmbral, false))
    {
        read2 = CleanOcrPlayerName(ocrResult2.Text);
    }

    // Consenso: si ambas coinciden → resultado seguro
    if (!string.IsNullOrEmpty(read1) && read1 == read2)
        return read1;

    // Si solo una tiene resultado → usar la más larga (más probable que sea correcta)
    if (string.IsNullOrEmpty(read1)) return read2;
    if (string.IsNullOrEmpty(read2)) return read1;
    return read1.Length >= read2.Length ? read1 : read2;
}
```

### Cambio de umbral en Regiones.json
No se modifica el JSON (mantiene 0.87 como referencia). El código usa 0.80 para la primera lectura:

```csharp
// En SetAliasVillain, al calcular umbral:
double nameUmbral = Math.Min(region.Umbral, 0.80); // Máximo 0.80 para nombres
```

---

## Mejora 3 — Limpieza post-OCR

### Problema
Tesseract devuelve caracteres basura para nombres: `"..Player_A"`, `"P1ayer"`, `"|"`, `"___"`.

### Después

```csharp
/// <summary>
/// Limpia resultado OCR de nombre de jugador: trim, eliminar no alfanuméricos,
/// validar longitud mínima.
/// </summary>
private static string CleanOcrPlayerName(string? rawName)
{
    if (string.IsNullOrWhiteSpace(rawName))
        return string.Empty;

    // Eliminar caracteres no alfanuméricos (excepto _, -, espacio)
    var cleaned = System.Text.RegularExpressions.Regex.Replace(rawName.Trim(), @"[^a-zA-Z0-9_\- ]", "");

    // Eliminar espacios/underscores al inicio/final
    cleaned = cleaned.Trim(' ', '_', '-');

    // Nombre debe tener al menos 2 caracteres
    return cleaned.Length >= 2 ? cleaned : string.Empty;
}
```

### Tabla de limpieza

| Input OCR | Después de limpieza | Válido? |
|-----------|-------------------|---------|
| `"Player_A"` | `"Player_A"` | ✓ |
| `"..P1ayer"` | `"P1ayer"` | ✓ |
| `"|"` | `""` | ✗ (< 2 chars) |
| `"___"` | `""` | ✗ |
| `" Hero123 "` | `"Hero123"` | ✓ |
| `"Ál€x"` | `"lx"` | ✗ (< 2 chars, unicode removed) |
| `"P3-name_1"` | `"P3-name_1"` | ✓ |

---

## Mejora 4 — GetActiveVillainId con alias real

### Antes
```csharp
private string GetActiveVillainId()
{
    var villain = _playerGameState.Players
        .Where(p => p.Active && !string.IsNullOrEmpty(p.Name))
        .OrderByDescending(p => p.Bet)
        .FirstOrDefault();
    return villain?.Name ?? "Unknown";  // Retorna "P3", "P1", etc.
}
```

### Después
```csharp
private string GetActiveVillainId()
{
    var villain = _playerGameState.Players
        .Where(p => p.Active && !string.IsNullOrEmpty(p.Name))
        .OrderByDescending(p => p.Bet)
        .FirstOrDefault();
    if (villain == null) return "Unknown";

    // Preferir alias real (nombre OCR) sobre seat name (P0, P1)
    if (!string.IsNullOrEmpty(villain.Alias))
        return villain.Alias;

    // Fallback: buscar alias cacheado en OpponentTracker por seat
    return _opponentTracker.ResolveName(villain.Name!) ?? villain.Name!;
}
```

---

## Mejora 5 — Seat-alias cache en OpponentTracker

### Problema
Si un nombre se lee correctamente en mano 5 pero falla en mano 6, el perfil de mano 5 se pierde porque el tracker busca por seat ("P3") en vez de alias.

### Después — OpponentTracker.cs

```csharp
// Mapeo seat → alias, persistente durante la sesión
private readonly Dictionary<string, string> _seatAliasCache = new(StringComparer.OrdinalIgnoreCase);

/// <summary>
/// Registra asociación seat → alias. Si el alias cambia para un seat
/// (nuevo jugador), transfiere el perfil existente o crea uno nuevo.
/// </summary>
public void RegisterSeatAlias(string seatName, string alias)
{
    if (string.IsNullOrWhiteSpace(seatName) || string.IsNullOrWhiteSpace(alias))
        return;

    _seatAliasCache[seatName] = alias;

    // Si ya existe perfil por seat name, migrar al alias
    if (_profiles.TryGetValue(seatName, out var seatProfile) &&
        !_profiles.ContainsKey(alias))
    {
        seatProfile.PlayerId = alias;
        _profiles[alias] = seatProfile;
        _profiles.Remove(seatName);
    }
}

/// <summary>
/// Resuelve un seat name a su alias conocido (o null si no hay).
/// </summary>
public string? ResolveName(string seatName)
{
    return _seatAliasCache.TryGetValue(seatName, out var alias) ? alias : null;
}
```

### Flujo integrado

```
Mano 1: SetAliasVillain → P3.Alias = "PlayerA"
         → RegisterSeatAlias("P3", "PlayerA")
         → RecordHandPlayed("PlayerA") ✓

Mano 2: SetAliasVillain → P3.Alias = "" (OCR falló)
         → RetryEmptyAliases → sigue vacío
         → GetActiveVillainId → villain.Alias vacío
         → _opponentTracker.ResolveName("P3") → "PlayerA" ← del cache
         → RecordHandPlayed("PlayerA") ✓ (sin pérdida)

Mano 3: Nuevo jugador en seat P3 → P3.Alias = "PlayerB"
         → RegisterSeatAlias("P3", "PlayerB") → actualiza cache
         → RecordHandPlayed("PlayerB") ✓
```

---

## Resumen de cambios

| Componente | Cambio | Esfuerzo |
|-----------|--------|----------|
| FrmMain.RetryEmptyAliases | Re-leer nombres vacíos cada game loop | Bajo |
| FrmMain.ReadPlayerNameOCR | 2 lecturas + consenso, umbral 0.80 | Medio |
| FrmMain.CleanOcrPlayerName | Regex limpieza, min 2 chars | Bajo |
| FrmMain.GetActiveVillainId | Alias > seat, fallback ResolveName | Bajo |
| FrmMain.SetAliasVillain | Usar ReadPlayerNameOCR + RegisterSeatAlias | Bajo |
| OpponentTracker | RegisterSeatAlias, ResolveName, migración de perfiles | Medio |
