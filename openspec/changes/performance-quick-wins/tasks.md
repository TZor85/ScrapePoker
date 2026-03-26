## 1. Pixel Sampling Optimizado

- [ ] 1.1 En `FrmMain.cs`, método `PerformEnhancedDetection`: reemplazar el bloque de `bitmap.GetPixel()` (líneas ~4017-4035) por un bloque `LockBits`/`unsafe` que lea los 9 píxeles directamente del buffer (`BitmapData.Scan0`). Mantener la misma lógica de offsets `(-2,0), (2,0), (0,-2), (0,2), (-1,-1), (1,1), (-1,1), (1,-1)` y bounds checking.
- [ ] 1.2 Extraer método helper `ReadPixelFromBuffer(byte* scan0, int stride, int x, int y, int bpp)` que devuelve `(byte R, byte G, byte B)` para mantener el código limpio.

## 2. LINQ Average Manual

- [ ] 2.1 En `FrmMain.cs`, reemplazar las 3 líneas `sampleColors.Average(c => c.B/R/G)` (líneas ~4038-4040) por un bucle `foreach` con acumuladores `int sumR = 0, sumG = 0, sumB = 0` y división final `sumX / count`. Usar las mismas variables `avgB`, `avgR`, `avgG` para no alterar el código downstream.

## 3. Region Lookup Cache

- [ ] 3.1 Crear `src/OpenScrape.App/Services/RegionLookupCache.cs` con:
  - Método `Initialize(List<RegionTableMap> maps)` que construye `Dictionary<string, Dictionary<string, Region>>`.
  - Método `GetRegion(string mapId, string regionName)` que devuelve `Region?`.
  - Método `GetRegions(string mapId)` que devuelve `List<Region>?` (para los casos que iteran sobre todas las regiones de un mapa).
- [ ] 3.2 Registrar `RegionLookupCache` como singleton en `Program.cs`.
- [ ] 3.3 En `FrmMain.cs`, inyectar `RegionLookupCache` y llamar a `Initialize()` cuando se carga `_regionsTableMap`.
- [ ] 3.4 Reemplazar los 16+ `_regionsTableMap?.FirstOrDefault(f => f.Id == "X")?.Regions?.FirstOrDefault(x => x.Name == "Y")` por `_regionCache.GetRegion("X", "Y")` en todo FrmMain.cs.
- [ ] 3.5 Reemplazar los `_regionsTableMap?.FirstOrDefault(f => f.Id == "X")?.Regions` por `_regionCache.GetRegions("X")` donde se itera sobre todas las regiones.

## 4. Card Cache Singleton

- [ ] 4.1 Crear `src/OpenScrape.App/Services/CardCacheService.cs` con:
  - Constructor que recibe `IDocumentStore`.
  - Método `async Task<List<CardDTO>> GetCardsAsync()` con lazy loading thread-safe (`SemaphoreSlim`).
  - Cache interno `List<CardDTO>?` que se llena una vez y se reutiliza.
- [ ] 4.2 Registrar `CardCacheService` como singleton en `Program.cs`.
- [ ] 4.3 Modificar `GetCardsFlopUseCase.cs`:
  - Inyectar `CardCacheService` en el constructor.
  - Reemplazar el bloque `if (_cardsImages == null) { ... session.Query<Card>() ... }` por `_cardsImages = await _cardCache.GetCardsAsync()`.
  - Eliminar el campo `_cardsImages` interno y la lógica de cache propia.
  - Añadir `await using` a cualquier sesión Marten restante.
- [ ] 4.4 Modificar `GetCardsTurnUseCase.cs` con los mismos cambios que 4.3.
- [ ] 4.5 Modificar `GetCardsRiverUseCase.cs` con los mismos cambios que 4.3.

## 5. Marten Session Dispose

- [ ] 5.1 En `GetCardsFlopUseCase.cs`, cambiar `var session = _dataBase.LightweightSession();` por `await using var session = _dataBase.LightweightSession();`.
- [ ] 5.2 En `GetCardsTurnUseCase.cs`, aplicar el mismo cambio.
- [ ] 5.3 En `GetCardsRiverUseCase.cs`, aplicar el mismo cambio.
- [ ] 5.4 Revisar que no haya otros UseCases con sesiones Marten sin `using` y corregirlos si existen.

## 6. Verificación

- [ ] 6.1 Compilar con `dotnet build OpenScrape.sln` y resolver errores.
- [ ] 6.2 Ejecutar tests con `dotnet test OpenScrape.sln` y verificar que todos pasan.
- [ ] 6.3 Verificar manualmente que el game loop de detección sigue funcionando correctamente (colores, regiones, cartas).
