# Mejora Detección de Jugadores — Diseño Técnico

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `FrmMain.cs` | SetEmptyPlayer, SetSitOutPlayer, SetActivePlayer, DetectFoldedPlayers, ValidatePlayerStates, game loop |
| `Player.cs` | Nuevo campo HasFolded |

---

## Fix 1 — Separar Empty y SitOut como estados independientes

### Problema
```csharp
// SetEmptyPlayer() actual:
if (colorMatch && region.Name.Contains("empty"))
{
    player.Empty = true;
    player.SitOut = true;  // ← INCORRECTO: Empty ≠ SitOut
}

// SetSitOutPlayer() actual:
if (isSittingOut)
{
    player.SitOut = true;
    player.Empty = true;  // ← INCORRECTO: SitOut ≠ Empty
}
```

### Después — SetEmptyPlayer()
```csharp
if (colorMatch && region.Name.Contains("empty"))
{
    player.Empty = true;
    // NO marcar SitOut — Empty y SitOut son estados distintos
    // Empty = asiento vacío (nadie sentado)
    // SitOut = jugador sentado pero no jugando
}
```

### Después — SetSitOutPlayer()
```csharp
if (isSittingOut)
{
    player.SitOut = true;
    // NO marcar Empty — jugador sigue sentado, puede volver
}
```

### Impacto en código dependiente
- `SetDealerPlayer()` filtra `(Empty || SitOut) && !Active` — esto sigue funcionando correctamente
- `SetVillainPosition()` filtra `!Empty && !SitOut` — sigue correcto
- `numOpponents` usa `p.Active` — no afectado directamente
- Los consumers que usaban `player.Empty` como proxy de "no está jugando" deben usar `player.Empty || player.SitOut`

---

## Fix 2 — Tolerancia en color detection

### Problema
```csharp
// Actual: valores exactos
private readonly int[] _colorPlaying = [17];
private readonly int[] _colorEmpty = [14, 15, 53, 59, 74];
```

Un solo píxel con antialiasing (B=18 en vez de 17) causa falso negativo.

### Después — SetActivePlayer()
```csharp
// Tolerancia ±5 en canal B
private static bool IsColorMatch(int actualB, int[] expectedValues, int tolerance = 5)
{
    return expectedValues.Any(expected => Math.Abs(actualB - expected) <= tolerance);
}

// En SetActivePlayer:
if (IsColorMatch(color.B, _colorPlaying, tolerance: 5))
    player.Active = true;
```

### Después — SetEmptyPlayer()
```csharp
// Mismo helper con tolerancia
if (IsColorMatch(color.B, _colorEmpty, tolerance: 5))
    player.Empty = true;
```

---

## Fix 3 — Detectar fold mid-hand

### Problema
Villano foldea en flop pero sigue `Active=true` en turn/river → `numOpponents` inflado.

### Nuevo campo — Player.cs
```csharp
public bool HasFolded { get; set; }
```

### Nuevo método — DetectFoldedPlayers()
```csharp
/// <summary>
/// Detecta villanos que foldearon durante la mano.
/// Un villano que estaba activo (Active=true) pero ya no muestra color de playing
/// en su región ha foldeado.
/// </summary>
private void DetectFoldedPlayers()
{
    if (!_gameLoopStateMachine.IsFlop && !_gameLoopStateMachine.IsTurn && !_gameLoopStateMachine.IsRiver)
        return;

    var playingRegions = _regionsTableMap?.FirstOrDefault(x => x.Id == "Playing");
    if (playingRegions?.Regions == null || _formImage.pbImage.Image == null) return;

    using var bitmap = new Bitmap(_formImage.pbImage.Image);

    foreach (var player in _playerGameState.Players.Where(p => p.Active && !p.HasFolded && p.Name != "P0"))
    {
        var regionName = $"p{player.ValuePosition}playing";
        var region = playingRegions.Regions.FirstOrDefault(r => r.Name == regionName);
        if (region == null) continue;

        var scaled = GetScaledRegion(region);
        var color = bitmap.GetPixel(scaled.X, scaled.Y);

        if (!IsColorMatch(color.B, _colorPlaying, tolerance: 5))
        {
            player.HasFolded = true;
            player.Active = false;
        }
    }
}
```

**Punto de inserción**: En `ProcessPostFlopAsync`, antes de procesar cada street:
```csharp
// Antes de ProcessFlopAsync / ProcessTurnAsync / ProcessRiverAsync:
DetectFoldedPlayers();
```

---

## Fix 4 — Re-evaluar Empty/SitOut en cada iteración

### Problema
El else branch del game loop solo llama `SetActivePlayer()`:
```csharp
else
{
    SetActivePlayer();
    // SetEmptyPlayer() y SetSitOutPlayer() NO se llaman
}
```

### Después
```csharp
else
{
    SetActivePlayer();
    // Re-evaluar empty/sitout para detectar cambios mid-session
    RefreshPlayerStates();

    if (_playerGameState.Position == TablePosition.None)
    {
        SetDealerPlayer();
        // ...
    }
}
```

### Nuevo método — RefreshPlayerStates()
```csharp
/// <summary>
/// Re-evalúa Empty y SitOut para detectar cambios mid-session
/// (jugador se va o hace sitout sin cambio de mano).
/// Solo actualiza jugadores no-hero.
/// </summary>
private void RefreshPlayerStates()
{
    var emptyRegions = _regionsTableMap?.FirstOrDefault(x => x.Id == "Empty");
    if (emptyRegions?.Regions == null || _formImage.pbImage.Image == null) return;

    using var bitmap = new Bitmap(_formImage.pbImage.Image);

    foreach (var region in emptyRegions.Regions)
    {
        var playerNumber = GetPlayerNumber(region.Name, "empty");
        if (playerNumber == null || playerNumber == "0") continue;

        var player = _playerGameState.Players.FirstOrDefault(f => f.Name == $"P{playerNumber}");
        if (player == null) continue;

        var scaled = GetScaledRegion(region);
        var color = bitmap.GetPixel(scaled.X, scaled.Y);

        bool wasEmpty = player.Empty;
        player.Empty = IsColorMatch(color.B, _colorEmpty, tolerance: 5);

        // Si cambió de no-empty a empty mid-session, desactivar
        if (!wasEmpty && player.Empty)
        {
            player.Active = false;
            player.SitOut = false;
        }
    }
}
```

---

## Fix 5 — Validación cruzada de estado

### Nuevo método — ValidatePlayerStates()
```csharp
/// <summary>
/// Validación cruzada: inferir estado Empty cuando múltiples señales coinciden.
/// Si un jugador no tiene alias, stack=0, bet=0, y no está activo → probablemente Empty.
/// </summary>
private void ValidatePlayerStates()
{
    foreach (var player in _playerGameState.Players.Where(p => p.Name != "P0"))
    {
        // Si no active, no empty, no sitout, pero sin datos → marcar empty
        if (!player.Active && !player.Empty && !player.SitOut &&
            string.IsNullOrEmpty(player.Alias) &&
            player.Stack == 0 && player.Bet == 0)
        {
            player.Empty = true;
        }

        // Si marcado active pero stack=0 y no es hero → probablemente folded o gone
        if (player.Active && player.Stack == 0 && player.Bet == 0 &&
            !player.HasFolded)
        {
            // No marcar directamente, pero log para diagnóstico
            LogDebug($"[WARNING] {player.Name} activo pero stack=0, bet=0 — posible detección incorrecta");
        }
    }
}
```

**Punto de inserción**: Después de `RetryEmptyAliases()` en el game loop:
```csharp
SetBetPlayer();
SetHeroStack();
RetryEmptyAliases();
ValidatePlayerStates();
```

---

## Resumen de cambios

| Componente | Cambio | Esfuerzo |
|-----------|--------|----------|
| Player.cs | Nuevo campo `HasFolded` | Bajo |
| SetEmptyPlayer | No marcar SitOut | Bajo |
| SetSitOutPlayer | No marcar Empty | Bajo |
| SetActivePlayer/SetEmptyPlayer | `IsColorMatch()` con tolerancia ±5 | Bajo |
| DetectFoldedPlayers | Nuevo método, detectar villanos foldeados por color | Medio |
| RefreshPlayerStates | Re-evaluar Empty mid-session | Medio |
| ValidatePlayerStates | Validación cruzada alias+stack+bet | Bajo |
| Game loop | Insertar DetectFoldedPlayers + RefreshPlayerStates + ValidatePlayerStates | Bajo |
