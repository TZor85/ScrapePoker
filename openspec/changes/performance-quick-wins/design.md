## Context

OpenScrape es una app WinForms (.NET 10.0) cuyo game loop principal corre en un `BackgroundWorker` cada ~100ms. El loop captura la pantalla, detecta colores en regiones predefinidas, ejecuta OCR y toma decisiones de póker. El rendimiento del loop es crítico: cada ms extra de latencia reduce la capacidad de reacción del bot.

Actualmente el loop tiene 5 ineficiencias medibles:
1. `Bitmap.GetPixel()` — 9 llamadas/frame con marshaling P/Invoke por píxel.
2. LINQ `.Average()` — 3 allocations de enumerador por frame.
3. `_regionsTableMap.FirstOrDefault()` — 16+ búsquedas O(n) por ciclo.
4. Queries de Cards duplicadas — 3 UseCases cargan las mismas 52 cartas independientemente.
5. Sesiones Marten sin dispose — resource leak acumulativo.

## Goals / Non-Goals

**Goals:**
- Reducir el tiempo de ejecución del hot loop de detección eliminando allocations y marshaling innecesario.
- Convertir búsquedas O(n) en lookups O(1) para regiones.
- Unificar la carga de cartas en un singleton compartido.
- Corregir resource leaks en sesiones Marten.

**Non-Goals:**
- Reescribir el pipeline de imagen completo (OpenCvSharp/SkiaSharp).
- Optimizar el Monte Carlo simulator (es otro sprint).
- Cambiar la arquitectura del BackgroundWorker a async puro.
- Optimizar el OcrService (cache keys, dHash) — eso es medio impacto, no quick win.

## Decisions

### 1. LockBits + unsafe para pixel sampling

**Decisión**: Reemplazar las 9 llamadas a `bitmap.GetPixel()` por un bloque `LockBits`/`unsafe` que lee directamente del buffer de píxeles.

**Alternativas consideradas**:
- `ColorDetectionService.GetPixelColor()` con GCHandle: ya existe en el proyecto pero requiere pinning manual y no mejora significativamente sobre LockBits.
- SkiaSharp `SKBitmap.GetPixel()`: requiere conversión de `System.Drawing.Bitmap` a `SKBitmap`, añade overhead de conversión.

**Rationale**: `LockBits` es el patrón estándar en .NET para acceso rápido a píxeles. Elimina el marshaling por píxel de `GetPixel()` y permite leer los 9 píxeles en una sola operación de lock/unlock. El bitmap ya es `System.Drawing.Bitmap`, así que no hay conversión.

---

### 2. Suma manual en lugar de LINQ Average

**Decisión**: Reemplazar `sampleColors.Average(c => c.B/R/G)` por un bucle `foreach` con acumuladores `int sumB, sumR, sumG`.

**Alternativas consideradas**:
- `Span<Color>` con stackalloc: requiere cambiar la lista a array fijo, más invasivo.
- Mantener LINQ con array pre-allocado: sigue creando enumeradores internos.

**Rationale**: El cambio es trivial (3 líneas LINQ → 1 foreach + 3 divisiones), elimina 3 allocations de `Func<Color, float>` + enumerador por frame, y es más legible para operaciones simples de promedio.

---

### 3. RegionLookupCache con diccionario anidado

**Decisión**: Crear `RegionLookupCache` que al recibir `List<RegionTableMap>` construye `Dictionary<string, Dictionary<string, Region>>` indexado por `[mapId][regionName]`.

**Alternativas consideradas**:
- Cache inline en FrmMain con campos `_regionAction`, `_regionFlop`, etc.: funciona para los 2 del hot loop pero no escala a las 16+ búsquedas del resto del archivo.
- `FrozenDictionary<>` (.NET 8+): mejor rendimiento de lectura pero requiere conversión desde el diccionario normal, overhead de setup no justificado para ~9 keys.

**Rationale**: Un diccionario anidado convierte O(n×m) en O(1) para todas las búsquedas de regiones. El servicio se inicializa una vez al cargar el mapa de tabla y se reutiliza durante toda la sesión. Patrón simple, sin dependencias extra.

**Interfaz**:
```csharp
public class RegionLookupCache
{
    public void Initialize(List<RegionTableMap> maps);
    public Region? GetRegion(string mapId, string regionName);
    public List<Region>? GetRegions(string mapId);
}
```

---

### 4. CardCacheService singleton

**Decisión**: Crear `CardCacheService` registrado como singleton que carga las 52 CardDTO al primer acceso (lazy) y las comparte con todos los UseCases.

**Alternativas consideradas**:
- Cache en `GetAllCards` feature y reinyectar: rompe la separación de capas (Features no debería ser singleton).
- `LruCache<string, List<CardDTO>>` existente: overhead innecesario para una colección fija de 52 elementos que nunca cambia.
- Precarga en `Program.cs` al arrancar: viable pero acopla el startup al estado de la BD.

**Rationale**: Lazy loading en el singleton evita fallos de arranque si la BD no está disponible, y garantiza una sola query para toda la vida de la aplicación. Los 3 UseCases inyectan el servicio y eliminan su cache interno `_cardsImages`.

**Interfaz**:
```csharp
public class CardCacheService(IDocumentStore documentStore)
{
    public async Task<List<CardDTO>> GetCardsAsync();
}
```

---

### 5. await using en sesiones Marten

**Decisión**: Añadir `await using` a todas las creaciones de `_dataBase.LightweightSession()` en los 3 UseCases.

**Rationale**: Las sesiones Marten implementan `IAsyncDisposable`. Sin `using`, la sesión no se libera hasta que el GC la recolecte, manteniendo conexiones de BD abiertas innecesariamente. El fix es una línea por UseCase.

## Data Flow

### Pixel Sampling (antes → después)

```
ANTES:
bitmap.GetPixel(x,y) → [P/Invoke marshal] → Color  (×9 por frame)
sampleColors.Average(c => c.B)  → [alloc Func + Enumerator] → double  (×3)

DESPUÉS:
bitmap.LockBits() → BitmapData.Scan0 → [unsafe pointer read] → (B,R,G)  (×9, single lock)
sumB/count, sumR/count, sumG/count → double  (zero alloc)
bitmap.UnlockBits()
```

### Region Lookup (antes → después)

```
ANTES:
_regionsTableMap.FirstOrDefault(f => f.Id == "User")  → O(n) scan
  .Regions.FirstOrDefault(x => x.Name == "uAction")   → O(m) scan
  = O(n×m) por lookup, 16+ veces por ciclo

DESPUÉS:
_regionCache.GetRegion("User", "uAction")  → O(1) dictionary lookup
```

### Card Loading (antes → después)

```
ANTES:
GetCardsFlopUseCase  → session.Query<Card>().ToListAsync()  → 52 Cards
GetCardsTurnUseCase  → session.Query<Card>().ToListAsync()  → 52 Cards (duplicado)
GetCardsRiverUseCase → session.Query<Card>().ToListAsync()  → 52 Cards (duplicado)

DESPUÉS:
CardCacheService (singleton, lazy)  → session.Query<Card>().ToListAsync()  → 52 Cards (una vez)
GetCardsFlopUseCase  → _cardCache.GetCardsAsync()  → cache hit
GetCardsTurnUseCase  → _cardCache.GetCardsAsync()  → cache hit
GetCardsRiverUseCase → _cardCache.GetCardsAsync()  → cache hit
```
