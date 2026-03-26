## Why

El game loop principal de OpenScrape corre cada ~100ms y acumula ineficiencias que degradan el rendimiento en tiempo real: `Bitmap.GetPixel()` con marshaling lento (9 llamadas/frame), LINQ `.Average()` con allocations por frame, búsquedas O(n) repetidas en `_regionsTableMap` (~20 veces/segundo), queries de 52 Cards a Marten sin cache compartido entre UseCases, y sesiones Marten sin `using` que provocan resource leaks.

Estas 5 optimizaciones son quick wins de alto impacto y bajo esfuerzo que reducen allocations, eliminan búsquedas redundantes y corrigen leaks de recursos.

## What Changes

1. **Pixel sampling optimizado** — Reemplazar `Bitmap.GetPixel()` por acceso directo a buffer con `LockBits`/`unsafe` en el hot loop de detección (9 lecturas/frame).
2. **LINQ Average manual** — Sustituir 3x `sampleColors.Average(c => c.X)` por suma manual con `foreach`, eliminando allocations de enumeradores.
3. **Region lookup cache** — Cachear las regiones de `_regionsTableMap` en un `Dictionary<string, Dictionary<string, Region>>` al cargar el mapa, convirtiendo 16+ búsquedas O(n) en O(1).
4. **Card cache singleton** — Extraer la carga de Cards a un servicio singleton `CardCacheService` que carga las 52 cartas una vez al arrancar, eliminando queries redundantes en GetCardsFlop/Turn/RiverUseCase.
5. **Marten session dispose** — Añadir `await using` a las sesiones Marten en los 3 UseCases de cards para evitar resource leaks.

## Capabilities

### New Capabilities

- `region-lookup-cache`: Diccionario pre-computado de regiones indexado por Id y Name, accesible en O(1) desde cualquier punto del game loop.
- `card-cache-singleton`: Servicio singleton que carga y cachea todas las CardDTO al arrancar, compartido entre todos los UseCases de detección de cartas.

### Modified Capabilities

- `pixel-sampling-optimizado`: El muestreo de color en `PerformEnhancedDetection` usa acceso directo al buffer del bitmap en lugar de `GetPixel()`.
- `linq-average-manual`: El cálculo de promedios RGB usa aritmética manual sin LINQ.
- `marten-session-dispose`: Las sesiones de Marten en UseCases de cards se gestionan con `await using`.

## Impact

- **`src/OpenScrape.App/Forms/FrmMain.cs`**: Modificar `PerformEnhancedDetection` (pixel sampling + LINQ average). Reemplazar 16+ `_regionsTableMap?.FirstOrDefault()` por lookups al diccionario cacheado. Inyectar `CardCacheService` y consumirlo en lugar de queries directas.
- **`src/OpenScrape.App/Services/RegionLookupCache.cs`** (nuevo): Diccionario de regiones pre-computado.
- **`src/OpenScrape.App/Services/CardCacheService.cs`** (nuevo): Singleton de cache de cartas.
- **`src/OpenScrape.App/Aplication/UseCases/GetCardsFlopUseCase.cs`**: Inyectar `CardCacheService`, añadir `await using` a sesión Marten.
- **`src/OpenScrape.App/Aplication/UseCases/GetCardsTurnUseCase.cs`**: Idem.
- **`src/OpenScrape.App/Aplication/UseCases/GetCardsRiverUseCase.cs`**: Idem.
- **`src/OpenScrape.App/Program.cs`**: Registrar `CardCacheService` y `RegionLookupCache` en DI.
- **Dependencias**: Ninguna nueva. Se usa `System.Drawing.Imaging` (BitmapData/LockBits) ya disponible.
